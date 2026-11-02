using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NameParser.Infrastructure.Data.Models
{
	/// <summary>
	/// Persisted "recent-races VMA" estimate for a member: the average VMA computed
	/// from up to the 5 most recent races within the last 3 months, plus one detail
	/// line per race (see <see cref="RecentRaceVmaLineEntity"/>). One (latest) log is
	/// kept per member; it is recomputed whenever a new race result is encoded.
	/// </summary>
	[Table("RecentRaceVmaLogs")]
	public class RecentRaceVmaLogEntity
	{
		[Key]
		public int Id { get; set; }

		[Required]
		[MaxLength(100)]
		public string MemberFirstName { get; set; }

		[Required]
		[MaxLength(100)]
		public string MemberLastName { get; set; }

		public DateTime CreatedDate { get; set; }

		/// <summary>
		/// Average estimated VMA (km/h) over the retained recent races, null when it
		/// could not be computed (no race or no calibration data).
		/// </summary>
		public double? AverageVma { get; set; }

		/// <summary>
		/// Number of races retained for the computation.
		/// </summary>
		public int RaceCount { get; set; }

		public bool IsSuccess { get; set; }

		/// <summary>
		/// Human-readable explanation of how the value was derived (or why it failed).
		/// </summary>
		[MaxLength(1000)]
		public string Details { get; set; }

		public List<RecentRaceVmaLineEntity> Lines { get; set; } = new List<RecentRaceVmaLineEntity>();
	}
}
