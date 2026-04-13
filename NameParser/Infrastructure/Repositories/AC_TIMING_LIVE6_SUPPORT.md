# AC Timing LIVE6 Format Support

## Overview
Enhanced the `AcnTimingRaceResultRepository` to handle the new chronorace.be LIVE6 format, which uses clean array data without HTML tags.

## Changes Made

### URL Support
The repository now supports the following URL formats:
1. **Standard ACN Timing URLs**: 
   - `https://www.acn-timing.com/?lng=FR#/events/{eventId}/ctx/{date}_{location}/generic/{raceId}/home/LIVE1`

2. **Direct chronorace.be API URLs** (NEW):
   - `https://results.chronorace.be/api/results/table/search/{context}/{viewId}?srch=&pageSize=10000`
   - Example: `https://results.chronorace.be/api/results/table/search/20260412_liege/LIVE6?srch=&pageSize=10000`

### Data Format Support

The parser now handles two different chronorace.be array formats:

#### 1. HTML Format (Old Style)
```json
["46.","M","411","<b>DE VOS Nicolas</b><br/><small></small>","BEL","M","","","Finish","<b>1:36:20</b><br/><small>  13.4km/h</small>","12","V1H","detail:411_3","","411"]
```
- Position, Sex, Bib, Name (HTML), Country, Sex2, ?, ?, Status, Time (HTML), Category Position, Category, Detail, Style, Bib2

#### 2. Clean Format (New Style - LIVE6)
```json
["1.","14263","ZALESKI Jules","TEAMULIEGE","BEL","M","","TEAMULIEGE","TEAMULIEGE","Finish","3:28:45","-","13.250","1","SEH","LINK:...","","14263","diplome.gif","..."]
```
- Position, Bib, Name, Team, Country, Sex, ?, ?, ?, Status, Time, ?, Speed, Category Position, Category, Link, ?, ?, ?, ?

### Key Features

1. **Automatic Format Detection**: The parser automatically detects which format is being used by checking if the name field contains HTML tags.

2. **DNS/DNF Support**: Participants who didn't start or finish (marked with "-" for position) are included in results but without position data.

3. **Sex Position Tracking**: Sex positions are calculated dynamically by counting finishers (participants with valid positions) of each sex.

4. **Null Groups Handling**: When the race hasn't started yet (Groups is null), returns appropriate message.

5. **Team vs Country**: 
   - Clean format: Uses team name if available
   - HTML format: Falls back to country code

### Output Format
Results are formatted using metadata tags for downstream processing:
```
TMEM;RACETIME;3:28:45;POS;1;SPEED;13.25;SEX;M;POSITIONSEX;1;CATEGORY;SEH;POSITIONCAT;1;TEAM;TEAMULIEGE;ISMEMBER;0;Jules;Zaleski
```

## Testing

### Unit Tests Added
1. `ParseJsonResults_ChronoraceCleanFormat_ReturnsCorrectResults`
   - Tests the new LIVE6 clean array format
   - Validates finishers with position and times
   - Validates DNS/DNF participants without positions

2. `ParseJsonResults_ChronoraceHtmlFormat_ReturnsCorrectResults`
   - Tests the old HTML format for backward compatibility
   - Verifies HTML tag stripping

3. `ParseJsonResults_ChronoraceNullGroups_ReturnsEmptyResults`
   - Tests behavior when race hasn't started (Groups is null)

### Example Usage
```csharp
var repository = new AcnTimingRaceResultRepository();
var members = GetClubMembers(); // Your list of club members

// Direct API URL (new format)
var results = repository.GetRaceResults(
    "https://results.chronorace.be/api/results/table/search/20260412_liege/LIVE6?srch=&pageSize=10000",
    members
);

// Returns 201 participants from the Liège race
```

## Implementation Details

### JSON Parsing Order
The parser checks for different JSON structures in this order:
1. Array root (simple JSON arrays)
2. Groups property (chronorace.be structure)
3. Other standard properties (rows, results, data, participants)

This ensures backward compatibility with existing formats while supporting the new chronorace.be structure.

### Name Parsing
Names in chronorace.be format are typically "LASTNAME Firstname":
- "ZALESKI Jules" → FirstName: "Jules", LastName: "Zaleski"
- "DE VOS Nicolas" → FirstName: "Nicolas", LastName: "De Vos"

The parser splits at the last space to handle multi-word last names correctly.

## Status Code
The API returns HTTP 201 (Created) for successful responses with data, which is handled correctly by the implementation.
