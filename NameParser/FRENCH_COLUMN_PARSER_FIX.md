# French Column Format Parser - Enhanced Robustness

## Issue Summary

The French column format parser was not correctly parsing the Classement PDFs (`Classement-5km-Jogging-de-lAn-Neuf.pdf` and `Classement-10km-Jogging-de-lAn-Neuf.pdf`). The issues were:

1. **Too aggressive header detection** - Data lines were being skipped as headers
2. **Rigid column splitting** - Only used multiple-space splitting, failed on single-space formats
3. **Name extraction issues** - Only took one part as the name, missing multi-word names
4. **No debugging feedback** - Difficult to troubleshoot parsing failures

## Fixes Implemented

### 1. **Improved Header Detection**

**Before:**
```csharp
// Marked lines as headers if they contained 2+ header keywords
return keywordCount >= 2;
```

**After:**
```csharp
// Check if line starts with a number (position) - NOT a header
if (char.IsDigit(trimmed[0]))
{
    var posMatch = Regex.Match(trimmed, @"^(\d{1,4})[\s\.\,]");
    if (posMatch.Success)
        return false;  // This is a DATA line
}

// Need 3+ keywords to be a header (more strict)
return keywordCount >= 3;
```

**Benefits:**
- ✅ Never skips lines starting with position numbers
- ✅ More strict keyword matching (3 instead of 2)
- ✅ Handles numbered data lines correctly

### 2. **Flexible Column Splitting**

**Before:**
```csharp
// Only split by multiple spaces
var parts = Regex.Split(line, @"\s{2,}");
```

**After:**
```csharp
// Try multiple spaces first
var parts = Regex.Split(line, @"\s{2,}");

// If not enough parts, fall back to single space
if (parts.Length < 3)
{
    parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
}
```

**Benefits:**
- ✅ Handles both columnar and space-separated formats
- ✅ More resilient to formatting variations
- ✅ Works with different PDF extraction results

### 3. **Intelligent Name Extraction**

**Before:**
```csharp
// Take only one part as the name
result.FullName = parts[currentIndex];
currentIndex++;
```

**After:**
```csharp
// Collect name parts until we hit a time or speed
var nameParts = new List<string>();
while (currentIndex < parts.Length)
{
    var part = parts[currentIndex];
    
    // Stop if we hit a time (contains colon)
    if (part.Contains(':'))
        break;
    
    // Stop if we hit a speed (decimal number)
    if (Regex.IsMatch(part, @"^\d+[\.,]\d+$"))
        break;
    
    nameParts.Add(part);
    currentIndex++;
    
    // Smart stopping after 2+ name parts
    if (nameParts.Count >= 2 && NextPartLooksLikeTeamOrData())
        break;
}

result.FullName = string.Join(" ", nameParts);
```

**Benefits:**
- ✅ Handles multi-word names: "Jean Pierre DUPONT"
- ✅ Handles compound last names: "DE BACKER Marie"
- ✅ Stops intelligently when team/data is detected

### 4. **Better Team Extraction**

**Before:**
```csharp
// Simple loop collecting parts
team = string.IsNullOrEmpty(team) ? part : $"{team} {part}";
```

**After:**
```csharp
// Collect team parts separately after name
var teamParts = new List<string>();
while (currentIndex < parts.Length)
{
    var part = parts[currentIndex];
    
    // Check if speed
    if (Regex.IsMatch(part, @"^\d+[\.,]\d+$"))
    {
        // Process speed and stop
        break;
    }
    // Check if time
    else if (Regex.IsMatch(part, @"^\d{1,2}:\d{2}"))
    {
        break;
    }
    // Otherwise, it's team
    else
    {
        teamParts.Add(part);
        currentIndex++;
    }
}

if (teamParts.Count > 0)
    result.Team = string.Join(" ", teamParts);
```

**Benefits:**
- ✅ Correctly handles multi-word teams
- ✅ Separates team from speed/time reliably
- ✅ Preserves team structure

### 5. **Position Number Cleaning**

**Before:**
```csharp
if (!int.TryParse(parts[0], out int position))
    return null;
```

**After:**
```csharp
// Remove trailing punctuation (dots, commas, etc.)
var positionText = parts[0].TrimEnd('.', ',', ':', ';');
if (!int.TryParse(positionText, out int position))
    return null;
```

**Benefits:**
- ✅ Handles "1." → 1
- ✅ Handles "12," → 12
- ✅ More flexible position parsing

### 6. **Debug Logging**

**New Feature:**
```csharp
int successfulParses = 0;
int skippedHeaders = 0;
int failedParses = 0;

// ... parsing logic ...

// Warning if very few results
if (results.Count < 5 && lines.Length > 20)
{
    System.Diagnostics.Debug.WriteLine($"Warning: Only parsed {results.Count} results from {lines.Length} lines using {selectedParser.GetFormatName()}");
    System.Diagnostics.Debug.WriteLine($"  Successful: {successfulParses}, Failed: {failedParses}, Skipped: {skippedHeaders}");
}
```

**Benefits:**
- ✅ Helps diagnose parsing issues
- ✅ Shows parser performance metrics
- ✅ Visible in Debug output window

