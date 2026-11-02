using System;
using System.Collections.Generic;

namespace NameParser.Application.Services
{
	/// <summary>
	/// Result of a recent-races VMA computation for a member: the averaged VMA and the
	/// per-race estimates it was computed from.
	/// </summary>
	public class RecentRacesVmaResult
	{
		public string FirstName { get; set; }
		public string LastName { get; set; }

		/// <summary>Averaged estimated VMA (km/h); null when it could not be computed.</summary>
		public double? AverageVma { get; set; }

		public string Details { get; set; }

		public List<RecentRaceVmaItem> Races { get; } = new List<RecentRaceVmaItem>();

		public bool IsSuccess => AverageVma.HasValue;
	}

	/// <summary>
	/// A single race used in a recent-races VMA computation.
	/// </summary>
	public class RecentRaceVmaItem
	{
		public int? RaceId { get; set; }
		public string RaceName { get; set; }
		public int? DistanceKm { get; set; }
		public DateTime RaceDate { get; set; }
		public double Speed { get; set; }
		public double? EstimatedVma { get; set; }
	}

	/// <summary>
	/// Athletes whose recent-races VMA is closest to a reference VMA.
	/// </summary>
	public class NearbyAthletesResult
	{
		/// <summary>Athletes just above the reference VMA (strongest first).</summary>
		public List<NearbyAthlete> Stronger { get; set; } = new List<NearbyAthlete>();

		/// <summary>Athletes just below the reference VMA (strongest first).</summary>
		public List<NearbyAthlete> Weaker { get; set; } = new List<NearbyAthlete>();
	}

	public class NearbyAthlete
	{
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public double Vma { get; set; }

		public string FullName => $"{FirstName} {LastName}".Trim();
	}
}
