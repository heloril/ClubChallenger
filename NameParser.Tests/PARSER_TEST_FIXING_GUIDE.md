# Parser Test Fixing Guide

## Overview

This guide helps identify and fix issues causing parser tests to fail. Follow this step-by-step approach to ensure all parsers work correctly.

## Step 1: Run All Parser Tests

```powershell
# Run from solution root
cd NameParser.Tests
./RunParserTests.ps1
```

Or in Visual Studio:
```
Test → Run All Tests (Ctrl+R, A)
Filter: ParsePdf_ShouldProduceExpectedNumberOfResults
```

## Step 2: Identify Failing Tests

Tests are organized by parser type:

### OtopFormatParser Tests (8 tests)
- 2026-01-25_Jogging de la CrossCup_Hannut_CJPL_10.20.pdf (expected: 110 results)
- 2026-01-25_Jogging de la CrossCup_Hannut_CJPL_5.20.pdf (expected: 84 results)
- 2026-01-18_Jogging d'Hiver_Sprimont_CJPL_12.00.pdf (expected: 175 results)
- 2026-01-18_Jogging d'Hiver_Sprimont_CJPL_7.00.pdf (expected: 175 results)
- 2026-02-01_Les Collines de Cointe_Liège_CJPL_5.00.pdf (expected: 156 results)
- 2026-02-01_Les Collines de Cointe_Liège_CJPL_10.00.pdf (expected: 262 results)
- 2025-11-16_Les 10 Miles_Liège_CJPL_16.90.pdf (expected: 217 results)
- 2025-11-16_Les 10 Miles_Liège_CJPL_7.30.pdf (expected: 163 results)

### GlobalPacingFormatParser Tests (2 tests)
- Classement-10km-Jogging-de-lAn-Neuf.pdf (expected: 354 results)
- Classement-5km-Jogging-de-lAn-Neuf.pdf (expected: 190 results)

### GoalTimingFormatParser Tests (2 tests)
- 20250421SeraingGC.pdf (expected: 279 results)
- 20250511BlancGravierGC.pdf (expected: 205 results)

### ChallengeLaMeuseFormatParser Tests (3 tests)
- La Zatopek en Famille 6.5kms.pdf (expected: 286 results)
- La Zatopek en Famille 10kms.pdf (expected: 469 results)
- La Zatopek en Famille 21kms.pdf (expected: 288 results)

## Step 3: Common Issues and Fixes

### Issue 1: Parser Not Selected (Wrong Parser Used)

**Symptoms:**
- Test output shows "Using Standard Format" or wrong parser name
- Test fails because expected parser wasn't selected

**Fix:**
Check `CanParse()` method is detecting the format correctly:

```csharp
// OtopFormatParser
public override bool CanParse(string pdfText, RaceMetadata metadata)
{
    var lower = pdfText.ToLowerInvariant();
    
    // Check for Otop-specific headers
    bool hasOtopHeaders = (lower.Contains("pl./s.") && lower.Contains("pl./c.")) ||
                         (lower.Contains("catég.") && lower.Contains("prénom"));
    
    if (!hasOtopHeaders && !lower.Contains("otop"))
        return false;

    // Verify minimum required columns are present
    var requiredColumns = new[] { "place", "nom", "prénom", "sexe", "temps" };
    int foundColumns = requiredColumns.Count(col => lower.Contains(col));
    
    return foundColumns >= 4; // At least 4 out of 5 required columns
}
```

**Debugging:**
1. Add debug logging to `CanParse`:
   ```csharp
   System.Diagnostics.Debug.WriteLine($"Parser: {GetFormatName()}, CanParse: {result}");
   ```

2. Check PDF content for expected headers

### Issue 2: Too Few Results Extracted

**Symptoms:**
- Test shows actual < expected (e.g., expected: 110, actual: 95)
- Some valid rows are not being parsed

**Possible Causes:**

