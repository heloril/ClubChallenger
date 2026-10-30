using System;

namespace NameParser.UI.ViewModels
{
	/// <summary>
	/// Unified row for the merged VMA grid. It represents either a VMA history entry
	/// (a measured or extrapolated value, deletable) or an extrapolation journal entry
	/// (an explanation of how a value was derived, or why it failed).
	/// </summary>
	public class VmaTimelineItem
	{
		public DateTime Date { get; set; }

		/// <summary>VMA value (km/h), null for failed extrapolation journal entries.</summary>
		public double? Vma { get; set; }

		/// <summary>Row category, e.g. "VMA testée", "VMA extrapolée", "Extrapolation".</summary>
		public string Type { get; set; }

		/// <summary>Explanation (populated for journal entries, empty for plain history rows).</summary>
		public string Details { get; set; }

		/// <summary>Identifier of the underlying VMA history record when the row is deletable.</summary>
		public int? RecordId { get; set; }

		/// <summary>True when the row is a VMA history entry that can be deleted.</summary>
		public bool CanDelete => RecordId.HasValue;

		public string VmaDisplay => Vma.HasValue
			? Vma.Value.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)
			: string.Empty;
	}
}
