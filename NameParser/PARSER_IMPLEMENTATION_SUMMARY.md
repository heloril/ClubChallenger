# Parser Implementation Summary

## Overview

All 4 timing system parsers have been fully implemented with proper column detection, validation, and data extraction.

## Implementation Status

### ✅ 1. OtopFormatParser - COMPLETE

**Detection Logic:**
- Requires "pl./s." AND "pl./c." headers OR "otop" keyword OR "sexe" AND "catég."
- Verifies at least 4 out of 5 required columns: place, nom, prénom, sexe, temps

**Column Mapping:**
```
Place     → Position
Dos.      → (Ignored - bib number)
Nom       → LastName
Prénom    → FirstName
Sexe      → Sex (m/f/M/F → M/F)
Pl./S.    → PositionBySex
Catég.    → AgeCategory (validated against valid categories)
Pl./C.    → PositionByCategory
Temps     → RaceTime
Vitesse   → Speed
Moy.      → TimePerKm (pace)
Points    → (Ignored)
Jetons    → (Ignored)
```

**Name Format:** 
- Separate columns for first and last name
- Combined as: `"{FirstName} {LastName}"`

**Valid Categories:**
- Senior H, Moins16 H, Espoir H, Veteran 1, Moins16 D, Vétéran 2, Veteran 3, Ainée 1, Espoir D, Senior D, Ainée 3, Vétéran 4, Ainée 2, Ainée 4

---

### ✅ 2. GlobalPacingFormatParser - COMPLETE

**Detection Logic:**
- Requires "clas.sexe" AND "clas.cat" headers OR "global pacing" keyword
- Verifies at least 4 out of 5 required columns: pl., nom, sexe, cat, temps

**Column Mapping:**
```
Pl.         → Position
Dos         → (Ignored - bib number)
Nom         → LastName, FirstName (parsed from comma-separated)
Sexe        → Sex (m/f/M/F → M/F)
Clas.Sexe   → PositionBySex
Cat         → AgeCategory (validated against valid categories)
Clas.Cat    → PositionByCategory
Club        → Team
Vitesse     → Speed
min/km      → TimePerKm (pace)
Temps       → RaceTime
Points      → (Ignored)
```

**Name Format:** 
- Format: "LASTNAME, Firstname"
- Parsed by splitting on comma
- LastName = before comma, FirstName = after comma

**Valid Categories:**
- Sen, V1, V2, Dam, Esp G, V3, A2, A1, A3, V4, Esp F

---

### ✅ 3. ChallengeLaMeuseFormatParser - COMPLETE

**Detection Logic:**
- Requires "zatopek" keyword OR filename contains "zatopek" OR "p.ca" header
- Verifies at least 3 out of 4 required columns: pos, nom, temps, catégorie

**Column Mapping:**
```
Pos.        → Position
Nom         → FirstName, LastName (parsed from full name)
Dos.        → (Ignored - bib number)
Temps       → RaceTime
Vitesse     → Speed
Allure      → TimePerKm (pace)
Club        → Team
Catégorie   → AgeCategory (canonical mapping)
P.Ca        → PositionByCategory
D.Cha       → (Ignored)
```

**Name Format:** 
- Format: "Firstname LASTNAME"
- Uses existing pattern-based parsing
- Applies canonical category mapping

**Valid Categories (Canonical):**
- Séniors, Vétérans 1, Espoirs Garçons, Vétérans 2, Espoirs Filles, Ainées 2, Vétérans 3, Dames, Ainées 1, Ainées 3, Vétérans 4, Ainées 4

**Special Features:**
- Uses `ResolveCanonicalCategoryFromLineLocal()` for exact category matching
- Preserves proper accents and capitalization
- P.Ca detection with multiple patterns: "P.Ca", "P Ca", "P/Ca", etc.

---

### ✅ 4. GoalTimingFormatParser - COMPLETE

**Detection Logic:**
- Requires "rank" AND "pl/cat" headers OR "rank" AND "t/km" OR "goal timing" keyword
- Verifies at least 4 out of 5 required columns: rank, nom, sexe, cat, temps

**Column Mapping:**
```
Rank        → Position
Dos         → (Ignored - bib number)
[empty]     → (Empty column)
Nom Prenom  → LastName, FirstName (parsed from space-separated)
Sexe        → Sex (H/F → M/F)
Club        → Team
Cat         → AgeCategory (validated against valid categories)
Pl/Cat      → PositionByCategory
Temps       → RaceTime
T/Km        → TimePerKm (pace)
Vitesse     → Speed
Points      → (Ignored)
```

