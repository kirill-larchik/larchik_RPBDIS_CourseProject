using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebApplication.Models
{
    public partial class Timetable
    {
        public int TimetableId { get; set; }

        [Required]
        [Display(Name = "Day of week")]
        [Range(1, 7)]
        public int DayOfWeek { get; set; }

        [Required]
        [Display(Name = "Month")]
        [Range(1, 12)]
        public int Month { get; set; }

        [Required]
        [Display(Name = "Year")]
        public int Year { get; set; }

        [Display(Name = "Show")]
        public int ShowId { get; set; }

        [Required]
        [Display(Name = "Start time")]
        [DataType(DataType.Time)]
        [Range(typeof(TimeSpan), "00:00:00", "20:59:59")]
        public TimeSpan StartTime { get; set; }

        [Display(Name = "End time")]
        public TimeSpan? EndTime { get; set; }

        [Display(Name = "Staff")]
        public int StaffId { get; set; }

        public Show Show { get; set; }
        public Staff Staff { get; set; }
    }
}
