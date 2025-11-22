using Mapsui;
using Mapsui.Geometries;
using Mapsui.Layers;
using Mapsui.Projection;
using Mapsui.Providers;
using Mapsui.Styles;
using Mapsui.UI.Wpf;
using Mapsui.Utilities;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.Marshalling;
using System.Windows;

namespace FFSchedule
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var map = new Map();
            map.Layers.Add(OpenStreetMap.CreateTileLayer());

            // читаем GeoJSON
            var geojsonPath = @"MapVector\nskDISTandKSTV.geojson";
            string geojson = File.ReadAllText(geojsonPath);

            var reader = new GeoJsonReader();
            FeatureCollection fc = reader.Read<FeatureCollection>(geojson);

            // список Mapsui Feature
            var mapsuiFeatures = new List<Mapsui.Providers.Feature>();

            foreach (NetTopologySuite.Features.IFeature f in fc)
            {
                var geom = f.Geometry;
                var polygons = ConvertGeometry(geom);

                // Цвет для границы и заливки
                string name = f.Attributes.Exists("name")
              ? f.Attributes["name"].ToString()
              : "Unknown";


                var borderColor = GetColorByName(name);


                foreach (var p in polygons)
                {
                    var feature = new Mapsui.Providers.Feature
                    {
                        Geometry = p
                    };

                    var layer = new MemoryLayer
                    {
                        Name = f.Attributes["name"].ToString(),
                        DataSource = new MemoryProvider(new List<Mapsui.Providers.Feature> { feature }),
                        Style = new VectorStyle
                        {
                            Fill = new Brush(Color.FromArgb(60, borderColor.R, borderColor.G, borderColor.B)), // заливка
                            Line = new Pen(borderColor, 2) // граница опаа
                        }
                    };

                    map.Layers.Add(layer);
                }
            }


            //map.Layers.Add(layer);

            // центровка не але вообще надо переделать
            // Zoom
            double lon = 82.92043;
            double lat = 55.03020;

            // Переводим в WebMercator (Mapsui так работает)
            var centerPoint = SphericalMercator.FromLonLat(lon, lat);

            // Центрируем карту
            MapControl.Navigator.CenterOn(centerPoint);

            // Можно сразу поставить масштаб (опционально)
            MapControl.Navigator.ZoomTo(200); // чем меньше число — тем ближе



            MapControl.Map = map;
        }

        private Color GetColorByName(string name)
        {
            var hash = name.GetHashCode();
            byte a = 245;
            byte r = (byte)((hash & 0xFF0000) >> 16);
            byte g = (byte)((hash & 0x00FF00) >> 8);
            byte b = (byte)(hash & 0x0000FF);
            return Color.FromArgb(a, r, g, b);
        }

        // ---------- КОНВЕРТАЦИЯ NTS → Mapsui ----------
        private List<Mapsui.Geometries.Polygon> ConvertGeometry(NetTopologySuite.Geometries.Geometry geom)
        {
            var result = new List<Mapsui.Geometries.Polygon>();

            if (geom is NetTopologySuite.Geometries.Polygon poly)
            {
                result.Add(ConvertPolygon(poly));
            }
            else if (geom is NetTopologySuite.Geometries.MultiPolygon multi)
            {
                foreach (var g in multi.Geometries)
                {
                    result.Add(ConvertPolygon((NetTopologySuite.Geometries.Polygon)g));
                }
            }

            return result;
        }

        private Mapsui.Geometries.Polygon ConvertPolygon(NetTopologySuite.Geometries.Polygon poly)
        {
            // внешнее кольцо
            var shellPoints = new List<Mapsui.Geometries.Point>();

            foreach (var c in poly.ExteriorRing.Coordinates)
            {
                var m = SphericalMercator.FromLonLat(c.X, c.Y);
                shellPoints.Add(new Mapsui.Geometries.Point(m.X, m.Y));
            }

            var shell = new Mapsui.Geometries.LinearRing(shellPoints);

            // внутренние кольца
            var holes = new List<Mapsui.Geometries.LinearRing>();

            for (int i = 0; i < poly.NumInteriorRings; i++)
            {
                var ringPoints = new List<Mapsui.Geometries.Point>();
                foreach (var c in poly.GetInteriorRingN(i).Coordinates)
                {
                    var m = SphericalMercator.FromLonLat(c.X, c.Y);
                    ringPoints.Add(new Mapsui.Geometries.Point(m.X, m.Y));
                }
                holes.Add(new Mapsui.Geometries.LinearRing(ringPoints));
            }

            return new Mapsui.Geometries.Polygon(shell, holes);
        }
    }
}
