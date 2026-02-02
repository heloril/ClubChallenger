-- Migration: Add Team, Speed and IsMember columns to Classifications table
-- Date: Generated automatically
-- Description: Adds Team (équipe), Speed (vitesse) and IsMember flag columns

-- Add Team column
IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE object_id = OBJECT_ID(N'[dbo].[Classifications]') 
               AND name = 'Team')
BEGIN
    ALTER TABLE [dbo].[Classifications]
    ADD [Team] NVARCHAR(200) NULL;
    
    PRINT 'Team column added successfully to Classifications table';
END
ELSE
BEGIN
    PRINT 'Team column already exists in Classifications table';
END

-- Add Speed column
IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE object_id = OBJECT_ID(N'[dbo].[Classifications]') 
               AND name = 'Speed')
BEGIN
    ALTER TABLE [dbo].[Classifications]
    ADD [Speed] FLOAT NULL;
    
    PRINT 'Speed column added successfully to Classifications table';
END
ELSE
BEGIN
    PRINT 'Speed column already exists in Classifications table';
END

-- Add IsMember column
IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE object_id = OBJECT_ID(N'[dbo].[Classifications]') 
               AND name = 'IsMember')
BEGIN
    ALTER TABLE [dbo].[Classifications]
    ADD [IsMember] BIT NOT NULL DEFAULT 1;
    
    PRINT 'IsMember column added successfully to Classifications table';
END
ELSE
BEGIN
    PRINT 'IsMember column already exists in Classifications table';
END

-- Optional: Create indexes for performance
-- CREATE INDEX IX_Classifications_Team ON Classifications(Team);
-- CREATE INDEX IX_Classifications_IsMember ON Classifications(IsMember);

PRINT 'Migration completed successfully';
