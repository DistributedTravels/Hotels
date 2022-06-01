using System.ComponentModel.DataAnnotations; // for [Key]
using System.ComponentModel.DataAnnotations.Schema; // for Identity

namespace Hotels.Database.Tables
{
    public class Room
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Type { get; set; }

        public int HotelId { get; set; }
        public Hotel Hotel { get; set; }
        public List<Reservation> Reservations { get; set; }

        public bool Removed { get; set; } = false;
    }
}
