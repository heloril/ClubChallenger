-- Migration: Add EmailLogs table for email tracking
-- Date: 2025
-- Description: Adds email logging functionality to track all emails sent through the mailing system

USE RaceManagementDb;
GO

-- Create EmailLogs table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EmailLogs')
BEGIN
    CREATE TABLE EmailLogs (
        Id INT PRIMARY KEY IDENTITY(1,1),
        EmailType NVARCHAR(50) NOT NULL,
        ChallengeId INT NULL,
        RecipientEmail NVARCHAR(255) NOT NULL,
        RecipientName NVARCHAR(255) NULL,
        Subject NVARCHAR(500) NULL,
        SentDate DATETIME2 NOT NULL DEFAULT GETDATE(),
        IsSuccess BIT NOT NULL DEFAULT 0,
        ErrorMessage NVARCHAR(MAX) NULL,
        IsTest BIT NOT NULL DEFAULT 0,
        SentBy NVARCHAR(100) NULL,
        CONSTRAINT FK_EmailLogs_Challenges FOREIGN KEY (ChallengeId) 
            REFERENCES Challenges(Id) ON DELETE SET NULL
    );
    
    PRINT 'EmailLogs table created successfully';
END
ELSE
BEGIN
    PRINT 'EmailLogs table already exists';
END
GO

-- Create indexes for better query performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_EmailLogs_RecipientEmail')
BEGIN
    CREATE INDEX IX_EmailLogs_RecipientEmail ON EmailLogs(RecipientEmail);
    PRINT 'Index IX_EmailLogs_RecipientEmail created';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_EmailLogs_ChallengeId')
BEGIN
    CREATE INDEX IX_EmailLogs_ChallengeId ON EmailLogs(ChallengeId);
    PRINT 'Index IX_EmailLogs_ChallengeId created';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_EmailLogs_SentDate')
BEGIN
    CREATE INDEX IX_EmailLogs_SentDate ON EmailLogs(SentDate DESC);
    PRINT 'Index IX_EmailLogs_SentDate created';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_EmailLogs_EmailType')
BEGIN
    CREATE INDEX IX_EmailLogs_EmailType ON EmailLogs(EmailType);
    PRINT 'Index IX_EmailLogs_EmailType created';
END
GO

-- Verify the table was created
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'EmailLogs')
BEGIN
    PRINT '✅ Migration completed successfully!';
    SELECT COUNT(*) AS InitialRecordCount FROM EmailLogs;
END
ELSE
BEGIN
    PRINT '❌ Migration failed - EmailLogs table not found';
END
GO