#### A. Header Detection Too Strict
```csharp
private bool IsHeaderRow(string line)
{
    var lower = line.ToLowerInvariant();
    // TOO STRICT - requires ALL keywords
    return lower.Contains("place") && lower.Contains("nom") && lower.Contains("prénom");
    
    // BETTER - requires some keywords
    return (lower.Contains("place") || lower.Contains("pl.")) && 
           lower.Contains("nom");
}
```

#### B. Column Detection Failing
```csharp
// Add debug logging
System.Diagnostics.Debug.WriteLine($"{GetFormatName()}: Detected {positions.Count} columns");
foreach (var col in positions.OrderBy(p => p.Value))
{
    System.Diagnostics.Debug.WriteLine($"  {col.Key}: position {col.Value}");
}
```

#### C. Data Validation Too Strict
```csharp
// PROBLEM: Rejecting valid data
if (!string.IsNullOrWhiteSpace(catText) && _validCategories.Contains(catText))
    result.AgeCategory = catText;  // Rejects valid but unlisted categories

// SOLUTION: Accept all non-empty values
if (!string.IsNullOrWhiteSpace(catText))
{
    // Accept category as-is (valid categories list is for reference only)
    result.AgeCategory = catText;
}
```

### Issue 3: Too Many Results Extracted

**Symptoms:**
- Test shows actual > expected (e.g., expected: 110, actual: 125)
- Header rows or footer rows being parsed as data

**Possible Causes:**

#### A. Header Not Being Skipped
```csharp
public override ParsedPdfResult ParseLine(string line, List<Member> members)
{
    // MUST return null for header
    if (!_headerParsed && IsHeaderRow(line))
    {
        _columnPositions = DetectColumnPositions(line);
        _headerParsed = true;
        return null; // ← IMPORTANT: Skip header
    }
    // ...
}
```

#### B. Invalid Lines Not Filtered
```csharp
// In ParseLineUsingColumns, validate position
if (!string.IsNullOrWhiteSpace(posText) && int.TryParse(posText.TrimEnd('.', ','), out int position))
    result.Position = position;
else
    return null; // ← IMPORTANT: Reject invalid position
```

### Issue 4: Column Coverage Low

**Symptoms:**
- Test output shows fields with < 90% coverage
- Many results missing expected data

**Fix:**
Check column mapping keywords:

```csharp
private Dictionary<string, int> DetectColumnPositions(string headerLine)
{
    var columnMappings = new Dictionary<string, string[]>
    {
        { "position", new[] { "place", "pl.", "pl ", "pos", "rank", "rang" } },
        { "name", new[] { "nom", "name", "participant", "nom prenom" } },
        { "sex", new[] { "sexe", "sex", "s.", "genre" } },
        { "category", new[] { "cat.", "cat ", "catég.", "catégorie", "category" } },
        // Add more variations as needed
    };
}
```

## Step 4: Parser-Specific Fixes

### OtopFormatParser

**Expected Behavior:**
- Separate columns for FirstName and LastName
- Sex values: m, f, M, F
- Categories with accents: Vétéran, Ainée, etc.

**Common Issues:**
1. **Column not detected**: Add "prénom", "prenom" variants
2. **Category not recognized**: Remove strict validation
3. **Name parsing fails**: Check column boundaries

**Fix:**
```csharp
// Accept all non-empty categories
if (!string.IsNullOrWhiteSpace(catText))
{
    result.AgeCategory = catText; // Don't validate against list
}
```

### GlobalPacingFormatParser

**Expected Behavior:**
- Name format: "LASTNAME, Firstname" (comma-separated)
- Sex values: m, f, M, F
- Categories: Sen, V1, V2, Dam, Esp G, Esp F

**Common Issues:**
1. **Name parsing fails**: Check comma splitting
2. **Column headers**: "Clas.Sexe", "Clas.Cat" detection

**Fix:**
```csharp
// Parse "LASTNAME, Firstname" format
if (nameText.Contains(","))
{
    var parts = nameText.Split(',');
    result.LastName = parts[0].Trim();
    result.FirstName = parts.Length > 1 ? parts[1].Trim() : parts[0].Trim();
}
```

### GoalTimingFormatParser

