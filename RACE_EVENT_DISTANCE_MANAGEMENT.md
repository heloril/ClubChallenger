# Race Event Distance Management Feature

## Overview
This feature adds the ability to manage predefined race distances for each race event. Race distances can now be decimal values (e.g., 10, 21.1, 42.195 km) and can be added or removed through the UI or imported from Excel.

## New Database Table
**RaceEventDistances**
- `Id`: Primary key
- `RaceEventId`: Foreign key to RaceEvents
- `DistanceKm`: DECIMAL(10,3) - The distance in kilometers

## UI Changes

### Race Event Management Tab
1. **Available Distances Panel** (right column):
   - Displays all predefined distances for the selected race event
   - Add new distance: Enter a decimal value and click "Add"
   - Remove distance: Select a distance and click "Remove Selected"

### Excel Import Format
The import function now supports distances:
```
Column 1: Date (e.g., 2024-01-15)
Column 2: Race Name (e.g., "Marathon de Paris")
Column 3: Distance in km (e.g., 42.195, 21.1, 10)
Column 4: Location (optional)
Column 5: Website URL (optional)
Column 6: Description (optional)
```

**Important Notes:**
- Distance column supports decimal values
- Multiple rows with the same event name and date will create one event with multiple distances
- Example: If you have 3 rows with "Trail des Bruyères" on the same date but with distances 10, 21.1, and 42, it will create one event with 3 distances

## Code Changes

### New Files
1. `NameParser\Infrastructure\Data\Models\RaceEventDistanceEntity.cs` - Entity for distance storage
2. `NameParser\Infrastructure\Data\Migrations\AddRaceEventDistances.sql` - SQL migration script

### Modified Files
1. **RaceManagementContext.cs**
   - Added `DbSet<RaceEventDistanceEntity>`
   - Configured relationships and unique constraints

2. **RaceEventRepository.cs**
   - Added `GetDistancesByEvent(int raceEventId)` - Get all distances for an event
   - Added `AddDistance(int raceEventId, decimal distanceKm)` - Add a distance to an event
   - Added `RemoveDistance(int distanceId)` - Remove a distance

3. **RaceEventExcelParser.cs**
   - Updated `ParseWithDistances()` to return `List<decimal>` instead of `List<int>`
   - Added `ParseDistance()` method to handle decimal parsing with culture-invariant conversion

4. **RaceEventManagementViewModel.cs**
   - Added `AvailableDistances` collection
   - Added `NewDistanceKm` and `SelectedDistance` properties
   - Added `AddDistanceCommand` and `RemoveDistanceCommand`
   - Updated import to handle and save distances

5. **MainWindow.xaml**
   - Replaced "Race Distances" section with "Available Distances" panel
   - Added text box for new distance entry
   - Added Add and Remove buttons
   - Updated import instructions to clarify decimal support

6. **DatabaseInitializer.cs**
   - Added migration to create RaceEventDistances table with proper constraints

## Usage

### Adding a Distance Manually
1. Select a race event from the list
2. In the "Available Distances" panel, enter a distance (e.g., 21.1)
3. Click "Add"
4. The distance appears in the list

### Removing a Distance
1. Select a race event from the list
2. In the "Available Distances" panel, click on a distance
3. Click "Remove Selected"

### Importing Distances from Excel
1. Prepare an Excel file with the format specified above
2. Click "Browse..." and select your file
3. Click "Import"
4. The system will create events and their associated distances

## Technical Notes

### Decimal Precision
- Distances are stored as `DECIMAL(10,3)` allowing values like 42.195
- The parser handles both comma and period as decimal separators

### Database Constraints
- Unique constraint on (RaceEventId, DistanceKm) prevents duplicate distances
- Foreign key with CASCADE delete ensures distances are removed when event is deleted

### Migration
The database migration will run automatically on application startup through the `DatabaseInitializer.Initialize()` method.

## Future Enhancements
- Link race processing to predefined distances for validation
- Show distance count in race events list
- Filter/search by distance
- Bulk distance operations
