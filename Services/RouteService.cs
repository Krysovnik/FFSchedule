using FFSchedule.Container;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.UI;
using Mapsui.UI.Wpf;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using Color = Mapsui.Styles.Color;


namespace FFSchedule.Services
{
    public class RouteService
    {        
        private readonly HttpClient httpClient;
        private readonly Map map;
        private readonly MapControl mapControl;
        private readonly List<FireStation> fireStations;
        private readonly IRouteCache _fileCache;

        private readonly HashSet<FireStation> _usedStations = new HashSet<FireStation>();
        private readonly MemoryLayer _routeLayer;
        private (double Lat, double Lon)? _lastTargetLocation;

        private Dictionary<string, List<RouteResult>> _memoryCache = new();
        private bool _isCacheLoaded = false;

        public RouteService(HttpClient httpClient, Map map, MapControl mapControl, List<FireStation> fireStations, IRouteCache fileCache)
        {
            this.httpClient = httpClient;
            this.map = map;
            this.mapControl = mapControl;
            this.fireStations = fireStations;
            _fileCache = fileCache;

            _routeLayer = new MemoryLayer
            {
                Name = "Shared_Route_Layer",
                MaxVisible = 80,
                Features = new List<GeometryFeature>()
            };
            this.map.Layers.Add(_routeLayer);
        }
        // Вспомогательный метод для ленивой загрузки кэша с диска при первом обращении
        private async Task EnsureCacheLoadedAsync()
        {
            if (!_isCacheLoaded)
            {
                _memoryCache = await _fileCache.LoadCacheAsync();
                _isCacheLoaded = true;
            }
        }

        private string GetCacheKey(double lat, double lon, int requiredEquipment)
        {
            return $"{Math.Round(lat, 5).ToString(CultureInfo.InvariantCulture)};" +
            $"{Math.Round(lon, 5).ToString(CultureInfo.InvariantCulture)};" +
            $"{requiredEquipment}";
        }

        public async Task<List<RouteResult>> BuildRoutesByRequirementAsync(double toLat, double toLon, int requiredEquipment)
        {        
            _lastTargetLocation = (toLat, toLon);
            string cacheKey = GetCacheKey(toLat, toLon, requiredEquipment);

            await EnsureCacheLoadedAsync();

            if (_memoryCache.TryGetValue(cacheKey, out var cachedResults))
            {
                System.Diagnostics.Debug.WriteLine("Маршрут взят из локального кэша");

                _usedStations.Clear();
                var tempFeatures = new List<GeometryFeature>();

                for(int i = 0; i < cachedResults.Count; i++)
                {
                    var res = cachedResults[i];
                    if(res.Station != null)
                    {
                        var station = fireStations.FirstOrDefault(s =>
                        s.Name == res.Station.Name && s.Address == res.Station.Address);

                        if (station != null)
                        {
                            _usedStations.Add(station);

                            res.Station = station;
                        }
                        else
                        {
                            _usedStations.Add(res.Station);
                        }

                    }
                    if(res.Success && res.RawCoordinates != null)
                    {
                        var color = new Color(255, 82, 82);
                        // Переводим DTO-координаты обратно в формат Mapsui/NetTopologySuite
                        var mapsuiCoords = res.RawCoordinates
                            .Select(c => new Coordinate(c.X, c.Y))
                            .ToList();
                        PrepareRouteFeature(mapsuiCoords, res.Distance, res.Duration, i, color, false, tempFeatures);
                    }
                }
                lock (_routeLayer.Features)
                {
                    _routeLayer.Features = tempFeatures;
                }
                mapControl.Refresh();

                return cachedResults.ToList();
            }

            var results = new List<RouteResult>();
            try
            {
                ClearRoute();

                var sortedStations = await GetSortedFireStations(toLat, toLon);

                int currentEquipmentSum = 0;
                var selectedStations = new List<FireStation>();

                foreach (var station in sortedStations)
                {
                    if (currentEquipmentSum >= requiredEquipment) break;

                    selectedStations.Add(station);
                    currentEquipmentSum += station.EquipmentCapacity;
                }

                var tempFeatures = new List<GeometryFeature>();

                for (int i = 0; i < selectedStations.Count; i++)
                {
                    var station = selectedStations[i];
                    _usedStations.Add(station);

                    var color = new Color(255, 82, 82);
                    var result = await BuildRouteInternalAsync(
                        station.Latitude, station.Longitude,
                        toLat, toLon, i, color, false, tempFeatures);
                    result.Station = station;

                    results.Add(result); 
                }
                lock (_routeLayer.Features)
                {
                    _routeLayer.Features = tempFeatures;
                }
                mapControl.Refresh();

                _memoryCache[cacheKey] = results;
                await _fileCache.SaveCacheAsync(_memoryCache);
            }
            catch (Exception ex)
            {
                results.Add(new RouteResult { Success = false, ErrorMessage = ex.Message });
            }
            return results;
        }

