using System;
using System.Collections.Generic;

namespace NameParser.Domain.Entities
{
    public class Challenge
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Year { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public Challenge()
        {
        }

        public Challenge(string name, int year, string description = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Challenge name cannot be empty", nameof(name));
            if (year < 1900 || year > 2100)
                throw new ArgumentException("Invalid year", nameof(year));

            Name = name;
            Year = year;
            Description = description;
            StartDate = startDate;
            EndDate = endDate;
        }

        public override string ToString()
        {
            return $"{Name} ({Year})";
        }

        public override bool Equals(object obj)
        {
            if (obj is Challenge other)
                return Name == other.Name && Year == other.Year;
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (Name?.GetHashCode() ?? 0);
                hash = hash * 23 + Year.GetHashCode();
                return hash;
            }
        }
    }
}
