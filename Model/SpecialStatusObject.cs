using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace FFSchedule.Model
{
    public class SpecialStatusObject
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public int RankId { get; set; }
        public TimeSpan PeoplePresenceTime { get; set; }
        public string MassScale { get; set; }
        public virtual Rank Rank { get; set; }
    }
}
