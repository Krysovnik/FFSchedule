using System;
using System.Collections.Generic;

namespace FFSchedule.Models;

public partial class SettlementMainDepartament
{
    public int SmdId { get; set; }

    public int? SeId { get; set; }

    public int? DptId { get; set; }

    public virtual Department? Dpt { get; set; }

    public virtual Settlement? Se { get; set; }
}
