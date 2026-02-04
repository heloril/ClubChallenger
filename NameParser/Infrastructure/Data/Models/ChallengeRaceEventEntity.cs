using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NameParser.Infrastructure.Data.Models
{
    [Table("ChallengeRaceEvents")]
    public class ChallengeRaceEventEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ChallengeId { get; set; }

        [Required]
        public int RaceEventId { get; set; }

        public int DisplayOrder { get; set; }

        [ForeignKey(nameof(ChallengeId))]
        public virtual ChallengeEntity Challenge { get; set; }

        [ForeignKey(nameof(RaceEventId))]
        public virtual RaceEventEntity RaceEvent { get; set; }
    }
}
