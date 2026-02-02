# Testing Guide for Two Tab Classification

## Testing Checklist

### ✅ Build Status
- [x] Build successful
- [x] No compilation errors
- [x] XAML parses correctly

### 🧪 Test Scenarios

## Scenario 1: Upload and Process a Race

### Steps:
1. Launch the application
2. Go to **"Upload & Process Race"** tab
3. Click **"📁 Browse File"** button
4. Select a valid Excel file with race results
5. Enter race information:
   - Race Name: "Test Marathon"
   - Year: 2024
   - Race Number: 1
   - Distance: 42 km
6. Click **"⚡ Process Race"** button

### Expected Results:
- ✅ Processing indicator shows "Processing..."
- ✅ Status message updates to "Race processed successfully!"
- ✅ Race appears in Races list
- ✅ Race status shows "Processed"
- ✅ Form clears after processing

## Scenario 2: View Race Classification

### Prerequisites:
- At least one race processed

### Steps:
1. Go to **"🏁 Race Classification"** tab
2. Click **"🔄 Refresh Races"** button
3. Select a race from the list
4. Click **"👁️ View Classification"** button

### Expected Results:
- ✅ Race list displays all processed races with:
  - ID, Year, Race #, Name, Distance, Status, Processed Date
- ✅ Race results grid populates with:
  - Rank, Position, First Name, Last Name, Team
  - Points, Race Time, Time/km, Speed
  - Member checkbox, Bonus KM
- ✅ Results are sorted by points (descending)
- ✅ Status message shows: "Loaded X classifications for race 'Name'"

### Test Data Verification:
```
Check that:
- Points are calculated correctly: (TREF / MemberTime) × 1000
- Bonus KM equals race distance (e.g., 42 for marathon)
- Race Time is formatted correctly (HH:MM:SS or MM:SS)
- Time/km is calculated: RaceTime / Distance
- Speed is calculated: Distance / (Time in hours)
```

## Scenario 3: Download Race Results

### Prerequisites:
- At least one race processed
- Race selected in Race Classification tab

### Steps:
1. Go to **"🏁 Race Classification"** tab
2. Select a processed race
3. Click **"💾 Download Results"** button
4. Choose save location and filename
5. Click Save

### Expected Results:
- ✅ Save dialog opens with default filename: "Race_YYYY_N_Name_Results.txt"
- ✅ File is created successfully
- ✅ Status message shows: "Results downloaded to: [path]"
- ✅ Success message box appears
- ✅ File contains:
  - Race header information
  - Table with: Rank, Name, Points, Bonus KM
  - All results in order

## Scenario 4: Delete Race

### Prerequisites:
- At least one race processed

### Steps:
1. Go to **"🏁 Race Classification"** tab
2. Select a race
3. Click **"🗑️ Delete Race"** button
4. Click "Yes" in confirmation dialog

### Expected Results:
- ✅ Confirmation dialog appears with warning message
- ✅ Race is removed from the list
- ✅ Associated classifications are deleted
- ✅ Status message shows: "Race 'Name' deleted successfully"
- ✅ Race results grid clears

## Scenario 5: View General Classification

### Prerequisites:
- At least 2-3 races processed in the same year
- Multiple members participated in multiple races

### Steps:
1. Go to **"📊 General Classification"** tab
2. Select year from dropdown (e.g., 2024)
3. Click **"📊 Load Classification"** button

### Expected Results:
- ✅ General classification results grid populates with:
  - Rank, First Name, Last Name, Team
  - Total Points, Total Bonus KM, Races count
  - Average Points, Best Position
  - Best Time, Best T/km
- ✅ Results are sorted by Total Points (descending), then Total Bonus KM (descending)
- ✅ Status message shows: "Loaded general classification for year YYYY (X members)"
- ✅ Only members (IsMember = true) are shown

### Test Data Verification:
```
For each member, verify:
1. Total Points = Sum of points from all races
2. Total Bonus KM = Sum of distances from all races
3. Races = Count of races participated
4. Avg Points = Total Points / Races
5. Best Position = Minimum position across all races
6. Best Time = Fastest race time
7. Best T/km = Best pace achieved
```

## Scenario 6: General Classification with Year Changes

### Prerequisites:
- Races processed in multiple years (e.g., 2023 and 2024)

### Steps:
1. Go to **"📊 General Classification"** tab
2. Select year 2024
3. Click **"📊 Load Classification"**
4. Note the results
5. Change year to 2023
6. Results should auto-update

### Expected Results:
- ✅ Classification updates automatically when year changes
- ✅ Only races from selected year are included
- ✅ Different members may appear for different years
- ✅ Points and bonus KM reflect only selected year

## Scenario 7: Multiple Race Points Aggregation

