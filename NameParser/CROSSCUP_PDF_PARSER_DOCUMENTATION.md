# PDF Race Result Parser - Multiple Format Support

## Overview
Enhanced the `PdfRaceResultRepository` to support multiple PDF formats with automatic format detection:
1. **CrossCup/CJPL Format** - Inline format with teams in parentheses
2. **French Column Format** - Structured columns (Pl., Dos, Nom, Club, Vitesse, Temps, min/km)
3. **Standard Format** - Generic race result format

## Automatic Format Detection

The parser detects the format automatically by analyzing the PDF content:
- **French Column Format**: Presence of "Pl.", "Dos", "Nom", "Vitesse", "Temps", "min/km"
- **CrossCup Format**: Presence of "CJPL", "CrossCup", or CJPL in filename category
- **Standard Format**: Default fallback for other formats

## Automatic Format Detection

The parser detects the format automatically by analyzing the PDF content:
- **French Column Format**: Presence of "Pl.", "Dos", "Nom", "Vitesse", "Temps", "min/km"
- **CrossCup Format**: Presence of "CJPL", "CrossCup", or CJPL in filename category
- **Standard Format**: Default fallback for other formats

## Supported Filename Formats

### CrossCup Pattern
```
YYYY-MM-DD_RaceName_Location_Category_Distance.pdf
```

### Example
```
2026-01-25_Jogging de la CrossCup_Hannut_CJPL_10.20.pdf
```

### French Column Format Pattern
```
Classement-Distance-RaceName.pdf
```

### Example
```
Classement-10km-Jogging-de-lAn-Neuf.pdf
```

### Extracted Metadata
- **Date**: 2026-01-25
- **Race Name**: Jogging de la CrossCup
- **Location**: Hannut
- **Category**: CJPL
- **Distance**: 10.20 km

---

## Format 1: French Column Format

### Column Structure
```
Pl.  Dos   Nom                Club              Vitesse  Temps       min/km
1    123   DUPONT Jean        AC Hannut         16.95    00:35:25    3:32
2    456   MARTIN Marie       Running Club      15.80    00:38:12    3:49
```

### Column Definitions
- **Pl.** (Place): Position/Rank (1, 2, 3, ...)
- **Dos**: Bib/Dossard number (currently parsed but not stored)
- **Nom**: Participant name (FirstName LASTNAME or LASTNAME FirstName)
- **Club**: Team/Club name
- **Vitesse**: Speed in km/h (e.g., 16.95)
- **Temps**: Race time in HH:MM:SS or MM:SS format
- **min/km**: Time per kilometer in MM:SS format

### Parsing Rules

#### Position Extraction
- First column value
- Must be a valid integer

#### Bib Number (Dos)
- Second column value
- Numeric value
- Currently detected but not stored in data model

#### Name Extraction
- Follows bib number
- Format: Can be "LASTNAME FirstName" or "FirstName LASTNAME"
- Uses capitalization heuristics for splitting

#### Team/Club Extraction
- One or more columns between name and speed
- All non-numeric text before speed value
- Can span multiple words (e.g., "Jogging Club de Bruxelles")

#### Speed Extraction
- First numeric value with decimal separator
- Format: `##.##` (supports both `.` and `,`)
- Units: km/h (optional in text)

#### Time Extraction
- **Race Time**: First time value (HH:MM:SS or MM:SS)
- **Time per km**: Second time value (MM:SS), must be < 10 minutes

### Example Lines

#### With All Data
```
Input: "1    123   Jean DUPONT        AC Hannut         16.95    00:35:25    3:32"
Output:
- Position: 1
- Bib: 123
- FirstName: Jean
- LastName: DUPONT
- Team: AC Hannut
- Speed: 16.95
- RaceTime: 00:35:25
- TimePerKm: 03:32
```

#### Without Bib Number
```
Input: "1   DUPONT Jean        AC Hannut         16.95    00:35:25    3:32"
Output:
- Position: 1
- FirstName: Jean
- LastName: DUPONT
- Team: AC Hannut
- Speed: 16.95
- RaceTime: 00:35:25
- TimePerKm: 03:32
```

