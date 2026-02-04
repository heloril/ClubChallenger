using System;

namespace NameParser.Domain.Entities
{
    /// <summary>
    /// Join entity representing the many-to-many relationship between Challenges and RaceEvents
    /// </summary>
    public class ChallengeRaceEvent
    {
        public Challenge Challenge { get; set; }
        public RaceEvent RaceEvent { get; set; }
        public int DisplayOrder { get; set; }

        public ChallengeRaceEvent()
        {
        }

        public ChallengeRaceEvent(Challenge challenge, RaceEvent raceEvent, int displayOrder = 0)
        {
            Challenge = challenge ?? throw new ArgumentNullException(nameof(challenge));
            RaceEvent = raceEvent ?? throw new ArgumentNullException(nameof(raceEvent));
            DisplayOrder = displayOrder;
        }

        public override bool Equals(object obj)
        {
            if (obj is ChallengeRaceEvent other)
                return Challenge?.Equals(other.Challenge) == true && RaceEvent?.Equals(other.RaceEvent) == true;
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (Challenge?.GetHashCode() ?? 0);
                hash = hash * 23 + (RaceEvent?.GetHashCode() ?? 0);
                return hash;
            }
        }
    }
}