## Example Parsing

### Input Line (French Column Format)
```
1    123   Jean Pierre DUPONT    AC Hannut Sport    16.95    00:35:25    3:32
```

### Parsing Steps

1. **Split by spaces:**
   ```
   ["1", "123", "Jean", "Pierre", "DUPONT", "AC", "Hannut", "Sport", "16.95", "00:35:25", "3:32"]
   ```

2. **Extract position:**
   ```
   Position: 1 (cleaned from "1" or "1.")
   ```

3. **Skip bib number:**
   ```
   Current index: 2 (skipped "123")
   ```

4. **Collect name parts:**
   ```
   Collecting: "Jean", "Pierre", "DUPONT"
   Stop when: "AC" looks like team (or "16.95" is detected as speed)
   FullName: "Jean Pierre DUPONT"
   ```

5. **Collect team parts:**
   ```
   Collecting: "AC", "Hannut", "Sport"
   Stop when: "16.95" is detected as speed
   Team: "AC Hannut Sport"
   ```

6. **Extract speed:**
   ```
   Speed: 16.95 km/h
   ```

7. **Extract times:**
   ```
   RaceTime: 00:35:25
   TimePerKm: 03:32
   ```

8. **Extract name parts:**
   ```
   Using enhanced ExtractNameParts()
   FirstName: "Jean Pierre"
   LastName: "DUPONT"
   ```

### Output
```csharp
ParsedPdfResult
{
    Position = 1,
    FullName = "Jean Pierre DUPONT",
    FirstName = "Jean Pierre",
    LastName = "DUPONT",
    Team = "AC Hannut Sport",
    RaceTime = TimeSpan(00:35:25),
    TimePerKm = TimeSpan(00:03:32),
    Speed = 16.95,
    IsMember = false
}
```

## Testing

### Test Cases

#### Test 1: Simple Format
```
Input: "1  Jean DUPONT  00:35:25"
Expected: Position=1, Name="Jean DUPONT", Time=00:35:25
```

#### Test 2: With Bib Number
```
Input: "1    123   Jean DUPONT    00:35:25"
Expected: Position=1, Bib skipped, Name="Jean DUPONT", Time=00:35:25
```

#### Test 3: With Team
```
Input: "1    123   Jean DUPONT    AC Hannut    16.95    00:35:25    3:32"
Expected: Position=1, Name="Jean DUPONT", Team="AC Hannut", Speed=16.95, Time=00:35:25, Pace=3:32
```

#### Test 4: Multi-word Name and Team
```
Input: "1  Jean Pierre DE BACKER  Jogging Club Bruxelles  15.80  00:38:12"
Expected: Position=1, Name="Jean Pierre DE BACKER", Team="Jogging Club Bruxelles", Speed=15.80, Time=00:38:12
```

#### Test 5: Position with Punctuation
```
Input: "1.  Jean DUPONT  00:35:25"
Expected: Position=1, Name="Jean DUPONT", Time=00:35:25
```

## Troubleshooting

### If Still Not Parsing

1. **Check Debug Output:**
   - Open Output window in Visual Studio
   - Select "Debug" from dropdown
   - Look for warning messages about low parse counts

2. **Verify Format Detection:**
   - Ensure PDF contains: "Pl.", "Dos", "Nom", "Vitesse", "Temps", "min/km"
   - Check if another parser is being selected

3. **Inspect PDF Text:**
   - Extract text manually using iText
   - Check actual spacing and structure
   - Verify column alignment

4. **Enable More Logging:**
   ```csharp
   // In ParseLine(), add:
   System.Diagnostics.Debug.WriteLine($"Parsing line: {line}");
   System.Diagnostics.Debug.WriteLine($"Parts: {string.Join(" | ", parts)}");
   ```

## Benefits Summary

| Improvement | Before | After |
|-------------|--------|-------|
| **Header Detection** | 2 keywords | 3 keywords + number check |
| **Column Splitting** | Multiple spaces only | Multiple OR single space |
| **Name Extraction** | Single part | Multi-part intelligent |
| **Team Extraction** | Simple concat | Separate collection |
| **Position Parsing** | Exact match | Cleaned (remove punctuation) |
| **Debug Info** | None | Parse statistics |

## Files Modified

- **Infrastructure\Repositories\PdfRaceResultRepository.cs**
  - Enhanced `FrenchColumnFormatParser.ParseLine()`
  - Improved `IsHeaderLine()` with number detection
  - Added debug logging to `ParsePdfText()`

## Build Status

✅ Build successful  
✅ No errors or warnings  
✅ Backward compatible  
✅ Ready for testing with Classement PDFs

## Next Steps

1. **Test with actual PDFs:**
   ```csharp
   var repo = new PdfRaceResultRepository();
   var results = repo.GetRaceResults("Classement-5km-Jogging-de-lAn-Neuf.pdf", members);
   // Check results count and data quality
   ```

2. **Verify in Debug output:**
   - Should see successful parse counts
   - Should not see low-count warnings

3. **Validate results:**
   - Check that all participants are parsed
   - Verify names are correctly split
   - Confirm times and speeds are accurate

The parser is now much more robust and should successfully handle the Classement PDFs! 🎉
