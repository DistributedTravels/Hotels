using Microsoft.EntityFrameworkCore;
using Hotels.Database.Tables;

namespace Hotels.Database
{
    public class HotelContext : DbContext
    {
        public HotelContext(DbContextOptions<HotelContext> options) : base(options) { } // service creation constructor
        public HotelContext() : base() { }
        public virtual DbSet<Country> Countries { get; set; }
        public virtual DbSet<Hotel> Hotels { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Hotel>().ToTable("Hotel");
            modelBuilder.Entity<Country>().ToTable("Country");
        }
    }
}
