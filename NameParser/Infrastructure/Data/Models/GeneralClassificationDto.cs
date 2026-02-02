using System;

namespace NameParser.Infrastructure.Data.Models
{
    public class GeneralClassificationDto
    {
        public int Rank { get; set; }
        public string MemberFirstName { get; set; }
        public string MemberLastName { get; set; }
        public string MemberEmail { get; set; }
        public string Team { get; set; }
        public int TotalPoints { get; set; }
        public int TotalBonusKm { get; set; }
        public int RaceCount { get; set; }
        public int AveragePoints { get; set; }
        public int? BestPosition { get; set; }
        public TimeSpan? BestRaceTime { get; set; }
        public TimeSpan? BestTimePerKm { get; set; }
    }
}
