using System;
using System.Collections.Generic;

namespace FFSchedule.Models;

public partial class Rank
{
    public int RId { get; set; }

    public string? RNumber { get; set; }

    public string? RTotalEquipmentQuantity { get; set; }

    public virtual ICollection<RankBudgetAllocation> RankBudgetAllocations { get; set; } = new List<RankBudgetAllocation>();

    public virtual ICollection<SpecialStatusObject> SpecialStatusObjects { get; set; } = new List<SpecialStatusObject>();
}
