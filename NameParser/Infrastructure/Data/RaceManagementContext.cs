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
        }

        public RaceManagementContext(DbContextOptions<RaceManagementContext> options)
            : base(options)
        {
        }

        public DbSet<RaceEntity> Races { get; set; }
        public DbSet<ClassificationEntity> Classifications { get; set; }
        public DbSet<ChallengeEntity> Challenges { get; set; }
        public DbSet<RaceEventEntity> RaceEvents { get; set; }
        public DbSet<ChallengeRaceEventEntity> ChallengeRaceEvents { get; set; }
        public DbSet<RaceEventDistanceEntity> RaceEventDistances { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var connectionString = ConfigurationManager.ConnectionStrings["RaceManagementDb"]?.ConnectionString;
                if (string.IsNullOrEmpty(connectionString))
                {
                    // Use catalog-based connection instead of file attachment
                    connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=RaceManagementDb;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
                }
                optionsBuilder.UseSqlServer(connectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Unique constraint on Year, RaceNumber, and DistanceKm
            // This allows multiple races with the same year/number but different distances
            modelBuilder.Entity<RaceEntity>()
                .HasIndex(r => new { r.Year, r.RaceNumber, r.DistanceKm })
                .IsUnique();

            modelBuilder.Entity<ClassificationEntity>()
                .HasOne(c => c.Race)
                .WithMany()
                .HasForeignKey(c => c.RaceId)
                .OnDelete(DeleteBehavior.Cascade);

            // Challenge-RaceEvent many-to-many relationship
            modelBuilder.Entity<ChallengeRaceEventEntity>()
                .HasOne(cre => cre.Challenge)
                .WithMany()
                .HasForeignKey(cre => cre.ChallengeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChallengeRaceEventEntity>()
                .HasOne(cre => cre.RaceEvent)
                .WithMany()
                .HasForeignKey(cre => cre.RaceEventId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint: A race event can only be added once to a challenge
            modelBuilder.Entity<ChallengeRaceEventEntity>()
                .HasIndex(cre => new { cre.ChallengeId, cre.RaceEventId })
                .IsUnique();

            // Race-RaceEvent relationship
            modelBuilder.Entity<RaceEntity>()
                .HasOne(r => r.RaceEvent)
                .WithMany()
                .HasForeignKey(r => r.RaceEventId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
