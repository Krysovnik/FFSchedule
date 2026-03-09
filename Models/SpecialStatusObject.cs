using System;
using System.Collections.Generic;

namespace FFSchedule.Models;

public partial class SpecialStatusObject
{
    public int SsoId { get; set; }

    public string? SsoName { get; set; }

    public string? SsoAddress { get; set; }

    public int? RId { get; set; }

    public int? StobId { get; set; }

    public virtual Rank? RIdNavigation { get; set; }

    public virtual StatusesOfSpeObj? Stob { get; set; }
}
