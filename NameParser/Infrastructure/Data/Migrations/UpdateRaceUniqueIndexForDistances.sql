-- Migration: Update unique index to allow same race name and year with different distances
-- This resolves the issue: "Cannot insert duplicate key row in object 'dbo.Races' with unique index 'IX_Races_Name_Year'"
-- Business requirement: Two races can have the same name and year if they have different distances (e.g., 5km vs 10km)

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- Step 1: Drop the existing unique index on Name and Year
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Races_Name_Year' AND object_id = OBJECT_ID('Races'))
BEGIN
    DROP INDEX IX_Races_Name_Year ON Races;
END;

-- Step 2: Drop the hors challenge unique index (we'll recreate with distance)
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Races_Name_HorsChallenge' AND object_id = OBJECT_ID('Races'))
BEGIN
    DROP INDEX IX_Races_Name_HorsChallenge ON Races;
END;

-- Step 3: Create new unique index including DistanceKm for regular races
-- This allows multiple races with the same name and year as long as distance differs
CREATE UNIQUE NONCLUSTERED INDEX IX_Races_Name_Year_DistanceKm
ON Races (Name, Year, DistanceKm)
WHERE Year IS NOT NULL AND DistanceKm IS NOT NULL;

-- Step 4: Create unique index for hors challenge races including distance
-- This allows multiple hors challenge races with the same name but different distances
CREATE UNIQUE NONCLUSTERED INDEX IX_Races_Name_DistanceKm_HorsChallenge
ON Races (Name, DistanceKm)
WHERE Year IS NULL AND DistanceKm IS NOT NULL;

GO
