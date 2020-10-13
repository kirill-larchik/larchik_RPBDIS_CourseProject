using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TvChannelOperations.Models
{
    public class Timetable
    {
        public int TimetableId { get; set; }
        public int DayOfWeek { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public int ShowId { get; set; }
        public Show Show { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int StaffId { get; set; }
        public Staff Staff { get; set; }
    }
}
