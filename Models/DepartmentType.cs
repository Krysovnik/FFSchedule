using System;
using System.Collections.Generic;

namespace FFSchedule.Models;

public partial class DepartmentType
{
    public int DtId { get; set; }

    public string? DtName { get; set; }

    public virtual ICollection<Department> Departments { get; set; } = new List<Department>();
}
