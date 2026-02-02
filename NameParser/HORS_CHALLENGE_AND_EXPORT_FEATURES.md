# Hors Challenge Races and Export with Filter

## Summary of Changes

This document describes the implementation of two key features:
1. **Hors Challenge races** - Races outside the regular challenge with no constraints
2. **Export with filter** - Race results export respects the current filter settings

---

## 1. Hors Challenge Races (No Constraints)

### Overview
"Hors Challenge" races are races that occur outside the regular yearly challenge. These races:
- Have no year requirement (Year is NULL)
- Have NO database constraints on year, race number, name, or distance
- Can have any combination of values (duplicates are allowed)

### Database Changes

**File: `Infrastructure\Data\Migrations\UpdateRaceConstraints.sql`**
- Removed unique constraint on Name for hors challenge races
- Regular races (with Year) still maintain unique Name per Year
- Hors challenge races can have duplicate names, numbers, distances, etc.

### UI Changes

**File: `..\NameParser.UI\MainWindow.xaml`**
- Checkbox: "Hors Challenge (no year)" disables the Year ComboBox
- Uses `InverseBoolConverter` to disable year selection when checked

### ViewModel Changes

**File: `..\NameParser.UI\ViewModels\MainViewModel.cs`**

#### Validation Logic
```csharp
private bool CanExecuteProcessRace(object parameter)
{
    // For hors challenge races, year is not required
    bool yearValid = IsHorsChallenge || Year > 0;
    
    return !IsProcessing && 
           !string.IsNullOrEmpty(SelectedFilePath) && 
           !string.IsNullOrEmpty(RaceName) &&
           yearValid &&
           RaceNumber > 0 &&
           DistanceKm > 0;
}
```

#### Race Retrieval After Save
```csharp
// Get the saved race - for hors challenge, get all hors challenge races
List<RaceEntity> races;
if (IsHorsChallenge)
{
    races = _raceRepository.GetHorsChallengeRaces();
}
else
{
    races = _raceRepository.GetRacesByYear(Year);
}
```

---

## 2. Export with Applied Filter

### Overview
When exporting race results, the currently applied member filter is now respected:
- If "Members Only" is active, export includes only members (+ winner)
- If "Non-Members Only" is active, export includes only non-members (+ winner)
- If "All Participants" is active, export includes everyone
- Winner (Position 1) is ALWAYS included regardless of filter

### ViewModel Changes

**File: `..\NameParser.UI\ViewModels\MainViewModel.cs`**

#### Export Method Updates
```csharp
private void ExecuteDownloadResult(object parameter)
{
    // Apply the current filter when exporting
    var classifications = _classificationRepository.GetClassificationsByRace(
        SelectedRace.Id, 
        IsMemberFilter);  // Pass current filter
    
    // Show filter status in export file
    if (IsMemberFilter.HasValue)
    {
        writer.WriteLine($"Filter: {(IsMemberFilter.Value ? "Members only" : "Non-members only")} (Winner always included)");
    }
    else
    {
        writer.WriteLine("Filter: All participants");
    }
}
```

#### Export File Format
The exported file now includes:
- Race name
- Year (or "Hors Challenge" if no year)
- Race number
- Distance
- Processed date
- **Filter status** (NEW)
- Classification results matching the filter

Example output:
```
Race: Brussels Marathon
Year: 2024
Race Number: 3
Distance: 42 km
Processed: 2024-01-15 14:30:00
Filter: Members only (Winner always included)
--------------------------------------------------------------------------------
Rank  Name                          Points    Bonus KM  
--------------------------------------------------------------------------------
1     John Doe                      100       15        
2     Jane Smith                    95        12        
```

---

## 3. Complete Feature Integration

### User Workflow

#### Creating a Hors Challenge Race:
1. Select race file
2. Enter race name
3. Enter race number (any value)
4. Enter distance (any value)
5. Check "Hors Challenge (no year)" ✓
6. Year field is disabled
7. Click "Process Race"

#### Exporting with Filter:
1. Select a race from the list
2. Click "View Classification"
3. Apply desired filter:
   - "👥 All Participants"
   - "✓ Members Only" 
   - "○ Non-Members Only"
4. Click "💾 Download Results"
5. Exported file contains filtered results + filter status

### Key Benefits:
✅ Hors challenge races have complete flexibility (no constraints)  
✅ Regular challenge races maintain data integrity (unique name per year)  
✅ Export always reflects what the user sees in the UI  
✅ Winner is always visible/exportable regardless of filter  
✅ Export file clearly indicates which filter was applied  

### Database Schema:
```sql
Races Table:
- Id (PK)
- Name
- Year (nullable)
- RaceNumber
- DistanceKm
- IsHorsChallenge (bit)
- FilePath
- Status
- CreatedDate
- ProcessedDate

Constraints:
- Regular races: Unique(Name, Year) where Year IS NOT NULL
- Hors challenge: NO constraints
```

---

## Files Modified

1. **Infrastructure\Data\Migrations\UpdateRaceConstraints.sql**
   - Removed hors challenge uniqueness constraint
   
2. **..\NameParser.UI\ViewModels\MainViewModel.cs**
   - Added `System.Collections.Generic` using
   - Updated `CanExecuteProcessRace` validation
   - Updated race retrieval logic after save
   - Updated `ExecuteDownloadResult` to apply filter
   - Added filter status to export file
   - Handle nullable year in export filename

3. **Infrastructure\Data\ClassificationRepository.cs**
   - `GetClassificationsByRace` now accepts `bool? isMemberFilter`
   - Winner always included when filter is applied

4. **..\NameParser.UI\MainWindow.xaml**
   - Filter buttons for member/non-member/all
   - "Winner is always shown" indicator
   
---

## Testing Scenarios

### Test 1: Hors Challenge Race
- Create race with IsHorsChallenge = true
- Verify no year is required
- Create another race with same name, number, distance
- Verify both can be saved (no constraint violation)

### Test 2: Export with Member Filter
- Load race classification
- Click "Members Only"
- Click "Download Results"
- Verify exported file contains only members + winner
- Verify filter status line in export

### Test 3: Export with Non-Member Filter
- Load race classification
- Click "Non-Members Only"
- Click "Download Results"
- Verify exported file contains only non-members + winner
- Verify filter status line in export

### Test 4: Export All
- Load race classification
- Click "All Participants"
- Click "Download Results"
- Verify exported file contains everyone
- Verify "All participants" status in export
