using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Nts.Extensions;
using Mapsui.Projections;
using Mapsui.Providers;
using Mapsui.Styles;
using Mapsui.Tiling;
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
            InitializeMap();
        }
        public void InitializeMap()
        {
            var map = new Map();
            map.Layers.Add(OpenStreetMap.CreateTileLayer());

            LoadGeoJsonLayer(map);

            SetInitialView(map);

            MapControl.Map = map;
        }
        private void LoadGeoJsonLayer(Map map)
        {
            // Путь к GeoJSON файлу
            var geojsonPath = @"MapVector\nskDISTandKSTV.geojson";
            if (!File.Exists(geojsonPath))
            {
                MessageBox.Show("Файл GeoJSON не найден.");
                return;
            }

            string geojson = File.ReadAllText(geojsonPath);
            var reader = new GeoJsonReader();
            FeatureCollection fc = reader.Read<FeatureCollection>(geojson);

            var features = new List<GeometryFeature>();

            foreach (NetTopologySuite.Features.IFeature f in fc)
            {
                var geom = f.Geometry;
                var polygons = ConvertGeometry(geom);

                string name = f.Attributes.Exists("name")
                    ? f.Attributes["name"].ToString()
                    : "Unknown";

                var borderColor = GetColorByName(name);

                foreach (var polygon in polygons)
                {
                    var projectedPolygon = ProjectGeometry(polygon);
                    var feature = new GeometryFeature
                    {
                        Geometry = projectedPolygon
                    };

                    var fillColor = new Color(borderColor.R, borderColor.G, borderColor.B, 60);

                    feature.Styles = new List<IStyle>
                    {
                        new VectorStyle
                        {
                            Fill = new Brush(fillColor),
                            Line = new Pen(borderColor, 2),
                            Outline = new Pen(borderColor, 2)
                        }
                    };

                    features.Add(feature);
                }
            }

            var layer = new MemoryLayer
            {
                Name = "GeoJSON Layer",
                Features = features,
                Style = null
            };

            map.Layers.Add(layer);
        }
        private void SetInitialView(Map map)
        {
            double lon = 82.92043;
            double lat = 55.03020;

            var centerPoint = SphericalMercator.FromLonLat(lon, lat);
            map.Navigator.CenterOn(centerPoint.x, centerPoint.y);
            map.Navigator.ZoomTo(200);
        }

        #region Вспомогательные методы

        private Color GetColorByName(string name)
        {
            var hash = name.GetHashCode();
            byte a = 100;
            byte r = (byte)((hash & 0xFF0000) >> 16);
            byte g = (byte)((hash & 0x00FF00) >> 8);
            byte b = (byte)(hash & 0x0000FF);
            return Color.FromArgb(a, r, g, b);
        }

        private List<Polygon> ConvertGeometry(Geometry geom)
        {
            var result = new List<Polygon>();

            if (geom is Polygon poly)
            {
                result.Add(poly);
            }
            else if (geom is MultiPolygon multi)
            {
                result.AddRange(multi.Geometries.Cast<Polygon>());
            }

            return result;
        }

        private Geometry ProjectGeometry(Geometry geometry)
        {
            var coordinates = geometry.Coordinates;
            var projectedCoordinates = new Coordinate[coordinates.Length];

            for (int i = 0; i < coordinates.Length; i++)
            {
                var projected = SphericalMercator.FromLonLat(coordinates[i].X, coordinates[i].Y);
                projectedCoordinates[i] = new Coordinate(projected.x, projected.y);
            }

            if (geometry is Polygon)
                return geometry.Factory.CreatePolygon(projectedCoordinates);
            else
                return geometry.Factory.CreateLineString(projectedCoordinates);
        }

        #endregion
    }
}
