# Export Fixes - Complete Columns and Filter Support

## Issues Fixed

### 1. Missing Columns in Exports
**Problem:** Export files only included basic columns (Position, Name, Time, Team, Category, Points)

**Solution:** All exports now include complete classification data:
- Position
- First Name / Last Name (separated)
- Sex
- Position by Sex
- Category
- Position by Category
- Team
- Points
- Race Time
- Time per Km
- Speed (km/h)
- Is Member (✓ indicator)
- Is Challenger (✓ indicator)
- Bonus KM

### 2. Filters Not Applied
**Problem:** Export methods were calling `GetClassificationsByRace(race.Id, null, null)`, ignoring active filters

**Solution:** Now passing active filter values:
```csharp
var classifications = _classificationRepository.GetClassificationsByRace(
    race.Id, 
    IsMemberFilter,      // Now applied
    IsChallengerFilter   // Now applied
);
```

## Files Modified

### MainViewModel.cs

#### 1. ExportRaceEventToHtml()
**Changes:**
- Added filter description to header
- Applied `IsMemberFilter` and `IsChallengerFilter`
- Added all 15 columns to HTML table
- Added CSS classes for members (light green) and challengers (bold)
- Shows participant count per race
- Includes ✓ indicators for Member/Challenger status

**Example Output:**
```html
<div class='event-info'>
    <strong>Date:</strong> 15/04/2026<br/>
    <strong>Location:</strong> Brussels<br/>
    <strong>Filter:</strong> Members only, Winner always shown<br/>
    <strong>Generated:</strong> 2026-02-04 14:30<br/>
</div>
```

#### 2. ExportRaceEventToExcel()
**Changes:**
- Added filter description to info row
- Applied both filters
- Added all 15 columns
- Color coding:
  - Members: Light green background
  - Challengers: Bold text
- Auto-fit columns for better readability
- Shows race number in title

**Features:**
- Each distance gets its own worksheet
- Professional formatting with headers
- Visual distinction for members/challengers
- All data fields included

#### 3. ExportRaceEventToWord()
**Changes:**
- Added filter description
- Applied both filters
- Added all 15 columns to table
- Shows participant count
- Shortened headers for better fit:
  - "Mbr" for Member
  - "Chl" for Challenger
  - "Pos/Sex" for Position by Sex
  - "Pos/Cat" for Position by Category

#### 4. ExportRaceEventSummary()
**Changes:**
- Added filter description
- Applied both filters
- Shows member and challenger counts
- Added visual indicators:
  - 👤 for Club Members
  - ⭐ for Challengers
- Includes legend at end

#### 5. New Helper Method: BuildFilterDescription()
```csharp
private string BuildFilterDescription()
{
    var filters = new List<string>();
    
    if (IsMemberFilter.HasValue)
    {
        filters.Add(IsMemberFilter.Value ? "Members only" : "Non-members only");
    }
    
    if (IsChallengerFilter.HasValue)
    {
        filters.Add(IsChallengerFilter.Value ? "Challengers only" : "Non-challengers only");
    }
    
    if (filters.Count > 0)
    {
        filters.Add("Winner always shown");
        return string.Join(", ", filters);
    }
    
    return string.Empty;
}
```

## Filter Behavior

The filters work the same as in the Race Classification view:

### Member Filter
- **All Participants**: Shows everyone (default)
- **Members Only**: Shows only club members (winner always included)
- **Non-Members Only**: Shows non-members (winner always included)

### Challenger Filter
- **All Participants**: Shows everyone (default)
- **Challengers Only**: Shows only challengers (winner always included)
- **Non-Challengers Only**: Shows non-challengers (winner always included)

**Important:** The race winner is ALWAYS shown regardless of filters, ensuring complete result integrity.

## Export Format Examples

