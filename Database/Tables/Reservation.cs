using System.ComponentModel.DataAnnotations; // for [Key]
using System.ComponentModel.DataAnnotations.Schema; // for Identity

namespace Hotels.Database.Tables
{
    public class Reservation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public Guid ReservationNumber { get; set; }
        public DateTime BeginDate { get; set; }
        public DateTime EndDate { get; set; }
        public int RoomId { get; set; }
        public Room Room { get; set; }
        
    }
}
