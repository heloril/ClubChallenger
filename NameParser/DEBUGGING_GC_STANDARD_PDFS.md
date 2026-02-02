# Debugging Grand Challenge & Standard Format PDFs

## Problem PDFs
1. `20250511BlancGravierGC.pdf` - Grand Challenge format
2. `20250421SeraingGC.pdf` - Grand Challenge format  
3. `Classement-10km-Jogging-de-lAn-Neuf.pdf` - Standard format

## What Was Fixed

### Enhanced Category Extraction Strategy

**Before:** Only extracted categories from text AFTER the name was identified.

**Now:** Triple extraction approach:
1. Extract from remaining text (after times/speeds removed)
2. Extract from the name part itself (embedded categories)
3. Clean the name AFTER extraction (removes category markers)

### Code Changes

#### 1. **Moved `CleanExtractedName` to Base Class**
- Now ALL parsers can use it
- Removes category markers from names after extraction
- Located in `BasePdfFormatParser`

#### 2. **GrandChallengeFormatParser Enhanced**
```csharp
// BEFORE: Only checked parts after first element
if (parts.Length > 1) {
    ExtractCategoryFromText(string.Join(" ", parts.Skip(1)), result);
}

// AFTER: Checks ALL parts including name
ExtractCategoryFromText(string.Join(" ", parts.Skip(1)), result);  // Other parts
ExtractCategoryFromText(result.FullName, result);  // Name itself
result.FullName = CleanExtractedName(result.FullName);  // Clean after
```

#### 3. **CrossCupFormatParser Enhanced**
```csharp
// AFTER: Added double extraction + cleaning
ExtractCategoryFromText(workingLine, result);
result.FullName = workingLine.Trim();
ExtractCategoryFromText(result.FullName, result);  // Also check name
result.FullName = CleanExtractedName(result.FullName);  // Clean
```

#### 4. **StandardFormatParser Enhanced**
Same approach as CrossCupFormatParser - double extraction + cleaning.

#### 5. **Enhanced Debug Logging**
Now shows WHAT was extracted and FROM WHERE:
```
ExtractCategoryFromText: extracted 2 items from 'DUPONT Jean SH 45'
    Extracted Sex: M from 'M'
    Extracted AgeCategory: SH
```

## How to Test

### Step 1: Run Application with Debug Output

1. Open **Output** window (View → Output)
2. Select **Debug** from dropdown
3. Process one of the problem PDFs

### Step 2: Check Debug Output

Look for lines like:
```
Parsing complete using Grand Challenge Format:
  ...
  Category Data:
    Sex: X/Y
    PositionBySex: X/Y
    AgeCategory: X/Y
    PositionByCategory: X/Y

ExtractCategoryFromText: extracted 2 items from 'DUPONT Jean M SH'
    Extracted Sex: M from 'M'
    Extracted AgeCategory: SH
```

### Expected Results

#### For Grand Challenge PDFs (GC format)
```
Parsing complete using Grand Challenge Format:
  Category Data:
    Sex: 350/350 or PARTIAL (e.g., 200/350)
    PositionBySex: PARTIAL
    AgeCategory: 350/350 or PARTIAL
    PositionByCategory: PARTIAL
```

**Why partial?** GC PDFs might not have ALL category info for every runner.

#### For Standard Format PDF
```
Parsing complete using Standard Format:
  Category Data:
    Sex: 0/350 to 350/350 (depends on PDF content)
    PositionBySex: 0/350 to 350/350
    AgeCategory: 0/350 to 350/350
    PositionByCategory: 0/350 to 350/350
```

## Possible Scenarios

### Scenario 1: Still 0/X extraction

**Cause:** PDF doesn't have category data at all, or it's in a completely different format.

**Action:**
1. Open the PDF manually
2. Check if there ARE sex/category columns
3. Look at the first few data rows
4. Share a screenshot or sample line

### Scenario 2: Partial extraction (e.g., 100/350)

**Cause:** Some rows have categories, some don't. This is NORMAL for some races.

**Action:** Check a few specific rows in the UI to see which ones have data.

### Scenario 3: Name extraction broken

**Symptom:** Names like "DUPONT Jean SH" become just "DUPONT Jean SH" (not cleaned)

**Cause:** `CleanExtractedName` not being called.

**Action:** Check the debug output - should see "Extracted AgeCategory: SH" before the name is set.

### Scenario 4: Categories extracted but not saved to DB

**Symptom:** Debug shows extraction working, but database has NULL values.

**Verification:**
```sql
SELECT TOP 10
    Position,
    MemberFirstName,
    Sex,
    AgeCategory,
    PositionBySex,
    PositionByCategory
FROM Classifications
WHERE RaceId = <your_race_id>
ORDER BY Position;
```

**If NULL:** Problem is in `RaceProcessingService` or `Classification.AddOrUpdateResult` - not the parser.

## Format-Specific Tips

### Grand Challenge Format

**Typical line format:**
```
1    DUPONT Jean        M  SH  00:35:25    AC Hannut    16.95
```

**Extraction order:**
1. Position: `1`
2. Times removed: `00:35:25`
3. Speed removed: `16.95`
4. Remaining: `DUPONT Jean        M  SH    AC Hannut`
5. Split by multi-space: `["DUPONT Jean", "M", "SH", "AC Hannut"]`
6. First part = name: `DUPONT Jean`
7. Extract from others: `M  SH    AC Hannut` → Gets M, SH
8. Extract from name: `DUPONT Jean` → Nothing (good!)
9. Clean name: `DUPONT Jean` → `DUPONT Jean` (unchanged)

### Standard Format

**Typical line format:**
```
1 DUPONT Jean M SH 00:35:25 16.95 AC Hannut
```

**Extraction order:**
1. Position: `1`
2. Times/speed removed
3. Remaining: `DUPONT Jean M SH AC Hannut`
4. Team extracted: `(AC Hannut)` if present
5. Remaining: `DUPONT Jean M SH`
6. Extract categories: Finds M, SH
7. Clean name: `DUPONT Jean`

## Common PDF Variations

### No Category Data
```
1 DUPONT Jean 00:35:25 AC Hannut
```
**Result:** All category fields NULL (expected!)

### Category After Name
```
1 DUPONT Jean SH 00:35:25 AC Hannut
```
**Result:** Should extract SH ✅

### Category Before Name  
```
1 M SH DUPONT Jean 00:35:25 AC Hannut
```
**Result:** Should extract M, SH, but name might include them ⚠️

### Category Mixed With Team
```
1 DUPONT Jean (AC Hannut M SH) 00:35:25
```
**Result:** Extracted when team is parsed ✅

## Next Steps

1. **Run your application** with one of the problem PDFs
2. **Check Debug Output** for extraction messages
3. **Report back:**
   - What does the debug output show?
   - What are the category statistics?
   - Can you share a sample line from the PDF?

---

**Version:** 1.2  
**Date:** 2026-02-01  
**Status:** ✅ Enhanced extraction for GC and Standard formats  
**Build:** ✅ Successful
