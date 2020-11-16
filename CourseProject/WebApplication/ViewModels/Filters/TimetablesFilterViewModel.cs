using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication.ViewModels.Filters
{
    public class TimetablesFilterViewModel
    {
        [Display(Name = "Day of week")]
        public int DayOfWeek { get; set; }

        [Display(Name = "Month")]
        public int Month { get; set; }

        [Display(Name = "Year")]
        public int Year { get; set; }

        [Display(Name = "Show")]
        public int Show { get; set; }

        [Display(Name = "Start time")]
        public TimeSpan StartTime { get; set; }

        [Display(Name = "End time")]
        public TimeSpan EndTime { get; set; }

        [Display(Name = "Staff")]
        public int? Staff { get; set; }
    }
}
