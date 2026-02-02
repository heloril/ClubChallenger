using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using NameParser.Application.Services;
using NameParser.Domain.Repositories;
using NameParser.Infrastructure.Repositories;
using System.ComponentModel.DataAnnotations;

namespace NameParser.Web.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly RaceProcessingService _raceProcessingService;
    private readonly IWebHostEnvironment _environment;

    public IndexModel(
        ILogger<IndexModel> logger,
        RaceProcessingService raceProcessingService,
        IWebHostEnvironment environment)
    {
        _logger = logger;
        _raceProcessingService = raceProcessingService;
        _environment = environment;
    }

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

    public string? StatusMessage { get; set; }
    public bool IsError { get; set; }

    public SelectList Years { get; set; } = null!;

    public void OnGet()
    {
        InitializeYears();
        Year = DateTime.Now.Year;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        InitializeYears();

        // Validate year requirement (unless Hors Challenge)
        if (!IsHorsChallenge && (!Year.HasValue || Year.Value <= 0))
        {
            ModelState.AddModelError(nameof(Year), "Year is required unless race is marked as Hors Challenge");
        }

        if (!ModelState.IsValid)
        {
            IsError = true;
            StatusMessage = "Please correct the errors below.";
            return Page();
        }

        try
        {
            // Save uploaded file temporarily
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}_{UploadedFile.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await UploadedFile.CopyToAsync(stream);
            }

            // Determine repository based on file type
            IRaceResultRepository repository;
            var extension = Path.GetExtension(UploadedFile.FileName).ToLowerInvariant();

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

            // Process the race
            var effectiveYear = IsHorsChallenge ? null : Year;
            await Task.Run(() => _raceProcessingService.ProcessRace(
                filePath,
                RaceName,
                RaceNumber,
                effectiveYear,
                DistanceKm,
                repository
            ));

            // Clean up uploaded file
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            StatusMessage = $"Race '{RaceName}' processed successfully!";
            IsError = false;

            // Clear form
            ModelState.Clear();
            RaceName = string.Empty;
            RaceNumber = 1;
            DistanceKm = 10;
            IsHorsChallenge = false;
            Year = DateTime.Now.Year;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing race");
            IsError = true;
            StatusMessage = $"Error processing race: {ex.Message}";
        }

        return Page();
    }

    private void InitializeYears()
    {
        var years = Enumerable.Range(2020, 11).Select(y => new SelectListItem
        {
            Value = y.ToString(),
            Text = y.ToString()
        }).ToList();

        Years = new SelectList(years, "Value", "Text");
    }
}
