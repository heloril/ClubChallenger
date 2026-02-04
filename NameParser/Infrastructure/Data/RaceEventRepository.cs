using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using NameParser.Infrastructure.Data.Models;

namespace NameParser.Infrastructure.Data
{
    public class RaceEventRepository
    {
        public List<RaceEventEntity> GetAll()
        {
            using (var context = new RaceManagementContext())
            {
                return context.RaceEvents
                    .OrderByDescending(re => re.EventDate)
                    .ToList();
            }
        }

        public RaceEventEntity GetById(int id)
        {
            using (var context = new RaceManagementContext())
            {
                return context.RaceEvents.Find(id);
            }
        }

        public int Create(RaceEventEntity raceEvent)
        {
            using (var context = new RaceManagementContext())
            {
                raceEvent.CreatedDate = DateTime.Now;
                context.RaceEvents.Add(raceEvent);
                context.SaveChanges();
                return raceEvent.Id;
            }
        }

        public void Update(RaceEventEntity raceEvent)
        {
            using (var context = new RaceManagementContext())
            {
                var existing = context.RaceEvents.Find(raceEvent.Id);
                if (existing != null)
                {
                    existing.Name = raceEvent.Name;
                    existing.EventDate = raceEvent.EventDate;
                    existing.Location = raceEvent.Location;
                    existing.WebsiteUrl = raceEvent.WebsiteUrl;
                    existing.Description = raceEvent.Description;
                    existing.ModifiedDate = DateTime.Now;
                    context.SaveChanges();
                }
            }
        }

        public void Delete(int id)
        {
            using (var context = new RaceManagementContext())
            {
                var raceEvent = context.RaceEvents.Find(id);
                if (raceEvent != null)
                {
                    // Delete associated ChallengeRaceEvents
                    var associations = context.ChallengeRaceEvents
                        .Where(cre => cre.RaceEventId == id)
                        .ToList();
                    context.ChallengeRaceEvents.RemoveRange(associations);

                    // Set RaceEventId to null for all associated races
                    var races = context.Races
                        .Where(r => r.RaceEventId == id)
                        .ToList();
                    foreach (var race in races)
                    {
                        race.RaceEventId = null;
                    }

                    context.RaceEvents.Remove(raceEvent);
                    context.SaveChanges();
                }
            }
        }

        public List<RaceEntity> GetRacesByEvent(int raceEventId)
        {
            using (var context = new RaceManagementContext())
            {
                return context.Races
                    .Where(r => r.RaceEventId == raceEventId)
                    .OrderBy(r => r.DistanceKm)
                    .ToList();
            }
        }

        public List<ChallengeEntity> GetChallengesByEvent(int raceEventId)
        {
            using (var context = new RaceManagementContext())
            {
                return context.ChallengeRaceEvents
                    .Where(cre => cre.RaceEventId == raceEventId)
                    .Select(cre => cre.Challenge)
                    .ToList();
            }
        }

        public List<RaceEventDistanceEntity> GetDistancesByEvent(int raceEventId)
        {
            using (var context = new RaceManagementContext())
            {
                return context.RaceEventDistances
                    .Where(red => red.RaceEventId == raceEventId)
                    .OrderBy(red => red.DistanceKm)
                    .ToList();
            }
        }

        public void AddDistance(int raceEventId, decimal distanceKm)
        {
            using (var context = new RaceManagementContext())
            {
                // Check if distance already exists
                var exists = context.RaceEventDistances
                    .Any(red => red.RaceEventId == raceEventId && red.DistanceKm == distanceKm);

                if (!exists)
                {
                    context.RaceEventDistances.Add(new RaceEventDistanceEntity
                    {
                        RaceEventId = raceEventId,
                        DistanceKm = distanceKm
                    });
                    context.SaveChanges();
                }
            }
        }

        public void RemoveDistance(int distanceId)
        {
            using (var context = new RaceManagementContext())
            {
                var distance = context.RaceEventDistances.Find(distanceId);
                if (distance != null)
                {
                    context.RaceEventDistances.Remove(distance);
                    context.SaveChanges();
                }
            }
        }
    }
}
