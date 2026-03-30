# UI Update Summary - ACN Timing URL Support

## What Changed

The **TabUploadProcess** in the ClubChallenger UI has been enhanced to support ACN Timing URLs in addition to file uploads.

## Updated Files

### 1. XAML Changes (`MainWindow.xaml`)

**Before:**
```xml
<TextBox Text="{Binding FileName, Mode=OneWay}" IsReadOnly="True"/>
<Button Content="Browse File..."/>
```

**After:**
```xml
<!-- Source Type Indicator -->
<TextBlock Text="{Binding SourceType}"/>
<TextBox Text="{Binding FileName, Mode=OneWay}" IsReadOnly="True"/>

<!-- URL Input Field -->
<TextBox Text="{Binding FilePath, UpdateSourceTrigger=PropertyChanged}" 
         ToolTip="Enter ACN Timing URL"/>
<Button Content="Clear"/>

<!-- File Browser -->
<Button Content="📁 Browse File..."/>
```

### 2. ViewModel Changes (`RaceDistanceUploadModel.cs`)

**New Properties:**
- `IsUrl` - Detects if source is a URL
- `SourceType` - Returns icon and type (🌐 URL, 📄 PDF, 📊 Excel, 📁 File)

### 3. MainViewModel Changes

**New Command:**
- `ClearDistanceSourceCommand` - Clears selected file or URL

**Updated Logic:**
- `ExecuteBrowseDistanceFile()` - Supports .json and .html files
- `ExecuteProcessAllDistances()` - Automatically selects correct parser based on source type

## New UI Layout

```
┌─────────────────────────────────────────────────────────────┐
│ Distance Files Upload                                        │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│ ┌──────────────────────────────────────────────────────────┐│
│ │ Distance: 10 km                                          ││
│ │                                                           ││
│ │ 🌐 URL │ ACN Timing URL                                  ││
│ │                                                           ││
│ │ ACN Timing URL:                                          ││
│ │ ┌────────────────────────────────────────┐  ┌────────┐  ││
│ │ │ https://www.acn-timing.com/...         │  │ Clear  │  ││
│ │ └────────────────────────────────────────┘  └────────┘  ││
│ │                                                           ││
│ │ ┌──────────────────┐  Ready to process                  ││
│ │ │ 📁 Browse File...│                                     ││
│ │ └──────────────────┘                                     ││
│ └──────────────────────────────────────────────────────────┘│
│                                                               │
│ ┌──────────────────────────────────────────────────────────┐│
│ │ Distance: 21 km                                          ││
│ │                                                           ││
│ │ 📄 PDF │ Marathon_21km_results.pdf                       ││
│ │                                                           ││
│ │ ACN Timing URL:                                          ││
│ │ ┌────────────────────────────────────────┐  ┌────────┐  ││
│ │ │                                         │  │ Clear  │  ││
│ │ └────────────────────────────────────────┘  └────────┘  ││
│ │                                                           ││
│ │ ┌──────────────────┐  Ready to process                  ││
│ │ │ 📁 Browse File...│                                     ││
│ │ └──────────────────┘                                     ││
│ └──────────────────────────────────────────────────────────┘│
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

## User Experience Flow

### Flow 1: Using URL

```
1. User pastes URL in text box
   ↓
2. System detects it's a URL (starts with http/https)
   ↓
3. Source indicator shows "🌐 URL"
   ↓
4. User clicks "Process All Selected Races"
   ↓
5. System uses AcnTimingRaceResultRepository
   ↓
6. Results fetched from web and processed
```

### Flow 2: Using File

```
1. User clicks "Browse File..."
   ↓
2. File dialog opens (supports .xlsx, .pdf, .json, .html)
   ↓
3. User selects file
   ↓
4. Source indicator shows appropriate icon (📄/📊/📁)
   ↓
5. User clicks "Process All Selected Races"
   ↓
6. System selects correct parser based on extension
   ↓
7. File processed and saved to database
```

### Flow 3: Clearing Source

```
1. User has selected file or entered URL
   ↓
