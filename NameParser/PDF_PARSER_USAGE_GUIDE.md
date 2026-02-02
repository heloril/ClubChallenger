# PDF Parser Usage Guide

## ✅ Implementation Complete

The PDF parser (`PdfRaceResultRepository`) is ready to use!

## 🚀 Basic Usage

### 1. Using PDF Parser Directly

```csharp
using NameParser.Infrastructure.Repositories;

// Create the PDF parser
var pdfParser = new PdfRaceResultRepository();

// Get members list
var memberRepository = new JsonMemberRepository("Members.json");
var members = memberRepository.GetMembersWithLastName();

// Parse PDF file
var results = pdfParser.GetRaceResults("Courses/Classement-10km-Jogging-de-lAn-Neuf.pdf", members);

// Results is a Dictionary<int, string> with:
// 0: Header
// 1: TREF (if found in PDF)
// 2+: Race results
```

### 2. With Race Processing Service

```csharp
using NameParser.Application.Services;
using NameParser.Domain.Services;
using NameParser.Infrastructure.Repositories;

// Create dependencies
var memberRepository = new JsonMemberRepository("Members.json");
var pdfParser = new PdfRaceResultRepository();
var pointsService = new PointsCalculationService();

// Create race processing service
var raceService = new RaceProcessingService(
    memberRepository,
    pdfParser,  // Use PDF parser
    pointsService);

// Process PDF file
var classification = raceService.ProcessAllRaces(
    new[] { "Courses/Classement-10km-Jogging-de-lAn-Neuf.pdf" });

// Classification now contains all results with calculated points
```

### 3. Auto File Type Detection

```csharp
using System.IO;
using NameParser.Domain.Repositories;
using NameParser.Infrastructure.Repositories;

public IRaceResultRepository GetParser(string filePath)
{
    var extension = Path.GetExtension(filePath).ToLowerInvariant();
    
    switch (extension)
    {
        case ".pdf":
            return new PdfRaceResultRepository();
        
        case ".xlsx":
        case ".xls":
            return new ExcelRaceResultRepository();
        
        default:
            throw new NotSupportedException(
                $"File type {extension} is not supported. " +
                "Supported: .pdf, .xlsx, .xls");
    }
}

// Usage
var parser = GetParser(selectedFile);
var results = parser.GetRaceResults(selectedFile, members);
```

### 4. Processing Multiple Files

```csharp
public void ProcessRaceFiles(string[] filePaths)
{
    var memberRepository = new JsonMemberRepository("Members.json");
    var pointsService = new PointsCalculationService();
    
    foreach (var filePath in filePaths)
    {
        try
        {
            // Get appropriate parser
            var parser = GetParser(filePath);
            
            // Create service
            var service = new RaceProcessingService(
                memberRepository,
                parser,
                pointsService);
            
            // Process
            var classification = service.ProcessAllRaces(new[] { filePath });
            
            Console.WriteLine($"✓ Processed: {Path.GetFileName(filePath)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error processing {filePath}: {ex.Message}");
        }
    }
}

// Usage
var files = new[]
{
    "Courses/01.10.Cointe.xlsx",
    "Courses/Classement-10km-Jogging-de-lAn-Neuf.pdf",
    "Courses/02.10.Geer.xlsx"
};

ProcessRaceFiles(files);
```

### 5. Updating UI File Dialog

```csharp
// In MainViewModel or file selection code
private void ExecuteUploadFile(object parameter)
{
    var openFileDialog = new OpenFileDialog
    {
        Filter = "Race Files (*.xlsx;*.pdf)|*.xlsx;*.pdf|" +
                 "Excel Files (*.xlsx)|*.xlsx|" +
                 "PDF Files (*.pdf)|*.pdf|" +
                 "All Files (*.*)|*.*",
        Title = "Select Race Result File"
    };

    if (openFileDialog.ShowDialog() == true)
    {
        SelectedFilePath = openFileDialog.FileName;
        
        // Determine file type
        var extension = Path.GetExtension(SelectedFilePath).ToLowerInvariant();
        StatusMessage = extension == ".pdf" 
            ? "PDF file selected" 
            : "Excel file selected";
    }
}
```