        public async Task<RouteResult?> BuildNextAdditionalRouteAsync(double toLat, double toLon)
        {
            try
            {
                var sortedStations = await GetSortedFireStations(toLat, toLon);
                var nextStation = sortedStations.FirstOrDefault(s => !_usedStations.Contains(s));

                if (nextStation == null)
                {
                    throw new Exception("Все доступные пожарные части уже задействованы на карте.");
                }

                _usedStations.Add(nextStation);
                int index = _usedStations.Count - 1;

                var color = new Color(76, 175, 80); 

                var currentFeatures = _routeLayer.Features.Cast<GeometryFeature>().ToList();

                var result = await BuildRouteInternalAsync(
                    nextStation.Latitude, nextStation.Longitude,
                    toLat, toLon,
                    index, color, true, currentFeatures);

                result.Station = nextStation;

                lock (_routeLayer.Features)
                {
                    _routeLayer.Features = currentFeatures;
                }
                mapControl.Refresh();

                return result;
            }
            catch (Exception ex)
            {
                return new RouteResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        public async Task ClearCache()
        {
            _memoryCache.Clear();
            await _fileCache.ClearCacheAsync();
        }

        private async Task<List<FireStation>> GetSortedFireStations(double toLat, double toLon)
        {
            var coordinates = string.Join(";",
                fireStations.Select(s => $"{s.Longitude.ToString(CultureInfo.InvariantCulture)},{s.Latitude.ToString(CultureInfo.InvariantCulture)}")) +
                $";{toLon.ToString(CultureInfo.InvariantCulture)},{toLat.ToString(CultureInfo.InvariantCulture)}";

            string tableUrl = $"http://router.project-osrm.org/table/v1/driving/{Uri.EscapeDataString(coordinates)}";

            var response = await httpClient.GetAsync(tableUrl);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var json = await JsonDocument.ParseAsync(stream);

            var durationsElement = json.RootElement.GetProperty("durations");
            var durations = durationsElement.EnumerateArray().ToList();

            int targetIndex = durations.Count - 1;

            var stationsWithTime = fireStations
                .Select((station, index) =>
                {
                    var durationRow = durations[index].EnumerateArray().ToList();
                    var durationToTarget = durationRow[targetIndex].GetDouble();

                    return new { Station = station, Duration = durationToTarget };
                })
                .OrderBy(x => x.Duration)
                .Select(x => x.Station)
                .ToList();

            return stationsWithTime;
        }

        private async Task<RouteResult> BuildRouteInternalAsync(double fromLat, double fromLon, double toLat, double toLon, int index, Color color, bool isDash, List<GeometryFeature> outputFeatures)
        {
            string coordinates = $"{fromLon.ToString(CultureInfo.InvariantCulture)},{fromLat.ToString(CultureInfo.InvariantCulture)};" +
                                 $"{toLon.ToString(CultureInfo.InvariantCulture)},{toLat.ToString(CultureInfo.InvariantCulture)}";

            string osrmUrl = $"http://router.project-osrm.org/route/v1/driving/{Uri.EscapeDataString(coordinates)}?overview=full&geometries=geojson";

            var response = await httpClient.GetAsync(osrmUrl);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var json = await JsonDocument.ParseAsync(stream);

            var routeGeometry = json.RootElement.GetProperty("routes")[0].GetProperty("geometry").GetProperty("coordinates");
            var distance = json.RootElement.GetProperty("routes")[0].GetProperty("distance").GetDouble();
            var duration = json.RootElement.GetProperty("routes")[0].GetProperty("duration").GetDouble();

            var coords = routeGeometry.EnumerateArray()
                .Select(c => new Coordinate(c[0].GetDouble(), c[1].GetDouble()))
                .ToList();

            PrepareRouteFeature(coords, distance, duration, index, color, isDash, outputFeatures);

            // Изменение: Сохраняем координаты в создаваемый RouteResult, чтобы они попали в JSON кэш
            var rawCoordsDto = coords.Select(c => new CoordinateDto { X = c.X, Y = c.Y }).ToList();

            return new RouteResult { Success = true, Distance = distance, Duration = duration, RawCoordinates = rawCoordsDto };
        }

        private void PrepareRouteFeature(List<Coordinate> coordinatesList, double distance, double duration, int index, Color color, bool isDash, List<GeometryFeature> outputFeatures)
        {
            var projectedCoords = coordinatesList.Select(c =>
            {
                var p = SphericalMercator.FromLonLat(c.X, c.Y);
                return new Coordinate(p.x, p.y);
            }).ToArray();

            var routeFeature = new GeometryFeature { Geometry = new LineString(projectedCoords) };

            var pen = new Pen(color, 2f);
            if (isDash)
            {
                pen.PenStyle = PenStyle.Dash;
            }

            routeFeature.Styles.Add(new VectorStyle
            {
                Outline = pen,
                MaxVisible = 80
            });

            string prefix = isDash ? "Доп. #" : "#";
            string infoText = $"{prefix}{index + 1}: {distance / 1000:F1} км, {duration / 60:F0} мин";

            routeFeature.Styles.Add(new LabelStyle
            {
                Text = infoText,
                Font = new Font { Size = 10, Bold = true, FontFamily = "Arial" },
                ForeColor = Color.Black,
                CollisionDetection = true,
                MaxWidth = 100,
                MaxVisible = 80
            });

            outputFeatures.Add(routeFeature);
        }

        public void ClearRoute()
        {
            _usedStations.Clear();
            _lastTargetLocation = null;

            lock (_routeLayer.Features)
            {
                _routeLayer.Features = new List<GeometryFeature>();
            }

            mapControl.Refresh();
        }
    }
}
