using FFSchedule.Class;
using FFSchedule.Container;
using FFSchedule.DepartamentWindows;
using FFSchedule.DepartamentWindows.JsonModels;
using FFSchedule.Models;
using FFSchedule.Page;
using FFSchedule.Presentation;
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

        private bool _polygonFillEnabled = true;
        private double _polygonBorderWidth = 0.5;

        public double searchLat;
        public double searchLon;
    
        public readonly FfsContext _dbcontext;

        public RouteService _routeService;
        public readonly SearchService _searchService;
        public MeasureService _measureService;
        private MeasureVisualizer _measureVisualizer;
        public readonly MapDataService _mapDataService;
        private readonly MapVisualizer _mapVisualizer;

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
            _mapVisualizer = new MapVisualizer(MapControl);

            _measureService = new MeasureService();
            _measureVisualizer = new MeasureVisualizer(MapControl);

            InitializeMap();

            searchCache = new JsonFileSearchCache();
            routeCache = new JsonFileRouteCache();

            _routeService = new RouteService(App.HttpClient, map, MapControl, fireStations.ToList(), routeCache);     
            _searchService = new SearchService(App.HttpClient, MapControl, searchCache);

            SideFrame.Navigate(new SearchPage(this));
        }
        public void InitializeMap()
        {
            map = new Map();
            MapControl.Map = map;
            map.Layers.Add(CreateOsmLayerWithCache());

            _mapVisualizer.InitializeLayers(map);
            _measureVisualizer.InitializeLayers(map);

            LoadMapData();
            SetInitialView(map);

            MapControl.PreviewMouseLeftButtonDown += MapControl_PreviewMouseLeftButtonDown;
            MapControl.MouseLeftButtonUp += MapControl_MouseLeftButtonUp;
            MapControl.MouseMove += MapControl_MouseMove;
            MapControl.MouseLeftButtonDown += MapControl_GlobalDoubleClickHandler;
            MapControl.MouseRightButtonDown += MapControl_MouseRightButtonDown;

            map.Widgets.Add(new ScaleBarWidget(map));
            map.Widgets.Add(new ZoomInOutWidget());
            map.Widgets.Add(new MouseCoordinatesWidget());
        }

        private void LoadMapData()
        {
            fireStations.Clear();

            var districtsData = _mapDataService.ParseGeoJson(System.IO.Path.Combine("MapVector", "nskDISTandKSTV.geojson"));
            var stationsData = _mapDataService.ParseGeoJson(System.IO.Path.Combine("MapVector", "FireStationPoints.geojson"));

            var combinedResult = new GeoJsonLoadResult
            {
                Polygons = districtsData.Polygons,
                FireStations = stationsData.FireStations
            };

            foreach (var station in combinedResult.FireStations)
            {
                fireStations.Add(station);
            }

            _mapVisualizer.RenderGeoJsonData(combinedResult, villageCouncilsVisible, fireStationsVisible);
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

                    _mapVisualizer.ClearAllGraphics();
                    MapControl.Map.Layers.Clear();

                    MapControl.Map.Layers.Add(CreateOsmLayerWithCache());
                    _mapVisualizer.InitializeLayers(MapControl.Map);

                    _mapVisualizer.InitializeLayers(map);
                    _measureVisualizer.InitializeLayers(map);

                    LoadMapData();

                    _routeService = new RouteService(App.HttpClient, map, MapControl, fireStations.ToList(), routeCache);
                    SetInitialView(MapControl.Map);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении карты: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
            _mapVisualizer.ClearHover();
            MapControl.Cursor = Cursors.Arrow;
        }

        private void MapControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_mapMode == MapMode.Delete)
            {
                HandleDeleteDepartment(e);
                return;
            }

            if (_mapMode == MapMode.Add)
            {
                HandleAddDepartment(e);
                return;
            }

            if (_isDraggingMap) return;

            var screenPosition = e.GetPosition(MapControl);
            var worldPosition = MapControl.Map.Navigator.Viewport.ScreenToWorld(screenPosition.X, screenPosition.Y);

            if (_measureService.CurrentMode != MeasureMode.None)
            {
                var lonLat = Mapsui.Projections.SphericalMercator.ToLonLat(worldPosition.X, worldPosition.Y);
                _measureService.HandleClick(new NetTopologySuite.Geometries.Coordinate(lonLat.lon, lonLat.lat));

                string resultText = _measureService.GetFormattedResult();
                _measureVisualizer.Render(_measureService.Points, _measureService.MouseMovePoint, _measureService.CurrentMode, resultText);
                return;
            }

            if (_mapMode != MapMode.Default || !fireStationsVisible) return;

            var clickedFeature = _mapVisualizer.FindFeatureAtPosition(worldPosition);
            if (clickedFeature == null) return;

            var stationName = clickedFeature["name"]?.ToString();
            if (string.IsNullOrWhiteSpace(stationName)) return;

            SideFrame.Navigate(new FireStationsPage(this));
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (SideFrame.Content is FireStationsPage page) page.SelectFireStation(stationName);
            }));
        }

        private void MapControl_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_measureService.CurrentMode != MeasureMode.None)
            {
                _measureService.StopMeasurement();
                _measureVisualizer.ClearGraphics();
                e.Handled = true;
            }
        }
        private void MapControl_MouseMove(object sender, MouseEventArgs e)
        {
            var currentPosition = e.GetPosition(MapControl);
            var worldPosition = MapControl.Map.Navigator.Viewport.ScreenToWorld(currentPosition.X, currentPosition.Y);

            if (_measureService != null && _measureService.CurrentMode != MeasureMode.None)
            {
                var lonLat = Mapsui.Projections.SphericalMercator.ToLonLat(worldPosition.X, worldPosition.Y);
                _measureService.HandleMouseMove(new NetTopologySuite.Geometries.Coordinate(lonLat.lon, lonLat.lat));

                string resultText = _measureService.GetFormattedResult();
                _measureVisualizer.Render(_measureService.Points, _measureService.MouseMovePoint, _measureService.CurrentMode, resultText);
            }

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (Math.Abs(currentPosition.X - _mouseDownPosition.X) > 4 || Math.Abs(currentPosition.Y - _mouseDownPosition.Y) > 4)
                {
                    _isDraggingMap = true;
                }
            }

            _lastMousePosition = currentPosition;

            if (!fireStationsVisible)
            {
                _mapVisualizer.ClearHover();
                MapControl.Cursor = Cursors.Arrow;
                return;
            }

            var feature = _mapVisualizer.FindFeatureAtPosition(worldPosition);

            if (feature != null)
            {
                _mapVisualizer.HandleHover(worldPosition);
                MapControl.Cursor = Cursors.Hand;
            }
            else
            {
                _mapVisualizer.ClearHover();
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
            _mapVisualizer.SetLayerVisibility("Polygons", villageCouncilsVisible);
        }

        private void ToggleFireStationsVisibility()
        {
            fireStationsVisible = !fireStationsVisible;
            _mapVisualizer.SetLayerVisibility("Points", fireStationsVisible);
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
            _measureVisualizer.ClearGraphics();
        }

        //загрузка и отрисовка векторного слоя
        private void UpdatePolygonStyles() => _mapVisualizer.SetPolygonStyles(_polygonFillEnabled, _polygonBorderWidth);

        private void HandleAddDepartment(MouseButtonEventArgs e)
        {
            var screenPos = e.GetPosition(MapControl);
            var worldPos = MapControl.Map.Navigator.Viewport.ScreenToWorld(screenPos.X, screenPos.Y);
            var lonLat = SphericalMercator.ToLonLat(worldPos.X, worldPos.Y);

            var result = MessageBox.Show($"Создать ПЧ здесь?\n\nШирота: {lonLat.lat:F6}\nДолгота: {lonLat.lon:F6}", "Подтверждение", MessageBoxButton.YesNo);
            if (result != MessageBoxResult.Yes) return;

            var wnd = new AddDepartmentWindow(_dbcontext, lonLat.lat, lonLat.lon) { Owner = this };
            if (wnd.ShowDialog() == true)
            {
                _routeService?.ClearCache();
                RefreshMap_Click(null, null);
            }
        }

        private void HandleDeleteDepartment(MouseButtonEventArgs e)
        {
            var screenPos = e.GetPosition(MapControl);
            var worldPos = MapControl.Map.Navigator.Viewport.ScreenToWorld(screenPos.X, screenPos.Y);

            var feature = _mapVisualizer.FindFeatureAtPosition(worldPos);
            if (feature == null) return;

            var name = feature["name"]?.ToString();
            if (string.IsNullOrWhiteSpace(name)) return;

            var result = MessageBox.Show($"Удалить пожарную часть:\n{name}?", "Подтверждение", MessageBoxButton.YesNo);
            if (result != MessageBoxResult.Yes) return;

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
    }
}
