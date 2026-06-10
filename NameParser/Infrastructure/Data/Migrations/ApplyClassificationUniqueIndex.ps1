# PowerShell script to apply the classification unique index migration
# This script adds a unique index on (RaceId, MemberFirstName, MemberLastName, Position)
# to allow duplicate names with different positions in the same race

Write-Host "Applying Classification Unique Index Migration..." -ForegroundColor Cyan

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$sqlFilePath = Join-Path $scriptPath "AddClassificationUniqueIndex.sql"

# Check if SQL file exists
if (-not (Test-Path $sqlFilePath)) {
	Write-Host "ERROR: SQL migration file not found at: $sqlFilePath" -ForegroundColor Red
	exit 1
}

# Connection string (uses LocalDB by default)
$connectionString = "Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=RaceManagementDb;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False"

Write-Host "Reading SQL migration file..." -ForegroundColor Yellow
$sqlContent = Get-Content $sqlFilePath -Raw

try {
	Write-Host "Connecting to database..." -ForegroundColor Yellow

	# Create connection
	$connection = New-Object System.Data.SqlClient.SqlConnection
	$connection.ConnectionString = $connectionString
	$connection.Open()

	Write-Host "Executing migration..." -ForegroundColor Yellow

	# Create command
	$command = $connection.CreateCommand()
	$command.CommandText = $sqlContent
	$command.CommandTimeout = 300 # 5 minutes

	# Execute and capture messages
	$connection.FireInfoMessageEventOnUserErrors = $true
	$connection.add_InfoMessage({
		param($sender, $eventArgs)
		Write-Host $eventArgs.Message -ForegroundColor Gray
	})

	$command.ExecuteNonQuery() | Out-Null

	Write-Host "`nMigration completed successfully!" -ForegroundColor Green
	Write-Host "The unique index IX_Classifications_RaceId_MemberFirstName_MemberLastName_Position has been created." -ForegroundColor Green
	Write-Host "This allows the same member name to appear multiple times in a race with different positions." -ForegroundColor Cyan
}
catch {
	Write-Host "`nERROR: Migration failed!" -ForegroundColor Red
	Write-Host $_.Exception.Message -ForegroundColor Red
	exit 1
}
finally {
	if ($connection -and $connection.State -eq 'Open') {
		$connection.Close()
		Write-Host "`nDatabase connection closed." -ForegroundColor Yellow
	}
}

Write-Host "`nPress any key to exit..." -ForegroundColor Cyan
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
