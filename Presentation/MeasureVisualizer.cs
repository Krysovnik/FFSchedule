using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.UI.Wpf;
using NetTopologySuite.Geometries;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Text;

namespace FFSchedule.Presentation
{
    public class MeasureVisualizer
    {
        private readonly MapControl _mapControl;
        public MemoryLayer MeasureLayer { get; }

        public MeasureVisualizer(MapControl mapControl)
        {
            _mapControl = mapControl;
            MeasureLayer = new MemoryLayer
            {
                Name = "MeasureLayer",
                Style = null
            };
        }
        public void InitializeLayers(Map map)
        {
            if (map.Layers.Contains(MeasureLayer))
                map.Layers.Remove(MeasureLayer);

            map.Layers.Add(MeasureLayer);
        }
        public void ClearGraphics()
        {
            MeasureLayer.Features = new List<IFeature>();
            _mapControl.Refresh();
        }
        public void Render(List<Coordinate> points, Coordinate? mousePoint, Services.MeasureMode mode, string labelText)
        {
            var features = new List<IFeature>();

            if(points.Count == 0)
            {
                MeasureLayer.Features = features;
                _mapControl.Refresh();
                return;
            }

            foreach (var p in points)
            {
                var mercator = SphericalMercator.FromLonLat(p.X, p.Y);
                features.Add(new GeometryFeature(new NetTopologySuite.Geometries.Point(mercator.x, mercator.y))
                {
                    Styles = new List<IStyle> { new SymbolStyle { Fill = new Brush(Color.Red), SymbolScale = 0.5f } }
                });
            }

            var renderPoints = new List<Coordinate>(points);
            if (mousePoint != null)
            {
                renderPoints.Add(mousePoint);
            }

            if (mode == Services.MeasureMode.Distance && renderPoints.Count > 1)
            {
                var lineCoords = renderPoints.Select(p => SphericalMercator.FromLonLat(p.X,p.Y))
                    .Select(p => new Coordinate(p.x,p.y)).ToArray();
                var lineString = new LineString(lineCoords);

                features.Add(new GeometryFeature(lineString)
                {
                    Styles = new List<IStyle>
                    {
                        new VectorStyle { Outline = new Pen(Color.Red, 2) },
                        new LabelStyle { Text = labelText, Font = new Font { Size = 12, Bold = true }
                        , BackColor = new Brush(Color.White) }
                    }
                });
            }

            if(mode == Services.MeasureMode.Area && renderPoints.Count > 2)
            {
                var polyCoords = renderPoints.Select(p => SphericalMercator.FromLonLat(p.X, p.Y))
                    .Select(p => new Coordinate(p.x, p.y)).ToList();

                polyCoords.Add(polyCoords[0]);

                var polygon = new Polygon(new LinearRing(polyCoords.ToArray()));

                features.Add(new GeometryFeature(polygon)
                {
                    Styles = new List<IStyle> {
                        new VectorStyle { Fill = new Brush(new Color(255, 0, 0, 40)), Outline = new Pen(Color.Red, 2) },
                        new LabelStyle { Text = labelText, Font = new Font { Size = 12, Bold = true }, BackColor = new Brush(Color.White) }
                    }
                });
            }

            MeasureLayer.Features = features;
            _mapControl.Refresh();
        }
    }
}
