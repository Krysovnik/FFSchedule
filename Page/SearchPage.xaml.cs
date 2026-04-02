using FFSchedule.Class;
using FFSchedule.Services;
using Microsoft.EntityFrameworkCore;
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
    /// Логика взаимодействия для SearchPage.xaml
    /// </summary>
    public partial class SearchPage : System.Windows.Controls.Page
    {
        private readonly MainWindow _mainWindow;
        public string SearchQuery { get; set; } = "";
        public SearchPage(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            DataContext = this;
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

        private void GenerateWordTable_Click(object sender, RoutedEventArgs e)
        {
            var settlements = _mainWindow._dbcontext.Settlements
                .Include(s => s.Vc)
                .Include(s => s.Tol)
                .Include(s => s.SettlementMainDepartaments)
                .ThenInclude(smd => smd.Dpt)
                .Include(s => s.SettlementDepartamentDistances)
                .ThenInclude(sdd => sdd.Dpt)
                .Include(s => s.EquipmentTypeQuantities)
                .ThenInclude(etq => etq.Dpt)
                .ToList();

            var exporter = new WordTableExporter();
            string templatePath = @"FFS/template.docx";
            string outputPath = @"FFS/Schedule.docx";
            exporter.ExportSettlementsOnly(settlements, templatePath, outputPath);
            MessageBox.Show("Word документ создан!");
        }
    }
}
