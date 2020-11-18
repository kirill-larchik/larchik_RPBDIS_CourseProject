using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication.ViewModels.Filters
{
    public class AppealsFilterViewModel
    {
        [Display(Name = "Full name")]
        public string FullName { get; set; }

        [Display(Name = "Organization")]
        public string Organization { get; set; }

        [Display(Name = "Show")]
        public string ShowName { get; set; }

        [Display(Name = "Goal request")]
        public string GoalRequest { get; set; }
    }
}
