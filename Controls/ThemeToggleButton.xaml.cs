using FFSchedule.Themes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public partial class ThemeToggleButton : UserControl
    {
        public ThemeToggleButton()
        {
            InitializeComponent();
            UpdateButtonContent();
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.ToggleTheme();
            UpdateButtonContent();
            this.Background = (SolidColorBrush)Application.Current.Resources["BackgroundBrush"];
        }

        private void UpdateButtonContent()
        {
            ToggleButton.Content = ThemeManager.CurrentTheme == ThemeManager.Theme.Light ? "🌙" : "☀️";
        }
    }
}
