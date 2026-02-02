# Domain-Driven Design Architecture

This application has been refactored to follow Domain-Driven Design (DDD) principles with a clear separation of concerns across multiple layers.

## Project Structure

```
NameParser/
├── Domain/                          # Core business logic
│   ├── Entities/                    # Domain entities
│   │   ├── Member.cs               # Member entity with business rules
│   │   ├── Race.cs                 # Race entity
│   │   └── RaceResult.cs           # Race result entity
│   ├── ValueObjects/                # Immutable value objects
│   │   └── RaceFileName.cs         # Value object for race file naming
│   ├── Aggregates/                  # Aggregate roots
│   │   └── Classification.cs       # Classification aggregate
│   ├── Services/                    # Domain services
│   │   └── PointsCalculationService.cs
│   └── Repositories/                # Repository interfaces
│       ├── IMemberRepository.cs
│       └── IRaceResultRepository.cs
│
├── Application/                     # Application orchestration
│   └── Services/
│       ├── RaceProcessingService.cs  # Main race processing logic
│       └── ReportGenerationService.cs # Report generation logic
│
├── Infrastructure/                  # External concerns
│   ├── Repositories/                # Repository implementations
│   │   ├── JsonMemberRepository.cs
│   │   └── ExcelRaceResultRepository.cs
│   └── Services/
│       └── FileOutputService.cs
│
└── Presentation/                    # UI layer
    ├── Program.cs                   # Entry point
    └── ConsoleLogger.cs             # Console logging
```

## Layer Responsibilities

### 1. Domain Layer
**Purpose**: Contains all business logic and rules.

- **Entities**: Objects with identity that persist over time
  - `Member`: Represents a race participant
  - `Race`: Represents a race event
  - `RaceResult`: Represents a member's result in a race

- **Value Objects**: Immutable objects defined by their attributes
  - `RaceFileName`: Encapsulates race file naming convention

- **Aggregates**: Clusters of entities treated as a single unit
  - `Classification`: Manages all member classifications across races

- **Domain Services**: Business logic that doesn't belong to a single entity
  - `PointsCalculationService`: Calculates race points based on time

- **Repository Interfaces**: Define data access contracts
  - No implementation details, only contracts

### 2. Application Layer
**Purpose**: Orchestrates domain objects to fulfill use cases.

- `RaceProcessingService`: Coordinates race file processing
- `ReportGenerationService`: Generates final reports

This layer:
- Depends only on Domain layer
- Orchestrates domain objects
- Implements use cases
- No business rules (those belong in Domain)

### 3. Infrastructure Layer
**Purpose**: Implements technical concerns.

- **Repository Implementations**:
  - `JsonMemberRepository`: Loads members from JSON
  - `ExcelRaceResultRepository`: Reads race results from Excel

- **Services**:
  - `FileOutputService`: File I/O operations

This layer:
- Implements Domain repository interfaces
- Handles external dependencies (files, databases, APIs)
- Contains framework-specific code

### 4. Presentation Layer
**Purpose**: User interface and program entry point.

- `Program.cs`: Main entry point, dependency injection, configuration
- `ConsoleLogger`: Console output management

## Key DDD Patterns Used

### 1. **Entities**
Objects with unique identity:
```csharp
public class Member
{
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    // Identity based on name combination
}
```

### 2. **Value Objects**
Immutable objects defined by their values:
```csharp
public class RaceFileName
{
    public int RaceNumber { get; private set; }
    public int DistanceKm { get; private set; }
    public string RaceName { get; private set; }
}
```

### 3. **Aggregates**
Clusters of objects with a root:
```csharp
public class Classification  // Aggregate Root
{
    private readonly Dictionary<string, MemberClassification> _classifications;
    // Controls all access to MemberClassification objects
}
```

### 4. **Repository Pattern**
Abstracts data access:
```csharp
public interface IMemberRepository
{
    List<Member> GetAll();
}
```

### 5. **Domain Services**
Business logic that spans multiple entities:
```csharp
public class PointsCalculationService
{
    public int CalculatePoints(TimeSpan referenceTime, TimeSpan memberTime)
    {
        // Business rule implementation
    }
}
```

### 6. **Dependency Inversion**
High-level modules don't depend on low-level modules:
- Application layer depends on Domain interfaces
- Infrastructure implements Domain interfaces
- Domain has no dependencies on other layers

## Benefits of This Architecture

1. **Testability**: Each layer can be tested independently
2. **Maintainability**: Clear separation of concerns
3. **Flexibility**: Easy to swap implementations (e.g., change from JSON to database)
4. **Business Logic Protection**: Core logic isolated from infrastructure changes
5. **Scalability**: Easy to add new features following established patterns

## How to Use

```csharp
// Manual dependency injection in Program.cs
var memberRepository = new JsonMemberRepository("Members.json");
var raceResultRepository = new ExcelRaceResultRepository();
var pointsCalculationService = new PointsCalculationService();

var raceProcessingService = new RaceProcessingService(
    memberRepository,
    raceResultRepository,
    pointsCalculationService);

var classification = raceProcessingService.ProcessAllRaces(raceFiles);
```

## Future Improvements

1. **Add IoC Container**: Use dependency injection framework (e.g., Microsoft.Extensions.DependencyInjection)
2. **Add Unit Tests**: Test domain logic independently
3. **Add Specifications Pattern**: For complex queries
4. **Add Events**: Domain events for loose coupling
5. **Add CQRS**: Separate read and write models if needed
6. **Add Validation**: FluentValidation for input validation
7. **Add Logging**: Structured logging with Serilog

## Migration from Old Code

The old monolithic `Program.cs` has been split into:
- Domain logic → Domain layer
- Data access → Infrastructure repositories
- Business workflows → Application services
- Entry point → Presentation layer

All existing functionality is preserved while gaining architectural benefits.
