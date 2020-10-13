using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TvChannelOperations.Models;

namespace TvChannelOperations.Data
{
    public class TvChannelsContext : DbContext
    {
        public TvChannelsContext(DbContextOptions<TvChannelsContext> options)
            : base(options)
        {

        }

        public DbSet<Appeal> Appeals { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<Position> Positions { get; set; }
        public DbSet<Show> Shows { get; set; }
        public DbSet<Staff> Staff { get; set; }
        public DbSet<Timetable> Timetables { get; set; }
    }
}
