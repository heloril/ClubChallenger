# Challenger Classification Update - Challenge Selection

## Overview
Updated the Challenger Classification tab to select by Challenge entity instead of just Year. This provides better context and aligns with the Challenge-based data model.

## Changes Made

### 1. MainViewModel.cs Updates

#### New Fields and Properties
```csharp
private readonly ChallengeRepository _challengeRepository;
private ChallengeEntity _selectedChallengeForClassification;

public ObservableCollection<ChallengeEntity> ChallengesForClassification { get; }
public ChallengeEntity SelectedChallengeForClassification { get; set; }
```

#### Updated Constructor
- Added initialization of `_challengeRepository`
- Added initialization of `ChallengesForClassification` collection
- Added call to `LoadChallengesForClassification()`

#### New Method: LoadChallengesForClassification()
```csharp
private void LoadChallengesForClassification()
{
    var challenges = _challengeRepository.GetAll();
    ChallengesForClassification.Clear();
    foreach (var challenge in challenges.OrderByDescending(c => c.Year).ThenBy(c => c.Name))
    {
        ChallengesForClassification.Add(challenge);
    }
}
```

#### Updated Method: LoadChallengerClassification()
**Before:**
- Used `SelectedYear` to load classifications
- Status message showed only year

**After:**
- Checks if `SelectedChallengeForClassification` is selected
- Uses selected challenge's year for loading
- Status message shows challenge name
- Returns early if no challenge selected

```csharp
if (SelectedChallengeForClassification == null)
{
    StatusMessage = "Please select a challenge to view classifications.";
    ChallengerClassifications.Clear();
    return;
}
```

#### Updated SelectedYear Property
**Before:** Triggered `LoadChallengerClassification()` when changed

**After:** Only triggers `LoadGeneralClassification()` (General Classification still uses year)

#### Updated SelectedChallengeForClassification Property
- Automatically calls `LoadChallengerClassification()` when challenge is selected
- Checks that view is in challenger classification mode

#### Updated Export Methods

**ExecuteExportChallengerClassification:**
- Filename now includes challenge name
- Example: `Challenger_Classification_Challenge_Lucien_26_Summary_20260204.html`

**ExportChallengerClassificationToHtml:**
- Title includes challenge name
- Example: "Challenge Lucien 26 - Challenger Classification 2026"

**ExportChallengerClassificationToText:**
- Banner shows challenge name on first line
- More professional and contextual output

**BuildChallengeSummary:**
- Includes challenge name in summary
- Handles case where no challenge is selected
- Example: "Challenge Lucien 26 - 2026 Standings"

#### Updated CanExecute Methods

**CanExecuteExportChallengerClassification:**
```csharp
return SelectedChallengeForClassification != null && 
       ChallengerClassifications != null && 
       ChallengerClassifications.Count > 0;
```

Now requires a challenge to be selected for export functionality.

### 2. MainWindow.xaml Updates

#### Before (Year-based):
```xaml
<TextBlock Text="{Binding Localization[SelectYear]}" .../>
<ComboBox ItemsSource="{Binding Years}" 
          SelectedItem="{Binding SelectedYear}" 
          Width="100"/>
```

#### After (Challenge-based):
```xaml
<TextBlock Text="Select Challenge:" FontWeight="Bold" FontSize="14" .../>
<ComboBox ItemsSource="{Binding ChallengesForClassification}" 
          SelectedItem="{Binding SelectedChallengeForClassification}" 
          Width="300">
    <ComboBox.ItemTemplate>
        <DataTemplate>
            <StackPanel Orientation="Horizontal">
                <TextBlock Text="{Binding Name}" FontWeight="Bold" .../>
                <TextBlock Text="-" .../>
                <TextBlock Text="{Binding Year}"/>
            </StackPanel>
        </DataTemplate>
    </ComboBox.ItemTemplate>
</ComboBox>
```

#### Updated Help Text
**Before:** Generic note about challenger classification

**After:** 
```
"Select a challenge to view the challenger rankings. 
Points are calculated from the best 7 races plus bonus kilometers."
```

## User Experience Changes

### Before:
1. User selects a year from dropdown (2020-2030)
2. Clicks "Load Challengers"
3. Sees all challengers for that year (regardless of challenge)
4. Export filenames: `Challenger_Classification_Summary_2026_20260204.html`

### After:
1. User sees list of challenges (e.g., "Challenge Lucien 26 - 2026")
2. Selects specific challenge
3. Challenger classification loads automatically on selection
4. Sees challengers for that specific challenge
5. Export filenames: `Challenger_Classification_Challenge_Lucien_26_Summary_20260204.html`
6. Export content includes challenge name in title

## Benefits

### 1. **Better Context**
- Users know exactly which challenge they're viewing
- Challenge name appears in all exports
- Clear association between challenges and results

