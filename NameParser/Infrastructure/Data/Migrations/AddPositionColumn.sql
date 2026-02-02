-- Migration: Add Position column to Classifications table
-- Date: Generated automatically
-- Description: Adds Position column to store the finishing position in the race

-- Add Position column to Classifications table
IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE object_id = OBJECT_ID(N'[dbo].[Classifications]') 
               AND name = 'Position')
BEGIN
    ALTER TABLE [dbo].[Classifications]
    ADD [Position] INT NULL;
    
    PRINT 'Position column added successfully to Classifications table';
END
ELSE
BEGIN
    PRINT 'Position column already exists in Classifications table';
END

-- Optional: Add index for performance when querying by position
-- CREATE INDEX IX_Classifications_Position ON Classifications(Position);

PRINT 'Migration completed successfully';
