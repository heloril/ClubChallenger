using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NameParser.Infrastructure.Data;
using NameParser.Infrastructure.Data.Models;
using NameParser.Web.Services;

namespace NameParser.Web.Pages;

public class RacesModel : PageModel
{
    private readonly RaceRepository _raceRepository;
    private readonly ClassificationRepository _classificationRepository;
    private readonly FacebookService _facebookService;
    private readonly ILogger<RacesModel> _logger;

    public RacesModel(
        RaceRepository raceRepository,
        ClassificationRepository classificationRepository,
        FacebookService facebookService,
        ILogger<RacesModel> logger)
    {
        _raceRepository = raceRepository;
        _classificationRepository = classificationRepository;
        _facebookService = facebookService;
        _logger = logger;
    }

    public List<RaceEntity> Races { get; set; } = new();
    public List<ClassificationEntity> Classifications { get; set; } = new();

    public int? SelectedRaceId { get; set; }
    public bool? MemberFilter { get; set; }
    public bool? ChallengerFilter { get; set; }

    public string StatusMessage { get; set; } = string.Empty;
    public bool IsError { get; set; }

    public void OnGet(int? raceId, bool? memberFilter, bool? challengerFilter)
    {
        try
        {
            // Load all races
            Races = _raceRepository.GetAllRaces();

            if (raceId.HasValue)
            {
                SelectedRaceId = raceId;
                MemberFilter = memberFilter;
                ChallengerFilter = challengerFilter;

                // Load classifications for selected race - pass filters directly to repository
                Classifications = _classificationRepository.GetClassificationsByRace(
                    raceId.Value, 
                    memberFilter, 
                    challengerFilter);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading races");
            StatusMessage = $"Error loading races: {ex.Message}";
            IsError = true;
        }
    }

    public IActionResult OnGetDownload(int raceId)
    {
        try
        {
            var race = _raceRepository.GetRaceById(raceId);
            if (race == null)
            {
                return NotFound();
            }

            var classifications = _classificationRepository.GetClassificationsByRace(raceId);

            // Generate CSV content
            var csvContent = GenerateCsvContent(race, classifications);
            var fileName = $"{race.Name}_{race.Year ?? 0}_Results.csv";

            return File(System.Text.Encoding.UTF8.GetBytes(csvContent), "text/csv", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading race results");
            StatusMessage = $"Error downloading results: {ex.Message}";
            IsError = true;
            return RedirectToPage();
        }
    }

    public IActionResult OnGetDelete(int raceId)
    {
        try
        {
            _raceRepository.DeleteRace(raceId);
            StatusMessage = "Race deleted successfully!";
            IsError = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting race");
            StatusMessage = $"Error deleting race: {ex.Message}";
            IsError = true;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostShareToFacebookAsync(int raceId)
    {
        try
        {
            var race = _raceRepository.GetRaceById(raceId);
            if (race == null)
            {
                return NotFound();
            }

            var allClassifications = _classificationRepository.GetClassificationsByRace(raceId);
            var resultsUrl = $"{Request.Scheme}://{Request.Host}/Races?raceId={raceId}";

            // Post 1: Full Results Summary
            var fullResultsSummary = BuildFullResultsSummary(race, allClassifications);

            // Post 2: Challenger Results (ALL challengers, regardless of points)
            var challengerResults = allClassifications.Where(c => c.IsChallenger).ToList();
            var challengerSummary = BuildChallengerResultsSummary(race, challengerResults);

            // Post both to Facebook
            var results = await _facebookService.PostRaceWithLatestResultsAsync(
                raceName: $"{race.Name} - {race.Year}",
                raceUrl: resultsUrl,
                fullResultsSummary: fullResultsSummary,
                challengerSummary: challengerSummary);

            // Check results
            var successCount = results.Count(r => r.Success);
            var failCount = results.Count(r => !r.Success);

            if (successCount == results.Count)
            {
                StatusMessage = $"Successfully shared both posts to Facebook! " +
                    $"Post 1 ID: {results[0].PostId}, Post 2 ID: {results[1].PostId}";
                IsError = false;
            }
            else if (successCount > 0)
            {
                StatusMessage = $"Partially successful: {successCount} of {results.Count} posts shared. " +
                    $"Errors: {string.Join(", ", results.Where(r => !r.Success).Select(r => r.ErrorMessage))}";
                IsError = true;
            }
            else
            {
                StatusMessage = $"Failed to share to Facebook: {string.Join(", ", results.Select(r => r.ErrorMessage))}";
                IsError = true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sharing to Facebook");
            StatusMessage = $"Error sharing to Facebook: {ex.Message}";
            IsError = true;
        }

        return RedirectToPage(new { raceId });
    }

    private string BuildFullResultsSummary(RaceEntity race, List<ClassificationEntity> allClassifications)
    {
        var summary = $"📊 Full Challenge Results for {race.Name} ({race.DistanceKm} km)\n\n";

        var topResults = allClassifications.OrderBy(c => c.Position).Take(3).ToList();
        summary += "🏆 Top 3 Overall:\n";

        for (int i = 0; i < topResults.Count && i < 3; i++)
        {
            var result = topResults[i];
            var medal = i == 0 ? "🥇" : i == 1 ? "🥈" : "🥉";
            summary += $"{medal} {result.Position}. {result.MemberFirstName} {result.MemberLastName}";

            if (result.RaceTime.HasValue)
            {
                summary += $" - {FormatTimeSpan(result.RaceTime)}";
            }

            summary += "\n";
        }

        var totalParticipants = allClassifications.Count;
        var membersCount = allClassifications.Count(c => c.IsMember);
        var challengersCount = allClassifications.Count(c => c.IsChallenger);

        summary += $"\n👥 Total Participants: {totalParticipants}";
        summary += $"\n🏃 Members: {membersCount}";
        summary += $"\n⭐ Challengers: {challengersCount}";

        return summary;
    }

    private string BuildChallengerResultsSummary(RaceEntity race, List<ClassificationEntity> results)
    {
        var summary = $"⭐ Challenger Results - {race.Name} ({race.DistanceKm} km)\n";
        summary += $"All Challengers Participating in This Race\n\n";

        var sortedResults = results.OrderByDescending(c => c.Points).ThenBy(c => c.Position).ToList();

        if (sortedResults.Count == 0)
        {
            summary += "No challengers participated in this race.\n";
            return summary;
        }

        summary += "🎯 Top Challengers:\n";
        var topCount = Math.Min(10, sortedResults.Count);

        for (int i = 0; i < topCount; i++)
        {
            var result = sortedResults[i];
            var position = result.Position.HasValue ? $"#{result.Position}" : "-";

            summary += $"⭐ {result.MemberFirstName} {result.MemberLastName}: {result.Points} pts ({position})";

            if (result.RaceTime.HasValue)
            {
                summary += $" - {FormatTimeSpan(result.RaceTime)}";
            }

            summary += "\n";
        }

        if (sortedResults.Count > topCount)
        {
            summary += $"\n... and {sortedResults.Count - topCount} more challengers!\n";
        }

        summary += $"\n📈 Total challengers: {sortedResults.Count}";

        return summary;
    }

    private string GenerateCsvContent(RaceEntity race, List<ClassificationEntity> classifications)
    {
        var csv = new System.Text.StringBuilder();
        
        // Header
        csv.AppendLine($"Race: {race.Name}");
        csv.AppendLine($"Year: {race.Year}");
        csv.AppendLine($"Distance: {race.DistanceKm} km");
        csv.AppendLine($"Date: {race.CreatedDate:yyyy-MM-dd}");
        csv.AppendLine();
        
        // Column headers
        csv.AppendLine("Rank,Position,First Name,Last Name,Sex,Pos/Sex,Category,Pos/Cat,Team,Points,Time,Time/km,Speed (km/h),Member,Challenger,Bonus KM");
        
        // Data rows
        foreach (var c in classifications.OrderBy(x => x.Position))
        {
            csv.AppendLine($"{c.Id},{c.Position},{c.MemberFirstName},{c.MemberLastName},{c.Sex},{c.PositionBySex},{c.AgeCategory},{c.PositionByCategory},{c.Team},{c.Points},{FormatTimeSpan(c.RaceTime)},{FormatTimeSpan(c.TimePerKm)},{c.Speed:F2},{(c.IsMember ? "Yes" : "No")},{(c.IsChallenger ? "Yes" : "No")},{c.BonusKm}");
        }
        
        return csv.ToString();
    }

    private string FormatTimeSpan(TimeSpan? timeSpan)
    {
        if (!timeSpan.HasValue)
            return "-";
        
        return timeSpan.Value.ToString(@"hh\:mm\:ss");
    }
}
