using BruTile;
using BruTile.Predefined;
using BruTile.Web;
using FFSchedule.Class;
using FFSchedule.Models;
using FFSchedule.Services;
using Mapsui;
using Mapsui.Animations;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Nts.Extensions;
using Mapsui.Projections;
using Mapsui.Providers;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.Tiling.Layers;
using Mapsui.UI;
using Mapsui.UI.Wpf;
using Mapsui.Utilities;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using OpenTK.Graphics.OpenGL;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.InteropServices.Marshalling;
using System.Text.Json;
using System.Text.Json.Serialization;
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

        private ObservableCollection<FireStation> fireStations = new ObservableCollection<FireStation>();

        private MemoryLayer _hoverLayer;

        private Dictionary<GeometryFeature, List<IStyle>> _originalStyles = new Dictionary<GeometryFeature, List<IStyle>>();

        private Mapsui.Layers.MemoryLayer _polygonLayer;

        private bool _polygonFillEnabled = true;
        private double _polygonBorderWidth = 0.5;

        private Dictionary<Mapsui.IFeature, Brush> _originalFills = new Dictionary<Mapsui.IFeature, Brush>();

        private readonly SearchService _searchService;

        private readonly FfsContext _dbcontext;

        public MainWindow()
        {
            InitializeComponent();
            InitializeMap();

            _dbcontext = new FfsContext();

            FireStationsListBox.ItemsSource = fireStations;

            var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10),
                DefaultRequestHeaders = { { "User-Agent", "FFSchedule/1.0 (popovis@mer.ci.nsu.ru)" } }
            };

            _searchService = new SearchService(httpClient, MapControl);
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

            FireStationInfoPanel.Visibility = Visibility.Collapsed;
            MapControl.MouseLeftButtonDown += MapControl_MouseLeftButtonDown;
            MapControl.MouseMove += MapControl_MouseMove;
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
        private void ButtonAddFireStation_Click(object sender, RoutedEventArgs e)
        {

        }
        private void ButtonRedFireStation_Click(object sender, RoutedEventArgs e)
        {

        }
        private void ButtonDelFireStation_Click(object sender, RoutedEventArgs e)
        {

        }


        //Карта
        private void MapControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!fireStationsVisible) return;
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
                    var name = closest["name"]?.ToString() ?? "Без названия";
                    var address = closest["address"]?.ToString() ?? "Не указан";
                    var district = closest["district"]?.ToString() ?? "Не указан";
                    var type = closest["type"]?.ToString() ?? "Не указан";
                    var phone = closest["phone"]?.ToString() ?? "Не указан";

                    FireStationName.Text = name;
                    FireStationAddress.Text = address;
                    FireStationDistrict.Text = district;
                    FireStationType.Text = type;
                    FireStationPhone.Text = phone;

                    FireStationInfoPanel.Visibility = Visibility.Visible;
                    NoSelectionText.Visibility = Visibility.Collapsed;
                }
                else
                {
                    FireStationInfoPanel.Visibility = Visibility.Collapsed;
                    NoSelectionText.Visibility = Visibility.Visible;
                }
            }
            else
            {
                FireStationInfoPanel.Visibility = Visibility.Collapsed;
                NoSelectionText.Visibility = Visibility.Visible;
            }
        }
        private void MapControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (!fireStationsVisible)
            {
                if (_hoverLayer != null)
                {
                    MapControl.Map.Layers.Remove(_hoverLayer);
                    _hoverLayer = null;
                    MapControl.Refresh();
                }
                MapControl.Cursor = Cursors.Arrow;
                return;
            }
            if (MapControl.Map?.Layers == null) return;

            if (_hoverLayer != null)
            {
                MapControl.Map.Layers.Remove(_hoverLayer);
                _hoverLayer = null;
            }

            var screenPosition = e.GetPosition(MapControl);
            var worldPosition = MapControl.Map.Navigator.Viewport.ScreenToWorld(
                screenPosition.X,
                screenPosition.Y
            );

            var pointsLayer = MapControl.Map.Layers.FirstOrDefault(l => l.Name == "Points") as MemoryLayer;
            if (pointsLayer == null) return;

            var features = pointsLayer.GetFeatures(
                new MRect(worldPosition.X, worldPosition.Y, worldPosition.X, worldPosition.Y),
                MapControl.Map.Navigator.Viewport.Resolution
            );

            var hoveredFeature = features.FirstOrDefault();

            if (hoveredFeature != null && hoveredFeature is GeometryFeature geometryFeature)
            {
                if (!_originalStyles.TryGetValue(geometryFeature, out var originalStyles))
                    return;

                _hoverLayer = new MemoryLayer
                {
                    Name = "HoverLayer",
                    Enabled = true
                };

                var highlightedStyle = new List<IStyle>();

                foreach (var style in originalStyles)
                {
                    if (style is SymbolStyle symbolStyle)
                    {
                        var originalFill = symbolStyle.Fill.Color;
                        var originalOutline = symbolStyle.Outline.Color;

                        int r = Math.Min(255, (int)(originalFill.Value.R * 2));
                        int g = Math.Min(255, (int)(originalFill.Value.G * 2));
                        int b = Math.Min(255, (int)(originalFill.Value.B * 2));
                        var highlightedFill = new Color(r, g, b, 2);

                        highlightedStyle.Add(new SymbolStyle
                        {
                            SymbolType = symbolStyle.SymbolType,
                            Fill = new Brush(highlightedFill),
                            SymbolScale = symbolStyle.SymbolScale,
                            Opacity = symbolStyle.Opacity,
                            MinVisible = symbolStyle.MinVisible,
                            MaxVisible = symbolStyle.MaxVisible
                        });
                    }
                    else if (style is LabelStyle labelStyle)
                    {
                        highlightedStyle.Add(labelStyle);
                    }
                }

                _hoverLayer.Features = new List<GeometryFeature> { geometryFeature };
                _hoverLayer.Style = null;
                MapControl.Map.Layers.Add(_hoverLayer);

                MapControl.Cursor = Cursors.Hand;
            }
            else
            {
                MapControl.Cursor = Cursors.Arrow;
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

                            var vs = feature.Styles.OfType<VectorStyle>().FirstOrDefault();
                            if (vs != null)
                            {
                                _originalFills[feature] = vs.Fill;
                            }

                            foreach (var attributeName in f.Attributes.GetNames())
                            {
                                feature[attributeName] = f.Attributes[attributeName];
                            }
                            polygonFeatures.Add(feature);
                        }
                    }
                    else if (geom is NetTopologySuite.Geometries.Point point)
                    {
                        var p = Mapsui.Projections.SphericalMercator.FromLonLat(point.X, point.Y);
                        var projectedPoint = new NetTopologySuite.Geometries.Point(p.x, p.y);
                        var feature = new Mapsui.Nts.GeometryFeature
                        {
                            Geometry = projectedPoint,
                            Styles = VectorStyles.GetPointStylesWithLabel(
                                f.Attributes.Exists("name") ? f.Attributes["name"]?.ToString() : null,
                                f.Attributes.Exists("type") ? f.Attributes["type"]?.ToString() : null)
                        };

                        foreach (var attributeName in f.Attributes.GetNames())
                        {
                            feature[attributeName] = f.Attributes[attributeName];
                        }
                        _originalStyles[feature] = new List<IStyle>(feature.Styles);
                        var station = new FireStation
                        {
                            Name = f.Attributes.Exists("name") ? f.Attributes["name"]?.ToString() : "Не указано",
                            Address = f.Attributes.Exists("address") ? f.Attributes["address"]?.ToString() : "Не указано",
                            District = f.Attributes.Exists("district") ? f.Attributes["district"]?.ToString() : "Не указано",
                            Type = f.Attributes.Exists("type") ? f.Attributes["type"]?.ToString() : "Не указано",
                            Phone = f.Attributes.Exists("phone") ? f.Attributes["phone"]?.ToString() : "Не указано",    
                            Longitude = point.X,
                            Latitude = point.Y
                        };
                        fireStations.Add(station);
                        pointFeatures.Add(feature);
                    }
                }
                if (polygonFeatures.Count > 0)
                {
                    _polygonLayer = new Mapsui.Layers.MemoryLayer
                    {
                        Name = "Polygons",
                        Features = polygonFeatures,
                        Style = null,
                        Enabled = villageCouncilsVisible
                    };
                    map.Layers.Add(_polygonLayer);
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

        private void UpdatePolygonStyles()
        {
            if (_polygonLayer == null) return;

            foreach (var feature in _polygonLayer.Features)
            {
                if (feature.Styles == null || feature.Styles.Count == 0) continue;

                var vs = feature.Styles.OfType<VectorStyle>().FirstOrDefault();
                if (vs == null) continue;

                vs.Fill = _polygonFillEnabled ? _originalFills[feature] : null;

                if (vs.Outline != null)
                {
                    vs.Outline.Width = (float)_polygonBorderWidth;
                    vs.Outline.PenStyle = PenStyle.Solid;
                }
            }
            MapControl.Refresh();
        }


        private void TogglePolygonFill_Click(object sender, RoutedEventArgs e)
        {
            var menu = sender as MenuItem;
            _polygonFillEnabled = menu.IsChecked;
            UpdatePolygonStyles();
        }

        private void BorderWidth_Thin_Click(object sender, RoutedEventArgs e)
        {
            _polygonBorderWidth = 0.5;
            UpdatePolygonStyles();
        }

        private void BorderWidth_Medium_Click(object sender, RoutedEventArgs e)
        {
            _polygonBorderWidth = 1.5;
            UpdatePolygonStyles();
        }

        private void BorderWidth_Thick_Click(object sender, RoutedEventArgs e)
        {
            _polygonBorderWidth = 2.5;
            UpdatePolygonStyles();
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

        //Поиск
        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            var query = SearchTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(query))
            {
                // Очистка старых маркеров и списка
                var oldPinLayer = MapControl.Map?.Layers.FirstOrDefault(l => l.Name == "SearchPin");
                if (oldPinLayer != null)
                {
                    MapControl.Map.Layers.Remove(oldPinLayer);
                    MapControl.Refresh();
                }
                SearchResultsLb.ItemsSource = null;
                SearchResultsLb.Visibility = Visibility.Collapsed;
                return;
            }

            SearchButton.IsEnabled = false;
            SearchResultsLb.ItemsSource = null;

            try
            {
                var results = await _searchService.SearchAsync(query);
                if (results == null || results.Count == 0)
                {
                    SearchResultsLb.Visibility = Visibility.Collapsed;
                    return;
                }

                SearchResultsLb.ItemsSource = results;
                SearchResultsLb.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка поиска:\n{ex.Message}", "Nominatim",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                SearchButton.IsEnabled = true;
            }
        }

        private void SearchResultsLb_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SearchResultsLb.SelectedItem is not NominatimResult res) return;
            _searchService.FlyToResult(res);
        }
        private void FireStationsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FireStationsListBox.SelectedItem == null)
            {
                HideInfoPanel();
                return;
            }

            var selectedStation = FireStationsListBox.SelectedItem as FireStation;
            if (selectedStation == null)
            {
                HideInfoPanel();
                return;
            }

            FireStationName.Text = selectedStation.Name ?? "Без названия";
            FireStationAddress.Text = selectedStation.Address ?? "Не указан";
            FireStationDistrict.Text = selectedStation.District ?? "Не указано";
            FireStationType.Text = selectedStation.Type ?? "Не указано";
            FireStationPhone.Text = selectedStation.Phone ?? "Не указано";

            FireStationInfoPanel.Visibility = Visibility.Visible;
            NoSelectionText.Visibility = Visibility.Collapsed;

            var projected = Mapsui.Projections.SphericalMercator.FromLonLat(
                selectedStation.Longitude,
                selectedStation.Latitude
            );

            MapControl.Map.Navigator.CenterOn(projected.x, projected.y);
            MapControl.Map.Navigator.ZoomToLevel(12);
        }

        private void HideInfoPanel()
        {
            FireStationInfoPanel.Visibility = Visibility.Collapsed;
            NoSelectionText.Visibility = Visibility.Visible;
        }
        private void GenerateWordTable_Click(object sender, RoutedEventArgs e)
        {
            var settlements = _dbcontext.Settlements.Include(s => s.Vc).Include(s => s.Tol).ToList(); //включая связи

            var exporter = new WordTableExporter();

            string templatePath = @"FFS/template.docx";
            string outputPath = @"FFS/Schedule.docx";

            exporter.ExportSettlementsOnly(settlements, templatePath, outputPath);

            MessageBox.Show("Word документ создан!");
        }
    }
}
