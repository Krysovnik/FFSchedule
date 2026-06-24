using System;
using System.Collections.Generic;
using System.Text;

namespace FFSchedule.Container
{
    public class RouteResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public double Distance { get; set; }
        public double Duration { get; set; }

        public FireStation? Station { get; set; }

        public List<CoordinateDto>? RawCoordinates { get; set; }
    }
}

