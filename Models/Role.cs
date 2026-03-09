using System;
using System.Collections.Generic;

namespace FFSchedule.Models;

public partial class Role
{
    public int RoId { get; set; }

    public string? RoName { get; set; }

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
