using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication.Models
{
    public class Appeal
    {
        public int AppealId { get; set; }

        [Required]
        [Display(Name = "Full name")]
        [StringLength(64, MinimumLength = 8, ErrorMessage = "Full name length must be between 8 and 64 symbols.")]
        public string FullName { get; set; }

        [Required]
        [Display(Name = "Organization")]
        [StringLength(64, MinimumLength = 8, ErrorMessage = "Organization length must be between 8 and 64 symbols.")]
        public string Organization { get; set; }

        [Display(Name = "Show")]
        public int? ShowId { get; set; }

        [Required]
        [Display(Name = "Goal request")]
        [StringLength(128, MinimumLength = 8, ErrorMessage = "Goal request length must be between 8 and 64 symbols.")]
        public string GoalRequest { get; set; }

        public Show Show { get; set; }
    }
}
