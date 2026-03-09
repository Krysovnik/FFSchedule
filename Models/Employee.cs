using System;
using System.Collections.Generic;

namespace FFSchedule.Models;

public partial class Employee
{
    public int EmId { get; set; }

    public string? EmLogin { get; set; }

    public string? EmPassword { get; set; }

    public string? EmFio { get; set; }

    public int? RoId { get; set; }

    public virtual Role? Ro { get; set; }
}
