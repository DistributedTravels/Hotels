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
        public virtual DbSet<Attraction> Attractions { get; set; }
        public virtual DbSet<AttractionInHotel> AttractionInHotel { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Hotel>().ToTable("Hotel");
            modelBuilder.Entity<Country>().ToTable("Country");
            modelBuilder.Entity<Attraction>().ToTable("Attraction");
            modelBuilder.Entity<AttractionInHotel>().ToTable("AttractionInHotel");
        }
    }
}
