using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace FFSchedule.Model
{
    public class VillageCouncil
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
