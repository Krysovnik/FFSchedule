using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace FFSchedule.Model
{
    public class CorrectionHistory
    {
        [Key]
        public int Id { get; set; }
        public DateTime date { get; set; }
        public string description { get; set; }
        public int EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }
    }
}
