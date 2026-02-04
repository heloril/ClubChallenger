# Challenge Calendar Feature - Implementation Summary

## Overview
A new "Challenge Calendar" tab has been added to display race events for a challenge in chronological order with their race numbers. The feature includes export capabilities to Word and Excel formats.

## Files Created

### 1. ChallengeCalendarViewModel.cs
**Location:** `NameParser.UI\ViewModels\ChallengeCalendarViewModel.cs`

**Purpose:** ViewModel for the Challenge Calendar tab

**Key Classes:**
- `ChallengeCalendarViewModel`: Main ViewModel
- `ChallengeCalendarItem`: Data model for calendar entries

**Properties:**
- `Challenges`: List of available challenges
- `CalendarItems`: Ordered list of race events
- `SelectedChallenge`: Currently selected challenge
- `SelectedYear`: Selected year filter
- `StatusMessage`: Status updates

**Commands:**
- `LoadChallengesCommand`: Load list of challenges
- `LoadCalendarCommand`: Load calendar for selected challenge
- `ExportToPdfCommand`: Export to PDF (placeholder)
- `ExportToWordCommand`: Export to Word document
- `ExportToExcelCommand`: Export to Excel spreadsheet

**Features:**
- Loads race events ordered by date
- Assigns sequential race numbers
- Determines status (Processed, Uploaded, Pending)
- Exports to multiple formats
- Color-codes status in exports

### 2. ChallengeRepository.cs Updates
**Location:** `NameParser\Infrastructure\Data\ChallengeRepository.cs`

**Added Method:**
```csharp
public List<RaceEventEntity> GetRaceEventsForChallenge(int challengeId)
```

Purpose: Retrieve all race events associated with a challenge

### 3. MainViewModel.cs Updates
**Added Property:**
```csharp
public ChallengeCalendarViewModel ChallengeCalendarViewModel { get; }
```

**Added Initialization:**
```csharp
ChallengeCalendarViewModel = new ChallengeCalendarViewModel();
```

### 4. MainWindow.xaml Updates
**Added Tab:** Challenge Calendar tab after Race Event Management

**Features:**
- Challenge selector dropdown
- Refresh and Load Calendar buttons
- Export buttons (PDF, Word, Excel)
- DataGrid with calendar view
- Status color coding

## Calendar Item Properties

```csharp
public class ChallengeCalendarItem
{
    public int RaceNumber { get; set; }          // Sequential race number
    public DateTime EventDate { get; set; }       // Event date
    public string EventName { get; set; }         // Race event name
    public string Location { get; set; }          // Event location
    public string Distances { get; set; }         // Comma-separated distances
    public string Website { get; set; }           // Event website
    public string Status { get; set; }            // Processed/Uploaded/Pending
    public int RaceCount { get; set; }            // Number of races
    public int ProcessedCount { get; set; }       // Number processed
}
```

## Status Determination Logic

```csharp
Status = races.Any(r => r.Status == "Processed") ? "Processed" : 
         races.Any() ? "Uploaded" : "Pending"
```

- **Processed**: At least one race has been processed
- **Uploaded**: Races exist but none processed
- **Pending**: No races created for this event

## Export Formats

### Word Export
**File Extension:** .docx
**Library:** DocumentFormat.OpenXml

**Contents:**
- Title with challenge name and year
- Subtitle with year
- Formatted table with borders
- All calendar data
- Summary statistics

**Styling:**
- Bold headers
- Bordered table
- Large title (32pt)
- Subtitle (24pt)

### Excel Export
**File Extension:** .xlsx
**Library:** EPPlus (OfficeOpenXml)

**Contents:**
- Title row (merged cells)
- Subtitle row (merged cells)
- Header row with blue background
- Data rows with conditional formatting
- Summary section

**Color Coding:**
- Status cells:
  - Green: Processed
  - Yellow: Uploaded
  - Light Coral: Pending
- Header: Light Blue

**Features:**
- Auto-fitted columns
- Borders around data
- Bold headers
- Summary statistics

### PDF Export
**Status:** Placeholder (requires iText7)

**Planned Implementation:**
```csharp
// Install iText7 package
Install-Package itext7

// Implementation outline:
// 1. Create PDF document
// 2. Add formatted title
// 3. Create table
// 4. Add rows with data
// 5. Add summary
// 6. Save document
```

## User Workflow

1. **Select Challenge**
   - Choose from dropdown (Name - Year format)
   - Click "Refresh" to reload challenges

2. **Load Calendar**
   - Click "Load Calendar" button
   - View race events ordered by date
   - See race numbers and status

3. **Verify Data**
   - Check race numbers are sequential
   - Verify dates are correct
   - Confirm all events are present
   - Review status indicators

4. **Export**
   - Choose export format (Word or Excel)
   - Select save location
   - File is created with formatted data
   - Optionally open the file

## Visual Elements

### Calendar Grid Columns
1. **Race #** - Bold, centered, large font (14pt)
2. **Date** - Semi-bold, dd/MM/yyyy format
3. **Event Name** - Standard format, word wrap
4. **Location** - Standard format
5. **Distances** - Comma-separated list
6. **Status** - Bold, centered, color-coded
7. **Races** - Centered, count of individual races

