# Parser Quick Reference & Testing Guide

## How to Test Each Parser

### 1. Otop Format
```
Sample Header:
Place  Dos.  Nom        Prénom    Sexe  Pl./S.  Catég.    Pl./C.  Temps     Vitesse  Moy.
1      123   DUPONT     Jean      M     1       Senior H  1       00:35:25  16.5     3:38

Detection Keywords: pl./s., pl./c., otop, sexe + catég.
Name Format: Separate columns (Nom | Prénom)
Sex Values: M, F, m, f
Categories: Senior H, Espoir H, Veteran 1, Ainée 1, etc.
```

### 2. Global Pacing Format
```
Sample Header:
Pl.  Dos  Nom                    Sexe  Clas.Sexe  Cat    Clas.Cat  Club      Vitesse  min/km  Temps     Points
1    123  DUPONT, Jean           M     1          Sen    1         AC Test   16.5     3:38    00:35:25  100

Detection Keywords: clas.sexe, clas.cat, global pacing
Name Format: "LASTNAME, Firstname" (comma-separated)
Sex Values: M, F, m, f
Categories: Sen, V1, V2, Dam, Esp G, Esp F, etc.
```

### 3. Challenge La Meuse Format
```
Sample Header:
Pos.  Nom              Dos.  Temps     Vitesse  Allure  Club      Catégorie        P.Ca  D.Cha
1     Jean DUPONT      123   00:35:25  16.5     3:38    AC Test   Séniors         1     5.2

Detection Keywords: zatopek, p.ca, p ca
Name Format: "Firstname LASTNAME" (space-separated)
Categories: Séniors, Vétérans 1, Espoirs Garçons, Dames, Ainées 1, etc. (canonical with accents)
```

### 4. Goal Timing Format
```
Sample Header:
Rank  Dos    Nom Prenom        Sexe  Club      Cat  Pl/Cat  Temps     T/Km  Vitesse  Points
1     123    DUPONT Jean       H     AC Test   SH   1       00:35:25  3:38  16.5     100

Detection Keywords: rank + pl/cat, rank + t/km, goal timing
Name Format: "LASTNAME Firstname" (space-separated, LASTNAME uppercase)
Sex Values: H (Homme) → M, F (Femme) → F
Categories: SH, V1, V2, SD, ESH, ESF, A1-A5, etc.
```

---

## Testing Checklist

### For Each Parser:

#### ✅ Column Detection
- [ ] Header row correctly identified
- [ ] All expected columns detected
- [ ] Column positions accurate
- [ ] Works with slight header variations

#### ✅ Data Extraction
- [ ] Position extracted correctly
- [ ] Name parsed in correct format
- [ ] Sex mapped to M/F
- [ ] Category extracted and validated
- [ ] Position by sex extracted
- [ ] Position by category extracted
- [ ] Race time extracted (> 15 min)
- [ ] Pace extracted (< 15 min)
- [ ] Speed extracted
- [ ] Team extracted

#### ✅ Edge Cases
- [ ] Missing optional columns handled
- [ ] Extra whitespace handled
- [ ] Column misalignment handled
- [ ] Invalid category ignored
- [ ] Invalid time format handled
- [ ] Empty name handled (returns null)
- [ ] Invalid position handled (returns null)

#### ✅ Member Matching
- [ ] Known members identified
- [ ] IsMember flag set correctly
- [ ] Non-members handled
- [ ] Name variations matched

---

## Common Issues & Solutions

### Issue: Column Not Detected
**Symptoms:** Field always empty or null
**Solution:** 
1. Check header line format in PDF
2. Add header keyword variation to `DetectColumnPositions`
3. Verify keyword is lowercase in mapping

### Issue: Name Not Parsed Correctly
**Symptoms:** FirstName/LastName reversed or combined
**Solution:**
1. Verify name format matches specification
2. Check `IsAllCaps` logic for LASTNAME detection
3. Verify comma/space splitting logic

### Issue: Category Not Extracted
**Symptoms:** AgeCategory always null despite being in PDF
**Solution:**
1. Check if category value is in `_validCategories` HashSet
2. Verify case-insensitive comparison
3. Check for extra spaces or punctuation in category value

