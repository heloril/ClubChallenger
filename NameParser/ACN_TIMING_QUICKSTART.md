# ACN Timing Integration - Quick Start Guide

## What's New

The ClubChallenger NameParser now supports parsing race results from **ACN Timing** website in addition to PDF and Excel files!

## ACN Timing Support

ACN Timing (https://www.acn-timing.com) is a popular timing system used for races in Belgium and France. You can now parse results directly from their website URLs or from downloaded HTML/JSON files.

## Quick Start

### 1. Direct URL Processing (Easiest)

```csharp
using NameParser.Infrastructure.Repositories;

// Create the repository
var repository = new AcnTimingRaceResultRepository();

// Parse results from URL
string url = "https://www.acn-timing.com/?lng=FR#/events/2156215316811398/ctx/20260328_spa/generic/198022_3/home/LIVEMARA3";
var results = repository.GetRaceResults(url, members);
```

### 2. Download Results First (Recommended for Offline Processing)

```csharp
using NameParser.Infrastructure.Services;
using NameParser.Infrastructure.Repositories;

// Download and cache
var downloader = new AcnTimingDownloader();
string cachedFile = downloader.DownloadRaceResults(url);

// Process cached file later
var repository = new AcnTimingRaceResultRepository();
var results = repository.GetRaceResults(cachedFile, members);
```

### 3. Interactive Download Utility

```csharp
// In your code, call the download utility
await AcnTimingDownloadUtility.RunDownloader(args);

// Or use the synchronous version
var downloader = new AcnTimingDownloader();
downloader.DownloadRaceResults(url, "my_race_results.json");
```

## URL Format

ACN Timing URLs look like this:
```
https://www.acn-timing.com/?lng=FR#/events/{eventId}/ctx/{date}_{location}/generic/{raceId}/home/{view}
```

Example:
```
https://www.acn-timing.com/?lng=FR#/events/2156215316811398/ctx/20260328_spa/generic/198022_3/home/LIVEMARA3
```

## Supported Features

✓ **Direct URL fetching** - Automatically downloads and parses  
✓ **JSON API parsing** - Preferred method, more reliable  
✓ **HTML parsing** - Fallback when API not available  
✓ **Cached files** - Process previously downloaded results  
✓ **Member matching** - Automatically identifies club members  
✓ **Complete race data** - Position, times, pace, speed, category info  

## Integration with Existing Code

The `AcnTimingRaceResultRepository` implements the same `IRaceResultRepository` interface as PDF and Excel repositories, so you can use it interchangeably:

```csharp
IRaceResultRepository repository;

if (source.EndsWith(".pdf"))
    repository = new PdfRaceResultRepository();
else if (source.EndsWith(".xlsx"))
    repository = new ExcelRaceResultRepository();
else if (source.StartsWith("http") || source.EndsWith(".json") || source.EndsWith(".html"))
    repository = new AcnTimingRaceResultRepository();

var results = repository.GetRaceResults(source, members);
```

## Using with RaceProcessingService

```csharp
var acnRepository = new AcnTimingRaceResultRepository();
var raceProcessingService = new RaceProcessingService(
    memberRepository,
    acnRepository,
    pointsCalculationService);

var classification = raceProcessingService.ProcessRace(
    "https://www.acn-timing.com/?lng=FR#/events/2156215316811398/ctx/20260328_spa/generic/198022_3/home/LIVEMARA3",
    "Marathon de Spa",
    10,
    2026,
    42,
    acnRepository);
```

## Example Output Format

Results are returned in the same format as PDF/Excel parsers:

```
Position;Nom complet;Prénom;Nom;Équipe;Temps;Temps/km;Vitesse;IsMember;Sexe;Position Sexe;Catégorie;Position Catégorie
1;Jean Dupont;Jean;Dupont;Club Challenger;1:45:30;5:02;14.5;True;H;1;Senior H;1
2;Marie Martin;Marie;Martin;Running Team;1:50:15;5:15;13.8;False;D;1;Senior D;1
```

## Tips & Best Practices

### 1. Cache Results for Offline Processing
Download results once and process multiple times:
```csharp
var downloader = new AcnTimingDownloader();
var cachedFile = downloader.DownloadRaceResults(url);
// Now you can process this file anytime without internet
```

### 2. Batch Download Multiple Races
```csharp
var urls = new[] {
    "https://www.acn-timing.com/.../race1/...",
    "https://www.acn-timing.com/.../race2/...",
    "https://www.acn-timing.com/.../race3/..."
};

foreach (var url in urls)
{
    var file = downloader.DownloadRaceResults(url);
    Console.WriteLine($"Downloaded: {file}");
}
```

### 3. Handle Network Errors
```csharp
try
{
    var results = repository.GetRaceResults(url, members);
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"Network error: {ex.Message}");
    // Fallback to cached file if available
}
```

## File Naming

Downloaded files are automatically named:
```
ACN_{location}_{raceId}_{timestamp}.{json|html}
```

Example:
```
ACN_20260328_spa_198022_3_20260328_143022.json
```

## What's Extracted

From ACN Timing results, the parser extracts:

- **Position** - Overall ranking
- **Full Name** - Participant's full name
- **First Name / Last Name** - When available
- **Team/Club** - Organization affiliation
- **Race Time** - Total time to complete the race
- **Pace** - Time per kilometer
- **Speed** - Average speed in km/h
- **Sex** - M/F/H/D gender classification
- **Sex Position** - Ranking within gender
- **Age Category** - Age group classification
- **Category Position** - Ranking within age group
- **Member Flag** - True if matched to club member list

## Troubleshooting

### "URL Not Working"
- Check the URL format is correct
- Verify the race event is still accessible
- Try downloading manually and saving as HTML

### "No Results Found"
- The page may require JavaScript rendering
- Try accessing the URL in a browser first
- Download the page source and save it locally

### "JSON Parsing Error"
- The API response format may vary
- Save the response and check its structure
- Report the issue with the JSON sample

## API vs HTML

The parser tries **API endpoints first** (JSON), then falls back to HTML if needed:

- ✓ **API/JSON** - More reliable, structured, faster
- ↻ **HTML** - Fallback, may need adjustment for layout changes

## Learn More

For detailed documentation and advanced usage, see:
- `Documentation/ACN_Timing_Integration.md` - Complete documentation
- `Examples/AcnTimingIntegrationExample.cs` - Code examples
- `Examples/ACN_Timing_Race_Configuration.json` - Configuration example

## Support

If you encounter issues with specific ACN Timing races:

1. Download the page source (HTML or JSON)
2. Check the structure/format
3. Adjust the parser if needed
4. Report unusual formats for future support

## Next Steps

1. Try the examples in `Examples/AcnTimingIntegrationExample.cs`
2. Download results from your next race
3. Process them alongside your PDF and Excel files
4. Enjoy automatic member matching and point calculation!
