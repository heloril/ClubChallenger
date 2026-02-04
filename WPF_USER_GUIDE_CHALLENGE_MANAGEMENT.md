# WPF Challenge Management - User Guide

## 🎉 New Features Added

Your WPF application now includes complete Challenge and Race Event management with two new tabs:
1. **Challenge Management** - Create and manage challenge series
2. **Race Event Management** - Create events and import from Excel

## 📋 Getting Started

### First Time Setup
1. **Launch the application** - The database will auto-create all necessary tables
2. Navigate to the new tabs to start managing challenges and events

## 🏆 Challenge Management Tab

### Creating a Challenge

1. Navigate to **"Challenge Management"** tab
2. Fill in the form:
   - **Name**: e.g., "Challenge 2024", "Trail Series"
   - **Year**: 2024 (or any year)
   - **Start Date** (optional): When the challenge begins
   - **End Date** (optional): When the challenge ends
   - **Description** (optional): Details about the challenge
3. Click **"Create"** button
4. The challenge appears in the "Challenges" list

### Associating Race Events with a Challenge

1. **Select a challenge** from the "Challenges" list
2. In the **"Available Race Events"** section at the bottom:
   - Browse through available events
   - Select an event you want to add
3. Click **"Add to Challenge"**
4. The event appears in **"Associated Race Events"** (top right panel)

### Removing Events from a Challenge

1. Select a challenge from the list
2. In **"Associated Race Events"**, select an event
3. Click **"Remove Event"**

### Editing a Challenge

1. Select a challenge from the list
2. Form fields populate with current values
3. Modify the fields as needed
4. Click **"Update"**

### Deleting a Challenge

1. Select a challenge from the list
2. Click **"Delete"** (red button)
3. Confirm the deletion
4. All associations are removed (events remain intact)

## 🏃 Race Event Management Tab

### Creating a Race Event Manually

1. Navigate to **"Race Event Management"** tab
2. Fill in the form:
   - **Event Name**: e.g., "Paris Marathon", "Trail des Fagnes"
   - **Event Date**: The date when the event takes place
   - **Location** (optional): City or region
   - **Website** (optional): Event website URL
   - **Description** (optional): Additional details
3. Click **"Create"**
4. Event appears in the "Events" list

### Importing Events from Excel

This is the recommended way to import multiple events at once!

#### Excel File Format
Your Excel file should have one of these formats:

**Basic Format (5 columns):**
| Date       | Race Name        | Location | Website           | Description |
|------------|------------------|----------|-------------------|-------------|
| 26/04/2026 | Paris Marathon   | Paris    | www.marathon.fr   | Annual race |
| 15/05/2026 | Trail des Fagnes | Liège    | www.trailfagnes.be| Mountain    |

**Extended Format with Distances (6 columns):**
| Date       | Race Name      | Distance (km) | Location | Website           | Description |
|------------|----------------|---------------|----------|-------------------|-------------|
| 26/04/2026 | Paris Marathon | 42            | Paris    | www.marathon.fr   | Full        |
| 26/04/2026 | Paris Marathon | 21            | Paris    | www.marathon.fr   | Half        |
| 26/04/2026 | Paris Marathon | 10            | Paris    | www.marathon.fr   | 10K         |

#### Import Steps

1. Prepare your Excel file (see formats above)
2. Save it as `.xlsx` format
3. In the **"Import from Excel"** section:
   - Click **"Browse..."**
   - Select your Excel file
   - Click **"Import"**
4. Review the import summary showing:
   - Number of events imported
   - Number of events skipped (duplicates)
5. Events appear in the "Events" list

#### Using the Sample File

A sample file is included: `NameParser\Challenge\Challenge Lucien 26.xlsx`

### Viewing Event Details

1. Select an event from the **"Events"** list
2. View associated information in side panels:
   - **Linked Challenges**: Shows which challenges include this event
   - **Race Distances**: Shows processed race results at different distances

### Editing an Event

1. Select an event from the list
2. Form populates with current values
3. Modify fields as needed
4. Click **"Update"**

### Deleting an Event

1. Select an event from the list
2. Click **"Delete"** (red button)
3. Confirm deletion
4. **Note**: 
   - Associations with challenges are removed
   - Race distance links are set to null (races remain)

## 🏁 Processing Races with Event Association

Now when you process race results, you can link them to events!

### Enhanced Race Processing

