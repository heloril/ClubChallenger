# 🚀 Quick Start - PDF Parser Integration Tests

## Run All Tests (Recommended)

```powershell
.\NameParser.Tests\Infrastructure\Repositories\Run-IntegrationTests.ps1
```

## Quick Commands

```bash
# Run all integration tests
dotnet test --filter "FullyQualifiedName~PdfParserIntegrationTests"

# Run comprehensive summary test
dotnet test --filter "FullyQualifiedName~Integration_ParseAllPdfs_ShouldSucceed"

# Run individual PDF tests
dotnet test --filter "FullyQualifiedName~ParsePdf_ShouldProduceExpectedNumberOfResults"
```

## Expected Results Summary

| Race Format | PDFs | Total Results |
|-------------|------|---------------|
| CrossCup/CJPL | 6 | 962 |
| Grand Challenge | 2 | 484 |
| Jogging de l'An Neuf | 2 | 544 |
| Les 10 Miles | 2 | 380 |
| Jogging de Boirs | 1 | 126 |
| **TOTAL** | **13** | **2,496** |

## Test Success Criteria

✅ **Result Count**: Actual = Expected (±0 tolerance)  
✅ **Positions**: ≥90% have valid position numbers  
✅ **Names**: ≥95% have valid participant names  

## Common Issues

### Result Count Mismatch
- Check smart extraction quality logs
- Verify orphaned time buffering
- Review format detection

### Missing Positions
- Check position regex patterns
- Verify column detection

### Invalid Names
- Review CleanExtractedName logic
- Check category marker detection

## Debug Tips

```bash
# Verbose output
dotnet test --filter "FullyQualifiedName~PdfParserIntegrationTests" --logger "console;verbosity=detailed"

# Single PDF test
dotnet test --filter "DisplayName~CrossCup"
```

## Files Created

✅ `PdfParserIntegrationTests.cs` - Test class (13 PDFs)  
✅ `Run-IntegrationTests.ps1` - PowerShell runner  
✅ `INTEGRATION_TESTS_README.md` - Full documentation  
✅ `INTEGRATION_TESTS_QUICK_START.md` - This file  

---
For detailed documentation, see: `INTEGRATION_TESTS_README.md`
