# Debugging Guide - Category Columns Not Being Filled

## Problem
Database columns (Sex, PositionBySex, AgeCategory, PositionByCategory) exist but are not being populated from PDF files.

## ✅ Fixes Applied

### 1. **Added Enhanced Debug Logging**
Now shows category extraction statistics:
```
Parsing complete using French Column Format:
  Total lines: 354
  Successful parses: 350
  Category Data:
    Sex: 350/350
    PositionBySex: 350/350
    AgeCategory: 350/350
    PositionByCategory: 350/350
```

### 2. **Added Category Extraction to All Parsers**
- ✅ FrenchColumnFormatParser (already had column-based extraction)
- ✅ CrossCupFormatParser (added `ExtractCategoryFromText`)
- ✅ StandardFormatParser (added `ExtractCategoryFromText`)
- ✅ GrandChallengeFormatParser (added `ExtractCategoryFromText`)

### 3. **Added Helper Method: ExtractCategoryFromText()**
Extracts category data from text when columns aren't available:
- Finds Sex markers (M, F, H, D)
- Finds Category codes (SH, SD, V1, V2, A1, etc.)
- Attempts to find position numbers after category/sex

---

## 🔍 How to Debug

### Step 1: Check Which Parser Is Being Used

**Run your application and process a PDF**

Look at the Debug Output window for:
```
Parsing complete using [PARSER NAME]:
```

**Possible parsers:**
- `French Column Format` - Column-based, best for category extraction
- `CrossCup/CJPL Format` - Pattern-based
- `Standard Format` - Fallback
- `Grand Challenge Format` - Specific format

**Action:** If not using French Column Format but PDF has columns, check `CanParse()` method.

---

### Step 2: Check Category Statistics

Look for this in Debug Output:
```
Category Data:
  Sex: X/Y
  PositionBySex: X/Y
  AgeCategory: X/Y
  PositionByCategory: X/Y
```

**Interpret Results:**

| Result | Meaning | Action |
|--------|---------|--------|
| 0/350 | No data extracted | PDF doesn't have these columns OR parser not detecting them |
| 350/350 | Perfect extraction | ✅ Working correctly |
| 100/350 | Partial extraction | Some rows have data, check PDF format consistency |

---

### Step 3: Verify PDF Format

**Check your PDF has the columns:**

Open the PDF and look for headers like:
- **Sex:** `Sexe`, `Sex`, `S.`
- **Pos/Sex:** `Pl./S.`, `Clas.Sexe`, `Pl. Sexe`
- **Category:** `Cat.`, `Catég.`, `Catégorie`
- **Pos/Cat:** `Pl./C.`, `Clas. Cat`, `Pl. Cat`

**If columns exist:**
- French Column Format should detect them
- Check Debug output for column detection:
```
Detected 11 columns:
  sex: position 50
  positionsex: position 60
  category: position 70
  positioncat: position 80
```

**If columns DON'T exist:**
- Data might be embedded in text
- Pattern-based extraction (`ExtractCategoryFromText`) will try to find it
- Results may be partial

---

### Step 4: Check Database

**Query to check if data is saved:**

```sql
SELECT TOP 10
    Position,
    MemberFirstName,
    MemberLastName,
    Sex,
    PositionBySex,
    AgeCategory,
    PositionByCategory,
    Team,
    RaceId
FROM Classifications
WHERE RaceId = <your_race_id>
ORDER BY Position;
```

**Expected Results:**

| Scenario | Sex | PosSex | Category | PosCat |
|----------|-----|--------|----------|--------|
| PDF has columns | M/F | Number | SH/V1/etc | Number |
| PDF no columns, embedded | M/F | Maybe | SH/V1/etc | Maybe |
| PDF no data | NULL | NULL | NULL | NULL |

---

## 🐛 Common Issues and Solutions

### Issue 1: All Category Fields Are NULL

**Cause:** Parser isn't extracting the data

**Debug Steps:**
1. Check Debug output for category statistics (should be > 0)
2. Check which parser is being used
3. Check PDF actually has the data

**Solutions:**

**A. If using French Column Format:**
Add debug logging to `DetectColumnPositions`:

```csharp
System.Diagnostics.Debug.WriteLine($"Detected {positions.Count} columns:");
foreach (var col in positions)
{
    System.Diagnostics.Debug.WriteLine($"  {col.Key}: position {col.Value}");
}
```

