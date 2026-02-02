# DDD Layer Dependencies

```
┌─────────────────────────────────────────────────────────────┐
│                    PRESENTATION LAYER                        │
│  ┌────────────┐                                             │
│  │ Program.cs │  Entry point, DI setup, Configuration       │
│  └────────────┘                                             │
└─────────────────────────────────────────────────────────────┘
                           │
                           ├── depends on ──▶
                           │
┌─────────────────────────────────────────────────────────────┐
│                    APPLICATION LAYER                         │
│  ┌─────────────────────────┐  ┌─────────────────────────┐  │
│  │ RaceProcessingService   │  │ ReportGenerationService │  │
│  │ • ProcessAllRaces()     │  │ • GenerateReport()      │  │
│  └─────────────────────────┘  └─────────────────────────┘  │
│           Orchestrates domain logic                          │
└─────────────────────────────────────────────────────────────┘
                           │
                           ├── depends on ──▶
                           │
┌─────────────────────────────────────────────────────────────┐
│                      DOMAIN LAYER                            │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ ENTITIES                                             │   │
│  │  • Member      • Race      • RaceResult             │   │
│  └─────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ AGGREGATES                                           │   │
│  │  • Classification (root)                            │   │
│  │    - MemberClassification                           │   │
│  └─────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ VALUE OBJECTS                                        │   │
│  │  • RaceFileName                                     │   │
│  └─────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ DOMAIN SERVICES                                      │   │
│  │  • PointsCalculationService                         │   │
│  └─────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ REPOSITORY INTERFACES                                │   │
│  │  • IMemberRepository                                │   │
│  │  • IRaceResultRepository                            │   │
│  └─────────────────────────────────────────────────────┘   │
│                  Pure business logic                        │
└─────────────────────────────────────────────────────────────┘
                           ▲
                           │
                    implements interfaces
                           │
┌─────────────────────────────────────────────────────────────┐
│                  INFRASTRUCTURE LAYER                        │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ REPOSITORY IMPLEMENTATIONS                           │   │
│  │  • JsonMemberRepository                             │   │
│  │  • ExcelRaceResultRepository                        │   │
│  └─────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ SERVICES                                             │   │
│  │  • FileOutputService                                │   │
│  └─────────────────────────────────────────────────────┘   │
│         External dependencies & implementations              │
└─────────────────────────────────────────────────────────────┘

```

## Dependency Rules

1. **Presentation** → Application → Domain ← Infrastructure
2. **Domain layer** has NO dependencies on other layers
3. **Infrastructure** implements Domain interfaces
4. **Application** orchestrates Domain objects
5. Dependencies point INWARD (toward Domain)

## Key Benefits

✓ **Testable**: Mock repositories, test domain logic in isolation
✓ **Flexible**: Swap JSON for database without touching domain
✓ **Maintainable**: Clear separation of concerns
✓ **Business-focused**: Core logic protected from tech changes