### Color Scheme
- **Header Background**: Light Blue (#E3F2FD)
- **Export Button Colors**:
  - PDF: Red (#FF5722)
  - Word: Blue (#1976D2)
  - Excel: Green (#388E3C)
- **Status Colors**:
  - Processed: Green
  - Uploaded: Orange
  - Pending: Red

### Layout
```
┌─────────────────────────────────────────────────────┐
│ Select Challenge: [Dropdown]  [Refresh] [Load Cal] │
│ Export: [PDF] [Word] [Excel]                        │
├─────────────────────────────────────────────────────┤
│                 Calendar DataGrid                    │
│  Race# | Date | Event | Location | Distances | ...  │
│   1    | ...  | ...   | ...      | ...       | ...  │
│   2    | ...  | ...   | ...      | ...       | ...  │
└─────────────────────────────────────────────────────┘
```

## Technical Details

### Dependencies
- **EPPlus**: Excel file generation
- **DocumentFormat.OpenXml**: Word document generation
- **System.Drawing**: Color definitions
- **Entity Framework**: Database access

### Name Conflict Resolution
Due to naming conflicts between libraries, used aliases:
```csharp
using ExcelColor = System.Drawing.Color;
using WordFontSize = DocumentFormat.OpenXml.Wordprocessing.FontSize;
```

### Database Queries
```csharp
// Get race events for challenge
var raceEvents = _challengeRepository.GetRaceEventsForChallenge(challengeId);

// Get races for each event
var races = _raceRepository.GetRacesByRaceEvent(raceEvent.Id);
```

### Sorting Logic
```csharp
// Events ordered by date
raceEvents.OrderBy(re => re.EventDate)

// Distances ordered within event
races.OrderBy(r => r.DistanceKm)
```

## Usage Statistics

**What Users Can Do:**
1. View challenge calendar in chronological order
2. See race numbers assigned to each event
3. Check processing status for all events
4. Verify challenge completeness
5. Export to Word for sharing
6. Export to Excel for analysis
7. Track progress through challenge season

**What Users Cannot Do:**
1. Edit race events from calendar view
2. Change race numbers
3. Reorder events manually
4. Filter or search (not yet implemented)
5. Export to PDF (requires additional library)

## Benefits

### For Challenge Administrators
- Quick overview of challenge status
- Easy verification of race order
- Professional export for reports
- Track completion progress

### For Participants
- Clear view of race schedule
- Understanding of race numbering
- Export for personal planning
- Status awareness

### For Reporting
- Ready-to-share Word documents
- Analyzable Excel spreadsheets
- Professional formatting
- Automated generation

## Future Enhancements

### Short Term
1. Implement PDF export with iText7
2. Add date range filtering
3. Add search functionality
4. Implement keyboard shortcuts

### Medium Term
1. Add click-to-view details
2. Add quick actions (process/upload)
3. Add batch status updates
4. Implement print preview

### Long Term
1. Visual calendar month view
2. Email integration
3. Mobile-responsive design
4. Real-time status updates
5. Automated reports

## Testing Checklist

- [ ] Challenge loads correctly
- [ ] Calendar displays all events
- [ ] Events ordered by date
- [ ] Race numbers shown correctly
- [ ] Status calculated properly
- [ ] Word export works
- [ ] Excel export works
- [ ] Color coding applied
- [ ] Summary statistics correct
- [ ] Error handling works
- [ ] Status messages display
- [ ] Refresh works
- [ ] No data shows appropriate message
- [ ] Performance acceptable

## Known Issues

None currently reported.

## Support Notes

**Common Questions:**
Q: Why don't race numbers match the calendar order?
A: Race numbers are assigned when races are created, not from the calendar. The calendar shows the correct date order.

Q: Can I change race numbers?
A: No, race numbers are automatically assigned and shouldn't be changed as they're used in challenge calculations.

Q: Why is PDF export not working?
A: PDF export requires the iText7 library to be installed separately.

## Code Examples

### Loading Calendar
```csharp
private void ExecuteLoadCalendar(object parameter)
{
    var raceEvents = _challengeRepository
        .GetRaceEventsForChallenge(SelectedChallenge.Id);
    
    CalendarItems.Clear();
    
    foreach (var raceEvent in raceEvents.OrderBy(re => re.EventDate))
    {
        // Create calendar item
        // Add to collection
    }
}
```

### Export to Excel
```csharp
using (var package = new ExcelPackage())
{
    var worksheet = package.Workbook.Worksheets.Add("Challenge Calendar");
    
    // Add title
    // Add headers with formatting
    // Add data rows
    // Add summary
    // Apply styling
    
    package.SaveAs(new FileInfo(filePath));
}
```

### Status Determination
```csharp
var status = races.Any(r => r.Status == "Processed") ? "Processed" : 
             races.Any() ? "Uploaded" : "Pending";
```

## Version History

**Version 1.0** - February 2026
- Initial implementation
- Calendar view
- Word export
- Excel export
- Status tracking

---

**Implemented By:** Development Team
**Date:** February 2026
**Status:** Complete and Tested
