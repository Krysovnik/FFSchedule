using FFSchedule.Models;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.UI;
using Mapsui.UI.Wpf;
using NetTopologySuite.Geometries;
using System;
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

        private readonly HashSet<FireStation> _usedStations = new HashSet<FireStation>();
        private readonly List<string> _additionalLayerNames = new List<string>();
        private (double Lat, double Lon)? _lastTargetLocation;

        public RouteService(HttpClient httpClient, Map map, MapControl mapControl, List<FireStation> fireStations)
        {
            this.httpClient = httpClient;
            this.map = map;
            this.mapControl = mapControl;
            this.fireStations = fireStations;
        }

        public async Task<List<RouteResult>> BuildRoutesByRequirementAsync(double toLat, double toLon, int requiredEquipment)
        {
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
                for (int i = 0; i < selectedStations.Count; i++)
                {
                    var station = selectedStations[i];
                    _usedStations.Add(station);

                    var color = new Color(255, 82, 82);
                    var result = await BuildRouteInternalAsync(
                        selectedStations[i].Latitude,
                        selectedStations[i].Longitude,
                        toLat, toLon, i, color, false);
                    result.Station = selectedStations[i];

                    results.Add(result);
                }
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

                var result = await BuildRouteInternalAsync(
                    nextStation.Latitude, nextStation.Longitude,
                    toLat, toLon,
                    index, color, true);

                result.Station = nextStation;
                return result;
            }
            catch (Exception ex)
            {
                return new RouteResult { Success = false, ErrorMessage = ex.Message };
            }
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
            var durations = json.RootElement.GetProperty("durations");
            var lastColumn = durations.EnumerateArray().Last();

            var stationsWithTime = fireStations
                .Select((station, index) => new { Station = station, Duration = lastColumn[index].GetDouble() })
                .OrderBy(x => x.Duration)
                .Select(x => x.Station)
                .ToList();

            return stationsWithTime;
        }

        private async Task<RouteResult> BuildRouteInternalAsync(double fromLat, double fromLon, double toLat, double toLon, int index, Color color, bool isDash)
        {
            string coordinates = $"{fromLon.ToString(CultureInfo.InvariantCulture)},{fromLat.ToString(CultureInfo.InvariantCulture)};" +
                                 $"{toLon.ToString(CultureInfo.InvariantCulture)},{toLat.ToString(CultureInfo.InvariantCulture)}";

            string osrmUrl = $"http://router.project-osrm.org/route/v1/driving/{Uri.EscapeDataString(coordinates)}?overview=full&geometries=geojson";

            var response = await httpClient.GetAsync(osrmUrl);
            var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            var routeGeometry = json.RootElement.GetProperty("routes")[0].GetProperty("geometry").GetProperty("coordinates");
            var distance = json.RootElement.GetProperty("routes")[0].GetProperty("distance").GetDouble();
            var duration = json.RootElement.GetProperty("routes")[0].GetProperty("duration").GetDouble();

            var coords = routeGeometry.EnumerateArray()
                .Select(c => new Coordinate(c[0].GetDouble(), c[1].GetDouble()))
                .ToList();

            DrawRoute(coords, distance, duration, index, color, isDash);

            return new RouteResult { Success = true, Distance = distance, Duration = duration };
        }

        private void DrawRoute(List<Coordinate> coordinatesList, double distance, double duration, int index, Color color, bool isDash)
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

            string layerName = isDash ? $"Route_Additional_{index}" : $"Route_{index}";
            if (isDash) _additionalLayerNames.Add(layerName);

            var routeLayer = new MemoryLayer
            {
                Name = layerName,
                Features = new[] { routeFeature },
                MaxVisible = 80
            };

            map.Layers.Add(routeLayer);
            mapControl.Refresh();
        }

        public void ClearRoute()
        {
            _usedStations.Clear();
            _additionalLayerNames.Clear();
            _lastTargetLocation = null;

            var layersToRemove = map.Layers.Where(l => l.Name != null && (l.Name.StartsWith("Route"))).ToList();
            foreach (var layer in layersToRemove)
            {
                map.Layers.Remove(layer);
            }
            mapControl.Refresh();
        }
    }
}
