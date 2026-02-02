# PowerShell script to fix framework version mismatch
# Run this from the solution directory

Write-Host "Fixing .NET Framework version mismatch..." -ForegroundColor Cyan
Write-Host ""

# Fix NameParser.UI project
$uiProjectFile = "NameParser.UI\NameParser.UI.csproj"

if (Test-Path $uiProjectFile) {
    Write-Host "Updating NameParser.UI project..." -ForegroundColor Yellow
    
    $content = Get-Content $uiProjectFile -Raw
    
    if ($content -match '<TargetFrameworkVersion>v4\.7\.2</TargetFrameworkVersion>') {
        $content = $content -replace '<TargetFrameworkVersion>v4\.7\.2</TargetFrameworkVersion>', '<TargetFrameworkVersion>v4.8</TargetFrameworkVersion>'
        $content | Set-Content $uiProjectFile -NoNewline
        Write-Host "✓ NameParser.UI now targets .NET Framework 4.8" -ForegroundColor Green
    } elseif ($content -match '<TargetFrameworkVersion>v4\.8</TargetFrameworkVersion>') {
        Write-Host "✓ NameParser.UI already targets .NET Framework 4.8" -ForegroundColor Green
    } else {
        Write-Host "⚠ Could not find TargetFrameworkVersion in NameParser.UI.csproj" -ForegroundColor Yellow
    }
} else {
    Write-Host "✗ NameParser.UI\NameParser.UI.csproj not found!" -ForegroundColor Red
    Write-Host "  Make sure you're running this from the solution directory." -ForegroundColor Yellow
}

Write-Host ""

# Check NameParser project
$mainProjectFile = "NameParser\NameParser.csproj"

if (Test-Path $mainProjectFile) {
    $content = Get-Content $mainProjectFile -Raw
    
    if ($content -match '<TargetFrameworkVersion>v4\.8</TargetFrameworkVersion>') {
        Write-Host "✓ NameParser targets .NET Framework 4.8" -ForegroundColor Green
    } else {
        Write-Host "⚠ NameParser does not target .NET Framework 4.8" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "Done!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Reopen Visual Studio" -ForegroundColor White
Write-Host "2. Right-click NameParser.UI → Add → Reference → Projects → Check 'NameParser'" -ForegroundColor White
Write-Host "3. Install EntityFramework packages (see INSTALL_ENTITY_FRAMEWORK.md)" -ForegroundColor White
Write-Host "4. Add System references to NameParser (see ADD_SYSTEM_REFERENCE.md)" -ForegroundColor White
Write-Host "5. Rebuild Solution" -ForegroundColor White
Write-Host ""
