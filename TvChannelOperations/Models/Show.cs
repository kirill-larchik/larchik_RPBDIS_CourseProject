using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TvChannelOperations.Models
{
    public class Show
    {
        public int ShowId { get; set; }
        public string Name { get; set; }
        public DateTime ReleaseDate { get; set; }
        public TimeSpan Duration { get; set; }
        public int Mark { get; set; }
        public int MarkMonth { get; set; }
        public int MarkYear { get; set; }
        public int GenreId { get; set; }
        public Genre Genre { get; set; }
        public string Description { get; set; }
        public ICollection<Appeal> Appeals { get; set; }
        public ICollection<Timetable> Timetables { get; set; }
    }
}
