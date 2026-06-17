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

            foreach(var p in _points)
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
                        new LabelStyle { Text = $"{areaHectares:F2} га", Font = new Font { Size = 12, Bold = true }, BackColor = new Brush(Color.White) }
                    }
                });
            }

            _measureLayer.Features = features;
            _mapControl.Refresh();
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

            var latRad = points.Average(p => p.Y) * Math.PI / 180.0;
            var kX = 111320.0 * Math.Cos(latRad);
            var kY = 110574.0;

            double area = 0;
            for (int i = 0; i < points.Count; i++)
            {
                var j = (i + 1) % points.Count;
                area += (points[i].X * kX) * (points[j].Y * kY);
                area -= (points[j].X * kX) * (points[i].Y * kY);
            }
            return Math.Abs(area) / 2.0 / 10000.0;
        }

        private double HaversineDistance(Coordinate p1, Coordinate p2)
        {
            double r = 6371000;
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