### Test Setup:
```
Year: 2024
Member: John Doe

Race 1 (Marathon, 42 km):
  - Time: 3:00:00
  - TREF: 3:30:00
  - Expected Points: (12600 / 10800) × 1000 = 1167
  - Bonus KM: 42

Race 2 (10K, 10 km):
  - Time: 40:00
  - TREF: 45:00
  - Expected Points: (2700 / 2400) × 1000 = 1125
  - Bonus KM: 10

Race 3 (Half Marathon, 21 km):
  - Time: 1:30:00
  - TREF: 1:45:00
  - Expected Points: (6300 / 5400) × 1000 = 1167
  - Bonus KM: 21
```

### Expected General Classification for John Doe:
```
Total Points: 1167 + 1125 + 1167 = 3459
Total Bonus KM: 42 + 10 + 21 = 73
Race Count: 3
Average Points: 3459 / 3 = 1153
```

### Verification Steps:
1. Process all three races
2. View each race classification individually (Race Classification tab)
   - Verify points for each race
3. View general classification (General Classification tab)
   - Verify aggregated totals

### Expected Results:
- ✅ Race Classification shows correct points per race
- ✅ General Classification shows correct totals
- ✅ No double-counting or missing races

## Scenario 8: Members vs Non-Members

### Test Setup:
- Process a race with both members and non-members
- Member: John Doe (IsMember = true)
- Non-member: Guest Runner (IsMember = false)

### Steps:
1. Process race with mixed participants
2. View **Race Classification** tab
3. View **General Classification** tab

### Expected Results:
- ✅ **Race Classification**: Shows both members and non-members
  - Member checkbox is checked for members
  - Member checkbox is unchecked for non-members
- ✅ **General Classification**: Shows only members
  - Non-members are filtered out
  - Only IsMember = true participants appear

## Scenario 9: Empty States

### Test 9a: No Races Processed
1. Fresh database or all races deleted
2. View **Race Classification** tab

**Expected:**
- ✅ Races list is empty
- ✅ Results grid is empty
- ✅ No errors or crashes

### Test 9b: No General Classification Data
1. Select a year with no processed races
2. View **General Classification** tab

**Expected:**
- ✅ Results grid is empty
- ✅ Status message indicates no data for selected year
- ✅ No errors or crashes

### Test 9c: Race with No Results
1. Select a race with status "Pending" or "Failed"
2. Try to view classification

**Expected:**
- ✅ View button is disabled (CanExecuteViewClassification = false)
- ✅ Or shows empty results with appropriate message

## Scenario 10: Tab Navigation

### Steps:
1. Start in **"Upload & Process Race"** tab
2. Click **"🏁 Race Classification"** tab
3. Click **"📊 General Classification"** tab
4. Click back to **"Upload & Process Race"** tab

### Expected Results:
- ✅ All tabs load without errors
- ✅ Tab content displays correctly
- ✅ Data persists between tab switches
- ✅ No visual glitches or layout issues
- ✅ Icons display correctly (🏁, 📊)

## Scenario 11: Error Handling

### Test 11a: Invalid File
1. Try to upload a non-Excel file or corrupted file

**Expected:**
- ✅ Error message displayed
- ✅ Status shows error details
- ✅ Application doesn't crash

### Test 11b: Missing Members.json
1. Remove or rename Members.json file
2. Try to process a race

**Expected:**
- ✅ Error message about missing file
- ✅ Processing fails gracefully
- ✅ Application doesn't crash

### Test 11c: Database Connection Error
1. Rename or delete database file
2. Try to load races or classifications

**Expected:**
- ✅ Error message displayed
- ✅ Application doesn't crash
- ✅ User can retry after fixing issue

## Scenario 12: Data Consistency

### Test Process:
1. Process Race 1
2. View in Race Classification → Note points
3. Process Race 2
4. View in Race Classification → Note points
5. View General Classification → Verify totals

### Expected Results:
- ✅ Race Classification shows individual race data
- ✅ General Classification shows aggregated data
- ✅ Numbers match exactly:
  - Sum of race points = General total points
  - Sum of race bonus KM = General total bonus KM
- ✅ No data discrepancies

## Performance Tests

### Test 13: Large Dataset
**Setup:** Process 10 races with 100 members each

**Expected:**
- ✅ All races process successfully
- ✅ Race Classification loads within 2 seconds
- ✅ General Classification loads within 3 seconds
- ✅ UI remains responsive
- ✅ No memory leaks

### Test 14: Rapid Operations
**Steps:**
1. Rapidly switch between tabs
2. Quickly select different races
3. Load different years in quick succession

**Expected:**
- ✅ No crashes or hangs
- ✅ Data loads correctly
- ✅ No race conditions or data corruption

## Visual Verification

