using DocumentFormat.OpenXml.Office2010.Word;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.UI.Wpf;
using System;
using System.Collections.Generic;
using System.Text;

namespace FFSchedule.Presentation
{
    public class SearchVisualizer
    {
        private readonly MapControl _mapControl;
        private const string SEARCH_PIN_LAYER = "SearchPin";
        private MemoryLayer? _pinLayer;

        public SearchVisualizer(MapControl mapControl)
        {
            _mapControl = mapControl;
        }

        public void InitializeLayers(Map map)
        {
            var old = map.Layers.FirstOrDefault(l => l.Name == SEARCH_PIN_LAYER);
            if (old != null) map.Layers.Remove(old);

            _pinLayer = new MemoryLayer
            {
                Name = SEARCH_PIN_LAYER,
                Style = null
            };
            map.Layers.Add(_pinLayer);
        }

        public void PutSearchPin(double lon, double lat, string displayName, string shortDisplayName)
        {
            if (_pinLayer == null) return;

            var merc = SphericalMercator.FromLonLat(lon, lat);

            var pin = new GeometryFeature
            {
                Geometry = new NetTopologySuite.Geometries.Point(merc.x, merc.y),
                ["label"] = displayName,
                ["shortLabel"] = shortDisplayName
            };

            pin.Styles.Add(new SymbolStyle
            {
                SymbolType = SymbolType.Rectangle,
                Fill = new Brush(Color.FromArgb(255, 255, 143, 0)),
                Outline = new Pen(new Color(255, 255, 255, 255), 3.0f) { PenStyle = PenStyle.Solid },
                SymbolScale = 0.35f,
                MinVisible = 1,
                MaxVisible = 500
            });

            pin.Styles.Add(new LabelStyle
            {
                Text = shortDisplayName,
                Font = new Font { Size = 12, Bold = true, FontFamily = "Arial" },
                ForeColor = new Color(0, 0, 0),
                BackColor = new Brush(new Color(255, 255, 255, 220)),
                Halo = new Pen(new Color(255, 255, 255, 200), 1.5f),
                Offset = new Offset(0.0, -18),
                HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Center,
                LineHeight = 1.2,
                MaxVisible = 80
            });

            _pinLayer.Features = new[] { pin };
            _mapControl.Refresh();
        }
        public void FlyToResult(double lon, double lat, string displayName, string shortDisplayName)
        {
            var merc = SphericalMercator.FromLonLat(lon, lat);

            _mapControl.Map?.Navigator?.CenterOn(merc.x, merc.y);
            _mapControl.Map?.Navigator?.ZoomToLevel(15);

            PutSearchPin(lon, lat, displayName, shortDisplayName);
        }

        public void RemoveSearchPin()
        {
            if (_pinLayer != null)
            {
                _pinLayer.Features = Enumerable.Empty<IFeature>();
                _mapControl.Refresh();
            }
        }
    }
}
