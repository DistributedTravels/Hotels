using Microsoft.EntityFrameworkCore;
using Hotels.Database.Tables;

namespace Hotels.Database
{
    public class HotelContext : DbContext
    {
        public HotelContext(DbContextOptions<HotelContext> options) : base(options) { } // service creation constructor
        public HotelContext() : base() { }

        public virtual DbSet<Hotel> Hotels { get; set; }
        public virtual DbSet<Room> Rooms { get; set; }
        public virtual DbSet<Reservation> Reservations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Hotel>().ToTable("Hotel");
            modelBuilder.Entity<Room>().ToTable("Room");
            modelBuilder.Entity<Reservation>().ToTable("Reservation");
        }
    }
}
