using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NameParser.Infrastructure.Data;
using NameParser.Infrastructure.Data.Models;

namespace NameParser.Web.Pages;

public class RacesModel : PageModel
{
    private readonly RaceRepository _raceRepository;
    private readonly ClassificationRepository _classificationRepository;
    private readonly ILogger<RacesModel> _logger;

    public RacesModel(
        RaceRepository raceRepository,
        ClassificationRepository classificationRepository,
        ILogger<RacesModel> logger)
    {
        _raceRepository = raceRepository;
        _classificationRepository = classificationRepository;
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
