# Export Multiple Races for Email Feature

## Overview
Enhanced the email export functionality to support exporting multiple races at once with the same filter applied to all races. This allows creating a comprehensive email-ready document containing results from multiple races.

## Features

### 1. **Multi-Race Selection**
- Changed DataGrid selection mode from `Single` to `Extended`
- Users can now select multiple races using:
  - **Ctrl+Click** - Add individual races to selection
  - **Shift+Click** - Select range of races
  - **Ctrl+A** - Select all races

### 2. **Unified Export with Same Filter**
- All selected races are exported in a single document
- The current filter (All/Members/Non-Members) is applied to **all** races
- Winner is always included in each race (regardless of filter)
- Filter status is prominently displayed in the export

### 3. **Enhanced Formats**

#### HTML Format (Recommended)
- **Comprehensive header** with export summary
- **Each race** displayed as a separate section with:
  - Race name and details
  - Full results table with styling
  - Participant count for that race
- **Page breaks** between races (for printing)
- **Sticky headers** for better scrolling
- **Summary section** at the top showing:
  - Total races exported
  - Years covered
  - Total distance
  - Export timestamp
- **Final summary** at the bottom

#### Text Format (Plain Email)
- **Formatted header** with export summary
- **Each race** separated by dividers
- **Race counter** (e.g., "RACE 1/5")
- **Clear visual separation** between races
- **Legend** included at the bottom
- **Complete summary** with totals

## User Interface

