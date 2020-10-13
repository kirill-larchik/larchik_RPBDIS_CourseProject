using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TvChannelOperations.Models
{
    public class Staff
    {
        public int StaffId { get; set; }
        public string FullName { get; set; }
        public int PositionId { get; set; }
        public Position Position { get; set; }
        public ICollection<Timetable> Timetables { get; set; }
    }
}
