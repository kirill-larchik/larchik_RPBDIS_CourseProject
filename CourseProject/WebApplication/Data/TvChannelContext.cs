using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using WebApplication.Models;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace WebApplication.Data
{
    public partial class TvChannelContext : DbContext
    {
        public TvChannelContext(DbContextOptions<TvChannelContext> options)
            : base(options) 
        {
            Database.EnsureCreated();
        }

        public virtual DbSet<Genre> Genres { get; set; }
        public virtual DbSet<Show> Shows { get; set; }
        public virtual DbSet<Timetable> Timetables { get; set; }
    }
}
