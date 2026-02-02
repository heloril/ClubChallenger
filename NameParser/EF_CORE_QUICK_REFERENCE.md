# EF Core Quick Reference

## Common Operations

### Database Operations

```csharp
// Ensure database exists (Development only)
using (var context = new RaceManagementContext())
{
    context.Database.EnsureCreated();
}

// Check if database can connect
using (var context = new RaceManagementContext())
{
    bool canConnect = context.Database.CanConnect();
}

// Execute raw SQL
using (var context = new RaceManagementContext())
{
    context.Database.ExecuteSqlRaw("DELETE FROM Classifications WHERE RaceId = {0}", raceId);
}
```

### Query Operations

```csharp
// Eager Loading (Include)
var classifications = context.Classifications
    .Include(c => c.Race)
    .Where(c => c.RaceId == raceId)
    .ToList();

// Multiple Includes
var classifications = context.Classifications
    .Include(c => c.Race)
    .ThenInclude(r => r.SomeOtherNavigation) // If there were more levels
    .ToList();

// Explicit Loading
var race = context.Races.Find(raceId);
context.Entry(race)
    .Collection(r => r.Classifications) // If this relationship existed
    .Load();

// AsNoTracking for read-only queries (better performance)
var races = context.Races
    .AsNoTracking()
    .Where(r => r.Year == 2024)
    .ToList();
```

### Add Operations

```csharp
// Add single entity
context.Races.Add(newRace);
context.SaveChanges();

// Add multiple entities
context.Classifications.AddRange(classificationList);
context.SaveChanges();
```

### Update Operations

```csharp
// Update tracked entity
var race = context.Races.Find(id);
if (race != null)
{
    race.Status = "Completed";
    context.SaveChanges();
}

// Update untracked entity
var race = new RaceEntity { Id = id, Status = "Completed" };
context.Races.Attach(race);
context.Entry(race).Property(r => r.Status).IsModified = true;
context.SaveChanges();

// Update specific properties
context.Races
    .Where(r => r.Id == id)
    .ExecuteUpdate(s => s.SetProperty(r => r.Status, "Completed"));
```

### Delete Operations

```csharp
// Delete tracked entity
var race = context.Races.Find(id);
if (race != null)
{
    context.Races.Remove(race);
    context.SaveChanges();
}

// Delete without loading (EF Core 3.1)
var race = new RaceEntity { Id = id };
context.Races.Attach(race);
context.Races.Remove(race);
context.SaveChanges();

// Bulk delete
var toDelete = context.Classifications.Where(c => c.RaceId == raceId);
context.Classifications.RemoveRange(toDelete);
context.SaveChanges();
```

## Advanced Queries

### Grouping and Aggregation

```csharp
// Group by with count
var racesByYear = context.Races
    .GroupBy(r => r.Year)
    .Select(g => new { Year = g.Key, Count = g.Count() })
    .ToList();

// Average, Sum, Max, Min
var avgDistance = context.Races.Average(r => r.DistanceKm);
var totalPoints = context.Classifications.Sum(c => c.Points);
```

### Pagination

```csharp
// Skip and Take for pagination
var page = 2;
var pageSize = 10;
var races = context.Races
    .OrderBy(r => r.Year)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToList();
```

### Filtering

```csharp
// Complex filters
var races = context.Races
    .Where(r => r.Year >= 2020 && r.Year <= 2024)
    .Where(r => r.Status == "Completed" || r.Status == "Pending")
    .OrderByDescending(r => r.Year)
    .ToList();

// Conditional filtering
IQueryable<RaceEntity> query = context.Races;
if (!string.IsNullOrEmpty(status))
{
    query = query.Where(r => r.Status == status);
}
var results = query.ToList();
```

## Configuration in OnModelCreating

### Index Configuration

```csharp
// Unique index
modelBuilder.Entity<RaceEntity>()
    .HasIndex(r => new { r.Year, r.RaceNumber })
    .IsUnique();

// Non-unique index
modelBuilder.Entity<ClassificationEntity>()
    .HasIndex(c => c.RaceId);
```

