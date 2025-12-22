using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace FFSchedule.Model
{
    public class SettlementDepartmentDistance
    {
        [Key]
        public int Id { get; set; }
        public int SettlementId { get; set; }
        public int DepartmentId { get; set; }
        public decimal Distance { get; set; }
        public virtual Department Department { get; set; }
        public virtual Settlement Settlement { get; set; }
    }
}
