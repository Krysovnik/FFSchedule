using System;
using System.Collections.Generic;

namespace FFSchedule.Models;

public partial class EquipmentType
{
    public int EtId { get; set; }

    public string? EtName { get; set; }

    public virtual ICollection<DepartamentEquipmentSummary> DepartamentEquipmentSummaries { get; set; } = new List<DepartamentEquipmentSummary>();

    public virtual ICollection<EquipmentTypeQuantity> EquipmentTypeQuantities { get; set; } = new List<EquipmentTypeQuantity>();

    public virtual ICollection<RankBudgetAllocation> RankBudgetAllocations { get; set; } = new List<RankBudgetAllocation>();
}