### HTML Export
```
🏁 Brussels 10K
═══════════════════════════════════════
Date: 15/04/2026
Location: Brussels Central
Filter: Members only, Winner always shown
Generated: 2026-02-04 14:30

10 km - Race #5 (45 participants)

┌──────┬────────────┬───────────┬─────┬─────────┬──────────┬─────────┬────────┬────────┬──────────┬──────────┬───────┬────────┬───────────┬──────────┐
│ Pos  │ First Name │ Last Name │ Sex │ Pos/Sex │ Category │ Pos/Cat │ Team   │ Points │ Time     │ Time/km  │ Speed │ Member │ Challenger│ Bonus KM │
├──────┼────────────┼───────────┼─────┼─────────┼──────────┼─────────┼────────┼────────┼──────────┼──────────┼───────┼────────┼───────────┼──────────┤
│ 1    │ John       │ Doe       │ M   │ 1       │ V1       │ 1       │ CLUB A │ 100    │ 00:35:23 │ 03:32    │ 16.95 │ ✓      │ ✓         │ 5        │
│ 2    │ Jane       │ Smith     │ F   │ 1       │ S1       │ 1       │ CLUB B │ 95     │ 00:37:45 │ 03:46    │ 15.89 │ ✓      │           │ 0        │
```

### Excel Export
- Separate worksheet for each distance
- Color-coded (green for members)
- Bold text for challengers
- All fields in sortable columns
- Auto-fitted column widths

### Word Export
- Professional document format
- Formatted tables with all data
- Easy to print or share via email
- Includes filter information

### Text Summary Export
- Quick overview format
- Top 10 per distance
- Member/Challenger indicators
- Participant counts
- Legend included

## Testing Checklist

- [x] HTML export includes all columns
- [x] Excel export includes all columns
- [x] Word export includes all columns
- [x] Summary export shows member/challenger indicators
- [x] Member filter applied correctly
- [x] Challenger filter applied correctly
- [x] Combined filters work correctly
- [x] Winner always shown regardless of filters
- [x] Filter description shown in exports
- [x] Participant counts accurate
- [x] Visual indicators (✓, 👤, ⭐) display correctly
- [x] Build successful

## Usage

1. **Select Race Event** from Race Classification tab
2. **Apply Filters** (optional):
   - Click "Members Only" or "Non-Members Only"
   - Click "Challengers Only" or "Non-Challengers Only"
3. **Click Export Button** (with dropdown arrow)
4. **Select Format**:
   - 📧 Export to HTML (Email)
   - 📊 Export to Excel (.xlsx)
   - 📝 Export to Word (.docx)
   - ⚡ Export Summary (Quick)
5. **Choose Save Location**
6. Export file is created with all columns and filters applied

## Benefits

### For Race Organizers
- Complete data export for records
- Professional formats for sharing
- Filtered views for specific groups
- Easy email distribution (HTML format)

### For Club Administrators
- Member-only reports
- Challenger tracking
- Performance analysis (Speed, Time/km data)
- Bonus km tracking

### For Participants
- Complete results with all details
- Easy to read formats
- Printable documents (Word/HTML)
- Shareable summaries

## Known Limitations

1. **PDF Export**: Still placeholder (requires iText7 library)
2. **Multiple Filter Combinations**: Only one filter type active at a time per category
3. **Large Datasets**: Excel may be slow for events with 1000+ participants

## Future Enhancements

### Planned Features
1. Implement PDF export with iText7
2. Add custom column selection
3. Add export templates
4. Add batch export (multiple races at once)
5. Add email integration (send directly)
6. Add cloud storage export (Google Drive, Dropbox)

### Possible Improvements
1. Chart/graph generation in exports
2. Statistics summary page
3. Comparison between years
4. Custom branding/logos
5. QR codes for result sharing

## Backward Compatibility

✅ **Fully compatible** with existing data and workflows
✅ **No breaking changes** to database or data structures
✅ **Enhanced functionality** - old exports still work, now with more data

## Support Notes

### Common Issues

**Q: Export is empty or missing participants**
A: Check that filters aren't too restrictive. Winners are always shown, so if you see only 1-2 people, you probably have both filters active.

**Q: Excel file won't open**
A: Ensure you have Excel or compatible software installed. Try opening with Google Sheets or LibreOffice.

**Q: HTML export doesn't look good in email**
A: Some email clients strip CSS. Save as HTML and attach as file instead.

**Q: Where are categories Pos/Sex and Pos/Cat calculated?**
A: These are calculated during race processing based on participant demographics.

---

**Fixed Date:** February 2026
**Version:** 2.1
**Status:** ✅ Complete
**Build:** ✅ Successful
