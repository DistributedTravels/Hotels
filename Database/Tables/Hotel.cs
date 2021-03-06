using System.ComponentModel.DataAnnotations; // for [Key]
using System.ComponentModel.DataAnnotations.Schema; // for Identity

namespace Hotels.Database.Tables
{
    public class Hotel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Country { get; set; }
        
        //potetially could be more conveniences
        public double BreakfastPrice { get; set; }
        public bool HasWifi { get; set; }
        public double PriceForNightForPerson { get; set; }

        public List<Room> Rooms { get; set; }
    }
}