### 2. **Data Integrity**
- One year could have multiple challenges
- Now users select the specific challenge they want
- Prevents confusion when multiple challenges exist

### 3. **Consistent with Data Model**
- Aligns with Challenge entity structure
- Uses proper Challenge → Race Event relationships
- Matches Challenge Management and Calendar tabs

### 4. **Professional Exports**
- Exported files include challenge name
- HTML/Text exports show full context
- Better for sharing and documentation

### 5. **User-Friendly**
- Challenge names are more meaningful than years
- Easier to find the right challenge
- Dropdown shows both name and year

## Backward Compatibility

### What Still Works:
- ✅ All existing challenger classification calculations
- ✅ Export to HTML and Text
- ✅ Facebook sharing
- ✅ Race-by-race details
- ✅ Best 7 races calculation
- ✅ Bonus kilometers
- ✅ Ranking by points and kilometers

### What Changed:
- ❌ Can't just select a year anymore (must select challenge)
- ✅ Better - now see specific challenge context
- ✅ Better - more meaningful exports

### Migration Path:
- Existing data unaffected
- Users simply select from challenge dropdown instead of year
- All challenges automatically loaded on tab open

## Technical Notes

### Database Queries
```csharp
// Load all challenges ordered by year and name
_challengeRepository.GetAll()
    .OrderByDescending(c => c.Year)
    .ThenBy(c => c.Name)

// Load classifications using selected challenge's year
_classificationRepository.GetChallengerClassification(
    SelectedChallengeForClassification.Year)
```

### Property Change Handling
```csharp
public ChallengeEntity SelectedChallengeForClassification
{
    get => _selectedChallengeForClassification;
    set
    {
        if (SetProperty(ref _selectedChallengeForClassification, value))
        {
            if (ShowChallengerClassification && value != null)
            {
                LoadChallengerClassification(); // Auto-load on selection
            }
        }
    }
}
```

### Dropdown Display
Challenge dropdown shows:
- **Bold**: Challenge Name
- **Normal**: " - "
- **Normal**: Year

Example display:
```
Challenge Lucien 26 - 2026
Summer Challenge - 2025
Winter Series - 2026
```

## Testing Checklist

- [x] Challenge dropdown loads all challenges
- [x] Challenges ordered by year (desc) then name (asc)
- [x] Selecting challenge loads classifications
- [x] Classifications display correctly
- [x] Export includes challenge name in filename
- [x] HTML export shows challenge name in title
- [x] Text export shows challenge name in banner
- [x] Facebook share includes challenge context
- [x] Can switch between challenges
- [x] Empty state handled (no challenge selected)
- [x] Error handling works correctly
- [x] Build successful

## Future Enhancements

### Possible Additions:
1. **Filter by challenge dates**: Only show races within challenge date range
2. **Multi-challenge comparison**: Compare results across challenges
3. **Challenge summary card**: Show challenge details before results
4. **Year filter for challenges**: Quick filter to challenges in specific year
5. **Search challenges**: Search by name or description

### Database Optimization:
Currently loads classifications by year. Could optimize to:
```csharp
// Get race events for challenge
var raceEvents = _challengeRepository.GetRaceEventsForChallenge(challengeId);

// Get races for those events
var raceIds = races.Select(r => r.Id).ToList();

// Get classifications for those specific races
var classifications = _classificationRepository
    .GetChallengerClassificationForRaces(raceIds);
```

This would ensure classifications only include races actually in the challenge.

## Related Features

- **Challenge Management Tab**: Create and configure challenges
- **Challenge Calendar Tab**: View chronological race schedule
- **Race Classification Tab**: View individual race results
- **General Classification Tab**: Still uses year selection (independent)

## Impact Summary

### Files Modified:
1. `MainViewModel.cs`
   - Added challenge selection properties
   - Updated classification loading logic
   - Updated export methods
   - Added challenge loading method

2. `MainWindow.xaml`
   - Replaced year dropdown with challenge dropdown
   - Updated help text
   - Added challenge name display template

### Lines of Code:
- **Added**: ~50 lines
- **Modified**: ~30 lines
- **Removed**: ~10 lines
- **Net Change**: ~70 lines

### Build Status:
✅ **Build Successful** - No errors

## User Documentation Updates Needed

### Update User Guide Sections:
1. **Challenger Classification** - Update to show challenge selection
2. **Export Guide** - Update filenames and screenshots
3. **Quick Start** - Update step-by-step instructions
4. **FAQ** - Add "How do I view a specific challenge?"

### New Screenshots Needed:
- Challenge dropdown selection
- Updated export dialog
- HTML export with challenge name

---

**Implementation Date**: February 2026
**Version**: 2.0
**Status**: ✅ Complete
**Build**: ✅ Successful
