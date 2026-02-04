using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NameParser.Infrastructure.Data.Models
{
    [Table("RaceEventDistances")]
    public class RaceEventDistanceEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RaceEventId { get; set; }

        [Required]
        public decimal DistanceKm { get; set; }

        [ForeignKey(nameof(RaceEventId))]
        public RaceEventEntity RaceEvent { get; set; }
    }
}
