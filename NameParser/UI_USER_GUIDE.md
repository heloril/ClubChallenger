# Race Management UI - User Guide

## Overview

The Race Management System provides a comprehensive user interface for uploading race result files, processing them, and managing race classifications with persistent database storage.

## Features

### 1. **Upload & Process Race**
- Upload Excel files (.xlsx) containing race results
- Define race metadata:
  - Race Name
  - Year (2020-2030)
  - Race Number
  - Distance in kilometers
- Process race results automatically
- Calculate points based on reference time

### 2. **View Results**
- View all processed races
- Display classifications for selected races
- Sort and filter race data
- Real-time status updates

### 3. **Download Results**
- Export race results to text or CSV files
- Formatted reports with race details
- Rankings with points and bonus kilometers

### 4. **Database Persistence**
- All race data stored in local SQL database
- Classifications saved permanently
- Historical race data accessible
- Data integrity maintained with foreign keys

## Getting Started

### Prerequisites

1. **.NET Framework 4.8** or higher
2. **SQL Server LocalDB** (usually included with Visual Studio)
3. **Microsoft Office Excel** COM components (for reading Excel files)
4. **Members.json** file in the application directory

### Installation

1. Build the solution in Visual Studio
2. Ensure all NuGet packages are restored:
   - EntityFramework 6.x
   - Newtonsoft.Json
3. Run the NameParser.UI project

### First Run

On first run, the application will:
1. Create the local database automatically
2. Initialize tables (Races, Classifications)
3. Ready for use immediately

## How to Use

### Processing a Race

1. **Open the Application**
   - The main window opens with two tabs

2. **Navigate to "Upload & Process Race" tab**

3. **Select Excel File**
   - Click "📁 Browse File"
   - Choose your Excel file containing race results
   - File path will be displayed

4. **Enter Race Information**
   - **Race Name**: Enter a descriptive name (e.g., "Brussels Marathon")
   - **Year**: Select the year from dropdown (2020-2030)
   - **Race Number**: Enter a unique number for this race in the year
   - **Distance**: Enter race distance in kilometers

5. **Process**
   - Click "⚡ Process Race" button
   - Wait for processing to complete
   - Status message will confirm success

### Viewing Results

1. **Navigate to "View Results" tab**

2. **Race List**
   - View all processed races
   - Columns: ID, Year, Race #, Name, Distance, Status, Processed Date
   - Click to select a race

3. **View Classifications**
   - Select a race from the list
   - Click "👁️ View Classification"
   - Classifications appear in bottom grid
   - Sorted by points (highest first)

4. **Download Results**
   - Select a race
   - Click "💾 Download Results"
   - Choose save location
   - File contains formatted race results

5. **Delete Race**
   - Select a race
   - Click "🗑️ Delete"
   - Confirm deletion
   - Race and all classifications removed

## Database Schema

### Tables

#### Races Table
```sql
- Id (int, primary key)
- Name (varchar(100))
- Year (int)
- RaceNumber (int)
- DistanceKm (int)
- FilePath (varchar(500))
- CreatedDate (datetime)
- ProcessedDate (datetime, nullable)
- Status (varchar(50))
```

#### Classifications Table
```sql
- Id (int, primary key)
- RaceId (int, foreign key to Races)
- MemberFirstName (varchar(100))
- MemberLastName (varchar(100))
- MemberEmail (varchar(200))
- Points (int)
- BonusKm (int)
- RaceTime (time, nullable)
- CreatedDate (datetime)
```

## Architecture

### Layers

1. **Presentation Layer** (NameParser.UI)
   - WPF User Interface
   - MVVM pattern with ViewModels
   - Data binding and commands

2. **Application Layer**
   - RaceProcessingService
   - ReportGenerationService

3. **Domain Layer**
   - Entities (Member, Race, RaceResult)
   - Aggregates (Classification)
   - Domain Services (PointsCalculationService)

4. **Infrastructure Layer**
   - Database Context (Entity Framework)
   - Repositories (Race, Classification)
   - Excel reading (ExcelRaceResultRepository)

### Key Components

#### ViewModels
- **MainViewModel**: Main application logic
  - Commands for all user actions
  - Observable collections for data binding
  - Async processing support

#### Data Access
- **RaceRepository**: CRUD operations for races
- **ClassificationRepository**: Manage classifications
- **RaceManagementContext**: Entity Framework DbContext

## Connection String

