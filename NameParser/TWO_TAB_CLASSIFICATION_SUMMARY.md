# Two Tab Classification Implementation Summary

## Overview
The UI has been restructured to separate Race Classification and General Classification into two distinct tabs, providing a clearer user experience and better organization of data.

## Changes Made

### 1. UI Structure (MainWindow.xaml)

The application now has **3 main tabs**:

#### Tab 1: Upload & Process Race
- Upload Excel files with race results
- Enter race information (name, year, race number, distance)
- Process race data
- Instructions for users

#### Tab 2: 🏁 Race Classification
- **Purpose**: View results for individual races
- **Features**:
  - Select a race from the list
  - View race-specific results with points
  - Actions: Refresh, View Classification, Download Results, Delete Race
  - Display columns:
    - Rank, Position, Name, Team
    - Points (calculated per race)
    - Race Time, Time/km, Speed
    - Member status
    - Bonus KM (race distance)

#### Tab 3: 📊 General Classification
- **Purpose**: View aggregated results across all races in a year
- **Features**:
  - Select year to view
  - Load general classification button
  - Explanatory text: "General classification sums points from all races in the selected year plus bonus KM per race"
  - Display columns:
    - Rank, Name, Team
    - Total Points (sum of all race points)
    - Total Bonus KM (sum of all race distances)
    - Race Count
    - Average Points
    - Best Position, Best Time, Best T/km

## Points Calculation Logic

### Race Classification (Per Race)
Points are computed individually for each race based on:
- Member's performance in that specific race
- Reference time (TREF) comparison
- Position in the race
- Each race awards bonus KM equal to the race distance

**Implementation**: `PointsCalculationService.cs` calculates points per race

### General Classification (Yearly)
Total points are calculated as:
```
Total Points = Sum(Points from all races in year) + Sum(Bonus KM from all races)
```

**Implementation**: `ClassificationRepository.GetGeneralClassification()` method
```csharp
TotalPoints = g.Sum(c => c.Points),
TotalBonusKm = g.Sum(c => c.BonusKm),
```

Where:
- Each race contributes its points
- Each race contributes bonus KM equal to its distance
- Only members (IsMember = true) are included in general classification
- Results are ordered by TotalPoints DESC, then TotalBonusKm DESC

## Data Models

### ClassificationEntity (Race Results)
Stores individual race results:
- RaceId (FK to Race)
- Member details (FirstName, LastName, Email)
- Points (for this specific race)
- BonusKm (race distance)
- RaceTime, TimePerKm, Speed
- Position, Team, IsMember

### GeneralClassificationDto (Yearly Aggregation)
Aggregates data across all races in a year:
- Rank
- Member details
- TotalPoints (sum of all race points)
- TotalBonusKm (sum of all race distances)
- RaceCount
- AveragePoints
- Best metrics (BestPosition, BestRaceTime, BestTimePerKm)

## User Workflow

1. **Upload & Process**: User uploads race results and processes them
2. **Race Classification**: User views individual race results
   - Select a race from the list
   - Click "View Classification" to see results
   - Download or delete races as needed
3. **General Classification**: User views yearly rankings
   - Select a year
   - Click "Load Classification" to see aggregated results
   - View total points, race participation, and best performances

## Benefits of Two Tab Approach

1. **Clear Separation**: Race-specific vs. aggregated results
2. **Reduced Complexity**: No toggle buttons or conditional visibility
3. **Better UX**: Dedicated space for each type of classification
4. **Easier Navigation**: Tab labels with icons (🏁 and 📊)
5. **Contextual Information**: Each tab has relevant information and actions

## Technical Details

### ViewModels
The `MainViewModel` already supports both views:
- `Classifications` collection for race results
- `GeneralClassifications` collection for yearly aggregation
- Commands remain the same:
  - `ViewClassificationCommand` - loads race results
  - `ViewGeneralClassificationCommand` - loads general classification

### Converters Used
- `TimeSpanToStringConverter` - formats race times
- `BoolToVisibilityConverter` - shows/hides processing indicator

### No Code Changes Required
The existing backend logic remains unchanged:
- Points calculation per race works correctly
- General classification aggregation is already implemented
- Only UI structure was reorganized for better clarity
