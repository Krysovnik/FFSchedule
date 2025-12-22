using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace FFSchedule.Model
{
    public class DepartmentEquipment
    {
        [Key]
        public int Id { get; set; }
        public int DepartmentId { get; set; }
        public int Quantity { get; set; }
        public int EquipmentId { get; set; }
        public virtual Department Department { get; set; }
        public virtual Equipment Equipment { get; set; }
    }
}
