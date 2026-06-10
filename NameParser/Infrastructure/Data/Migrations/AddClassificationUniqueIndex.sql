-- Migration: Add unique index on RaceId, MemberFirstName, MemberLastName, and Position
-- This allows the same person name to appear multiple times in a race result (with different positions)
-- but prevents accidental exact duplicates at the same position

-- Drop existing index if it exists (in case of re-run)
IF EXISTS (
	SELECT * FROM sys.indexes 
	WHERE name = 'IX_Classifications_RaceId_MemberFirstName_MemberLastName_Position' 
	AND object_id = OBJECT_ID(N'[dbo].[Classifications]')
)
BEGIN
	DROP INDEX [IX_Classifications_RaceId_MemberFirstName_MemberLastName_Position] ON [dbo].[Classifications];
	PRINT 'Existing index IX_Classifications_RaceId_MemberFirstName_MemberLastName_Position dropped';
END

-- Create unique index including Position to allow duplicates with different positions
IF NOT EXISTS (
	SELECT * FROM sys.indexes 
	WHERE name = 'IX_Classifications_RaceId_MemberFirstName_MemberLastName_Position' 
	AND object_id = OBJECT_ID(N'[dbo].[Classifications]')
)
BEGIN
	-- Use filtered index to handle NULL positions
	CREATE UNIQUE NONCLUSTERED INDEX [IX_Classifications_RaceId_MemberFirstName_MemberLastName_Position]
	ON [dbo].[Classifications] ([RaceId], [MemberFirstName], [MemberLastName], [Position])
	WHERE [Position] IS NOT NULL;
	PRINT 'Unique index IX_Classifications_RaceId_MemberFirstName_MemberLastName_Position created successfully';
END
ELSE
BEGIN
	PRINT 'Index IX_Classifications_RaceId_MemberFirstName_MemberLastName_Position already exists';
END

-- Verify the index was created
SELECT 
	i.name AS IndexName,
	i.is_unique AS IsUnique,
	i.has_filter AS HasFilter,
	i.filter_definition AS FilterDefinition,
	COL_NAME(ic.object_id, ic.column_id) AS ColumnName,
	ic.key_ordinal AS ColumnOrder
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
WHERE i.name = 'IX_Classifications_RaceId_MemberFirstName_MemberLastName_Position'
	AND i.object_id = OBJECT_ID(N'[dbo].[Classifications]')
ORDER BY ic.key_ordinal;
