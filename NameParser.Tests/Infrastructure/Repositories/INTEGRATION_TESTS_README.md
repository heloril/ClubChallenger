# PDF Parser Integration Tests

## Overview

This directory contains comprehensive integration tests for the PDF race result parser. These tests validate that each PDF file is parsed correctly and produces the expected number of classification results.

## Test Files

### PdfParserIntegrationTests.cs

Main integration test class that validates:
- **Expected Result Counts**: Each PDF produces exactly the expected number of classification entries
- **Valid Positions**: At least 90% of results have valid position numbers
- **Valid Names**: At least 95% of results have valid participant names
- **Comprehensive Summary**: Overall integration report across all PDFs

## Test Coverage

The integration tests cover **13 PDF files** across multiple race formats:

| PDF File | Race | Distance | Expected Count |
|----------|------|----------|----------------|
| `2026-01-25_Jogging de la CrossCup_Hannut_CJPL_10.20.pdf` | CrossCup 10km | 10.20 km | 110 |
| `2026-01-25_Jogging de la CrossCup_Hannut_CJPL_5.20.pdf` | CrossCup 5km | 5.20 km | 84 |
| `2026-01-18_Jogging d'Hiver_Sprimont_CJPL_12.00.pdf` | Jogging d'Hiver 12km | 12.00 km | 175 |
| `2026-01-18_Jogging d'Hiver_Sprimont_CJPL_7.00.pdf` | Jogging d'Hiver 7km | 7.00 km | 175 |
| `2026-02-01_Les Collines de Cointe_Liège_CJPL_5.00.pdf` | Les Collines de Cointe 5km | 5.00 km | 156 |
| `2026-02-01_Les Collines de Cointe_Liège_CJPL_10.00.pdf` | Les Collines de Cointe 10km | 10.00 km | 262 |
| `20250421SeraingGC.pdf` | Grand Challenge Seraing | 10.00 km | 279 |
| `20250511BlancGravierGC.pdf` | Grand Challenge Blanc Gravier | 10.00 km | 205 |
| `Classement-10km-Jogging-de-lAn-Neuf.pdf` | Jogging de l'An Neuf 10km | 10.00 km | 354 |
| `Classement-5km-Jogging-de-lAn-Neuf.pdf` | Jogging de l'An Neuf 5km | 5.00 km | 190 |
| `2025-11-16_Les 10 Miles_Liège_CJPL_16.90.pdf` | Les 10 Miles 16.9km | 16.90 km | 217 |
| `2025-11-16_Les 10 Miles_Liège_CJPL_7.30.pdf` | Les 10 Miles 7.3km | 7.30 km | 163 |
| `Jogging de Boirs 2026.pdf` | Jogging de Boirs | 10.00 km | 126* |

\* *Combined: 78 + 48 = 126 (multiple classifications in same PDF)*

**Total Expected Results Across All PDFs: 2,496 classification entries**

## Running the Tests

### Using PowerShell Script (Recommended)

```powershell
# Run all integration tests with detailed reporting
.\NameParser.Tests\Infrastructure\Repositories\Run-IntegrationTests.ps1
```

This script will:
1. ✅ Verify TestFiles directory exists
2. ✅ Run comprehensive integration test
3. ✅ Test individual PDF result counts
4. ✅ Validate position data
5. ✅ Validate name data
6. ✅ Provide detailed summary and troubleshooting tips

### Using .NET CLI

```bash
# Run all integration tests
dotnet test --filter "FullyQualifiedName~PdfParserIntegrationTests"

# Run specific test categories
dotnet test --filter "FullyQualifiedName~Integration_ParseAllPdfs_ShouldSucceed"
dotnet test --filter "FullyQualifiedName~ParsePdf_ShouldProduceExpectedNumberOfResults"
dotnet test --filter "FullyQualifiedName~ParsedResults_ShouldHaveValidPositions"
dotnet test --filter "FullyQualifiedName~ParsedResults_ShouldHaveValidNames"
```

## Test Requirements

### Prerequisites

1. **TestFiles Directory**: Must exist at `NameParser.Tests/TestFiles/`
2. **PDF Files**: All 13 test PDF files must be present
3. **.NET 8 SDK**: Required to run tests

### File Structure

```
NameParser.Tests/
├── TestFiles/
│   ├── 2026-01-25_Jogging de la CrossCup_Hannut_CJPL_10.20.pdf
│   ├── 2026-01-25_Jogging de la CrossCup_Hannut_CJPL_5.20.pdf
│   ├── 2026-01-18_Jogging d'Hiver_Sprimont_CJPL_12.00.pdf
│   ├── 2026-01-18_Jogging d'Hiver_Sprimont_CJPL_7.00.pdf
│   ├── 2026-02-01_Les Collines de Cointe_Liège_CJPL_5.00.pdf
│   ├── 2026-02-01_Les Collines de Cointe_Liège_CJPL_10.00.pdf
│   ├── 20250421SeraingGC.pdf
│   ├── 20250511BlancGravierGC.pdf
│   ├── Classement-10km-Jogging-de-lAn-Neuf.pdf
│   ├── Classement-5km-Jogging-de-lAn-Neuf.pdf
│   ├── 2025-11-16_Les 10 Miles_Liège_CJPL_16.90.pdf
│   ├── 2025-11-16_Les 10 Miles_Liège_CJPL_7.30.pdf
│   └── Jogging de Boirs 2026.pdf
└── Infrastructure/
    └── Repositories/
        ├── PdfParserIntegrationTests.cs
        └── Run-IntegrationTests.ps1
```

## Understanding Test Results

