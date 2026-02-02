# Quick Start Guide - DDD Architecture

## Running the Application

The application works exactly as before, with the same command-line arguments:

```bash
# Run with defaults (current directory, Members.json)
NameParser.exe

# Specify custom directory
NameParser.exe "C:\RaceData"

# Specify custom directory and members file
NameParser.exe "C:\RaceData" "CustomMembers.json"
```

## Understanding the New Structure

### When you want to...

#### 1. Change how points are calculated
📁 **Edit**: `Domain/Services/PointsCalculationService.cs`
```csharp
public int CalculatePoints(TimeSpan referenceTime, TimeSpan memberTime)
{
    // Modify calculation logic here
    var points = Math.Round(referenceTime.TotalSeconds / memberTime.TotalSeconds * 1000);
    return (int)points;
}
```

#### 2. Change how members are loaded (e.g., from database instead of JSON)
📁 **Create new**: `Infrastructure/Repositories/SqlMemberRepository.cs`
```csharp
public class SqlMemberRepository : IMemberRepository
{
    public List<Member> GetAll()
    {
        // Load from SQL database instead of JSON
    }
}
```
Then update `Program.cs`:
```csharp
var memberRepository = new SqlMemberRepository(); // Changed!
```

#### 3. Change race result reading (e.g., from CSV instead of Excel)
📁 **Create new**: `Infrastructure/Repositories/CsvRaceResultRepository.cs`
```csharp
public class CsvRaceResultRepository : IRaceResultRepository
{
    public Dictionary<int, string> GetRaceResults(string filePath, List<Member> members)
    {
        // Read CSV instead of Excel
    }
}
```

#### 4. Add validation to Member entity
📁 **Edit**: `Domain/Entities/Member.cs`
```csharp
public Member(string firstName, string lastName, string email = null)
{
    if (string.IsNullOrWhiteSpace(lastName))
        throw new ArgumentException("Last name cannot be empty");
    
    // Add more validation
    if (firstName?.Length < 2)
        throw new ArgumentException("First name too short");
    
    FirstName = firstName;
    LastName = lastName;
    Email = email;
}
```

#### 5. Change report format
📁 **Edit**: `Application/Services/ReportGenerationService.cs`
```csharp
public string GenerateReport(Classification classification)
{
    // Change output format here
    // CSV, JSON, XML, etc.
}
```

#### 6. Add logging
📁 **Edit**: Any service
```csharp
public class RaceProcessingService
{
    public Classification ProcessAllRaces(IEnumerable<string> raceFiles)
    {
        Console.WriteLine($"Processing {raceFiles.Count()} files...");
        // Add logging where needed
    }
}
```

## Folder Navigation Guide

```
📦 Your Project
├── 📂 Domain/                      ← Business Rules
│   ├── 📂 Entities/                ← Core objects (Member, Race)
│   ├── 📂 Aggregates/              ← Complex business objects (Classification)
│   ├── 📂 ValueObjects/            ← Immutable values (RaceFileName)
│   ├── 📂 Services/                ← Business calculations (Points)
│   └── 📂 Repositories/            ← Data contracts (interfaces)
│
├── 📂 Application/                 ← Use Cases
│   └── 📂 Services/                ← Workflows (Processing, Reports)
│
├── 📂 Infrastructure/              ← Technical Stuff
│   ├── 📂 Repositories/            ← Data access (JSON, Excel)
│   └── 📂 Services/                ← File operations
│
├── 📂 Presentation/                ← User Interface
│   ├── Program.cs                  ← Start here!
│   └── ConsoleLogger.cs            ← Console output
│
└── 📄 StringExtensions.cs          ← Shared utilities
```

## Architecture at a Glance

```
Program.cs
    ↓ creates
Services (Application)
    ↓ uses
Domain Objects (Business Logic)
    ↓ needs data from
Repositories (Infrastructure)
```

## Common Tasks

### Task: Add a new race result source (e.g., Web API)

1. Create interface method (if needed):
   ```csharp
   // Domain/Repositories/IRaceResultRepository.cs
   Dictionary<int, string> GetRaceResultsFromApi(string url, List<Member> members);
   ```

2. Implement it:
   ```csharp
   // Infrastructure/Repositories/WebApiRaceResultRepository.cs
   public class WebApiRaceResultRepository : IRaceResultRepository
   {
       public Dictionary<int, string> GetRaceResultsFromApi(string url, List<Member> members)
       {
           // Call web API
       }
   }
   ```

3. Use it:
   ```csharp
   // Program.cs
   var raceResultRepository = new WebApiRaceResultRepository();
   ```

### Task: Change classification algorithm

📁 Edit: `Domain/Aggregates/Classification.cs`

The `AddOrUpdateResult` method contains the logic:
```csharp
public void AddOrUpdateResult(Member member, Race race, int points)
{
    // Modify how classifications are calculated and stored
}
```

### Task: Add new output format (e.g., Excel report)

1. Create new service:
   ```csharp
   // Application/Services/ExcelReportService.cs
   public class ExcelReportService
   {
       public void GenerateExcelReport(Classification classification, string filePath)
       {
           // Generate Excel file
       }
   }
   ```

2. Use in Program.cs:
   ```csharp
   var excelReportService = new ExcelReportService();
   excelReportService.GenerateExcelReport(classification, "output.xlsx");
   ```

## Debugging Tips

### Problem: Can't find where member data is loaded
👉 Look in: `Infrastructure/Repositories/JsonMemberRepository.cs`

### Problem: Points calculation seems wrong
👉 Look in: `Domain/Services/PointsCalculationService.cs`

### Problem: Race file parsing issues
👉 Look in: `Domain/ValueObjects/RaceFileName.cs`

### Problem: Classification not working
👉 Look in: `Domain/Aggregates/Classification.cs`

### Problem: Want to see processing flow
👉 Start in: `Presentation/Program.cs` → follow the service calls

## Testing Strategy

### Unit Tests (Recommended)
```csharp
[TestClass]
public class PointsCalculationServiceTests
{
    [TestMethod]
    public void CalculatePoints_WhenMemberFaster_ReturnsHigherPoints()
    {
        // Arrange
        var service = new PointsCalculationService();
        var refTime = TimeSpan.FromMinutes(30);
        var memberTime = TimeSpan.FromMinutes(25);
        
        // Act
        var points = service.CalculatePoints(refTime, memberTime);
        
        // Assert
        Assert.IsTrue(points > 1000);
    }
}
```

### Integration Tests
Test the full workflow:
```csharp
[TestMethod]
public void ProcessRace_WithValidFile_GeneratesClassification()
{
    // Setup test data
    // Run RaceProcessingService
    // Verify classification output
}
```

## Key Concepts

1. **Entities**: Objects with identity (Member, Race)
2. **Value Objects**: Objects defined by their values (RaceFileName)
3. **Aggregates**: Clusters of related objects (Classification)
4. **Services**: Operations that don't belong to a single object
5. **Repositories**: Abstract data access
6. **Dependency Inversion**: High-level code doesn't depend on low-level details

## Further Reading

- `DDD_ARCHITECTURE.md` - Detailed architecture explanation
- `MIGRATION_GUIDE.md` - How we got from old to new code
- `ARCHITECTURE_DIAGRAM.md` - Visual representation

## Need Help?

1. Check which layer handles your concern (see folder structure above)
2. Read the relevant service/entity
3. Refer to architecture documentation
4. Remember: Business logic → Domain, Data access → Infrastructure, Workflows → Application
