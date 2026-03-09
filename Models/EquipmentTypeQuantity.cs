using System;
using System.Collections.Generic;

namespace FFSchedule.Models;

public partial class EquipmentTypeQuantity
{
    public int EtqId { get; set; }

    public int? SeId { get; set; }

    public int? DptId { get; set; }

    public int? EtId { get; set; }

    public int? EtqQuantity { get; set; }

    public string? EtqTime { get; set; }

    public virtual Department? Dpt { get; set; }

    public virtual EquipmentType? Et { get; set; }

    public virtual Settlement? Se { get; set; }
}
