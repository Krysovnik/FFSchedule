using Mapsui.Nts;
using System;
using System.Collections.Generic;
using System.Text;

namespace FFSchedule.Container
{
    public class CachedRouteData
    {
        public List<RouteResult> RouteResults { get; set; } = new();
        public List<GeometryFeature> Features { get; set; } = new();
        public HashSet<FireStation> UsedStations { get; set; } = new();
    }
}
