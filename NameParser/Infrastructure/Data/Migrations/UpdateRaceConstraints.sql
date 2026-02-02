-- Migration: Make Year nullable and update uniqueness constraint
-- This allows races "hors challenge" (without a specific year/date)
-- and multiple distances per race number/year (only name must be unique per year)

-- Step 1: Make Year column nullable
ALTER TABLE Races
ALTER COLUMN Year INT NULL;

-- Step 2: Add a constraint to ensure Name is unique per Year
-- (allowing NULL years for hors challenge races)
-- First, check if there's an existing constraint and drop it
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Races_Name_Year' AND object_id = OBJECT_ID('Races'))
BEGIN
    DROP INDEX IX_Races_Name_Year ON Races;
END;

-- Create unique index on Name and Year (allowing multiple distances)
CREATE UNIQUE NONCLUSTERED INDEX IX_Races_Name_Year
ON Races (Name, Year)
WHERE Year IS NOT NULL;

-- For hors challenge races (Year IS NULL), ensure Name is unique
CREATE UNIQUE NONCLUSTERED INDEX IX_Races_Name_HorsChallenge
ON Races (Name)
WHERE Year IS NULL;

-- Step 3: Add IsHorsChallenge flag for clarity (optional but recommended)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Races') AND name = 'IsHorsChallenge')
BEGIN
    ALTER TABLE Races
    ADD IsHorsChallenge BIT NOT NULL DEFAULT 0;
END;
GO
