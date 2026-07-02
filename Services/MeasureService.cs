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
        public List<Coordinate> Points { get; } = new List<Coordinate>();
        public Coordinate? MouseMovePoint { get; private set; }
        public MeasureMode CurrentMode { get; private set; } = MeasureMode.None;
        public void StartMeasurement(MeasureMode measureMode)
        {
            Clear();
            CurrentMode = measureMode;
        }
        public void StopMeasurement()
        {
            CurrentMode = MeasureMode.None;
            MouseMovePoint = null;
        }
        public void Clear()
        {
            Points.Clear();
            MouseMovePoint = null;
            CurrentMode = MeasureMode.None;
        }
        public void HandleClick(Coordinate lonLat)
        {
            if (CurrentMode == MeasureMode.None) return;
            Points.Add(lonLat);
        }
        public void HandleMouseMove(Coordinate lonLat)
        {
            if (CurrentMode == MeasureMode.None || Points.Count == 0) return;
            MouseMovePoint = lonLat;
        }
        public string GetFormattedResult()
        {
            var renderPoints = new List<Coordinate>(Points);
            if (MouseMovePoint != null)
            {
                renderPoints.Add(MouseMovePoint);
            }

            if (CurrentMode == MeasureMode.Distance && renderPoints.Count > 1)
            {
                double distanceMeters = CalculateDistance(renderPoints);
                return FormatDistance(distanceMeters);
            }

            if (CurrentMode == MeasureMode.Area && renderPoints.Count > 2)
            {
                double areaSquareMeters = CalculateArea(renderPoints);
                return FormatArea(areaSquareMeters);
            }

            return string.Empty;
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

            var avgLat = points.Average(p => p.Y);
            var latRad = avgLat * Math.PI / 180.0;

            var projectedCoords = points.Select(p =>
            {
                double x = p.X * 20037508.34 / 180;
                double y = Math.Log(Math.Tan((90 + p.Y) * Math.PI / 360)) / (Math.PI / 180);
                y = y * 20037508.34 / 180;
                return new Coordinate(x, y);
            }).ToList();

            projectedCoords.Add(new Coordinate(projectedCoords[0].X, projectedCoords[0].Y));

            double mercatorArea = Math.Abs(Area.OfRing(projectedCoords.ToArray()));
            double cosLat = Math.Cos(latRad);

            return mercatorArea * (cosLat * cosLat);
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
        private string FormatDistance(double meters)
        {
            if (meters < 1000) return $"{meters:F1} м";
            return $"{meters / 1000:F2} км";
        }
        private string FormatArea(double squareMeters)
        {
            if (squareMeters < 100_000) return $"{squareMeters:F1} м²";
            if (squareMeters < 10_000_000) return $"{squareMeters / 10_000:F2} га";
            return $"{squareMeters / 1_000_000:F2} км²";
        }
    }
}
