using FFSchedule.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FFSchedule.Page
{
    /// <summary>
    /// Логика взаимодействия для RoutePage.xaml
    /// </summary>
    public partial class RoutePage : System.Windows.Controls.Page
    {
        private readonly MainWindow _mainWindow;

        public RoutePage(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
        }
        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            var query = SearchTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(query))
            {
                _mainWindow.MapControl.Map.Layers
                    .FirstOrDefault(l => l.Name == "SearchPin")?.Dispose();
                SearchResultsLb.ItemsSource = null;
                SearchResultsLb.Visibility = Visibility.Collapsed;
                return;
            }

            SearchButton.IsEnabled = false;
            SearchResultsLb.ItemsSource = null;

            try
            {
                var results = await _mainWindow._searchService.SearchAsync(query);
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
                MessageBox.Show($"Ошибка поиска:\n{ex.Message}", "Nominatim", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                SearchButton.IsEnabled = true;
            }
        }

        private void SearchResultsLb_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SearchResultsLb.SelectedItem is not NominatimResult res) return;
            _mainWindow.searchLat = res.Lat;
            _mainWindow.searchLon = res.Lon;
            _mainWindow._searchService.FlyToResult(res);
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

                RouteButton.IsEnabled = false;
                _mainWindow.LoadingIndicator.Visibility = Visibility.Visible;
                _mainWindow.routeService.ClearRoute();

                var result = await _mainWindow.routeService.BuildRouteFromFireStationAsync(
                    _mainWindow.searchLat, _mainWindow.searchLon);

                if (result.Success)
                {
                    BlockDistance.Text = $"Длина: {result.Distance / 1000:F1} км";
                    BlockDuration.Text = $"Время: {result.Duration / 60:F1} мин";
                    await Task.Delay(100);
                    _mainWindow.MapControl.Refresh();
                }
                else
                {
                    MessageBox.Show(result.ErrorMessage);
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
    }
}
