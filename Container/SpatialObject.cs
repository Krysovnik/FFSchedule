using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace FFSchedule.Models
{
    public class SpatialObject
    {
        public Geometry Geometry { get; set; } = null!;
        public Dictionary<string, object> Attributes { get; set; } = new();
    }
}
