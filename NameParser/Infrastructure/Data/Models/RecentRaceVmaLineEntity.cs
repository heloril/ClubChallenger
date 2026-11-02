using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NameParser.Infrastructure.Data.Models
{
	/// <summary>
	/// One race line of a <see cref="RecentRaceVmaLogEntity"/>: the race it refers to,
	/// its date, the member's average speed in that race and the VMA estimated from it.
	/// </summary>
	[Table("RecentRaceVmaLines")]
	public class RecentRaceVmaLineEntity
	{
		[Key]
		public int Id { get; set; }

		public int RecentRaceVmaLogId { get; set; }

		/// <summary>
		/// Reference to the race (Races.Id) this line was computed from, when known.
		/// Used to deduplicate lines per race and to recompute the all-time VMA reliably.
		/// </summary>
		public int? RaceId { get; set; }

		[MaxLength(300)]
		public string RaceName { get; set; }

		public int? DistanceKm { get; set; }

		public DateTime? RaceDate { get; set; }

		/// <summary>Member's speed (km/h) in this race.</summary>
		public double Speed { get; set; }

		/// <summary>VMA (km/h) estimated from this single race.</summary>
		public double? EstimatedVma { get; set; }

		public RecentRaceVmaLogEntity RecentRaceVmaLog { get; set; }
	}
}
