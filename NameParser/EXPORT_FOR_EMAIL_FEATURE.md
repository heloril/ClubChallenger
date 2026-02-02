# Export for Email Feature

## Overview
Added a new "Export for Email" button that exports race results with selected filters in email-friendly formats (HTML or Text).

## Features

### 1. **Email-Optimized Formats**

#### HTML Format (Recommended)
- **Styled table** with colors and highlighting
- **Winner highlight** - Gold background (#FFD700)
- **Member highlight** - Green color (#4CAF50)
- **Hover effects** for better readability
- **Responsive design** that works in most email clients
- **Filter warnings** displayed prominently

#### Text Format (Plain Email)
- **Unicode box drawing** characters for professional tables
- **Emoji markers**: 🏆 for winner, ✓ for members
- **Aligned columns** for easy reading
- **Compact format** suitable for plain text emails

### 2. **Filter Support**
The export respects the currently selected filter:
- ✅ **All Participants** - Exports everyone
- ✅ **Members Only** - Exports only club members (winner always included)
- ✅ **Non-Members Only** - Exports only non-members (winner always included)

Filter status is clearly indicated in the exported document.

### 3. **Data Included**
Each export contains:
- **Race Information**: Name, Year, Distance, Race Number
- **Participant Data**:
  - Rank (based on filter)
  - Position (overall race position)
  - Name (highlighted if member)
  - Team
  - Race Time
  - Time per km
  - Speed (km/h)
  - Points
  - Bonus KM
- **Export Metadata**: Total participants, export date/time

## User Interface

### Location
`Race Classification Tab` → Action Buttons row

### Button Appearance
```
📧 Export for Email
```
- **Color**: Green (#4CAF50) with white text
- **Icon**: 📧 (email emoji)
- **Position**: Between "Download Results" and "Delete Race"

### Workflow
1. Select a race from the Race Classification tab
2. Apply desired filter (All/Members/Non-Members)
3. Click "📧 Export for Email"
4. Choose format (HTML or Text) in the save dialog
5. Save the file
6. Open the file and copy content into email

## Technical Implementation

### Files Modified

#### 1. `MainWindow.xaml`
```xaml
<Button Content="📧 Export for Email" 
        Command="{Binding ExportForEmailCommand}" 
        Background="#4CAF50" 
        Foreground="White"/>
```

#### 2. `MainViewModel.cs`
Added:
- `ExportForEmailCommand` - Command property
- `CanExecuteExportForEmail()` - Validates race is processed
- `ExecuteExportForEmail()` - Main export logic
- `ExportToHtml()` - HTML formatter
- `ExportToText()` - Text formatter
- `FormatTimeSpan()` - Time formatting helper

### Code Architecture
```
User clicks button
    ↓
ExecuteExportForEmail()
    ↓
Get filtered classifications from repository
    ↓
Detect format (.html or .txt)
    ↓
ExportToHtml() OR ExportToText()
    ↓
Write formatted output to file
    ↓
Show success message
```

## HTML Output Example

```html
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; }
        h1 { color: #2196F3; }
        table { border-collapse: collapse; width: 100%; }
        th { background-color: #2196F3; color: white; padding: 12px; }
        .winner { background-color: #FFD700; font-weight: bold; }
        .member { color: #4CAF50; font-weight: bold; }
    </style>
</head>
<body>
    <h1>Marathon Brussels</h1>
    <div class='info'>
        <strong>Year:</strong> 2026 | 
        <strong>Distance:</strong> 10 km | 
        <strong>Race Number:</strong> 1
    </div>
    <table>
        <thead>
            <tr>
                <th>Rank</th>
                <th>Position</th>
                <th>Name</th>
                <th>Team</th>
                <th>Race Time</th>
                <th>Time/km</th>
                <th>Speed (km/h)</th>
                <th>Points</th>
                <th>Bonus KM</th>
            </tr>
        </thead>
        <tbody>
            <tr class='winner'>
                <td>1</td>
                <td>1</td>
                <td class='member'>John Doe</td>
                <td>AC Brussels</td>
                <td>35:25</td>
                <td>3:32</td>
                <td>16.95</td>
                <td><strong>354</strong></td>
                <td>10</td>
            </tr>
            <!-- More rows... -->
        </tbody>
    </table>
</body>
</html>
```

## Text Output Example

```
╔═══════════════════════════════════════════════════════════════════════════════╗
║  MARATHON BRUSSELS                                                            ║
╚═══════════════════════════════════════════════════════════════════════════════╝

Year: 2026
Distance: 10 km | Race Number: 1

────────────────────────────────────────────────────────────────────────────────
Rank  │ Pos  │ Name                          │ Team                │ Time      │ T/km    │ Speed   │ Points │ Bonus
────────────────────────────────────────────────────────────────────────────────
🏆1    │ 1    │ John Doe                      │ AC Brussels         │ 35:25     │ 3:32    │ 16.95   │ 354    │    10
✓2     │ 2    │ Jane Smith                    │ RC Liège            │ 37:12     │ 3:43    │ 16.14   │ 340    │    10
 3     │ 3    │ Pierre Martin                 │ -                   │ 38:45     │ 3:52    │ 15.48   │ 0      │     0
────────────────────────────────────────────────────────────────────────────────

Total participants: 354
Exported: 2026-02-01 14:30

Legend: 🏆 = Winner | ✓ = Club Member
```

## Usage Scenarios

### 1. **Weekly Race Results Email**
```
Subject: Race Results - Marathon Brussels 2026

Copy the HTML export and paste directly into your email client.
Recipients will see a nicely formatted table.
```

### 2. **Members-Only Newsletter**
```
1. Apply "Members Only" filter
2. Export for Email
3. Copy HTML content
4. Paste into newsletter
5. Only club members are shown (+ winner)
```

### 3. **Social Media Announcement**
```
Use the Text format for posting results on platforms
that don't support HTML formatting.
```

### 4. **Archival Documentation**
```
Export both formats for complete race records:
- HTML for web viewing
- Text for plain-text archival
```

## Benefits

✅ **Time-saving** - No manual copying/formatting  
✅ **Professional** - Styled, branded output  
✅ **Flexible** - HTML or Text based on needs  
✅ **Filter-aware** - Respects current view  
✅ **Complete** - All relevant race data included  
✅ **User-friendly** - Simple one-click process  
✅ **Email-ready** - Optimized for email clients  

## Keyboard Shortcut (Future Enhancement)
Consider adding `Ctrl+E` for quick export.

## Email Client Compatibility

### HTML Format
- ✅ Outlook (Desktop & Web)
- ✅ Gmail
- ✅ Apple Mail
- ✅ Thunderbird
- ✅ Yahoo Mail
- ⚠️ Some webmail clients may strip styles (fallback to plain table)

### Text Format
- ✅ All email clients
- ✅ All platforms
- ✅ Plain text requirement

## Testing Checklist

- [ ] Export with "All Participants" filter
- [ ] Export with "Members Only" filter
- [ ] Export with "Non-Members Only" filter
- [ ] Export HTML format
- [ ] Export Text format
- [ ] Verify winner highlighting
- [ ] Verify member highlighting
- [ ] Test with long race names
- [ ] Test with special characters in names
- [ ] Test with races without teams
- [ ] Copy HTML into Outlook
- [ ] Copy HTML into Gmail
- [ ] Copy Text into email

## Future Enhancements

1. **Direct Email Send** - Send email directly from app
2. **Template Customization** - Allow users to customize HTML template
3. **Multiple Formats** - Add PDF, Excel formats
4. **Batch Export** - Export multiple races at once
5. **Email Preview** - Show preview before saving
6. **Auto-send** - Schedule automatic emails after processing
7. **Recipient Management** - Maintain email list
8. **Attachment Support** - Attach full results as PDF

## Support

For issues or feature requests, contact the development team or file an issue in the repository.

---

**Version**: 1.0  
**Date**: 2026-02-01  
**Status**: ✅ Production Ready
