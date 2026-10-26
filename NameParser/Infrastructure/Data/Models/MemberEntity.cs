using System.Collections.Generic;

namespace NameParser.Infrastructure.Data.Models
{
	public class MemberEntity
	{
		public int Id { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string Email { get; set; }
		public bool IsMember { get; set; }
		public bool IsChallenger { get; set; }

		public ICollection<MemberVmaHistoryEntity> VmaHistory { get; set; } = new List<MemberVmaHistoryEntity>();
	}
}