**Expected Behavior:**
- Name format: "LASTNAME Firstname" (space-separated, LASTNAME uppercase)
- Sex values: H → M, F → F
- Categories: SH, V1, V2, SD, ESH, ESF, A1-A5

**Common Issues:**
1. **Sex mapping**: H (Homme) should map to M
2. **Name parsing**: Detect uppercase first word

**Fix:**
```csharp
// Map H to M
if (sexText == "H" || sexText == "M")
    result.Sex = "M";
else if (sexText == "F" || sexText == "D")
    result.Sex = "F";

// Parse "LASTNAME Firstname"
if (IsAllCaps(parts[0]))
{
    result.LastName = parts[0];
    result.FirstName = string.Join(" ", parts.Skip(1));
}
```

### ChallengeLaMeuseFormatParser

**Expected Behavior:**
- Name format: "Firstname LASTNAME"
- Canonical categories: Séniors, Vétérans 1, Espoirs Garçons, etc.
- P.Ca (Position par Catégorie) detection

**Common Issues:**
1. **Category not canonical**: Uses non-canonical spellings
2. **P.Ca not detected**: Multiple pattern variations

**Fix:**
```csharp
// Use canonical category resolution
ResolveCanonicalCategoryFromLineLocal(workingLine, result);

// Detect P.Ca variations
var pcaMatch = Regex.Match(workingLine, @"\bP\.?\s*\.??\s*ca\.?\s*[:\-]?\s*(\d{1,3})\b", RegexOptions.IgnoreCase);
```

## Step 5: Verify Fixes

After making changes:

1. **Build solution**:
   ```powershell
   dotnet build
   ```

2. **Run single test**:
   ```powershell
   dotnet test --filter "FullyQualifiedName~ParsePdf_ShouldProduceExpectedNumberOfResults" --logger "console;verbosity=detailed"
   ```

3. **Check column coverage**:
   - Look for "✓" vs "⚠" indicators
   - Ensure all expected columns have ≥90% coverage

4. **Run all tests**:
   ```powershell
   dotnet test
   ```

## Step 6: Document Issues

For each failing test, document:

### Test: [Test Name]
**Expected Parser:** [Parser Name]  
**Expected Results:** [Number]  
**Actual Results:** [Number]  
**Issue:** [Description]  
**Fix Applied:** [Description]  
**Status:** ✅ FIXED / ⚠ IN PROGRESS / ❌ PENDING

Example:
```
### Test: 2026-01-25_Jogging de la CrossCup_Hannut_CJPL_10.20.pdf
**Expected Parser:** OtopFormatParser
**Expected Results:** 110
**Actual Results:** 105
**Issue:** 5 results not extracted - category validation too strict
**Fix Applied:** Removed _validCategories.Contains() check
**Status:** ✅ FIXED
```

## Quick Reference: Changes Made

### ✅ Completed Fixes

1. **OtopFormatParser**
   - ✅ Removed strict category validation
   - Accepts all category values present in PDF

2. **GlobalPacingFormatParser**
   - ✅ Removed strict category validation
   - Accepts all category values present in PDF

3. **GoalTimingFormatParser**
   - ✅ Removed strict category validation
   - Accepts all category values present in PDF

### 🔍 Next Steps

1. Run tests to identify which specific PDFs are failing
2. For each failure, check:
   - Is correct parser being selected?
   - Are all rows being detected?
   - Is column detection working?
   - Are required fields being extracted?

3. Apply targeted fixes based on failure patterns

## Debugging Commands

```powershell
# Run specific test with detailed output
dotnet test --filter "DisplayName~2026-01-25_Jogging de la CrossCup" --logger "console;verbosity=detailed"

# Run all tests for specific parser
dotnet test --filter "DisplayName~OtopFormatParser" --logger "console;verbosity=detailed"

# View debug output in Visual Studio
# Output window → Show output from: Debug
```

## Success Criteria

✅ All 15 tests passing  
✅ All parsers selecting correctly  
✅ All expected columns ≥90% coverage  
✅ Result counts match expectations  

---

## Date
2025-01-XX (Test fixing guide)
