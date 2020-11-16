using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using WebApplication.Models;
using WebApplication.ViewModels.Filters;

namespace WebApplication.ViewModels.Entities
{
    public class StaffViewModel : IEntitiesViewModel<Staff>
    {
        [Display(Name = "Staff")]
        public IEnumerable<Staff> Entities { get; set; }
        [Display(Name = "Employee")]
        public Staff Entity { get; set; }
        [Display(Name = "Positions")]
        public IEnumerable<Position> SelectList { get; set; }
        [Display(Name = "Position")]
        public string PositionName { get; set; }


        public PageViewModel PageViewModel { get; set; }
        public DeleteViewModel DeleteViewModel { get; set; }
        public SortViewModel SortViewModel { get; set; }
        public StaffFilterViewModel StaffFilterViewModel { get; set; }
    }
}
