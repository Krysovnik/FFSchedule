using DocumentFormat.OpenXml.Drawing;
using FFSchedule.Class;
using FFSchedule.Controls;
using FFSchedule.Models;
using FFSchedule.Page;
using FFSchedule.Services;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.Tiling.Layers;
using Mapsui.UI;
using Mapsui.UI.Wpf;
using Mapsui.Widgets.ButtonWidgets;
using Mapsui.Widgets.InfoWidgets;
using Mapsui.Widgets.ScaleBar;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FFSchedule
{
    public partial class MainWindow : Window
    {
        private Map map;

        private readonly HttpClient httpClient = new HttpClient();

        private bool fireStationsVisible = true;
        private bool villageCouncilsVisible = true;

        public ObservableCollection<FireStation> fireStations = new ObservableCollection<FireStation>();

        private MemoryLayer? _hoverLayer;

        private Dictionary<GeometryFeature, List<IStyle>> _originalStyles = new Dictionary<GeometryFeature, List<IStyle>>();

        private Mapsui.Layers.MemoryLayer _polygonLayer;

        private bool _polygonFillEnabled = true;
        private double _polygonBorderWidth = 0.5;

        private Dictionary<Mapsui.IFeature, Brush> _originalFills = new Dictionary<Mapsui.IFeature, Brush>();

        public readonly SearchService _searchService;

        public double searchLat;
        public double searchLon;

        public RouteService routeService;

        public readonly FfsContext _dbcontext;


        public MainWindow()
        {
            InitializeComponent();

            _dbcontext = new FfsContext();

            httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(70),
                DefaultRequestHeaders = { { "User-Agent", "FFSchedule/1.0 (popovis@mer.ci.nsu.ru)" } }
            };

            InitializeMap();

            routeService = new RouteService(httpClient, map, MapControl, fireStations.ToList());

            _searchService = new SearchService(httpClient, MapControl);

            SideFrame.Navigate(new SearchPage(this));
        }
        public void InitializeMap()
        {
            map = new Map();
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
            MapControl.MouseMove += MapControl_MouseMove;
            MapControl.MouseLeftButtonDown += MapControl_GlobalDoubleClickHandler;
            MapControl.Map.Widgets.Add(new ScaleBarWidget(map));
            MapControl.Map.Widgets.Add(new ZoomInOutWidget());
            MapControl.Map.Widgets.Add(new MouseCoordinatesWidget());      
        } 
        //Menu
        private void RefreshMap_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MapControl.Map?.Layers != null)
                {
                    fireStations.Clear();
                    MapControl.Map.Layers.Clear();
                    MapControl.Map.Layers.Add(OpenStreetMap.CreateTileLayer());
                    LoadGeoJsonLayer(MapControl.Map, @"MapVector\nskDISTandKSTV.geojson");
                    if (fireStationsVisible)
                    {
                        LoadGeoJsonLayer(MapControl.Map, @"MapVector\FireStationPoints.geojson");
                    }
                    routeService = new RouteService(httpClient, map, MapControl, fireStations.ToList());
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
        //Карта
        private void MapControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!fireStationsVisible) return;

            var screenPosition = e.GetPosition(MapControl);
            var worldPosition = MapControl.Map.Navigator.Viewport.ScreenToWorld(
                screenPosition.X, screenPosition.Y);

            var layer = MapControl.Map.Layers.FirstOrDefault(l => l.Name == "Points");
            if (layer is MemoryLayer memoryLayer)
            {
                var features = memoryLayer.GetFeatures(
                    new MRect(worldPosition.X, worldPosition.Y, worldPosition.X, worldPosition.Y),
                    MapControl.Map.Navigator.Viewport.Resolution);

                var closest = features.FirstOrDefault();
                if (closest != null)
                {
                    // Извлекаем имя ПЧ
                    var stationName = closest["name"]?.ToString();

                    if (!string.IsNullOrWhiteSpace(stationName))
                    {
                        // ✅ Переключаемся на страницу с ПЧ
                        SideFrame.Navigate(new FireStationsPage(this));

                        // ✅ Ждём, пока страница загрузится, и вызываем выбор
                        // Используем Dispatcher для отложенного вызова
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            if (SideFrame.Content is FireStationsPage fireStationsPage)
                            {
                                fireStationsPage.SelectFireStation(stationName);
                            }
                        }), System.Windows.Threading.DispatcherPriority.Background);
                    }
                }
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
        private async void MapControl_GlobalDoubleClickHandler(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount != 2) return;

            var screenPos = e.GetPosition(MapControl);
            var worldPos = MapControl.Map.Navigator.Viewport.ScreenToWorld(screenPos.X, screenPos.Y);
            var lonLat = Mapsui.Projections.SphericalMercator.ToLonLat(worldPos.X, worldPos.Y);

            if (!(SideFrame.Content is RoutePage) && !(SideFrame.Content is SearchPage))
            {
                SideFrame.Navigate(new SearchPage(this));
            }

            // Обратный геокодинг
            var result = await _searchService.ReverseSearchAsync(lonLat.lat, lonLat.lon);
            if (result != null)
            {
                GlobalSearchBox.FillAndSelect(result);
            }
        }
        private void OnGlobalResultSelected(object sender, NominatimResult res)
        {
            this.searchLat = res.Lat;
            this.searchLon = res.Lon;
            this._searchService.FlyToResult(res);
        }
        private void ClearSearchAndRoute_Click(object sender, RoutedEventArgs e)
        {
            GlobalSearchBox.ResetView(); ;

            _searchService.RemoveSearchPin();

            routeService.ClearRoute();

            searchLat = 0;
            searchLon = 0;

            MapControl.Refresh();
            if (SideFrame.Content is RoutePage routePage)
            {
                var stationsList = routePage.FindName("StationsListBox") as ListBox;
                if (stationsList != null)
                {
                    stationsList.Visibility = Visibility.Collapsed;
                }
                var addButton = routePage.FindName("AddRouteButton") as Button;
                if (addButton != null)
                {
                    addButton.Visibility = Visibility.Collapsed;
                }
                routePage.ClearView();
            }
        }
        //Кнопки  
        private void NavigateToSearch(object sender, RoutedEventArgs e)
        {
            SideFrame.Navigate(new SearchPage(this));
        }

        private void NavigateToFireStations(object sender, RoutedEventArgs e)
        {
            SideFrame.Navigate(new FireStationsPage(this));
        }

        private void NavigateToRoute(object sender, RoutedEventArgs e)
        {
            SideFrame.Navigate(new RoutePage(this));
        }
        private void WordToRoute(object sender, RoutedEventArgs e)
        {
            SideFrame.Navigate(new WordPage(this));
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
        #endregion        
    }
}
