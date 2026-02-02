using System;
using System.Collections.Generic;
using System.Linq;

namespace NameParser.Infrastructure.Data.Models
{
    public class ChallengerClassificationDto
    {
        public int RankByPoints { get; set; }
        public int RankByKms { get; set; }
        public string ChallengerFirstName { get; set; }
        public string ChallengerLastName { get; set; }
        public string ChallengerEmail { get; set; }
        public string Team { get; set; }
        
        // Total Points = Sum of best 7 races + bonus
        public int TotalPoints { get; set; }
        public int TotalBonusKm { get; set; }
        public int Best7RacesPoints { get; set; }
        
        // Total Kilometers
        public int TotalKilometers { get; set; }
        
        // Race count
        public int RaceCount { get; set; }
        
        // Race by race details
        public List<RaceDetail> RaceDetails { get; set; }

        public ChallengerClassificationDto()
        {
            RaceDetails = new List<RaceDetail>();
        }

        /// <summary>
        /// Gets the top 7 race details sorted by points (descending)
        /// </summary>
        public List<RaceDetail> GetBest7Races()
        {
            return RaceDetails
                .OrderByDescending(r => r.Points)
                .Take(7)
                .ToList();
        }
    }

    public class RaceDetail
    {
        public string RaceName { get; set; }
        public int RaceNumber { get; set; }
        public int DistanceKm { get; set; }
        public int Points { get; set; }
        public int BonusKm { get; set; }
        public int? Position { get; set; }
        public TimeSpan? RaceTime { get; set; }
        public double? Speed { get; set; }
        public bool IsInBest7 { get; set; }
    }
}
