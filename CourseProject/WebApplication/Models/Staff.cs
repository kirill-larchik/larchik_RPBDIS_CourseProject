using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication.Models
{
    public class Staff
    {
        public Staff()
        {
            Timetables = new HashSet<Timetable>();
        }

        public int StaffId { get; set; }

        [Required]
        [Display(Name = "Full name")]
        [StringLength(64, MinimumLength = 8, ErrorMessage = "Full name length must be between 8 and 64 symbols.")]
        public string FullName { get; set; }

        [Display(Name = "Position")]
        public int PositionId { get; set; }

        public Position Position { get; set; }

        public ICollection<Timetable> Timetables { get; set; }
    }
}