### Issue: Time Always Extracted as Pace (or vice versa)
**Symptoms:** RaceTime and TimePerKm swapped
**Solution:**
1. Check 15-minute threshold logic
2. Verify time is in correct format (h:mm:ss for race, mm:ss for pace)
3. Check column mapping (Temps vs Moy./min/km/T/Km)

### Issue: Parser Not Selected
**Symptoms:** StandardFormatParser used instead of specific parser
**Solution:**
1. Check `CanParse` detection keywords
2. Verify required columns are in PDF header
3. Check parser priority order (earlier parsers evaluated first)

---

## Debug Logging

All parsers include debug logging:

```csharp
System.Diagnostics.Debug.WriteLine($"ParserName: Detected {positions.Count} columns");
System.Diagnostics.Debug.WriteLine($"ParserName parser error: {ex.Message}");
```

View in Visual Studio:
1. Run in Debug mode
2. Open **Output** window (View → Output)
3. Select **Debug** from dropdown
4. Look for parser messages

---

## Performance Notes

### Column Detection Performance
- Done once per PDF (on first header row)
- Cached in `_columnPositions` dictionary
- Reused for all subsequent data rows

### Category Validation Performance
- Uses `HashSet<string>` for O(1) lookup
- Case-insensitive comparison via `StringComparer.OrdinalIgnoreCase`

### Name Parsing Performance
- Simple string operations (Split, Trim, Join)
- No regex for name parsing (fast)
- Fallback to `ExtractNameParts` only when needed

---

## Example Test Cases

### Test Case 1: Otop Format
```csharp
[Fact]
public void OtopParser_ShouldParseValidLine()
{
    var parser = new OtopFormatParser();
    var line = "1      123   DUPONT     Jean      M     1       Senior H  1       00:35:25  16.5     3:38";
    var result = parser.ParseLine(line, new List<Member>());
    
    Assert.NotNull(result);
    Assert.Equal(1, result.Position);
    Assert.Equal("Jean", result.FirstName);
    Assert.Equal("DUPONT", result.LastName);
    Assert.Equal("M", result.Sex);
    Assert.Equal("Senior H", result.AgeCategory);
}
```

### Test Case 2: Global Pacing Format
```csharp
[Fact]
public void GlobalPacingParser_ShouldParseName WithComma()
{
    var parser = new GlobalPacingFormatParser();
    var line = "1    123  DUPONT, Jean           M     1          Sen    1         AC Test   16.5     3:38    00:35:25  100";
    var result = parser.ParseLine(line, new List<Member>());
    
    Assert.NotNull(result);
    Assert.Equal("DUPONT", result.LastName);
    Assert.Equal("Jean", result.FirstName);
}
```

### Test Case 3: Challenge La Meuse Format
```csharp
[Fact]
public void ChallengeLaMeuseParser_ShouldMapCanonicalCategory()
{
    var parser = new ChallengeLaMeuseFormatParser();
    var line = "1     Jean DUPONT      123   00:35:25  16.5     3:38    AC Test   Séniors         1     5.2";
    var result = parser.ParseLine(line, new List<Member>());
    
    Assert.NotNull(result);
    Assert.Equal("Séniors", result.AgeCategory); // With accent
}
```

### Test Case 4: Goal Timing Format
```csharp
[Fact]
public void GoalTimingParser_ShouldMapSexCorrectly()
{
    var parser = new GoalTimingFormatParser();
    var line = "1     123    DUPONT Jean       H     AC Test   SH   1       00:35:25  3:38  16.5     100";
    var result = parser.ParseLine(line, new List<Member>());
    
    Assert.NotNull(result);
    Assert.Equal("M", result.Sex); // H mapped to M
}
```

---

## Visual Studio Testing

### Run Tests
```
Test → Run All Tests
```

### View Test Output
```
Test → Test Explorer
Right-click test → Open Additional Output for this Result
```

### Debug Single Test
```
Right-click test → Debug
Set breakpoints in parser code
```

---

## Date
2025-01-XX (Testing guide creation)
