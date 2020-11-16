using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using WebApplication.Models;
using WebApplication.ViewModels.Filters;

namespace WebApplication.ViewModels.Entities
{
    public class ShowsViewModel : IEntitiesViewModel<Show>
    {
        [Display(Name = "Shows")]
        public IEnumerable<Show> Entities { get; set; }
        [Display(Name = "Show")]
        public Show Entity { get; set; }
        [Display(Name = "Genres")]
        public IEnumerable<Genre> SelectList { get; set; }

        [Display(Name = "Genre")]
        public string GenreName { get; set; }


        public PageViewModel PageViewModel { get; set; }
        public DeleteViewModel DeleteViewModel { get; set; }
        public SortViewModel SortViewModel { get; set; }
        public ShowsFilterViewModel ShowsFilterViewModel { get; set; }

    }
}
