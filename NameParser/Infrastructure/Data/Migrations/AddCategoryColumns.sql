-- Migration: Add Sex, PositionBySex, AgeCategory and PositionByCategory columns to Classifications table
-- Date: 2026-02-01
-- Description: Adds columns for gender classification, age category and their respective positions

-- Add Sex column (M/F)
IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE object_id = OBJECT_ID(N'[dbo].[Classifications]') 
               AND name = 'Sex')
BEGIN
    ALTER TABLE [dbo].[Classifications]
    ADD [Sex] NVARCHAR(1) NULL;
    
    PRINT 'Sex column added successfully to Classifications table';
END
ELSE
BEGIN
    PRINT 'Sex column already exists in Classifications table';
END

-- Add PositionBySex column
IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE object_id = OBJECT_ID(N'[dbo].[Classifications]') 
               AND name = 'PositionBySex')
BEGIN
    ALTER TABLE [dbo].[Classifications]
    ADD [PositionBySex] INT NULL;
    
    PRINT 'PositionBySex column added successfully to Classifications table';
END
ELSE
BEGIN
    PRINT 'PositionBySex column already exists in Classifications table';
END

-- Add AgeCategory column
IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE object_id = OBJECT_ID(N'[dbo].[Classifications]') 
               AND name = 'AgeCategory')
BEGIN
    ALTER TABLE [dbo].[Classifications]
    ADD [AgeCategory] NVARCHAR(50) NULL;
    
    PRINT 'AgeCategory column added successfully to Classifications table';
END
ELSE
BEGIN
    PRINT 'AgeCategory column already exists in Classifications table';
END

-- Add PositionByCategory column
IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE object_id = OBJECT_ID(N'[dbo].[Classifications]') 
               AND name = 'PositionByCategory')
BEGIN
    ALTER TABLE [dbo].[Classifications]
    ADD [PositionByCategory] INT NULL;
    
    PRINT 'PositionByCategory column added successfully to Classifications table';
END
ELSE
BEGIN
    PRINT 'PositionByCategory column already exists in Classifications table';
END

-- Optional: Create indexes for performance on category searches
IF NOT EXISTS (SELECT * FROM sys.indexes 
               WHERE name = 'IX_Classifications_Sex_PositionBySex' 
               AND object_id = OBJECT_ID(N'[dbo].[Classifications]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Classifications_Sex_PositionBySex]
    ON [dbo].[Classifications] ([Sex] ASC, [PositionBySex] ASC);
    
    PRINT 'Index IX_Classifications_Sex_PositionBySex created successfully';
END
ELSE
BEGIN
    PRINT 'Index IX_Classifications_Sex_PositionBySex already exists';
END

IF NOT EXISTS (SELECT * FROM sys.indexes 
               WHERE name = 'IX_Classifications_AgeCategory_PositionByCategory' 
               AND object_id = OBJECT_ID(N'[dbo].[Classifications]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Classifications_AgeCategory_PositionByCategory]
    ON [dbo].[Classifications] ([AgeCategory] ASC, [PositionByCategory] ASC);
    
    PRINT 'Index IX_Classifications_AgeCategory_PositionByCategory created successfully';
END
ELSE
BEGIN
    PRINT 'Index IX_Classifications_AgeCategory_PositionByCategory already exists';
END

GO

-- Verification query
SELECT 
    c.name AS ColumnName,
    t.name AS DataType,
    c.max_length AS MaxLength,
    c.is_nullable AS IsNullable
FROM sys.columns c
INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID(N'[dbo].[Classifications]')
AND c.name IN ('Sex', 'PositionBySex', 'AgeCategory', 'PositionByCategory')
ORDER BY c.column_id;
