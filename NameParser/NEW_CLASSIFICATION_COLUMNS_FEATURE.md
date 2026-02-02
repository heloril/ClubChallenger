# New Classification Columns Feature

## Overview
Added support for extracting and displaying additional classification data from PDF race results:
- Sex (Gender): M or F
- Position by Sex: Ranking within gender
- Age Category: Various age/category classifications
- Position by Category: Ranking within age category

Also fixed a speed parsing issue where speeds like "15.00" were being interpreted as "1500".

## New Columns

### 1. Sex (Sexe)
**Column Headers Recognized:**
- `sexe`
- `sex`
- `s.`

**Values:**
- `M` - Male (Masculin, Homme, H)
- `F` - Female (Féminin, Dame, D)

**Extraction Logic:**
- Case-insensitive matching
- Accepts variations: m, h, mas, hom → M
- Accepts variations: f, d, fem, dam → F

### 2. Position by Sex (Pl./S., Clas.Sexe)
**Column Headers Recognized:**
- `pl./s.`
- `clas.sexe`
- `pl. sexe`
- `pos.sexe`
- `classement sexe`

**Values:** Integer representing rank within gender

### 3. Age Category (Cat, Catég.)
**Column Headers Recognized:**
- `cat.`
- `cat `
- `catég.`
- `catégorie`
- `category`

**Common Categories:**
- **Senior:** SH (Senior Homme), SD (Senior Dame)
- **Veterans:** V1, V2, V3 (Veteran 1, 2, 3)
- **Youth:** ESF, A1, A2, A3 (Ainée/Espoir)
- **Long form:** Senior H, Veteran 1, Veteran 2, Ainée 1, Ainée 2, Senior D, Esp G

**Validation:**
- 1-20 characters
- Letters, numbers, spaces, hyphens allowed
- Filters out obvious non-category data

### 4. Position by Category (Clas. Cat, Pl./C.)
**Column Headers Recognized:**
- `pl./c.`
- `clas. cat`
- `pl. cat`
- `pos.cat`
- `classement cat`

**Values:** Integer representing rank within age category

## Speed Fix

### Problem
Speed values were being parsed incorrectly:
- Expected: `15.00 km/h`
- Got: `1500 km/h`

### Root Cause
PDF extraction sometimes returns speed values without decimal separators (e.g., "1500" instead of "15.00")

### Solution
Enhanced `ParseSpeed()` method with automatic decimal correction:
```csharp
// Check if the value is too large (likely missing decimal point)
// For example, 1500 should be 15.00
if (speed > 100 && speed < 10000)
{
    // Likely missing decimal point - divide by 100
    speed = speed / 100.0;
}
```

**Range Validation:**
- Valid range: 1.0 - 30.0 km/h
- Values outside this range are rejected
- Debug logging shows adjustments made

## Database Changes

### New Columns in Classifications Table

```sql
Sex                  NVARCHAR(1)    NULL   -- M or F
PositionBySex        INT            NULL   -- Rank within gender
AgeCategory          NVARCHAR(50)   NULL   -- Age category name
PositionByCategory   INT            NULL   -- Rank within category
```

### Indexes Created

For performance optimization:
```sql
IX_Classifications_Sex_PositionBySex
IX_Classifications_AgeCategory_PositionByCategory
```

### Migration File
`Infrastructure\Data\Migrations\AddCategoryColumns.sql`

**To Apply Migration:**
1. Run the SQL script against your database
2. Script is idempotent (safe to run multiple times)
3. Includes verification queries

## Code Changes

### Files Modified

#### 1. Data Models
- `ClassificationEntity.cs` - Added 4 new properties
- `MemberClassification.cs` - Added 4 new properties + UpdateCategoryInfo method

#### 2. Repositories
- `PdfRaceResultRepository.cs`:
  - Updated `ParsedPdfResult` class
  - Enhanced `DetectColumnPositions` with new column mappings
  - Added extraction logic in `ParseLineUsingColumns`
  - Fixed `ParseSpeed` method
  - Updated `ToDelimitedString` to include new fields
  
- `ClassificationRepository.cs`:
  - Updated `SaveClassifications` to save new fields

#### 3. Domain
- `Classification.cs`:
  - Added overload for `AddOrUpdateResult` with new parameters
  - Added `UpdateCategoryInfo` method to MemberClassification

#### 4. Services
- `RaceProcessingService.cs`:
  - Updated `ParsedRaceResult` class
  - Added extraction of SEX, POSITIONSEX, CATEGORY, POSITIONCAT
  - Updated classification call to include new fields
  - Enhanced logging

