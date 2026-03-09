using System;
using System.Collections.Generic;

namespace FFSchedule.Models;

public partial class RankBudgetAllocation
{
    public int RbaId { get; set; }

    public int? RId { get; set; }

    public int? EtId { get; set; }

    public int? RbaTypeTotal { get; set; }

    public virtual EquipmentType? Et { get; set; }

    public virtual Rank? RIdNavigation { get; set; }
}
