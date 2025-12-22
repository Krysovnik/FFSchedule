using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace FFSchedule.Model
{
    public class RankBudgetAllLocation
    {
        [Key]
        public int Id { get; set; }
        public int RankId { get; set; }
        public int EquipmentQuantity { get; set; }
        public string TotalAmount { get; set; }
        public virtual Rank Rank { get; set; }
    }
}
