# Apply the UpdateRaceUniqueIndexForDistances migration
# This script applies the migration to update the unique index on Races table

param(
    [string]$ServerInstance = ".\SQLEXPRESS",
    [string]$Database = "RaceManagement"
)

Write-Host "Applying UpdateRaceUniqueIndexForDistances migration..." -ForegroundColor Cyan
Write-Host "Server: $ServerInstance" -ForegroundColor Gray
Write-Host "Database: $Database" -ForegroundColor Gray

$ScriptPath = Join-Path $PSScriptRoot "UpdateRaceUniqueIndexForDistances.sql"

try {
    # Use Invoke-Sqlcmd if available (SQL Server module)
    if (Get-Command Invoke-Sqlcmd -ErrorAction SilentlyContinue) {
        Invoke-Sqlcmd -ServerInstance $ServerInstance -Database $Database -InputFile $ScriptPath
        Write-Host "Migration applied successfully!" -ForegroundColor Green
    }
    else {
        # Fallback to sqlcmd.exe
        $sqlcmdPath = "sqlcmd"
        & $sqlcmdPath -S $ServerInstance -d $Database -i $ScriptPath
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Migration applied successfully!" -ForegroundColor Green
        }
        else {
            Write-Host "Migration failed with exit code: $LASTEXITCODE" -ForegroundColor Red
            exit $LASTEXITCODE
        }
    }
}
catch {
    Write-Host "Error applying migration: $_" -ForegroundColor Red
    exit 1
}
