using System;
using System.Collections.Generic;

namespace FFSchedule.Models;

public partial class DepartmentVillageCouncilTime
{
    public int DptId { get; set; }

    public int VcId { get; set; }

    public int? TravelTimeMinutes { get; set; }

    public virtual Department Dpt { get; set; } = null!;

    public virtual VillageCouncil Vc { get; set; } = null!;
}
