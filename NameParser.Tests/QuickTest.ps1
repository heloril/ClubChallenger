# Quick Test Runner - Shows Pass/Fail Status
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Quick Parser Test Status" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Running all parser tests..." -ForegroundColor Yellow
Write-Host ""

# Run tests and capture output
$output = dotnet test --filter "FullyQualifiedName~ParsePdf_ShouldProduceExpectedNumberOfResults" --logger "console;verbosity=normal" 2>&1

# Parse results
$output | ForEach-Object {
    $line = $_.ToString()
    
    # Highlight passed tests
    if ($line -match "Passed.*ParsePdf_ShouldProduceExpectedNumberOfResults") {
        Write-Host $line -ForegroundColor Green
    }
    # Highlight failed tests
    elseif ($line -match "Failed.*ParsePdf_ShouldProduceExpectedNumberOfResults") {
        Write-Host $line -ForegroundColor Red
    }
    # Show summary
    elseif ($line -match "Total tests:.*Passed:.*Failed:") {
        Write-Host ""
        Write-Host $line -ForegroundColor Cyan
        Write-Host ""
    }
    # Show other important lines
    elseif ($line -match "Test run|Starting test") {
        Write-Host $line -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. For detailed debug info: ./DiagnoseParser.ps1" -ForegroundColor White
Write-Host "  2. To run specific test: dotnet test --filter 'DisplayName~[FileName]'" -ForegroundColor White
Write-Host "  3. Check PARSER_DIAGNOSTIC_FIXES.md for troubleshooting" -ForegroundColor White
Write-Host "========================================" -ForegroundColor Cyan
