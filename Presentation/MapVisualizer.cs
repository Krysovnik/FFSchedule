using FFSchedule.Container;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Styles;
using Mapsui.UI.Wpf;
using NetTopologySuite.Geometries;

namespace FFSchedule.Presentation
{
    public class MapVisualizer
    {
        private readonly MapControl _mapControl;
        private readonly MemoryLayer _polygonLayer;
        private readonly MemoryLayer _stationLayer;
        private readonly MemoryLayer _hoverLayer;

        private bool _polygonFillEnabled = true;
        private double _polygonBorderWidth = 0.5;
        private GeoJsonLoadResult? _lastLoadedData;

        public Dictionary<GeometryFeature, Brush?> OriginalFills { get; } = new();
        public Dictionary<GeometryFeature, List<IStyle>> OriginalStyles { get; } = new();

        public MapVisualizer(MapControl mapControl)
        {
            _mapControl = mapControl;

            _polygonLayer = new MemoryLayer { Name = "Polygons", Style = null };
            _stationLayer = new MemoryLayer { Name = "Points", Style = null };
            _hoverLayer = new MemoryLayer { Name = "HoverLayer", Enabled = false };        
        }
        public void InitializeLayers(Map map)
        {
            if (map.Layers.Contains(_polygonLayer)) map.Layers.Remove(_polygonLayer);
            if (map.Layers.Contains(_stationLayer)) map.Layers.Remove(_stationLayer);
            if (map.Layers.Contains(_hoverLayer)) map.Layers.Remove(_hoverLayer);

            map.Layers.Add(_polygonLayer);
            map.Layers.Add(_stationLayer);
            map.Layers.Add(_hoverLayer);
        }
        public void RenderGeoJsonData(GeoJsonLoadResult data, bool villageCouncilsVisible, bool fireStationsVisible)
        {
            _lastLoadedData = data;
            OriginalFills.Clear();
            OriginalStyles.Clear();

            // 1. Полигоны
            _polygonLayer.Enabled = villageCouncilsVisible;
            var polygonFeatures = new List<GeometryFeature>();
            foreach (var polyObj in data.Polygons)
            {
                var projectedCoords = polyObj.Geometry.Coordinates.Select(c =>
                {
                    var p = Mapsui.Projections.SphericalMercator.FromLonLat(c.X, c.Y);
                    return new Coordinate(p.x, p.y);
                }).ToArray();

                var projectedPolygon = polyObj.Geometry.Factory.CreatePolygon(projectedCoords);
                string? nameAttr = polyObj.Attributes.ContainsKey("name") ? polyObj.Attributes["name"]?.ToString() : null;

                var feature = new GeometryFeature
                {
                    Geometry = projectedPolygon,
                    Styles = new List<IStyle>
                    {
                        VectorStyles.GetPolygonStyle(nameAttr),
                        VectorStyles.GetLabelStyle(nameAttr)
                    }
                };

                var vs = feature.Styles.OfType<VectorStyle>().FirstOrDefault();
                if (vs != null)
                {
                    OriginalFills[feature] = vs.Fill;
                }

                polygonFeatures.Add(feature);
            }
            _polygonLayer.Features = polygonFeatures;

            // 2. Точки пожарных частей
            _stationLayer.Enabled = fireStationsVisible;
            var stationFeatures = new List<GeometryFeature>();
            foreach (var station in data.FireStations)
            {
                var p = Mapsui.Projections.SphericalMercator.FromLonLat(station.Longitude, station.Latitude);
                var ntsPoint = new NetTopologySuite.Geometries.Point(p.x, p.y);

                var feature = new GeometryFeature
                {
                    Geometry = ntsPoint,
                    Styles = VectorStyles.GetPointStylesWithLabel(station.Name, station.Type)
                };

                feature["name"] = station.Name;

                OriginalStyles[feature] = new List<IStyle>(feature.Styles);
                stationFeatures.Add(feature);
            }
            _polygonLayer.Features = polygonFeatures;
            _stationLayer.Features = stationFeatures;

            UpdatePolygonStylesInternal();
            _mapControl.Refresh();
        }
        public GeometryFeature? FindFeatureAtPosition(MPoint worldPos, double screenRadius = 15)
        {
            if (!_stationLayer.Enabled) return null;

            double worldRadius = screenRadius * _mapControl.Map.Navigator.Viewport.Resolution;
            var searchRect = new MRect(
                worldPos.X - worldRadius, worldPos.Y - worldRadius,
                worldPos.X + worldRadius, worldPos.Y + worldRadius
            );

            return _stationLayer.GetFeatures(searchRect, 0).FirstOrDefault() as GeometryFeature;
        }
        public void HandleHover(MPoint worldPos)
        {
            var feature = FindFeatureAtPosition(worldPos);
            if (feature != null)
            {
                if (!OriginalStyles.TryGetValue(feature, out var originalStyles)) return;
                if (_hoverLayer.Enabled && _hoverLayer.Features.FirstOrDefault() == feature) return;

                var highlightedStyle = new List<IStyle>();
                foreach (var style in originalStyles)
                {
                    if (style is SymbolStyle symbolStyle)
                    {
                        Color originalColor = symbolStyle.Fill?.Color ?? Color.Red;

                        int r = Math.Min(255, (int)(originalColor.R * 2));
                        int g = Math.Min(255, (int)(originalColor.G * 2));
                        int b = Math.Min(255, (int)(originalColor.B * 2));

                        highlightedStyle.Add(new SymbolStyle
                        {
                            SymbolType = symbolStyle.SymbolType,
                            Fill = new Brush(new Color(r, g, b, originalColor.A)),
                            Outline = symbolStyle.Outline,
                            SymbolScale = symbolStyle.SymbolScale * 1.15f
                        });
                    }
                    else if (style is LabelStyle labelStyle)
                    {
                        highlightedStyle.Add(labelStyle);
                    }
                }

                _hoverLayer.Features = new List<GeometryFeature> { feature };
                var styleCollection = new StyleCollection();
                foreach (var s in highlightedStyle) styleCollection.Styles.Add(s);

                _hoverLayer.Style = styleCollection;
                _hoverLayer.Enabled = true;
                _mapControl.Refresh();
            }
            else
            {
                ClearHover();
            }
        }
        public void ClearHover()
        {
            if (_hoverLayer.Enabled)
            {
                _hoverLayer.Enabled = false;
                _hoverLayer.Features = Enumerable.Empty<IFeature>();
                _mapControl.Refresh();
            }
        }
        public void SetPolygonStyles(bool fillEnabled, double borderWidth)
        {
            _polygonFillEnabled = fillEnabled;
            _polygonBorderWidth = borderWidth;
            UpdatePolygonStylesInternal();
        }
        private void UpdatePolygonStylesInternal()
        {
            if (_polygonLayer.Features == null) return;

            foreach (var feature in _polygonLayer.Features.Cast<GeometryFeature>())
            {
                var vs = feature.Styles.OfType<VectorStyle>().FirstOrDefault();
                if (vs == null) continue;

                vs.Fill = _polygonFillEnabled
                    ? (OriginalFills.TryGetValue(feature, out var fill) ? (Brush?)fill : vs.Fill)
                    : null;

                if (vs.Outline != null)
                {
                    vs.Outline.Width = (float)_polygonBorderWidth;
                    vs.Outline.PenStyle = PenStyle.Solid;
                }
            }
            _mapControl.Refresh();
        }
        public void SetLayerVisibility(string layerName, bool visible)
        {
            var layer = _mapControl.Map.Layers.FirstOrDefault(l => l.Name == layerName);
            if (layer != null)
            {
                layer.Enabled = visible;
                _mapControl.Refresh();
            }
        }
        public void ClearAllGraphics()
        {
            _hoverLayer.Enabled = false;
            _hoverLayer.Features = Enumerable.Empty<IFeature>();
            _polygonLayer.Features = Enumerable.Empty<IFeature>();
            _stationLayer.Features = Enumerable.Empty<IFeature>();
            _mapControl.Refresh();
        }
    }
}
