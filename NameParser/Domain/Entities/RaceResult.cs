using System;

namespace NameParser.Domain.Entities
{
    public class RaceResult
    {
        public Member Member { get; private set; }
        public Race Race { get; private set; }
        public TimeSpan Time { get; private set; }
        public int Points { get; private set; }

        public RaceResult(Member member, Race race, TimeSpan time, int points)
        {
            Member = member ?? throw new ArgumentNullException(nameof(member));
            Race = race ?? throw new ArgumentNullException(nameof(race));
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
