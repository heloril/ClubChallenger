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
                return FirstName == other.FirstName && LastName == other.LastName;
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (FirstName?.GetHashCode() ?? 0);
                hash = hash * 23 + (LastName?.GetHashCode() ?? 0);
                return hash;
            }
        }
    }
}
