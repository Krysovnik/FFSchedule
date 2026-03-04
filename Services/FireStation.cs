using System;
using System.Collections.Generic;
using System.Text;

namespace FFSchedule.Services
{
    public class FireStation
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string District { get; set; }
        public string Type { get; set; }
        public string Phone { get; set; }

        public double Latitude { get; set; }  
        public double Longitude { get; set; } 
    }
}
