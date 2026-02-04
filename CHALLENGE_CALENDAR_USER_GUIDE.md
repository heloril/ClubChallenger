# Challenge Calendar Feature - User Guide

## Overview
The Challenge Calendar tab provides a chronological view of all race events in a challenge, ordered by date with their assigned race numbers. This feature helps verify that challenges are complete and correctly configured, and provides multiple export options for sharing and documentation.

## Key Features

### 1. **Calendar View**
- All race events displayed in chronological order
- Race numbers shown for each event (matching the date ordering)
- Status indicators for each race event
- Distance information for all races in the event
- Visual indicators for completion status

### 2. **Export Functionality**
- **PDF Export**: Professional document format (requires iText7 library)
- **Word Export**: Fully implemented with formatted tables
- **Excel Export**: Color-coded status indicators and full statistics

### 3. **Status Tracking**
- **Processed**: Race results have been uploaded and processed
- **Uploaded**: Race has been created but results not yet processed
- **Pending**: Race event exists but no races created yet

## How to Use

### Viewing the Challenge Calendar

1. **Open the Challenge Calendar Tab**
   - Click on the "Challenge Calendar" tab in the main window

2. **Select a Challenge**
   - From the "Select Challenge" dropdown, choose the challenge you want to view
   - The dropdown shows: Challenge Name - Year

3. **Load the Calendar**
   - Click the "📅 Load Calendar" button
   - The calendar will display all race events ordered by date

### Understanding the Calendar Display

The calendar shows the following information for each race event:

| Column | Description |
|--------|-------------|
| **Race #** | Sequential number assigned based on date order |
| **Date** | Event date in dd/MM/yyyy format |
| **Event Name** | Name of the race event |
| **Location** | Where the race takes place |
| **Distances** | All available distances (e.g., "10km, 21.1km, 42.195km") |
| **Status** | Current processing status (color-coded) |
| **Races** | Number of individual races for this event |

### Status Colors

- **Green (Processed)**: All races have been processed with results
- **Orange (Uploaded)**: Races created but awaiting results
- **Red (Pending)**: Race event exists but no races created

## Exporting the Calendar

### Export to Word (.docx)

**Best for**: Sharing with stakeholders, printing, email attachments

**Features:**
- Professional table format with borders
- Header row with bold formatting
- All race event details included
- Summary statistics at the bottom
- Automatic page formatting

**How to Export:**
1. Load the challenge calendar
2. Click "📝 Export to Word"
3. Choose save location and filename
4. The Word document opens automatically (if you choose)

**What's Included:**
- Challenge name and year as title
- Complete race calendar table
- Total race events count
- Date, location, and status for each event

### Export to Excel (.xlsx)

**Best for**: Data analysis, further editing, tracking progress

**Features:**
- Color-coded headers (light blue)
- Status indicators with background colors:
  - Green: Processed
  - Yellow: Uploaded
  - Light coral: Pending
- Auto-fitted columns
- Summary statistics section
- Professional borders and formatting

**How to Export:**
1. Load the challenge calendar
2. Click "📊 Export to Excel"
3. Choose save location and filename
4. Excel file is created with formatted data

**Excel Contents:**
- Title row with challenge name and year
- Headers with color coding
- All race event data
- Color-coded status cells
- Summary section with:
  - Total race events
  - Number processed
  - Number pending

### Export to PDF

**Best for**: Final documentation, archiving, official records

**Status**: Requires additional library (iText7)
**How to implement**: See Technical Notes section below

**Planned Features:**
- Professional PDF layout
- Embedded fonts for consistency
- Page numbers
- Header/footer with challenge information
- Table of contents for multi-page calendars

## Verifying Challenge Completeness

### What to Check

1. **Race Numbers Match Date Order**
   - Verify that Race # 1 is the earliest date
   - Ensure sequential numbering with no gaps
   - Confirm last race number matches total count

2. **All Expected Events Present**
   - Check that all planned races are listed
   - Verify dates are correct
   - Confirm no duplicate entries

3. **Status Indicators**
   - All events should eventually be "Processed" (green)
   - "Pending" events need attention
   - "Uploaded" events need results processing

4. **Distance Coverage**
   - Verify all expected distances are available
   - Check for missing distances
   - Confirm distance values are correct (10, 21.1, 42.195, etc.)

### Common Issues and Solutions

**Issue: Race numbers don't match expectations**
- **Cause**: Events may have been added out of order
- **Solution**: The race number is automatically assigned based on the race year and creation order. The calendar shows the correct chronological order regardless of the assigned number.

**Issue: Some events show "Pending" status**
- **Cause**: No races have been created for this event
- **Solution**: Go to "Upload and Process" tab and add race results for these events

**Issue: Events show "Uploaded" but not "Processed"**
- **Cause**: Races created but results not yet processed
- **Solution**: Process the race results in the "Race Classification" tab

**Issue: Missing events in the calendar**
- **Cause**: Events not associated with the challenge
- **Solution**: Go to "Challenge Management" tab and associate the missing events

## Use Cases

### 1. Pre-Season Planning
- Export calendar to Word/Excel
- Share with participants to show full race schedule
- Use as marketing material
- Print for bulletin boards

