using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using NameParser.Domain.Aggregates;
using NameParser.Infrastructure.Data.Models;

namespace NameParser.Infrastructure.Data
{
    public class ClassificationRepository
    {
        public void SaveClassifications(int raceId, Classification classification)
        {
            using (var context = new RaceManagementContext())
            {
                var entities = new List<ClassificationEntity>();

                foreach (var memberClass in classification.GetAllClassifications())
                {
                    var entity = new ClassificationEntity
                    {
                        RaceId = raceId,
                        MemberFirstName = memberClass.Member.FirstName,
                        MemberLastName = memberClass.Member.LastName,
                        MemberEmail = memberClass.Member.Email,
                        Points = memberClass.Points,
                        BonusKm = memberClass.BonusKm,
                        RaceTime = memberClass.RaceTime,
                        TimePerKm = memberClass.TimePerKm,
                        Position = memberClass.Position,
                        Team = memberClass.Team,
                        Speed = memberClass.Speed,
                        Sex = memberClass.Sex,
                        PositionBySex = memberClass.PositionBySex,
                        AgeCategory = memberClass.AgeCategory,
                        PositionByCategory = memberClass.PositionByCategory,
                        IsMember = memberClass.IsMember,
                        IsChallenger = memberClass.IsChallenger,
                        CreatedDate = DateTime.Now
                    };

                    entities.Add(entity);
                }

                context.Classifications.AddRange(entities);
                context.SaveChanges();
            }
        }

        public List<ClassificationEntity> GetClassificationsByRace(int raceId, bool? isMemberFilter = null, bool? isChallengerFilter = null)
        {
            using (var context = new RaceManagementContext())
            {
                var query = context.Classifications
                    .Include(c => c.Race)
                    .Where(c => c.RaceId == raceId);

                // Apply member filter if provided, but always include the winner (Position == 1)
                if (isMemberFilter.HasValue)
                {
                    query = query.Where(c => c.IsMember == isMemberFilter.Value || c.Position == 1);
                }

                // Apply challenger filter if provided, but always include the winner (Position == 1)
                if (isChallengerFilter.HasValue)
                {
                    query = query.Where(c => c.IsChallenger == isChallengerFilter.Value || c.Position == 1);
                }

                return query
                    .OrderBy(c => c.Position ?? int.MaxValue)
                    .ThenByDescending(c => c.Points)
                    .ToList();
            }
        }

        public List<ClassificationEntity> GetClassificationsByYear(int year)
        {
            using (var context = new RaceManagementContext())
            {
                return context.Classifications
                    .Include(c => c.Race)
                    .Where(c => c.Race.Year == year)
                    .OrderByDescending(c => c.Points)
                    .ToList();
            }
        }

        public List<GeneralClassificationDto> GetGeneralClassification(int year)
        {
            using (var context = new RaceManagementContext())
            {
                // Group by member and sum points and bonus km
                var generalClassification = context.Classifications
                    .Include(c => c.Race)
                    .Where(c => c.Race.Year == year && c.IsMember) // Only members in general classification
                    .GroupBy(c => new { c.MemberFirstName, c.MemberLastName, c.MemberEmail, c.Team })
                    .Select(g => new GeneralClassificationDto
                    {
                        MemberFirstName = g.Key.MemberFirstName,
                        MemberLastName = g.Key.MemberLastName,
                        MemberEmail = g.Key.MemberEmail,
                        Team = g.Key.Team,
                        TotalPoints = g.Sum(c => c.Points),
                        TotalBonusKm = g.Sum(c => c.BonusKm),
                        RaceCount = g.Count(),
                        AveragePoints = (int)g.Average(c => c.Points),
                        BestPosition = g.Min(c => c.Position),
                        BestRaceTime = g.Where(c => c.RaceTime.HasValue).Min(c => c.RaceTime),
                        BestTimePerKm = g.Where(c => c.TimePerKm.HasValue).Min(c => c.TimePerKm)
                    })
                    .OrderByDescending(c => c.TotalPoints)
                    .ThenByDescending(c => c.TotalBonusKm)
                    .ToList();

                // Add rank
                int rank = 1;
                foreach (var classification in generalClassification)
                {
                    classification.Rank = rank++;
                }

                return generalClassification;
            }
        }

        public void DeleteClassificationsByRace(int raceId)
        {
            using (var context = new RaceManagementContext())
            {
                var classifications = context.Classifications.Where(c => c.RaceId == raceId);
                context.Classifications.RemoveRange(classifications);
                context.SaveChanges();
            }
        }

        public List<ChallengerClassificationDto> GetChallengerClassification(int year)
        {
            using (var context = new RaceManagementContext())
            {
                // Get all challengers' classifications for the year
                var challengerClassifications = context.Classifications
                    .Include(c => c.Race)
                    .Where(c => c.Race.Year == year && c.IsChallenger)
                    .ToList();

                // Group by challenger
                var grouped = challengerClassifications
                    .GroupBy(c => new { c.MemberFirstName, c.MemberLastName, c.MemberEmail, c.Team })
                    .Select(g => new
                    {
                        Challenger = g.Key,
                        Classifications = g.OrderByDescending(c => c.Points).ToList()
                    })
                    .ToList();

                var result = new List<ChallengerClassificationDto>();

                foreach (var group in grouped)
                {
                    // Get best 7 races by points
                    var best7 = group.Classifications.OrderByDescending(c => c.Points).Take(7).ToList();
                    var best7Points = best7.Sum(c => c.Points);
                    var totalBonus = group.Classifications.Sum(c => c.BonusKm);
                    var totalKilometers = group.Classifications.Sum(c => c.Race.DistanceKm);

                    // Create race details
                    var raceDetails = group.Classifications.Select(c => new RaceDetail
                    {
                        RaceName = c.Race.Name,
                        RaceNumber = c.Race.RaceNumber,
                        DistanceKm = c.Race.DistanceKm,
                        Points = c.Points,
                        BonusKm = c.BonusKm,
                        Position = c.Position,
                        RaceTime = c.RaceTime,
                        Speed = c.Speed,
                        IsInBest7 = best7.Contains(c)
                    }).OrderBy(r => r.RaceNumber).ToList();

                    var dto = new ChallengerClassificationDto
                    {
                        ChallengerFirstName = group.Challenger.MemberFirstName,
                        ChallengerLastName = group.Challenger.MemberLastName,
                        ChallengerEmail = group.Challenger.MemberEmail,
                        Team = group.Challenger.Team,
                        Best7RacesPoints = best7Points,
                        TotalBonusKm = totalBonus,
                        TotalPoints = best7Points + totalBonus,
                        TotalKilometers = totalKilometers,
                        RaceCount = group.Classifications.Count,
                        RaceDetails = raceDetails
                    };

                    result.Add(dto);
                }

                // Sort by total points (descending), then by total kms (descending)
                result = result
                    .OrderByDescending(c => c.TotalPoints)
                    .ThenByDescending(c => c.TotalKilometers)
                    .ToList();

                // Assign ranks
                int rankByPoints = 1;
                foreach (var item in result)
                {
                    item.RankByPoints = rankByPoints++;
                }

                // Sort by kilometers and assign km ranks
                var sortedByKms = result.OrderByDescending(c => c.TotalKilometers).ToList();
                int rankByKms = 1;
                foreach (var item in sortedByKms)
                {
                    item.RankByKms = rankByKms++;
                }

                // Return sorted by points
                return result;
            }
        }
    }
}
