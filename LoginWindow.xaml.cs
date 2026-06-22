using FFSchedule.Models;
using System.Linq;
using System.Windows;

namespace FFSchedule
{
    public partial class LoginWindow : Window
    {
        private readonly FfsContext _context;

        public LoginWindow()
        {
            InitializeComponent();
            _context = new FfsContext();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show(
                    "Введите логин и пароль",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            Employee? employee = _context.Employees
                .FirstOrDefault(x => x.EmLogin == login && x.EmPassword == password);

            if (employee != null)
            {

                FFSchedule.Properties.Settings.Default.IsLoggedIn = true;
                FFSchedule.Properties.Settings.Default.Save(); 

                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show(
                    "Неверный логин или пароль",
                    "Ошибка авторизации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}