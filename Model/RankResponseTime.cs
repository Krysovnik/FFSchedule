using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace FFSchedule.Model
{
    public class RankResponseTime
    {
        [Key]
        public int Id { get; set; }
        public int RankId { get; set; }
        public int DepartmentEquipmentSummaryId { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public virtual Rank Rank { get; set; }
        public virtual DepartmentEquipmentSummary DepartmentEquipmentSummary { get; set; }
    }
}
