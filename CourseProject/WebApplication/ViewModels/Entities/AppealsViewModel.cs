using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using WebApplication.Models;
using WebApplication.ViewModels.Filters;

namespace WebApplication.ViewModels.Entities
{
    public class AppealsViewModel : IEntitiesViewModel<Appeal>
    {
        [Display(Name = "Appeals")]
        public IEnumerable<Appeal> Entities { get; set; }
        [Display(Name = "Appeal")]
        public Appeal Entity { get; set; }
        public IEnumerable<Show> SelectList { get; set; }

        [Display(Name = "Show")]
        public string ShowName { get; set; }


        public PageViewModel PageViewModel { get; set; }
        public DeleteViewModel DeleteViewModel { get; set; }
        public SortViewModel SortViewModel { get; set; }
        public AppealsFilterViewModel AppealsFilterViewModel { get; set; }
    }
}
