using System.ComponentModel.DataAnnotations; // for [Key]
using System.ComponentModel.DataAnnotations.Schema; // for Identity

namespace Hotels.Database.Tables
{
    public class Attraction
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public List<AttractionInHotel> HotelsWithAttraction { get; set; }
    }
}
