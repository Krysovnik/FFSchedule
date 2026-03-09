using System;
using System.Collections.Generic;

namespace FFSchedule.Models;

public partial class TypesOfLocality
{
    public int TolId { get; set; }

    public string? TolName { get; set; }

    public virtual ICollection<Settlement> Settlements { get; set; } = new List<Settlement>();
}
