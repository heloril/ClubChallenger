-- Migration: Add TimePerKm column to Classifications table
-- Date: Generated automatically
-- Description: Adds TimePerKm column to store time per kilometer for race results

-- Add TimePerKm column to Classifications table
IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE object_id = OBJECT_ID(N'[dbo].[Classifications]') 
               AND name = 'TimePerKm')
BEGIN
    ALTER TABLE [dbo].[Classifications]
    ADD [TimePerKm] TIME NULL;
    
    PRINT 'TimePerKm column added successfully to Classifications table';
END
ELSE
BEGIN
    PRINT 'TimePerKm column already exists in Classifications table';
END

-- Optional: Add indexes for performance if needed
-- CREATE INDEX IX_Classifications_RaceTime ON Classifications(RaceTime);
-- CREATE INDEX IX_Classifications_TimePerKm ON Classifications(TimePerKm);

PRINT 'Migration completed successfully';
