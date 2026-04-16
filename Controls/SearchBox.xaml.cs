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

namespace FFSchedule.Controls
{
    /// <summary>
    /// Логика взаимодействия для SearchBox.xaml
    /// </summary>
    public partial class SearchBox : UserControl
    {
        public SearchService SearchService { get; set; }

        public static readonly DependencyProperty SearchQueryProperty =
            DependencyProperty.Register(nameof(SearchQuery),
                typeof(string), typeof(SearchBox),
                new FrameworkPropertyMetadata(string.Empty,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string SearchQuery
        {
            get => (string)GetValue(SearchQueryProperty);
            set => SetValue(SearchQueryProperty, value);
        }

        public event EventHandler<NominatimResult>? ResultSelected;

        private readonly MainWindow _mainWindow;

        public SearchBox()
        {
            InitializeComponent();
            _mainWindow = (MainWindow)Application.Current.MainWindow;
        }
        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            var query = SearchTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(query))
            {
                ClearResults();
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
            if (SearchResultsLb.SelectedItem is NominatimResult res)
                ResultSelected?.Invoke(this, res);
        }

        private void ClearResults()
        {
            _mainWindow._searchService.RemoveSearchPin();
            SearchResultsLb.ItemsSource = null;
            SearchResultsLb.Visibility = Visibility.Collapsed;
        }
    }
}
