using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using NameParser.Infrastructure.Data.Models;
using Newtonsoft.Json;

namespace NameParser.Infrastructure.Data
{
    public static class DatabaseInitializer
    {
        private static bool _initialized = false;
        private static readonly object _lock = new object();

        public static void Initialize()
        {
            if (_initialized)
                return;

            lock (_lock)
            {
                if (_initialized)
                    return;

                using (var context = new RaceManagementContext())
                {
                    try
                    {
                        // Ensure the database is created
                        context.Database.EnsureCreated();

                        // Apply custom migrations
                        ApplyCustomMigrations(context);

                        _initialized = true;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Database initialization error: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        private static void ApplyCustomMigrations(RaceManagementContext context)
        {
            try
            {
                // Check if new columns exist, if not add them
                var connection = context.Database.GetDbConnection();
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    // Check for Sex column
                    command.CommandText = @"
                        IF NOT EXISTS (
                            SELECT * FROM sys.columns 
                            WHERE object_id = OBJECT_ID(N'[dbo].[Classifications]') 
                            AND name = 'Sex'
                        )
                        BEGIN
                            ALTER TABLE [dbo].[Classifications] ADD [Sex] NVARCHAR(1) NULL;
                            PRINT 'Sex column added';
                        END";
                    command.ExecuteNonQuery();

                    // Check for PositionBySex column
                    command.CommandText = @"
                        IF NOT EXISTS (
                            SELECT * FROM sys.columns 
                            WHERE object_id = OBJECT_ID(N'[dbo].[Classifications]') 
                            AND name = 'PositionBySex'
                        )
                        BEGIN
                            ALTER TABLE [dbo].[Classifications] ADD [PositionBySex] INT NULL;
                            PRINT 'PositionBySex column added';
                        END";
                    command.ExecuteNonQuery();

                    // Check for AgeCategory column
                    command.CommandText = @"
                        IF NOT EXISTS (
                            SELECT * FROM sys.columns 
                            WHERE object_id = OBJECT_ID(N'[dbo].[Classifications]') 
                            AND name = 'AgeCategory'
                        )
                        BEGIN
                            ALTER TABLE [dbo].[Classifications] ADD [AgeCategory] NVARCHAR(50) NULL;
                            PRINT 'AgeCategory column added';
                        END";
                    command.ExecuteNonQuery();

                    // Check for PositionByCategory column
                    command.CommandText = @"
                        IF NOT EXISTS (
                            SELECT * FROM sys.columns 
                            WHERE object_id = OBJECT_ID(N'[dbo].[Classifications]') 
                            AND name = 'PositionByCategory'
                        )
                        BEGIN
                            ALTER TABLE [dbo].[Classifications] ADD [PositionByCategory] INT NULL;
                            PRINT 'PositionByCategory column added';
                        END";
                    command.ExecuteNonQuery();

                    // Check for IsChallenger column
                    command.CommandText = @"
                        IF NOT EXISTS (
                            SELECT * FROM sys.columns 
                            WHERE object_id = OBJECT_ID(N'[dbo].[Classifications]') 
                            AND name = 'IsChallenger'
                        )
                        BEGIN
                            ALTER TABLE [dbo].[Classifications] ADD [IsChallenger] BIT NOT NULL DEFAULT 0;
                            PRINT 'IsChallenger column added';
                        END";
                    command.ExecuteNonQuery();

                    // Create indexes if they don't exist
                    command.CommandText = @"
                        IF NOT EXISTS (
                            SELECT * FROM sys.indexes 
                            WHERE name = 'IX_Classifications_Sex_PositionBySex' 
                            AND object_id = OBJECT_ID(N'[dbo].[Classifications]')
                        )
                        BEGIN
                            CREATE NONCLUSTERED INDEX [IX_Classifications_Sex_PositionBySex]
                            ON [dbo].[Classifications] ([Sex] ASC, [PositionBySex] ASC);
                            PRINT 'Index IX_Classifications_Sex_PositionBySex created';
                        END";
                    command.ExecuteNonQuery();

                    command.CommandText = @"
                        IF NOT EXISTS (
                            SELECT * FROM sys.indexes 
                            WHERE name = 'IX_Classifications_AgeCategory_PositionByCategory' 
                            AND object_id = OBJECT_ID(N'[dbo].[Classifications]')
                        )
                        BEGIN
                            CREATE NONCLUSTERED INDEX [IX_Classifications_AgeCategory_PositionByCategory]
                            ON [dbo].[Classifications] ([AgeCategory] ASC, [PositionByCategory] ASC);
                            PRINT 'Index IX_Classifications_AgeCategory_PositionByCategory created';
                        END";
                    command.ExecuteNonQuery();

                    // Check for FileContent column in Races table
                    command.CommandText = @"
                        IF NOT EXISTS (
                            SELECT * FROM sys.columns 
                            WHERE object_id = OBJECT_ID(N'[dbo].[Races]') 
                            AND name = 'FileContent'
                        )
                        BEGIN
                            ALTER TABLE [dbo].[Races] ADD [FileContent] VARBINARY(MAX) NULL;
                            PRINT 'FileContent column added to Races table';
                        END";
                    command.ExecuteNonQuery();

                    // Check for FileName column in Races table
                    command.CommandText = @"
                        IF NOT EXISTS (
                            SELECT * FROM sys.columns 
                            WHERE object_id = OBJECT_ID(N'[dbo].[Races]') 
                            AND name = 'FileName'
                        )
                        BEGIN
                            ALTER TABLE [dbo].[Races] ADD [FileName] NVARCHAR(255) NULL;
                            PRINT 'FileName column added to Races table';
                        END";
                    command.ExecuteNonQuery();

                    // Check for FileExtension column in Races table
                    command.CommandText = @"
                        IF NOT EXISTS (
                            SELECT * FROM sys.columns 
                            WHERE object_id = OBJECT_ID(N'[dbo].[Races]') 
                            AND name = 'FileExtension'
                        )
                        BEGIN
                            ALTER TABLE [dbo].[Races] ADD [FileExtension] NVARCHAR(10) NULL;
                            PRINT 'FileExtension column added to Races table';
                        END";
                    command.ExecuteNonQuery();

                    // Update unique index from (Year, RaceNumber) to (Year, RaceNumber, DistanceKm)
                    // This allows multiple races with same year/number but different distances
                    command.CommandText = @"
                        IF EXISTS (
                            SELECT * FROM sys.indexes 
                            WHERE name = 'IX_Races_Year_RaceNumber' 
                            AND object_id = OBJECT_ID(N'[dbo].[Races]')
                        )
                        BEGIN
                            DROP INDEX [IX_Races_Year_RaceNumber] ON [dbo].[Races];
                            PRINT 'Old index IX_Races_Year_RaceNumber dropped';
                        END

                        IF NOT EXISTS (
                            SELECT * FROM sys.indexes 
                            WHERE name = 'IX_Races_Year_RaceNumber_DistanceKm' 
                            AND object_id = OBJECT_ID(N'[dbo].[Races]')
                        )
                        BEGIN
                            CREATE UNIQUE NONCLUSTERED INDEX [IX_Races_Year_RaceNumber_DistanceKm]
                            ON [dbo].[Races] ([Year] ASC, [RaceNumber] ASC, [DistanceKm] ASC);
                            PRINT 'New index IX_Races_Year_RaceNumber_DistanceKm created';
                        END";
                    command.ExecuteNonQuery();

                    // Add RaceEventId column to Races table
                    command.CommandText = @"
                        IF NOT EXISTS (
                            SELECT * FROM sys.columns 
                            WHERE object_id = OBJECT_ID(N'[dbo].[Races]') 
                            AND name = 'RaceEventId'
                        )
                        BEGIN
                            ALTER TABLE [dbo].[Races] ADD [RaceEventId] INT NULL;
                            PRINT 'RaceEventId column added to Races table';
                        END";
                    command.ExecuteNonQuery();

                    // Create RaceEvents table if it doesn't exist
                    command.CommandText = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RaceEvents]') AND type in (N'U'))
                        BEGIN
                            CREATE TABLE [dbo].[RaceEvents] (
                                [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                [Name] NVARCHAR(200) NOT NULL,
                                [EventDate] DATETIME2(7) NOT NULL,
                                [Location] NVARCHAR(200) NULL,
                                [WebsiteUrl] NVARCHAR(500) NULL,
                                [Description] NVARCHAR(2000) NULL,
                                [CreatedDate] DATETIME2(7) NOT NULL,
                                [ModifiedDate] DATETIME2(7) NULL
                            );
                            PRINT 'RaceEvents table created';
                        END";
                    command.ExecuteNonQuery();

                    // Create Challenges table if it doesn't exist
                    command.CommandText = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Challenges]') AND type in (N'U'))
                        BEGIN
                            CREATE TABLE [dbo].[Challenges] (
                                [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                [Name] NVARCHAR(200) NOT NULL,
                                [Description] NVARCHAR(1000) NULL,
                                [Year] INT NOT NULL,
                                [StartDate] DATETIME2(7) NULL,
                                [EndDate] DATETIME2(7) NULL,
                                [CreatedDate] DATETIME2(7) NOT NULL,
                                [ModifiedDate] DATETIME2(7) NULL
                            );
                            PRINT 'Challenges table created';
                        END";
                    command.ExecuteNonQuery();

                    // Create ChallengeRaceEvents table if it doesn't exist
                    command.CommandText = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ChallengeRaceEvents]') AND type in (N'U'))
                        BEGIN
                            CREATE TABLE [dbo].[ChallengeRaceEvents] (
                                [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                [ChallengeId] INT NOT NULL,
                                [RaceEventId] INT NOT NULL,
                                [DisplayOrder] INT NOT NULL,
                                CONSTRAINT [FK_ChallengeRaceEvents_Challenges_ChallengeId] 
                                    FOREIGN KEY ([ChallengeId]) REFERENCES [dbo].[Challenges] ([Id]) ON DELETE CASCADE,
                                CONSTRAINT [FK_ChallengeRaceEvents_RaceEvents_RaceEventId] 
                                    FOREIGN KEY ([RaceEventId]) REFERENCES [dbo].[RaceEvents] ([Id]) ON DELETE CASCADE
                            );
                            PRINT 'ChallengeRaceEvents table created';
                        END";
                    command.ExecuteNonQuery();

                    // Create unique index on ChallengeRaceEvents if it doesn't exist
                    command.CommandText = @"
                        IF NOT EXISTS (
                            SELECT * FROM sys.indexes 
                            WHERE name = 'IX_ChallengeRaceEvents_ChallengeId_RaceEventId' 
                            AND object_id = OBJECT_ID(N'[dbo].[ChallengeRaceEvents]')
                        )
                        BEGIN
                            CREATE UNIQUE NONCLUSTERED INDEX [IX_ChallengeRaceEvents_ChallengeId_RaceEventId]
                            ON [dbo].[ChallengeRaceEvents] ([ChallengeId] ASC, [RaceEventId] ASC);
                            PRINT 'Unique index on ChallengeRaceEvents created';
                        END";
                    command.ExecuteNonQuery();

                    // Add foreign key from Races to RaceEvents if it doesn't exist
                    command.CommandText = @"
                        IF NOT EXISTS (
                            SELECT * FROM sys.foreign_keys 
                            WHERE name = 'FK_Races_RaceEvents_RaceEventId' 
                            AND parent_object_id = OBJECT_ID(N'[dbo].[Races]')
                        )
                        AND EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Races]') AND name = 'RaceEventId')
                        AND EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RaceEvents]') AND type in (N'U'))
                        BEGIN
                            ALTER TABLE [dbo].[Races]
                            ADD CONSTRAINT [FK_Races_RaceEvents_RaceEventId] 
                                FOREIGN KEY ([RaceEventId]) REFERENCES [dbo].[RaceEvents] ([Id]) ON DELETE SET NULL;
                            PRINT 'Foreign key from Races to RaceEvents created';
                        END";
                    command.ExecuteNonQuery();

                    // Create RaceEventDistances table if it doesn't exist
                    command.CommandText = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RaceEventDistances]') AND type in (N'U'))
                        BEGIN
                            CREATE TABLE [dbo].[RaceEventDistances] (
                                [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                [RaceEventId] INT NOT NULL,
                                [DistanceKm] DECIMAL(10,3) NOT NULL,
                                CONSTRAINT [FK_RaceEventDistances_RaceEvents_RaceEventId] 
                                    FOREIGN KEY ([RaceEventId]) REFERENCES [dbo].[RaceEvents] ([Id]) ON DELETE CASCADE
                            );
                            PRINT 'RaceEventDistances table created';
                        END";
                    command.ExecuteNonQuery();

                    // Create unique index on RaceEventDistances if it doesn't exist
                    command.CommandText = @"
                        IF NOT EXISTS (
                            SELECT * FROM sys.indexes 
                            WHERE name = 'IX_RaceEventDistances_RaceEventId_DistanceKm' 
                            AND object_id = OBJECT_ID(N'[dbo].[RaceEventDistances]')
                        )
                        BEGIN
                            CREATE UNIQUE NONCLUSTERED INDEX [IX_RaceEventDistances_RaceEventId_DistanceKm]
                            ON [dbo].[RaceEventDistances] ([RaceEventId] ASC, [DistanceKm] ASC);
                            PRINT 'Unique index on RaceEventDistances created';
                        END";
                    command.ExecuteNonQuery();

                    // Create unique index on Classifications to allow duplicate names with different positions
                    command.CommandText = @"
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
                            PRINT 'Unique index IX_Classifications_RaceId_MemberFirstName_MemberLastName_Position created';
                        END";
                    command.ExecuteNonQuery();

                    // Create Members table if it doesn't exist
                    command.CommandText = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Members]') AND type in (N'U'))
                        BEGIN
                            CREATE TABLE [dbo].[Members] (
                                [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                [FirstName] NVARCHAR(200) NULL,
                                [LastName] NVARCHAR(200) NOT NULL,
                                [Email] NVARCHAR(400) NULL,
                                [IsMember] BIT NOT NULL DEFAULT 1,
                                [IsChallenger] BIT NOT NULL DEFAULT 0
                            );
                            CREATE NONCLUSTERED INDEX [IX_Members_LastName_FirstName]
                                ON [dbo].[Members] ([LastName] ASC, [FirstName] ASC);
                            PRINT 'Members table created';
                        END";
                    command.ExecuteNonQuery();

                    // Create MemberVmaHistories table if it doesn't exist
                    command.CommandText = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MemberVmaHistories]') AND type in (N'U'))
                        BEGIN
                            CREATE TABLE [dbo].[MemberVmaHistories] (
                                [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                [MemberId] INT NOT NULL,
                                [Vma] FLOAT NOT NULL,
                                [RecordedDate] DATETIME2(7) NOT NULL,
                                [IsExtrapolated] BIT NOT NULL DEFAULT 0,
                                CONSTRAINT [FK_MemberVmaHistories_Members_MemberId]
                                    FOREIGN KEY ([MemberId]) REFERENCES [dbo].[Members] ([Id]) ON DELETE CASCADE
                            );
                            CREATE NONCLUSTERED INDEX [IX_MemberVmaHistories_MemberId]
                                ON [dbo].[MemberVmaHistories] ([MemberId] ASC);
                            PRINT 'MemberVmaHistories table created';
                        END";
                    command.ExecuteNonQuery();

                    // Check for IsExtrapolated column on MemberVmaHistories (added later)
                    command.CommandText = @"
                        IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MemberVmaHistories]') AND type in (N'U'))
                        AND NOT EXISTS (
                            SELECT * FROM sys.columns
                            WHERE object_id = OBJECT_ID(N'[dbo].[MemberVmaHistories]')
                            AND name = 'IsExtrapolated'
                        )
                        BEGIN
                            ALTER TABLE [dbo].[MemberVmaHistories] ADD [IsExtrapolated] BIT NOT NULL DEFAULT 0;
                            PRINT 'IsExtrapolated column added';
                        END";
                    command.ExecuteNonQuery();

                    // Create VmaExtrapolationLogs table if it doesn't exist
                    command.CommandText = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[VmaExtrapolationLogs]') AND type in (N'U'))
                        BEGIN
                            CREATE TABLE [dbo].[VmaExtrapolationLogs] (
                                [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                [MemberFirstName] NVARCHAR(100) NOT NULL,
                                [MemberLastName] NVARCHAR(100) NOT NULL,
                                [CreatedDate] DATETIME2(7) NOT NULL,
                                [Method] NVARCHAR(50) NULL,
                                [CalibrationPointCount] INT NOT NULL DEFAULT 0,
                                [TargetSpeed] FLOAT NULL,
                                [Slope] FLOAT NULL,
                                [Intercept] FLOAT NULL,
                                [PredictedVma] FLOAT NULL,
                                [IsSuccess] BIT NOT NULL DEFAULT 0,
                                [Details] NVARCHAR(1000) NULL,
                                [CreatedBy] NVARCHAR(200) NULL
                            );
                            CREATE NONCLUSTERED INDEX [IX_VmaExtrapolationLogs_Member]
                                ON [dbo].[VmaExtrapolationLogs] ([MemberLastName] ASC, [MemberFirstName] ASC);
                            PRINT 'VmaExtrapolationLogs table created';
                        END";
                    command.ExecuteNonQuery();
                }

                connection.Close();

                // One-time migration of member data from the legacy JSON files into the database.
                SeedMembersFromJson(context);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Migration warning: {ex.Message}");
                // Don't throw - allow app to continue if migration fails
            }
        }

        /// <summary>
        /// One-time import of member/challenger data from the legacy JSON files
        /// (Members.json, Challenge.json) into the database. Runs only while the
        /// Members table is empty so it is safe to call on every startup.
        /// </summary>
        private static void SeedMembersFromJson(RaceManagementContext context)
        {
            try
            {
                if (context.Members.Any())
                    return;

                var merged = new Dictionary<string, MemberEntity>();

                void Import(string fileName, bool asMember, bool asChallenger)
                {
                    foreach (var dto in ReadMemberDtos(fileName))
                    {
                        if (string.IsNullOrWhiteSpace(dto.LastName))
                            continue;

                        var key = $"{dto.FirstName?.NormalizeForComparison() ?? string.Empty}|{dto.LastName?.NormalizeForComparison() ?? string.Empty}";

                        if (!merged.TryGetValue(key, out var entity))
                        {
                            entity = new MemberEntity
                            {
                                FirstName = dto.FirstName,
                                LastName = dto.LastName,
                                Email = dto.Email,
                                IsMember = false,
                                IsChallenger = false
                            };
                            merged[key] = entity;
                        }

                        if (string.IsNullOrWhiteSpace(entity.Email) && !string.IsNullOrWhiteSpace(dto.Email))
                            entity.Email = dto.Email;

                        if (asMember || (dto.IsMember ?? false))
                            entity.IsMember = true;
                        if (asChallenger || (dto.IsChallenger ?? false))
                            entity.IsChallenger = true;

                        if (dto.Vma.HasValue && !entity.VmaHistory.Any())
                        {
                            entity.VmaHistory.Add(new MemberVmaHistoryEntity
                            {
                                Vma = dto.Vma.Value,
                                RecordedDate = DateTime.Now
                            });
                        }
                    }
                }

                Import("Members.json", asMember: true, asChallenger: false);
                Import("Challenge.json", asMember: false, asChallenger: true);

                if (merged.Count == 0)
                    return;

                context.Members.AddRange(merged.Values);
                context.SaveChanges();

                System.Diagnostics.Debug.WriteLine($"Seeded {merged.Count} member(s) from JSON into the database.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Member seeding warning: {ex.Message}");
                // Don't throw - allow app to continue even if seeding fails
            }
        }

        private static IEnumerable<SeedMemberDto> ReadMemberDtos(string fileName)
        {
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
            if (!File.Exists(filePath))
                return Enumerable.Empty<SeedMemberDto>();

            var json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<List<SeedMemberDto>>(json) ?? new List<SeedMemberDto>();
        }

        private class SeedMemberDto
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            public bool? IsMember { get; set; }
            public bool? IsChallenger { get; set; }
            public double? Vma { get; set; }
        }
    }
}