2. User clicks "Clear" button
   ↓
3. Source is cleared
   ↓
4. Status resets to "No file or URL selected"
```

## Source Type Detection Matrix

| Input | IsUrl | Extension | Parser Used | Icon |
|-------|-------|-----------|-------------|------|
| https://www.acn-timing.com/... | ✓ | N/A | AcnTimingRaceResultRepository | 🌐 URL |
| file.pdf | ✗ | .pdf | PdfRaceResultRepository | 📄 PDF |
| file.xlsx | ✗ | .xlsx | ExcelRaceResultRepository | 📊 Excel |
| file.json | ✗ | .json | AcnTimingRaceResultRepository | 📁 File |
| file.html | ✗ | .html | AcnTimingRaceResultRepository | 📁 File |

## Code Architecture

```
┌─────────────────────────────────────────────────┐
│          MainWindow.xaml (View)                  │
│  • URL TextBox                                   │
│  • Clear Button                                  │
│  • Browse Button                                 │
│  • Source Type Display                           │
└──────────────────┬──────────────────────────────┘
                   │ Bindings
                   ↓
┌─────────────────────────────────────────────────┐
│    RaceDistanceUploadModel (Model)               │
│  • FilePath (string)                             │
│  • IsUrl (computed)                              │
│  • SourceType (computed)                         │
│  • FileName (computed)                           │
└──────────────────┬──────────────────────────────┘
                   │ Commands
                   ↓
┌─────────────────────────────────────────────────┐
│       MainViewModel (ViewModel)                  │
│  • BrowseDistanceFileCommand                     │
│  • ClearDistanceSourceCommand                    │
│  • ProcessAllDistancesCommand                    │
│                                                   │
│  Logic:                                          │
│  • DetectSourceType()                            │
│  • SelectParser()                                │
│  • ProcessRace()                                 │
└──────────────────┬──────────────────────────────┘
                   │
                   ↓
┌─────────────────────────────────────────────────┐
│        Race Processing Service                   │
│                                                   │
│  ┌──────────────────────────────────────────┐  │
│  │ if (IsUrl)                                 │  │
│  │   → AcnTimingRaceResultRepository         │  │
│  │ else if (.pdf)                             │  │
│  │   → PdfRaceResultRepository               │  │
│  │ else if (.json || .html)                   │  │
│  │   → AcnTimingRaceResultRepository         │  │
│  │ else                                       │  │
│  │   → ExcelRaceResultRepository             │  │
│  └──────────────────────────────────────────┘  │
└─────────────────────────────────────────────────┘
```

## Benefits

1. **No Manual Downloads** - Fetch results directly from web
2. **Flexible Input** - Use URLs or files interchangeably
3. **Clear Visual Feedback** - Icons show source type
4. **Easy Correction** - Clear button for quick reset
5. **Automatic Detection** - System handles all parser selection
6. **Mixed Sources** - Different distances can use different sources

## Testing Checklist

- [ ] Enter ACN Timing URL → Processes correctly
- [ ] Browse and select PDF → Processes correctly
- [ ] Browse and select Excel → Processes correctly
- [ ] Browse and select JSON → Processes correctly
- [ ] Browse and select HTML → Processes correctly
- [ ] Clear URL → Resets correctly
- [ ] Clear file → Resets correctly
- [ ] Mix URL and files → All process correctly
- [ ] Invalid URL → Shows error message
- [ ] Network failure → Shows error message
- [ ] Source type icon → Displays correctly
- [ ] Status messages → Update appropriately

## Migration Notes

**No Breaking Changes:**
- Existing file-based workflows continue to work
- No changes to database schema
- Backward compatible with all existing features

**Enhanced Features:**
- File dialog now includes .json and .html filters
- Status messages include source type information
- Clear button adds convenience

## Support

For issues or questions about URL support:
1. Check `UI_URL_SUPPORT.md` for usage instructions
2. Review `ACN_TIMING_QUICKSTART.md` for ACN Timing specifics
3. See `Documentation/ACN_Timing_Integration.md` for technical details
