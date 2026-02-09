# Script to populate RaceEventDistances for upcoming races
# This ensures distances appear in the calendar section of mailings

Write-Host "=== Race Event Distances Configuration Tool ===" -ForegroundColor Cyan
Write-Host ""

# Common race distances (in km)
$commonDistances = @(
    @{ Name = "5 km"; Distance = 5.0 },
    @{ Name = "10 km"; Distance = 10.0 },
    @{ Name = "Semi-marathon (21.1 km)"; Distance = 21.1 },
    @{ Name = "Marathon (42.2 km)"; Distance = 42.2 }
)

Write-Host "This script will help you configure distances for race events." -ForegroundColor Yellow
Write-Host "Distances configured here will appear in the weekly newsletter calendar." -ForegroundColor Yellow
Write-Host ""

# Example usage - You'll need to get the RaceEventId from your database
Write-Host "Example: To add distances to a race event" -ForegroundColor Cyan
Write-Host ""
Write-Host "  1. Open the application and find the Race Event ID" -ForegroundColor White
Write-Host "  2. Use the UI or run this query in SQL:" -ForegroundColor White
Write-Host "     SELECT Id, Name, EventDate, Location FROM RaceEvents WHERE EventDate >= GETDATE() ORDER BY EventDate" -ForegroundColor Gray
Write-Host ""
Write-Host "  3. Then add distances using the application's Race Event management screen" -ForegroundColor White
Write-Host "     Or execute SQL like:" -ForegroundColor White
Write-Host "     INSERT INTO RaceEventDistances (RaceEventId, DistanceKm) VALUES (123, 10.0)" -ForegroundColor Gray
Write-Host ""

Write-Host "[INFO] Common Challenge Race Distances:" -ForegroundColor Cyan
Write-Host "  - Jogging de l'An Neuf: 5 km, 10 km" -ForegroundColor White
Write-Host "  - CrossCup / CJPL races: 10.2 km (typical)" -ForegroundColor White
Write-Host "  - Challenge Lucien Campeggio: Various (5-10 km)" -ForegroundColor White
Write-Host ""

Write-Host "=== Quick SQL Commands ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "-- View upcoming races without distances:" -ForegroundColor Green
Write-Host @"
SELECT re.Id, re.Name, re.EventDate, re.Location,
       (SELECT COUNT(*) FROM RaceEventDistances WHERE RaceEventId = re.Id) AS DistanceCount
FROM RaceEvents re
WHERE re.EventDate >= GETDATE()
ORDER BY re.EventDate
"@ -ForegroundColor Gray

Write-Host ""
Write-Host "-- Add a distance to a race event (replace 123 with actual RaceEventId):" -ForegroundColor Green
Write-Host "INSERT INTO RaceEventDistances (RaceEventId, DistanceKm) VALUES (123, 10.0)" -ForegroundColor Gray
Write-Host ""

Write-Host "=== Alternative: Automatic Distance Detection ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "The MemberMailingViewModel has been updated to automatically fall back to" -ForegroundColor Yellow
Write-Host "showing distances from past editions of the same race if RaceEventDistances" -ForegroundColor Yellow
Write-Host "is not configured. This means:" -ForegroundColor Yellow
Write-Host ""
Write-Host "  1. If you configure distances in RaceEventDistances -> Those will be shown" -ForegroundColor White
Write-Host "  2. If not configured -> Distances from past races with same RaceEventId will be shown" -ForegroundColor White
Write-Host "  3. If neither exists -> 'A confirmer' will be displayed" -ForegroundColor White
Write-Host ""

Write-Host "[OK] Update complete! The mailing system will now show distances." -ForegroundColor Green
Write-Host ""
Write-Host "To test:" -ForegroundColor Cyan
Write-Host "  1. Open the Member Mailing tab" -ForegroundColor White
Write-Host "  2. Select a date" -ForegroundColor White
Write-Host "  3. Click 'Generate Template'" -ForegroundColor White
Write-Host "  4. Check the calendar section for distances" -ForegroundColor White
Write-Host ""
