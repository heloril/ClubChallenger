# PDF Parser - Quick Reference

## 🚀 Quick Start

```csharp
// Create PDF parser
var pdfParser = new PdfRaceResultRepository();

// Get members
var members = memberRepository.GetMembersWithLastName();

// Extract results from PDF
var results = pdfParser.GetRaceResults("path/to/race.pdf", members);

// Process with existing service
var service = new RaceProcessingService(
    memberRepository,
    pdfParser,  // Use PDF parser instead of Excel
    pointsCalculationService);

var classification = service.ProcessAllRaces(new[] { "race.pdf" });
```

## 📦 Installation

```bash
dotnet add package itext7 --version 8.0.5
```

## 📊 What It Extracts

| Data | Example | Pattern |
|------|---------|---------|
| **Position** | 1, 2, 3... | `^\d+[\s\.]+` |
| **Name** | DUPONT Jean | After position |
| **Time** | 00:35:25 | `\d{1,2}:\d{2}:\d{2}` |
| **Speed** | 16.95 km/h | `\d+[\.,]\d+` |
| **Team** | (Team A) | In parentheses/brackets |

## 🎯 PDF Format Expected

```
Place  Nom              Temps     Vitesse  Equipe
────   ─────────────    ───────   ───────  ──────
1      DUPONT Jean      00:35:25  16.95    Team A
2      MARTIN Sophie    00:37:12  16.13    Team B
```

## ✅ Features

- ✅ Multi-page PDF support
- ✅ Member matching (diacritic-insensitive)
- ✅ Multiple name formats (LAST First, First Last)
- ✅ Multiple time formats (HH:MM:SS, MM:SS)
- ✅ Speed with/without km/h unit
- ✅ Team in parentheses or brackets
- ✅ Compatible with existing RaceProcessingService
- ✅ Same output format as Excel parser

## 🔧 File Type Detection

```csharp
IRaceResultRepository GetRepository(string filePath)
{
    var extension = Path.GetExtension(filePath).ToLowerInvariant();
    
    return extension switch
    {
        ".pdf" => new PdfRaceResultRepository(),
        ".xlsx" => new ExcelRaceResultRepository(),
        _ => throw new NotSupportedException($"File type {extension} not supported")
    };
}
```

## 📝 Output Format

Same as Excel parser:
```
TMEM;1;DUPONT;Jean;00:35:25;RACETYPE;RACE_TIME;RACETIME;00:35:25;POS;1;TEAM;Team A;SPEED;16.95;ISMEMBER;1;
```

## ⚠️ Requirements

- **Text-based PDF** (not scanned images)
- **Position at line start** (e.g., "1 Name...")
- **Time in standard format** (HH:MM:SS or MM:SS)

## 🐛 Troubleshooting

| Issue | Solution |
|-------|----------|
| No results | Check PDF has selectable text |
| Wrong names | Verify name format matches pattern |
| No member match | Check Members.json spelling |
| Missing speed/team | Optional - still processes |

## 📂 File Locations

- **Implementation:** `Infrastructure/Repositories/PdfRaceResultRepository.cs`
- **Interface:** `Domain/Repositories/IRaceResultRepository.cs`
- **Example PDF:** `Courses/Classement-10km-Jogging-de-lAn-Neuf.pdf`

## 🎓 Key Differences: PDF vs Excel

| | PDF | Excel |
|---|-----|-------|
| **Extraction** | Text parsing | Cell reading |
| **Accuracy** | Good | Excellent |
| **Speed** | Moderate | Fast |
| **Flexibility** | Regex patterns | Column-based |

## ✅ Status

- Build: **SUCCESSFUL** ✅
- Package: **iText7 8.0.5** installed ✅
- Interface: **IRaceResultRepository** implemented ✅
- Ready for: **Testing and Use** ✅

---

**Use `PdfRaceResultRepository` just like `ExcelRaceResultRepository` - same interface, same workflow!**