#### Multi-word Team Name
```
Input: "12   456   Marie MARTIN       Jogging Club Bruxelles    15.80    00:38:12"
Output:
- Position: 12
- Bib: 456
- FirstName: Marie
- LastName: MARTIN
- Team: Jogging Club Bruxelles
- Speed: 15.80
- RaceTime: 00:38:12
```

---

## Format 2: CrossCup/CJPL Format

---

## Format 2: CrossCup/CJPL Format

### Detection Criteria
- Presence of "CJPL" in PDF content
- Presence of "CrossCup" or "Cross Cup" in PDF content
- Category extracted from filename contains "CJPL"

### Typical Line Format
```
Position FirstName LASTNAME (Team) Time Speed
```

### Examples
```
1 Jean DUPONT (AC Hannut) 00:35:25 16.95
12 Marie MARTIN 00:38:12 15.80
45 Pierre-Louis DE BACKER (Running Club) 00:42:15 14.25
```

### Parsing Rules

#### Name Extraction
- **Format**: `FirstName LASTNAME` (last name often in UPPERCASE)
- **Multi-word first names**: Supported (e.g., "Pierre-Louis")
- **Multi-word last names**: Supported (e.g., "DE BACKER")
- **Logic**: If last word is ALL CAPS → LastName, everything before → FirstName

#### Team Extraction
- Teams are enclosed in parentheses: `(Team Name)`
- Also supports brackets: `[Team Name]`
- Removed from name before name parsing

#### Time Extraction
- **Race Time**: Primary time in format `HH:MM:SS` or `MM:SS`
- **Time per KM**: Secondary time (if present) < 10 minutes
- Removed from line after extraction to isolate name and team

#### Speed Extraction
- Format: `##.## km/h` or just `##.##`
- Decimal separator: Supports both `.` and `,`
- Usually last numeric value on the line

#### Position Extraction
- First number on the line
- Format: `123` or `123.` or `123 `

## Member Matching

### Registered Members
If a participant matches a registered member:
- Uses member's official FirstName and LastName
- Sets `IsMember = true`
- Matching is diacritic-insensitive and case-insensitive

### Non-Members
If no match found:
- Extracts FirstName and LastName from full name
- Sets `IsMember = false`
- Uses intelligent name parsing based on capitalization

## Code Structure

### New Classes

#### `RaceMetadata`
```csharp
private class RaceMetadata
{
    public DateTime? RaceDate { get; set; }
    public string RaceName { get; set; }
    public string Location { get; set; }
    public string Category { get; set; }
    public double? DistanceKm { get; set; }
}
```

### New Methods

1. **`ExtractMetadataFromFilename(string filePath)`**
   - Extracts race metadata from filename
   - Returns `RaceMetadata` object

2. **`IsCrossCupFormat(string pdfText)`**
   - Detects if PDF is in CrossCup format
   - Returns `bool`

3. **`ParseCrossCupResultLine(...)`**
   - Parses a single result line in CrossCup format
   - Returns `ParsedPdfResult` or `null`

4. **`ExtractNamePartsCrossCup(string fullName)`**
   - Splits full name into first and last name
   - Uses capitalization heuristics
   - Returns `(firstName, lastName)` tuple

### Enhanced Methods

**`GetRaceResults()`**
- Now extracts metadata from filename before parsing
- Stores in `_raceMetadata` field

**`ParsePdfText()`**
- Detects format type (CrossCup vs standard)
- Routes to appropriate parser method

## Usage Example

```csharp
var repository = new PdfRaceResultRepository();
var members = LoadMembers(); // Your member list

// Automatically handles CrossCup format
var results = repository.GetRaceResults(
    "2026-01-25_Jogging de la CrossCup_Hannut_CJPL_10.20.pdf", 
    members);

// Results dictionary contains:
// - Key 0: Header
// - Key 1: Reference time (TREF)
// - Keys 2+: Participant results in delimited format
```

## Delimited Output Format

Each participant result is formatted as:
```
TMEM|TWINNER;Position;LastName;FirstName;Time;RACETYPE;type;RACETIME;time;TIMEPERKM;pace;POS;position;TEAM;team;SPEED;speed;ISMEMBER;flag;
```

