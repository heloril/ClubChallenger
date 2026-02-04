using System;
using System.Collections.Generic;
using System.Linq;
using NameParser.Infrastructure.Data.Models;

namespace NameParser.Infrastructure.Data
{
    public class ChallengeRepository
    {
        public List<ChallengeEntity> GetAll()
        {
            using (var context = new RaceManagementContext())
            {
                return context.Challenges
                    .OrderByDescending(c => c.Year)
                    .ThenBy(c => c.Name)
                    .ToList();
            }
        }

        public List<ChallengeEntity> GetByYear(int year)
        {
            using (var context = new RaceManagementContext())
            {
                return context.Challenges
                    .Where(c => c.Year == year)
                    .OrderBy(c => c.Name)
                    .ToList();
            }
        }

        public ChallengeEntity GetById(int id)
        {
            using (var context = new RaceManagementContext())
            {
                return context.Challenges.Find(id);
            }
        }

        public int Create(ChallengeEntity challenge)
        {
            using (var context = new RaceManagementContext())
            {
                challenge.CreatedDate = DateTime.Now;
                context.Challenges.Add(challenge);
                context.SaveChanges();
                return challenge.Id;
            }
        }

        public void Update(ChallengeEntity challenge)
        {
            using (var context = new RaceManagementContext())
            {
                var existing = context.Challenges.Find(challenge.Id);
                if (existing != null)
                {
                    existing.Name = challenge.Name;
                    existing.Description = challenge.Description;
                    existing.Year = challenge.Year;
                    existing.StartDate = challenge.StartDate;
                    existing.EndDate = challenge.EndDate;
                    existing.ModifiedDate = DateTime.Now;
                    context.SaveChanges();
                }
            }
        }

        public void Delete(int id)
        {
            using (var context = new RaceManagementContext())
            {
                var challenge = context.Challenges.Find(id);
                if (challenge != null)
                {
                    // Delete associated ChallengeRaceEvents first
                    var associations = context.ChallengeRaceEvents
                        .Where(cre => cre.ChallengeId == id)
                        .ToList();
                    context.ChallengeRaceEvents.RemoveRange(associations);

                    context.Challenges.Remove(challenge);
                    context.SaveChanges();
                }
            }
        }

        public void AssociateRaceEvent(int challengeId, int raceEventId, int displayOrder = 0)
        {
            using (var context = new RaceManagementContext())
            {
                // Check if association already exists
                var existing = context.ChallengeRaceEvents
                    .FirstOrDefault(cre => cre.ChallengeId == challengeId && cre.RaceEventId == raceEventId);

                if (existing == null)
                {
                    var association = new ChallengeRaceEventEntity
                    {
                        ChallengeId = challengeId,
                        RaceEventId = raceEventId,
                        DisplayOrder = displayOrder
                    };
                    context.ChallengeRaceEvents.Add(association);
                    context.SaveChanges();
                }
            }
        }

        public void DisassociateRaceEvent(int challengeId, int raceEventId)
        {
            using (var context = new RaceManagementContext())
            {
                var association = context.ChallengeRaceEvents
                    .FirstOrDefault(cre => cre.ChallengeId == challengeId && cre.RaceEventId == raceEventId);

                if (association != null)
                {
                    context.ChallengeRaceEvents.Remove(association);
                    context.SaveChanges();
                }
            }
        }

        public List<RaceEventEntity> GetRaceEventsByChallenge(int challengeId)
        {
            using (var context = new RaceManagementContext())
            {
                return context.ChallengeRaceEvents
                    .Where(cre => cre.ChallengeId == challengeId)
                    .OrderBy(cre => cre.DisplayOrder)
                    .Select(cre => cre.RaceEvent)
                    .ToList();
            }
        }

        public List<RaceEventEntity> GetRaceEventsForChallenge(int challengeId)
        {
            // Alias for GetRaceEventsByChallenge - for consistency with naming
            return GetRaceEventsByChallenge(challengeId);
        }
    }
}
