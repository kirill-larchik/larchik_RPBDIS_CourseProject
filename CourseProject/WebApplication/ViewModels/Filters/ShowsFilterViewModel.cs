using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication.ViewModels.Filters
{
    public class ShowsFilterViewModel
    {
        [Display(Name = "Show")]
        public string Name { get; set; }

        [Display(Name = "Release date")]
        [DataType(DataType.Date)]
        public DateTime ReleaseDate { get; set; }

        [Display(Name = "Duration (Less)")]
        public TimeSpan Duration { get; set; }

        [Display(Name = "Mark")]
        public int Mark { get; set; }

        [Display(Name = "Mark month")]
        public int MarkMonth { get; set; }

        [Display(Name = "Mark year")]
        public int MarkYear { get; set; }

        [Display(Name = "Genre")]
        public string GenreName { get; set; }

        [Display(Name = "Description")]
        public string Description { get; set; }
    }
}
