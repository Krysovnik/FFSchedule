using System;
using System.Collections.Generic;
using System.Text;

namespace FFSchedule.DepartamentWindows.JsonModels
{
    public class GeoJson
    {
        public string? type { get; set; }
        public string? name { get; set; }
        public Crs? crs { get; set; }
        public List<Feature>? features { get; set; }
    }

    public class Crs
    {
        public string? type { get; set; }
        public CrsProps? properties { get; set; }
    }

    public class CrsProps
    {
        public string? name { get; set; }
    }

    public class Feature
    {
        public string? type { get; set; }
        public Dictionary<string, object>? properties { get; set; }
        public Geometry? geometry { get; set; }
    }

    public class Geometry
    {
        public string? type { get; set; }
        public double[]? coordinates { get; set; }
    }
}
