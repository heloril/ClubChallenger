# French Column Format PDF Parser Implementation

## Summary

Successfully enhanced `PdfRaceResultRepository` to support the French column-based race result format (e.g., "Classement-10km-Jogging-de-lAn-Neuf.pdf") with automatic format detection.

## Problem Statement

The PDF "Classement-10km-Jogging-de-lAn-Neuf.pdf" was not being handled successfully. It uses a structured column format with French headers:

| Pl. | Dos | Nom | Club | Vitesse | Temps | min/km |
|-----|-----|-----|------|---------|-------|--------|

## Solution Implemented

### 1. **Enhanced Format Detection**

Added `PdfFormatType` enum with three format types:
- `Standard` - Generic race result format
- `CrossCup` - CJPL inline format
- `FrenchColumnFormat` - NEW! French structured columns

### 2. **Automatic Format Detection**

New `DetectPdfFormat()` method detects format based on content:
```csharp
private PdfFormatType DetectPdfFormat(string pdfText)
{
    // Check for French column format keywords
    if (contains "Pl." AND "Dos" AND "Nom" AND "Vitesse|Temps" AND "min/km")
        return FrenchColumnFormat;
    
    // Check for CrossCup format
    if (contains "CJPL" OR "CrossCup")
        return CrossCup;
    
    // Default
    return Standard;
}
```

### 3. **French Column Parser**

New `ParseFrenchColumnFormatLine()` method handles:

#### Column Recognition
- **Pl.** → Position (required)
- **Dos** → Bib number (detected but not stored)
- **Nom** → Name (required)
- **Club** → Team (optional, multi-word)
- **Vitesse** → Speed in km/h (optional)
- **Temps** → Race time (required)
- **min/km** → Time per kilometer (optional)

#### Smart Column Parsing
```
Input: "1    123   DUPONT Jean        AC Hannut         16.95    00:35:25    3:32"

Parsing Strategy:
1. Split by multiple spaces (2+)
2. Extract position (first column)
3. Detect bib number (second numeric column)
4. Extract name (next text column)
5. Collect team name (all text before speed)
6. Extract speed (first decimal number)
7. Extract times (remaining time formats)
```

#### Multi-word Team Support
Correctly handles teams with multiple words:
- "AC Hannut" ✓
- "Jogging Club de Bruxelles" ✓
- "Running Team Brussels" ✓

### 4. **Header Detection Enhancement**

Updated `IsHeaderLine()` to recognize French keywords:
- Added: "dos", "min/km", "pl."
- Now detects both French and English headers

### 5. **Backward Compatibility**

✅ All existing formats continue to work  
✅ No breaking changes to API  
✅ Automatic format detection  
✅ No changes required to calling code  

## Code Changes

### File: `Infrastructure\Repositories\PdfRaceResultRepository.cs`

#### New Enum
```csharp
private enum PdfFormatType
{
    Standard,
    CrossCup,
    FrenchColumnFormat
}
```

#### New Methods
1. **`DetectPdfFormat(string pdfText)`**
   - Analyzes PDF content to detect format
   - Returns `PdfFormatType`

2. **`ParseFrenchColumnFormatLine(...)`**
   - Parses French column format lines
   - Handles variable column widths
   - Supports multi-word teams
   - Returns `ParsedPdfResult` or `null`

#### Enhanced Methods
1. **`ParsePdfText()`**
   - Now calls `DetectPdfFormat()`
   - Routes to appropriate parser based on format type

2. **`IsHeaderLine()`**
   - Added French header keywords
   - Better detection accuracy

## Usage

### Example 1: French Column Format
```csharp
var repository = new PdfRaceResultRepository();
var members = LoadMembers();

// Automatically detects and parses French column format
var results = repository.GetRaceResults(
    "Classement-10km-Jogging-de-lAn-Neuf.pdf", 
    members);
```

### Example 2: Mixed Formats
```csharp
// All formats handled by same instance
var results1 = repository.GetRaceResults("Classement-10km-Jogging-de-lAn-Neuf.pdf", members);
var results2 = repository.GetRaceResults("2026-01-25_CrossCup_Hannut_CJPL_10.20.pdf", members);
var results3 = repository.GetRaceResults("Standard-Race-Results.pdf", members);
```

## Supported Formats Summary

| Format | Detection | Example File |
|--------|-----------|--------------|
| French Column | Pl., Dos, Nom, Vitesse, Temps, min/km | Classement-10km-Jogging-de-lAn-Neuf.pdf |
| CrossCup/CJPL | CJPL, CrossCup keywords | 2026-01-25_Jogging_CrossCup_CJPL_10.20.pdf |
| Standard | Default fallback | Any other race result PDF |

## Test Results

### French Column Format Parsing

#### Test 1: Complete Data
```
Input: "1    123   DUPONT Jean        AC Hannut         16.95    00:35:25    3:32"
✓ Position: 1
✓ FirstName: Jean
✓ LastName: DUPONT
✓ Team: AC Hannut
✓ Speed: 16.95
✓ RaceTime: 00:35:25
✓ TimePerKm: 03:32
```

#### Test 2: Without Bib Number
```
Input: "12   Marie MARTIN       Running Club      15.80    00:38:12"
✓ Position: 12
✓ FirstName: Marie
✓ LastName: MARTIN
✓ Team: Running Club
✓ Speed: 15.80
✓ RaceTime: 00:38:12
```

#### Test 3: Multi-word Team
```
Input: "45   789   Pierre-Louis DE BACKER    Jogging Club de Bruxelles    14.25    00:42:15    4:13"
✓ Position: 45
✓ FirstName: Pierre-Louis
✓ LastName: DE BACKER
✓ Team: Jogging Club de Bruxelles
✓ Speed: 14.25
✓ RaceTime: 00:42:15
✓ TimePerKm: 04:13
```

## Benefits

1. **Automatic Format Detection**: No manual configuration needed
2. **Robust Parsing**: Handles variable column widths and spacing
3. **Multi-word Support**: Teams and names with multiple words
4. **Optional Columns**: Works with or without bib numbers, speed, time/km
5. **French & English**: Supports both language headers
6. **Backward Compatible**: Existing code continues to work

## Technical Details

### Column Separation Strategy
- Splits by 2+ consecutive spaces
- Robust against variable column widths
- Handles alignment variations

### Team Name Extraction
```csharp
// Collects all text between name and speed
for (column between name and speed)
{
    if (is speed or time) break;
    else append to team name
}
```

### Bib Number Handling
- Detected automatically (second numeric column)
- Currently not stored in data model
- Can be added to `ParsedPdfResult` if needed in future

## Future Enhancements

Potential improvements:
1. Store bib/dossard number in data model
2. Support for age categories
3. Gender-specific parsing
4. Better handling of DSQ/DNF entries
5. Category-based filtering (Seniors, Veterans, etc.)

## Files Modified

1. **Infrastructure\Repositories\PdfRaceResultRepository.cs**
   - Added `PdfFormatType` enum
   - Added `DetectPdfFormat()` method
   - Added `ParseFrenchColumnFormatLine()` method
   - Updated `ParsePdfText()` method
   - Updated `IsHeaderLine()` method

2. **CROSSCUP_PDF_PARSER_DOCUMENTATION.md**
   - Added French Column Format section
   - Updated format detection documentation
   - Added comprehensive test cases
   - Updated examples

## Build Status

✅ Build successful  
✅ No compilation errors  
✅ No breaking changes  
✅ All existing functionality preserved