#### 5. UI
- `MainWindow.xaml`:
  - Added 4 new DataGrid columns:
    - Sex (50px width)
    - Pos/Sex (70px width)
    - Category (80px width)
    - Pos/Cat (70px width)
  - Positioned after Last Name, before Team

## Data Flow

### 1. PDF Extraction
```
PDF File
  ↓
ExtractTextFromPdf()
  ↓
Header Detection (DetectColumnPositions)
  ↓
Column-based Extraction (ParseLineUsingColumns)
  ↓
ParsedPdfResult (with Sex, PositionBySex, Category, PositionByCategory)
```

### 2. Data Processing
```
ParsedPdfResult
  ↓
ToDelimitedString() (adds SEX;M;POSITIONSEX;5;CATEGORY;V1;POSITIONCAT;2)
  ↓
RaceProcessingService.ParseRaceResult()
  ↓
Classification.AddOrUpdateResult()
  ↓
ClassificationRepository.SaveClassifications()
  ↓
Database (Classifications table)
```

### 3. UI Display
```
Database
  ↓
ClassificationRepository.GetClassificationsByRace()
  ↓
MainViewModel.Classifications (ObservableCollection)
  ↓
DataGrid Display (with new columns)
```

## UI Display

### Race Classification Tab

**New Columns Order:**
```
Rank | Position | First Name | Last Name | Sex | Pos/Sex | Category | Pos/Cat | Team | Points | ...
```

**Example Display:**
| Rank | Pos | First | Last | Sex | Pos/Sex | Cat | Pos/Cat | Team | Points |
|------|-----|-------|------|-----|---------|-----|---------|------|--------|
| 1 | 1 | John | Doe | M | 1 | SH | 1 | AC Brussels | 354 |
| 2 | 2 | Jane | Smith | F | 1 | SD | 1 | RC Liège | 340 |
| 3 | 5 | Pierre | Martin | M | 4 | V1 | 1 | AC Hannut | 320 |

## Export Support

### Email Exports
Both single and multiple race exports now include the new columns in HTML and Text formats.

**HTML Format:**
- New columns automatically included in table
- Proper formatting and styling applied

**Text Format:**
- Columns included with appropriate width
- Aligned with other data

**Note:** Export templates automatically detect and include available columns.

## Testing

### Test Scenarios

#### 1. PDF with All Columns
```
Pl. | Dos | Nom          | Sexe | Pl./S. | Cat. | Pl./C. | Club      | Temps    | Vitesse | min/km
1   | 123 | DUPONT Jean  | M    | 1      | SH   | 1      | AC Bxl    | 00:35:25 | 16.95   | 3:32
```
**Expected:** All fields extracted correctly

#### 2. PDF with Missing Columns
```
Pl. | Nom          | Temps    | Club
1   | DUPONT Jean  | 00:35:25 | AC Bxl
```
**Expected:** Missing columns are NULL, no errors

#### 3. Speed Correction
```
Input: "1695" (missing decimal)
Expected: 16.95 km/h
```

#### 4. Category Variations
Test all category formats:
- Short: SH, SD, V1, V2, V3, A1, A2
- Long: Senior H, Veteran 1, Ainée 1
- Special: ESF, Esp G

### Debug Logging

Enable debug output to see:
- Column detection results
- Parsing success/failures
- Speed adjustments
- Category extraction

**Example Output:**
```
Detected 11 columns:
  position: position 0
  name: position 11
  sex: position 50
  positionsex: position 55
  category: position 65
  positioncat: position 75
  team: position 85
  speed: position 120
  time: position 135
  pace: position 150

Position 1: Extracted Sex: M, PositionBySex: 1, Category: SH, PositionByCategory: 1
Position 2: Extracted Sex: F, PositionBySex: 1, Category: SD, PositionByCategory: 1
Speed adjusted from 1695 to 16.95 km/h. Original: '1695'
```

## Database Queries

### Get Winners by Gender
```sql
SELECT 
    MemberFirstName,
    MemberLastName,
    Sex,
    PositionBySex,
    Points
FROM Classifications
WHERE RaceId = @RaceId 
  AND PositionBySex = 1
ORDER BY Sex;
```

### Get Category Leaders
```sql
SELECT 
    AgeCategory,
    MemberFirstName,
    MemberLastName,
    PositionByCategory,
    Points
FROM Classifications
WHERE RaceId = @RaceId 
  AND PositionByCategory = 1
ORDER BY AgeCategory;
```

