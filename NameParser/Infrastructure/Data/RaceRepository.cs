using System.Collections.Generic;
using System.Linq;
using NameParser.Domain.Entities;
using NameParser.Infrastructure.Data.Models;
using NameParser.Infrastructure.Services;

namespace NameParser.Infrastructure.Data
{
    public class RaceRepository
    {
        private readonly FileStorageService _fileStorageService;

        public RaceRepository()
        {
            _fileStorageService = new FileStorageService();
        }

        public void SaveRace(RaceDistance raceDistance, int? year, string filePath, bool isHorsChallenge = false, int? raceEventId = null)
        {
            using (var context = new RaceManagementContext())
            {
                // Check if a race with the same year, race number, and distance already exists
                var existingRace = context.Races
                    .FirstOrDefault(r => r.Year == year 
                                      && r.RaceNumber == raceDistance.RaceNumber 
                                      && r.DistanceKm == raceDistance.DistanceKm);

                if (existingRace != null)
                {
                    throw new System.InvalidOperationException(
                        $"A race with Year={year?.ToString() ?? "Hors Challenge"}, RaceNumber={raceDistance.RaceNumber}, " +
                        $"and Distance={raceDistance.DistanceKm}km already exists (ID: {existingRace.Id}). " +
                        $"Please use a different race number, distance, or delete the existing race first.");
                }

                // Determine if the source is a URL or a local file
                bool isUrl = filePath.StartsWith("http://", System.StringComparison.OrdinalIgnoreCase) || 
                             filePath.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase);

                byte[] fileContent = null;
                string fileName = null;
                string fileExtension = null;

                // Only read file content for local files, not URLs
                if (!isUrl)
                {
                    var fileData = _fileStorageService.ReadRaceFile(filePath);
                    fileContent = fileData.content;
                    fileName = fileData.fileName;
                    fileExtension = fileData.extension;
                }
                else
                {
                    // For URLs, store the URL as the file name for reference
                    fileName = filePath;
                    fileExtension = ".url";
                }

                var entity = new RaceEntity
                {
                    Name = raceDistance.Name,
                    Year = year,
                    RaceNumber = raceDistance.RaceNumber,
                    DistanceKm = raceDistance.DistanceKm,
                    RaceEventId = raceEventId,
                    FileContent = fileContent,
                    FileName = fileName,
                    FileExtension = fileExtension,
                    FilePath = null, // Deprecated, no longer storing file path
                    CreatedDate = System.DateTime.Now,
                    Status = "Pending",
                    IsHorsChallenge = isHorsChallenge
                };

                context.Races.Add(entity);
                context.SaveChanges();
            }
        }

        public void UpdateRaceStatus(int raceId, string status)
        {
            using (var context = new RaceManagementContext())
            {
                var race = context.Races.Find(raceId);
                if (race != null)
                {
                    race.Status = status;
                    race.ProcessedDate = System.DateTime.Now;
                    context.SaveChanges();
                }
            }
        }

        public List<RaceEntity> GetRacesByYear(int year)
        {
            using (var context = new RaceManagementContext())
            {
                return context.Races
                    .Where(r => r.Year == year)
                    .OrderBy(r => r.RaceNumber)
                    .ToList();
            }
        }

        public List<RaceEntity> GetAllRaces()
        {
            using (var context = new RaceManagementContext())
            {
                return context.Races
                    .OrderByDescending(r => r.Year ?? 0) // Hors challenge races (null) will be at the end
                    .ThenBy(r => r.RaceNumber)
                    .ThenBy(r => r.DistanceKm)
                    .ToList();
            }
        }

        public List<RaceEntity> GetHorsChallengeRaces()
        {
            using (var context = new RaceManagementContext())
            {
                return context.Races
                    .Where(r => r.IsHorsChallenge || r.Year == null)
                    .OrderBy(r => r.RaceNumber)
                    .ThenBy(r => r.DistanceKm)
                    .ToList();
            }
        }

        public RaceEntity GetRaceById(int id)
        {
            using (var context = new RaceManagementContext())
            {
                return context.Races.Find(id);
            }
        }

        public void DeleteRace(int id)
        {
            using (var context = new RaceManagementContext())
            {
                var race = context.Races.Find(id);
                if (race != null)
                {
                    // File content is stored in database and will be deleted with the race entity
                    context.Races.Remove(race);
                    context.SaveChanges();
                }
            }
        }

        public void AssociateRaceWithEvent(int raceId, int raceEventId)
        {
            using (var context = new RaceManagementContext())
            {
                var race = context.Races.Find(raceId);
                if (race != null)
                {
                    race.RaceEventId = raceEventId;
                    context.SaveChanges();
                }
            }
        }

        public void DisassociateRaceFromEvent(int raceId)
        {
            using (var context = new RaceManagementContext())
            {
                var race = context.Races.Find(raceId);
                if (race != null)
                {
                    race.RaceEventId = null;
                    context.SaveChanges();
                }
            }
        }

        public List<RaceEntity> GetRacesByRaceEvent(int raceEventId)
        {
            using (var context = new RaceManagementContext())
            {
                return context.Races
                    .Where(r => r.RaceEventId == raceEventId)
                    .OrderBy(r => r.DistanceKm)  // Order by distance
                    .ToList();
            }
        }
    }
}
