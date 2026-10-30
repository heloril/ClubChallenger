using System;

namespace NameParser.Domain.Entities
{
	/// <summary>
	/// Represents a single VMA history entry for a member, exposing whether the
	/// value was measured through a test or extrapolated from race times.
	/// </summary>
	public class MemberVmaRecord
	{
		public int Id { get; set; }
		public double Vma { get; set; }
		public DateTime RecordedDate { get; set; }
		public bool IsExtrapolated { get; set; }

		public string Source => IsExtrapolated ? "Extrapolée" : "Test";
	}
}
