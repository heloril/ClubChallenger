-- Migration: Add RaceEventDistances table
-- This table stores predefined distances for each race event

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RaceEventDistances')
BEGIN
    CREATE TABLE RaceEventDistances (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        RaceEventId INT NOT NULL,
        DistanceKm DECIMAL(10,3) NOT NULL,
        CONSTRAINT FK_RaceEventDistances_RaceEvents FOREIGN KEY (RaceEventId) 
            REFERENCES RaceEvents(Id) ON DELETE CASCADE,
        CONSTRAINT UQ_RaceEventDistances UNIQUE (RaceEventId, DistanceKm)
    );

    PRINT 'RaceEventDistances table created successfully';
END
ELSE
BEGIN
    PRINT 'RaceEventDistances table already exists';
END
GO
