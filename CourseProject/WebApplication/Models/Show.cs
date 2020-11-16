using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication.Models
{
    public partial class Show
    {
        public Show()
        {
            Timetables = new HashSet<Timetable>();
        }

        [Required]
        public int ShowId { get; set; }

        [Required]
        [Display(Name = "Name")]
        [StringLength(32, MinimumLength = 4, ErrorMessage = "Name length must be between 4 and 32 symbols.")]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Release date")]
        [DataType(DataType.Date)]
        public DateTime ReleaseDate { get; set; }

        [Required]
        [Display(Name = "Duration")]
        [DataType(DataType.Time)]
        [Range(typeof(TimeSpan), "00:15:00", "03:00:00")]
        public TimeSpan Duration { get; set; }

        [Required]
        [Display(Name = "Mark")]
        [Range(1, 10)]
        public int Mark { get; set; }

        [Required]
        [Display(Name = "Mark month")]
        [Range(1, 12)]
        public int MarkMonth { get; set; }

        [Required]
        [Display(Name = "Mark year")]
        public int MarkYear { get; set; }

        [Display(Name = "Genre")]
        public int GenreId { get; set; }

        [Required]
        [Display(Name = "Description")]
        [StringLength(32, MinimumLength = 8, ErrorMessage = "Name length must be between 8 and 32 symbols.")]
        public string Description { get; set; }

        [Display(Name = "Genre")]
        public Genre Genre { get; set; }
        public ICollection<Timetable> Timetables { get; set; }
    }
}
