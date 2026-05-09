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
        private readonly Color[] _routeColors =
        {
            new Color(255, 82, 82),
            new Color(76, 175, 80)
        };

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
                    var color = new Color(255, 82, 82);
                    var result = await BuildRouteInternalAsync(
                        selectedStations[i].Latitude,
                        selectedStations[i].Longitude,
                        toLat, toLon, i, color);
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
        private async Task<List<FireStation>> GetSortedFireStations(double toLat, double toLon)
        {
            var coordinates = string.Join(";",
                fireStations.Select(s => $"{s.Longitude.ToString(CultureInfo.InvariantCulture)},{s.Latitude.ToString(CultureInfo.InvariantCulture)}")) +
                $";{toLon.ToString(CultureInfo.InvariantCulture)},{toLat.ToString(CultureInfo.InvariantCulture)}";

            string tableUrl = $"http://router.project-osrm.org/table/v1/driving/{Uri.EscapeDataString(coordinates)}";

            var response = await httpClient.GetAsync(tableUrl);
            response.EnsureSuccessStatusCode();

            var json  = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var durations = json.RootElement.GetProperty("durations");
            var lastColumn = durations.EnumerateArray().Last();

            var stationsWithTime = fireStations
                .Select((station, index) => new { Station = station, Duration = lastColumn[index].GetDouble() })
                .OrderBy(x => x.Duration)
                .Select(x => x.Station)
                .ToList();

            return stationsWithTime;
        }
        private async Task<RouteResult> BuildRouteInternalAsync(double fromLat, double fromLon, double toLat, double toLon, int index, Color color)
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

            DrawRoute(coords, distance, duration, index, color);

            return new RouteResult { Success = true, Distance = distance, Duration = duration };
        }
        private void DrawRoute(List<Coordinate> coordinatesList, double distance, double duration, int index, Color color)
        {
            var projectedCoords = coordinatesList.Select(c =>
            {
                var p = SphericalMercator.FromLonLat(c.X, c.Y);
                return new Coordinate(p.x, p.y);
            }).ToArray();

            var routeFeature = new GeometryFeature { Geometry = new LineString(projectedCoords) };

            routeFeature.Styles.Add(new VectorStyle
            {
                Outline = new Pen(color, 2f),
                MaxVisible = 80
            });

            string infoText = $"#{index + 1}: {distance / 1000:F1} км, {duration / 60:F0} мин";

            routeFeature.Styles.Add(new LabelStyle
            {
                Text = infoText,
                Font = new Font { Size = 10, Bold = true, FontFamily = "Arial" },
                ForeColor = Color.Black,
                CollisionDetection = true,
                MaxWidth = 100,
                MaxVisible = 80
            });

            var routeLayer = new MemoryLayer
            {
                Name = $"Route_{index}",
                Features = new[] { routeFeature },
                MaxVisible = 80
            };

            map.Layers.Add(routeLayer);
            mapControl.Refresh();
        }

        public void ClearRoute()
        {
            var layersToRemove = map.Layers.Where(l => l.Name != null && l.Name.StartsWith("Route")).ToList();
            foreach (var layer in layersToRemove)
            {
                map.Layers.Remove(layer);
            }
            mapControl.Refresh();
        }
    }
}
