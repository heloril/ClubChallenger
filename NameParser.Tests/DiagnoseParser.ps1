# Parser Diagnostic Script
# Run this to see detailed parsing information for each PDF

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "PDF Parser Diagnostic Tool" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$testPdfs = @(
    @{ Name = "CrossCup 10km"; File = "2026-01-25_Jogging de la CrossCup_Hannut_CJPL_10.20.pdf"; Expected = 110; Parser = "OtopFormatParser" }
    @{ Name = "CrossCup 5km"; File = "2026-01-25_Jogging de la CrossCup_Hannut_CJPL_5.20.pdf"; Expected = 84; Parser = "OtopFormatParser" }
    @{ Name = "Jogging d'Hiver 12km"; File = "2026-01-18_Jogging d'Hiver_Sprimont_CJPL_12.00.pdf"; Expected = 175; Parser = "OtopFormatParser" }
    @{ Name = "Jogging d'Hiver 7km"; File = "2026-01-18_Jogging d'Hiver_Sprimont_CJPL_7.00.pdf"; Expected = 175; Parser = "OtopFormatParser" }
    @{ Name = "Collines de Cointe 5km"; File = "2026-02-01_Les Collines de Cointe_Liège_CJPL_5.00.pdf"; Expected = 156; Parser = "OtopFormatParser" }
    @{ Name = "Collines de Cointe 10km"; File = "2026-02-01_Les Collines de Cointe_Liège_CJPL_10.00.pdf"; Expected = 262; Parser = "OtopFormatParser" }
    @{ Name = "Seraing GC"; File = "20250421SeraingGC.pdf"; Expected = 279; Parser = "GoalTimingFormatParser" }
    @{ Name = "Blanc Gravier GC"; File = "20250511BlancGravierGC.pdf"; Expected = 205; Parser = "GoalTimingFormatParser" }
    @{ Name = "An Neuf 10km"; File = "Classement-10km-Jogging-de-lAn-Neuf.pdf"; Expected = 354; Parser = "GlobalPacingFormatParser" }
    @{ Name = "An Neuf 5km"; File = "Classement-5km-Jogging-de-lAn-Neuf.pdf"; Expected = 190; Parser = "GlobalPacingFormatParser" }
    @{ Name = "10 Miles 16.9km"; File = "2025-11-16_Les 10 Miles_Liège_CJPL_16.90.pdf"; Expected = 217; Parser = "OtopFormatParser" }
    @{ Name = "10 Miles 7.3km"; File = "2025-11-16_Les 10 Miles_Liège_CJPL_7.30.pdf"; Expected = 163; Parser = "OtopFormatParser" }
    @{ Name = "Zatopek 6.5km"; File = "La Zatopek en Famille 6.5kms.pdf"; Expected = 286; Parser = "ChallengeLaMeuseFormatParser" }
    @{ Name = "Zatopek 10km"; File = "La Zatopek en Famille 10kms.pdf"; Expected = 469; Parser = "ChallengeLaMeuseFormatParser" }
    @{ Name = "Zatopek 21km"; File = "La Zatopek en Famille 21kms.pdf"; Expected = 288; Parser = "ChallengeLaMeuseFormatParser" }
)

Write-Host "Testing PDF parsing with Debug output..." -ForegroundColor Yellow
Write-Host "Check Visual Studio Output window (Debug) for detailed logs" -ForegroundColor Yellow
Write-Host ""

# Run single test to see debug output
Write-Host "Running diagnostic test..." -ForegroundColor Green
dotnet test --filter "FullyQualifiedName~ParsePdf_ShouldProduceExpectedNumberOfResults" `
    --logger "console;verbosity=detailed" `
    -- RunConfiguration.EnvironmentVariables.VSTEST_HOST_DEBUG=0

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Diagnostic Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Check the test output above for:" -ForegroundColor Yellow
Write-Host "  1. Which parser was selected for each PDF" -ForegroundColor White
Write-Host "  2. How many columns were detected" -ForegroundColor White
Write-Host "  3. How many results were parsed" -ForegroundColor White
Write-Host "  4. Column coverage percentages" -ForegroundColor White
Write-Host ""
Write-Host "Common Issues:" -ForegroundColor Yellow
Write-Host "  - Wrong parser selected → Check CanParse() logic" -ForegroundColor White
Write-Host "  - 0 columns detected → Check header detection" -ForegroundColor White
Write-Host "  - Low result count → Check data row parsing" -ForegroundColor White
Write-Host "  - Low column coverage → Check column extraction" -ForegroundColor White
Write-Host ""
