using System;
using System.Collections.Generic;

namespace FFSchedule.Models;

public partial class DepartamentEquipmentSummary
{
    public int DesId { get; set; }

    public int? DptId { get; set; }

    public int? DesQuantity { get; set; }

    public int? EtId { get; set; }

    public virtual Department? Dpt { get; set; }

    public virtual EquipmentType? Et { get; set; }
}
