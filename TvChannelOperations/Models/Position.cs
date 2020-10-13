using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TvChannelOperations.Models
{
    public class Position
    {
        public int PositionId { get; set; }
        public string Name { get; set; }
        public ICollection<Staff> Staff { get; set; }
    }
}
