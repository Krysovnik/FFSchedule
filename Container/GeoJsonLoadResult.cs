using System;
using System.Collections.Generic;
using System.Text;
using FFSchedule.Models;

namespace FFSchedule.Container
{
    public class GeoJsonLoadResult
    {
        public List<SpatialObject> Polygons { get; set; } = new();
        public List<FireStation> FireStations { get; set; } = new();
    }
}
