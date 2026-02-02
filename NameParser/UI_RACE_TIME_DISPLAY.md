# UI Display for Race Time and Time Per Kilometer

## Overview
The UI has been updated to display **Race Time** and **Time per Kilometer** in the Classifications view.

## Changes Made

### 1. New Converter Created
**File**: `NameParser.UI\Converters\TimeSpanToStringConverter.cs`

This converter formats `TimeSpan` values for display:
- Times ≥ 1 hour: Displayed as `h:mm:ss` (e.g., "1:23:45")
- Times < 1 hour: Displayed as `mm:ss` (e.g., "45:30")
- Null values: Displayed as "-"

### 2. MainWindow.xaml Updates

#### Added Converter Resource (Line ~15)
```xml
<converters:TimeSpanToStringConverter x:Key="TimeSpanToStringConverter"/>
```

#### Updated Classifications DataGrid (Lines ~219-226)
Added two new columns to the Classifications grid:

| Column | Binding | Width | Description |
|--------|---------|-------|-------------|
| **Race Time** | `{Binding RaceTime, Converter={StaticResource TimeSpanToStringConverter}}` | 110 | Displays the member's race finish time (for race time races) |
| **Time/km** | `{Binding TimePerKm, Converter={StaticResource TimeSpanToStringConverter}}` | 100 | Displays the member's time per kilometer (for time/km races) |

## Column Order in UI

The Classifications DataGrid now displays columns in this order:
1. **Rank** - Classification ID
2. **First Name** - Member's first name
3. **Last Name** - Member's last name
4. **Points** - Calculated points
5. **Race Time** - Race finish time (or "-" if not applicable)
6. **Time/km** - Time per kilometer (or "-" if not applicable)
7. **Bonus KM** - Accumulated bonus kilometers

## How It Works

### Race Time Display
- For **Race Time races** (≥ 15 minutes reference time):
  - **Race Time** column shows the finish time
  - **Time/km** column shows "-"
  - Example: Race Time = "1:15:30", Time/km = "-"

### Time Per Kilometer Display
- For **Time per km races** (< 15 minutes reference time):
  - **Race Time** column shows "-"
  - **Time/km** column shows the time per kilometer
  - Example: Race Time = "-", Time/km = "4:35"

## Visual Example

```
┌──────┬────────────┬───────────┬────────┬───────────┬──────────┬──────────┐
│ Rank │ First Name │ Last Name │ Points │ Race Time │ Time/km  │ Bonus KM │
├──────┼────────────┼───────────┼────────┼───────────┼──────────┼──────────┤
│  1   │ John       │ Doe       │  100   │ 45:23     │    -     │   10     │
│  2   │ Jane       │ Smith     │   95   │ 47:45     │    -     │   10     │
│  3   │ Bob        │ Johnson   │   90   │ 50:12     │    -     │   10     │
└──────┴────────────┴───────────┴────────┴───────────┴──────────┴──────────┘

For Time/km races:
┌──────┬────────────┬───────────┬────────┬───────────┬──────────┬──────────┐
│ Rank │ First Name │ Last Name │ Points │ Race Time │ Time/km  │ Bonus KM │
├──────┼────────────┼───────────┼────────┼───────────┼──────────┼──────────┤
│  1   │ Alice      │ Brown     │  100   │     -     │  4:15    │   10     │
│  2   │ Charlie    │ Davis     │   95   │     -     │  4:28    │   10     │
│  3   │ Eve        │ Wilson    │   90   │     -     │  4:42    │   10     │
└──────┴────────────┴───────────┴────────┴───────────┴──────────┴──────────┘
```

## Usage

1. **Process a race** using the "Upload & Process Race" tab
2. **Navigate to "View Results"** tab
3. **Select a race** from the Races list
4. **Click "View Classification"** to load results
5. The Classifications grid will show:
   - All member results
   - Points calculated
   - **Race times** (if applicable)
   - **Time per kilometer** (if applicable)

## Technical Notes

- Times are stored as `TimeSpan?` (nullable) in the database
- The converter handles null values gracefully (displays as "-")
- The UI automatically formats times based on duration
- No code changes needed in ViewModels - binding works directly with entity properties

## Benefits

✅ **Complete Information**: Users can now see actual race times, not just points
✅ **Race Type Clarity**: Immediately clear whether it's a race time or time/km race
✅ **Performance Tracking**: Can track improvement over time
✅ **Easy Comparison**: Compare performances across different race types
✅ **Professional Display**: Clean, formatted time display

## Future Enhancements

Consider adding:
- Sorting by Race Time or Time/km
- Filtering by race type
- Export times to Excel/CSV
- Charts showing time trends
- Personal best indicators
- Color coding for race types
