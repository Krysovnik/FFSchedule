using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FFSchedule.Themes
{
    public static class ThemeManager
    {
        private const string LightThemePath = "/Themes/LightThemes.xaml";
        private const string DarkThemePath = "/Themes/DarkThemes.xaml";
        private const string StylesPath = "Styles.xaml";

        public enum Theme
        {
            Light,
            Dark
        }

        public static Theme CurrentTheme { get; private set; } = Theme.Light;

        public static void SwitchTheme(Theme theme)
        {
            CurrentTheme = theme;

            var app = Application.Current;
            if (app == null) return;

            app.Resources.MergedDictionaries.Clear();

            ResourceDictionary themeDictionary = new ResourceDictionary();
            themeDictionary.Source = theme == Theme.Light
                ? new System.Uri(LightThemePath, System.UriKind.Relative)
                : new System.Uri(DarkThemePath, System.UriKind.Relative);

            app.Resources.MergedDictionaries.Add(themeDictionary);

            ResourceDictionary stylesDictionary = new ResourceDictionary();
            stylesDictionary.Source = new System.Uri(StylesPath, System.UriKind.Relative);
            app.Resources.MergedDictionaries.Add(stylesDictionary);

            foreach (Window window in app.Windows)
            {
                window.Resources.MergedDictionaries.Clear();
                window.Resources.MergedDictionaries.Add(themeDictionary);
                window.Resources.MergedDictionaries.Add(stylesDictionary);
            }
        }

        public static void ToggleTheme()
        {
            var newTheme = CurrentTheme == Theme.Light ? Theme.Dark : Theme.Light;
            SwitchTheme(newTheme);
        }
    }
}