1. Go to **"Upload and Process"** tab (first tab)
2. Upload your race file (Excel/PDF)
3. Fill in race details
4. **NEW**: Select a **"Race Event"** from the dropdown
   - This is optional
   - Shows event name and date
   - Links your race result to the event
5. Click **"Process Race"**

### Benefits of Linking

- View all race distances for an event in one place
- Track which challenges include the event
- Better organization and reporting
- Foundation for future features (event calendars, statistics)

## 💡 Typical Workflow

### Setup Phase (Once)

1. **Import Race Events**
   - Use Excel import with `Challenge Lucien 26.xlsx` format
   - Import all planned events for the year

2. **Create Challenges**
   - Create challenge for the year (e.g., "Challenge 2024")
   - Associate relevant events with the challenge

### Ongoing Operations (Throughout the Year)

3. **Process Race Results**
   - After each race, upload the results file
   - Select the corresponding race event
   - Process as usual
   - Results are now linked to the event

4. **View Progress**
   - Check race event details to see processed distances
   - View challenge progress with associated events
   - Use existing classification features

## 🔍 Understanding the Data Model

```
Challenge (e.g., "Challenge 2024")
    ↓ (can have many)
ChallengeRaceEvent (association)
    ↓ (links to)
RaceEvent (e.g., "Paris Marathon - 26/04/2024")
    ↓ (can have many)
Race/RaceDistance (e.g., "42 km")
    ↓ (has many)
RaceResult (individual participant results)
```

### Key Concepts

- **Challenge**: A competition series grouping multiple events (e.g., yearly challenge)
- **Race Event**: A specific racing occasion (e.g., Paris Marathon 2024)
- **Race Distance**: A distance category within an event (e.g., 42 km, 21 km, 10 km)
- **Race Result**: Individual participant results for a distance

### Many-to-Many Relationships

- One event can belong to multiple challenges
- One challenge can have multiple events
- One event can have multiple distances
- Each distance can have multiple results

## 🎯 Use Cases

### Use Case 1: Annual Club Challenge

1. Create challenge: "RCAR Challenge 2024"
2. Import all planned events from Excel
3. Associate 10 events with the challenge
4. Throughout the year, process race results
5. Track member participation across all events

### Use Case 2: Trail Running Series

1. Create challenge: "Trail Series 2024"
2. Manually create 5 trail events
3. Associate only trail events with this challenge
4. Create separate challenge for road races
5. Some events might belong to both challenges

### Use Case 3: Event Management

1. Import complete race calendar from Excel
2. Create events even before results are available
3. When results come in, select the event from dropdown
4. Build up historical database of events and results

## 🚨 Important Notes

### Data Safety

- **Deleting a challenge** removes associations but keeps events and results
- **Deleting an event** sets race links to null but keeps the races
- **Deleting a race** removes associated results (use with caution)
- Always back up your database before major changes

### Database Location

- Database file: `RaceManagementDb.mdf`
- Usually located in: `%LOCALAPPDATA%\Microsoft\Microsoft SQL Server Local DB\Instances\MSSQLLocalDB`
- Or check the app.config for connection string

### Performance Tips

- Import events in bulk using Excel (faster than manual entry)
- Create challenges before the season starts
- Process races regularly throughout the year
- Use the existing classification tabs for analysis

## 🔧 Troubleshooting

### "Challenge already exists" error
- Check if a challenge with same name and year exists
- Update the existing one instead of creating new

### "Event import failed"
- Verify Excel format matches expected columns
- Check date format in Excel (should be recognizable dates)
- Ensure no blank rows in the middle of data

### "Race event dropdown is empty"
- Click "Browse" button to refresh the list
- Check Race Event Management tab to ensure events exist
- Restart application if needed

### Database errors on startup
- Application will try to create missing tables automatically
- Check that you have SQL Server LocalDB installed
- Review logs for specific error messages

## 📊 Next Steps

After setting up challenges and events:

1. **Use existing features** for race processing
2. **View classifications** in General/Challenger tabs
3. **Export results** for email/Facebook sharing
4. **Track progress** throughout the season

## 🆘 Getting Help

If you encounter issues:

1. Check this guide for common scenarios
2. Review the implementation documentation
3. Check database connection in app.config
4. Review application logs for errors

---

**Enjoy managing your challenges and events! 🏆🏃‍♂️🎉**
