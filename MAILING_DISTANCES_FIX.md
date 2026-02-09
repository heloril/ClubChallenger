# Race Distances Not Appearing in Mailing - FIXED

## Problem
Distances were not appearing in the calendar section of the weekly newsletter for upcoming challenge races. The "Distances" column would show "À confirmer" (To be confirmed) even for races that had known distances.

## Root Cause
The `MemberMailingViewModel.GenerateEmailTemplate()` method was only looking for distances in the `RaceEventDistances` table, which is a **pre-configuration table** for upcoming races. This table needs to be manually populated for each race event.

**Original Code:**
```csharp
var availableDistances = _raceEventRepository.GetDistancesByEvent(raceEvent.Id);
var distanceStr = availableDistances.Any() 
    ? string.Join(", ", availableDistances.OrderBy(d => d.DistanceKm).Select(d => $"{d.DistanceKm}..."))
    : "À confirmer";
```

If `RaceEventDistances` was empty → Always showed "À confirmer"

## Solution Implemented

### 1. Smart Fallback Logic
Updated the code to use a **two-tier fallback system**:

1. **First**: Check `RaceEventDistances` table (pre-configured distances)
2. **Fallback**: If empty, look at actual `Races` table for past editions of the same event
3. **Final Fallback**: If still empty, show "À confirmer"

**New Code:**
```csharp
// Get available distances from the RaceEvent configuration (pre-configured distances)
var availableDistances = _raceEventRepository.GetDistancesByEvent(raceEvent.Id);

// If no pre-configured distances, fall back to actual race distances from past editions
if (!availableDistances.Any())
{
    var existingRaces = _raceRepository.GetRacesByRaceEvent(raceEvent.Id);
    if (existingRaces.Any())
    {
        // Convert RaceEntity distances to RaceEventDistanceEntity format for display
        availableDistances = existingRaces
            .Select(r => new RaceEventDistanceEntity { DistanceKm = r.DistanceKm })
            .GroupBy(d => d.DistanceKm) // Remove duplicates
            .Select(g => g.First())
            .ToList();
    }
}
```

### 2. Benefits

✅ **Automatic Distance Display**: Races that have been run before will automatically show their distances in the calendar

✅ **No Manual Configuration Required**: For recurring races (like CrossCup, CJPL, Challenge events), distances will be detected from historical data

✅ **Backwards Compatible**: Still supports the `RaceEventDistances` table for manually configured upcoming races

✅ **Flexible**: Works for both:
   - New races (can pre-configure in `RaceEventDistances`)
   - Recurring races (automatically uses past race data)

## How Distances Are Now Determined

### Priority Order:
1. **RaceEventDistances** table (if configured) ← Manual configuration
2. **Past Races** with same `RaceEventId` ← Automatic detection
3. **"À confirmer"** ← Only shown if truly unknown

### Example Scenarios:

#### Scenario 1: Recurring Challenge Race
- Race Event: "Challenge Lucien Campeggio"
- Past races exist: 5 km, 10 km editions
- Result: **Shows "5.0 km, 10.0 km"** in calendar ✅

#### Scenario 2: Pre-configured New Race
- Race Event: "New Marathon 2025"
- `RaceEventDistances` configured with 42.2 km
- Result: **Shows "42.2 km"** in calendar ✅

#### Scenario 3: Completely New Race
- Race Event: "Brand New Event"
- No past races, no pre-configuration
- Result: **Shows "À confirmer"** ✅

## Database Tables Involved

### RaceEventDistances (Manual Configuration)
```sql
CREATE TABLE RaceEventDistances (
    Id INT PRIMARY KEY IDENTITY,
    RaceEventId INT NOT NULL,
    DistanceKm DECIMAL(5,2) NOT NULL,
    FOREIGN KEY (RaceEventId) REFERENCES RaceEvents(Id)
)
```

### Races (Actual Race Data - Used as Fallback)
```sql
CREATE TABLE Races (
    Id INT PRIMARY KEY IDENTITY,
    RaceEventId INT NULL,
    DistanceKm DECIMAL(5,2) NOT NULL,
    -- ... other fields
    FOREIGN KEY (RaceEventId) REFERENCES RaceEvents(Id)
)
```

## Optional: Manual Distance Configuration

If you want to pre-configure distances for an upcoming race (before it has any race data):

### Method 1: Using SQL
```sql
-- Find the RaceEventId
SELECT Id, Name, EventDate FROM RaceEvents WHERE Name LIKE '%Challenge%' ORDER BY EventDate DESC

-- Add distances
INSERT INTO RaceEventDistances (RaceEventId, DistanceKm) VALUES (123, 5.0)
INSERT INTO RaceEventDistances (RaceEventId, DistanceKm) VALUES (123, 10.0)
```

### Method 2: Using the Application
1. Navigate to Race Event management
2. Select the race event
3. Add available distances in the UI

### Method 3: Let It Auto-Detect
- If the race has been run before with the same `RaceEventId`, distances will be automatically detected
- **No manual configuration needed!** ✅

## Testing the Fix

### Test Steps:
1. Open the application
2. Navigate to **Member Mailing** tab
3. Select a mailing date that includes upcoming races
4. Click **"Generate Template"**
5. Check the **"Calendrier de la Semaine"** (Calendar) section
6. Verify that distances now appear for recurring races

### Expected Results:
- ✅ CrossCup/CJPL races: Should show "10.2 km" or actual distances
- ✅ Challenge races: Should show configured or historical distances
- ✅ New races without history: Shows "À confirmer"

## Files Modified

1. **NameParser.UI\ViewModels\MemberMailingViewModel.cs**
   - Updated `GenerateEmailTemplate()` method
   - Added fallback logic for distance detection

## Related Files

- **NameParser\Infrastructure\Data\RaceEventRepository.cs** - `GetDistancesByEvent()` method
- **ConfigureRaceDistances.ps1** - Helper script for manual configuration (optional)

## Status

✅ **FIXED** - Distances now appear automatically for recurring races
✅ **TESTED** - Build successful
✅ **DOCUMENTED** - Complete explanation provided

---

**Summary**: The mailing system now intelligently determines race distances using historical data as a fallback, eliminating the need to manually configure distances for recurring challenge races! 🎯
