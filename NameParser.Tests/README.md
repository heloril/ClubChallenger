# NameParser.Tests

This project contains comprehensive unit tests for the PDF Parser functionality in the NameParser application.

## Test Structure

### Test Files

1. **PdfRaceResultRepositoryTests.cs**
   - Integration tests for PDF parsing functionality
   - Tests all PDF formats (CrossCup, Grand Challenge, Standard, etc.)
   - Performance and edge case tests
   - Comprehensive test of all PDFs in the PDF folder

2. **PdfParserUnitTests.cs**
   - Unit tests for specific parsing components
   - Metadata extraction tests
   - Format detection tests
   - Time and speed parsing tests
   - Name extraction and cleaning tests
   - Category extraction tests
   - Disqualification detection tests

3. **PdfParserDataQualityTests.cs**
   - Data quality validation tests
   - Consistency checks
   - Field validation
   - Parsing statistics
   - Special character handling

## Test Data

Test PDFs are automatically copied from the `NameParser\PDF` folder to the test output directory (`TestData\PDF`).

The following PDF files are used for testing:
- `2026-01-25_Jogging de la CrossCup_Hannut_CJPL_10.20.pdf`
- `2026-01-25_Jogging de la CrossCup_Hannut_CJPL_5.20.pdf`
- `2026-01-18_Jogging d'Hiver_Sprimont_CJPL_12.00.pdf`
- `2026-01-18_Jogging d'Hiver_Sprimont_CJPL_7.00.pdf`
- `2025-11-16_Les 10 Miles_Liège_CJPL_16.90.pdf`
- `2025-11-16_Les 10 Miles_Liège_CJPL_7.30.pdf`
- `20250421SeraingGC.pdf`
- `20250511BlancGravierGC.pdf`
- `Classement-10km-Jogging-de-lAn-Neuf.pdf`
- `Classement-5km-Jogging-de-lAn-Neuf.pdf`
- `Jogging de Boirs 2026.pdf`

## Running the Tests

### From Visual Studio
1. Open the solution in Visual Studio
2. Open Test Explorer (Test > Test Explorer)
3. Click "Run All Tests"

### From Command Line
```bash
dotnet test
```

### Run Specific Test Class
```bash
dotnet test --filter "FullyQualifiedName~PdfRaceResultRepositoryTests"
```

### Run Specific Test
```bash
dotnet test --filter "FullyQualifiedName~GetRaceResults_WithCrossCupFormat_ShouldParseSuccessfully"
```

## Test Coverage

The test suite covers:
- ✅ All PDF format types (CrossCup, Grand Challenge, Standard, French Column)
- ✅ Metadata extraction from various filename formats
- ✅ Time parsing (multiple formats)
- ✅ Speed parsing (with decimal corrections)
- ✅ Name extraction and cleaning
- ✅ Category and gender extraction
- ✅ Position parsing
- ✅ Disqualification detection
- ✅ Data quality validation
- ✅ Performance benchmarks
- ✅ Edge cases and error handling

## Test Technologies

- **xUnit**: Testing framework
- **FluentAssertions**: Fluent assertion library for readable tests
- **Moq**: Mocking framework (if needed for future tests)

## Adding New Tests

To add tests for a new PDF format:

1. Add the PDF file to the `NameParser\PDF` folder
2. The build process will automatically copy it to the test output
3. Add test methods in `PdfRaceResultRepositoryTests.cs`:
```csharp
[Fact]
public void GetRaceResults_WithNewFormat_ShouldParseSuccessfully()
{
    // Arrange
    var pdfPath = GetTestPdfPath("your-new-file.pdf");

    // Act
    var results = _repository.GetRaceResults(pdfPath, _testMembers);

    // Assert
    results.Should().NotBeEmpty();
    results.Count.Should().BeGreaterThan(2);
}
```

## Continuous Integration

These tests are designed to run in CI/CD pipelines. They:
- Do not require external dependencies
- Use relative paths for test data
- Have reasonable execution times
- Provide clear error messages

## Troubleshooting

### Test PDFs Not Found
If tests fail with "PDF file not found" errors:
1. Verify the PDF files exist in `NameParser\PDF`
2. Check the project file includes the PDF files with `<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>`
3. Rebuild the test project

### Parsing Failures
If specific PDF parsing tests fail:
1. Check the Debug output for parsing diagnostics
2. Verify the PDF format matches expected patterns
3. Review the parser implementation for the specific format

## Future Enhancements

Potential areas for additional testing:
- [ ] Multi-page PDF handling
- [ ] Large file performance tests
- [ ] Corrupted PDF handling
- [ ] Concurrent parsing tests
- [ ] Memory usage profiling
- [ ] Culture-specific number parsing
