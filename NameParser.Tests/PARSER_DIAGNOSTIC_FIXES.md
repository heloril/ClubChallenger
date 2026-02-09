# Parser Fixes Applied - Diagnostic Mode

## Changes Made (Phase 2)

### 1. ✅ Added Parser Selection Logging
**Location:** `ParsePdfText()` method

**What:** Added debug logging to show which parser is being tested and selected
```csharp
System.Diagnostics.Debug.WriteLine($"=== Testing parsers for: {_raceMetadata?.RaceName ?? "Unknown"} ===");
System.Diagnostics.Debug.WriteLine($"  {parser.GetFormatName()}: {(canParse ? "YES" : "no")}");
System.Diagnostics.Debug.WriteLine($"=== Selected: {selectedParser.GetFormatName()} ===");
```

**Why:** Helps identify if wrong parser is being selected

---

### 2. ✅ Added Column Detection Logging
**Location:** All column-based parsers (Otop, GlobalPacing, GoalTiming)

**What:** Added logging when header is detected
```csharp
System.Diagnostics.Debug.WriteLine($"Otop: Header detected, {_columnPositions.Count} columns found");
```

**Why:** Helps identify if header detection is failing

---

### 3. ✅ Made Name Extraction More Lenient (OtopFormatParser)
**Location:** `ParseLineUsingColumns()` in OtopFormatParser

**Before:**
- Required BOTH FirstName AND LastName
- Returned null if either was missing

**After:**
- Accepts if EITHER FirstName OR LastName is present
- Uses available name for both fields if only one is present
- Only returns null if NO name found

**Why:** Some PDFs might have data in only one name column

---

### 4. ✅ Improved Header Detection (OtopFormatParser)
**Location:** `IsHeaderRow()` in OtopFormatParser

**Before:**
```csharp
return lower.Contains("place") && lower.Contains("nom") && lower.Contains("prénom");
```

**After:**
```csharp
return (lower.Contains("place") || lower.Contains(" pl.") || lower.Contains(" pl ")) && 
       lower.Contains("nom");
```

**Why:** 
- More lenient - doesn't require "prénom"
- Accepts "pl." as well as "place"
- Increases chance of detecting header

---

### 5. ✅ Made Time Extraction More Lenient
**Location:** All column-based parsers (Otop, GlobalPacing, GoalTiming)

**Before:**
- Only accepted times > 15 minutes for RaceTime
- Rejected shorter times completely

**After:**
```csharp
if (parsedTime.Value.TotalMinutes > RaceTimeThresholdMinutes)
    result.RaceTime = parsedTime.Value;
else if (parsedTime.Value.TotalMinutes > 0)
    result.TimePerKm = parsedTime.Value; // Might be pace
```

**Why:**
- Accepts ANY positive time value
- Auto-detects if it's race time (>15 min) or pace (<15 min)
- Prevents losing valid results due to time threshold

---

## How to Diagnose Issues

### Step 1: Run the Diagnostic Script
```powershell
cd NameParser.Tests
./DiagnoseParser.ps1
```

### Step 2: View Debug Output in Visual Studio
1. Run tests in Debug mode
2. Open **Output** window (View → Output)
3. Select **Debug** from dropdown
4. Look for these messages:

```
=== Testing parsers for: CrossCup 10km ===
  Otop Format: YES
  Global Pacing Format: no
  Challenge La Meuse Format: no
  Goal Timing Format: no
  Standard Format: no
=== Selected: Otop Format ===

Otop: Header detected, 10 columns found
Parsing complete using Otop Format:
  Total lines: 150
  Successful parses: 110
  Failed parses: 25
  Skipped headers: 2
```

### Step 3: Interpret the Output

#### ✅ Good Signs:
- Correct parser selected
- Header detected with multiple columns (8-12 columns)
- Successful parses ≈ Expected result count
- Low failed parse count

#### ⚠️ Warning Signs:
- Wrong parser selected → Fix `CanParse()` logic
- 0 columns detected → Header detection failed
- Successful parses << Expected → Data parsing issues
- High failed parse count → Column extraction issues

---

## Common Issues and Solutions