### Example
```
TMEM;1;DUPONT;Jean;00:35:25;RACETYPE;RACE_TIME;RACETIME;00:35:25;POS;1;TEAM;AC Hannut;SPEED;16.95;ISMEMBER;1;
```

## Error Handling

- **File not found**: Throws `FileNotFoundException`
- **PDF parsing error**: Throws `InvalidOperationException` with inner exception
- **Invalid lines**: Silently skipped (returns `null` from parser)
- **Missing data**: Only returns results with Position AND RaceTime

## Testing Scenarios

### Test Case 1: French Column Format with All Columns
```
Input Line: "1    123   DUPONT Jean        AC Hannut         16.95    00:35:25    3:32"
Expected Output:
- Format: FrenchColumnFormat
- Position: 1
- FirstName: Jean
- LastName: DUPONT
- Team: AC Hannut
- Speed: 16.95
- RaceTime: 00:35:25
- TimePerKm: 03:32
```

### Test Case 2: French Column Format without Bib
```
Input Line: "12   Marie MARTIN       Running Club      15.80    00:38:12"
Expected Output:
- Format: FrenchColumnFormat
- Position: 12
- FirstName: Marie
- LastName: MARTIN
- Team: Running Club
- Speed: 15.80
- RaceTime: 00:38:12
```

### Test Case 3: French Column Format with Multi-word Team
```
Input Line: "45   789   Pierre-Louis DE BACKER    Jogging Club de Bruxelles    14.25    00:42:15    4:13"
Expected Output:
- Format: FrenchColumnFormat
- Position: 45
- FirstName: Pierre-Louis
- LastName: DE BACKER
- Team: Jogging Club de Bruxelles
- Speed: 14.25
- RaceTime: 00:42:15
- TimePerKm: 04:13
```

### Test Case 4: CrossCup Format with Teams
```
Input Line: "1 Jean DUPONT (AC Hannut) 00:35:25 16.95"
Expected Output:
- Format: CrossCup
- Position: 1
- FirstName: Jean
- LastName: DUPONT
- Team: AC Hannut
- RaceTime: 00:35:25
- Speed: 16.95
```

### Test Case 5: CrossCup Format without Team
```
Input Line: "12 Marie MARTIN 00:38:12 15.80"
Expected Output:
- Format: CrossCup
- Position: 12
- FirstName: Marie
- LastName: MARTIN
- Team: (empty)
- RaceTime: 00:38:12
- Speed: 15.80
```

### Test Case 6: CrossCup Multi-word Names
```
Input Line: "45 Pierre-Louis DE BACKER (Running Club) 00:42:15 14.25"
Expected Output:
- Format: CrossCup
- Position: 45
- FirstName: Pierre-Louis
- LastName: DE BACKER
- Team: Running Club
- RaceTime: 00:42:15
- Speed: 14.25
```

### Test Case 7: French Column Format Detection
```
Input PDF contains: "Pl.  Dos  Nom  Club  Vitesse  Temps  min/km"
Expected: Format detected as FrenchColumnFormat
```

### Test Case 8: CrossCup Filename Metadata
```
Input: "2026-01-25_Jogging de la CrossCup_Hannut_CJPL_10.20.pdf"
Expected Output:
- RaceDate: 2026-01-25
- RaceName: Jogging de la CrossCup
- Location: Hannut
- Category: CJPL
- DistanceKm: 10.20
```

### Test Case 9: French Column Filename
```
Input: "Classement-10km-Jogging-de-lAn-Neuf.pdf"
Expected: Successfully parsed with FrenchColumnFormat detection
```

## Backward Compatibility

The enhancements maintain full backward compatibility:
- Standard format PDFs continue to work as before
- Format detection is automatic
- No changes required to calling code
- Both formats can be processed by the same repository instance

## Future Enhancements

Potential improvements:
1. Support for category-based filtering in results
2. Use extracted distance for validation
3. Store race metadata alongside results
4. Support for relay team formats
5. Better handling of DNS/DNF participants
6. Age category extraction
