# ACN Timing Integration

This document describes how to use the ACN Timing parser to fetch and parse race results from the ACN Timing website.

## Overview

The ACN Timing integration supports three methods of parsing race results:

1. **Direct URL fetching** - Automatically downloads and parses results from ACN Timing URLs
2. **Cached JSON files** - Parse previously downloaded JSON results
3. **Cached HTML files** - Parse previously downloaded HTML results

## ACN Timing URL Format

ACN Timing URLs typically follow this format:
```
https://www.acn-timing.com/?lng=FR#/events/{eventId}/ctx/{date}_{location}/generic/{raceId}/home/{view}
```

Example:
```
https://www.acn-timing.com/?lng=FR#/events/2156215316811398/ctx/20260328_spa/generic/198022_3/home/LIVEMARA3
```

## Usage Examples

### 1. Using Direct URL (Recommended)

```csharp
using NameParser.Infrastructure.Repositories;
using NameParser.Domain.Repositories;

// Create the ACN Timing repository
IRaceResultRepository repository = new AcnTimingRaceResultRepository();

// Fetch and parse results directly from URL
string url = "https://www.acn-timing.com/?lng=FR#/events/2156215316811398/ctx/20260328_spa/generic/198022_3/home/LIVEMARA3";
var results = repository.GetRaceResults(url, members);
```

### 2. Download and Cache Results First

```csharp
using NameParser.Infrastructure.Services;
using NameParser.Infrastructure.Repositories;

// Download and save results
var downloader = new AcnTimingDownloader();
string cachedFilePath = await downloader.DownloadRaceResultsAsync(url);
// Or synchronously:
// string cachedFilePath = downloader.DownloadRaceResults(url);

// Parse the cached file
IRaceResultRepository repository = new AcnTimingRaceResultRepository();
var results = repository.GetRaceResults(cachedFilePath, members);
```

### 3. Parse Existing Cached Files

```csharp
// If you already have a downloaded JSON or HTML file
IRaceResultRepository repository = new AcnTimingRaceResultRepository();
var results = repository.GetRaceResults("ACN_20260328_spa_198022_3_20260328_143022.json", members);
```

### 4. Using with RaceProcessingService

```csharp
using NameParser.Application.Services;
using NameParser.Infrastructure.Repositories;

var memberRepository = new JsonMemberRepository("Members.json");
var acnRepository = new AcnTimingRaceResultRepository();
var pointsCalculationService = new PointsCalculationService();

var raceProcessingService = new RaceProcessingService(
    memberRepository,
    acnRepository,
    pointsCalculationService);

// Process race with ACN Timing URL
var classification = raceProcessingService.ProcessRace(
    "https://www.acn-timing.com/?lng=FR#/events/2156215316811398/ctx/20260328_spa/generic/198022_3/home/LIVEMARA3",
    "Marathon de Spa",
    10,
    2026,
    42,
    acnRepository);
```

## Supported Data Fields

The ACN Timing parser extracts the following information when available:

- Position (overall ranking)
- Full name
- First name / Last name (when available)
- Team/Club
- Race time
- Pace (time per km)
- Speed (km/h)
- Sex (M/F/H/D)
- Sex position
- Age category
- Category position
- Member matching

## API vs HTML Parsing

The repository automatically attempts to use JSON API endpoints first (more reliable and structured), and falls back to HTML parsing if the API is not available.

### API Parsing (Preferred)
- More reliable and structured
- Faster processing
- Better field extraction

### HTML Parsing (Fallback)
- Works when API is not available
- May be less accurate for complex layouts
- Requires more pattern matching

## File Naming Convention

When downloading results, files are automatically named with the following pattern:
```
ACN_{location}_{raceId}_{timestamp}.{json|html}
```

Example:
```
ACN_20260328_spa_198022_3_20260328_143022.json
```

## Troubleshooting

### URL Not Working
- Verify the URL format is correct
- Check if the event/race ID is valid
- Try downloading the file manually first and parse it locally

### No Results Found
- The ACN Timing website may use JavaScript rendering
- Try downloading the page manually and saving as HTML
- Check if the table structure matches expected patterns

### JSON Parsing Errors
- The API response format may vary between events
- Save the JSON response and examine its structure
- The parser attempts multiple common field name variations

## Integration with Existing Code

The `AcnTimingRaceResultRepository` implements the same `IRaceResultRepository` interface as the existing PDF and Excel repositories, so it can be used interchangeably:

```csharp
// Choose repository based on file type or source
IRaceResultRepository repository;

if (source.EndsWith(".pdf"))
    repository = new PdfRaceResultRepository();
else if (source.EndsWith(".xlsx"))
    repository = new ExcelRaceResultRepository();
else if (source.StartsWith("http") || source.EndsWith(".json") || source.EndsWith(".html"))
    repository = new AcnTimingRaceResultRepository();
else
    throw new Exception("Unsupported format");

var results = repository.GetRaceResults(source, members);
```

## Advanced Configuration

### Custom HTTP Headers
If you need to modify HTTP headers for authentication or other purposes, modify the static constructor in `AcnTimingRaceResultRepository`:

```csharp
static AcnTimingRaceResultRepository()
{
    _httpClient.DefaultRequestHeaders.Add("User-Agent", "Your-User-Agent");
    _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer your-token");
}
```

### Timeout Configuration
Adjust the download timeout in `AcnTimingDownloader`:

```csharp
_httpClient.Timeout = TimeSpan.FromSeconds(60); // Increase timeout
```

## Notes

- The ACN Timing website structure may change over time, requiring updates to the parser
- Some events may require authentication or have restricted access
- Network connectivity is required for direct URL fetching
- Large result sets may take longer to download and parse