### 2. Mid-Season Tracking
- Check status of each event
- Identify events needing attention
- Track progress through the season
- Report to board/committee

### 3. End-of-Season Documentation
- Export complete calendar with all events processed
- Archive for historical records
- Use in annual reports
- Share accomplishments with members

### 4. Challenge Validation
- Verify challenge is properly configured
- Ensure race numbers are logical
- Check for data quality issues
- Confirm all events are included

## Tips & Best Practices

### 1. Regular Monitoring
- Check the calendar after each race event
- Verify status updates to "Processed"
- Monitor for any pending items

### 2. Export for Records
- Export to Excel monthly for tracking
- Export to Word at season end for archives
- Keep backup copies of exports

### 3. Data Quality
- Use calendar to spot errors early
- Check for duplicate entries
- Verify dates and locations
- Confirm distance information

### 4. Communication
- Share Word export with participants
- Use Excel for committee meetings
- Post PDF on website (when available)

## Technical Notes

### PDF Export Implementation

To implement PDF export, you'll need to:

1. **Install iText7**
   ```
   Install-Package itext7
   ```

2. **Implementation Example**
   ```csharp
   using iText.Kernel.Pdf;
   using iText.Layout;
   using iText.Layout.Element;
   
   // Create PDF document
   // Add title
   // Create table with race events
   // Add status indicators
   // Save document
   ```

3. **Features to Include**
   - Professional table layout
   - Color-coded status (if using color)
   - Page headers/footers
   - Challenge name and year
   - Date/time generated

### Customization Options

You can customize the calendar view by modifying:

1. **Date Format**
   - Change in `ChallengeCalendarItem` binding
   - Default: `dd/MM/yyyy`
   - Options: `MM/dd/yyyy`, `yyyy-MM-dd`, etc.

2. **Status Colors**
   - Modify in MainWindow.xaml DataTriggers
   - Change Excel export color codes
   - Update Word export if needed

3. **Additional Columns**
   - Add to `ChallengeCalendarItem` class
   - Update XAML DataGrid
   - Include in export methods

## Keyboard Shortcuts

- **Ctrl+R**: Refresh challenges (when implemented)
- **Ctrl+L**: Load calendar (when implemented)
- **Ctrl+E**: Export to Excel (when implemented)
- **Ctrl+W**: Export to Word (when implemented)

## Troubleshooting

### Calendar Not Loading
**Problem**: No data appears after clicking Load Calendar
**Solutions:**
1. Ensure a challenge is selected
2. Check that the challenge has associated race events
3. Verify database connection
4. Check status message for errors

### Export Fails
**Problem**: Export buttons don't work or error occurs
**Solutions:**
1. Ensure calendar is loaded first
2. Check that you have write permissions to save location
3. Close any open files with the same name
4. Verify required libraries are installed (EPPlus, OpenXML)

### Missing Events
**Problem**: Calendar doesn't show all expected events
**Solutions:**
1. Go to Challenge Management tab
2. Verify events are associated with the challenge
3. Check that events exist in Race Event Management
4. Use the "Add to Challenge" feature if needed

### Incorrect Race Numbers
**Problem**: Race numbers don't match expected order
**Note**: Race numbers are assigned automatically when races are created, not from the calendar view. The calendar simply displays the existing race numbers in date order. The race number is used for challenge point calculations and should match the date order for consistency.

## Related Features

- **Challenge Management**: Create and configure challenges
- **Race Event Management**: Define race events and distances
- **Upload and Process**: Add race results to events
- **Race Classification**: View and process individual race results
- **Challenger Classification**: See how challengers are performing

## Future Enhancements

Planned improvements for future releases:

1. **PDF Export**: Complete implementation with iText7
2. **Email Integration**: Send calendar directly via email
3. **Calendar Print View**: Optimized layout for printing
4. **Filter Options**: Filter by status, date range, location
5. **Search Functionality**: Find specific events quickly
6. **Batch Operations**: Update multiple event statuses at once
7. **Visual Calendar**: Month-view calendar display
8. **Event Details**: Click to see full event information
9. **Quick Actions**: Process/upload directly from calendar
10. **Mobile View**: Responsive design for tablets/phones

## FAQ

**Q: Can I edit race events from the calendar view?**
A: No, the calendar is read-only. Use Race Event Management tab to edit events.

**Q: Why do some events have multiple rows?**
A: They don't - each row is one event. The "Distances" column shows all distances for that event.

**Q: Can I change the race number order?**
A: Race numbers are assigned automatically based on race creation order. The calendar shows the date order regardless of the race number.

**Q: How often should I export the calendar?**
A: Export after each race event to track progress, and at the end of the season for final documentation.

**Q: Can I customize the export format?**
A: Yes, developers can modify the export methods in `ChallengeCalendarViewModel.cs` to customize formatting, colors, and content.

**Q: Why can't I export to PDF?**
A: PDF export requires the iText7 library. This is a placeholder feature that can be implemented by installing the library and adding the export code.

## Support

For issues or questions:
1. Check this user guide
2. Review error messages in the status bar
3. Check the Output window for detailed errors
4. Consult the technical documentation
5. Contact system administrator

---

**Last Updated**: February 2026
**Version**: 1.0
**Feature Status**: Active
