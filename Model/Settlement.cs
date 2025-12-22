using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace FFSchedule.Model
{
    public class Settlement
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }   
        public int VillageCouncilId { get; set; }
        public int TypesOfLocalityId { get; set; }
        public virtual VillageCouncil VillageCouncil { get; set; }
        public virtual TypesOfLocality TypesOfLocality { get; set; }
    }
}
