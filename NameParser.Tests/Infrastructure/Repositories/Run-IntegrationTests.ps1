# PDF Parser Integration Test Runner
# This script runs all integration tests and provides detailed reporting

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "PDF Parser Integration Tests" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Change to test project directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$testProjectPath = Join-Path $scriptPath ".." ".." ".."
Set-Location $testProjectPath

# Check if TestFiles directory exists
$testFilesPath = Join-Path (Get-Location) "TestFiles"
if (-not (Test-Path $testFilesPath)) {
    Write-Host "❌ ERROR: TestFiles directory not found at:" -ForegroundColor Red
    Write-Host "   $testFilesPath" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please ensure the TestFiles directory exists with the required PDF files." -ForegroundColor Yellow
    exit 1
}

$pdfFiles = Get-ChildItem $testFilesPath -Filter "*.pdf"
Write-Host "Found $($pdfFiles.Count) PDF files in TestFiles directory" -ForegroundColor Green
Write-Host ""

# Test 1: Overall Integration Test
Write-Host "[1/4] Running comprehensive integration test..." -ForegroundColor White
Write-Host "This test parses all PDFs and compares actual vs expected result counts." -ForegroundColor Gray
Write-Host ""
dotnet test --filter "FullyQualifiedName~Integration_ParseAllPdfs_ShouldSucceed" --logger "console;verbosity=normal"
$test1Result = $LASTEXITCODE

# Test 2: Individual PDF Tests (Expected Counts)
Write-Host ""
Write-Host "[2/4] Testing individual PDF result counts..." -ForegroundColor White
Write-Host "Verifying each PDF produces the expected number of classification results." -ForegroundColor Gray
Write-Host ""
dotnet test --filter "FullyQualifiedName~ParsePdf_ShouldProduceExpectedNumberOfResults" --logger "console;verbosity=normal"
$test2Result = $LASTEXITCODE

# Test 3: Position Validation
Write-Host ""
Write-Host "[3/4] Validating position data..." -ForegroundColor White
Write-Host "Checking that parsed results contain valid position numbers." -ForegroundColor Gray
Write-Host ""
dotnet test --filter "FullyQualifiedName~ParsedResults_ShouldHaveValidPositions" --logger "console;verbosity=minimal"
$test3Result = $LASTEXITCODE

# Test 4: Name Validation
Write-Host ""
Write-Host "[4/4] Validating name data..." -ForegroundColor White
Write-Host "Checking that parsed results contain valid participant names." -ForegroundColor Gray
Write-Host ""
dotnet test --filter "FullyQualifiedName~ParsedResults_ShouldHaveValidNames" --logger "console;verbosity=minimal"
$test4Result = $LASTEXITCODE

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Integration Test Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

$allPassed = ($test1Result -eq 0) -and ($test2Result -eq 0) -and ($test3Result -eq 0) -and ($test4Result -eq 0)

function Write-TestResult {
    param($name, $result)
    if ($result -eq 0) {
        Write-Host "✓ $name" -ForegroundColor Green
    } else {
        Write-Host "✗ $name" -ForegroundColor Red
    }
}

Write-TestResult "Comprehensive integration test" $test1Result
Write-TestResult "Individual PDF result counts" $test2Result
Write-TestResult "Position data validation" $test3Result
Write-TestResult "Name data validation" $test4Result

Write-Host ""
if ($allPassed) {
    Write-Host "🎉 All integration tests PASSED! ✓" -ForegroundColor Green
    Write-Host ""
    Write-Host "Summary:" -ForegroundColor Cyan
    Write-Host "  • All $($pdfFiles.Count) PDF files parsed successfully" -ForegroundColor White
    Write-Host "  • Expected result counts match actual counts" -ForegroundColor White
    Write-Host "  • Position data is valid" -ForegroundColor White
    Write-Host "  • Name data is valid" -ForegroundColor White
    Write-Host ""
    Write-Host "The PDF parser is working correctly! 🚀" -ForegroundColor Green
    exit 0
} else {
    Write-Host "⚠️  Some integration tests FAILED! ✗" -ForegroundColor Red
    Write-Host ""
    Write-Host "Review the test output above for details." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Common issues and solutions:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  1. Result count mismatch:" -ForegroundColor Cyan
    Write-Host "     • Check if smart table extraction is working correctly" -ForegroundColor White
    Write-Host "     • Verify orphaned time line buffering is active" -ForegroundColor White
    Write-Host "     • Review PDF format detection (CrossCup, GrandChallenge, etc.)" -ForegroundColor White
    Write-Host ""
    Write-Host "  2. Missing positions:" -ForegroundColor Cyan
    Write-Host "     • Check position extraction regex patterns" -ForegroundColor White
    Write-Host "     • Verify column detection for tabular formats" -ForegroundColor White
    Write-Host ""
    Write-Host "  3. Invalid names:" -ForegroundColor Cyan
    Write-Host "     • Check name cleaning logic (CleanExtractedName)" -ForegroundColor White
    Write-Host "     • Verify name parsing doesn't stop too early" -ForegroundColor White
    Write-Host "     • Review category/marker detection" -ForegroundColor White
    Write-Host ""
    Write-Host "  4. PDF file not found:" -ForegroundColor Cyan
    Write-Host "     • Ensure all test PDFs exist in TestFiles directory" -ForegroundColor White
    Write-Host "     • Check file naming matches test data exactly" -ForegroundColor White
    Write-Host ""
    Write-Host "Debug tips:" -ForegroundColor Yellow
    Write-Host "  • Run individual tests for failing PDFs" -ForegroundColor White
    Write-Host "  • Check Debug output for parsing statistics" -ForegroundColor White
    Write-Host "  • Compare expected vs actual counts in test output" -ForegroundColor White
    Write-Host "  • Review smart extraction quality checks" -ForegroundColor White
    Write-Host ""
    exit 1
}
