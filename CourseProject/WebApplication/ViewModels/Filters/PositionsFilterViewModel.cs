using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication.ViewModels.Filters
{
    public class PositionsFilterViewModel
    {
        [Display(Name = "Position")]
        public string PositionName { get; set; }
    }
}
