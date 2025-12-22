using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace FFSchedule.Model
{
    public class Department
    {
        [Key]
        public int Id { get; set; }
        public string Number { get; set; }
        public int DepartmentTypeId { get; set; }
        public string address { get; set; }
        public string PhoneNumber { get; set; }
        public virtual DepartmentType DepartmentType { get; set; }
    }
}
