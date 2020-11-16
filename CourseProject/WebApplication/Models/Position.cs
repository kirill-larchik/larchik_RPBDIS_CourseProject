using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication.Models
{
    public class Position
    {
        public Position()
        {
            Staffs = new HashSet<Staff>();
        }

        public int PositionId { get; set; }

        [Required]
        [Display(Name = "Position")]
        [StringLength(16, MinimumLength = 4, ErrorMessage = "Full name length must be between 4 and 16 symbols.")]
        public string Name { get; set; }

        public ICollection<Staff> Staffs { get; set; }
    }
}
