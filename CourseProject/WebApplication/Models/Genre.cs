using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebApplication.Models
{
    public partial class Genre
    {
        public Genre()
        {
            Shows = new HashSet<Show>();
        }

        public int GenreId { get; set; }

        [Required]
        [Display(Name = "Genre")]
        [StringLength(16, MinimumLength = 4, ErrorMessage = "Name length must be between 4 and 16 symbols.")]
        public string GenreName { get; set; }

        [Required]
        [Display(Name = "Description")]
        [StringLength(32, MinimumLength = 8, ErrorMessage = "Description length must be between 8 and 32 symbols.")]
        public string GenreDescription { get; set; }

        public ICollection<Show> Shows { get; set; }
    }
}
