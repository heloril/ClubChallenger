using System;

namespace NameParser.Infrastructure.Data.Models
{
	public class MemberVmaHistoryEntity
	{
		public int Id { get; set; }
		public int MemberId { get; set; }
		public double Vma { get; set; }
		public DateTime RecordedDate { get; set; }

		public MemberEntity Member { get; set; }
	}
}
