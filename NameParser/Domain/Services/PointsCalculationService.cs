using System;

namespace NameParser.Domain.Services
{
    public class PointsCalculationService
    {
        public int CalculatePoints(TimeSpan referenceTime, TimeSpan memberTime)
        {
            if (memberTime.TotalSeconds == 0)
                throw new ArgumentException("Member time cannot be zero", nameof(memberTime));

            var points = Math.Round(referenceTime.TotalSeconds / memberTime.TotalSeconds * 1000);
            return (int)points;
        }

        public bool IsValidRaceTime(TimeSpan time)
        {
            return time.TotalMinutes > 10 && time.TotalHours < 5;
        }
    }
}
