using FFSchedule.Container;
using System.Windows;
using System.Windows.Controls;


namespace FFSchedule.Page
{
    /// <summary>
    /// Логика взаимодействия для FireStationsPage.xaml
    /// </summary>
    public partial class FireStationsPage : System.Windows.Controls.Page
    {
        private readonly MainWindow _mainWindow;

        public FireStationsPage(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            FireStationsListBox.ItemsSource = mainWindow.fireStations;
        }

        private void FireStationsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FireStationsListBox.SelectedItem == null)
            {
                HideInfoPanel();
                return;
            }

            var selectedStation = FireStationsListBox.SelectedItem as FireStation;
            if (selectedStation == null) { HideInfoPanel(); return; }

            FireStationName.Text = selectedStation.Name ?? "Без названия";
            FireStationAddress.Text = selectedStation.Address ?? "Не указан";
            FireStationDistrict.Text = selectedStation.District ?? "Не указано";
            FireStationType.Text = selectedStation.Type ?? "Не указано";
            FireStationPhone.Text = selectedStation.Phone ?? "Не указано";

            FireStationInfoPanel.Visibility = Visibility.Visible;
            NoSelectionText.Visibility = Visibility.Collapsed;

            var projected = Mapsui.Projections.SphericalMercator.FromLonLat(
                selectedStation.Longitude, selectedStation.Latitude);
            _mainWindow.MapControl.Map.Navigator.CenterOn(projected.x, projected.y);
            _mainWindow.MapControl.Map.Navigator.ZoomToLevel(12);
        }

        private void HideInfoPanel()
        {
            FireStationInfoPanel.Visibility = Visibility.Collapsed;
            NoSelectionText.Visibility = Visibility.Visible;
        }
        public void SelectFireStation(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;

            var station = _mainWindow.fireStations
                .FirstOrDefault(fs => fs.Name?.Equals(name, StringComparison.OrdinalIgnoreCase) == true);

            if (station != null)
            {
                FireStationsListBox.SelectedItem = station;
                FireStationsListBox.ScrollIntoView(station); // Прокрутить к элементу
            }
        }
    }
}
