using FFSchedule.Models;
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
using Microsoft.EntityFrameworkCore;

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
            Loaded += (s, e) => LoadRanks();
        }
        private void OnResultSelected(object sender, NominatimResult res)
        {
            _mainWindow.searchLat = res.Lat;
            _mainWindow.searchLon = res.Lon;
            _mainWindow._searchService.FlyToResult(res);
        }
        private async void LoadRanks()
        {
            using (var db = new FfsContext())
            {
                var ranks = await db.Ranks
                       .OrderBy(r => r.RNumber)
                       .ToListAsync();
                RankComboBox.ItemsSource = ranks;
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
