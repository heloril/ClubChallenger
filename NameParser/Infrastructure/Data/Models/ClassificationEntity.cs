using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NameParser.Infrastructure.Data.Models
{
    [Table("Classifications")]
    public class ClassificationEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RaceId { get; set; }

        [ForeignKey("RaceId")]
        public virtual RaceEntity Race { get; set; }

        [Required]
        [MaxLength(100)]
        public string MemberFirstName { get; set; }

        [Required]
        [MaxLength(100)]
        public string MemberLastName { get; set; }

        [MaxLength(200)]
        public string MemberEmail { get; set; }

        [Required]
        public int Points { get; set; }

        [Required]
        public int BonusKm { get; set; }

        public TimeSpan? RaceTime { get; set; }

        public TimeSpan? TimePerKm { get; set; }

        public int? Position { get; set; }

        [MaxLength(200)]
        public string Team { get; set; }

        public double? Speed { get; set; }

        [MaxLength(1)]
        public string Sex { get; set; }

        public int? PositionBySex { get; set; }

        [MaxLength(50)]
        public string AgeCategory { get; set; }

        public int? PositionByCategory { get; set; }

        public bool IsMember { get; set; }

        public bool IsChallenger { get; set; }

        public DateTime CreatedDate { get; set; }
    }
}