### Success Indicators

✅ **All Tests Pass**
```
🎉 All integration tests PASSED! ✓

Summary:
  • All 13 PDF files parsed successfully
  • Expected result counts match actual counts
  • Position data is valid
  • Name data is valid

The PDF parser is working correctly! 🚀
```

### Failure Scenarios

❌ **Result Count Mismatch**
```
✗ FAIL | 2026-02-01_Les Collines de Cointe_Liège_CJPL_10.00.pdf
       Expected: 262, Actual: 250
```

**Possible Causes:**
- Missing entries (actual < expected)
- Orphaned time lines not being detected
- Smart table extraction quality checks failing
- Format parser not detecting PDF correctly

**Solutions:**
1. Check smart extraction debug output
2. Verify orphaned time line buffering is working
3. Review format detection (CrossCup, GrandChallenge, etc.)
4. Check for page breaks or multi-page handling issues

❌ **Invalid Positions**
```
✗ ParsedResults_ShouldHaveValidPositions
   At least 90% of results should have valid positions
```

**Possible Causes:**
- Position regex not matching format
- Column detection issues in tabular PDFs
- Position field extraction incorrect

**Solutions:**
1. Check `PositionPattern` regex in format parsers
2. Verify column boundary detection
3. Review position extraction logic

❌ **Invalid Names**
```
✗ ParsedResults_ShouldHaveValidNames
   At least 95% of results should have valid names
```

**Possible Causes:**
- Name cleaning logic too aggressive
- Name parsing stopping too early at markers
- Category/marker detection interfering with names

**Solutions:**
1. Review `CleanExtractedName` logic
2. Check category detection patterns
3. Verify name extraction doesn't stop prematurely

## Key Features Tested

### 1. Smart Table Extraction
- Adaptive row grouping based on font size
- Column boundary detection
- Quality-based fallback to standard extraction

### 2. Orphaned Time Line Buffering
- Detects times on separate lines
- Merges with participant data
- Pattern: `00:22:26 898` followed by participant row

### 3. Format Detection
- **CrossCup/CJPL**: Standardized CJPL race format
- **Grand Challenge**: GC-specific format
- **French Column**: Column-based with header detection
- **Standard**: Fallback parser for generic formats

### 4. Data Validation
- Position numbers (1-4 digits)
- Participant names (first + last)
- Race times (HH:MM:SS or MM:SS)
- Speed values (km/h)
- Categories and positions by sex/category

## Troubleshooting

### Test Files Not Found

```powershell
❌ ERROR: TestFiles directory not found
```

**Solution:** Create `TestFiles` directory and add all required PDFs

### Compilation Errors

```bash
dotnet build
```

Verify all dependencies are installed and code compiles successfully.

### Debugging Individual PDFs

Run a single PDF test:

```bash
dotnet test --filter "FullyQualifiedName~ParsePdf_ShouldProduceExpectedNumberOfResults and DisplayName~CrossCup"
```

### Viewing Debug Output

Enable verbose logging:

```bash
dotnet test --filter "FullyQualifiedName~PdfParserIntegrationTests" --logger "console;verbosity=detailed"
```

Look for:
- Smart extraction quality check logs
- Format parser detection logs
- Parsing statistics (successful/failed/skipped lines)

## Expected Behavior

### Result Count Calculation

Classification results are counted as entries with `ID >= 2`:
- **ID 0**: Header row
- **ID 1**: Reference time (TREF) - if present
- **ID 2+**: Classification results

Example:
```
0: Header;Position;Name;Time;Team;Speed;
1: TREF;00:15:00;RACETYPE;RACE_TIME;
2: TMEM;1;DUPONT;Jean;00:35:25;...
3: TWINNER;2;MARTIN;Pierre;00:36:15;...
...
```

### Multi-Classification PDFs

Some PDFs contain multiple classifications (e.g., "Jogging de Boirs 2026.pdf"):
- 78 results in first classification
- 48 results in second classification
- **Total: 126 results**

The parser handles these correctly and includes all entries.

## Maintenance

### Adding New Test PDFs

1. Add PDF to `TestFiles` directory
2. Update `PdfTestData` in `PdfParserIntegrationTests.cs`:

```csharp
new object[] { "new-race.pdf", "Race Name", 10.00, 250 }
```

3. Run integration tests to verify
4. Update this README with new entry

### Updating Expected Counts

If PDF contents change:
1. Run tests to see actual counts
2. Verify actual count is correct (not a parsing bug)
3. Update expected count in test data
4. Update README table

## CI/CD Integration

### GitHub Actions

```yaml
- name: Run Integration Tests
  run: |
    cd NameParser.Tests
    pwsh Infrastructure/Repositories/Run-IntegrationTests.ps1
```

### Azure DevOps

```yaml
- task: PowerShell@2
  inputs:
    filePath: 'NameParser.Tests/Infrastructure/Repositories/Run-IntegrationTests.ps1'
    errorActionPreference: 'stop'
```

## Related Documentation

- **Unit Tests**: `PdfParserUnitTests.cs` - Low-level parser unit tests
- **Data Quality Tests**: `PdfParserDataQualityTests.cs` - Data completeness validation
- **Row Count Validation**: `Validate-RowCounts.ps1` - Row count validation script

## Support

For issues or questions:
1. Check test output for specific error messages
2. Review debug logs for parser statistics
3. Verify PDF files are not corrupted
4. Check format parser detection is working
5. Ensure smart extraction quality checks pass

---

**Last Updated**: January 2026  
**Test Coverage**: 13 PDFs, 2,496 expected results  
**Success Rate Target**: 100%