### Category Distribution
```sql
SELECT 
    AgeCategory,
    Sex,
    COUNT(*) AS Count,
    AVG(Speed) AS AvgSpeed
FROM Classifications
WHERE RaceId = @RaceId
GROUP BY AgeCategory, Sex
ORDER BY AgeCategory, Sex;
```

## Known Limitations

### 1. Column Detection
- Depends on consistent header formatting
- Multiple header variations supported but not all possible formats
- Some PDFs may need manual mapping additions

### 2. Category Names
- No standardization across races
- Different organizations use different codes
- System stores as-is, no normalization

### 3. Speed Correction
- Assumes missing decimal means divide by 100
- Works for typical running speeds (10-20 km/h)
- May not work for unusual formats

### 4. Backward Compatibility
- Old race data won't have these fields (NULL values)
- Existing functionality unaffected
- New races will automatically extract when available

## Future Enhancements

### 1. Category Normalization
- Map variations to standard categories
- Configurable category aliases
- Age range validation

### 2. Gender Statistics
- Automatic gender distribution charts
- Comparison reports
- Performance analysis by gender

### 3. Category Analysis
- Age category performance trends
- Category recommendations
- Historical category data

### 4. Export Options
- Filter by gender
- Filter by category
- Category-specific rankings

### 5. UI Improvements
- Sortable by new columns
- Filter by gender/category
- Visual indicators (icons)
- Color coding by category

## Troubleshooting

### Issue: Columns not extracted
**Solution:** 
1. Check Debug output for column detection
2. Verify PDF header matches expected keywords
3. Add new keyword mapping if needed

### Issue: Speed showing as 1500
**Solution:**
- Fixed in latest version
- Speed will be auto-corrected to 15.00
- Check Debug output for "Speed adjusted" messages

### Issue: Category not recognized
**Solution:**
- Check if category format is unusual
- Add new category pattern to extraction logic
- Categories are stored as-is (no validation)

### Issue: Missing data in old races
**Solution:**
- Expected behavior - old races won't have new data
- Re-process old PDFs to extract new fields
- NULL values are handled gracefully

## Migration Guide

### Applying Database Changes

1. **Backup Database**
```sql
BACKUP DATABASE [RaceManagement] 
TO DISK = 'C:\Backups\RaceManagement_BeforeMigration.bak';
```

2. **Run Migration Script**
```sql
-- From SQL Server Management Studio
-- Open: Infrastructure\Data\Migrations\AddCategoryColumns.sql
-- Execute against your database
```

3. **Verify Changes**
```sql
-- Check columns exist
SELECT 
    c.name AS ColumnName,
    t.name AS DataType,
    c.max_length AS MaxLength,
    c.is_nullable AS IsNullable
FROM sys.columns c
INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID(N'[dbo].[Classifications]')
AND c.name IN ('Sex', 'PositionBySex', 'AgeCategory', 'PositionByCategory');
```

4. **Test with Sample Data**
- Process a new race with the updated code
- Verify new columns are populated
- Check UI displays correctly

### Rolling Back (if needed)

```sql
-- Remove columns (WARNING: Data loss!)
ALTER TABLE [dbo].[Classifications] DROP COLUMN Sex;
ALTER TABLE [dbo].[Classifications] DROP COLUMN PositionBySex;
ALTER TABLE [dbo].[Classifications] DROP COLUMN AgeCategory;
ALTER TABLE [dbo].[Classifications] DROP COLUMN PositionByCategory;

-- Remove indexes
DROP INDEX IX_Classifications_Sex_PositionBySex ON [dbo].[Classifications];
DROP INDEX IX_Classifications_AgeCategory_PositionByCategory ON [dbo].[Classifications];
```

## Summary

### ✅ Implemented
- Sex column (M/F)
- Position by Sex
- Age Category (multiple formats)
- Position by Category
- Speed parsing fix (1500 → 15.00)
- Database migration
- UI columns
- Export support

### 🎯 Benefits
- More detailed race classifications
- Gender-specific rankings
- Age category analysis
- Accurate speed data
- Professional race reports
- Better data for analytics

### 📊 Impact
- **Database:** 4 new columns + 2 indexes
- **UI:** 4 new visible columns
- **Processing:** Minimal performance impact
- **Backward Compatible:** Yes (NULL for old data)

---

**Version**: 1.0  
**Date**: 2026-02-01  
**Status**: ✅ Production Ready  
**Build**: ✅ Successful
