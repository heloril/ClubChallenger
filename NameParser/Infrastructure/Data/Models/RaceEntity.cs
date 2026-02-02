using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NameParser.Infrastructure.Data.Models
{
    [Table("Races")]
    public class RaceEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Year of the race. Null for "hors challenge" races (outside regular challenge)
        /// </summary>
        public int? Year { get; set; }

        [Required]
        public int RaceNumber { get; set; }

        [Required]
        public int DistanceKm { get; set; }

        [MaxLength(500)]
        [Obsolete("Use FileContent instead. Kept for backward compatibility.")]
        public string FilePath { get; set; }

        /// <summary>
        /// Binary content of the uploaded race file (Excel or PDF)
        /// </summary>
        public byte[] FileContent { get; set; }

        /// <summary>
        /// Original filename of the uploaded file
        /// </summary>
        [MaxLength(255)]
        public string FileName { get; set; }

        /// <summary>
        /// File extension (.xlsx, .pdf, etc.)
        /// </summary>
        [MaxLength(10)]
        public string FileExtension { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? ProcessedDate { get; set; }

        [MaxLength(50)]
        public string Status { get; set; }

        /// <summary>
        /// Flag indicating if this is a "hors challenge" race (not part of the yearly challenge)
        /// </summary>
        public bool IsHorsChallenge { get; set; }
    }
}
