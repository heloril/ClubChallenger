# Race Event-Based Upload Feature

## Overview
The Upload and Process tab has been completely redesigned to work with Race Events and their predefined distances. This streamlines the race processing workflow by eliminating manual entry of race details.

## Key Changes

### 1. New Workflow
**Old Workflow:**
- Manually enter: Race Name, Year, Race Number, Distance
- Select one file
- Process one race at a time

**New Workflow:**
- Select a Race Event from dropdown
- System shows all available distances for that event
- Upload a file for each distance you want to process
- Process all selected distances at once
- System automatically assigns race number and extracts year from event date

### 2. Benefits
- **Less manual entry**: Race name and year are taken from the event
- **Automatic race numbering**: System calculates the next available race number
- **Batch processing**: Process multiple distances in one operation
- **Consistency**: All races from the same event share the same race number
- **Error reduction**: Less typing means fewer mistakes

## Technical Implementation

### New Files
1. **RaceDistanceUploadModel.cs**
   - View model for tracking file uploads per distance
   - Properties: `Distance`, `FilePath`, `HasFile`, `StatusMessage`

2. **ZeroToVisibilityConverter.cs**
   - Converter for showing/hiding "no distances" message

### Modified Files

#### MainViewModel.cs
**New Properties:**
- `SelectedUploadRaceEvent`: The selected race event for upload
- `AvailableDistancesForUpload`: Collection of distances with file upload status
- `SelectedDistanceUpload`: Currently selected distance
- `_nextRaceNumber`: Auto-calculated next race number

**New Commands:**
- `BrowseDistanceFileCommand`: Opens file dialog for a specific distance
- `ProcessAllDistancesCommand`: Processes all distances with uploaded files

**New Methods:**
- `LoadAvailableDistancesForUpload()`: Loads distances when event is selected
- `CalculateNextRaceNumber(int year)`: Calculates next race number for the year
- `ExecuteBrowseDistanceFile(RaceDistanceUploadModel)`: Handles file selection
- `CanExecuteProcessAllDistances()`: Validates processing can start
- `ExecuteProcessAllDistances()`: Processes all selected races

#### RelayCommand.cs
**Added:**
- `RelayCommand<T>`: Generic version for typed command parameters

#### MainWindow.xaml
**Complete redesign of Upload and Process tab:**
- Race Event selector (ComboBox)
- Distance list with individual file browsers
- Status tracking per distance
- Simplified instructions
- Single "Process All" button

## Usage

### Processing Races

1. **Select Race Event**
   - Choose the event from the dropdown
   - System loads all predefined distances
   - System displays the next race number that will be assigned

2. **Upload Files**
   - For each distance you want to process, click "Browse File..."
   - Select the Excel (.xlsx) or PDF file containing results
   - File name is displayed
   - Status shows "Ready to process"

3. **Process**
   - Click "Process All Selected Races"
   - System shows confirmation dialog with:
     - Event name and date
     - Year and race number
     - List of distances to process
   - Click "Yes" to proceed

4. **Results**
   - Each distance is processed sequentially
   - Status updates show progress
   - Success dialog shows summary
   - Form clears automatically if all succeed

### Example Scenario

**Event**: Trail des Bruyères (15/06/2024)
**Distances**: 10 km, 21 km, 42 km

**Steps:**
1. Select "Trail des Bruyères - 15/06/2024" from dropdown
2. System shows 3 distances with browse buttons
3. Upload 10km_results.xlsx for 10 km distance
4. Upload 21km_results.xlsx for 21 km distance
5. Skip 42 km (no file uploaded)
6. Click "Process All Selected Races"
7. System processes 2 races with race number 5:
   - Trail des Bruyères (10 km) - Race #5
   - Trail des Bruyères (21 km) - Race #5

## Database Impact

All races from the same event processing operation:
- Share the same `RaceNumber`
- Have the same `Year` (from event date)
- Have the same `Name` (from event)
- Are linked to the same `RaceEventId`
- Have different `DistanceKm` values

## Error Handling

- If a race event has no distances defined, user is directed to Race Event Management tab
- If no files are selected, warning message is shown
- Each distance is processed independently - failures don't stop others
- Failed distances show error messages
- Summary shows successful vs. failed counts

## Future Enhancements

- Drag-and-drop file upload
- Progress bar for multiple race processing
- Ability to edit race details before processing
- Validation against race event dates
- Support for uploading all distances at once (zip file)
