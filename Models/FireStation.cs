using System;
using System.Collections.Generic;
using System.Text;

namespace FFSchedule.Models
{
    public class FireStation
    {
        public string? Name { get; set; }
        public string? Address { get; set; }
        public string? District { get; set; }
        public string? Type { get; set; }
        public string? Phone { get; set; }

        public double Latitude { get; set; }  
        public double Longitude { get; set; }
        public override string ToString() => $"{Name} ({Address})";
        public int EquipmentCapacity => Type?.ToLower() == "псч" ? 2 : 1;
    }
}