Look for `sex`, `positionsex`, `category`, `positioncat` in the output.

**B. If columns not detected:**
The header might use different keywords. Check PDF and add to column mappings:

```csharp
{ "sex", new[] { "sexe", "sex", "s.", "YOUR_KEYWORD_HERE" } },
```

**C. If using other parsers:**
They use `ExtractCategoryFromText()` which looks for patterns in text. This works best when data is formatted like:
```
Jean DUPONT M 45 V1 12 AC Brussels
```

### Issue 2: Partial Data (e.g., Sex extracted but not Category)

**Cause:** PDF format varies or data in unexpected locations

**Solution:** 
Check the `CleanExtractedName()` method - it might be removing category markers thinking they're part of the name.

Add debug before cleaning:
```csharp
System.Diagnostics.Debug.WriteLine($"Raw extracted: '{rawName}'");
```

### Issue 3: Data Extracted But Not Saved

**Cause:** Data not passed through the pipeline

**Check:**
1. `ParsedPdfResult` properties populated ✅ (already verified by debug logging)
2. `ToDelimitedString()` includes new fields ✅ (already implemented)
3. `RaceProcessingService.ParseRaceResult()` extracts them ✅ (already implemented)
4. `Classification.AddOrUpdateResult()` accepts them ✅ (already implemented)
5. `ClassificationRepository.SaveClassifications()` saves them ✅ (already implemented)

**Verify:**
```sql
-- Check if any classifications have category data
SELECT 
    COUNT(*) as TotalRows,
    COUNT(Sex) as WithSex,
    COUNT(AgeCategory) as WithCategory
FROM Classifications
WHERE RaceId = <your_race_id>;
```

### Issue 4: Wrong Parser Being Used

**Cause:** `CanParse()` method not detecting format correctly

**Solution:**
Check which parser is selected in Debug output, then check its `CanParse()` method.

Example - if your PDF should use French Column Format but doesn't:
```csharp
public override bool CanParse(string pdfText, RaceMetadata metadata)
{
    var lowerText = pdfText.ToLowerInvariant();
    
    // Add debug
    System.Diagnostics.Debug.WriteLine($"Checking French Column Format:");
    System.Diagnostics.Debug.WriteLine($"  Contains 'pl.': {lowerText.Contains("pl.")}");
    System.Diagnostics.Debug.WriteLine($"  Contains 'nom': {lowerText.Contains("nom")}");
    
    return (lowerText.Contains("pl.") || lowerText.Contains("pl ")) &&
           lowerText.Contains("dos") &&
           lowerText.Contains("nom") &&
           (lowerText.Contains("vitesse") || lowerText.Contains("temps")) &&
           lowerText.Contains("min/km");
}
```

---

## ✅ Testing Checklist

After the fixes, test with a PDF:

- [ ] Start application
- [ ] Open Debug Output window (View → Output → Show output from: Debug)
- [ ] Process a PDF with category columns
- [ ] Check Debug output shows:
  - [ ] Parser name
  - [ ] Category statistics (should be > 0 if PDF has data)
  - [ ] Column detection (if French Column Format)
- [ ] View race classification in UI
- [ ] Check if new columns show data
- [ ] Query database to verify data is saved

---

## 📊 What to Report If Still Not Working

If category columns are still not being filled, provide:

1. **Debug Output:**
   ```
   Parsing complete using [PARSER NAME]:
     Category Data:
       Sex: X/Y
       ...
   ```

2. **PDF Sample:**
   - Screenshot of the PDF header row
   - First few data rows

3. **Database Query Result:**
   ```sql
   SELECT TOP 3 Position, Sex, AgeCategory 
   FROM Classifications 
   WHERE RaceId = <id>;
   ```

4. **Which parser is being used**

---

## 🚀 Next Steps

1. **Run your application** with Debug Output visible
2. **Process a test PDF** that should have category data
3. **Check the Debug output** for category statistics
4. **Verify in database** that data is saved

The debug logging will now show you exactly:
- ✅ Which parser is being used
- ✅ How many records have category data extracted
- ✅ If column detection is working

This will help pinpoint exactly where the issue is in the pipeline!

---

**Version:** 1.1  
**Date:** 2026-02-01  
**Status:** ✅ Enhanced with debugging tools
