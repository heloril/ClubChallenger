using System;
using System.Collections.Generic;

namespace NameParser.Domain.Entities
{
    public class RaceEvent
    {
        public string Name { get; set; }
        public DateTime EventDate { get; set; }
        public string Location { get; set; }
        public string WebsiteUrl { get; set; }
        public string Description { get; set; }

        public RaceEvent()
        {
        }

        public RaceEvent(string name, DateTime eventDate, string location = null, string websiteUrl = null, string description = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Race event name cannot be empty", nameof(name));

            Name = name;
            EventDate = eventDate;
            Location = location;
            WebsiteUrl = websiteUrl;
            Description = description;
        }

        public override string ToString()
        {
            return $"{Name} - {EventDate:dd/MM/yyyy}";
        }

        public override bool Equals(object obj)
        {
            if (obj is RaceEvent other)
                return Name == other.Name && EventDate.Date == other.EventDate.Date;
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (Name?.GetHashCode() ?? 0);
                hash = hash * 23 + EventDate.Date.GetHashCode();
                return hash;
            }
        }
    }
}
