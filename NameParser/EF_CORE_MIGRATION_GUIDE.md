# Entity Framework Core Migration Guide

## Migration Summary

Your project has been successfully migrated from **Entity Framework 6** to **Entity Framework Core 3.1**.

### Why EF Core 3.1?
- EF Core 3.1 is the last version that supports .NET Framework 4.8
- Provides better performance and modern features
- Aligns with Microsoft's recommended migration path

## Changes Made

### 1. Package Updates
**Removed:**
- EntityFramework 6.5.1
- EntityFramework.SqlServerCompact 6.5.1

**Added:**
- Microsoft.EntityFrameworkCore 3.1.32
- Microsoft.EntityFrameworkCore.SqlServer 3.1.32
- Microsoft.EntityFrameworkCore.Tools 3.1.32
- System.Configuration.ConfigurationManager 4.7.0

### 2. DbContext Updates (`RaceManagementContext.cs`)

**Key Changes:**
- Changed from `System.Data.Entity.DbContext` to `Microsoft.EntityFrameworkCore.DbContext`
- Added `OnConfiguring()` method for connection string configuration
- Updated `OnModelCreating()` to use `ModelBuilder` instead of `DbModelBuilder`
- Changed relationship configuration:
  - `HasRequired().WithMany().WillCascadeOnDelete(true)` → `HasOne().WithMany().OnDelete(DeleteBehavior.Cascade)`

### 3. Repository Updates

**ClassificationRepository.cs:**
- Changed string-based Include to lambda-based Include
- `Include("Race")` → `Include(c => c.Race)`

### 4. Configuration Updates (`app.config`)

**Removed:**
- Entity Framework 6 configuration sections
- `defaultConnectionFactory` and `providers` configuration

**Retained:**
- Connection strings (unchanged)
- Startup configuration (unchanged)

## Database Migrations

### Creating Initial Migration

If you need to recreate your database or generate migrations:

```powershell
# Add initial migration
dotnet ef migrations add InitialCreate --project NameParser.csproj

# Update database
dotnet ef database update --project NameParser.csproj
```

### Using Existing Database

Your existing database should work without modifications. EF Core 3.1 is compatible with databases created by EF6.

## Key Differences to Be Aware Of

### 1. Lazy Loading
EF Core requires explicit configuration for lazy loading. Your current code uses eager loading with `Include()`, which works correctly.

### 2. Connection Management
- EF6: Used named connection strings with `base("name=RaceManagementDb")`
- EF Core: Uses `OnConfiguring()` to read connection strings from configuration

### 3. Model Configuration
- `HasRequired()` → `HasOne()`
- `WillCascadeOnDelete()` → `OnDelete()`
- `DbModelBuilder` → `ModelBuilder`

### 4. Database Initialization
- EF6: Used `Database.SetInitializer()`
- EF Core: Uses migrations or `Database.EnsureCreated()` if needed

## Testing Recommendations

1. **Test Database Creation:** Ensure the database can be created if it doesn't exist
2. **Test CRUD Operations:** Verify all repository methods work correctly
3. **Test Relationships:** Confirm cascade deletes work as expected
4. **Test Include Operations:** Verify eager loading works with lambda expressions

## Additional Features in EF Core

### 1. Better Performance
EF Core 3.1 includes significant performance improvements over EF6.

### 2. LINQ Improvements
More LINQ operations can be translated to SQL, reducing client-side evaluation.

### 3. Global Query Filters
You can define filters that apply to all queries for an entity:

```csharp
modelBuilder.Entity<RaceEntity>()
    .HasQueryFilter(r => r.Status != "Deleted");
```

### 4. Value Conversions
Custom conversions between property types and database types:

```csharp
modelBuilder.Entity<RaceEntity>()
    .Property(r => r.Status)
    .HasConversion<string>();
```

## Troubleshooting

### Issue: "No database provider configured"
**Solution:** Ensure `OnConfiguring()` is called or inject `DbContextOptions`.

### Issue: "Cannot find table"
**Solution:** Run migrations or use `Database.EnsureCreated()` in development.

### Issue: "Connection string not found"
**Solution:** Verify app.config is copied to output directory and connection string name matches.

## Next Steps

1. Run comprehensive tests on all database operations
2. Consider adding database migrations for version control
3. Review EF Core documentation for additional features: https://docs.microsoft.com/ef/core/
4. Consider migrating to EF Core 6+ when upgrading to .NET 6+ in the future

## Migration Validation Checklist

- [x] Entity Framework 6 packages removed
- [x] Entity Framework Core 3.1 packages installed
- [x] DbContext updated to use EF Core
- [x] Repositories updated with lambda-based Include
- [x] app.config cleaned up
- [x] Project builds successfully
- [ ] Database connection tested
- [ ] CRUD operations tested
- [ ] All unit tests pass (if applicable)
- [ ] Integration tests pass (if applicable)

## References

- [EF Core Documentation](https://docs.microsoft.com/ef/core/)
- [EF6 to EF Core Migration Guide](https://docs.microsoft.com/ef/efcore-and-ef6/porting/)
- [EF Core 3.1 Release Notes](https://docs.microsoft.com/ef/core/what-is-new/ef-core-3.x/)
