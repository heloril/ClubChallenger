using System;

namespace NameParser.Domain.Entities
{
    public class Member
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public bool IsMember { get; set; }
        public bool IsChallenger { get; set; }

        public Member()
        {
        }

        public Member(string firstName, string lastName, string email = null, bool isMember = false, bool isChallenger = false)
        {
            if (string.IsNullOrWhiteSpace(lastName))
                throw new ArgumentException("Last name cannot be empty", nameof(lastName));

            FirstName = firstName;
            LastName = lastName;
            Email = email;
            IsMember = isMember;
            IsChallenger = isChallenger;
        }

        public string GetFullName()
        {
            return $"{FirstName} {LastName.ToUpper()}";
        }

        public override string ToString()
        {
            return GetFullName();
        }

        public override bool Equals(object obj)
        {
            if (obj is Member other)
            {
                var normalizedFirstName = FirstName?.NormalizeForComparison() ?? "";
                var normalizedLastName = LastName?.NormalizeForComparison() ?? "";
                var otherNormalizedFirstName = other.FirstName?.NormalizeForComparison() ?? "";
                var otherNormalizedLastName = other.LastName?.NormalizeForComparison() ?? "";

                return normalizedFirstName == otherNormalizedFirstName && 
                       normalizedLastName == otherNormalizedLastName;
            }
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                var normalizedFirstName = FirstName?.NormalizeForComparison() ?? "";
                var normalizedLastName = LastName?.NormalizeForComparison() ?? "";
                hash = hash * 23 + normalizedFirstName.GetHashCode();
                hash = hash * 23 + normalizedLastName.GetHashCode();
                return hash;
            }
        }
    }
}
