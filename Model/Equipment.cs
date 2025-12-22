using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace FFSchedule.Model
{
    public class Equipment
    {
        [Key]
        public int Id { get; set; }
        public string LicensePlate { get; set; }
        public int EquipmentTypeId { get; set; }
        public virtual EquipmentType EquipmentType { get; set; }
    }
}
