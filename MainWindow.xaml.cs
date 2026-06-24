using DocumentFormat.OpenXml.Drawing;
using FFSchedule.Class;
using FFSchedule.Container;
using FFSchedule.Controls;
using FFSchedule.DepartamentWindows;
using FFSchedule.DepartamentWindows.JsonModels;
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

        private bool fireStationsVisible = true;
        private bool villageCouncilsVisible = true;

        public ObservableCollection<FireStation> fireStations = new ObservableCollection<FireStation>();

        private readonly MemoryLayer _hoverLayer = new MemoryLayer { Name = "HoverLayer", Enabled = false };

        private Dictionary<GeometryFeature, List<IStyle>> _originalStyles = new Dictionary<GeometryFeature, List<IStyle>>();

        private Mapsui.Layers.MemoryLayer? _polygonLayer;

        private bool _polygonFillEnabled = true;
        private double _polygonBorderWidth = 0.5;

        private Dictionary<Mapsui.IFeature, Brush> _originalFills = new Dictionary<Mapsui.IFeature, Brush>();   

        public double searchLat;
        public double searchLon;
    
        public readonly FfsContext _dbcontext;

        public RouteService _routeService;
        public readonly SearchService _searchService;
        public MeasureService _measureService;
        public readonly MapDataService _mapDataService; 

        private System.Windows.Point _mouseDownPosition;
        private bool _isDraggingMap;

        private System.Windows.Point _lastMousePosition;

        public IRouteCache routeCache;
        public ISearchCache searchCache;

        private enum MapMode
        {
            None,
            Add,
            Delete,
            Measure,
            Default
        }

        private MapMode _mapMode = MapMode.Default;

        public MainWindow()
        {
            InitializeComponent();

            _dbcontext = new FfsContext();

            _mapDataService = new MapDataService();

            InitializeMap();

            searchCache = new JsonFileSearchCache();
            routeCache = new JsonFileRouteCache();

            _routeService = new RouteService(App.HttpClient, map, MapControl, fireStations.ToList(), routeCache);     
            _searchService = new SearchService(App.HttpClient, MapControl, searchCache);
            _measureService = new MeasureService(MapControl);                

            SideFrame.Navigate(new SearchPage(this));
        }
        public void InitializeMap()
        {
            map = new Map();
            //Тайловая подложка
            map.Layers.Add(CreateOsmLayerWithCache());
            //Районы
            LoadGeoJsonLayer(map, @"MapVector\nskDISTandKSTV.geojson");

            //Точки пч/псч
            LoadGeoJsonLayer(map, @"MapVector\FireStationPoints.geojson");

            map.Layers.Add(_hoverLayer);

            //Начальный вид
            SetInitialView(map);

            //Контролер lib
            MapControl.Map = map;

            MapControl.PreviewMouseLeftButtonDown += MapControl_PreviewMouseLeftButtonDown;
            MapControl.MouseLeftButtonUp += MapControl_MouseLeftButtonUp;
            MapControl.MouseMove += MapControl_MouseMove;
            MapControl.MouseLeftButtonDown += MapControl_GlobalDoubleClickHandler;
            MapControl.MouseRightButtonDown += MapControl_MouseRightButtonDown;

            MapControl.Map.Widgets.Add(new ScaleBarWidget(map));
            MapControl.Map.Widgets.Add(new ZoomInOutWidget());
            MapControl.Map.Widgets.Add(new MouseCoordinatesWidget());      
        }

        public void StartAddDepartmentMode()
        {
            _mapMode = MapMode.Add;
        }

        private bool _skipNextClick = false;
        public void StartDeleteDepartmentMode()
        {
            _mapMode = MapMode.Delete;
            _skipNextClick = false;
        }

        //Menu
        private void RefreshMap_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MapControl.Map?.Layers != null)
                {
                    _measureService?.Clear();
                    _routeService?.ClearRoute();
                    _searchService?.RemoveSearchPin();

                    _hoverLayer.Enabled = false;
                    _hoverLayer.Features = Enumerable.Empty<IFeature>();

                    _polygonLayer = null;
                    _originalFills.Clear();
                    _originalStyles.Clear();

                    fireStations.Clear();

                    MapControl.Map.Layers.Clear();
                    MapControl.Map.Layers.Add(_hoverLayer);
                    MapControl.Map.Layers.Add(CreateOsmLayerWithCache());

                    //Заново собираем карту
                    MapControl.Map.Layers.Add(CreateOsmLayerWithCache());
                    LoadGeoJsonLayer(MapControl.Map, @"MapVector\nskDISTandKSTV.geojson");

                    if (fireStationsVisible)
                    {
                        LoadGeoJsonLayer(MapControl.Map, @"MapVector\FireStationPoints.geojson");
                    }

                    //Пересоздаем сервис с новым списком станций
                    _routeService = new RouteService(App.HttpClient, map, MapControl, fireStations.ToList(), routeCache);

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

        private void ClearCache_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string cacheFolder = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "FFSchedule", "TileCache"
                );

                if (System.IO.Directory.Exists(cacheFolder))
                {
                    _routeService?.ClearCache();
                    _searchService?.ClearCache();
                    System.IO.Directory.Delete(cacheFolder, true);
                    System.IO.Directory.CreateDirectory(cacheFolder);
                    MessageBox.Show("Кэш изображений карты, маршрутов и поиска очищен. Изменения вступят со следующим запуском.",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось очистить кэш: {ex.Message}", "Ошибка");
            }
        }

        private void ExportMapImage_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PNG Image|*.png",
                FileName = $"Map_Export_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    byte[] snapshot = MapControl.GetSnapshot();
                    if (snapshot != null && snapshot.Length > 0)
                    {
                        System.IO.File.WriteAllBytes(saveFileDialog.FileName, snapshot);
                        MessageBox.Show("Изображение карты успешно сохранено!", "Экспорт",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Не удалось получить снимок карты (карта пуста или еще не отрисовалась).",
                            "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка");
                }
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            FFSchedule.Properties.Settings.Default.IsLoggedIn = false;
            FFSchedule.Properties.Settings.Default.Save();

            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();

            this.Close();
        }

        //Карта
        private void MapControl_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _mouseDownPosition = e.GetPosition(MapControl);
            _isDraggingMap = false;

            if (_hoverLayer.Enabled)
            {
                _hoverLayer.Enabled = false;
                _hoverLayer.Features = Enumerable.Empty<IFeature>();
                MapControl.Cursor = Cursors.Arrow;
                MapControl.Refresh();
            }
        }

        private void MapControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"MouseUp: _mapMode={_mapMode}, _skipNextClick={_skipNextClick}, _isDraggingMap={_isDraggingMap}");

            if (_mapMode == MapMode.Delete)
            {
                if (_skipNextClick)
                {
                    _skipNextClick = false;
                    System.Diagnostics.Debug.WriteLine("Skipped!");
                    return;
                }
                HandleDeleteDepartment(e);
                return;
            }

            if (_mapMode == MapMode.Add)
            {
                HandleAddDepartment(e);
                return;
            }

            if (_mapMode == MapMode.Delete)
            {
                HandleDeleteDepartment(e);
                return;
            }

            if (_isDraggingMap)
                return;

            if (_measureService.CurrentMode != MeasureMode.None)
            {
                var screenPosition = e.GetPosition(MapControl);
                var worldPosition = MapControl.Map.Navigator.Viewport.ScreenToWorld(
                    screenPosition.X, screenPosition.Y);

                _measureService.HandleClick(worldPosition);
                return;
            }

            if (_mapMode != MapMode.Default)
                return;

            if (!fireStationsVisible)
                return;

            var screenPos = e.GetPosition(MapControl);
            var worldPos = MapControl.Map.Navigator.Viewport.ScreenToWorld(screenPos.X, screenPos.Y);

            var layer = MapControl.Map.Layers.FirstOrDefault(l => l.Name == "Points") as MemoryLayer;
            if (layer == null)
                return;

            double screenRadius = 15;
            double worldRadius = screenRadius * MapControl.Map.Navigator.Viewport.Resolution;

            var searchRect = new MRect(
                worldPos.X - worldRadius,
                worldPos.Y - worldRadius,
                worldPos.X + worldRadius,
                worldPos.Y + worldRadius);

            var closest = layer.GetFeatures(searchRect, 0).FirstOrDefault();
            if (closest == null)
                return;

            var stationName = closest["name"]?.ToString();
            if (string.IsNullOrWhiteSpace(stationName))
                return;

            SideFrame.Navigate(new FireStationsPage(this));

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (SideFrame.Content is FireStationsPage page)
                {
                    page.SelectFireStation(stationName);
                }
            }));
        }

        private void MapControl_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Если в данный момент мы что-то измеряем — завершаем процесс по клику ПКМ
            if (_measureService.CurrentMode != MeasureMode.None)
            {
                _measureService.StopMeasurement();
                e.Handled = true;
            }
        }
        private void MapControl_MouseMove(object sender, MouseEventArgs e)
        {
            var currentPosition = e.GetPosition(MapControl);

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (Math.Abs(currentPosition.X - _mouseDownPosition.X) > 4 ||
                    Math.Abs(currentPosition.Y - _mouseDownPosition.Y) > 4)
                {
                    _isDraggingMap = true;
                }
            }

            if (e.LeftButton == MouseButtonState.Pressed && _isDraggingMap)
            {
                return; 
            }

            if (Math.Abs(currentPosition.X - _lastMousePosition.X) < 3 &&
                Math.Abs(currentPosition.Y - _lastMousePosition.Y) < 3)
                return;

            _lastMousePosition = currentPosition;

            if (!fireStationsVisible)
            {
                if (_hoverLayer.Enabled)
                {
                    _hoverLayer.Enabled = false;
                    _hoverLayer.Features = Enumerable.Empty<IFeature>();
                    MapControl.Refresh();
                }
                MapControl.Cursor = Cursors.Arrow;
                return;
            }

            if (MapControl.Map?.Layers == null) return;

            var screenPosition = e.GetPosition(MapControl);
            var worldPosition = MapControl.Map.Navigator.Viewport.ScreenToWorld(screenPosition.X, screenPosition.Y);

            var pointsLayer = MapControl.Map.Layers.FirstOrDefault(l => l.Name == "Points") as MemoryLayer;
            if (pointsLayer == null) return;

            double screenRadius = 15;
            double worldRadius = screenRadius * MapControl.Map.Navigator.Viewport.Resolution;

            var searchRect = new MRect(
                worldPosition.X - worldRadius,
                worldPosition.Y - worldRadius,
                worldPosition.X + worldRadius,
                worldPosition.Y + worldRadius
            );

            var features = pointsLayer.GetFeatures(searchRect, 0);

            var geometryFeature = features.FirstOrDefault() as GeometryFeature;

            if (geometryFeature != null)
            {
                if (!_originalStyles.TryGetValue(geometryFeature, out var originalStyles))
                    return;

                if (_hoverLayer.Enabled && _hoverLayer.Features.FirstOrDefault() == geometryFeature)
                {
                    MapControl.Cursor = Cursors.Hand;
                    return;
                }

                var highlightedStyle = new List<IStyle>();

                foreach (var style in originalStyles)
                {
                    if (style is SymbolStyle symbolStyle)
                    {
                        var originalFill = symbolStyle?.Fill?.Color;

                        int r = Math.Min(255, (int)(originalFill.Value.R * 2));
                        int g = Math.Min(255, (int)(originalFill.Value.G * 2));
                        int b = Math.Min(255, (int)(originalFill.Value.B * 2));
                        var highlightedFill = new Color(r, g, b, 2);

                        highlightedStyle.Add(new SymbolStyle
                        {
                            SymbolType = symbolStyle.SymbolType,
                            Fill = new Brush(highlightedFill),
                            Outline = symbolStyle.Outline,
                            SymbolScale = symbolStyle.SymbolScale * 1.15f,
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
                var styleCollection = new StyleCollection();
                foreach (var s in highlightedStyle)
                {
                    styleCollection.Styles.Add(s);
                }
                _hoverLayer.Style = styleCollection;
                _hoverLayer.Enabled = true;

                MapControl.Cursor = Cursors.Hand;
                MapControl.Refresh();
            }
            else
            {
                if (_hoverLayer.Enabled)
                {
                    _hoverLayer.Enabled = false;
                    _hoverLayer.Features = Enumerable.Empty<IFeature>();
                    MapControl.Cursor = Cursors.Arrow;
                    MapControl.Refresh();
                }
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
                _searchService?.AddToHistory(result);
                GlobalSearchBox.FillAndSelect(result);
            }
        }
        private void OnGlobalResultSelected(object sender, NominatimResult res)
        {
            this.searchLat = res.Lat;
            this.searchLon = res.Lon;
            this._searchService?.AddToHistory(res);
            this._searchService?.FlyToResult(res);
        }
        private void ClearSearchAndRoute_Click(object sender, RoutedEventArgs e)
        {
            GlobalSearchBox.ResetView(); ;

            _searchService.RemoveSearchPin();

            _routeService.ClearRoute();

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

        private void MeasureDistance_Click(object sender, RoutedEventArgs e)
        {
            _measureService.StartMeasurement(MeasureMode.Distance);
        }

        private void MeasureArea_Click(object sender, RoutedEventArgs e)
        {
            _measureService.StartMeasurement(MeasureMode.Area);
        }

        private void ClearMeasure_Click(object sender, RoutedEventArgs e)
        {
            _measureService.Clear();
        }

        //загрузка и отрисовка векторного слоя
        private void LoadGeoJsonLayer(Map map, string geojsonPath)
        {
            if (map == null || string.IsNullOrEmpty(geojsonPath)) return;
            try
            {
                var loadedData = _mapDataService.ParseGeoJson(geojsonPath);

                if(loadedData.PolygonFeatures.Count > 0)
                {
                    foreach (var feature in loadedData.PolygonFeatures)
                    {
                        var vs = feature.Styles.OfType<VectorStyle>().FirstOrDefault();
                        if (vs != null) _originalFills[feature] = vs.Fill;
                    }

                    _polygonLayer = new MemoryLayer
                    {
                        Name = "Polygons",
                        Features = loadedData.PolygonFeatures,
                        Style = null,
                        Enabled = villageCouncilsVisible
                    };
                    map.Layers.Add(_polygonLayer);
                }

                if(loadedData.PointFeatures.Count > 0)
                {
                    foreach(var feature in loadedData.PointFeatures)
                    {
                        _originalStyles[feature] = new List<IStyle>(feature.Styles);
                    }
                    foreach(var station in loadedData.FireStations)
                    {
                        fireStations.Add(station);
                    }
                    map.Layers.Add(new MemoryLayer
                    {
                        Name = "Points",
                        Features = loadedData.PointFeatures,
                        Style = null,
                        Enabled = fireStationsVisible
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки векторного слоя: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void HandleAddDepartment(MouseButtonEventArgs e)
        {
            var screenPos = e.GetPosition(MapControl);

            var worldPos = MapControl.Map.Navigator.Viewport.ScreenToWorld(screenPos.X, screenPos.Y);

            var lonLat = SphericalMercator.ToLonLat(worldPos.X, worldPos.Y);

            var result = MessageBox.Show(
                $"Создать ПЧ здесь?\n\n" +
                $"Широта: {lonLat.lat:F6}\n" +
                $"Долгота: {lonLat.lon:F6}",
                "Подтверждение",
                MessageBoxButton.YesNo);

            if (result != MessageBoxResult.Yes)
                return;

            var wnd = new AddDepartmentWindow(_dbcontext, lonLat.lat, lonLat.lon);

            wnd.Owner = this;

            if (wnd.ShowDialog() == true)
            {
                _routeService?.ClearCache();
                RefreshMap_Click(null, null);
            }

            wnd.ShowDialog();
        }

        private void HandleDeleteDepartment(MouseButtonEventArgs e)
        {
            if (_skipNextClick)
            {
                _skipNextClick = false;
                return;
            }

            var screenPos = e.GetPosition(MapControl);
            var worldPos = MapControl.Map.Navigator.Viewport.ScreenToWorld(screenPos.X, screenPos.Y);

            var layer = MapControl.Map.Layers.FirstOrDefault(l => l.Name == "Points") as MemoryLayer;
            if (layer == null) return;

            double screenRadius = 15;
            double worldRadius = screenRadius * MapControl.Map.Navigator.Viewport.Resolution;

            var searchRect = new MRect(
                worldPos.X - worldRadius,
                worldPos.Y - worldRadius,
                worldPos.X + worldRadius,
                worldPos.Y + worldRadius);

            var feature = layer.GetFeatures(searchRect, 0).FirstOrDefault() as GeometryFeature;
            if (feature == null) return;

            var name = feature["name"]?.ToString();
            if (string.IsNullOrWhiteSpace(name)) return;

            var result = MessageBox.Show(
                $"Удалить пожарную часть:\n{name}?",
                "Подтверждение",
                MessageBoxButton.YesNo);

            if (result != MessageBoxResult.Yes)
                return;

            DeleteFromDatabase(name);
            DeleteFromGeoJson(name);

            _routeService?.ClearCache();

            MessageBox.Show("Удалено");

            RefreshMap_Click(null, null);
        }

        private void DeleteFromDatabase(string name)
        {
            var dept = _dbcontext.Departments.FirstOrDefault(x => x.DptName == name);
            if (dept == null) return;

            _dbcontext.Departments.Remove(dept);
            _dbcontext.SaveChanges();
        }

        private void DeleteFromGeoJson(string name)
        {
            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "MapVector", "FireStationPoints.geojson");

            var json = File.ReadAllText(path);

            var geo = JsonSerializer.Deserialize<GeoJson>(json);

            if (geo?.features == null) return;

            geo.features = geo.features
                .Where(f => f.properties["name"]?.ToString() != name)
                .ToList();

            File.WriteAllText(path, JsonSerializer.Serialize(geo, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
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

        private Mapsui.Layers.ILayer CreateOsmLayerWithCache()
        {
            string cacheFolder = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FFSchedule",
            "TileCache"
            );

            if (!Directory.Exists(cacheFolder))
            {
                Directory.CreateDirectory(cacheFolder);
            }

            var fileCache = new BruTile.Cache.FileCache(cacheFolder, "png");

            var osmSource = BruTile.Predefined.KnownTileSources.Create(
                BruTile.Predefined.KnownTileSource.OpenStreetMap,
                persistentCache: fileCache
            );

            return new Mapsui.Tiling.Layers.TileLayer(osmSource)
            {
                Name = "OpenStreetMap"
            };
        }

        #region Вспомогательные методы

        private List<Polygon> ConvertGeometry(NetTopologySuite.Geometries.Geometry geom)
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