### Relationship Configuration

```csharp
// One-to-Many with cascade delete
modelBuilder.Entity<ClassificationEntity>()
    .HasOne(c => c.Race)
    .WithMany() // or .WithMany(r => r.Classifications) if property exists
    .HasForeignKey(c => c.RaceId)
    .OnDelete(DeleteBehavior.Cascade);

// Delete behaviors
// - Cascade: Delete related entities
// - SetNull: Set foreign key to null
// - Restrict: Prevent delete if related entities exist
// - NoAction: No action (database will handle)
```

### Property Configuration

```csharp
// Required/Optional
modelBuilder.Entity<RaceEntity>()
    .Property(r => r.Name)
    .IsRequired()
    .HasMaxLength(100);

// Default value
modelBuilder.Entity<RaceEntity>()
    .Property(r => r.CreatedDate)
    .HasDefaultValueSql("GETDATE()");

// Computed column
modelBuilder.Entity<RaceEntity>()
    .Property(r => r.FullName)
    .HasComputedColumnSql("[Year] + '-' + [Name]");
```

## Migration Commands

```powershell
# List migrations
dotnet ef migrations list

# Add new migration
dotnet ef migrations add <MigrationName>

# Update database to latest migration
dotnet ef database update

# Update to specific migration
dotnet ef database update <MigrationName>

# Remove last migration (not applied to database)
dotnet ef migrations remove

# Generate SQL script
dotnet ef migrations script

# Generate SQL from one migration to another
dotnet ef migrations script <FromMigration> <ToMigration>

# Drop database
dotnet ef database drop
```

## Performance Tips

### 1. Use AsNoTracking for Read-Only Queries
```csharp
var races = context.Races.AsNoTracking().ToList();
```

### 2. Avoid N+1 Queries with Include
```csharp
// Bad: N+1 query problem
var classifications = context.Classifications.ToList();
foreach (var c in classifications)
{
    var raceName = c.Race.Name; // Lazy load for each item
}

// Good: Single query with Include
var classifications = context.Classifications
    .Include(c => c.Race)
    .ToList();
```

### 3. Use Projection for Specific Fields
```csharp
// Instead of loading entire entity
var races = context.Races.ToList();

// Load only what you need
var raceNames = context.Races
    .Select(r => new { r.Id, r.Name })
    .ToList();
```

### 4. Batch Operations
```csharp
// Bad: Multiple SaveChanges calls
foreach (var item in items)
{
    context.Add(item);
    context.SaveChanges(); // Don't do this
}

// Good: Single SaveChanges call
context.AddRange(items);
context.SaveChanges();
```

## Common Patterns

### Repository Pattern (as in your code)
```csharp
public class RaceRepository
{
    public List<RaceEntity> GetAll()
    {
        using (var context = new RaceManagementContext())
        {
            return context.Races.ToList();
        }
    }
}
```

### Unit of Work Pattern
```csharp
public class UnitOfWork : IDisposable
{
    private readonly RaceManagementContext _context;
    private RaceRepository _raceRepository;
    
    public UnitOfWork()
    {
        _context = new RaceManagementContext();
    }
    
    public RaceRepository Races => _raceRepository ??= new RaceRepository(_context);
    
    public void Save()
    {
        _context.SaveChanges();
    }
    
    public void Dispose()
    {
        _context?.Dispose();
    }
}
```

## Troubleshooting Common Issues

### "The entity type requires a primary key"
**Solution:** Ensure entity has `[Key]` attribute or configure in OnModelCreating.

### "Cannot insert explicit value for identity column"
**Solution:** Don't set the Id property when adding new entities.

### "A second operation started on this context"
**Solution:** Don't reuse context instances across async operations or use separate context instances.

### "SQL Server doesn't exist or access denied"
**Solution:** Check connection string and ensure SQL Server LocalDB is installed.

## Resources

- [EF Core Documentation](https://docs.microsoft.com/ef/core/)
- [EF Core Query Documentation](https://docs.microsoft.com/ef/core/querying/)
- [EF Core Performance](https://docs.microsoft.com/ef/core/performance/)
