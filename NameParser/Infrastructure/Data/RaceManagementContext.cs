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
        }
    }
}
