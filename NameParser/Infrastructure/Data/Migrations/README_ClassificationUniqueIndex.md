# Classification Duplicate Names Support

## Overview
This migration adds support for duplicate member first name and last name combinations in the same race result. This is useful when multiple people with the same name participate in the same race.

## Changes Made

### 1. Database Schema Change
A new unique index has been added to the `Classifications` table:

**Index:** `IX_Classifications_RaceId_MemberFirstName_MemberLastName_Position`

**Columns:** 
- `RaceId`
- `MemberFirstName`
- `MemberLastName`
- `Position`

**Filter:** `WHERE [Position] IS NOT NULL`

### 2. Why This Approach?

#### Before (No Unique Index)
- Any number of duplicates allowed
- No protection against accidental duplicate imports
- Hard to identify genuine duplicates vs. legitimate same-name participants

#### After (Unique Index with Position)
- **Allows:** Same name multiple times in one race (with different positions)
  - Example: "John Smith" at position 5 and "John Smith" at position 12
- **Prevents:** Exact duplicates at the same position
  - Example: Two "John Smith" entries both at position 5
- **Benefits:** 
  - You can import race results with duplicate names
  - Protection against accidental re-imports
  - You can manually clean up after import based on position

### 3. Files Modified

#### `NameParser\Infrastructure\Data\RaceManagementContext.cs`
- Added unique index configuration in `OnModelCreating` method
- EF Core will use this configuration for future migrations

#### `NameParser\Infrastructure\Data\DatabaseInitializer.cs`
- Added SQL migration to create the unique index
- Runs automatically on application startup

#### New Files Created

1. **`AddClassificationUniqueIndex.sql`**
   - SQL script to create the unique index
   - Can be run manually if needed

2. **`ApplyClassificationUniqueIndex.ps1`**
   - PowerShell script to apply the migration
   - Useful for running the migration separately

## How to Apply the Migration

### Option 1: Automatic (Recommended)
The migration will apply automatically the next time you run the application. The `DatabaseInitializer` will execute the SQL during startup.

### Option 2: Manual PowerShell Script
```powershell
cd NameParser\Infrastructure\Data\Migrations
.\ApplyClassificationUniqueIndex.ps1
```

### Option 3: Manual SQL
1. Open SQL Server Management Studio or Visual Studio SQL Server Object Explorer
2. Connect to `(LocalDB)\MSSQLLocalDB`
3. Select the `RaceManagementDb` database
4. Execute the contents of `AddClassificationUniqueIndex.sql`

## Usage Example

### Scenario: Race with Duplicate Names

Imagine you have a race result file with:
```
Position | First Name | Last Name
---------|------------|----------
5        | John       | Smith
12       | John       | Smith
25       | John       | Smith
```

### Before This Change
- All three would be saved if there was no unique constraint
- OR, if there was a unique constraint on (RaceId, FirstName, LastName), only the first one would be saved and the rest would fail

### After This Change
- ✅ All three entries will be saved successfully (different positions)
- ✅ If you try to import the same file twice, the second import will fail with a unique constraint violation (same position)
- ✅ You can manually review and clean up the duplicates based on position after import

## Manual Cleanup After Import

After importing race results with duplicate names, you can:

1. Query duplicates:
```sql
SELECT RaceId, MemberFirstName, MemberLastName, Position, COUNT(*) as Count
FROM Classifications
GROUP BY RaceId, MemberFirstName, MemberLastName, Position
HAVING COUNT(*) > 1
```

2. Review the results in the UI
3. Delete incorrect entries based on position using the application's delete functionality

## Technical Notes

- The unique index uses a **filtered index** with `WHERE [Position] IS NOT NULL`
- This allows entries with NULL positions (if any exist) to not conflict with the unique constraint
- The index is **non-clustered** for better performance on lookups without affecting insert performance
- The constraint is enforced at the database level, providing data integrity protection

## Rollback (If Needed)

If you need to remove this constraint, run:

```sql
DROP INDEX [IX_Classifications_RaceId_MemberFirstName_MemberLastName_Position] 
ON [dbo].[Classifications];
```

## Impact on Existing Data

- **No impact on existing data**: The index is created with the current data
- If you have existing duplicates with the same position, the migration will fail
- In that case, clean up the duplicates first before applying the migration