Default connection string in App.config:
```xml
<add name="RaceManagementDb" 
     connectionString="Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=|DataDirectory|\RaceManagement.mdf;Integrated Security=True;Connect Timeout=30" 
     providerName="System.Data.SqlClient" />
```

### Changing Database Location

To use a different database:
1. Edit App.config
2. Modify the connection string
3. Update `Data Source` and `AttachDbFilename`

## File Formats

### Excel Input Format

The Excel file should contain:
- Header row with column names
- Member names (first name and last name)
- Race times in format `h:mm:ss`
- Reference time row with identifier "TREF"

Example:
```
| ID  | Name           | Time    |
|-----|----------------|---------|
| 1   | TREF           | 0:30:00 |
| 2   | John Smith     | 0:35:00 |
| 3   | Jane Doe       | 0:33:00 |
```

### Members.json Format

```json
[
  {
    "FirstName": "John",
    "LastName": "Smith",
    "Email": "john.smith@example.com"
  },
  {
    "FirstName": "Jane",
    "LastName": "Doe",
    "Email": "jane.doe@example.com"
  }
]
```

## Points Calculation

Points are calculated using the formula:
```
Points = (ReferenceTime / MemberTime) × 1000
```

Where:
- ReferenceTime = Time from TREF row
- MemberTime = Individual member's race time
- Result rounded to nearest integer

Validation:
- Time must be > 10 minutes
- Time must be < 5 hours

## Troubleshooting

### Database Issues

**Problem**: Cannot create database
**Solution**: 
- Ensure SQL Server LocalDB is installed
- Check Windows permissions
- Try running Visual Studio as administrator

**Problem**: Connection string error
**Solution**:
- Verify connection string in App.config
- Check SQL Server LocalDB is running
- Use SQL Server Object Explorer in Visual Studio

### Excel Reading Issues

**Problem**: Cannot read Excel file
**Solution**:
- Install Microsoft Office or Excel runtime
- Ensure file is not open in Excel
- Check file permissions

**Problem**: Members not found in results
**Solution**:
- Verify Members.json exists
- Check name spelling matches
- Ensure diacritics are handled (é, ñ, etc.)

### UI Issues

**Problem**: Process button disabled
**Solution**:
- Ensure all fields are filled
- Select a valid Excel file
- Check race name is not empty

**Problem**: No classifications shown
**Solution**:
- Ensure race is processed (Status = "Processed")
- Click "View Classification" button
- Check database for data

## Advanced Features

### Batch Processing

To process multiple races:
1. Process each race individually
2. Use same year but different race numbers
3. View all races in "View Results" tab

### Export Options

Results can be exported as:
- Plain text (.txt) - formatted report
- CSV (.csv) - spreadsheet compatible
- Both formats include full race details

### Data Management

- **Refresh**: Updates race list from database
- **Delete**: Removes race and all classifications (cascade delete)
- **Status Tracking**: Pending → Processed states

## Best Practices

1. **Unique Race Numbers**: Use sequential numbers per year (1, 2, 3...)
2. **Descriptive Names**: Include location and type (e.g., "Brussels 10K")
3. **Backup**: Regularly backup RaceManagement.mdf file
4. **Members File**: Keep Members.json updated with all participants
5. **File Naming**: Use consistent naming for Excel files

## Security Notes

- Database stored locally (not exposed to network)
- No user authentication (single-user application)
- File permissions follow Windows ACLs
- SQL injection protected by Entity Framework

## Performance

- Local database = fast queries
- Excel reading uses COM = moderate speed
- Async processing = UI remains responsive
- Recommended: < 1000 participants per race

## Updates and Maintenance

### Database Migrations

When updating schema:
1. Backup database file
2. Update model classes
3. Entity Framework recreates database (or use migrations)

### Adding Features

The layered architecture allows easy extensions:
- New UI views → Add to TabControl
- New business logic → Domain layer
- New data storage → Infrastructure layer

## Support

For issues or questions:
1. Check error messages in status bar
2. Review log files (if logging enabled)
3. Check database with SQL Server Object Explorer
4. Review source code documentation

## Version History

**Version 1.0** (Initial Release)
- Upload and process race files
- Database persistence
- View and download results
- Delete races
- Multi-year support

---

**Built with**: WPF, Entity Framework 6, .NET Framework 4.8
**Architecture**: Domain-Driven Design (DDD)
**Pattern**: MVVM (Model-View-ViewModel)
