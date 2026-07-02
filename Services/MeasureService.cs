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
        public MemoryLayer? MeasureLayer { get; }

        private readonly List<Coordinate> _points = new List<Coordinate>();

        private Coordinate? _mouseMovePoint;

        public MeasureMode CurrentMode { get; private set; } = MeasureMode.None;

        public MeasureService(MapControl mapControl)
        {
            _mapControl = mapControl;
            MeasureLayer = new MemoryLayer
            {
                Name = "MeasureLayer",
                Style = null 
            };
        }

        public void StartMeasurement(MeasureMode measureMode)
        {
            Clear();
            CurrentMode = measureMode;
        }

        public void StopMeasurement()
        {
            CurrentMode = MeasureMode.None;
            _mouseMovePoint = null;
            _mapControl?.Refresh();
        }

        public void Clear()
        {
            _points.Clear();
            _mouseMovePoint = null;
            if (MeasureLayer != null)
            {
                MeasureLayer.Features = new List<IFeature>();
            }
            CurrentMode = MeasureMode.None;
            _mapControl?.Refresh();
        }

        public void HandleClick(MPoint worldPosition)
        {
            if (CurrentMode == MeasureMode.None) return;

            var lonLat = SphericalMercator.ToLonLat(worldPosition.X, worldPosition.Y);
            _points.Add(new Coordinate(lonLat.lon, lonLat.lat));

            Redraw();
        }

        public void HandleMouseMove(MPoint worldPosition)
        {
            if (CurrentMode == MeasureMode.None || _points.Count == 0) return;

            var lonLat = SphericalMercator.ToLonLat(worldPosition.X, worldPosition.Y);
            _mouseMovePoint = new Coordinate(lonLat.lon, lonLat.lat);

            Redraw();
        }

        private void Redraw()
        {
            var features = new List<IFeature>();
            if (_points.Count == 0) return;

            // 1. Рисуем все зафиксированные кликами точки
            foreach (var p in _points)
            {
                var mercator = SphericalMercator.FromLonLat(p.X, p.Y);
                features.Add(new GeometryFeature(new Point(mercator.x, mercator.y))
                {
                    Styles = new List<IStyle> { new SymbolStyle { Fill = new Brush(Color.Red), SymbolScale = 0.5f } }
                });
            }

            var renderPoints = new List<Coordinate>(_points);
            if (_mouseMovePoint != null)
            {
                renderPoints.Add(_mouseMovePoint);
            }

            // 2. ЛОГИКА ДЛЯ ЛИНЕЙКИ
            if (CurrentMode == MeasureMode.Distance && renderPoints.Count > 1)
            {
                var lineCoords = renderPoints.Select(p => SphericalMercator.FromLonLat(p.X, p.Y))
                                             .Select(p => new Coordinate(p.x, p.y)).ToArray();

                var lineString = new LineString(lineCoords);

                double distanceMeters = CalculateDistance(renderPoints);
                string distanceText = FormatDistance(distanceMeters);

                features.Add(new GeometryFeature(lineString)
                {
                    Styles = new List<IStyle> {
                        new VectorStyle { Outline = new Pen(Color.Red, 2) },
                        new LabelStyle { Text = distanceText, Font = new Font { Size = 12, Bold = true }, BackColor = new Brush(Color.White) }
                    }
                });
            }

            // 3. ЛОГИКА ДЛЯ ПЛОЩАДИ
            if (CurrentMode == MeasureMode.Area && renderPoints.Count > 2)
            {
                var polyCoords = renderPoints.Select(p => SphericalMercator.FromLonLat(p.X, p.Y))
                                             .Select(p => new Coordinate(p.x, p.y)).ToList();
                polyCoords.Add(polyCoords[0]);

                var polygon = new Polygon(new LinearRing(polyCoords.ToArray()));

                double areaSquareMeters = CalculateArea(renderPoints);
                string areaText = FormatArea(areaSquareMeters);

                features.Add(new GeometryFeature(polygon)
                {
                    Styles = new List<IStyle> {
                        new VectorStyle { Fill = new Brush(new Color(255, 0, 0, 40)), Outline = new Pen(Color.Red, 2) },
                        new LabelStyle { Text = areaText, Font = new Font { Size = 12, Bold = true }, BackColor = new Brush(Color.White) }
                    }
                });
            }

            if (MeasureLayer != null)
            {
                MeasureLayer.Features = features;
            }
            _mapControl?.Refresh();
        }

        private double CalculateDistance(List<Coordinate> points)
        {
            double totalDist = 0;
            for (int i = 0; i < points.Count - 1; i++)
            {
                totalDist += HaversineDistance(points[i], points[i + 1]);
            }
            return totalDist;
        }

        private double CalculateArea(List<Coordinate> points)
        {
            if (points.Count < 3) return 0;

            // 1. Вычисляем среднюю широту полигона для коррекции искажения Меркатора
            var avgLat = points.Average(p => p.Y);
            var latRad = avgLat * Math.PI / 180.0;

            // 2. Переводим точки в плоские координаты Меркатора (в метрах)
            var projectedCoords = points.Select(p =>
            {
                var merc = SphericalMercator.FromLonLat(p.X, p.Y);
                return new Coordinate(merc.x, merc.y);
            }).ToList();

            projectedCoords.Add(new Coordinate(projectedCoords[0].X, projectedCoords[0].Y));

            // 3. Считаем "искаженную" площадь в проекции Меркатора
            double mercatorArea = Math.Abs(Area.OfRing(projectedCoords.ToArray()));

            // 4. Корректируем площадь: делим на масштабный коэффициент проекции в квадрате!
            double cosLat = Math.Cos(latRad);
            double realAreaSquareMeters = mercatorArea * (cosLat * cosLat);

            return realAreaSquareMeters;
        }

        private double HaversineDistance(Coordinate p1, Coordinate p2)
        {
            double r = 6371000; // Радиус Земли в метрах
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

        private string FormatDistance(double meters)
        {
            if (meters < 1000)
                return $"{meters:F1} м";

            return $"{meters / 1000:F2} км";
        }

        private string FormatArea(double squareMeters)
        {
            if (squareMeters < 100_000)
                return $"{squareMeters:F1} м²";
            if (squareMeters < 10_000_000)
                return $"{squareMeters / 10_000:F2} га";

            return $"{squareMeters / 1_000_000:F2} км²";
        }
    }
}