**Name Format:** 
- Format: "LASTNAME Firstname"
- Parsed by detecting UPPERCASE first word as LastName
- Rest of words = FirstName

**Valid Categories:**
- SH, V1, V2, SD, V3, ESH, A2, A1, V4, ESF, V5, A3, A4, A5

**Sex Mapping:**
- H (Homme) → M (Male)
- F (Femme) → F (Female)

---

## Common Features

### All Parsers Include:

1. **Column Detection**
   - Header row detection
   - Column position mapping
   - Flexible keyword matching

2. **Data Extraction**
   - Position (required)
   - Name (required, format varies)
   - Sex (optional, mapped to M/F)
   - Category (optional, validated)
   - Position by Sex (optional)
   - Position by Category (optional)
   - Race Time (optional)
   - Speed (optional)
   - Pace/TimePerKm (optional)
   - Team (optional)

3. **Validation**
   - Position must be numeric
   - Name must not be empty
   - Categories validated against parser-specific lists
   - Times validated (race time > 15 min, pace < 15 min)

4. **Member Matching**
   - Attempts to match against member list
   - Sets `IsMember` flag if matched
   - Falls back to name part extraction

5. **Error Handling**
   - Try-catch in all ParseLineUsingColumns
   - Returns null on parsing failure
   - Debug logging for troubleshooting

---

## Parser Priority Order

Parsers are evaluated in this sequence:

1. **OtopFormatParser** (most specific)
2. **GlobalPacingFormatParser**
3. **ChallengeLaMeuseFormatParser**
4. **GoalTimingFormatParser**
5. **StandardFormatParser** (fallback, catches all)

First matching parser is used for the entire PDF.

---

## Testing Recommendations

### For Each Parser:

1. **Column Detection Tests**
   - Verify header row is correctly identified
   - Check all columns are detected with correct positions
   - Test with slight variations in header format

2. **Data Extraction Tests**
   - Verify all fields are extracted correctly
   - Test edge cases (missing columns, extra spaces)
   - Verify name format parsing

3. **Category Validation Tests**
   - Valid categories are accepted
   - Invalid categories are rejected
   - Case-insensitive matching works

4. **Sex Mapping Tests**
   - All sex value variants map correctly (m/f/M/F/H/D)
   - Unknown values are ignored

5. **Time Parsing Tests**
   - Race time vs pace discrimination (15-minute threshold)
   - Various time formats (h:mm:ss, mm:ss, m:ss)

6. **Member Matching Tests**
   - Members are correctly identified
   - Non-members are handled
   - Name variations are matched

---

## Implementation Details

### Helper Methods (Each Parser Has):

```csharp
private bool IsHeaderRow(string line)
// Detects if a line is the header row

private Dictionary<string, int> DetectColumnPositions(string headerLine)
// Maps column names to their positions in the line

private ParsedPdfResult ParseLineUsingColumns(string line, List<Member> members)
// Main parsing logic using detected column positions

private string ExtractColumnValue(string line, string columnKey)
// Extracts text from a specific column

private int GetNextColumnPosition(int currentPosition)
// Gets the start position of the next column
```

### Category Validation

Each parser maintains a `HashSet<string>` of valid categories using case-insensitive comparison:

```csharp
private static readonly HashSet<string> _validCategories = 
    new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ... };
```

Categories are only set if they match this list.

### Sex Value Mapping

All parsers normalize sex values to M or F:

```csharp
if (sexText == "M" || sexText == "H")
    result.Sex = "M";
else if (sexText == "F" || sexText == "D")
    result.Sex = "F";
```

GoalTiming additionally maps:
- H (Homme) → M
- F (Femme) → F

---

## Build Status

✅ **Build: Successful**
- All parsers compile without errors
- No warnings
- Ready for testing with real PDF samples

---

## Next Steps

1. **Testing with Real PDFs**
   - Obtain sample PDFs from each timing system
   - Verify column detection accuracy
   - Validate data extraction completeness

2. **Edge Case Handling**
   - Test with malformed headers
   - Test with missing columns
   - Test with unusual name formats

3. **Performance Optimization**
   - Profile parsing speed
   - Optimize column detection if needed
   - Cache column positions per file

4. **Documentation**
   - Add XML comments to parser classes
   - Create usage examples
   - Document known limitations

---

## Date
2025-01-XX (Implementation completion)
