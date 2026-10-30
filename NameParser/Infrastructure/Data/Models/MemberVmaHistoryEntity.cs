using System;

namespace NameParser.Infrastructure.Data.Models
{
	public class MemberVmaHistoryEntity
	{
		public int Id { get; set; }
		public int MemberId { get; set; }
		public double Vma { get; set; }
		public DateTime RecordedDate { get; set; }

		/// <summary>
		/// True when this VMA value was extrapolated from race times rather than
		/// measured through an actual VMA test.
		/// </summary>
		public bool IsExtrapolated { get; set; }

		public MemberEntity Member { get; set; }
	}
}