### Issue 1: Wrong Parser Selected

**Symptoms:**
```
=== Testing parsers for: CrossCup 10km ===
  Otop Format: no
  Global Pacing Format: no
  ...
=== Selected: Standard Format ===  ← WRONG!
```

**Solution:**
Check `CanParse()` method - make detection less strict:
```csharp
// Too strict:
return foundColumns >= 5; // All 5 required

// Better:
return foundColumns >= 3; // At least 3 out of 5
```

---

### Issue 2: Header Not Detected

**Symptoms:**
```
Otop: Header detected, 0 columns found  ← PROBLEM!
Parsing complete using Otop Format:
  Successful parses: 0  ← NO RESULTS!
```

**Solution:**
1. Check `IsHeaderRow()` - make it more lenient
2. Check `DetectColumnPositions()` - add more keyword variations
3. Check actual PDF header format

---

### Issue 3: Low Result Count

**Symptoms:**
```
Parsing complete using Otop Format:
  Total lines: 150
  Successful parses: 50  ← Expected 110!
  Failed parses: 85  ← TOO MANY!
```

**Solution:**
1. Check `ParseLineUsingColumns()` - might be too strict
2. Check required field validation - make optional
3. Check column boundary detection - might be cutting off data

---

### Issue 4: Low Column Coverage

**Symptoms:**
```
📋 Column Coverage Analysis:
   ⚠ Sex                    10/110  (  9.1%)  ← LOW!
   ⚠ AgeCategory            25/110  ( 22.7%)  ← LOW!
```

**Solution:**
1. Check column mapping keywords in `DetectColumnPositions()`
2. Check extraction logic in `ParseLineUsingColumns()`
3. Check actual PDF to see if data exists

---

## Testing Strategy

### 1. Test One PDF at a Time
```powershell
dotnet test --filter "DisplayName~CrossCup_10.20"
```

### 2. Check Debug Output for That PDF
Look for:
- Parser selection
- Column detection
- Parse statistics

### 3. Fix Issues for That Parser
- Update `CanParse()` if wrong parser
- Update `IsHeaderRow()` if header not detected
- Update `ParseLineUsingColumns()` if parsing fails

### 4. Re-test and Verify
```powershell
dotnet test --filter "DisplayName~CrossCup_10.20"
```

### 5. Move to Next PDF
Repeat until all tests pass

---

## Expected Debug Output Pattern

### ✅ Successful Parsing Example:
```
=== Testing parsers for: CrossCup 10km ===
  Otop Format: YES
=== Selected: Otop Format ===
Otop: Header detected, 10 columns found
Otop: Detected columns:
  position: 0
  lastname: 12
  firstname: 30
  sex: 48
  positionsex: 56
  category: 68
  positioncat: 85
  time: 100
  speed: 120
  pace: 135

Parsing complete using Otop Format:
  Total lines: 150
  Successful parses: 110
  Failed parses: 5
  Skipped headers: 2
  Category Data:
    Sex: 110/110
    PositionBySex: 110/110
    AgeCategory: 110/110
    PositionByCategory: 110/110
  Other Data:
    RaceTime: 110/110
    Speed: 110/110

📋 Column Coverage Analysis:
   ✓ Position              110/110  (100.0%)
   ✓ FirstName             110/110  (100.0%)
   ✓ LastName              110/110  (100.0%)
   ✓ Sex                   110/110  (100.0%)
   ✓ PositionBySex         110/110  (100.0%)
   ✓ AgeCategory           110/110  (100.0%)
   ✓ PositionByCategory    110/110  (100.0%)
   ✓ RaceTime              110/110  (100.0%)
   ✓ Speed                 110/110  (100.0%)
   ✓ TimePerKm             110/110  (100.0%)
   ✓ All expected columns have good coverage (≥90%)
```

---

## Next Steps

1. **Run DiagnoseParser.ps1** to see current state
2. **Check Output window** for parser selection and column detection
3. **Identify failing PDFs** from test results
4. **Fix one parser at a time** based on debug output
5. **Re-run tests** after each fix
6. **Document issues** found and solutions applied

---

## Date
2025-01-XX (Diagnostic mode implementation)
