using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace FFSchedule.Model
{
    public class DepartmentEquipmentSummary
    {
        [Key]
        public int Id { get; set; }
        public int DepartmentId { get; set; } 
        public int EquipmentTypeId { get; set; }
        public int EquipmentQuantity { get; set; }
        public virtual Department Department { get; set; }
        public virtual EquipmentType EquipmentType { get; set; }
    }
}
