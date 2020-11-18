using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using WebApplication.Models;
using WebApplication.ViewModels.Filters;

namespace WebApplication.ViewModels.Entities
{
    public class TimetablesViewModel : IEntitiesViewModel<Timetable>
    {
        [Display(Name = "Timetables")]
        public IEnumerable<Timetable> Entities { get; set; }
        [Display(Name = "Timetable")]
        public Timetable Entity { get; set; }
        public IEnumerable<string> ShowsSelectList { get; set; }
        public IEnumerable<string> StaffSelectList { get; set; }

        [Display(Name = "Show")]
        public string ShowName { get; set; }
        [Display(Name = "Staff")]
        public string StaffName { get; set; }


        public PageViewModel PageViewModel { get; set; }
        public DeleteViewModel DeleteViewModel { get; set; }
        public SortViewModel SortViewModel { get; set; }
        public TimetablesFilterViewModel TimetablesFilterViewModel { get; set; }

    }
}