### Layout Checks:
- ✅ All columns are visible and properly sized
- ✅ Text is readable and not cut off
- ✅ Buttons are properly aligned
- ✅ Grid scrollbars appear when needed
- ✅ Status bar always visible at bottom
- ✅ No overlapping elements

### Icon Checks:
- ✅ 📁 appears on Browse button
- ✅ ⚡ appears on Process button
- ✅ 🔄 appears on Refresh button
- ✅ 👁️ appears on View buttons
- ✅ 💾 appears on Download button
- ✅ 🗑️ appears on Delete button
- ✅ 🏁 appears on Race Classification tab
- ✅ 📊 appears on General Classification tab

### Formatting Checks:
- ✅ Times display as HH:MM:SS or MM:SS
- ✅ Dates display as YYYY-MM-DD HH:MM
- ✅ Speeds display with 2 decimal places (XX.XX)
- ✅ Points display as integers
- ✅ Bonus KM display as integers

## Regression Tests

### Test Previous Bug Fix:
**Issue:** BooleanToVisibilityConverter resource not found

**Test:**
1. Start application
2. Process a race (triggers IsProcessing = true)

**Expected:**
- ✅ Processing indicator shows correctly
- ✅ No XamlParseException
- ✅ No StaticResourceExtension errors

## Integration Tests

### Full Workflow Test:
1. Upload and process 3 races in 2024
2. View each race individually in Race Classification
3. Download results for each race
4. View General Classification for 2024
5. Verify totals match sum of individual races
6. Delete one race
7. Verify General Classification updates correctly
8. Process more races in 2025
9. Switch years in General Classification
10. Verify data isolation between years

**Expected:**
- ✅ All operations complete successfully
- ✅ Data remains consistent throughout
- ✅ No errors or crashes

## Test Report Template

```
Test Date: ___________
Tester: ___________
Version: ___________

┌─────────────────────────────────────────────────────────┐
│ Scenario                        │ Status │ Notes        │
├─────────────────────────────────┼────────┼──────────────┤
│ 1. Upload and Process Race      │ ☐ Pass │              │
│                                  │ ☐ Fail │              │
├─────────────────────────────────┼────────┼──────────────┤
│ 2. View Race Classification     │ ☐ Pass │              │
│                                  │ ☐ Fail │              │
├─────────────────────────────────┼────────┼──────────────┤
│ 3. Download Race Results        │ ☐ Pass │              │
│                                  │ ☐ Fail │              │
├─────────────────────────────────┼────────┼──────────────┤
│ 4. Delete Race                  │ ☐ Pass │              │
│                                  │ ☐ Fail │              │
├─────────────────────────────────┼────────┼──────────────┤
│ 5. View General Classification  │ ☐ Pass │              │
│                                  │ ☐ Fail │              │
├─────────────────────────────────┼────────┼──────────────┤
│ 6. Year Changes                 │ ☐ Pass │              │
│                                  │ ☐ Fail │              │
├─────────────────────────────────┼────────┼──────────────┤
│ 7. Points Aggregation           │ ☐ Pass │              │
│                                  │ ☐ Fail │              │
├─────────────────────────────────┼────────┼──────────────┤
│ 8. Members vs Non-Members       │ ☐ Pass │              │
│                                  │ ☐ Fail │              │
├─────────────────────────────────┼────────┼──────────────┤
│ 9. Empty States                 │ ☐ Pass │              │
│                                  │ ☐ Fail │              │
├─────────────────────────────────┼────────┼──────────────┤
│ 10. Tab Navigation              │ ☐ Pass │              │
│                                  │ ☐ Fail │              │
├─────────────────────────────────┼────────┼──────────────┤
│ 11. Error Handling              │ ☐ Pass │              │
│                                  │ ☐ Fail │              │
├─────────────────────────────────┼────────┼──────────────┤
│ 12. Data Consistency            │ ☐ Pass │              │
│                                  │ ☐ Fail │              │
└─────────────────────────────────┴────────┴──────────────┘

Overall Result: ☐ Pass ☐ Fail

Issues Found:
1. ___________________________________________
2. ___________________________________________
3. ___________________________________________

Recommendations:
1. ___________________________________________
2. ___________________________________________
3. ___________________________________________
```

## Quick Smoke Test (5 minutes)

For rapid verification after changes:

1. ✅ Launch app → No errors
2. ✅ Upload & Process tab → Browse file works
3. ✅ Process a race → Success message
4. ✅ Race Classification tab → Race appears
5. ✅ View classification → Results show
6. ✅ General Classification tab → Select year
7. ✅ Load classification → Results show
8. ✅ All tabs navigate smoothly
9. ✅ No visual glitches
10. ✅ Status messages update correctly

**Time: ~5 minutes**
**Purpose: Verify basic functionality after code changes**
