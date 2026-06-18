using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Mapsui.Layers;
using Mapsui.UI.Wpf;
using NetTopologySuite.Geometries;
using System.Text;
using Mapsui;
using Mapsui.Nts;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Graphics.OpenGL;
using NetTopologySuite.Algorithm;

namespace FFSchedule.Services
{
    public enum MeasureMode { None, Distance, Area }
    public class MeasureService
    {
        private readonly MapControl? _mapControl;
        private readonly MemoryLayer? _measureLayer;
        private readonly List<Coordinate> _points = new List<Coordinate>();

        public MeasureMode CurrentMode { get; private set; } = MeasureMode.None;

        public MeasureService(MapControl mapControl)
        {
            _mapControl = mapControl;
            _measureLayer = new MemoryLayer
            {
                Name = "MeasureLayer",
                Style = null
            };
            _mapControl.Map.Layers.Add(_measureLayer);
        }

        public void StartMeasurement(MeasureMode measureMode)
        {
            Clear();
            CurrentMode = measureMode;
        }
        public void StopMeasurement()
        {
            CurrentMode = MeasureMode.None;
            _mapControl?.Refresh();
        }

        public void Clear()
        {
            _points.Clear();
            if(_measureLayer != null)
            {
                _measureLayer.Features = new List<IFeature>();
            }
            CurrentMode = MeasureMode.None;
            _mapControl?.Refresh();
        }

        public void HandleClick(MPoint worldPosition)
        {
            if(CurrentMode == MeasureMode.None) return;

            var lonLat = SphericalMercator.ToLonLat(worldPosition.X, worldPosition.Y);
            _points.Add(new Coordinate(lonLat.lon, lonLat.lat));

            Redraw();
        }

        private void Redraw()
        {
            var features = new List<IFeature>();
            if (_points.Count == 0) return;

            // Отрисовка опорных точек (маркеров клика)
            foreach (var p in _points)
            {
                var mercator = SphericalMercator.FromLonLat(p.X, p.Y);
                features.Add(new GeometryFeature(new Point(mercator.x, mercator.y))
                {
                    Styles = new List<IStyle> { new SymbolStyle { Fill = new Brush(Color.Red), SymbolScale = 0.5 } }
                });
            }

            // Логика для Линейки
            if (CurrentMode == MeasureMode.Distance && _points.Count > 1)
            {
                var lineCoords = _points.Select(p => SphericalMercator.FromLonLat(p.X, p.Y))
                                       .Select(p => new Coordinate(p.x, p.y)).ToArray();

                var lineString = new LineString(lineCoords);
                var distance = CalculateDistance(_points);

                features.Add(new GeometryFeature(lineString)
                {
                    Styles = new List<IStyle> {
                        new VectorStyle { Outline = new Pen(Color.Red, 2) },
                        new LabelStyle { Text = $"{distance:F2} км", Font = new Font { Size = 12, Bold = true }, BackColor = new Brush(Color.White) }
                    }
                });
            }

            // Логика для Площади
            if (CurrentMode == MeasureMode.Area && _points.Count > 2)
            {
                var polyCoords = _points.Select(p => SphericalMercator.FromLonLat(p.X, p.Y))
                                       .Select(p => new Coordinate(p.x, p.y)).ToList();
                polyCoords.Add(polyCoords[0]);

                var polygon = new Polygon(new LinearRing(polyCoords.ToArray()));
                var areaHectares = CalculateArea(_points);

                features.Add(new GeometryFeature(polygon)
                {
                    Styles = new List<IStyle> {
                        new VectorStyle { Fill = new Brush(new Color(255, 0, 0, 50)), Outline = new Pen(Color.Red, 2) },
                        new LabelStyle { Text = $"{areaHectares:F2} м²", Font = new Font { Size = 12, Bold = true }, BackColor = new Brush(Color.White) }
                    }
                });
            }

            _measureLayer?.Features = features;
            _mapControl?.Refresh();
        }

        private double CalculateDistance(List<Coordinate> points)
        {
            double totalDist = 0;
            for (int i = 0; i < points.Count - 1; i++)
            {
                totalDist += HaversineDistance(points[i], points[i + 1]);
            }
            return totalDist / 1000; 
        }

        private double CalculateArea(List<Coordinate> points)
        {
            if (points.Count < 3) return 0;

            //для начала найдем среднию широту (проеция какого-то Меркатора)
            var avgLat = points.Average(p => p.Y);
            var latRad = avgLat * Math.PI / 180.0;

            //потом в метры 
            var projectedCoords = points.Select(p =>
            {
                var merc = SphericalMercator.FromLonLat(p.X, p.Y);
                return new Coordinate(merc.x, merc.y);
            }).ToList();

            //замыкаем
            projectedCoords.Add(new Coordinate(projectedCoords[0].X, projectedCoords[0].Y));

            //площадь в метры Меркатора
            double mercatorArea = Math.Abs(Area.OfRing(projectedCoords.ToArray()));

            //корректируем искажение Меркатора для широты Новосибирска (делением на cos²(lat))
            double cosLat = Math.Cos(latRad);
            double areaSquareMeters = mercatorArea * cosLat * cosLat;

            return areaSquareMeters;
        }

        private double HaversineDistance(Coordinate p1, Coordinate p2)
        {
            double r = 6371000;//радиус земли, чтоб не забыл
            double phi1 = p1.Y * Math.PI / 180;
            double phi2 = p2.Y * Math.PI / 180;
            double dPhi = (p2.Y - p1.Y) * Math.PI / 180;
            double dLambda = (p2.X - p1.X) * Math.PI / 180;

            double a = Math.Sin(dPhi / 2) * Math.Sin(dPhi / 2) +
                       Math.Cos(phi1) * Math.Cos(phi2) *
                       Math.Sin(dLambda / 2) * Math.Sin(dLambda / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return r * c;
        }

    }
}
