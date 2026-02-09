# Test Enhancement: Column Coverage Validation

## Overview

Enhanced the `PdfParserIntegrationTests` to validate that all expected columns for each parser are properly extracted and populated with valid data.

## Changes Made

### 1. Test Method Signature Update

**Before:**
```csharp
public void ParsePdf_ShouldProduceExpectedNumberOfResults(
    string filename, 
    string expectedRaceName, 
    double expectedDistance, 
    int expectedResultCount)
```

**After:**
```csharp
public void ParsePdf_ShouldProduceExpectedNumberOfResults(
    string filename, 
    string expectedRaceName, 
    double expectedDistance, 
    int expectedResultCount,
    string expectedParserName)  // NEW PARAMETER
```

### 2. Column Coverage Validation

Added comprehensive validation of all expected columns for each parser:

#### ValidateParserColumns()
- Analyzes all parsed results
- Counts how many results have each expected field populated
- Calculates coverage percentage for each field
- Reports fields with < 90% coverage as warnings

#### GetExpectedColumnsForParser()
Returns expected columns for each parser:

**OtopFormatParser:**
- Position, FirstName, LastName, Sex, PositionBySex
- AgeCategory, PositionByCategory, RaceTime, Speed, TimePerKm

**GlobalPacingFormatParser:**
- Position, FirstName, LastName, Sex, PositionBySex
- AgeCategory, PositionByCategory, Team, RaceTime, Speed, TimePerKm

**ChallengeLaMeuseFormatParser:**
- Position, FirstName, LastName, RaceTime, Speed
- TimePerKm, Team, AgeCategory, PositionByCategory

**GoalTimingFormatParser:**
- Position, FirstName, LastName, Sex, Team
- AgeCategory, PositionByCategory, RaceTime, TimePerKm, Speed

### 3. Result Parsing Logic

#### ParseDelimitedResult()
Parses the delimited string format:
```
TYPE;Position;LastName;FirstName;Time;RACETYPE;value;RACETIME;value;SEX;value;...
```

Extracts:
- Fixed position fields (Position, LastName, FirstName, Time)
- KEY;VALUE pairs (RACETIME, SPEED, SEX, etc.)
- Maps to standardized field names

#### IsFieldPresent()
Validates field values:
- **Position**: Must be positive number
- **Names**: Must not be "Unknown"
- **Times**: Must not be "00:00:00"
- **Speed**: Must be positive number
- **Sex**: Must be "M" or "F"
- **Categories**: Must not be empty
- **Team**: Must not be empty

### 4. Updated Test Output

**Enhanced Output Example:**
```
📄 File: 2026-01-25_Jogging de la CrossCup_Hannut_CJPL_10.20.pdf
📊 Race: CrossCup 10km
🔍 Expected Parser: OtopFormatParser
📏 Distance: 10.20 km
✅ Expected results: 110
📈 Actual results: 110
✓ Result count matches expected!

📋 Column Coverage Analysis:
   Parser: OtopFormatParser
   Expected columns and coverage:
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

**Warning Example (if coverage < 90%):**
```
📋 Column Coverage Analysis:
   Parser: GlobalPacingFormatParser
   Expected columns and coverage:
   ✓ Position              190/190  (100.0%)
   ✓ FirstName             190/190  (100.0%)
   ✓ LastName              190/190  (100.0%)
   ✓ Sex                   190/190  (100.0%)
   ⚠ PositionBySex          85/190  ( 44.7%)
   ✓ AgeCategory           190/190  (100.0%)
   ⚠ PositionByCategory     90/190  ( 47.4%)
   ✓ Team                  190/190  (100.0%)
   ✓ RaceTime              190/190  (100.0%)
   ✓ Speed                 190/190  (100.0%)
   ✓ TimePerKm             190/190  (100.0%)

   ⚠️  Some columns have less than 90% coverage!
   This may indicate:
      - Column detection issues in the parser
      - Missing data in the PDF
      - Column mapping errors
```

### 5. Updated All Related Tests

All test methods now accept and use the parser name parameter:
- `ParsePdf_ShouldProduceExpectedNumberOfResults`
- `Integration_ParseAllPdfs_ShouldSucceed`
- `ParsedResults_ShouldHaveValidPositions`
- `ParsedResults_ShouldHaveValidNames`

## Benefits

1. **Early Detection**: Identifies missing column extraction immediately
2. **Coverage Metrics**: Shows exact percentage of results with each field
3. **Parser-Specific**: Validates only expected columns for each parser
4. **Actionable Feedback**: Clear indicators of what needs fixing
5. **Regression Prevention**: Catches if parser changes break field extraction

## Usage

Run tests as usual:
```bash
dotnet test
```

Or in Visual Studio:
```
Test → Run All Tests
```

Tests will now show detailed column coverage statistics in the output.

## Expected Outcomes

### ✅ Ideal State
All columns for each parser should show ≥90% coverage:
- All parsers extract their expected fields consistently
- PDFs are well-formed and contain all expected data

### ⚠️ Warning State
Some columns show <90% coverage:
- May indicate parser bugs (column detection issues)
- May indicate PDF quality issues (missing data)
- Requires investigation

### ❌ Failure State
Test fails completely:
- Parser doesn't extract required fields (Position, Name)
- PDF parsing throws exception
- Requires immediate fix

## Next Steps

1. **Run Tests**: Execute tests on all PDF samples
2. **Review Output**: Check column coverage statistics
3. **Fix Parser Issues**: Address any columns with <90% coverage
4. **Validate**: Re-run tests to confirm fixes
5. **Document**: Update parser documentation with findings

## Date
2025-01-XX (Test enhancement implementation)
