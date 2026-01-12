using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Nts.Extensions;
using Mapsui.Projections;
using Mapsui.Providers;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI;
using Mapsui.UI.Wpf;
using Mapsui.Utilities;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.Marshalling;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace FFSchedule
{
    public partial class MainWindow : Window
    {
        private bool fireStationsVisible = true;
        private bool villageCouncilsVisible = true;
        public MainWindow()
        {
            InitializeComponent();
            InitializeMap();
        }
        public void InitializeMap()
        {
            var map = new Map();

            //Тайловая подложка
            map.Layers.Add(OpenStreetMap.CreateTileLayer());

            //Районы
            LoadGeoJsonLayer(map, @"MapVector\nskDISTandKSTV.geojson");

            //Точки пч/псч
            LoadGeoJsonLayer(map, @"MapVector\FireStationPoints.geojson");

            //Начальный вид
            SetInitialView(map);

            //Контролер lib
            MapControl.Map = map;

            MapControl.MouseLeftButtonDown += MapControl_MouseLeftButtonDown;
        }
        private void MapControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var screenPosition = e.GetPosition(MapControl);
            var worldPosition = MapControl.Map.Navigator.Viewport.ScreenToWorld(
                screenPosition.X,
                screenPosition.Y
            );

            var layer = MapControl.Map.Layers.FirstOrDefault(l => l.Name == "Points");
            if (layer is MemoryLayer memoryLayer)
            {
                var features = memoryLayer.GetFeatures(
                    new MRect(worldPosition.X, worldPosition.Y, worldPosition.X, worldPosition.Y),
                    MapControl.Map.Navigator.Viewport.Resolution
                );

                var closest = features.FirstOrDefault();

                if (closest != null)
                {
                    var name = closest["name"]?.ToString()
                            ?? "Без названия";

                    MessageBox.Show(name, "Маркер", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        //Menu
        private void RefreshMap_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MapControl.Map?.Layers != null)
                {
                    MapControl.Map.Layers.Clear();
                    MapControl.Map.Layers.Add(OpenStreetMap.CreateTileLayer());
                    LoadGeoJsonLayer(MapControl.Map, @"MapVector\nskDISTandKSTV.geojson");
                    if (fireStationsVisible)
                    {
                        LoadGeoJsonLayer(MapControl.Map, @"MapVector\FireStationPoints.geojson");
                    }
                    SetInitialView(MapControl.Map);
                    MapControl.Refresh();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении карты: {ex.Message}",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        //Кнопки
        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            if (MapControl.Map?.Navigator != null)
            {
                MapControl.Map.Navigator.ZoomIn();
            }
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            if (MapControl.Map?.Navigator != null)
            {
                MapControl.Map.Navigator.ZoomOut();
            }
        }
        private void FireStationsToggleButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleFireStationsVisibility();
        }
        private void VillageCouncilsToggleButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleVillageCouncilsVisibility();
        }
        private void ToggleVillageCouncilsVisibility()
        {
            villageCouncilsVisible = !villageCouncilsVisible;
            VillageCouncilsToggleButton.Content = villageCouncilsVisible ? "📖" : "📕";
            VillageCouncilsToggleButton.ToolTip = villageCouncilsVisible
                ? "Скрыть сельсоветы"
                : "Показать сельсоветы";
            if (MapControl.Map?.Layers != null)
            {
                var pointsLayer = MapControl.Map.Layers.FirstOrDefault(l => l.Name == "Polygons");
                if (pointsLayer != null)
                {
                    pointsLayer.Enabled = villageCouncilsVisible;
                    MapControl.Refresh();
                }
            }
        }
        private void ToggleFireStationsVisibility()
        {
            fireStationsVisible = !fireStationsVisible;
            FireStationsToggleButton.Content = fireStationsVisible ? "⬤" : "○";
            FireStationsToggleButton.ToolTip = fireStationsVisible
                ? "Скрыть пожарные части"
                : "Показать пожарные части";

            if (MapControl.Map?.Layers != null)
            {
                var pointsLayer = MapControl.Map.Layers.FirstOrDefault(l => l.Name == "Points");
                if (pointsLayer != null)
                {
                    pointsLayer.Enabled = fireStationsVisible;
                    MapControl.Refresh();
                }
            }
        }

        //загрузка и отрисовка векторного слоя
        private void LoadGeoJsonLayer(Map map, string geojsonPath)
        {
            if (map == null || string.IsNullOrEmpty(geojsonPath)) return;
            if (!File.Exists(geojsonPath))
            {
                MessageBox.Show($"Файл GeoJSON не найден: {geojsonPath}");
                return;
            }

            try
            {
                string geojson = File.ReadAllText(geojsonPath);
                var reader = new NetTopologySuite.IO.GeoJsonReader();
                var fc = reader.Read<NetTopologySuite.Features.FeatureCollection>(geojson);

                var polygonFeatures = new List<Mapsui.Nts.GeometryFeature>();
                var pointFeatures = new List<Mapsui.Nts.GeometryFeature>();

                foreach (var f in fc)
                {
                    var geom = f.Geometry;

                    // Полигоны
                    if (geom is NetTopologySuite.Geometries.Polygon || geom is NetTopologySuite.Geometries.MultiPolygon)
                    {
                        foreach (var polygon in ConvertGeometry(geom))
                        {
                            var projectedCoords = polygon.Coordinates
                                .Select(c =>
                                {
                                    var p = Mapsui.Projections.SphericalMercator.FromLonLat(c.X, c.Y);
                                    return new NetTopologySuite.Geometries.Coordinate(p.x, p.y);
                                }).ToArray();

                            var projectedPolygon = polygon.Factory.CreatePolygon(projectedCoords);

                            string nameAttr = f.Attributes.Exists("name") ? f.Attributes["name"]?.ToString() : null;

                            var feature = new Mapsui.Nts.GeometryFeature
                            {
                                Geometry = projectedPolygon,
                                Styles = new List<IStyle>
                        {
                            VectorStyles.GetPolygonStyle(nameAttr),
                            VectorStyles.GetLabelStyle(nameAttr)
                        }
                            };
                            foreach (var attributeName in f.Attributes.GetNames())
                            {
                                feature[attributeName] = f.Attributes[attributeName];
                            }
                            polygonFeatures.Add(feature);
                        }
                    }
                    // Точки
                    else if (geom is NetTopologySuite.Geometries.Point point)
                    {
                        var p = Mapsui.Projections.SphericalMercator.FromLonLat(point.X, point.Y);
                        var projectedPoint = new NetTopologySuite.Geometries.Point(p.x, p.y);

                        string label = f.Attributes.Exists("name") ? f.Attributes["name"]?.ToString() : null;

                        var feature = new Mapsui.Nts.GeometryFeature
                        {
                            Geometry = projectedPoint,
                            Styles = VectorStyles.GetPointStylesWithLabel(label)
                        };
                        foreach (var attributeName in f.Attributes.GetNames())
                        {
                            feature[attributeName] = f.Attributes[attributeName];
                        }
                        pointFeatures.Add(feature);
                    }
                }

                if (polygonFeatures.Count > 0)
                {
                    map.Layers.Add(new Mapsui.Layers.MemoryLayer
                    {
                        Name = "Polygons",
                        Features = polygonFeatures,
                        Style = null,
                        Enabled = villageCouncilsVisible
                    });
                }

                if (pointFeatures.Count > 0)
                {
                    map.Layers.Add(new Mapsui.Layers.MemoryLayer
                    {
                        Name = "Points",
                        Features = pointFeatures,
                        Style = null,
                        Enabled = fireStationsVisible
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки GeoJSON: {ex.Message}");
            }
        }


        private void SetInitialView(Map map)
        {
            double lon = 82.92043;
            double lat = 55.03020;

            var centerPoint = SphericalMercator.FromLonLat(lon, lat);
            map.Navigator.CenterOn(centerPoint.x, centerPoint.y);
            map.Navigator.ZoomTo(200);
        }

        #region Вспомогательные методы

        /*private Color GetColorByName(string name)
        {
            var hash = name.GetHashCode();
            byte a = 100;
            byte r = (byte)((hash & 0xFF0000) >> 16);
            byte g = (byte)((hash & 0x00FF00) >> 8);
            byte b = (byte)(hash & 0x0000FF);
            return Color.FromArgb(a, r, g, b);
        }*/

        private List<Polygon> ConvertGeometry(Geometry geom)
        {
            var result = new List<Polygon>();

            if (geom is Polygon poly)
            {
                result.Add(poly);
            }
            else if (geom is MultiPolygon multi)
            {
                result.AddRange(multi.Geometries.Cast<Polygon>());
            }

            return result;
        }

        private Geometry ProjectGeometry(Geometry geometry)
        {
            var coordinates = geometry.Coordinates;
            var projectedCoordinates = new Coordinate[coordinates.Length];

            for (int i = 0; i < coordinates.Length; i++)
            {
                var projected = SphericalMercator.FromLonLat(coordinates[i].X, coordinates[i].Y);
                projectedCoordinates[i] = new Coordinate(projected.x, projected.y);
            }

            if (geometry is Polygon)
                return geometry.Factory.CreatePolygon(projectedCoordinates);
            else
                return geometry.Factory.CreateLineString(projectedCoordinates);
        }

        #endregion
    }
}
