using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TvChannelOperations.Models
{
    public class Genre
    {
        public int GenreId { get; set; }
        public string GenreName { get; set; }
        public string GenreDescription { get; set; }
        public ICollection<Show> Shows { get; set; }
    }
}
