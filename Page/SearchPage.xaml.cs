namespace FFSchedule.Page
{
    /// <summary>
    /// Логика взаимодействия для SearchPage.xaml
    /// </summary>
    public partial class SearchPage : System.Windows.Controls.Page
    {
        private readonly MainWindow _mainWindow;
        public SearchPage(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
        }
    }
}
