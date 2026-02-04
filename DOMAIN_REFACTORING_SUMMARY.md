# Domain Model Refactoring - Challenge Management System

## Overview
This refactoring introduces a new domain model to support challenge management, where a challenge groups multiple race events, and each race event can have multiple distances.

## New Domain Entities

### 1. **Challenge** (New)
Represents a competition series that groups multiple race events (e.g., "2024 Running Challenge").

**Properties:**
- `Name` - Challenge name
- `Description` - Optional description
- `Year` - Year of the challenge
- `StartDate` - Optional start date
- `EndDate` - Optional end date

**Location:** `NameParser\Domain\Entities\Challenge.cs`

### 2. **RaceEvent** (New)
Represents a specific racing occasion with multiple distances.

**Properties:**
- `Name` - Event name (e.g., "Paris Marathon")
- `EventDate` - Date of the event
- `Location` - Optional location
- `WebsiteUrl` - Optional website URL
- `Description` - Optional description/notes

**Location:** `NameParser\Domain\Entities\RaceEvent.cs`

### 3. **RaceDistance** (Renamed from Race)
Represents a specific distance/category within a race event.

**Properties:**
- `RaceNumber` - Identifier number
- `Name` - Distance name
- `DistanceKm` - Distance in kilometers

**Changes:**
- Renamed from `Race` to `RaceDistance` to better reflect its purpose
- File renamed: `Race.cs` → `RaceDistance.cs`

**Location:** `NameParser\Domain\Entities\RaceDistance.cs`

### 4. **ChallengeRaceEvent** (New)
Join entity for many-to-many relationship between Challenges and RaceEvents.

**Properties:**
- `Challenge` - Reference to Challenge
- `RaceEvent` - Reference to RaceEvent
- `DisplayOrder` - Optional ordering

**Location:** `NameParser\Domain\Entities\ChallengeRaceEvent.cs`

### 5. **RaceResult** (Updated)
Updated to reference `RaceDistance` instead of `Race`.

**Changes:**
- Property `Race` renamed to `RaceDistance`
- Constructor parameter updated

**Location:** `NameParser\Domain\Entities\RaceResult.cs`

## Updated Files

### Domain Layer
1. **NameParser\Domain\Aggregates\Classification.cs**
   - All methods updated to use `RaceDistance` instead of `Race`
   - Constructor parameters updated

### Application Layer
1. **NameParser\Application\Services\RaceProcessingService.cs**
   - All method signatures updated to use `RaceDistance`
   - Variable names changed from `race` to `raceDistance` for clarity

2. **NameParser\Application\Services\ReportGenerationService.cs**
   - Updated to instantiate `RaceDistance` instead of `Race`

### Infrastructure Layer
1. **NameParser\Infrastructure\Data\RaceRepository.cs**
   - `SaveRace` method updated to accept `RaceDistance` parameter

### Presentation Layer
1. **NameParser.UI\ViewModels\MainViewModel.cs**
   - Updated `ExecuteProcessRace` method
   - Updated `ExecuteReprocessRace` method
   - Variables renamed to `raceDistance` for consistency

## Database Schema (Future Work)

The following entities will need corresponding database tables:

1. **Challenges** table
   ```sql
   - Id (PK)
   - Name
   - Description
   - Year
   - StartDate
   - EndDate
   ```

2. **RaceEvents** table
   ```sql
   - Id (PK)
   - Name
   - EventDate
   - Location
   - WebsiteUrl
   - Description
   ```

3. **ChallengeRaceEvents** table (junction table)
   ```sql
   - ChallengeId (FK)
   - RaceEventId (FK)
   - DisplayOrder
   ```

4. **RaceDistances** table (refactor of existing Races table)
   ```sql
   - Id (PK)
   - RaceEventId (FK) - to be added
   - RaceNumber
   - Name
   - DistanceKm
   - (existing file storage fields)
   ```

## Domain Relationships

```
Challenge (1) ←→ (*) ChallengeRaceEvent (*) ←→ (1) RaceEvent
                                                      ↓ (1)
                                                      ↓
                                                    (*) RaceDistance
                                                      ↓ (1)
                                                      ↓
                                                    (*) RaceResult
```

## Migration Notes

- **Backward Compatibility**: The existing `RaceEntity` database table remains unchanged for now
- **Entity vs. Database**: `RaceDistance` is the domain entity, while `RaceEntity` remains the database model
- **Future Steps**: 
  1. Create database infrastructure for Challenge and RaceEvent
  2. Add repositories for new entities
  3. Update UI to support challenge and event management
  4. Migrate existing data to new structure

## Benefits

1. **Better Domain Language**: Names now accurately reflect their purpose
2. **Flexibility**: Race events can belong to multiple challenges
3. **Extensibility**: Easy to add event-level information (location, website, etc.)
4. **Clarity**: Clear separation between event (occasion) and distance (category)

## Build Status

✅ All projects build successfully
✅ No breaking changes to existing functionality
✅ Domain model properly refactored
