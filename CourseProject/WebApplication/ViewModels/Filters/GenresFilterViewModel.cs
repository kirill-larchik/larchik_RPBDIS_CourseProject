using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication.ViewModels.Filters
{
    public class GenresFilterViewModel
    {
        [Display(Name = "Genre")]
        public string GenreName { get; set; } = null!;

        [Display(Name = "Description")]
        public string GenreDescription { get; set; } = null!;
    }
}
