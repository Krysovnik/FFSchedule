using System;
using System.Collections.Generic;

namespace FFSchedule.Models;

public partial class VillageCouncil
{
    public int VcId { get; set; }

    public string? VcName { get; set; }

    public virtual ICollection<DepartmentVillageCouncilTime> DepartmentVillageCouncilTimes { get; set; } = new List<DepartmentVillageCouncilTime>();

    public virtual ICollection<Settlement> Settlements { get; set; } = new List<Settlement>();
}
