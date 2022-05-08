using System.ComponentModel.DataAnnotations; // for [Key]
using System.ComponentModel.DataAnnotations.Schema; // for Identity

namespace Hotels.Database.Tables
{
    public class Room
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        public int NumberOfPersons { get; set; }

        public Hotel Hotel { get; set; }
        public List<Reservation> Reservations { get; set; }
        public List<CharacteristicOfRoom> CharacteristicOfRooms { get; set; }
    }
}
