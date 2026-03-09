using System;
using System.Collections.Generic;

namespace FFSchedule.Models;

public partial class StatusesOfSpeObj
{
    public int StobId { get; set; }

    public string? StobName { get; set; }

    public virtual ICollection<SpecialStatusObject> SpecialStatusObjects { get; set; } = new List<SpecialStatusObject>();
}
