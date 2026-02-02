# .NET 8.0 Upgrade Plan

**Solution**: NameParser.sln  
**Current Framework**: .NET Framework 4.8  
**Target Framework**: .NET 8.0 (LTS)  
**Branch**: upgrade-to-NET8  
**Date**: 2024

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Migration Strategy](#migration-strategy)
3. [Detailed Dependency Analysis](#detailed-dependency-analysis)
4. [Implementation Timeline](#implementation-timeline)
5. [Detailed Execution Steps](#detailed-execution-steps)
6. [Project-by-Project Migration Plans](#project-by-project-migration-plans)
7. [Package Update Reference](#package-update-reference)
8. [Breaking Changes Catalog](#breaking-changes-catalog)
9. [Risk Management](#risk-management)
10. [Testing & Validation Strategy](#testing--validation-strategy)
11. [Complexity & Effort Assessment](#complexity--effort-assessment)
12. [Source Control Strategy](#source-control-strategy)
13. [Success Criteria](#success-criteria)

---

## Executive Summary

### Scope

This plan outlines the upgrade of the **NameParser solution** from .NET Framework 4.8 to .NET 8.0 (Long Term Support). The solution contains:

- **2 projects**: NameParser (Console App) and NameParser.UI (WPF App)
- **~2,000 lines of code**
- **10 unique NuGet packages** requiring updates
- **1 project dependency**: NameParser.UI depends on NameParser

### Selected Strategy

**All-At-Once Strategy** - All projects upgraded simultaneously in a single coordinated operation.

**Rationale**: 
- Small solution (2 projects only)
- Clear, linear dependency structure (no circular dependencies)
- Both projects currently on .NET Framework 4.8
- All packages have known compatible versions for .NET 8.0
- Coordinated upgrade minimizes complexity and enables single validation cycle

### Complexity Assessment

**Discovered Metrics:**
- Project count: 2
- Dependency depth: 1 level
- High-risk factors: 1 (Entity Framework Core 3.1 → 8.0 upgrade)
- Package conflicts: 2 (duplicate Microsoft.Extensions.Primitives, EF Core Tools version mismatch)
- Security vulnerabilities: None

**Complexity Classification: SIMPLE**

The solution structure is straightforward with clear separation between core logic (NameParser) and UI layer (NameParser.UI). The main challenge is the Entity Framework Core version jump from 3.1 to 8.0, which spans 4 major versions.

### Critical Issues

1. **Entity Framework Core 3.1 End-of-Life**: Must upgrade to EF Core 8.0, requires code review for breaking changes
2. **Package Conflicts**: Duplicate Microsoft.Extensions.Primitives references (3.1.32 and 10.0.2)
3. **Legacy Project Format**: Both projects use old-style .csproj requiring conversion to SDK-style
4. **Package Management**: Mixed PackageReference and packages.config formats

### Expected Iterations

This plan uses a **fast batch approach**:
- Phase 1: Discovery & Classification (3 iterations) - Complete
- Phase 2: Foundation (3 iterations) - Next
- Phase 3: Detail Generation (2-3 iterations) - Batch all projects together

### Recommended Approach

**All-at-once simultaneous upgrade** - Update both projects, all packages, and resolve all compilation errors in a single coordinated operation, followed by comprehensive testing.

---

## Migration Strategy

### Approach Selection

**Selected: All-At-Once Strategy**

All projects in the solution will be upgraded simultaneously in a single coordinated operation. All project files are updated to `net8.0-windows`, all packages updated to target versions, and all compilation errors resolved in one atomic upgrade cycle.

### Justification

This strategy is ideal for the NameParser solution because:

1. **Small Solution Size**: Only 2 projects make coordination manageable
2. **Simple Dependency Structure**: Linear dependency (NameParser → NameParser.UI) with no circular references
3. **Homogeneous Starting Point**: Both projects on .NET Framework 4.8
4. **Package Compatibility**: All packages have known .NET 8.0-compatible versions
5. **Unified Testing**: Single validation cycle for the entire solution is more efficient than phased testing

### All-At-Once Strategy Rationale

**Advantages for this solution:**
- Fastest completion time (single upgrade cycle)
- No multi-targeting complexity
- Clean dependency resolution (all packages align at once)
- Both projects benefit from .NET 8.0 improvements immediately
- Simpler coordination (no intermediate states to maintain)

**Risk Mitigation:**
- Comprehensive build → fix → rebuild cycle
- Thorough testing of Entity Framework Core functionality
- Excel processing validation
- UI functionality validation

### Dependency-Based Ordering

While both projects are updated simultaneously, the **execution order within the atomic upgrade** respects dependencies:

1. **Update all project files** (TargetFramework properties)
2. **Update all package references** (across both projects)
3. **Restore dependencies** (dotnet restore)
4. **Build solution** to identify compilation errors
5. **Fix all compilation errors** (EF Core breaking changes, obsolete APIs, etc.)
6. **Rebuild solution** to verify fixes
7. **Verify**: Solution builds with 0 errors

### Parallel vs Sequential Execution

**Execution Model**: Atomic coordinated update

- All project file changes are made together
- All package updates applied together
- Build cycle processes entire solution at once
- Error fixes address all projects in the same operation

This is **not** a sequential project-by-project approach. The upgrade treats both projects as a unified system.

### Phase Definitions

**Phase 0: Prerequisites** (if applicable)
- Verify .NET 8.0 SDK installation
- Validate SQL Server compatibility for EF Core 8.0
- Backup current solution state

**Phase 1: Atomic Upgrade**
- Convert both projects to SDK-style
- Update all TargetFramework properties
- Update all package references
- Restore, build, fix compilation errors, rebuild

**Phase 2: Validation**
- Execute comprehensive testing
- Validate EF Core data access
- Validate Excel processing
- Validate UI functionality
- Address any test failures

### Special Considerations

#### Entity Framework Core 3.1 → 8.0

This upgrade spans **4 major versions** (4.0, 5.0, 6.0, 7.0, 8.0). Key areas requiring attention:

- **Query behavior**: Split query is now default for collections
- **Tracking**: Changes in change detection and tracking behavior
- **Migrations**: May need regeneration
- **SQL Server**: Minimum version requirements
- **Value converters**: Behavior changes for certain types

**Mitigation**: Detailed review of breaking changes documentation, comprehensive testing of all data access code.

#### Project Format Conversion

Both projects use **legacy .csproj format** and must be converted to SDK-style:

- **NameParser**: `ToolsVersion="4.0"`, uses `<Reference>` elements
- **NameParser.UI**: `ToolsVersion="15.0"`, uses packages.config

**Conversion approach**: Manual conversion recommended to ensure clean output and proper handling of:
- Content files with `CopyToOutputDirectory`
- WPF-specific elements
- Newtonsoft.Json HintPath reference

#### Windows-Only Target Framework

Both projects **must use `net8.0-windows`** (not `net8.0`) due to Windows-specific dependencies:
- WPF (NameParser.UI)
- Windows Forms (NameParser)
- COM Interop for Excel (NameParser)

### Success Criteria for Strategy

The All-At-Once strategy is successful when:
- Both projects build without errors in a single solution build
- All packages resolve without conflicts
- All tests pass in unified test run
- No intermediate states or multi-targeting required
- Single commit captures the entire upgrade

---

## Detailed Dependency Analysis

### Dependency Graph

```
NameParser (Console App - net48 → net8.0-windows)
    ↑
    └── NameParser.UI (WPF App - net48 → net8.0-windows)
```

**Migration Order**: NameParser → NameParser.UI (bottom-up, respecting dependency direction)

**Critical Path**: NameParser is the foundation - it must be successfully converted and validated before NameParser.UI can be upgraded.

### Project Groupings

Since this is an **All-At-Once Strategy**, all projects will be upgraded simultaneously in a single atomic operation:

**Single Upgrade Phase**:
- NameParser.csproj
- NameParser.UI.csproj

Both projects will have their target frameworks and packages updated together, followed by a unified build and fix cycle.

### Dependency Details

#### NameParser (0 project dependencies)

**Package Dependencies** (9 packages):
- ExcelDataReader 3.8.0
- ExcelDataReader.DataSet 3.7.0
- Microsoft.EntityFrameworkCore 3.1.32
- Microsoft.EntityFrameworkCore.SqlServer 3.1.32
- Microsoft.EntityFrameworkCore.Tools 10.0.2
- Microsoft.Extensions.Primitives 10.0.2 (duplicate)
- Microsoft.Extensions.Primitives 3.1.32 (duplicate)
- Microsoft.Office.Interop.Excel 16.0.18925
- System.Configuration.ConfigurationManager 10.0.2

**Framework References** (legacy - will be removed in SDK conversion):
- Microsoft.CSharp, System, System.Core, System.Data
- System.Windows, System.Windows.Forms

**Special Note**: Newtonsoft.Json is referenced via HintPath, not proper PackageReference

#### NameParser.UI (1 project dependency)

**Package Dependencies** (1 package):
- Newtonsoft.Json 13.0.4 (via packages.config)

**Project Dependencies**:
- NameParser.csproj

**Framework References** (legacy - will be removed in SDK conversion):
- WPF: WindowsBase, PresentationCore, PresentationFramework, System.Xaml
- BCL: System, System.Core, System.Data, System.Xml, System.Net.Http, etc.
- System.Windows.Forms

### Circular Dependency Analysis

**Result**: None detected. Clean hierarchical structure.

### Windows-Specific Considerations

Both projects require **`net8.0-windows`** target framework moniker (not `net8.0`) due to:
- WPF usage (NameParser.UI)
- System.Windows.Forms usage (NameParser)
- COM Interop for Microsoft.Office.Interop.Excel (NameParser)

This restricts the solution to Windows-only deployment.

---

## Implementation Timeline

### Phase 0: Prerequisites

**Objective**: Ensure environment readiness

**Operations**:
- Verify .NET 8.0 SDK installed (`dotnet --list-sdks`)
- Verify SQL Server version compatible with EF Core 8.0 (SQL Server 2012+)
- Document current build process
- Review and backup existing EF Core migrations

**Deliverables**: 
- Environment validated and ready
- Current state documented

**Estimated Duration**: Minimal (verification steps)

---

### Phase 1: Atomic Upgrade

**Objective**: Upgrade both projects simultaneously to .NET 8.0

**Operations** (performed as single coordinated batch):

1. **Convert project files to SDK-style**
   - NameParser.csproj
   - NameParser.UI.csproj

2. **Update all TargetFramework properties**
   - Both projects: `net48` → `net8.0-windows`

3. **Update all package references**
   - Align Entity Framework Core packages to 8.0.11
   - Remove duplicate Microsoft.Extensions.Primitives
   - Update all other packages to recommended versions
   - Convert Newtonsoft.Json to proper PackageReference

4. **Restore dependencies**
   - `dotnet restore`

5. **Build solution and fix all compilation errors**
   - Focus on EF Core breaking changes
   - Address obsolete API usage
   - Fix namespace changes
   - Resolve configuration updates

6. **Rebuild to verify**
   - `dotnet build` with 0 errors

**Deliverables**: 
- Solution builds successfully with 0 errors
- All packages at target versions
- No dependency conflicts

**Estimated Duration**: 8-12 hours (includes error fixing)

---

### Phase 2: Test Validation

**Objective**: Validate functionality and address test failures

**Operations**:

1. **Data access validation**
   - Test EF Core queries (CRUD operations)
   - Validate migrations
   - Test complex queries and joins
   - Verify connection strings

2. **Excel processing validation**
   - Test ExcelDataReader functionality
   - Test Office Interop operations (if used)
   - Validate file I/O

3. **UI validation**
   - Launch NameParser.UI application
   - Test all user interactions
   - Validate data binding
   - Check for visual rendering issues

4. **Integration testing**
   - End-to-end workflow testing
   - Performance validation
   - Error handling verification

5. **Address failures**
   - Fix any test failures discovered
   - Adjust EF Core queries if needed
   - Resolve runtime issues

**Deliverables**: 
- All tests pass
- Application runs successfully
- No functional regressions

**Estimated Duration**: 4-7 hours

---

### Total Timeline

- **Phase 0**: < 1 hour
- **Phase 1**: 8-12 hours
- **Phase 2**: 4-7 hours
- **Total**: 12-19 hours

**Calendar Time** (with All-At-Once): 1-3 days depending on issue discovery and resolution time.

---

## Detailed Execution Steps

This section provides the step-by-step execution sequence for the All-At-Once upgrade. All steps are performed in a single coordinated operation.

---

### Step 1: Convert Projects to SDK-Style

**Projects to Convert:**
- NameParser.csproj
- NameParser.UI.csproj

**Conversion Actions:**

#### NameParser.csproj

**Current Format**: Legacy .NET Framework format (`ToolsVersion="4.0"`)

**Target Format**: SDK-style project

**Key Transformations:**
1. Replace `<Project ToolsVersion="4.0" ...>` with `<Project Sdk="Microsoft.NET.Sdk">`
2. Remove all `<Reference>` elements (BCL references auto-included in SDK-style)
3. Convert inline `<PackageReference>` to clean format (remove Version attributes, use element content)
4. Add `<TargetFramework>net8.0-windows</TargetFramework>`
5. Add `<OutputType>Exe</OutputType>`
6. Remove `<Import>` elements (SDK handles automatically)
7. Preserve `<Content>` items with `CopyToOutputDirectory`
8. Remove `AssemblyInfo.cs` properties (SDK auto-generates)
9. Add `<UseWindowsForms>true</UseWindowsForms>` (for System.Windows.Forms usage)

**Special Handling:**
- Newtonsoft.Json: Convert HintPath reference to proper PackageReference
- Content files: Preserve `CopyToOutputDirectory="Always"` for data files
- app.config: Keep as content file or migrate to appsettings.json

#### NameParser.UI.csproj

**Current Format**: Legacy WPF project (`ToolsVersion="15.0"`)

**Target Format**: SDK-style WPF project

**Key Transformations:**
1. Replace `<Project ToolsVersion="15.0" ...>` with `<Project Sdk="Microsoft.NET.Sdk">`
2. Add `<TargetFramework>net8.0-windows</TargetFramework>`
3. Add `<OutputType>WinExe</OutputType>`
4. Add `<UseWPF>true</UseWPF>`
5. Add `<UseWindowsForms>true</UseWindowsForms>` (if System.Windows.Forms used)
6. Remove all `<Reference>` elements
7. Remove `ProjectTypeGuids` (SDK handles WPF automatically)
8. Convert packages.config to PackageReference for Newtonsoft.Json
9. Remove explicit XAML file listings (SDK auto-includes)
10. Preserve `<ProjectReference>` to NameParser

**Expected Outcome**: Both projects use modern SDK-style format, ready for .NET 8.0 targeting

---

### Step 2: Update All Project TargetFramework Properties

**Update in all projects:**

```xml
<TargetFramework>net8.0-windows</TargetFramework>
```

**Projects:**
- NameParser.csproj
- NameParser.UI.csproj

**Why `net8.0-windows`**: Required for:
- WPF (NameParser.UI)
- System.Windows.Forms (NameParser)
- COM Interop (Microsoft.Office.Interop.Excel)

**Verification**: Check both .csproj files contain `net8.0-windows` as TargetFramework

---

### Step 3: Update All Package References

See **§Package Update Reference** for complete package-by-package matrix.

**Key Package Updates (affecting NameParser):**

| Package | Current | Target | Reason |
|---------|---------|--------|--------|
| Microsoft.EntityFrameworkCore | 3.1.32 | 8.0.11 | Framework alignment, end-of-life |
| Microsoft.EntityFrameworkCore.SqlServer | 3.1.32 | 8.0.11 | Match EF Core version |
| Microsoft.EntityFrameworkCore.Tools | 10.0.2 | 8.0.11 | Match EF Core version, resolve conflict |
| Microsoft.Extensions.Primitives | 3.1.32/10.0.2 | 8.0.0 | Remove duplicate, single version |
| ExcelDataReader | 3.8.0 | 3.8.2 | Bug fixes |
| ExcelDataReader.DataSet | 3.7.0 | 3.8.0 | Align with ExcelDataReader |
| System.Configuration.ConfigurationManager | 10.0.2 | 8.0.1 | Framework alignment |
| Newtonsoft.Json | HintPath | 13.0.3 | Proper PackageReference |

**Key Package Updates (affecting NameParser.UI):**

| Package | Current | Target | Reason |
|---------|---------|--------|--------|
| Newtonsoft.Json | 13.0.4 | 13.0.3 | Convert from packages.config to PackageReference |

**Critical Actions:**
1. **Remove duplicate Microsoft.Extensions.Primitives** - keep only 8.0.0 version
2. **Align all EF Core packages to 8.0.11** - critical for compatibility
3. **Delete packages.config** in NameParser.UI after conversion

---

### Step 4: Restore Dependencies

**Command**: `dotnet restore NameParser.sln`

**Expected Outcome**: All packages download successfully, no conflicts reported

**Troubleshooting**: If restore fails, check for:
- Conflicting package versions
- Missing package sources
- Network connectivity

---

### Step 5: Build Solution to Identify Errors

**Command**: `dotnet build NameParser.sln`

**Expected Outcome**: Build completes, compilation errors reported (expected on first build after upgrade)

**Error Categories to Expect:**

1. **EF Core Breaking Changes**
   - Changed method signatures
   - Obsolete APIs
   - Query behavior changes

2. **Namespace Changes**
   - Moved types
   - Removed obsolete namespaces

3. **Configuration Changes**
   - ConfigurationManager usage (likely compatible)

4. **Windows Forms / WPF**
   - Unlikely, but check for deprecated APIs

---

### Step 6: Fix All Compilation Errors

See **§Breaking Changes Catalog** for comprehensive list of expected issues and fixes.

**Focus Areas:**

#### Entity Framework Core (High Priority)

**File**: `Infrastructure/Data/RaceManagementContext.cs`

**Potential Issues:**
- `OnConfiguring` / `OnModelCreating` signature changes
- Connection string handling
- Migration configuration

**Fixes**: Review EF Core 8.0 breaking changes, update DbContext configuration

---

**Files**: `Infrastructure/Data/ClassificationRepository.cs`, `RaceRepository.cs`

**Potential Issues:**
- `.Include()` / `.ThenInclude()` behavior changes
- Query splitting (now default for collections)
- Tracking behavior changes
- Async method changes

**Fixes**:
- Add `.AsSplitQuery()` or `.AsSingleQuery()` if needed
- Update query patterns to EF Core 8.0 recommendations
- Check for removed LINQ methods

---

**Files**: `Infrastructure/Data/Models/*.cs`

**Potential Issues:**
- Value converter changes
- Navigation property configuration

**Fixes**: Review entity configurations, update as needed

---

#### Configuration System

**File**: `Program.cs`

**Potential Issues**:
- ConfigurationManager API is compatible via package (likely no changes)

**Verification**: Test configuration loading at runtime

---

#### Excel Processing

**Files**: `Infrastructure/Repositories/ExcelRaceResultRepository.cs`

**Potential Issues**: Unlikely (ExcelDataReader API stable)

**Verification**: Runtime testing required

---

#### General Obsolescence

**All Files**:

**Potential Issues**:
- Obsolete APIs with `[Obsolete]` attribute
- Deprecated methods

**Fixes**: Use IDE suggestions or breaking changes docs to find replacements

---

### Step 7: Rebuild Solution to Verify Fixes

**Command**: `dotnet build NameParser.sln`

**Expected Outcome**: Build succeeds with **0 errors**

**Success Criteria**:
- 0 compilation errors
- Warnings acceptable (review for severity)
- All projects build successfully

**If Errors Remain**: Return to Step 6, address remaining errors

---

### Step 8: Verify Build Success

**Verification Checklist**:
- [ ] `dotnet build NameParser.sln` completes with exit code 0
- [ ] No compilation errors reported
- [ ] Both NameParser.exe and NameParser.UI.exe generated
- [ ] All dependencies resolved
- [ ] No package restore warnings

**Deliverable**: Clean build of entire solution on .NET 8.0

---

### Step 9: Execute Comprehensive Tests

See **§Testing & Validation Strategy** for full test plan.

**Test Execution Order**:

1. **Data Access Tests** (Priority: HIGH)
   - CRUD operations for all entities (Classification, Race, RaceResult, Member)
   - Complex queries with joins
   - Transactions
   - Connection string validation

2. **Excel Processing Tests** (Priority: HIGH)
   - Read Excel files with ExcelDataReader
   - Process race data from files
   - Handle malformed files gracefully

3. **Business Logic Tests** (Priority: MEDIUM)
   - Classification calculations
   - Points calculation
   - Report generation

4. **UI Tests** (Priority: MEDIUM)
   - Launch NameParser.UI
   - User interaction flows
   - Data binding validation
   - Visual rendering checks

5. **Integration Tests** (Priority: MEDIUM)
   - End-to-end workflows
   - Performance validation

**Expected Outcome**: All tests pass, or failures identified for fixing

---

### Step 10: Address Test Failures

**For Each Failure**:

1. **Categorize**: EF Core query issue, runtime error, UI problem, etc.
2. **Diagnose**: Review error messages, stack traces, breaking changes docs
3. **Fix**: Apply code changes
4. **Rebuild**: `dotnet build`
5. **Retest**: Verify fix resolves issue
6. **Regression Check**: Ensure fix doesn't break other areas

**Common Failure Categories**:

- **EF Core Query Behavior**: Apply `.AsSingleQuery()` or `.AsSplitQuery()`
- **Migration Issues**: Regenerate migrations if schema problems occur
- **Value Converter Issues**: Update value converter implementations
- **Office Interop**: Check COM registration, Office installation

---

### Step 11: Final Validation

**Comprehensive Validation Checklist**:
- [ ] Solution builds with 0 errors
- [ ] All tests pass
- [ ] Application launches successfully
- [ ] Data access operations work correctly
- [ ] Excel file processing functional
- [ ] UI renders correctly
- [ ] No performance regressions
- [ ] No memory leaks or resource issues

**Deliverable**: Fully functional .NET 8.0 solution

---

### Execution Notes

**Single Pass Approach**: Steps 1-7 represent a single coordinated upgrade pass. This is **not** a retry loop - all project updates and package updates are done together, then errors are fixed systematically.

**Error Fixing Scope**: Step 6 addresses **all compilation errors across both projects** in the same operation, referencing the breaking changes catalog.

**Testing Scope**: Steps 9-10 test **entire solution** together, validating the complete upgrade.

---

## Project-by-Project Migration Plans

This section provides detailed specifications for each project in the solution. While the All-At-Once strategy upgrades both projects simultaneously, these specifications provide comprehensive details for each project's unique requirements.

---

### Project: NameParser

**Project Path**: `NameParser\NameParser.csproj`

**Current State**:
- **Target Framework**: net48 (.NET Framework 4.8)
- **Project Format**: Legacy (ToolsVersion="4.0")
- **Output Type**: Exe (Console Application)
- **LOC**: ~1,500
- **Package Count**: 9 packages (with duplicates and conflicts)
- **Project Dependencies**: None
- **Key Technologies**: Entity Framework Core 3.1, ExcelDataReader, Office Interop, JSON serialization

**Target State**:
- **Target Framework**: net8.0-windows
- **Project Format**: SDK-style
- **Output Type**: Exe
- **Package Count**: 8 packages (duplicates removed, versions aligned)

**Architecture**:
- **Domain Layer**: Aggregates, Entities, Repositories (interfaces), Services, Value Objects
- **Application Layer**: RaceProcessingService, ReportGenerationService
- **Infrastructure Layer**: Data (EF Core DbContext, repositories, entities), External repositories (Excel, JSON), Services (file I/O)
- **Presentation Layer**: ConsoleLogger
- **Entry Point**: Program.cs

**Migration Steps**:

#### 1. Prerequisites
- Verify .NET 8.0 SDK installed
- Backup existing EF Core migrations folder
- Document current connection strings
- Verify SQL Server version (2012+ required for EF Core 8.0)

#### 2. Project File Conversion

**Convert to SDK-Style Project**:

Replace legacy format with:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <RootNamespace>NameParser</RootNamespace>
    <AssemblyName>NameParser</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <!-- Package references here -->
  </ItemGroup>

  <ItemGroup>
    <!-- Content files with CopyToOutputDirectory -->
    <Content Include="Challenge.json" CopyToOutputDirectory="Always" />
    <Content Include="Contact Information (Responses).xlsx" CopyToOutputDirectory="Always" />
    <Content Include="Courses\**\*.xlsx" CopyToOutputDirectory="Always" />
    <!-- Preserve other content files as needed -->
  </ItemGroup>
</Project>
```

**Key Changes**:
- Remove all `<Reference>` elements (BCL references auto-included)
- Remove `<Compile>` elements (SDK auto-includes .cs files)
- Remove `AssemblyInfo.cs` or disable auto-generation if keeping manual assembly attributes
- Preserve content files with explicit `CopyToOutputDirectory`
- Convert Newtonsoft.Json HintPath to PackageReference
- Keep app.config as content file or migrate to modern configuration

#### 3. Package Updates

**Remove Duplicates**:
- Microsoft.Extensions.Primitives: Remove both 3.1.32 and 10.0.2, add single 8.0.0 version

**Update to Target Versions**:

```xml
<PackageReference Include="ExcelDataReader" Version="3.8.2" />
<PackageReference Include="ExcelDataReader.DataSet" Version="3.8.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.11" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.11" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.11">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
<PackageReference Include="Microsoft.Extensions.Primitives" Version="8.0.0" />
<PackageReference Include="Microsoft.Office.Interop.Excel" Version="16.0.18925.20022" />
<PackageReference Include="System.Configuration.ConfigurationManager" Version="8.0.1" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
```

**Critical**: All Entity Framework Core packages **must be 8.0.11** (aligned versions)

#### 4. Expected Breaking Changes

**Entity Framework Core 3.1 → 8.0**:

**File**: `Infrastructure/Data/RaceManagementContext.cs`
- **Issue**: DbContext configuration API changes
- **Fix**: Review `OnConfiguring` and `OnModelCreating` for deprecated patterns
- **Breaking Change**: Connection string format, provider configuration

**Files**: `Infrastructure/Data/ClassificationRepository.cs`, `RaceRepository.cs`
- **Issue**: Split query is now default for collection navigation
- **Fix**: Explicitly use `.AsSingleQuery()` if old behavior desired, or `.AsSplitQuery()` to make explicit
- **Breaking Change**: Query translation differences, tracking behavior

**Files**: `Infrastructure/Data/Models/*.cs`
- **Issue**: Entity configuration changes, value converter behavior
- **Fix**: Review entity configurations, update value converters if used

**Configuration System**:
- **File**: `Program.cs`
- **Issue**: ConfigurationManager usage (likely compatible)
- **Fix**: Minimal changes expected, verify at runtime

**Excel Processing**:
- **File**: `Infrastructure/Repositories/ExcelRaceResultRepository.cs`
- **Issue**: ExcelDataReader API (likely compatible)
- **Fix**: Runtime testing required

**General**:
- **Issue**: Obsolete API usage
- **Fix**: Use IDE suggestions or [Obsolete] attribute messages to find replacements

#### 5. Code Modifications

**Focus Areas**:

1. **EF Core Queries**: Review all LINQ queries for breaking changes
2. **Async Patterns**: Verify async/await patterns follow .NET 8.0 best practices
3. **Dispose Patterns**: Ensure proper disposal with SDK-style (often automatic)
4. **Configuration**: Test ConfigurationManager.AppSettings access

**Expected Changes**:
- Query behavior adjustments (`.AsSingleQuery()` or `.AsSplitQuery()`)
- Replace obsolete EF Core APIs
- Update migration code if regenerating migrations

#### 6. Testing Strategy

**Unit Testing** (if tests exist):
- Test all repository methods (CRUD operations)
- Test domain services (PointsCalculationService)
- Test value objects (RaceFileName parsing)

**Integration Testing**:
- Full database round-trip (create, read, update, delete)
- Complex queries with joins
- Transaction handling
- Excel file reading (sample files)
- JSON serialization/deserialization

**Manual Testing**:
- Run console application
- Process race files end-to-end
- Verify output files generated correctly
- Check database state after operations

#### 7. Validation Checklist

- [ ] Project builds without errors
- [ ] Project builds without warnings
- [ ] All EF Core migrations compatible (or regenerated)
- [ ] Database connection successful
- [ ] All CRUD operations work
- [ ] Excel file processing functional
- [ ] JSON serialization functional
- [ ] Configuration loading works
- [ ] No package dependency conflicts
- [ ] Performance acceptable
- [ ] No memory leaks

**Success Criteria**: NameParser functions identically to .NET Framework 4.8 version, with all tests passing.

---

### Project: NameParser.UI

**Project Path**: `NameParser.UI\NameParser.UI.csproj`

**Current State**:
- **Target Framework**: net48 (.NET Framework 4.8)
- **Project Format**: Legacy WPF (ToolsVersion="15.0")
- **Output Type**: WinExe (WPF Application)
- **LOC**: ~500
- **Package Count**: 1 package (Newtonsoft.Json via packages.config)
- **Project Dependencies**: NameParser.csproj
- **Key Technologies**: WPF, XAML, data binding

**Target State**:
- **Target Framework**: net8.0-windows
- **Project Format**: SDK-style WPF
- **Output Type**: WinExe
- **Package Count**: 1 package (Newtonsoft.Json via PackageReference)

**Migration Steps**:

#### 1. Prerequisites
- Verify NameParser project successfully upgraded (dependency requirement)
- Review XAML files for deprecated patterns
- Document current UI workflows

#### 2. Project File Conversion

**Convert to SDK-Style WPF Project**:

Replace legacy format with:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <UseWindowsForms>true</UseWindowsForms>
    <RootNamespace>NameParser.UI</RootNamespace>
    <AssemblyName>NameParser.UI</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\NameParser\NameParser.csproj" />
  </ItemGroup>
</Project>
```

**Key Changes**:
- Remove `ProjectTypeGuids` (SDK handles WPF automatically)
- Remove all `<Reference>` elements (WPF assemblies auto-included)
- Remove explicit XAML file listings (SDK auto-discovers)
- Convert packages.config to PackageReference
- Delete `packages.config` file after conversion
- Preserve `<ProjectReference>` to NameParser

#### 3. Package Updates

**Convert Newtonsoft.Json**:
- **From**: packages.config entry (Newtonsoft.Json 13.0.4)
- **To**: PackageReference (Newtonsoft.Json 13.0.3)

```xml
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
```

**Delete**: `packages.config` file

#### 4. Expected Breaking Changes

**WPF on .NET 8.0**:
- **Issue**: Minimal - WPF is stable across .NET versions
- **Potential Issues**: Deprecated XAML patterns, data binding changes
- **Fix**: Review .NET 8.0 WPF breaking changes (if any)

**Project Reference**:
- **Issue**: NameParser.UI depends on NameParser
- **Requirement**: NameParser must build successfully first
- **Fix**: Ensure NameParser targeting net8.0-windows

**System.Windows.Forms** (if used):
- **Issue**: Minimal changes expected
- **Fix**: Runtime testing

#### 5. Code Modifications

**Expected Changes**: Minimal

**Focus Areas**:
- XAML data binding expressions
- Code-behind event handlers
- ViewModel patterns (if MVVM used)
- Window/control initialization

**Unlikely Changes**:
- WPF control usage
- XAML syntax
- Resource dictionaries

#### 6. Testing Strategy

**UI Testing**:
- Launch application
- Navigate all windows/views
- Test all button clicks and interactions
- Validate data binding updates
- Check for visual rendering issues
- Test with different DPI settings (Windows 10/11)

**Integration Testing**:
- Verify calls to NameParser project work correctly
- Test data flow between UI and core logic
- Validate file selection dialogs
- Test error handling and user feedback

**Manual Testing**:
- End-to-end user workflows
- Edge cases (empty inputs, invalid data)
- Performance under load

#### 7. Validation Checklist

- [ ] Project builds without errors
- [ ] Project builds without warnings
- [ ] Application launches successfully
- [ ] All UI elements render correctly
- [ ] Data binding works correctly
- [ ] Project reference to NameParser works
- [ ] All user interactions functional
- [ ] No visual glitches or layout issues
- [ ] Error handling works
- [ ] Performance acceptable

**Success Criteria**: NameParser.UI launches and operates identically to .NET Framework 4.8 version, with all UI functionality intact.

---

### Cross-Project Considerations

**Dependency Synchronization**:
- NameParser.UI **must** be built after NameParser
- Both projects **must** target `net8.0-windows` (not `net8.0`)
- Shared packages (Newtonsoft.Json) should use compatible versions

**Build Order**:
1. NameParser (no dependencies)
2. NameParser.UI (depends on NameParser)

**Integration Points**:
- NameParser.UI references NameParser types directly
- Verify no breaking changes in NameParser public API
- Test data flow between projects

---

### All-At-Once Execution Note

While these specifications are detailed per-project, the **All-At-Once strategy** upgrades both projects simultaneously:
- Both .csproj files converted together
- All packages updated together
- Single solution build identifies all errors
- All errors fixed in coordinated operation
- Unified testing validates entire solution

---

## Package Update Reference

This section provides a comprehensive matrix of all package updates required for the upgrade.

### Package Update Matrix

| Package | Current Version | Target Version | Projects Affected | Update Reason | Priority |
|---------|----------------|----------------|-------------------|---------------|----------|
| **Entity Framework Core** |
| Microsoft.EntityFrameworkCore | 3.1.32 | 8.0.11 | NameParser | End-of-life, framework compatibility, security | 🔴 CRITICAL |
| Microsoft.EntityFrameworkCore.SqlServer | 3.1.32 | 8.0.11 | NameParser | Must match EF Core runtime version | 🔴 CRITICAL |
| Microsoft.EntityFrameworkCore.Tools | 10.0.2 | 8.0.11 | NameParser | Version conflict resolution, match EF Core | 🔴 CRITICAL |
| **Excel Processing** |
| ExcelDataReader | 3.8.0 | 3.8.2 | NameParser | Bug fixes, improved compatibility | 🟡 RECOMMENDED |
| ExcelDataReader.DataSet | 3.7.0 | 3.8.0 | NameParser | Align with ExcelDataReader, consistency | 🟡 RECOMMENDED |
| **System Extensions** |
| Microsoft.Extensions.Primitives | 3.1.32 (dup1) | 8.0.0 | NameParser | Remove duplicate, framework alignment | 🔴 CRITICAL |
| Microsoft.Extensions.Primitives | 10.0.2 (dup2) | *REMOVE* | NameParser | Remove duplicate, use single version | 🔴 CRITICAL |
| **Configuration** |
| System.Configuration.ConfigurationManager | 10.0.2 | 8.0.1 | NameParser | Framework alignment | 🟡 RECOMMENDED |
| **Office Interop** |
| Microsoft.Office.Interop.Excel | 16.0.18925 | 16.0.18925 | NameParser | No update needed (COM interop) | ✅ KEEP CURRENT |
| **JSON Serialization** |
| Newtonsoft.Json | HintPath | 13.0.3 | NameParser | Convert to PackageReference | 🟡 RECOMMENDED |
| Newtonsoft.Json | 13.0.4 (packages.config) | 13.0.3 | NameParser.UI | Convert from packages.config to PackageReference | 🟡 RECOMMENDED |

---

### Package Groups by Category

#### Critical Updates (Must Complete)

**Entity Framework Core Ecosystem** (NameParser only):
- **Current**: EF Core 3.1.32 runtime + 10.0.2 Tools (mismatched)
- **Target**: EF Core 8.0.11 runtime + 8.0.11 Tools (aligned)
- **Breaking Changes**: YES - 4 major version jump (3.1 → 4.0 → 5.0 → 6.0 → 7.0 → 8.0)
- **Impact**: High - requires code review and testing
- **References**:
  - [EF Core 8.0 Breaking Changes](https://learn.microsoft.com/ef/core/what-is-new/ef-core-8.0/breaking-changes)
  - [EF Core 6.0 Breaking Changes](https://learn.microsoft.com/ef/core/what-is-new/ef-core-6.0/breaking-changes)
  - [EF Core 5.0 Breaking Changes](https://learn.microsoft.com/ef/core/what-is-new/ef-core-5.0/breaking-changes)

**Microsoft.Extensions.Primitives Deduplication** (NameParser only):
- **Current**: 3.1.32 AND 10.0.2 (duplicate entries)
- **Target**: 8.0.0 (single entry)
- **Action**: Remove both old entries, add single 8.0.0 entry
- **Impact**: Low - resolves dependency conflict

#### Recommended Updates

**Excel Processing** (NameParser only):
- ExcelDataReader 3.8.0 → 3.8.2 (bug fixes)
- ExcelDataReader.DataSet 3.7.0 → 3.8.0 (alignment)
- **Breaking Changes**: None expected
- **Impact**: Low - API stable

**Configuration Management** (NameParser only):
- System.Configuration.ConfigurationManager 10.0.2 → 8.0.1
- **Breaking Changes**: None expected
- **Impact**: Low - API stable

**JSON Serialization** (Both projects):
- NameParser: Convert HintPath reference to PackageReference 13.0.3
- NameParser.UI: Convert packages.config to PackageReference 13.0.3
- **Breaking Changes**: None (API unchanged)
- **Impact**: Low - packaging format only

#### No Update Required

**Office Interop** (NameParser only):
- Microsoft.Office.Interop.Excel 16.0.18925 - Keep current
- **Reason**: COM interop, version stable
- **Testing**: Runtime validation required (requires Office installed)

---

### Package Update Procedure

#### For NameParser.csproj

1. **Remove duplicate Microsoft.Extensions.Primitives entries**:
   - Delete `<PackageReference Include="Microsoft.Extensions.Primitives" Version="3.1.32" />`
   - Delete `<PackageReference Include="Microsoft.Extensions.Primitives" Version="10.0.2" />`

2. **Add single Microsoft.Extensions.Primitives**:
   - Add `<PackageReference Include="Microsoft.Extensions.Primitives" Version="8.0.0" />`

3. **Update Entity Framework Core packages**:
   ```xml
   <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.11" />
   <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.11" />
   <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.11">
     <PrivateAssets>all</PrivateAssets>
     <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
   </PackageReference>
   ```

4. **Update Excel packages**:
   ```xml
   <PackageReference Include="ExcelDataReader" Version="3.8.2" />
   <PackageReference Include="ExcelDataReader.DataSet" Version="3.8.0" />
   ```

5. **Update configuration package**:
   ```xml
   <PackageReference Include="System.Configuration.ConfigurationManager" Version="8.0.1" />
   ```

6. **Convert Newtonsoft.Json from HintPath to PackageReference**:
   - Remove `<Reference Include="Newtonsoft.Json"><HintPath>...</HintPath></Reference>`
   - Add `<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />`

7. **Keep Microsoft.Office.Interop.Excel unchanged**:
   ```xml
   <PackageReference Include="Microsoft.Office.Interop.Excel" Version="16.0.18925.20022" />
   ```

#### For NameParser.UI.csproj

1. **Delete packages.config file**: `NameParser.UI\packages.config`

2. **Add PackageReference for Newtonsoft.Json**:
   ```xml
   <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
   ```

---

### Package Compatibility Notes

#### Entity Framework Core 8.0.11
- **Minimum .NET Version**: .NET 6.0 (✅ net8.0-windows compatible)
- **SQL Server Minimum**: SQL Server 2012 (verify environment)
- **Known Breaking Changes**: Query behavior, tracking, migrations
- **Migration Path**: Review all versions 4.0 through 8.0 breaking changes

#### ExcelDataReader 3.8.2
- **Target Framework**: .NET Standard 2.0 (✅ compatible with net8.0-windows)
- **Dependencies**: System.Text.Encoding.CodePages (auto-included)
- **Breaking Changes**: None

#### System.Configuration.ConfigurationManager 8.0.1
- **Target Framework**: .NET Standard 2.0 / .NET 6.0+ (✅ compatible)
- **API**: Stable, ConfigurationManager.AppSettings unchanged
- **Breaking Changes**: None expected

#### Newtonsoft.Json 13.0.3
- **Target Framework**: .NET Standard 2.0 (✅ compatible)
- **API**: Stable across .NET versions
- **Breaking Changes**: None
- **Note**: Consider migrating to System.Text.Json in future (not required for upgrade)

#### Microsoft.Office.Interop.Excel 16.0.18925
- **Type**: COM Interop
- **Runtime Requirement**: Microsoft Office installed on target machine
- **Testing**: Must validate on deployment environment
- **Alternative**: EPPlus or ClosedXML for future (not required now)

---

### Transitive Dependencies

**After package updates, verify transitive dependencies**:

- EF Core 8.0 brings in: Microsoft.Extensions.* packages (8.0.x)
- ExcelDataReader brings in: System.Text.Encoding.CodePages
- Ensure no conflicts between explicit and transitive package versions

**Resolution Strategy**:
- Let SDK resolve transitive dependencies automatically
- Only add explicit PackageReference if conflict occurs
- Use `dotnet list package --include-transitive` to inspect

---

### Post-Update Validation

After updating all packages:

1. **Restore packages**: `dotnet restore NameParser.sln`
2. **Check for conflicts**: Review restore output for warnings
3. **Inspect versions**: `dotnet list package` to verify all versions match plan
4. **Verify no duplicates**: Ensure Microsoft.Extensions.Primitives appears only once
5. **Build to validate**: `dotnet build NameParser.sln` to catch package-related issues

---

### Package Update Rollback

If package updates cause critical issues:

**Option 1**: Revert to previous versions temporarily
- Revert EF Core to 3.1.32 (not recommended - end-of-life)
- Investigate specific package causing issue

**Option 2**: Incremental package updates
- Update non-EF Core packages first
- Update EF Core separately
- Isolate problematic package

**Option 3**: Git rollback
- `git reset --hard HEAD~1` if committed
- `git checkout -- NameParser.csproj NameParser.UI.csproj` if not committed

---

### All-At-Once Note

In the All-At-Once strategy, **all package updates happen simultaneously** before the first build. This means:
- All packages updated in single operation
- Dependencies resolve together
- Single build reveals all compatibility issues
- Single fix cycle addresses all package-related errors

---

## Breaking Changes Catalog

This section documents expected breaking changes from .NET Framework 4.8 to .NET 8.0, with focus on Entity Framework Core 3.1 → 8.0.

---

### Entity Framework Core 3.1 → 8.0 Breaking Changes

This upgrade spans **4 major versions**. Review all breaking changes documentation:
- [EF Core 4.0 Breaking Changes](https://learn.microsoft.com/ef/core/what-is-new/ef-core-4.0/breaking-changes)
- [EF Core 5.0 Breaking Changes](https://learn.microsoft.com/ef/core/what-is-new/ef-core-5.0/breaking-changes)
- [EF Core 6.0 Breaking Changes](https://learn.microsoft.com/ef/core/what-is-new/ef-core-6.0/breaking-changes)
- [EF Core 7.0 Breaking Changes](https://learn.microsoft.com/ef/core/what-is-new/ef-core-7.0/breaking-changes)
- [EF Core 8.0 Breaking Changes](https://learn.microsoft.com/ef/core/what-is-new/ef-core-8.0/breaking-changes)

#### Category 1: Query Behavior Changes

**Issue**: Split Query is Now Default for Collections

**Affected Code**: Queries with `.Include()` for collection navigation properties

**Example**:
```csharp
// This query behavior changes
var races = context.Races
    .Include(r => r.RaceResults)
    .ToList();
```

**Impact**: Multiple SQL queries instead of single JOIN query

**Fix Options**:
1. **Accept new behavior** (recommended for most cases)
2. **Explicit single query**: Add `.AsSingleQuery()` to restore old behavior
3. **Explicit split query**: Add `.AsSplitQuery()` to make intent clear

**Files Likely Affected**:
- `Infrastructure/Data/ClassificationRepository.cs`
- `Infrastructure/Data/RaceRepository.cs`

---

**Issue**: Query Translation Differences

**Affected Code**: Complex LINQ queries, especially with GroupBy, OrderBy, nested queries

**Impact**: Some LINQ expressions that worked in 3.1 may not translate in 8.0, or translate differently

**Fix**: 
- Review compiler errors for untranslatable queries
- Use `.AsEnumerable()` to switch to client evaluation if needed (with caution)
- Simplify complex queries or split into multiple queries

---

#### Category 2: Tracking and Change Detection

**Issue**: Tracking Behavior Changes

**Affected Code**: Queries that rely on change tracking, `AsNoTracking()` usage

**Impact**: Performance characteristics may differ

**Fix**: Review tracking patterns, explicitly use `.AsNoTracking()` or `.AsTracking()` where behavior matters

**Files Likely Affected**: All repository files

---

#### Category 3: Migrations

**Issue**: Migration Differences in SQL Generation

**Affected Code**: Existing migrations may generate different SQL in EF Core 8.0

**Impact**: If regenerating migrations, SQL may differ from current database state

**Fix Options**:
1. **Keep existing migrations** (recommended if database already matches)
2. **Regenerate migrations**: Drop all migrations, create new initial migration, compare SQL
3. **Add new migration**: If schema changes needed, add new migration on top

**Files Likely Affected**:
- `Migrations` folder (if exists)

**Testing**: Validate migrations against development database before production

---

#### Category 4: Value Converters

**Issue**: Value Converter Behavior Changes

**Affected Code**: Custom value converters for entity properties

**Impact**: Conversion logic may behave differently

**Fix**: Review value converter implementations, update if needed

**Files Likely Affected**:
- `Infrastructure/Data/RaceManagementContext.cs` (OnModelCreating)
- Entity configuration files

---

#### Category 5: SQL Server Provider

**Issue**: Minimum SQL Server Version Requirement

**Current**: EF Core 3.1 supports SQL Server 2008+
**New**: EF Core 8.0 supports SQL Server 2012+

**Impact**: Verify target SQL Server version is 2012 or higher

**Fix**: Upgrade SQL Server if below 2012 (unlikely for most environments)

**Verification**: Check connection string and server version

---

**Issue**: Connection String Changes

**Affected Code**: Connection string configuration

**Impact**: Some connection string keywords may be deprecated

**Fix**: Review connection strings, update if warnings/errors occur

**Files Likely Affected**:
- `app.config` (connection strings section)
- `Infrastructure/Data/RaceManagementContext.cs` (OnConfiguring)

---

### .NET Framework 4.8 → .NET 8.0 Breaking Changes

#### Obsolete APIs

**Issue**: APIs marked `[Obsolete]` in .NET Framework may be removed in .NET 8.0

**Affected Code**: Any code using deprecated APIs

**Fix**: Use IDE suggestions or compiler messages to find replacements

**Common Patterns**:
- `BinaryFormatter` → Use JSON serialization or other alternatives
- Certain `System.Drawing` APIs → Use modern alternatives

**Files**: Scan all .cs files for obsolete attribute warnings

---

#### Configuration System

**Issue**: System.Configuration.ConfigurationManager API

**Status**: ✅ Compatible via NuGet package (System.Configuration.ConfigurationManager 8.0.1)

**Affected Code**: `ConfigurationManager.AppSettings`, `ConfigurationManager.ConnectionStrings`

**Impact**: Low - API remains compatible

**Fix**: Minimal changes expected, verify at runtime

**Files Likely Affected**:
- `Program.cs`
- Any code reading app.config

---

#### Windows Forms / WPF

**Issue**: Most APIs stable, some deprecated patterns removed

**Status**: ✅ Generally compatible

**Affected Code**: WPF XAML, Windows Forms controls

**Impact**: Low - Microsoft committed to Windows desktop support

**Fix**: Review .NET 8.0 breaking changes for WPF/WinForms specifically

**Files Likely Affected**:
- `NameParser.UI` project (all XAML and code-behind)

---

### Excel Processing Libraries

#### ExcelDataReader 3.8.0 → 3.8.2

**Breaking Changes**: None

**Impact**: Bug fixes only

**Files Likely Affected**:
- `Infrastructure/Repositories/ExcelRaceResultRepository.cs`

**Testing**: Runtime validation with sample Excel files

---

#### Microsoft.Office.Interop.Excel

**Breaking Changes**: None (COM interop)

**Impact**: Behavior should be identical

**Caveat**: Requires Office installed on runtime machine

**Files Likely Affected**:
- Any code using `Microsoft.Office.Interop.Excel` namespace

**Testing**: Runtime validation with Office automation

---

### JSON Serialization

#### Newtonsoft.Json 13.0.3

**Breaking Changes**: None

**Impact**: API stable across .NET versions

**Files Likely Affected**:
- `Infrastructure/Repositories/JsonMemberRepository.cs`
- Any code using `JsonConvert` or `JObject`

**Future Consideration**: System.Text.Json is now built-in to .NET, but migration not required for this upgrade

---

### Compilation Error Patterns

**Expected Errors After First Build**:

1. **Namespace not found**
   - **Cause**: Namespace moved or removed
   - **Fix**: Update using statements, find new namespace

2. **Type or member not found**
   - **Cause**: API removed or renamed
   - **Fix**: Find replacement API in breaking changes docs

3. **Obsolete warnings/errors**
   - **Cause**: API marked obsolete, now error
   - **Fix**: Use suggested replacement from error message

4. **Query translation errors** (EF Core)
   - **Cause**: LINQ expression no longer translates to SQL
   - **Fix**: Simplify query, use `.AsEnumerable()`, or split query

5. **Migration errors** (EF Core)
   - **Cause**: Migration code incompatible with EF Core 8.0
   - **Fix**: Regenerate migrations or update migration code

---

### Breaking Changes Resolution Strategy

**For Each Error**:

1. **Categorize**: EF Core, BCL API, package-specific, or configuration
2. **Search**: Check breaking changes documentation for specific issue
3. **Identify Fix**: Find recommended replacement or workaround
4. **Apply**: Update code
5. **Rebuild**: Verify fix resolves error
6. **Test**: Ensure fix doesn't introduce regression

**Priority Order**:
1. Fix EF Core errors first (highest impact)
2. Fix compilation errors preventing build
3. Address warnings
4. Runtime testing to catch non-compile-time issues

---

### Runtime Breaking Changes

Some breaking changes only appear at runtime:

**EF Core Query Behavior**:
- Different query results (rare, but possible)
- Performance differences
- **Detection**: Comprehensive testing

**Configuration**:
- Connection string parsing differences
- **Detection**: Application startup testing

**Excel/Office Interop**:
- COM registration issues
- **Detection**: Excel processing testing

**Mitigation**: Comprehensive test suite covering all functionality

---

## Risk Management

### High-Risk Changes

| Project | Risk Level | Description | Mitigation |
|---------|------------|-------------|------------|
| NameParser | 🔴 HIGH | Entity Framework Core 3.1 → 8.0 (4 major version jump) | Comprehensive testing of all data access, review breaking changes docs for EF Core 4.0, 5.0, 6.0, 7.0, 8.0 |
| NameParser | 🟡 MEDIUM | Package version conflicts (duplicate Microsoft.Extensions.Primitives, EF Tools mismatch) | Clean removal of duplicates, align all EF Core packages to 8.0.11 |
| NameParser | 🟡 MEDIUM | COM Interop (Microsoft.Office.Interop.Excel) compatibility | Runtime testing with Office installed, validate Excel automation |
| NameParser.UI | 🟡 MEDIUM | WPF project conversion from legacy format | Careful manual conversion, preserve XAML files and resources |
| Both | 🟡 MEDIUM | Legacy project format conversion | Manual SDK-style conversion, validate all file inclusions |

### All-At-Once Strategy Risk Factors

**Higher Initial Risk**: Upgrading both projects simultaneously creates a larger initial change surface. This increases the risk compared to incremental migration but is acceptable for a 2-project solution.

**Mitigation Strategies**:
1. **Atomic Build Cycle**: Build entire solution after all updates to catch all errors at once
2. **Comprehensive Fix Pass**: Address all compilation errors before attempting to run
3. **Unified Testing**: Test entire solution together to catch integration issues
4. **Single Commit**: All changes in one commit for easy rollback if needed

### Security Vulnerabilities

**Current Status**: None detected in assessment

However, upgrading from .NET Framework 4.8 to .NET 8.0 will provide:
- Security patches and improvements from 5+ years of .NET evolution
- Updated TLS/SSL support
- Modern cryptography APIs

### Contingency Plans

#### EF Core Blocking Issues

**If**: Entity Framework Core 8.0 queries produce incorrect results or fail

**Options**:
1. Review EF Core 8.0 breaking changes documentation for specific issue
2. Apply `.AsSplitQuery()` or `.AsSingleQuery()` to adjust query behavior
3. Regenerate migrations if database schema issues occur
4. Consult EF Core 8.0 migration guide for known patterns

#### Office Interop Compatibility Issues

**If**: Microsoft.Office.Interop.Excel fails at runtime on .NET 8.0

**Options**:
1. Verify Office installation on target machine
2. Check COM registration and permissions
3. Consider alternative: EPPlus or ClosedXML (requires code changes)
4. Test with different Office versions (2016, 2019, 365)

#### WPF Rendering or Binding Issues

**If**: NameParser.UI has visual or binding problems after upgrade

**Options**:
1. Review .NET 8.0 WPF breaking changes
2. Check for deprecated XAML patterns
3. Validate data binding expressions
4. Test on multiple Windows versions (10, 11)

#### Package Conflict Resolution Fails

**If**: NuGet restore fails due to unresolvable package conflicts

**Options**:
1. Manually align transitive dependencies
2. Add explicit PackageReference for conflicting packages
3. Use `<PackageReference Update>` in Directory.Build.props
4. Check for platform-specific package variants

### Rollback Strategy

**If critical blockers prevent completion:**

1. **Immediate Rollback**: `git reset --hard HEAD~1` (if single commit)
2. **Branch Rollback**: `git checkout master` (upgrade-to-NET8 branch remains for investigation)
3. **Incremental Approach**: Switch to phased migration if All-At-Once proves too complex

### Risk Timeline

- **Highest Risk Period**: During initial build after all updates (Phase 1)
- **Medium Risk Period**: During EF Core testing (Phase 2)
- **Lower Risk Period**: After successful build and data access validation

---

## Testing & Validation Strategy

This section defines the comprehensive testing approach for validating the .NET 8.0 upgrade.

---

### Testing Philosophy

**Goal**: Ensure the upgraded solution functions identically to the .NET Framework 4.8 version with no functional regressions.

**Approach**: Multi-level testing from unit to end-to-end

**Risk-Based Prioritization**: Focus testing on high-risk areas (Entity Framework Core, Excel processing)

---

### Test Levels

#### Level 1: Build Validation

**When**: After all project and package updates, after each error fix cycle

**Checklist**:
- [ ] `dotnet build NameParser.sln` exits with code 0
- [ ] No compilation errors
- [ ] Review warnings (address critical warnings)
- [ ] Both NameParser.exe and NameParser.UI.exe generated
- [ ] All dependencies restored successfully

**Pass Criteria**: Clean build with 0 errors

---

#### Level 2: Unit Testing (if unit tests exist)

**Scope**: Individual methods and classes

**Focus Areas**:

**Domain Layer**:
- [ ] PointsCalculationService logic
- [ ] RaceFileName value object parsing
- [ ] Classification aggregate behavior
- [ ] Entity validation rules

**Application Layer**:
- [ ] RaceProcessingService workflows
- [ ] ReportGenerationService output

**Infrastructure Layer**:
- [ ] Repository CRUD operations (with test database)
- [ ] JSON serialization/deserialization
- [ ] Excel file parsing (with test files)

**Execution**: `dotnet test` (if test project exists)

**Pass Criteria**: All unit tests pass, no test failures

---

#### Level 3: Integration Testing

**Scope**: Component interactions, data access, file I/O

**Priority: HIGH** (due to EF Core upgrade)

##### Data Access Testing (Critical)

**Test Database Setup**: Use test/development database, not production

**Test Categories**:

1. **CRUD Operations**:
   - [ ] Create new Classification records
   - [ ] Read Classifications from database
   - [ ] Update Classification properties
   - [ ] Delete Classifications
   - [ ] Repeat for Race, RaceResult, Member entities

2. **Complex Queries**:
   - [ ] Join queries (e.g., Races with RaceResults)
   - [ ] Filtering and sorting
   - [ ] Aggregations (Count, Sum, Average)
   - [ ] GroupBy operations
   - [ ] Include/ThenInclude navigation properties

3. **Transactions**:
   - [ ] Multi-entity saves within transaction
   - [ ] Transaction rollback on error
   - [ ] Concurrent access scenarios

4. **Migrations** (if applicable):
   - [ ] Apply migrations to test database
   - [ ] Verify schema matches expectations
   - [ ] Rollback migrations

5. **Connection Handling**:
   - [ ] Connection string validation
   - [ ] Connection pooling behavior
   - [ ] Dispose patterns

**Query Behavior Validation**:
- [ ] Verify split query vs single query results match expectations
- [ ] Check for N+1 query problems
- [ ] Validate lazy loading behavior (if enabled)

**Pass Criteria**: All data access operations work correctly, query results match expected data

---

##### Excel Processing Testing

**Test Files**: Use sample Excel files from `Courses\` directory

**Test Categories**:

1. **ExcelDataReader**:
   - [ ] Read .xlsx files successfully
   - [ ] Parse race data from Excel
   - [ ] Handle multiple sheets
   - [ ] Handle empty cells gracefully
   - [ ] Handle malformed files (error handling)

2. **Office Interop** (if used):
   - [ ] COM automation works on test machine
   - [ ] Excel file generation
   - [ ] Excel file modification
   - [ ] Proper disposal of COM objects

**Environment Note**: Office Interop requires Microsoft Office installed

**Pass Criteria**: All Excel files process correctly, no data loss or corruption

---

##### JSON Serialization Testing

**Test Categories**:

1. **Serialization**:
   - [ ] Serialize Member objects to JSON
   - [ ] Serialize complex object graphs
   - [ ] Handle null values correctly

2. **Deserialization**:
   - [ ] Deserialize JSON from `Challenge.json`
   - [ ] Handle missing properties gracefully
   - [ ] Validate deserialized object state

**Pass Criteria**: JSON operations work identically to .NET Framework 4.8 version

---

##### Configuration Testing

**Test Categories**:

1. **AppSettings**:
   - [ ] Read `ConfigurationManager.AppSettings` values
   - [ ] Validate all expected keys present
   - [ ] Type conversion works correctly

2. **Connection Strings**:
   - [ ] Read `ConfigurationManager.ConnectionStrings`
   - [ ] Validate connection string format
   - [ ] Test database connection

**Pass Criteria**: All configuration values load correctly

---

#### Level 4: System Testing

**Scope**: End-to-end workflows, entire application behavior

**Test Scenarios**:

##### Scenario 1: Console Application Workflow (NameParser)

1. [ ] Launch NameParser.exe
2. [ ] Load member data from JSON
3. [ ] Process race files from Excel
4. [ ] Calculate classifications and points
5. [ ] Generate output files
6. [ ] Verify output correctness
7. [ ] Check database state after processing

**Pass Criteria**: Complete workflow executes successfully, output matches expected results

---

##### Scenario 2: UI Application Workflow (NameParser.UI)

1. [ ] Launch NameParser.UI.exe
2. [ ] Verify all windows/views render correctly
3. [ ] Test file selection dialogs
4. [ ] Execute core NameParser logic via UI
5. [ ] Display results in UI
6. [ ] Test all button clicks and interactions
7. [ ] Test error scenarios (invalid inputs)

**Pass Criteria**: UI functions correctly, no visual glitches, all interactions work

---

##### Scenario 3: Integration Workflow

1. [ ] Run console application to process data
2. [ ] Verify database updated correctly
3. [ ] Launch UI application
4. [ ] Display processed data in UI
5. [ ] Modify data via UI
6. [ ] Verify changes persisted to database

**Pass Criteria**: Data flows correctly between console app, database, and UI app

---

#### Level 5: Performance Testing

**Scope**: Ensure no significant performance regressions

**Test Categories**:

1. **Query Performance**:
   - [ ] Measure EF Core query execution times
   - [ ] Compare to .NET Framework 4.8 baseline (if available)
   - [ ] Identify slow queries
   - [ ] Check for excessive database round-trips

2. **Excel Processing Performance**:
   - [ ] Measure large Excel file processing time
   - [ ] Verify memory usage acceptable

3. **Startup Performance**:
   - [ ] Measure application startup time
   - [ ] Compare to baseline

4. **Memory Usage**:
   - [ ] Monitor memory consumption during operations
   - [ ] Check for memory leaks (use diagnostic tools)

**Pass Criteria**: Performance within acceptable range (no >20% regression without justification)

---

### High-Priority Test Areas (Risk-Based)

Due to the Entity Framework Core 3.1 → 8.0 upgrade, prioritize these areas:

1. **🔴 CRITICAL**: All EF Core query operations
2. **🔴 CRITICAL**: Database CRUD operations
3. **🟡 HIGH**: Excel file processing
4. **🟡 HIGH**: End-to-end workflows
5. **🟢 MEDIUM**: UI rendering and interactions
6. **🟢 MEDIUM**: Configuration loading
7. **🟢 LOW**: JSON serialization

---

### Test Environment Setup

**Requirements**:

- **Operating System**: Windows 10/11 (for WPF and Office Interop)
- **.NET 8.0 SDK**: Installed and verified
- **SQL Server**: Test database (2012+ version)
- **Microsoft Office**: Installed (if testing Office Interop)
- **Test Data**: Sample Excel files, test database with known data, JSON files

**Setup Steps**:

1. [ ] Restore test database from backup or run migrations
2. [ ] Populate test data (Members, Races, RaceResults)
3. [ ] Copy sample Excel files to test directory
4. [ ] Verify connection strings point to test database (not production!)
5. [ ] Configure app.config or appsettings.json for test environment

---

### Test Execution Sequence

**Recommended Order**:

1. **Build Validation** → Verify solution builds
2. **Unit Tests** → Test individual components
3. **Data Access Integration Tests** → Validate EF Core (high priority)
4. **Excel Processing Tests** → Validate file I/O
5. **Configuration Tests** → Validate settings load
6. **Console Application System Tests** → End-to-end NameParser
7. **UI Application System Tests** → End-to-end NameParser.UI
8. **Integration System Tests** → Full workflow across apps
9. **Performance Tests** → Validate acceptable performance

**Rationale**: Test in order of increasing scope, catch issues early at unit/integration level

---

### Test Documentation

**For Each Test Category**:

1. **Document Test Cases**: Write explicit test steps (or automated test code)
2. **Record Results**: Pass/Fail status
3. **Log Issues**: Any failures or unexpected behavior
4. **Track Fixes**: Link fixes to specific test failures

**Test Report Template**:

```
## Test Report: [Category Name]

**Date**: [Date]
**Tester**: [Name]
**Environment**: [OS, .NET version, SQL Server version]

### Test Results

- Total Tests: X
- Passed: Y
- Failed: Z

### Failed Tests

1. [Test Name]: [Failure Description]
   - **Expected**: [Expected behavior]
   - **Actual**: [Actual behavior]
   - **Fix Applied**: [Description of fix]

### Notes

[Any additional observations]
```

---

### Regression Testing

**After Fixing Issues**:

1. **Retest Failed Tests**: Verify fix resolves issue
2. **Regression Check**: Rerun previously passing tests to ensure fix didn't break other areas
3. **Full Suite**: After all fixes, run complete test suite

**Pass Criteria**: All tests pass with no regressions

---

### Manual Testing Checklist (Non-Automated)

**Console Application (NameParser)**:
- [ ] Application launches without errors
- [ ] Command-line arguments work (if applicable)
- [ ] Console output displays correctly
- [ ] Log files generated correctly
- [ ] Graceful shutdown

**UI Application (NameParser.UI)**:
- [ ] Splash screen displays (if applicable)
- [ ] Main window renders correctly
- [ ] All menus accessible
- [ ] All buttons functional
- [ ] Data grids display data correctly
- [ ] Forms validate input correctly
- [ ] Dialog boxes work
- [ ] Application closes gracefully

---

### Test Success Criteria

**The upgrade is validated when**:

- [ ] Solution builds with 0 errors
- [ ] All unit tests pass (if exist)
- [ ] All data access integration tests pass
- [ ] All Excel processing tests pass
- [ ] All configuration tests pass
- [ ] All system tests pass
- [ ] Performance within acceptable range
- [ ] No critical bugs discovered
- [ ] Manual testing complete with no showstoppers

---

### Test Failure Response

**If Critical Test Fails**:

1. **Stop Further Testing**: Address critical failure first
2. **Diagnose**: Root cause analysis
3. **Fix**: Apply code changes
4. **Rebuild**: `dotnet build`
5. **Retest**: Verify fix
6. **Resume**: Continue test suite

**If Non-Critical Test Fails**:

1. **Log Issue**: Document failure
2. **Continue Testing**: Complete test suite
3. **Triage**: Assess all failures together
4. **Prioritize Fixes**: Address based on severity
5. **Batch Fix and Retest**: Fix multiple issues, then retest

---

### Testing for All-At-Once Strategy

**Key Differences from Incremental**:

- **No Partial Testing**: Cannot test NameParser alone before NameParser.UI (both upgraded together)
- **Unified Test Run**: All tests executed against fully upgraded solution
- **Higher Test Priority**: Comprehensive testing more critical since no incremental validation

**Mitigation**: Structured test plan with clear priorities ensures thorough validation despite single upgrade cycle

---

## Complexity & Effort Assessment

### Per-Project Complexity

| Project | Complexity | Dependencies | Risk Factors | Relative Effort |
|---------|------------|--------------|--------------|-----------------|
| **NameParser** | MEDIUM | 9 packages, 0 projects | EF Core 3.1→8.0, package conflicts, Excel COM interop | HIGH |
| **NameParser.UI** | LOW | 1 package, 1 project | WPF conversion, depends on NameParser success | LOW |

### Complexity Ratings Explained

**NameParser - MEDIUM Complexity:**
- **Code Volume**: ~1,500 LOC (moderate)
- **Technology Depth**: Entity Framework Core, Excel processing, JSON serialization
- **Breaking Changes**: High (EF Core 3.1 → 8.0 spans 4 major versions)
- **Package Count**: 9 packages with conflicts
- **Testing Scope**: Data access, file I/O, business logic

**NameParser.UI - LOW Complexity:**
- **Code Volume**: ~500 LOC (small)
- **Technology Depth**: WPF UI layer, minimal logic
- **Breaking Changes**: Low (WPF is stable across .NET versions)
- **Package Count**: 1 package (Newtonsoft.Json)
- **Testing Scope**: UI rendering, data binding

### Phase Complexity Assessment

**All-At-Once Strategy simplifies complexity** by eliminating multi-targeting and intermediate states:

**Phase 1: Atomic Upgrade**
- **Complexity**: HIGH
- **Reason**: All changes applied simultaneously, larger error surface
- **Mitigation**: Systematic approach, comprehensive error fixing

**Phase 2: Validation**
- **Complexity**: MEDIUM
- **Reason**: Must test entire solution at once
- **Mitigation**: Structured test plan, prioritize high-risk areas (EF Core)

### Resource Requirements

**Skill Levels Required:**

1. **Project Conversion**: Intermediate
   - Understanding of SDK-style project format
   - Familiarity with .NET project structure
   - Ability to manually edit .csproj files

2. **Entity Framework Core**: Intermediate to Advanced
   - Knowledge of EF Core query patterns
   - Understanding of migrations
   - Ability to diagnose and fix breaking changes

3. **Package Management**: Intermediate
   - NuGet package resolution understanding
   - Ability to resolve dependency conflicts
   - Understanding of transitive dependencies

4. **WPF**: Intermediate
   - Understanding of WPF project structure
   - XAML knowledge
   - Data binding concepts

**Parallel Capacity:**

Not applicable for this upgrade - All-At-Once strategy requires coordinated sequential execution by a single executor to maintain consistency.

### Effort Comparison

**If Incremental Strategy Were Used** (for comparison):
- Phase 1: NameParser only → 8-12 hours
- Phase 2: NameParser.UI → 4-6 hours
- **Total**: 12-18 hours (similar to All-At-Once)

**All-At-Once Strategy** (selected):
- Phase 1: Both projects simultaneously → 8-12 hours
- Phase 2: Unified testing → 4-7 hours
- **Total**: 12-19 hours

**Conclusion**: Similar effort, but All-At-Once is faster in calendar time and simpler coordination.

### Relative Complexity by Task Category

| Task Category | Complexity | Justification |
|---------------|------------|---------------|
| Project Conversion | MEDIUM | Manual SDK-style conversion, WPF considerations |
| Package Updates | MEDIUM | Version conflicts, EF Core alignment |
| EF Core Migration | HIGH | 4 major version jump, query behavior changes |
| Compilation Fixes | MEDIUM | Depends on breaking changes encountered |
| Testing | MEDIUM | Comprehensive data access and UI testing |
| Documentation | LOW | Straightforward documentation updates |

---

## Source Control Strategy

This section defines how the upgrade will be managed in version control (Git).

---

### Branching Strategy

**Current State**:
- **Main Branch**: `master`
- **Upgrade Branch**: `upgrade-to-NET8` (created and checked out)
- **Starting Commit**: All pending changes committed before upgrade started

**Branching Approach**: Feature branch for upgrade

**Branch Lifecycle**:
1. Create `upgrade-to-NET8` branch from `master` ✅ (completed)
2. Perform all upgrade work on `upgrade-to-NET8`
3. Test and validate on `upgrade-to-NET8`
4. Merge `upgrade-to-NET8` → `master` after successful validation
5. Optionally delete `upgrade-to-NET8` after merge

---

### Commit Strategy

**Approach for All-At-Once Strategy**: Single atomic commit (preferred)

**Recommended**: One comprehensive commit containing the entire upgrade

**Rationale**:
- All-At-Once strategy makes all changes simultaneously
- Single commit reflects atomic nature of upgrade
- Easier rollback if issues discovered
- Clear "before" and "after" states

**Commit Structure**:

```
Subject: Upgrade solution to .NET 8.0

Body:
- Convert NameParser and NameParser.UI to SDK-style projects
- Update TargetFramework to net8.0-windows for both projects
- Upgrade Entity Framework Core 3.1.32 → 8.0.11
- Update all NuGet packages to .NET 8.0 compatible versions
- Remove duplicate Microsoft.Extensions.Primitives references
- Fix compilation errors from EF Core breaking changes
- Validate all functionality with comprehensive testing

Projects upgraded:
- NameParser (Console App)
- NameParser.UI (WPF App)

Key package updates:
- Microsoft.EntityFrameworkCore: 3.1.32 → 8.0.11
- Microsoft.EntityFrameworkCore.SqlServer: 3.1.32 → 8.0.11
- Microsoft.EntityFrameworkCore.Tools: 10.0.2 → 8.0.11
- ExcelDataReader: 3.8.0 → 3.8.2
- ExcelDataReader.DataSet: 3.7.0 → 3.8.0
- Microsoft.Extensions.Primitives: (deduplicated) → 8.0.0
- System.Configuration.ConfigurationManager: 10.0.2 → 8.0.1
- Newtonsoft.Json: (converted to PackageReference) → 13.0.3

Breaking changes addressed:
- EF Core query behavior (split query default)
- [List specific breaking changes fixed]

Testing completed:
- Build validation: ✓
- Data access integration tests: ✓
- Excel processing tests: ✓
- UI functionality tests: ✓
- End-to-end workflows: ✓

Resolves: #[issue number if applicable]
```

**Alternative Approach**: Staged commits (if single commit becomes too large)

If the upgrade requires multiple fix iterations, consider:

1. **Commit 1**: Project conversion and package updates
   ```
   feat: Convert projects to SDK-style and update packages for .NET 8.0
   ```

2. **Commit 2**: Compilation error fixes
   ```
   fix: Resolve compilation errors from .NET 8.0 upgrade
   ```

3. **Commit 3**: Test fixes and adjustments
   ```
   fix: Address runtime issues and test failures
   ```

**Use staged commits only if**: Single commit proves too difficult to manage or review

---

### Commit Message Format

**Follow Conventional Commits** (optional but recommended):

- `feat:` New features or capabilities
- `fix:` Bug fixes or error corrections
- `refactor:` Code changes that neither fix bugs nor add features
- `chore:` Build process, tooling, dependencies
- `docs:` Documentation updates

**For this upgrade**: `feat:` or `chore:` are appropriate

**Examples**:
- `feat: Upgrade solution to .NET 8.0`
- `chore: Upgrade to .NET 8.0 and update dependencies`

---

### File Organization

**Files Changed** (expected):

**Modified**:
- `NameParser/NameParser.csproj` (complete rewrite for SDK-style)
- `NameParser.UI/NameParser.UI.csproj` (complete rewrite for SDK-style)
- `NameParser.sln` (may have format updates)
- Code files with breaking change fixes (various .cs files)

**Deleted**:
- `NameParser/Properties/AssemblyInfo.cs` (SDK auto-generates)
- `NameParser.UI/Properties/AssemblyInfo.cs` (SDK auto-generates)
- `NameParser.UI/packages.config` (converted to PackageReference)
- Legacy project files backup (e.g., `*.csproj.bak` if created)

**Added**:
- None expected (unless new files needed for fixes)

**Preserved**:
- All source code files (.cs)
- All XAML files
- Content files (Excel, JSON)
- app.config
- All other project content

---

### Code Review Process

**Before Merge to `master`**:

1. **Self-Review**:
   - [ ] Review all changed files in diff
   - [ ] Verify no unintended changes
   - [ ] Check for debug code or temporary changes
   - [ ] Validate commit message accuracy

2. **Pull Request** (if team process requires):
   - Create PR: `upgrade-to-NET8` → `master`
   - **PR Title**: "Upgrade solution to .NET 8.0"
   - **PR Description**: Include commit body content
   - **Reviewers**: Assign team members

3. **Review Checklist for Reviewers**:
   - [ ] Project files converted correctly to SDK-style
   - [ ] All package versions align with plan
   - [ ] No duplicate or conflicting packages
   - [ ] Breaking changes addressed appropriately
   - [ ] No hardcoded values or magic strings introduced
   - [ ] Code follows project conventions
   - [ ] Tests pass (CI/CD if configured)

4. **Approval and Merge**:
   - Obtain required approvals
   - Merge strategy: **Squash** or **Merge commit** (team preference)
   - Delete `upgrade-to-NET8` branch after merge (optional)

---

### Merge Strategy

**Recommended**: Merge commit (preserves upgrade branch history)

**Command**:
```bash
git checkout master
git merge upgrade-to-NET8 --no-ff
git push origin master
```

**Alternative**: Squash merge (single commit in master)

**Command**:
```bash
git checkout master
git merge upgrade-to-NET8 --squash
git commit -m "Upgrade solution to .NET 8.0"
git push origin master
```

**Team Decision**: Choose based on team's Git workflow preferences

---

### Rollback Strategy

**If Critical Issue Discovered After Merge**:

**Option 1**: Revert merge commit
```bash
git revert -m 1 [merge_commit_sha]
git push origin master
```

**Option 2**: Hard reset (only if not shared with team yet)
```bash
git reset --hard [commit_before_merge]
git push origin master --force
```

**Option 3**: Create hotfix branch from pre-upgrade state
```bash
git checkout -b hotfix-revert [commit_before_merge]
# Make necessary changes
git checkout master
git merge hotfix-revert
```

**Recommendation**: Option 1 (revert) is safest for shared branches

---

### Git Workflow Summary

**Phase 0: Setup** ✅ (completed)
```bash
git checkout master
git pull origin master
git add -A
git commit -m "Committing pending changes before .NET 8 upgrade"
git checkout -b upgrade-to-NET8
```

**Phase 1: Upgrade Work** (in progress)
```bash
# Make all project changes
# Update packages
# Fix compilation errors
git add -A
git commit -m "Upgrade solution to .NET 8.0"
```

**Phase 2: Testing** (validation)
```bash
# Run all tests
# Fix any issues discovered
git add -A
git commit --amend  # If using single commit approach
# OR
git commit -m "fix: Address [specific issue]"  # If using staged commits
```

**Phase 3: Merge** (after successful validation)
```bash
git checkout master
git merge upgrade-to-NET8 --no-ff
git push origin master
git branch -d upgrade-to-NET8  # Optional: delete branch
```

---

### .gitignore Considerations

**Verify .gitignore excludes**:
- `bin/` directories
- `obj/` directories
- `*.user` files
- `*.suo` files
- `.vs/` directory
- NuGet package cache (if locally restored)

**Check for**: Accidentally committed binaries or IDE-specific files

---

### Backup Strategy

**Before Merge to `master`**:

1. **Create backup branch** (optional safety net):
   ```bash
   git checkout master
   git branch backup-before-net8-upgrade
   git push origin backup-before-net8-upgrade
   ```

2. **Tag pre-upgrade state** (recommended):
   ```bash
   git tag pre-net8-upgrade master
   git push origin pre-net8-upgrade
   ```

**Benefit**: Easy recovery point if rollback needed

---

### Continuous Integration / Continuous Deployment (CI/CD)

**If CI/CD Configured**:

1. **Upgrade Branch Build**:
   - Verify CI build passes on `upgrade-to-NET8` branch
   - Review build logs for warnings or errors
   - Ensure tests pass in CI environment

2. **Pre-Merge Validation**:
   - Require CI build success before merge to `master`
   - Consider deploying to staging environment for validation

3. **Post-Merge**:
   - Monitor `master` branch CI build
   - Deploy to production after validation

**If No CI/CD**: Manual build and test validation before merge

---

### Documentation Updates

**Update Documentation**:

- [ ] README.md: Update .NET version requirement
- [ ] Build instructions: Update to .NET 8.0 SDK
- [ ] Developer setup guide: .NET 8.0 prerequisites
- [ ] Release notes: Document .NET 8.0 upgrade
- [ ] Change log: Add entry for version upgrade

**Commit Documentation**:
- Include documentation updates in upgrade commit, OR
- Separate commit: `docs: Update documentation for .NET 8.0`

---

### Source Control Best Practices for This Upgrade

1. ✅ **Work on feature branch** (`upgrade-to-NET8`)
2. ✅ **Single commit for All-At-Once** (or minimal staged commits)
3. ✅ **Comprehensive commit message** (what, why, testing)
4. ✅ **Tag or backup before merge** (safety net)
5. ✅ **Code review before merge** (if team process)
6. ✅ **CI/CD validation** (if configured)
7. ✅ **Merge to `master`** only after full validation
8. ✅ **Monitor production** after deployment

---

### All-At-Once Strategy Alignment

The **single commit approach aligns perfectly with All-At-Once strategy**:
- Reflects atomic nature of upgrade
- Clear before/after boundary
- Easy rollback if needed
- Simplifies code review (all changes together)

This approach treats the upgrade as a single unit of work, consistent with the All-At-Once execution model.

---

## Success Criteria

This section defines the measurable criteria that determine when the .NET 8.0 upgrade is complete and successful.

---

### Technical Criteria

#### Build Success
- [ ] Solution builds with `dotnet build NameParser.sln` and exits with code 0
- [ ] **0 compilation errors** across all projects
- [ ] Acceptable warning count (no critical warnings)
- [ ] Both NameParser.exe and NameParser.UI.exe binaries generated
- [ ] All NuGet packages restore without conflicts
- [ ] No package dependency warnings

#### Target Framework Migration
- [ ] NameParser.csproj targets `net8.0-windows`
- [ ] NameParser.UI.csproj targets `net8.0-windows`
- [ ] Both projects use SDK-style project format
- [ ] No references to .NET Framework 4.8 remain

#### Package Updates
- [ ] All packages updated to planned versions (see §Package Update Reference)
- [ ] Entity Framework Core packages aligned at version 8.0.11
- [ ] No duplicate package references (Microsoft.Extensions.Primitives deduplicated)
- [ ] Newtonsoft.Json converted from HintPath to PackageReference in NameParser
- [ ] packages.config removed from NameParser.UI
- [ ] `dotnet list package` shows no vulnerabilities
- [ ] `dotnet list package --outdated` shows no critical outdated packages

#### Code Quality
- [ ] All breaking changes from EF Core 3.1 → 8.0 addressed
- [ ] No `[Obsolete]` API usage with errors
- [ ] Code follows project conventions and style
- [ ] No hardcoded values or temporary debug code
- [ ] Dispose patterns correct (no resource leaks)

---

### Functional Criteria

#### Data Access
- [ ] All Entity Framework Core CRUD operations work correctly
- [ ] Queries return expected results (no query behavior regressions)
- [ ] Complex queries with joins execute successfully
- [ ] Transactions work correctly
- [ ] Database migrations compatible (or successfully regenerated)
- [ ] Connection strings work
- [ ] No data loss or corruption

#### Excel Processing
- [ ] ExcelDataReader successfully reads Excel files
- [ ] Race data parsed correctly from Excel files
- [ ] Excel file format support unchanged
- [ ] Error handling for malformed files works
- [ ] Office Interop functions correctly (if used)

#### Configuration
- [ ] `ConfigurationManager.AppSettings` accessible
- [ ] `ConfigurationManager.ConnectionStrings` accessible
- [ ] All configuration values load correctly
- [ ] No configuration parsing errors

#### Console Application (NameParser)
- [ ] Application launches without errors
- [ ] All command-line workflows complete successfully
- [ ] Output files generated correctly
- [ ] Logging functional
- [ ] Graceful shutdown

#### UI Application (NameParser.UI)
- [ ] Application launches without errors
- [ ] All windows/views render correctly
- [ ] Data binding works correctly
- [ ] All button clicks and interactions functional
- [ ] Project reference to NameParser resolves
- [ ] No visual glitches or layout issues

#### Integration
- [ ] End-to-end workflows execute successfully
- [ ] Data flows correctly between console app, database, and UI
- [ ] No functional regressions compared to .NET Framework 4.8 version

---

### Performance Criteria

- [ ] Application startup time acceptable (no >20% regression)
- [ ] EF Core query performance acceptable (no >20% regression)
- [ ] Excel file processing time acceptable
- [ ] Memory usage within acceptable range
- [ ] No memory leaks detected
- [ ] No excessive CPU usage

**Note**: If performance regressions detected, investigate and optimize, or document acceptable trade-offs

---

### Testing Criteria

#### Test Execution
- [ ] All unit tests pass (if unit tests exist)
- [ ] All integration tests pass
- [ ] All system tests pass
- [ ] Manual testing checklist complete
- [ ] No critical bugs discovered
- [ ] All test failures addressed and resolved

#### Test Coverage
- [ ] Data access layer tested comprehensively (high priority)
- [ ] Excel processing tested with sample files
- [ ] UI functionality tested (all views/interactions)
- [ ] End-to-end workflows tested
- [ ] Error handling tested (edge cases)

---

### Documentation Criteria

- [ ] README.md updated with .NET 8.0 requirement
- [ ] Build instructions updated
- [ ] Developer setup guide updated
- [ ] Known issues documented (if any)
- [ ] Migration notes documented
- [ ] Release notes updated

---

### Source Control Criteria

- [ ] All changes committed to `upgrade-to-NET8` branch
- [ ] Commit message comprehensive and accurate
- [ ] Code review completed (if team process requires)
- [ ] Approved for merge to `master`
- [ ] CI/CD build passes (if configured)

---

### Deployment Criteria (for merge to master)

- [ ] All technical criteria met
- [ ] All functional criteria met
- [ ] All testing criteria met
- [ ] Team approval obtained
- [ ] Ready for production deployment

---

### All-At-Once Strategy Specific Criteria

Since the All-At-Once strategy upgrades both projects simultaneously:

- [ ] Both projects build successfully in single solution build
- [ ] Both projects run without errors
- [ ] No intermediate states or multi-targeting
- [ ] Single commit captures entire upgrade (or minimal staged commits)
- [ ] Unified test run validates entire solution

---

### Acceptance Checklist

**Before declaring upgrade complete, verify**:

#### Phase 0: Prerequisites
- [x] .NET 8.0 SDK installed ✅
- [x] Git branch created (`upgrade-to-NET8`) ✅
- [x] Pending changes committed ✅
- [ ] SQL Server version verified (2012+)
- [ ] Test environment prepared

#### Phase 1: Atomic Upgrade
- [ ] Both projects converted to SDK-style
- [ ] TargetFramework updated to `net8.0-windows` (both projects)
- [ ] All packages updated to target versions
- [ ] Duplicate packages removed
- [ ] Solution builds with 0 errors
- [ ] All compilation errors fixed

#### Phase 2: Validation
- [ ] Data access integration tests pass
- [ ] Excel processing tests pass
- [ ] Configuration tests pass
- [ ] Console application system tests pass
- [ ] UI application system tests pass
- [ ] End-to-end integration tests pass
- [ ] Performance validated

#### Phase 3: Finalization
- [ ] Documentation updated
- [ ] Changes committed with comprehensive message
- [ ] Code review completed
- [ ] Ready for merge to `master`

---

### Sign-Off

**Project Lead**: [Name] - [ ] Approved for merge  
**Technical Lead**: [Name] - [ ] Approved for merge  
**QA Lead**: [Name] - [ ] Testing complete  

**Date of Completion**: _______________

---

### Post-Upgrade Validation

**After merge to `master` and deployment**:

- [ ] Production build successful
- [ ] Application deployed to production environment
- [ ] Smoke tests pass in production
- [ ] Monitoring shows no errors
- [ ] Team members can build and run locally
- [ ] No rollback required

---

### Definition of Done

**The .NET 8.0 upgrade is DONE when**:

1. ✅ All Technical Criteria met
2. ✅ All Functional Criteria met
3. ✅ All Performance Criteria met
4. ✅ All Testing Criteria met
5. ✅ All Documentation Criteria met
6. ✅ All Source Control Criteria met
7. ✅ Code merged to `master`
8. ✅ Deployed to production (or ready for deployment)
9. ✅ Team sign-off obtained
10. ✅ No critical issues discovered

**At this point**: The solution is successfully upgraded to .NET 8.0 and ready for continued development and production use.

---

### Success Metrics

**Measurable Outcomes**:

- **Build Status**: 0 errors, minimal warnings
- **Test Pass Rate**: 100% (all tests pass)
- **Functional Regression**: 0 (no features broken)
- **Performance Regression**: <20% (acceptable range)
- **Security Vulnerabilities**: 0 (all packages secure)
- **Deployment Success**: 100% (no rollbacks)

**Project Success**: Upgrade completed within estimated time (12-19 hours), with all functionality preserved and validated.

---

### Lessons Learned (Post-Upgrade)

**After completion, document**:

- What went well
- What was challenging
- Unexpected issues encountered
- Time estimates accuracy
- Process improvements for future upgrades

**Future Reference**: Use lessons learned to inform future .NET version upgrades or similar migration projects.

---

*Plan generated by GitHub Copilot App Modernization Agent*
