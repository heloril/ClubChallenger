# Race Event-Based Classification Refactoring

## Overview
This refactoring changes the Race Classification tab to work with race events instead of individual races. Users can now select a race event and view all races within that event (grouped and ordered by distance).

## Changes Made

### 1. RaceRepository.cs
**Added Method:**
```csharp
public List<RaceEntity> GetRacesByRaceEvent(int raceEventId)
```
- Returns all races for a specific race event, ordered by distance

### 2. MainViewModel.cs

**New Properties:**
- `SelectedRaceEventForClassification` - The selected race event for viewing classifications
- `RacesInSelectedEvent` - ObservableCollection of races in the selected event
- `_selectedRaceEventForClassification` - Backing field

**Updated Properties:**
- `IsMemberFilter` - Now triggers view update when `SelectedRaceEventForClassification` changes
- `IsChallengerFilter` - Now triggers view update when `SelectedRaceEventForClassification` changes

**New Methods:**
- `LoadRacesForSelectedEvent()` - Loads all races for the selected event, ordered by distance
- `BuildFullRaceEventResultsSummary()` - Builds Facebook summary for full event results
- `BuildRaceEventChallengerResultsSummary()` - Builds Facebook summary for challenger results

**Updated Methods:**
- `CanExecuteViewClassification()` - Now checks if selected event has processed races
- `ExecuteViewClassification()` - Loads classifications for all races in event, grouped by distance
- `CanExecuteReprocessRace()` - Now checks if selected event has races with stored files
- `ExecuteReprocessRace()` - Reprocesses all races in the selected event
- `CanExecuteExportForEmail()` - Now checks if selected event has processed races
- `CanExecuteShareRaceToFacebook()` - Now checks if selected event has processed races
- `ExecuteShareRaceToFacebook()` - Shares all races from the event to Facebook

### 3. MainWindow.xaml

**Race Classification Tab Structure:**
```
┌─ Race Event Selection (ComboBox)
├─ Races in Event Grid (Shows all races, ordered by distance)
│  └─ Action Buttons:
│     • View All Classifications
│     • Reprocess All Races
│     • Export Results
│     • Share to Facebook
└─ Classifications DataGrid (With filters)
   └─ Shows classifications from all races, with distance column
```

**Key Changes:**
- Race event selection dropdown at the top
- Middle section shows races in the selected event (ordered by distance)
- Bottom section shows classifications with a "Distance" column added
- Filters and export functionality retained
- All operations now work on the entire race event

## Features

### 1. Race Event Selection
- Select a race event from a dropdown
- Automatically loads all races in that event (ordered by distance)
- Shows race count and status

### 2. View Classifications
- Displays results from all races in the event
- Classifications are grouped by distance
- Distance column added to clearly identify which race each result belongs to
- Filters (member/challenger) apply to all races

### 3. Reprocess Race Event
- Reprocesses all races in the selected event at once
- Shows progress and handles errors gracefully
- Provides detailed feedback on success/failure for each race

### 4. Export and Share
- Export exports all races from the event
- Facebook sharing posts results for all distances in one event
- Challenger summary aggregates challengers across all distances

## Benefits

1. **Better Organization**: Group races by event instead of showing a flat list
2. **Efficiency**: Process/view/export all distances at once
3. **Consistency**: All races in an event share the same race number and event details
4. **Clarity**: Distance column makes it clear which race each result belongs to
5. **User Experience**: Single selection to work with multiple related races

## Usage Flow

1. **Select Race Event**: Choose an event from the dropdown
2. **View Races**: See all races in the event (ordered by distance) in the middle grid
3. **View Classifications**: Click "View All Classifications" to see results from all races
4. **Filter** (Optional): Apply member/challenger filters - they affect all races
5. **Export/Share**: Use export or Facebook share to publish results for the entire event
6. **Reprocess** (If needed): Reprocess all races in the event with one click

## Migration Notes

### Backward Compatibility
- The old `Races` collection is still maintained for other parts of the app
- The old `SelectedRace` property is kept but no longer used in the Race Classification tab
- Export for individual races still available through "Export Multiple Races" if needed

### Data Requirements
- Races must be associated with a race event (RaceEventId must be set)
- Race events must exist in the database before races can be selected by event

## Known Issues / TODO

1. **Compilation Errors**: There are some Localization property access issues that need to be fixed
   - The code uses `Localization[key]` but the compiler sees `Localization` as a type in some contexts
   - This likely affects the export methods that weren't part of the main refactoring

2. **Distance Column Binding**: The classification DataGrid has a binding to `Race.DistanceKm`
   - Need to verify that `ClassificationEntity` has a navigation property to `RaceEntity`
   - May need to include the related race when loading classifications

3. **SelectionChanged Event**: The `RacesDataGrid_SelectionChanged` event handler in MainWindow.xaml.cs may need updating or removal

## Next Steps

1. Fix compilation errors in export/localization methods
2. Test the distance column binding in classifications grid
3. Update the `RacesDataGrid_SelectionChanged` handler if needed
4. Test all scenarios:
   - Viewing classifications for events with multiple distances
   - Reprocessing events
   - Exporting events
   - Sharing events to Facebook
5. Update any documentation that referenced individual race selection

## Example Scenario

**Before:**
- User sees flat list of all races
- Selects "10km - Marathon de Paris - Race #5"
- Views only 10km results
- Has to select "21km - Marathon de Paris - Race #5" separately to see half-marathon results

**After:**
- User sees list of race events
- Selects "Marathon de Paris - 15/04/2024"
- Sees all distances (10km, 21km, 42km) for that event
- Views all results at once, grouped by distance
- Can reprocess, export, or share the entire event with one action
