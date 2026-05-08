using System;
using System.Collections.Generic;

namespace FFSchedule.Models;

public partial class Settlement
{
    public int SeId { get; set; }

    public string? SeName { get; set; }

    public int? VcId { get; set; }

    public int? TolId { get; set; }

    public int? Optkp { get; set; }

    public virtual ICollection<EquipmentTypeQuantity> EquipmentTypeQuantities { get; set; } = new List<EquipmentTypeQuantity>();

    public virtual ICollection<SettlementDepartamentDistance> SettlementDepartamentDistances { get; set; } = new List<SettlementDepartamentDistance>();

    public virtual ICollection<SettlementMainDepartament> SettlementMainDepartaments { get; set; } = new List<SettlementMainDepartament>();

    public virtual TypesOfLocality? Tol { get; set; }

    public virtual VillageCouncil? Vc { get; set; }
}
