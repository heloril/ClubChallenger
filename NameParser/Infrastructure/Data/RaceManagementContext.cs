using System;
using System.Configuration;
using Microsoft.EntityFrameworkCore;
using NameParser.Infrastructure.Data.Models;

namespace NameParser.Infrastructure.Data
{
    public class RaceManagementContext : DbContext
    {
        public RaceManagementContext()
        {
            Database.EnsureCreated();

            // Apply custom migrations if needed
            ApplyCustomMigrations();
        }

        public RaceManagementContext(DbContextOptions<RaceManagementContext> options)
            : base(options)
        {
        }

        public DbSet<RaceEntity> Races { get; set; }
        public DbSet<ClassificationEntity> Classifications { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var connectionString = ConfigurationManager.ConnectionStrings["RaceManagementDb"]?.ConnectionString;
                if (string.IsNullOrEmpty(connectionString))
                {
                    connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=|DataDirectory|\RaceManagement.mdf;Integrated Security=True;Connect Timeout=30";
                }
                optionsBuilder.UseSqlServer(connectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<RaceEntity>()
                .HasIndex(r => new { r.Year, r.RaceNumber })
                .IsUnique();

            modelBuilder.Entity<ClassificationEntity>()
                .HasOne(c => c.Race)
                .WithMany()
                .HasForeignKey(c => c.RaceId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        private void ApplyCustomMigrations()
        {
            try
            {
                // Check if new columns exist, if not add them
                var connection = Database.GetDbConnection();
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
                }

                connection.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Migration warning: {ex.Message}");
                // Don't throw - allow app to continue if migration fails
            }
        }
    }
}
