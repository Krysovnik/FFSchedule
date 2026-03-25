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

        public RouteService(HttpClient httpClient, Map map, MapControl mapControl, List<FireStation> fireStations)
        {
            this.httpClient = httpClient;
            this.map = map;
            this.mapControl = mapControl;
            this.fireStations = fireStations;
        }

        public async Task<RouteResult> BuildRouteFromFireStationAsync(double toLat, double toLon)
        {
            try
            {
                var nearestStation = await FindNearestFireStation(toLat, toLon);
                if (nearestStation == null)
                {
                    return new RouteResult { Success = false, ErrorMessage = "Не удалось найти пожарную часть" };
                }
                return await BuildRouteAsync(nearestStation.Latitude, nearestStation.Longitude, toLat, toLon);
            }
            catch (Exception ex)
            {
                return new RouteResult { Success = false, ErrorMessage = $"Ошибка: {ex.Message}" };
            }
        }

        public async Task<RouteResult> BuildRouteAsync(double fromLat, double fromLon, double toLat, double toLon)
        {
            try
            {
                ClearRoute();

                string coordinates = $"{fromLon.ToString(CultureInfo.InvariantCulture)},{fromLat.ToString(CultureInfo.InvariantCulture)};" +
                           $"{toLon.ToString(CultureInfo.InvariantCulture)},{toLat.ToString(CultureInfo.InvariantCulture)}";

                string osrmUrl = $"http://router.project-osrm.org/route/v1/driving/{Uri.EscapeDataString(coordinates)}?overview=full&geometries=geojson";

                var response = await httpClient.GetAsync(osrmUrl);
                response.EnsureSuccessStatusCode();

                var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

                var code = json.RootElement.GetProperty("code").GetString();
                if (code != "Ok")
                {
                    return new RouteResult { Success = false, ErrorMessage = $"OSRM ошибка: {code}" };
                }

                var routes = json.RootElement.GetProperty("routes");
                if (routes.GetArrayLength() == 0)
                {
                    return new RouteResult { Success = false, ErrorMessage = "Маршрут не найден" };
                }

                var routeGeometry = json.RootElement
                    .GetProperty("routes")
                    .EnumerateArray()
                    .First()
                    .GetProperty("geometry")
                    .GetProperty("coordinates");

                var coordinatesList = new List<Coordinate>();
                foreach (var coord in routeGeometry.EnumerateArray())
                {
                    double lon = coord[0].GetDouble();
                    double lat = coord[1].GetDouble();
                    coordinatesList.Add(new Coordinate(lon, lat));
                }

                double distance = json.RootElement
                    .GetProperty("routes")[0]
                    .GetProperty("distance").GetDouble();

                double duration = json.RootElement
                    .GetProperty("routes")[0]
                    .GetProperty("duration").GetDouble();

                DrawRoute(coordinatesList);

                return new RouteResult
                {
                    Success = true,
                    Distance = distance,
                    Duration = duration
                };
            }
            catch (HttpRequestException ex)
            {
                return new RouteResult { Success = false, ErrorMessage = $"Ошибка сети: {ex.Message}" };
            }
            catch (JsonException ex)
            {
                return new RouteResult { Success = false, ErrorMessage = $"Ошибка разбора JSON: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new RouteResult { Success = false, ErrorMessage = $"Ошибка при построении маршрута: {ex.Message}" };
            }
        }

        private async Task<FireStation> FindNearestFireStation(double toLat, double toLon)
        {
            var coordinates = string.Join(";",
                fireStations.Select(s => $"{s.Longitude.ToString(CultureInfo.InvariantCulture)},{s.Latitude.ToString(CultureInfo.InvariantCulture)}")) +
            $";{toLon.ToString(CultureInfo.InvariantCulture)},{toLat.ToString(CultureInfo.InvariantCulture)}";

            string tableUrl = $"http://router.project-osrm.org/table/v1/driving/{Uri.EscapeDataString(coordinates)}";

            try
            {
                var response = await httpClient.GetAsync(tableUrl);
                response.EnsureSuccessStatusCode();

                var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var durations = json.RootElement.GetProperty("durations");

                var lastColumn = durations.EnumerateArray().Last(); 
                double minDuration = double.MaxValue;
                int nearestIndex = -1;

                for (int i = 0; i < fireStations.Count; i++)
                {
                    var duration = lastColumn[i].GetDouble();
                    if (duration < minDuration)
                    {
                        minDuration = duration;
                        nearestIndex = i;
                    }
                }
                MessageBox.Show($"Найдена станция: {(nearestIndex >= 0 ? fireStations[nearestIndex].Name : "null")}");
                return nearestIndex >= 0 ? fireStations[nearestIndex] : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при поиске ближайшей станции: {ex.Message}");
                return null;
            }
        }

        private void DrawRoute(List<Coordinate> coordinatesList, Color lineColor = default, float lineWidth = 4f)
        {
            var projectedCoords = coordinatesList.Select(c =>
            {
                var p = SphericalMercator.FromLonLat(c.X, c.Y);
                return new Coordinate(p.x, p.y);
            }).ToArray();

            var routeLine = new LineString(projectedCoords);

            var color = lineColor != default ? lineColor : Color.Red;

            var routeFeature = new GeometryFeature
            {
                Geometry = routeLine,
                Styles = new List<IStyle>
                {
                    new VectorStyle
                    {
                        Fill = null,
                        Outline = new Pen(color, lineWidth)
                    }
                }
            };

            var routeLayer = new MemoryLayer
            {
                Name = "Route",
                Features = new[] { routeFeature }
            };

            map.Layers.Add(routeLayer);
            mapControl.Refresh();
        }

        public void ClearRoute()
        {
            var routeLayer = map.Layers.FirstOrDefault(l => l.Name == "Route");
            if (routeLayer != null)
            {
                map.Layers.Remove(routeLayer);
                mapControl.Refresh();
            }
        }
    }
}
