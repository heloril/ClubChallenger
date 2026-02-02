# PowerShell script to fix project references
# Run this from the solution directory

Write-Host "Adding missing assembly references to NameParser.csproj..." -ForegroundColor Green

$projectFile = "NameParser.csproj"

if (-Not (Test-Path $projectFile)) {
    Write-Host "Error: NameParser.csproj not found in current directory!" -ForegroundColor Red
    Write-Host "Please run this script from the solution directory." -ForegroundColor Yellow
    exit 1
}

# Read the project file
$content = Get-Content $projectFile -Raw

# Check if System reference already exists
if ($content -notmatch '<Reference Include="System"') {
    Write-Host "Adding System reference..." -ForegroundColor Yellow
    
    # Find the closing tag of the first ItemGroup with References
    $pattern = '(<Reference Include="Microsoft\.CSharp"[^>]*/>)'
    $replacement = '$1' + "`n    <Reference Include=`"System`" />"
    
    $content = $content -replace $pattern, $replacement
    
    Write-Host "✓ System reference added" -ForegroundColor Green
} else {
    Write-Host "✓ System reference already exists" -ForegroundColor Green
}

# Check if System.ComponentModel.DataAnnotations reference exists
if ($content -notmatch '<Reference Include="System\.ComponentModel\.DataAnnotations"') {
    Write-Host "Adding System.ComponentModel.DataAnnotations reference..." -ForegroundColor Yellow
    
    # Add after System reference
    $pattern = '(<Reference Include="System"[^>]*/>)'
    $replacement = '$1' + "`n    <Reference Include=`"System.ComponentModel.DataAnnotations`" />"
    
    $content = $content -replace $pattern, $replacement
    
    Write-Host "✓ System.ComponentModel.DataAnnotations reference added" -ForegroundColor Green
} else {
    Write-Host "✓ System.ComponentModel.DataAnnotations reference already exists" -ForegroundColor Green
}

# Save the modified content
$content | Set-Content $projectFile -NoNewline

Write-Host ""
Write-Host "Project file updated successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Close and reopen Visual Studio" -ForegroundColor White
Write-Host "2. Install Entity Framework packages:" -ForegroundColor White
Write-Host "   Install-Package EntityFramework -Version 6.4.4 -ProjectName NameParser" -ForegroundColor Yellow
Write-Host "   Install-Package EntityFramework -Version 6.4.4 -ProjectName NameParser.UI" -ForegroundColor Yellow
Write-Host "3. Rebuild the solution" -ForegroundColor White
Write-Host ""
