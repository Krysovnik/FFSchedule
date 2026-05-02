using System;
using System.Collections.Generic;

namespace FFSchedule.Models;

public partial class Department
{
    public int DptId { get; set; }

    public string? DptName { get; set; }

    public int? DtId { get; set; }

    public string? DptAddress { get; set; }

    public string? DptPhoneNum { get; set; }

    public int? FbId { get; set; }

    public string? DptShort { get; set; }

    public int? DptFiretrucks { get; set; }

    public int? DptHasLadder { get; set; }

    public virtual ICollection<DepartamentEquipmentSummary> DepartamentEquipmentSummaries { get; set; } = new List<DepartamentEquipmentSummary>();

    public virtual DepartmentType? Dt { get; set; }

    public virtual ICollection<EquipmentTypeQuantity> EquipmentTypeQuantities { get; set; } = new List<EquipmentTypeQuantity>();

    public virtual FireBrigade? Fb { get; set; }

    public virtual ICollection<SettlementDepartamentDistance> SettlementDepartamentDistances { get; set; } = new List<SettlementDepartamentDistance>();

    public virtual ICollection<SettlementMainDepartament> SettlementMainDepartaments { get; set; } = new List<SettlementMainDepartament>();
}
