using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TvChannelOperations.Models
{
    public class Appeal
    {
        public int AppealId { get; set; }
        public string FullName { get; set; }
        public string Organization { get; set; }
        public int ShowId { get; set; }
        public Show Show { get; set; }
        public string GoalRequest { get; set; } 
    }
}
