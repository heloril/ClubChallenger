# Race Time and Time Per Kilometer Implementation

## Overview
This update adds support for storing **RaceTime** and **TimePerKm** in the database. Previously, only points were being stored, but now the actual race times are persisted.

## Changes Made

### 1. Database Model Changes

#### `ClassificationEntity.cs`
- ✅ **Added**: `TimePerKm` property (TimeSpan?) to store time per kilometer
- ✅ **Existing**: `RaceTime` property (TimeSpan?) was already present but not being used

```csharp
public TimeSpan? RaceTime { get; set; }      // For race time races (>15 min)
public TimeSpan? TimePerKm { get; set; }     // For time per km races (<15 min)
```

### 2. Domain Model Changes

#### `Classification.cs` - MemberClassification class
- ✅ **Added**: `RaceTime` property (TimeSpan?)
- ✅ **Added**: `TimePerKm` property (TimeSpan?)
- ✅ **Updated**: Constructor to accept optional `raceTime` and `timePerKm` parameters
- ✅ **Added**: `UpdateTimes()` method to update times for existing classifications

#### `Classification.cs` - Classification class
- ✅ **Added**: New overload of `AddOrUpdateResult()` that accepts race times
- ✅ **Maintained**: Original `AddOrUpdateResult()` for backward compatibility

```csharp
// New method signature
public void AddOrUpdateResult(Member member, Race race, int points, TimeSpan? raceTime, TimeSpan? timePerKm)
```

### 3. Repository Changes

#### `ClassificationRepository.cs`
- ✅ **Updated**: `SaveClassifications()` now saves both `RaceTime` and `TimePerKm` to database

```csharp
RaceTime = memberClass.RaceTime,
TimePerKm = memberClass.TimePerKm,
```

### 4. Service Changes

#### `RaceProcessingService.cs`
- ✅ **Updated**: `ProcessSingleRace()` now captures and stores race times
- ✅ **Logic**: 
  - For **Time Per Km races** (< 15 min): Stores time in `TimePerKm`, `RaceTime` is null
  - For **Race Time races** (≥ 15 min): Stores time in `RaceTime`, `TimePerKm` is null

```csharp
classification.AddOrUpdateResult(member, race, (int)Math.Round(points), 
    isTimePerKmRace ? null : memberTime,  // RaceTime (null if time per km race)
    timePerKm);                            // TimePerKm (null if race time race)
```

## Database Migration

### Migration Script Created
**Location**: `Infrastructure\Data\Migrations\AddTimePerKmColumn.sql`

### How to Apply Migration

**Option 1: Using SQL Server Management Studio**
1. Open SQL Server Management Studio
2. Connect to your database
3. Open the migration script: `Infrastructure\Data\Migrations\AddTimePerKmColumn.sql`
4. Execute the script

**Option 2: Using Command Line**
```bash
sqlcmd -S your_server_name -d your_database_name -i Infrastructure\Data\Migrations\AddTimePerKmColumn.sql
```

**Option 3: Using Entity Framework Tools (if installed)**
```bash
dotnet ef migrations add AddTimePerKmToClassifications
dotnet ef database update
```

## Testing the Changes

### 1. Verify Database Schema
After running the migration, verify the `Classifications` table has both columns:
```sql
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Classifications' 
AND COLUMN_NAME IN ('RaceTime', 'TimePerKm');
```

### 2. Process a Race and Check Data
1. Run your application
2. Process a race file
3. Query the database to verify times are being stored:

```sql
SELECT 
    MemberFirstName,
    MemberLastName,
    Points,
    RaceTime,
    TimePerKm,
    CASE 
        WHEN TimePerKm IS NOT NULL THEN 'Time Per Km Race'
        WHEN RaceTime IS NOT NULL THEN 'Race Time Race'
        ELSE 'No Time Data'
    END AS RaceType
FROM Classifications
ORDER BY CreatedDate DESC;
```

## Data Flow

### Race Processing Flow:
1. **Excel File** → `ExcelRaceResultRepository` extracts times and race type
2. **Race Type Detection**: 
   - Reference time < 15 min → Time Per Km race
   - Reference time ≥ 15 min → Race Time race
3. **Time Storage**:
   - Time Per Km: Stored in `TimePerKm` column
   - Race Time: Stored in `RaceTime` column
4. **Points Calculation**: `(reference_time / member_time) × 100`
5. **Database**: Both times and points are persisted

## Benefits

✅ **Complete Data**: Now stores both times and points
✅ **Race Type Tracking**: Can identify if race was time-based or distance-based
✅ **Historical Analysis**: Can analyze performance trends over time
✅ **Accurate Reporting**: Can display actual finish times in reports
✅ **Backward Compatible**: Existing code continues to work

## Notes

- **Null Handling**: Either `RaceTime` OR `TimePerKm` will be populated, not both
- **Time Format**: Times are stored as `TimeSpan` (SQL Server `TIME` type)
- **Existing Data**: Old records will have null times (can be backfilled if needed)
- **Debug Output**: Service logs race type, position, and times for verification

## Future Enhancements

Consider adding:
- Position column to track finishing position
- Average pace calculations
- Personal best tracking
- Performance comparisons across races
