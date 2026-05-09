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
                _mainWindow.LoadingIndicator.Visibility = Visibility.Visible;

                StationsListBox.ItemsSource = null;

                var results = await _mainWindow.routeService.BuildRoutesByRequirementAsync(
                    _mainWindow.searchLat,
                    _mainWindow.searchLon,
                    neededEquipment);

                if (results.Any(r => r.Success))
                {
                    var displayItems = results.Where(r => r.Success).Select(r => new
                    {
                        StationName = r.Station != null ? $"Станция {r.Station.Name}" : "Неизвестная станция",
                        DistanceText = $"Путь: {r.Distance / 1000:F1} км",
                        DurationText = $"Время: {r.Duration / 60:F1} мин"
                    }).ToList();

                    StationsListBox.Visibility = Visibility.Visible;
                    StationsListBox.ItemsSource = displayItems;

                    _mainWindow.MapControl.Refresh();
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
