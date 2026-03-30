# UI URL Support for ACN Timing

## Overview

The ClubChallenger UI now supports entering ACN Timing URLs directly in the **Upload and Process** tab, in addition to browsing for files. This allows you to fetch race results directly from the ACN Timing website without manually downloading files.

## New Features

### 1. URL Input Field

Each distance in the "Upload Results by Distance" section now has:
- **URL Text Box**: Enter ACN Timing URLs directly
- **Browse File Button**: Browse for local files (PDF, Excel, JSON, HTML)
- **Clear Button**: Clear the selected file or URL
- **Source Type Indicator**: Shows whether the source is a URL, PDF, Excel, etc.

### 2. Supported Source Types

The system now handles multiple source types:
- 🌐 **ACN Timing URLs** - Direct links from www.acn-timing.com
- 📄 **PDF Files** - Standard race result PDFs
- 📊 **Excel Files** - .xlsx spreadsheets
- 📁 **JSON Files** - ACN Timing cached JSON data
- 📁 **HTML Files** - ACN Timing cached HTML pages

## How to Use

### Method 1: Enter URL Directly

1. Select a Race Event
2. For the desired distance, paste the ACN Timing URL in the text box
3. The system will automatically detect it's a URL
4. Click "Process All Selected Races"

Example URL:
```
https://www.acn-timing.com/?lng=FR#/events/2156215316811398/ctx/20260328_spa/generic/198022_3/home/LIVEMARA3
```

### Method 2: Browse for Files

1. Select a Race Event
2. Click "Browse File..." for the desired distance
3. Select a PDF, Excel, JSON, or HTML file
4. Click "Process All Selected Races"

### Method 3: Mix URLs and Files

You can mix sources for different distances:
- Distance 10 km: Use ACN Timing URL
- Distance 21 km: Use local PDF file
- Distance 42 km: Use Excel spreadsheet

## UI Changes

### Before
```
Distance: 10 km
[No file selected]
[Browse File...]
```

### After
```
Distance: 10 km
🌐 URL | ACN Timing URL

ACN Timing URL:
[https://www.acn-timing.com/...] [Clear]

[📁 Browse File...]
```

## Visual Indicators

The UI shows different icons based on source type:

| Icon | Type | Description |
|------|------|-------------|
| 🌐 URL | URL | ACN Timing web address |
| 📄 PDF | File | PDF document |
| 📊 Excel | File | Excel spreadsheet |
| 📁 File | File | Other files (JSON, HTML) |

## Workflow Examples

### Example 1: Process Race from ACN Timing URL

1. Navigate to **Upload and Process** tab
2. Select "Marathon de Spa 2026" from race events
3. For "42 km" distance, paste:
   ```
   https://www.acn-timing.com/?lng=FR#/events/2156215316811398/ctx/20260328_spa/generic/198022_3/home/LIVEMARA3
   ```
4. Click "Process All Selected Races"
5. System automatically:
   - Fetches results from ACN Timing
   - Matches club members
   - Calculates points
   - Saves to database

### Example 2: Process Multiple Distances with Different Sources

1. Select race event
2. For "10 km": Paste ACN Timing URL
3. For "21 km": Click Browse and select local PDF
4. For "42 km": Click Browse and select Excel file
5. Click "Process All Selected Races"
6. All three distances are processed automatically

## Technical Details

### Automatic Parser Selection

The system automatically selects the correct parser:

```csharp
if (IsUrl)
{
    // Use ACN Timing parser for URLs
    parser = new AcnTimingRaceResultRepository();
}
else if (extension == ".pdf")
{
    // Use PDF parser
    parser = new PdfRaceResultRepository();
}
else if (extension == ".json" || extension == ".html")
{
    // Use ACN Timing parser for cached files
    parser = new AcnTimingRaceResultRepository();
}
else
{
    // Use Excel parser
    parser = new ExcelRaceResultRepository();
}
```

### URL Detection

URLs are automatically detected by checking if the input starts with `http://` or `https://`:

```csharp
public bool IsUrl => FilePath.StartsWith("http://") || 
                     FilePath.StartsWith("https://");
```

### File Dialog Filters

The file browser now includes ACN Timing file types:

```
Race Result Files (*.xlsx;*.pdf;*.json;*.html)
Excel Files (*.xlsx)
PDF Files (*.pdf)
ACN Timing Files (*.json;*.html)
All Files (*.*)
```

## Error Handling

### Invalid URL
If you enter an invalid URL, the system will show an error message during processing.

### Network Issues
If the ACN Timing website is unreachable, you'll see a network error. In this case:
1. Check your internet connection
2. Try downloading the results manually
3. Use a cached JSON/HTML file instead

### Mixed Formats
You can process different formats in the same race event without issues.

## Benefits

✅ **Faster Processing** - No need to manually download files  
✅ **Direct Access** - Fetch latest results from ACN Timing  
✅ **Flexibility** - Mix URLs and files for different distances  
✅ **Automatic Detection** - System automatically selects correct parser  
✅ **Clear Indication** - Visual icons show source type  
✅ **Easy Clearing** - One-click to clear and start over  

## Tips

1. **Copy Full URL**: Make sure to copy the complete URL from your browser's address bar
2. **Use Latest Results**: URLs always fetch the most recent data from ACN Timing
3. **Cache for Offline**: If you'll process repeatedly, download and cache the files first
4. **Check Status**: Watch the status message for progress updates
5. **Mix Sources**: Don't hesitate to use different sources for different distances

## Troubleshooting

### URL Not Working
- Verify the URL is complete and correct
- Check if the ACN Timing page is accessible in your browser
- Try downloading the page and using the HTML file instead

### Processing Fails
- Check the status message for specific error details
- Ensure the URL points to race results (not event home page)
- Verify internet connection is active

### Results Not Matching
- ACN Timing URLs always fetch fresh data
- If you need consistent results, download and cache the file first
- Use cached JSON/HTML files for reproducible processing

## Related Documentation

- `ACN_TIMING_QUICKSTART.md` - Quick start guide for ACN Timing integration
- `Documentation/ACN_Timing_Integration.md` - Technical documentation
- `RACE_EVENT_BASED_UPLOAD_FEATURE.md` - Race event upload system

## Future Enhancements

Potential improvements:
- Batch URL download with progress indicator
- URL validation before processing
- Automatic URL detection from clipboard
- Save URL history for quick access
- Preview results before processing
