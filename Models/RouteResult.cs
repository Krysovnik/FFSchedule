using System;
using System.Collections.Generic;
using System.Text;

namespace FFSchedule.Models
{
    public class RouteResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public double Distance { get; set; }
        public double Duration { get; set; }
    }
}

