using System;

namespace NameParser.Domain.Entities
{
    public class RaceResult
    {
        public Member Member { get; private set; }
        public RaceDistance RaceDistance { get; private set; }
        public TimeSpan Time { get; private set; }
        public int Points { get; private set; }

        public RaceResult(Member member, RaceDistance raceDistance, TimeSpan time, int points)
        {
            Member = member ?? throw new ArgumentNullException(nameof(member));
            RaceDistance = raceDistance ?? throw new ArgumentNullException(nameof(raceDistance));
            Time = time;
            Points = points;
        }

        public void UpdatePoints(int newPoints)
        {
            if (newPoints > Points)
                Points = newPoints;
        }
    }
}
