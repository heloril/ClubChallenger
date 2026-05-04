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
                    // For URLs, use AcnTimingRaceResultRepository to download via API
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"Downloading and caching content from ACN-Timing URL: {filePath}");

                        // Use AcnTimingRaceResultRepository to handle API calls properly
                        var acnTimingRepo = new NameParser.Infrastructure.Repositories.AcnTimingRaceResultRepository();

                        // Call GetRaceResults which will handle URL parsing, API calls, and return results
                        // We pass an empty members list here since we only want to cache the data, not process it yet
                        var tempResults = acnTimingRepo.GetRaceResults(filePath, new System.Collections.Generic.List<NameParser.Domain.Entities.Member>());

                        if (tempResults != null && tempResults.Count > 1) // More than just header
                        {
                            // The repository successfully downloaded and parsed the data
                            // Now we need to get the raw JSON content that was downloaded
                            // Since GetRaceResults returns parsed data, we need to re-download the raw JSON

                            var httpClient = new System.Net.Http.HttpClient();
                            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                            httpClient.DefaultRequestHeaders.Add("Accept", "application/json, text/html, */*");

                            // Parse the URL to get the API endpoint
                            string apiUrl = null;
                            if (filePath.Contains("acn-timing.com"))
                            {
                                // Extract context and viewId from ACN-Timing URL
                                var match = System.Text.RegularExpressions.Regex.Match(filePath, @"ctx/([^/]+)/generic/[^/]+/home/([^/\?]+)");
                                if (match.Success)
                                {
                                    var contextValue = match.Groups[1].Value;
                                    var viewId = match.Groups[2].Value;
                                    apiUrl = $"https://results.chronorace.be/api/results/table/search/{contextValue}/{viewId}?srch=&pageSize=1000";
                                }
                            }
                            else if (filePath.Contains("chronorace.be/api"))
                            {
                                // Already an API URL
                                apiUrl = filePath;
                                if (!apiUrl.Contains("?"))
                                {
                                    apiUrl += "?srch=&pageSize=1000";
                                }
                            }

                            if (!string.IsNullOrEmpty(apiUrl))
                            {
                                System.Diagnostics.Debug.WriteLine($"Fetching raw JSON from API: {apiUrl}");
                                var response = httpClient.GetAsync(apiUrl).GetAwaiter().GetResult();
                                if (response.IsSuccessStatusCode)
                                {
                                    var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                                    fileContent = System.Text.Encoding.UTF8.GetBytes(content);
                                    fileName = filePath; // Store the original URL for reference
                                    fileExtension = ".json"; // Mark as JSON for proper parsing
                                    System.Diagnostics.Debug.WriteLine($"Successfully cached {fileContent.Length} bytes from API");
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"Failed to download API content: HTTP {response.StatusCode}");
                                    fileName = filePath;
                                    fileExtension = ".url";
                                }
                            }
                            else
                            {
                                // Could not parse URL, but validation passed, so store URL reference
                                System.Diagnostics.Debug.WriteLine($"Could not extract API URL from: {filePath}");
                                fileName = filePath;
                                fileExtension = ".url";
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"ACN-Timing repository returned no results for URL: {filePath}");
                            fileName = filePath;
                            fileExtension = ".url";
                        }
                    }
                    catch (System.Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error caching ACN-Timing URL content: {ex.Message}");
                        // Store URL info even if caching fails
                        fileName = filePath;
                        fileExtension = ".url";
                    }
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

        /// <summary>
        /// Updates the file content of a race (useful for caching downloaded URL content)
        /// </summary>
        public void UpdateRaceFileContent(int raceId, byte[] fileContent, string fileExtension)
        {
            using (var context = new RaceManagementContext())
            {
                var race = context.Races.Find(raceId);
                if (race != null)
                {
                    race.FileContent = fileContent;
                    race.FileExtension = fileExtension;
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
