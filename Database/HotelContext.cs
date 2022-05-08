using Microsoft.EntityFrameworkCore;
using Hotels.Database.Tables;

namespace Hotels.Database
{
    public class HotelContext : DbContext
    {
        public HotelContext(DbContextOptions<HotelContext> options) : base(options) { } // service creation constructor
        public HotelContext() : base() { }

        public static string ConnString { get; set; }
        public virtual DbSet<Country> Countries { get; set; }
        public virtual DbSet<Hotel> Hotels { get; set; }
        public virtual DbSet<Attraction> Attractions { get; set; }
        public virtual DbSet<AttractionInHotel> AttractionInHotel { get; set; }
        public virtual DbSet<Room> Rooms { get; set; }
        public virtual DbSet<Reservation> Reservations { get; set; }
        public virtual DbSet<Characteristic> Characteristics { get; set; }
        public virtual DbSet<CharacteristicOfRoom> CharacteristicOfRooms { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Hotel>().ToTable("Hotel");
            modelBuilder.Entity<Country>().ToTable("Country");
            modelBuilder.Entity<Attraction>().ToTable("Attraction");
            modelBuilder.Entity<AttractionInHotel>().ToTable("AttractionInHotel");
            modelBuilder.Entity<Room>().ToTable("Room");
            modelBuilder.Entity<Reservation>().ToTable("Reservation");
            modelBuilder.Entity<Characteristic>().ToTable("Characteristic");
            modelBuilder.Entity<CharacteristicOfRoom>().ToTable("CharacteristicOfRoom");
        }
    }
}
