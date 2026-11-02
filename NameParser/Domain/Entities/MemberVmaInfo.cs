namespace NameParser.Domain.Entities
{
	/// <summary>
	/// Lightweight projection of a member together with the VMA that best represents
	/// their current level (a measured value when available, otherwise an
	/// extrapolated one). Used to build training groups of athletes with a close VMA.
	/// </summary>
	public class MemberVmaInfo
	{
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string Email { get; set; }
		public double Vma { get; set; }
		public bool IsExtrapolated { get; set; }
	}
}
