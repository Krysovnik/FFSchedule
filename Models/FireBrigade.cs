using System;
using System.Collections.Generic;

namespace FFSchedule.Models;

public partial class FireBrigade
{
    public int FbId { get; set; }

    public string? FbName { get; set; }

    public virtual ICollection<Department> Departments { get; set; } = new List<Department>();
}
