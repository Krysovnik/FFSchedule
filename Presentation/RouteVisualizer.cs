using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.UI.Wpf;
using FFSchedule.Container;
using NetTopologySuite.Geometries;

namespace FFSchedule.Presentation
{
    public class RouteVisualizer
    {
        private readonly MapControl _mapControl;
        private const string ROUTE_LAYER_NAME = "SHARED_ROUTE_LAYER";
        private MemoryLayer? _routeLayer;

        public RouteVisualizer(MapControl mapControl)
        {
            _mapControl = mapControl;
        }

        public void InitializeLayers(Map map)
        {
            var old = map.Layers.FirstOrDefault(l => l.Name == ROUTE_LAYER_NAME);
            if (old != null) map.Layers.Remove(old);

            _routeLayer = new MemoryLayer
            {
                Name = ROUTE_LAYER_NAME,
                MaxVisible = 80,
                Style = null
            };

            map.Layers.Add(_routeLayer);
        }

        public void ClearRouteGraphics()
        {
            if (_routeLayer == null) return;

            lock (_routeLayer.Features)
            {
                _routeLayer.Features = new List<GeometryFeature>();
            }
            _mapControl.Refresh();
        }

        public void RenderRoutes(List<RouteResult> results)
        {
            if (_routeLayer == null) return;

            var tempFeatures = new List<GeometryFeature>();

            for (int i = 0; i < results.Count; i++)
            {
                var res = results[i];
                if (!res.Success || res.RawCoordinates == null) continue;

                var color = new Color(255, 82, 82);
                PrepareRouteFeature(res.RawCoordinates, res.Distance, res.Duration, i, color, false, tempFeatures);
            }

            lock (_routeLayer.Features)
            {
                _routeLayer.Features = tempFeatures;
            }
            _mapControl.Refresh();
        }

        public void RenderAdditionalRoute(RouteResult additionalResult, int globalIndex)
        {
            if (_routeLayer == null || !additionalResult.Success || additionalResult.RawCoordinates == null) return;

            lock (_routeLayer.Features)
            {
                var currentFeatures = _routeLayer.Features.Cast<GeometryFeature>().ToList();

                var color = new Color(76, 175, 80);
                PrepareRouteFeature(additionalResult.RawCoordinates, additionalResult.Distance, additionalResult.Duration, globalIndex, color, true, currentFeatures);

                _routeLayer.Features = currentFeatures;
            }
            _mapControl.Refresh();
        }

        private void PrepareRouteFeature(List<CoordinateDto> rawCoords, double distance, double duration, int index, Color color, bool isDash, List<GeometryFeature> outputFeatures)
        {
            var projectedCoords = rawCoords.Select(c =>
            {
                var p = SphericalMercator.FromLonLat(c.X, c.Y);
                return new Coordinate(p.x, p.y);
            }).ToArray();

            var routeFeature = new GeometryFeature { Geometry = new LineString(projectedCoords) };

            var pen = new Pen(color, 2f);
            if (isDash)
            {
                pen.PenStyle = PenStyle.Dash;
            }

            routeFeature.Styles.Add(new VectorStyle
            {
                Outline = pen,
                MaxVisible = 80
            });

            string prefix = isDash ? "Доп. #" : "#";
            string infoText = $"{prefix}{index + 1}: {distance / 1000:F1} км, {duration / 60:F0} мин";

            routeFeature.Styles.Add(new LabelStyle
            {
                Text = infoText,
                Font = new Font { Size = 10, Bold = true, FontFamily = "Arial" },
                ForeColor = Color.Black,
                CollisionDetection = true,
                MaxWidth = 100,
                MaxVisible = 80
            });

            outputFeatures.Add(routeFeature);
        }
    }
}