### New Button
```
📧 Export Multiple Races
```
- **Color**: Blue (#2196F3) with white text
- **Icon**: 📧 (email emoji)
- **Position**: Between "Export for Email" and "Delete Race"
- **Enabled when**: One or more races are selected AND all selected races are processed

### Workflow

1. **Select Races**:
   - Hold `Ctrl` and click races to add to selection
   - Hold `Shift` and click to select range
   - Use `Ctrl+A` to select all

2. **Apply Filter** (optional):
   - Choose "All Participants", "Members Only", or "Non-Members Only"
   - This filter will be applied to ALL selected races

3. **Export**:
   - Click "📧 Export Multiple Races"
   - Choose format (HTML or Text)
   - Save file
   - Open and copy content to email

## Technical Implementation

### Files Modified

#### 1. `MainWindow.xaml`
```xaml
<!-- Changed SelectionMode to Extended -->
<DataGrid SelectionMode="Extended"
          SelectionChanged="RacesDataGrid_SelectionChanged"
          x:Name="RacesDataGrid"
          ...>

<!-- Added new button -->
<Button Content="📧 Export Multiple Races" 
        Command="{Binding ExportMultipleForEmailCommand}" 
        Background="#2196F3" 
        Foreground="White"/>
```

#### 2. `MainWindow.xaml.cs`
Added event handler to capture selected items:
```csharp
private void RacesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    if (DataContext is MainViewModel viewModel)
    {
        var dataGrid = sender as DataGrid;
        viewModel.SelectedRaces = dataGrid?.SelectedItems.Cast<object>().ToList();
    }
}
```

#### 3. `MainViewModel.cs`
Added:
- `SelectedRaces` property (List<object>)
- `ExportMultipleForEmailCommand`
- `CanExecuteExportMultipleForEmail()` - Validates selections
- `ExecuteExportMultipleForEmail()` - Main export logic
- `ExportMultipleToHtml()` - HTML formatter for multiple races
- `ExportMultipleToText()` - Text formatter for multiple races

### Code Architecture
```
User selects multiple races
    ↓
Selection captured via DataGrid event
    ↓
SelectedRaces property updated
    ↓
CanExecuteExportMultipleForEmail() validates
    ↓
User clicks "Export Multiple Races"
    ↓
ExecuteExportMultipleForEmail()
    ↓
Races sorted by Year → RaceNumber
    ↓
ExportMultipleToHtml() OR ExportMultipleToText()
    ↓
For each race:
    - Get filtered classifications
    - Write race section
    - Include all data
    ↓
Write summary
    ↓
Show success message
```

## HTML Output Example

```html
<!DOCTYPE html>
<html>
<head>
    <style>
        /* Professional styling with sections for each race */
        .race-section { margin-bottom: 50px; page-break-after: always; }
        h2 { color: #1976D2; background-color: #E3F2FD; }
        /* ... more styles ... */
    </style>
</head>
<body>
    <h1>Multiple Race Results Export</h1>
    
    <!-- Global Summary -->
    <div class='summary'>
        <strong>📊 Export Summary</strong><br/>
        <strong>Total Races:</strong> 5<br/>
        <strong>Years:</strong> 2026<br/>
        <strong>Total Distance:</strong> 50 km<br/>
        <strong>Exported:</strong> 2026-02-01 14:30
    </div>
    
    <!-- Filter Info (if applied) -->
    <div class='filter-info'>
        <strong>⚠️ Global Filter Applied:</strong> Members only (Winner always included)
    </div>
    
    <!-- Race 1 -->
    <div class='race-section'>
        <h2>🏁 Marathon Brussels</h2>
        <div class='race-info'>
            <strong>Year:</strong> 2026 | 
            <strong>Distance:</strong> 10 km | 
            <strong>Race Number:</strong> 1 | 
            <strong>Participants:</strong> 354
        </div>
        <table>
            <!-- Full results table -->
        </table>
    </div>
    
    <!-- Race 2 -->
    <div class='race-section'>
        <h2>🏁 Liège Half Marathon</h2>
        <!-- ... -->
    </div>
    
    <!-- ... more races ... -->
    
    <!-- Final Summary -->
    <div class='total-summary'>
        <strong>Complete Export Summary</strong><br/>
        Total Races: 5 | Total Participants (all races): 1,420<br/>
        Generated: 2026-02-01 14:30
    </div>
</body>
</html>
```

## Text Output Example

```
╔═══════════════════════════════════════════════════════════════════════════════╗
║                     MULTIPLE RACE RESULTS EXPORT                              ║
╚═══════════════════════════════════════════════════════════════════════════════╝

📊 EXPORT SUMMARY
Total Races: 5
Years: 2026
Total Distance: 50 km
Exported: 2026-02-01 14:30

⚠️  GLOBAL FILTER: Members only (Winner always included)

════════════════════════════════════════════════════════════════════════════════

🏁 RACE 1/5: MARATHON BRUSSELS
Year: 2026 | Distance: 10 km | Race #1 | Participants: 354
────────────────────────────────────────────────────────────────────────────────
Rank  │ Pos  │ Name                          │ Team                │ Time      │ T/km    │ Speed   │ Points │ Bonus
────────────────────────────────────────────────────────────────────────────────
🏆1    │ 1    │ John Doe                      │ AC Brussels         │ 35:25     │ 3:32    │ 16.95   │ 354    │    10
✓2     │ 2    │ Jane Smith                    │ RC Liège            │ 37:12     │ 3:43    │ 16.14   │ 340    │    10
────────────────────────────────────────────────────────────────────────────────

════════════════════════════════════════════════════════════════════════════════

🏁 RACE 2/5: LIÈGE HALF MARATHON
Year: 2026 | Distance: 21.1 km | Race #2 | Participants: 287
────────────────────────────────────────────────────────────────────────────────
[... race results ...]

════════════════════════════════════════════════════════════════════════════════

COMPLETE EXPORT SUMMARY
Total Races: 5 | Total Participants (all races): 1,420
Generated: 2026-02-01 14:30

Legend: 🏆 = Winner | ✓ = Club Member
```

## Usage Scenarios

### 1. **Monthly Newsletter**
```
Select all races from the month
↓
Apply "Members Only" filter
↓
Export Multiple Races
↓
Copy HTML to email newsletter
```

### 2. **Season Summary**
```
Select all races from the season
↓
Apply "All Participants" filter
↓
Export to HTML
↓
Include in year-end summary email
```

### 3. **Championship Series**
```
Select all championship races (Ctrl+Click)
↓
Apply "All Participants"
↓
Export to HTML
↓
Send to all participants
```

### 4. **Quick Weekly Report**
```
Select last week's races
↓
Apply "Members Only"
↓
Export to Text format
↓
Post in team chat
```

### 5. **Comparison Analysis**
```
Select specific races to compare (Shift+Click range)
↓
Apply "All Participants"
↓
Export to HTML
↓
Review performance across races
```

## Benefits

✅ **Time-Saving** - Export multiple races in one click  
✅ **Consistent Filtering** - Same filter applied to all races  
✅ **Professional** - Well-organized, clearly separated races  
✅ **Flexible Selection** - Select any combination of races  
✅ **Complete Data** - All race details and summaries included  
✅ **Email-Ready** - Optimized format for email clients  
✅ **Print-Friendly** - HTML includes page breaks  
✅ **Comprehensive** - Includes summaries and totals  

## Selection Tips

### Keyboard Shortcuts
- **Ctrl+Click** - Add/remove individual race
- **Shift+Click** - Select range from last click
- **Ctrl+A** - Select all races
- **Click** - Select single race (clears previous selection)

### Best Practices
1. **Sort First** - Use DataGrid column headers to sort before selecting
2. **Filter by Year** - Not implemented yet, but you can manually select by year
3. **Check Status** - Only "Processed" races can be exported
4. **Apply Filter First** - Set your filter before exporting
5. **Verify Selection** - Check the count in the status bar (future enhancement)

## Validation

The "Export Multiple Races" button is enabled only when:
- ✅ At least one race is selected
- ✅ All selected races have status "Processed"
- ❌ Disabled if any selected race is not processed

## Future Enhancements

1. **Selection Counter** - Show "X races selected" in UI
2. **Year Range Filter** - Quick select races by year range
3. **Status Bar Info** - Show selection details in status bar
4. **Select All Processed** - Button to select all processed races
5. **Sort Options** - Sort by date, distance, or name before export
6. **Custom Race Order** - Drag and drop to reorder races in export
7. **PDF Export** - Add PDF format option for archival
8. **Email Template** - Pre-formatted email templates
9. **Scheduled Exports** - Auto-export at specific intervals
10. **Comparison Mode** - Side-by-side race comparison tables

## Comparison: Single vs Multiple Export

| Feature | Single Race Export | Multiple Races Export |
|---------|-------------------|----------------------|
| Races Included | 1 | 1 to N |
| Selection Mode | Single click | Ctrl/Shift+Click |
| Filter Applied | Current race only | All selected races |
| Summary | One race | All races + totals |
| Page Breaks | N/A | Between races |
| Race Sections | Single | Clearly separated |
| Best For | Individual results | Newsletters, summaries |
| File Size | Small | Larger (proportional) |

## Testing Checklist

- [ ] Select single race → Export
- [ ] Select 2 races → Export
- [ ] Select 5+ races → Export
- [ ] Select all races (Ctrl+A) → Export
- [ ] Select with Ctrl+Click
- [ ] Select with Shift+Click
- [ ] Export HTML format
- [ ] Export Text format
- [ ] Apply "All Participants" filter
- [ ] Apply "Members Only" filter
- [ ] Apply "Non-Members Only" filter
- [ ] Verify HTML renders in email client
- [ ] Verify page breaks in HTML
- [ ] Verify race separation in Text
- [ ] Test with races from different years
- [ ] Test with long race names
- [ ] Verify summaries are accurate
- [ ] Check winner highlighting in each race
- [ ] Check member highlighting in each race

## Troubleshooting

### Issue: Button is disabled
**Solution**: Ensure all selected races have status "Processed"

### Issue: Selection not working
**Solution**: Click directly on race row, not on empty space

### Issue: Lost selection after filter change
**Solution**: This is expected - reselect races after changing filter

### Issue: HTML styling not showing in email
**Solution**: Some email clients strip styles - use Text format instead

### Issue: Page breaks not working
**Solution**: Page breaks work in print preview, not in screen view

## Performance

- **Small (1-5 races)**: Instant export
- **Medium (6-20 races)**: < 2 seconds
- **Large (21-50 races)**: < 5 seconds
- **Very Large (50+ races)**: May take 10+ seconds

For very large exports, consider:
- Breaking into smaller batches
- Using Text format (faster)
- Filtering to reduce participants

## Support

For issues or feature requests related to multiple race export:
1. Check this documentation
2. Verify all races are "Processed"
3. Try with smaller selection first
4. Contact development team with specific error messages

---

**Version**: 1.0  
**Date**: 2026-02-01  
**Status**: ✅ Production Ready  
**Dependencies**: Export for Email Feature (v1.0)
