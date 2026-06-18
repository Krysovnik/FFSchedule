using System;
using System.Collections.Generic;
using System.Text;

namespace FFSchedule.Container
{
    public class GeoJsonLoadResult
    {
        public List<Mapsui.Nts.GeometryFeature> PolygonFeatures { get; set; } = new();
        public List<Mapsui.Nts.GeometryFeature> PointFeatures { get; set; } = new();
        public List<FireStation> FireStations { get; set; } = new();
    }
}
