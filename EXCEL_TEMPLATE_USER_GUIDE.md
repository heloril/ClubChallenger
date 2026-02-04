# Excel Template Feature - User Guide

## Overview
The Race Event Management tab now includes an "Export Template" feature that generates a pre-formatted Excel file to help you import race events quickly and correctly.

## How to Use

### Step 1: Export the Template
1. Open the application
2. Navigate to the **Race Event Management** tab
3. Locate the **"Import from Excel"** section
4. Click the **"📄 Export Template"** button (orange button with document icon)
5. Choose where to save the template file
6. The template will be saved with a timestamp (e.g., `RaceEvent_Import_Template_20241215.xlsx`)
7. Click **"Yes"** when asked if you want to open the file

### Step 2: Fill in Your Race Events
The template includes:

#### Header Row (Blue background)
- **Date**: Event date in dd/MM/yyyy format
- **Race Name**: Name of the race event (Required)
- **Distance (km)**: Race distance as a decimal number (Required)
- **Location**: Where the race takes place (Optional)
- **Website**: Full URL to the race website (Optional)
- **Description**: Additional information about the race (Optional)

#### Instruction Row (Gray background)
Shows the expected format for each column

#### Example Rows (3 sample events)
- Shows how to create a race event with multiple distances
- All three examples are for "Marathon de Paris" with different distances (10km, 21.1km, 42.195km)
- **Important**: Delete these example rows before importing your data!

#### Notes Section
- Explains key rules and best practices
- Located below the example data

### Step 3: Enter Your Data

**Single Distance Event Example:**
```
15/04/2024 | City Marathon | 42.195 | New York | https://nycmarathon.com | Annual marathon
```

**Multiple Distance Event Example:**
```
15/04/2024 | City Marathon | 10     | New York | https://nycmarathon.com | 10K race
15/04/2024 | City Marathon | 21.1   | New York | https://nycmarathon.com | Half marathon
15/04/2024 | City Marathon | 42.195 | New York | https://nycmarathon.com | Full marathon
```

The system will automatically group rows with the same name and date into a single race event with multiple distances.

### Step 4: Import Your Data
1. Save your Excel file after filling in your race events
2. Return to the Race Event Management tab
3. Click **"Browse..."** and select your filled template
4. Click **"Import"**
5. Review the import summary showing:
   - Number of events imported
   - Number of distances added
   - Any skipped rows (with reasons)

## Important Rules

### Date Format
- **Must be**: dd/MM/yyyy
- **Example**: 15/04/2024 (April 15, 2024)
- **Not**: 04/15/2024 or 2024-04-15

### Race Name
- **Required field**
- Will be used as the event name in the system
- Multiple rows with the same name and date = one event with multiple distances

### Distance
- **Required field**
- Can be a decimal number (10, 21.1, 42.195, etc.)
- In kilometers

### Optional Fields
- Location, Website, and Description are optional
- Can be left blank if not available

### Multiple Distances
To create an event with multiple distances:
1. Use the **same Race Name** for all rows
2. Use the **same Date** for all rows
3. Use **different Distance** values
4. Each distance will be available for uploading results

## Template Features

### Visual Design
- **Blue header row**: Easy to identify column names
- **Gray instruction row**: Format hints for each field
- **Example data**: Shows correct formatting
- **Professional styling**: Borders, fonts, and colors

### Built-in Help
- Detailed notes section explaining:
  - How multiple distances work
  - Required vs optional fields
  - Date format requirements
  - Reminder to delete examples

### Column Widths
Pre-sized columns to fit typical data:
- Date: 15 characters
- Race Name: 30 characters
- Distance: 18 characters
- Location: 25 characters
- Website: 40 characters
- Description: 40 characters

## Tips & Best Practices

### 1. Start with the Template
Don't create your own Excel file from scratch. Always start with the exported template to ensure correct formatting.

### 2. Delete Example Data
Remember to delete rows 3-5 (the example Marathon de Paris entries) before adding your own data.

### 3. Copy-Paste from Other Sources
You can copy event information from other sources (websites, calendars) and paste into the template, but verify the date format.

### 4. One Row Per Distance
Each distance needs its own row, even if it's the same event.

### 5. Save Your Work
Keep your filled template as a backup. You can add more events to it later and re-import.

### 6. Test with One Event First
If you're new to the system, try importing a single event first to understand the process.

## Troubleshooting

### Import Shows "Skipped" Events
**Possible causes:**
- Date is not in dd/MM/yyyy format
- Race Name is empty
- Distance is not a valid number
- Event with same name and date already exists

**Solution:**
- Check the import summary message for details
- Fix the errors in your Excel file
- Re-import

### Template Won't Open After Export
**Possible cause:**
- Excel is not installed
- File association not set

**Solution:**
- Manually open Excel first
- Use File > Open to open the template
- Or open the file from Windows Explorer

### Excel Warns About File Format
**Cause:**
- EPPlus generates .xlsx files which are sometimes flagged

**Solution:**
- Click "Yes" to open the file
- The file is safe - it's generated by your application

## Bonus: Race Results Template

The template service also includes a method to generate race results templates for a specific distance. This feature might be added to the UI in a future update.

Example usage (programmatically):
```csharp
var templateService = new ExcelTemplateService();
templateService.GenerateRaceResultsTemplate("10km_results.xlsx", 10);
```

This creates a template specifically for entering race results (Position, Name, Time, etc.)

## Video Tutorial
[Future: Add link to video demonstration]

## Need Help?
- Check the Notes section in the exported template
- Review the example rows for formatting guidance
- Contact support if issues persist
