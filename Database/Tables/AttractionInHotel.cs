using System.ComponentModel.DataAnnotations; // for [Key]
using System.ComponentModel.DataAnnotations.Schema; // for Identity

namespace Hotels.Database.Tables
{
    public class AttractionInHotel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public Hotel Hotel { get; set; }
        public Attraction Attraction { get; set; }
    }
}
