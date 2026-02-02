using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using NameParser.Application.Services;
using NameParser.Domain.Repositories;
using NameParser.Infrastructure.Data;
using NameParser.Infrastructure.Data.Models;
using NameParser.Infrastructure.Repositories;
using System.ComponentModel.DataAnnotations;

namespace NameParser.Web.Pages;

public class ManagementModel : PageModel
{
    private readonly ILogger<ManagementModel> _logger;
    private readonly RaceProcessingService _raceProcessingService;
    private readonly RaceRepository _raceRepository;
    private readonly ClassificationRepository _classificationRepository;
    private readonly IWebHostEnvironment _environment;

    public ManagementModel(
        ILogger<ManagementModel> logger,
        RaceProcessingService raceProcessingService,
        RaceRepository raceRepository,
        ClassificationRepository classificationRepository,
        IWebHostEnvironment environment)
    {
        _logger = logger;
        _raceProcessingService = raceProcessingService;
        _raceRepository = raceRepository;
        _classificationRepository = classificationRepository;
        _environment = environment;
    }

    // Upload properties
    [BindProperty]
    [Required]
    public IFormFile UploadedFile { get; set; } = null!;

    [BindProperty]
    [Required]
    public string RaceName { get; set; } = string.Empty;

    [BindProperty]
    public int? Year { get; set; }

    [BindProperty]
    [Required]
    [Range(1, int.MaxValue)]
    public int RaceNumber { get; set; } = 1;

    [BindProperty]
    [Required]
    [Range(1, int.MaxValue)]
    public int DistanceKm { get; set; } = 10;

    [BindProperty]
    public bool IsHorsChallenge { get; set; }

    // Races view properties
    public List<RaceEntity> Races { get; set; } = new();
    public List<ClassificationEntity> Classifications { get; set; } = new();
    public int? SelectedRaceId { get; set; }
    public bool? MemberFilter { get; set; }
    public bool? ChallengerFilter { get; set; }

    // General classification properties
    public List<GeneralClassificationDto> GeneralClassifications { get; set; } = new();
    public int SelectedYear { get; set; } = DateTime.Now.Year;

    // Common properties
    public string StatusMessage { get; set; } = string.Empty;
    public bool IsError { get; set; }
    public string ActiveTab { get; set; } = "upload";
    public List<SelectListItem> Years { get; set; } = new();

    public void OnGet(string? tab, int? raceId, bool? memberFilter, bool? challengerFilter, int? year)
    {
        try
        {
            // Initialize years dropdown
            InitializeYears();

            // Set active tab
            ActiveTab = tab ?? "upload";

            // Load races for races tab
            if (ActiveTab == "races" || raceId.HasValue)
            {
                ActiveTab = "races";
                Races = _raceRepository.GetAllRaces();

                if (raceId.HasValue)
                {
                    SelectedRaceId = raceId;
                    MemberFilter = memberFilter;
                    ChallengerFilter = challengerFilter;

                    Classifications = _classificationRepository.GetClassificationsByRace(
                        raceId.Value,
                        memberFilter,
                        challengerFilter);
                }
            }

            // Load general classification for general tab
            if (ActiveTab == "general")
            {
                SelectedYear = year ?? DateTime.Now.Year;
                GeneralClassifications = _classificationRepository.GetGeneralClassification(SelectedYear);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading management page");
            StatusMessage = $"Error loading data: {ex.Message}";
            IsError = true;
        }
    }

    public async Task<IActionResult> OnPostUploadAsync()
    {
        InitializeYears();
        ActiveTab = "upload";

        if (!ModelState.IsValid)
        {
            StatusMessage = "Please fill in all required fields.";
            IsError = true;
            return Page();
        }

        // Validate Year for non-hors-challenge races
        if (!IsHorsChallenge && !Year.HasValue)
        {
            ModelState.AddModelError("Year", "Year is required for challenge races.");
            StatusMessage = "Please select a year for challenge races.";
            IsError = true;
            return Page();
        }

        try
        {
            // Save uploaded file temporarily
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{Guid.NewGuid()}_{UploadedFile.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await UploadedFile.CopyToAsync(fileStream);
            }

            // Determine repository based on file type
            IRaceResultRepository repository;
            var extension = Path.GetExtension(UploadedFile.FileName).ToLower();

            if (extension == ".pdf")
            {
                repository = new PdfRaceResultRepository();
            }
            else if (extension == ".xlsx")
            {
                repository = new ExcelRaceResultRepository();
            }
            else
            {
                IsError = true;
                StatusMessage = "Unsupported file format. Please upload an Excel (.xlsx) or PDF file.";
                return Page();
            }

            // Save race to database first
            var race = new Domain.Entities.Race(RaceNumber, RaceName, DistanceKm);
            _raceRepository.SaveRace(race, Year, filePath, IsHorsChallenge);

            // Get the saved race ID
            var savedRaces = _raceRepository.GetAllRaces();
            var savedRace = savedRaces.OrderByDescending(r => r.Id).First();

            // Process the race
            var effectiveYear = IsHorsChallenge ? null : Year;
            var classification = await Task.Run(() => _raceProcessingService.ProcessRace(
                filePath,
                RaceName,
                RaceNumber,
                effectiveYear,
                DistanceKm,
                repository
            ));

            // Save classifications
            _classificationRepository.SaveClassifications(savedRace.Id, classification);

            // Update race status
            _raceRepository.UpdateRaceStatus(savedRace.Id, "Processed");

            // Clean up uploaded file
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            StatusMessage = $"Race '{RaceName}' processed successfully! {classification.GetAllClassifications().Count()} participants recorded.";
            IsError = false;

            // Clear form
            ModelState.Clear();
            RaceName = string.Empty;
            Year = null;
            RaceNumber = 1;
            DistanceKm = 10;
            IsHorsChallenge = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing race");
            StatusMessage = $"Error processing race: {ex.Message}";
            IsError = true;
        }

        return Page();
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

            var csvContent = GenerateCsvContent(race, classifications);
            var fileName = $"{race.Name}_{race.Year ?? 0}_Results.csv";

            return File(System.Text.Encoding.UTF8.GetBytes(csvContent), "text/csv", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading race results");
            StatusMessage = $"Error downloading results: {ex.Message}";
            IsError = true;
            ActiveTab = "races";
            return RedirectToPage(new { tab = "races" });
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

        return RedirectToPage(new { tab = "races" });
    }

    private void InitializeYears()
    {
        var currentYear = DateTime.Now.Year;
        Years = Enumerable.Range(currentYear - 5, 11)
            .OrderByDescending(y => y)
            .Select(y => new SelectListItem { Value = y.ToString(), Text = y.ToString() })
            .ToList();
    }

    private string GenerateCsvContent(RaceEntity race, List<ClassificationEntity> classifications)
    {
        var csv = new System.Text.StringBuilder();

        csv.AppendLine($"Race: {race.Name}");
        csv.AppendLine($"Year: {race.Year}");
        csv.AppendLine($"Distance: {race.DistanceKm} km");
        csv.AppendLine($"Date: {race.CreatedDate:yyyy-MM-dd}");
        csv.AppendLine();

        csv.AppendLine("Rank,Position,First Name,Last Name,Sex,Pos/Sex,Category,Pos/Cat,Team,Points,Time,Time/km,Speed (km/h),Member,Challenger,Bonus KM");

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
