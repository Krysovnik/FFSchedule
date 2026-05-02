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

        private CancellationTokenSource? _searchCts;

        public SearchBox()
        {
            InitializeComponent();
            _mainWindow = (MainWindow)Application.Current.MainWindow;
        }
        private async void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var query = SearchTextBox.Text.Trim();

            _searchCts?.Cancel();

            if (string.IsNullOrWhiteSpace(query))
            {
                ClearResults();
                return;
            }

            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            try
            {
                HideAllStates();
                LoadingIndicator.Visibility = Visibility.Visible;

                await Task.Delay(500, token);

                var results = await _mainWindow._searchService.SearchAsync(query);

                if (token.IsCancellationRequested) return;

                LoadingIndicator.Visibility = Visibility.Collapsed;

                if (results != null && results.Any())
                {
                    SearchResultsLb.ItemsSource = results;
                    SearchResultsLb.Visibility = Visibility.Visible;
                }
                else
                {
                    NoResultsText.Visibility = Visibility.Visible;
                }
            }
            catch (TaskCanceledException)
            {

            }
            catch (Exception ex)
            {
                if (token.IsCancellationRequested) return;

                LoadingIndicator.Visibility = Visibility.Collapsed;
                ErrorText.Text = $"Ошибка: {ex.Message}";
                ErrorText.Visibility = Visibility.Visible;
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
        private void HideAllStates()
        {
            SearchResultsLb.Visibility = Visibility.Collapsed;
            LoadingIndicator.Visibility = Visibility.Collapsed;
            NoResultsText.Visibility = Visibility.Collapsed;
            ErrorText.Visibility = Visibility.Collapsed;
        }
        public async Task ExternalSearchAndSelectFirst(double lat, double lon)
        {
            var result = await _mainWindow._searchService.ReverseSearchAsync(lat, lon);
            if (result != null)
            {
                SearchTextBox.Text = result.DisplayName;
                ResultSelected?.Invoke(this, result);
            }
        }
        public void FillAndSelect(NominatimResult result)
        {
            SearchTextBox.Text = result.ShortDisplayName;
            //HideAllStates();
            ResultSelected?.Invoke(this, result);
        }
        public void ResetView()
        {
            SearchQuery = string.Empty;
            SearchTextBox.Text = string.Empty;
            HideAllStates();
        }
    }
}
