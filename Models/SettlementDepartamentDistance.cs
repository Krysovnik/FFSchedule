using System;
using System.Collections.Generic;

namespace FFSchedule.Models;

public partial class SettlementDepartamentDistance
{
    public int SddId { get; set; }

    public int? SeId { get; set; }

    public int? DptId { get; set; }

    public double? SddDistanceKm { get; set; }

    public virtual Department? Dpt { get; set; }

    public virtual Settlement? Se { get; set; }
}
