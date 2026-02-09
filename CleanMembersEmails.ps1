# Clean up Members.json email addresses
# Removes < >, trailing commas, and other invalid characters

$jsonPath = "NameParser\Members.json"
$backupPath = "NameParser\Members.json.backup"

Write-Host "Cleaning Members.json email addresses..." -ForegroundColor Cyan

# Create backup
Copy-Item $jsonPath $backupPath -Force
Write-Host "[OK] Backup created: $backupPath" -ForegroundColor Green

# Read the JSON file as text and fix common JSON syntax errors
$jsonContent = Get-Content $jsonPath -Raw

# Fix trailing comma before closing bracket (invalid JSON)
$jsonContent = $jsonContent -replace ',\s*\]', ']'
$jsonContent = $jsonContent -replace ',\s*\}', '}'

# Parse JSON
try {
    $members = $jsonContent | ConvertFrom-Json
}
catch {
    Write-Host "[ERROR] Failed to parse JSON: $_" -ForegroundColor Red
    exit 1
}

$cleanedCount = 0
$invalidCount = 0
$alreadyCleanCount = 0

foreach ($member in $members) {
    if ([string]::IsNullOrWhiteSpace($member.Email)) {
        continue
    }

    $originalEmail = $member.Email
    $cleanedEmail = $originalEmail

    # Remove "Name <email>" format - extract just the email
    if ($cleanedEmail -match '<(.+)>') {
        $cleanedEmail = $matches[1]
    }

    # Remove < and > brackets (in case they're standalone)
    $cleanedEmail = $cleanedEmail -replace '<', '' -replace '>', ''

    # Remove trailing commas, semicolons, colons
    $cleanedEmail = $cleanedEmail.TrimEnd(',', ';', ':', ' ', "`t", "`r", "`n")
    $cleanedEmail = $cleanedEmail.TrimStart(',', ';', ':', ' ', "`t", "`r", "`n")

    # Remove any spaces within the email
    $cleanedEmail = $cleanedEmail -replace '\s+', ''

    # Trim whitespace
    $cleanedEmail = $cleanedEmail.Trim()

    # Validate email
    if ($cleanedEmail -notmatch '@' -or $cleanedEmail.Length -lt 5) {
        Write-Host "  [WARN] Invalid email for $($member.FirstName) $($member.LastName): '$originalEmail' -> REMOVED" -ForegroundColor Yellow
        $member.Email = ""
        $invalidCount++
    }
    elseif ($originalEmail -ne $cleanedEmail) {
        Write-Host "  [OK] Cleaned: '$originalEmail' -> '$cleanedEmail'" -ForegroundColor Green
        $member.Email = $cleanedEmail
        $cleanedCount++
    }
    else {
        $alreadyCleanCount++
    }
}

# Convert back to JSON with proper formatting
$jsonOutput = $members | ConvertTo-Json -Depth 10

# Save the cleaned JSON
$jsonOutput | Set-Content $jsonPath -Encoding UTF8

Write-Host "`n=== Summary ===" -ForegroundColor Cyan
Write-Host "[OK] Cleaned: $cleanedCount emails" -ForegroundColor Green
Write-Host "[OK] Already clean: $alreadyCleanCount emails" -ForegroundColor Green
if ($invalidCount -gt 0) {
    Write-Host "[WARN] Invalid/Removed: $invalidCount emails" -ForegroundColor Yellow
}
Write-Host "[INFO] Total members: $($members.Count)" -ForegroundColor Cyan
Write-Host "`n[OK] Cleaned file saved to: $jsonPath" -ForegroundColor Green
Write-Host "[OK] Backup saved to: $backupPath" -ForegroundColor Green
