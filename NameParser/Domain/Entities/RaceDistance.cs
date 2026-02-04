using System;

namespace NameParser.Domain.Entities
{
    public class RaceDistance
    {
        public int RaceNumber { get; set; }
        public string Name { get; set; }
        public int DistanceKm { get; set; }

        public RaceDistance()
        {
        }

        public RaceDistance(int raceNumber, string name, int distanceKm)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Race name cannot be empty", nameof(name));
            if (distanceKm <= 0)
                throw new ArgumentException("Distance must be positive", nameof(distanceKm));

            RaceNumber = raceNumber;
            Name = name;
            DistanceKm = distanceKm;
        }

        public override string ToString()
        {
            return $"{RaceNumber}.{DistanceKm}.{Name}";
        }
    }
}