### 6. In Race Processing

```csharp
// Update ProcessRaceCommand execution
private async void ExecuteProcessRace(object parameter)
{
    IsProcessing = true;
    StatusMessage = "Processing race...";

    try
    {
        await Task.Run(() =>
        {
            var race = new Race(RaceNumber, RaceName, DistanceKm);
            _raceRepository.SaveRace(race, Year, SelectedFilePath);

            var memberRepository = new JsonMemberRepository("Members.json");
            
            // Auto-select parser based on file type
            IRaceResultRepository raceResultRepository;
            var extension = Path.GetExtension(SelectedFilePath).ToLowerInvariant();
            
            if (extension == ".pdf")
            {
                raceResultRepository = new PdfRaceResultRepository();
            }
            else
            {
                raceResultRepository = new ExcelRaceResultRepository();
            }
            
            var pointsCalculationService = new PointsCalculationService();

            var raceProcessingService = new RaceProcessingService(
                memberRepository,
                raceResultRepository,
                pointsCalculationService);

            var classification = raceProcessingService.ProcessAllRaces(
                new[] { SelectedFilePath });

            // Save classification...
        });

        StatusMessage = "Race processed successfully!";
    }
    catch (Exception ex)
    {
        StatusMessage = $"Error: {ex.Message}";
    }
    finally
    {
        IsProcessing = false;
    }
}
```

## 📊 Expected PDF Format

```
CLASSEMENT 10KM - JOGGING DE L'AN NEUF

Place  Nom                    Temps    Vitesse  Equipe
─────  ──────────────────────  ───────  ───────  ──────────
1      DUPONT Jean            00:35:25  16.95    Team A
2      MARTIN Sophie          00:37:12  16.13    Team B
3      BERNARD Luc            00:38:45  15.48    (CLUB X)
```

**Requirements:**
- Position at start of line
- Time in HH:MM:SS or MM:SS format
- Text-based PDF (not scanned image)

## 🎯 What Gets Extracted

| Data | Example | Required |
|------|---------|----------|
| Position | 1, 2, 3... | ✅ Yes |
| Name | DUPONT Jean | ✅ Yes |
| Time | 00:35:25 | ✅ Yes |
| Speed | 16.95 km/h | ❌ No |
| Team | (Team A) | ❌ No |

## ✅ Benefits

- **Same Interface:** Works with existing `RaceProcessingService`
- **Same Output:** Compatible with existing code
- **Member Matching:** Automatic matching with registered members
- **Flexible:** Handles various PDF layouts
- **Error Handling:** Graceful failure for missing data

## 🚨 Error Handling

```csharp
try
{
    var pdfParser = new PdfRaceResultRepository();
    var results = pdfParser.GetRaceResults(pdfPath, members);
}
catch (FileNotFoundException ex)
{
    Console.WriteLine($"PDF file not found: {ex.Message}");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Failed to parse PDF: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected error: {ex.Message}");
}
```

## 📦 Dependencies

**Installed Package:**
```xml
<PackageReference Include="itext7" Version="8.0.5" />
```

**Required Namespaces:**
```csharp
using NameParser.Infrastructure.Repositories;
using NameParser.Domain.Repositories;
using NameParser.Application.Services;
```

## 🔍 Testing

**Test PDF:** `Courses/Classement-10km-Jogging-de-lAn-Neuf.pdf`

```csharp
// Simple test
var parser = new PdfRaceResultRepository();
var members = new JsonMemberRepository("Members.json").GetMembersWithLastName();
var results = parser.GetRaceResults(
    "Courses/Classement-10km-Jogging-de-lAn-Neuf.pdf", 
    members);

Console.WriteLine($"Extracted {results.Count} entries");
```

## 📝 Summary

| Feature | Status |
|---------|--------|
| **Implementation** | ✅ Complete |
| **Interface** | ✅ IRaceResultRepository |
| **Build** | ✅ Successful |
| **Documentation** | ✅ Complete |
| **Ready to Use** | ✅ Yes |

---

**Use `PdfRaceResultRepository` exactly like `ExcelRaceResultRepository` - same interface, same workflow!**
