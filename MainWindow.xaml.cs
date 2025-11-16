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
using BruTile.MbTiles;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Tiling;
using Mapsui.Tiling.Extensions;
using Mapsui.Tiling.Layers;
using Mapsui.UI.Wpf;
using Mapsui.Utilities;
using Microsoft.Data.Sqlite;
using SQLite;
using System;
using System.IO;

namespace FFSchedule;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        string mbTilesPath = @"Map\OpenStreetMap.mbtiles";

        if (!File.Exists(mbTilesPath))
        {
            MessageBox.Show("Файл .mbtiles не найден. Проверьте путь.");
            return;
        }

        try
        {
            var connectionString = new SQLiteConnectionString(mbTilesPath);
            var mbTilesSource = new MbTilesTileSource(connectionString);

            var tileLayer = new TileLayer(mbTilesSource);

            var map = new Map();
            map.Layers.Add(tileLayer);

            MapControl.Map = map;

            var extent = mbTilesSource.Schema.Extent;
            MapControl.Map.Navigator.ZoomToBox(extent.ToMRect());

            Console.WriteLine($"Extent: {extent}");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки .mbtiles: {ex.Message}\nПроверьте путь к файлу и его формат. Стек: {ex.StackTrace}");
        }
    }
}
/*using BruTile.MbTiles;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Tiling;
using Mapsui.Tiling.Extensions;
using Mapsui.Tiling.Layers;
using Mapsui.UI.Wpf;
using Mapsui.Utilities;
using Microsoft.Data.Sqlite;
using SQLite;
using System;
using System.IO;
using System.Windows;

namespace MapsuiMbTilesDemo
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            string mbTilesPath = @"C:\Users\Иван\source\repos\MapsuiMbTilesDemo\MapsuiMbTilesDemo\MapsuiMbTilesDemo\Map\OpenStreetMap.mbtiles";

            if (!File.Exists(mbTilesPath))
            {
                MessageBox.Show("Файл .mbtiles не найден. Проверьте путь.");
                return;
            }

            try
            {
                var connectionString = new SQLiteConnectionString(mbTilesPath);
                var mbTilesSource = new MbTilesTileSource(connectionString);

                var tileLayer = new TileLayer(mbTilesSource);

                var map = new Map();
                map.Layers.Add(tileLayer);

                MapControl.Map = map;

                var extent = mbTilesSource.Schema.Extent;
                MapControl.Map.Navigator.ZoomToBox(extent.ToMRect());

                Console.WriteLine($"Extent: {extent}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки .mbtiles: {ex.Message}\nПроверьте путь к файлу и его формат. Стек: {ex.StackTrace}");
            }
        }
    }
}*/