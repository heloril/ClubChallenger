# Migration Script for EmailLogs Table

Execute this SQL script on your RaceManagementDb database to add email logging functionality:

```sql
-- Create EmailLogs table
CREATE TABLE EmailLogs (
    Id INT PRIMARY KEY IDENTITY(1,1),
    EmailType NVARCHAR(50) NOT NULL,
    ChallengeId INT NULL,
    RecipientEmail NVARCHAR(255) NOT NULL,
    RecipientName NVARCHAR(255) NULL,
    Subject NVARCHAR(500) NULL,
    SentDate DATETIME2 NOT NULL,
    IsSuccess BIT NOT NULL,
    ErrorMessage NVARCHAR(MAX) NULL,
    IsTest BIT NOT NULL DEFAULT 0,
    SentBy NVARCHAR(100) NULL,
    CONSTRAINT FK_EmailLogs_Challenges FOREIGN KEY (ChallengeId) REFERENCES Challenges(Id) ON DELETE SET NULL
);

-- Create index for better query performance
CREATE INDEX IX_EmailLogs_RecipientEmail ON EmailLogs(RecipientEmail);
CREATE INDEX IX_EmailLogs_ChallengeId ON EmailLogs(ChallengeId);
CREATE INDEX IX_EmailLogs_SentDate ON EmailLogs(SentDate DESC);
CREATE INDEX IX_EmailLogs_EmailType ON EmailLogs(EmailType);
```

## Features Added

This migration adds:

1. **EmailLogs table** to track all emails sent through the system
2. **Email tracking** for both Challenge and Member mailings
3. **Status monitoring** - success/failure tracking with error messages
4. **Historical data** - complete audit trail of all emails sent
5. **Test email tracking** - separate tracking for test emails

## How to Apply

### Option 1: SQL Server Management Studio (SSMS)
1. Open SSMS and connect to `(LocalDB)\MSSQLLocalDB`
2. Select database `RaceManagementDb`
3. Copy and paste the SQL script above
4. Execute (F5)

### Option 2: Visual Studio SQL Server Object Explorer
1. Open Visual Studio
2. View → SQL Server Object Explorer
3. Expand (LocalDB)\MSSQLLocalDB → Databases → RaceManagementDb
4. Right-click → New Query
5. Paste the SQL script and execute

### Option 3: Command Line (sqlcmd)
```bash
sqlcmd -S "(LocalDB)\MSSQLLocalDB" -d RaceManagementDb -Q "CREATE TABLE EmailLogs (Id INT PRIMARY KEY IDENTITY(1,1), EmailType NVARCHAR(50) NOT NULL, ChallengeId INT NULL, RecipientEmail NVARCHAR(255) NOT NULL, RecipientName NVARCHAR(255) NULL, Subject NVARCHAR(500) NULL, SentDate DATETIME2 NOT NULL, IsSuccess BIT NOT NULL, ErrorMessage NVARCHAR(MAX) NULL, IsTest BIT NOT NULL DEFAULT 0, SentBy NVARCHAR(100) NULL, CONSTRAINT FK_EmailLogs_Challenges FOREIGN KEY (ChallengeId) REFERENCES Challenges(Id) ON DELETE SET NULL);"
```
