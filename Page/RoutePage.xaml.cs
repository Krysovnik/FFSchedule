using FFSchedule.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;

namespace FFSchedule.Page
{
    public partial class RoutePage : System.Windows.Controls.Page
    {
        private readonly MainWindow _mainWindow;

        private readonly ObservableCollection<dynamic> _displayTracks = new ObservableCollection<dynamic>();

        public RoutePage(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            StationsListBox.ItemsSource = _displayTracks;
            Loaded += (s, e) => LoadRanks();
        }
        private async void LoadRanks()
        {
            using (var db = new FfsContext())
            {
                var ranks = await db.Ranks
                       .OrderBy(r => r.RNumber)
                       .ToListAsync();
                RankComboBox.ItemsSource = ranks;

                if (ranks.Any())
                {
                    RankComboBox.SelectedIndex = 1;
                }
            }
        }
        private async void BuildRoute_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_mainWindow.searchLat == 0 && _mainWindow.searchLon == 0)
                {
                    MessageBox.Show("Введите адрес");
                    return;
                }

                var selectedRank = RankComboBox.SelectedItem as Rank;
                if (selectedRank == null) return;

                int neededEquipment = selectedRank.RTotalEquipmentQuantity ?? 0;

                RouteButton.IsEnabled = false;
                AddRouteButton.Visibility = Visibility.Collapsed;
                _mainWindow.LoadingIndicator.Visibility = Visibility.Visible;

                _displayTracks.Clear();
                _mainWindow.ClearRoute();

                var results = await _mainWindow._routeService.BuildRoutesByRequirementAsync(
                    _mainWindow.searchLat,
                    _mainWindow.searchLon,
                    neededEquipment);

                if (results.Any(r => r.Success))
                {
                    _mainWindow.RenderRoutesOnMap(results);

                    foreach (var r in results.Where(r => r.Success))
                    {
                        _displayTracks.Add(new
                        {
                            StationName = r.Station != null ? $"Станция {r.Station.Name}" : "Неизвестная станция",
                            DistanceText = $"Путь: {r.Distance / 1000:F1} км",
                            DurationText = $"Время: {r.Duration / 60:F1} мин"
                        });
                    }

                    StationsListBox.Visibility = Visibility.Visible;
                    AddRouteButton.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
            finally
            {
                _mainWindow.LoadingIndicator.Visibility = Visibility.Collapsed;
                RouteButton.IsEnabled = true;
            }
        }

        private async void AddRouteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_mainWindow.searchLat == 0 && _mainWindow.searchLon == 0)
                {
                    MessageBox.Show("Введите адрес или выберите точку на карте");
                    return;
                }

                AddRouteButton.IsEnabled = false;
                _mainWindow.LoadingIndicator.Visibility = Visibility.Visible;

                var additionalRoute = await _mainWindow._routeService.BuildNextAdditionalRouteAsync(
                    _mainWindow.searchLat,
                    _mainWindow.searchLon);

                if (additionalRoute != null)
                {
                    if (additionalRoute.Success)
                    {
                        int globalIndex = _mainWindow._routeService.UsedStationsCount - 1;

                        _mainWindow.RenderAdditionalRouteOnMap(additionalRoute, globalIndex);

                        _displayTracks.Add(new
                        {
                            StationName = additionalRoute.Station != null ? $"Доп. Станция {additionalRoute.Station.Name}" : "Доп. станция",
                            DistanceText = $"Путь: {additionalRoute.Distance / 1000:F1} км",
                            DurationText = $"Время: {additionalRoute.Duration / 60:F1} мин"
                        });
                    }
                    else
                    {
                        MessageBox.Show(additionalRoute.ErrorMessage, "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении маршрута: {ex.Message}");
            }
            finally
            {
                _mainWindow.LoadingIndicator.Visibility = Visibility.Collapsed;
                AddRouteButton.IsEnabled = true;
            }
        }
        public void ClearView()
        {
            _displayTracks.Clear();
            AddRouteButton.Visibility = Visibility.Collapsed;
            StationsListBox.Visibility = Visibility.Collapsed;
        }
    }
}
