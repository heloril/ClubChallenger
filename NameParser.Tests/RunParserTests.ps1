# PowerShell script to run parser tests and identify failures
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "PDF Parser Test Runner" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Run the specific test
Write-Host "Running ParsePdf_ShouldProduceExpectedNumberOfResults tests..." -ForegroundColor Yellow
dotnet test --filter "FullyQualifiedName~ParsePdf_ShouldProduceExpectedNumberOfResults" --logger "console;verbosity=detailed"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Test run complete!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
