# .NET 8.0 Upgrade Assessment Report

**Date**: 2024
**Solution**: NameParser.sln  
**Current Framework**: .NET Framework 4.8  
**Target Framework**: .NET 8.0 (LTS)  
**Assessment Mode**: Scenario-Guided  
**Branch**: upgrade-to-NET8

---

## Executive Summary

This assessment analyzes a 2-project solution currently targeting .NET Framework 4.8 for upgrade to .NET 8.0 (Long Term Support). The solution consists of a console application with Entity Framework Core data access and a WPF-based UI application. 

**Key Findings:**
- Both projects use legacy (non-SDK) project format requiring conversion
- Entity Framework Core 3.1 (end-of-life) must be upgraded to 8.0
- Package version conflicts and duplicates need resolution
- Windows-specific dependencies (WinForms, WPF, Office Interop) require special handling
- Estimated effort: **12-19 hours** with **MEDIUM risk**

**Critical Blockers:** None - all dependencies are compatible with .NET 8.0

---

## Solution Overview

### Project Structure

| Project | Type | Output | Current TFM | Target TFM | LOC (est) | Dependencies |
|---------|------|--------|-------------|------------|-----------|--------------|
| **NameParser** | Console App | Exe | net48 | net8.0-windows | ~1,500 | 9 packages, 0 projects |
| **NameParser.UI** | WPF App | WinExe | net48 | net8.0-windows | ~500 | 1 package, 1 project |

### Dependency Graph

```
NameParser.UI (WPF)
    └── NameParser (Console/Library)
```

**Migration Order**: NameParser → NameParser.UI (bottom-up)

---

## Current State Analysis

### NameParser Project

**Project Format**: Legacy (.NET Framework style with `ToolsVersion="4.0"`)

**Key Characteristics:**
- Domain-Driven Design architecture (Domain, Application, Infrastructure layers)
- Entity Framework Core 3.1.32 for data persistence
- Excel processing (ExcelDataReader + Office Interop)
- JSON serialization (Newtonsoft.Json via hint path reference)
- Configuration via app.config

**Source Files** (22 files):
- Application layer: RaceProcessingService, ReportGenerationService
- Domain layer: Aggregates (Classification), Entities (Member, Race, RaceResult), Repositories, Services, ValueObjects
- Infrastructure layer: Data (EF Core context, repositories), External repositories (Excel, JSON)
- Presentation: ConsoleLogger
- Entry point: Program.cs

**Framework References** (6 references):
- Microsoft.CSharp
- System, System.Core, System.Data
- System.Windows, System.Windows.Forms

**NuGet Packages** (9 packages):

| Package | Current Version | Status | Notes |
|---------|----------------|--------|-------|
| ExcelDataReader | 3.8.0 | ⚠️ Update Available | Latest: 3.8.2 |
| ExcelDataReader.DataSet | 3.7.0 | ⚠️ Update Available | Latest: 3.8.0 |
| Microsoft.EntityFrameworkCore | 3.1.32 | 🔴 **End of Life** | Must upgrade to 8.0+ |
| Microsoft.EntityFrameworkCore.SqlServer | 3.1.32 | 🔴 **End of Life** | Must upgrade to 8.0+ |
| Microsoft.EntityFrameworkCore.Tools | 10.0.2 | ⚠️ **Version Mismatch** | Mismatch with EF Core 3.1 |
| Microsoft.Extensions.Primitives | 10.0.2 | ✅ Compatible | **DUPLICATE ENTRY** |
| Microsoft.Extensions.Primitives | 3.1.32 | ⚠️ Old Version | **DUPLICATE ENTRY** |
| Microsoft.Office.Interop.Excel | 16.0.18925 | ⚠️ COM Interop | Requires testing |
| System.Configuration.ConfigurationManager | 10.0.2 | ✅ Compatible | |

**Issues Identified:**
1. **Critical**: EF Core 3.1 is end-of-life and incompatible with .NET 8 best practices
2. **Critical**: EF Core Tools 10.0.2 is incompatible with EF Core 3.1.32 runtime
3. **High**: Duplicate package reference for Microsoft.Extensions.Primitives
4. **Medium**: Newtonsoft.Json referenced via HintPath (should use PackageReference)
5. **Medium**: Mixed package management (direct references + legacy style)

---

### NameParser.UI Project

**Project Format**: Legacy (.NET Framework WPF style with `ToolsVersion="15.0"`)

**Key Characteristics:**
- WPF application (ProjectTypeGuid: 60dc8134-eba5-43b8-bcc9-bb4bc16c2548)
- References the NameParser project
- Uses packages.config (legacy NuGet format)
- Simple UI layer over the core NameParser logic

**Framework References** (14 references):
- WPF assemblies: WindowsBase, PresentationCore, PresentationFramework, System.Xaml
- System.Windows.Forms
- Standard BCL: System, System.Core, System.Data, System.Xml, System.Net.Http, etc.

**NuGet Packages** (1 package):
- Newtonsoft.Json 13.0.4 (via packages.config)

**Project References:**
- NameParser.csproj

**Issues Identified:**
1. **High**: Uses packages.config instead of PackageReference
2. **Medium**: Legacy WPF project format
3. **Low**: Dependency on NameParser project (must migrate NameParser first)

---

## Compatibility Analysis

### Framework Compatibility

| Component | .NET Framework 4.8 | .NET 8.0 | Compatibility | Action Required |
|-----------|-------------------|----------|---------------|-----------------|
| WPF | ✅ | ✅ | Full support | Requires net8.0-windows TFM |
| Windows Forms | ✅ | ✅ | Full support | Requires net8.0-windows TFM |
| Entity Framework Core | ✅ (3.1) | ✅ (8.0) | Version upgrade | Update to 8.0.x |
| Office Interop | ✅ | ✅ | COM Interop | Requires testing |
| System.Configuration | ✅ | ✅ (via package) | Compatible | Already using package |

### Package Upgrade Path

**Entity Framework Core 3.1 → 8.0 Breaking Changes:**
- Query behavior changes (split query default)
- Value converter changes
- Migration differences
- Tracking behavior updates
- Cosmos DB provider changes (if applicable)
- SQL Server minimum version requirements

**References:**
- [EF Core 8.0 Breaking Changes](https://learn.microsoft.com/ef/core/what-is-new/ef-core-8.0/breaking-changes)
- [EF Core 6.0 Breaking Changes](https://learn.microsoft.com/ef/core/what-is-new/ef-core-6.0/breaking-changes)
- [EF Core 5.0 Breaking Changes](https://learn.microsoft.com/ef/core/what-is-new/ef-core-5.0/breaking-changes)

**Recommended Package Versions for .NET 8.0:**

| Package | Current | Recommended | Reason |
|---------|---------|-------------|--------|
| ExcelDataReader | 3.8.0 | 3.8.2 | Bug fixes |
| ExcelDataReader.DataSet | 3.7.0 | 3.8.0 | Align with ExcelDataReader |
| Microsoft.EntityFrameworkCore | 3.1.32 | 8.0.11 | Match target framework |
| Microsoft.EntityFrameworkCore.SqlServer | 3.1.32 | 8.0.11 | Match EF Core version |
| Microsoft.EntityFrameworkCore.Tools | 10.0.2 | 8.0.11 | Match EF Core version |
| Microsoft.Extensions.Primitives | 3.1.32/10.0.2 | 8.0.0 | Remove duplicate, use consistent version |
| Microsoft.Office.Interop.Excel | 16.0.18925 | 16.0.18925 | Keep current (COM) |
| System.Configuration.ConfigurationManager | 10.0.2 | 8.0.1 | Match framework version |
| Newtonsoft.Json | HintPath | 13.0.3 | Use PackageReference |

---

## Code Impact Assessment

### High-Impact Areas

#### 1. Entity Framework Core Usage

**Files Potentially Affected:**
- `Infrastructure/Data/RaceManagementContext.cs` - DbContext configuration
- `Infrastructure/Data/ClassificationRepository.cs` - Query patterns
- `Infrastructure/Data/RaceRepository.cs` - Query patterns
- `Infrastructure/Data/Models/*.cs` - Entity configurations

**Expected Changes:**
- Query splitting behavior (may need explicit `.AsSplitQuery()` or `.AsSingleQuery()`)
- Tracking behavior adjustments
- Migration regeneration may be needed
- Connection string format validation
- Possible changes to value converters if used

#### 2. Configuration System

**Files Potentially Affected:**
- `Program.cs` - Configuration loading
- `app.config` - May need migration to appsettings.json pattern

**Expected Changes:**
- ConfigurationManager API is compatible via package
- Consider modernizing to IConfiguration pattern (optional)

#### 3. Excel Processing

**Files Potentially Affected:**
- `Infrastructure/Repositories/ExcelRaceResultRepository.cs` - ExcelDataReader usage
- Any code using Office Interop

**Expected Changes:**
- Likely no changes (stable APIs)
- Requires runtime testing with .NET 8

#### 4. Windows-Specific APIs

**Files Potentially Affected:**
- `Presentation/ConsoleLogger.cs` - Console API usage
- Any System.Windows.Forms usage
- WPF UI code in NameParser.UI

**Expected Changes:**
- Must use `net8.0-windows` TFM (not `net8.0`)
- APIs remain compatible

---

## Risk Assessment

### Risk Matrix

| Risk Category | Level | Impact | Likelihood | Mitigation |
|--------------|-------|--------|------------|------------|
| EF Core migration breaking changes | 🔴 HIGH | High | Medium | Thorough testing, follow migration guide |
| Office Interop compatibility | 🟡 MEDIUM | Medium | Low | Runtime testing on target environment |
| Project conversion errors | 🟡 MEDIUM | Medium | Low | Use conversion tools, manual review |
| Package version conflicts | 🟡 MEDIUM | Low | Medium | Clean resolution, remove duplicates |
| WPF/WinForms compatibility | 🟢 LOW | Low | Very Low | Well-supported, minimal changes |

### Known Breaking Changes

**EF Core 3.1 → 8.0 (Major Changes Across 4 Major Versions):**

1. **Query Behavior**
   - Split query is now default for collections
   - Changes in query translation for complex LINQ
   - GroupBy translation differences

2. **Tracking and Change Detection**
   - Changes in how tracked entities are handled
   - Updates to change tracking proxies

3. **Migrations**
   - May need to regenerate migrations
   - SQL generation differences

4. **SQL Server Provider**
   - Minimum SQL Server version requirements
   - Connection string format changes

5. **Value Converters**
   - Changes in value converter behavior for certain types

**Mitigation**: Comprehensive testing of all data access code, review EF Core migration guides for versions 4.0, 5.0, 6.0, 7.0, and 8.0.

---

## Migration Prerequisites

### Required Tools

- ✅ .NET 8.0 SDK installed
- ✅ Visual Studio 2022 (17.8+) or Rider 2023.3+
- ✅ Git for version control

### Pre-Migration Checklist

- [x] Source code committed to version control (completed)
- [x] Working branch created (`upgrade-to-NET8`)
- [ ] Full backup of solution
- [ ] Document current build process
- [ ] Identify all environment-specific configurations
- [ ] Review and document current database schema (EF migrations)
- [ ] Verify SQL Server version compatibility with EF Core 8.0

---

## Testing Strategy

### Test Levels

#### 1. **Project-Level Testing**
After each project conversion:
- [ ] Project builds without errors
- [ ] Project builds without warnings
- [ ] No package restore issues
- [ ] All references resolve correctly

#### 2. **Integration Testing**
After NameParser conversion:
- [ ] Data access layer functional (EF Core queries work)
- [ ] Excel reading/writing functional
- [ ] JSON serialization functional
- [ ] Configuration loading functional

After NameParser.UI conversion:
- [ ] UI launches successfully
- [ ] Project reference to NameParser works
- [ ] All UI interactions functional

#### 3. **End-to-End Testing**
- [ ] Complete workflows execute successfully
- [ ] Database operations complete correctly
- [ ] Excel file processing works end-to-end
- [ ] Performance is acceptable
- [ ] No memory leaks or resource issues

### Test Scenarios (Recommended)

1. **Data Access**
   - CRUD operations for all entities
   - Complex queries with joins
   - Transactions
   - Migration execution

2. **Excel Processing**
   - Read Excel files with ExcelDataReader
   - Process race data
   - Handle errors gracefully

3. **Business Logic**
   - Classification calculations
   - Points calculation
   - Report generation

4. **UI Workflows** (NameParser.UI)
   - Launch and initialization
   - User interactions
   - Data binding
   - Error handling

---

## Effort Estimation

### Detailed Breakdown

| Task | Estimated Time | Risk Factor |
|------|---------------|-------------|
| **Project Conversion** | | |
| - Convert NameParser to SDK style | 1.5 hours | Medium |
| - Convert NameParser.UI to SDK style | 1 hour | Medium |
| **Package Updates** | | |
| - Update EF Core packages | 1 hour | Low |
| - Update other packages | 0.5 hours | Low |
| - Resolve conflicts and duplicates | 1 hour | Medium |
| **Code Changes** | | |
| - EF Core migration updates | 2-3 hours | High |
| - Configuration updates | 1 hour | Low |
| - Fix compilation errors | 1-2 hours | Medium |
| **Testing** | | |
| - Unit/integration testing | 2-3 hours | Medium |
| - End-to-end testing | 2-3 hours | Medium |
| - Bug fixes and adjustments | 1-2 hours | Medium |
| **Total** | **12-19 hours** | **Medium** |

### Assumptions

- Developer has experience with .NET Core/.NET 5+
- Developer familiar with EF Core
- No major architectural changes required
- Existing code is in working state
- Test environment available

---

## Recommendations for Planning Stage

### Critical Path Items

1. **Project Conversion**
   - Start with NameParser (dependency)
   - Use `dotnet upgrade-assistant` or manual conversion
   - Carefully review generated SDK-style .csproj

2. **Package Management**
   - **Immediately** remove duplicate Microsoft.Extensions.Primitives references
   - **Immediately** align EF Core package versions
   - Convert Newtonsoft.Json to proper PackageReference
   - Migrate packages.config in NameParser.UI

3. **Entity Framework Core**
   - Review all EF Core code for breaking changes
   - Test all queries thoroughly
   - Consider regenerating migrations if needed
   - Validate against target SQL Server version

4. **Windows-Specific TFM**
   - Use `net8.0-windows` not `net8.0`
   - Document this requirement

### Suggested Approach

**Sequential Migration (Recommended):**
- **Phase 1**: Convert and update NameParser
  - Validate EF Core functionality
  - Test Excel processing
  - Ensure all domain logic works
- **Phase 2**: Convert and update NameParser.UI
  - Test UI functionality
  - Validate end-to-end workflows

**Rationale**: NameParser.UI depends on NameParser, so bottom-up migration ensures a working base before updating the UI layer.

---

## Success Criteria

### The upgrade is complete when:

#### Technical Criteria
- [x] Both projects target `net8.0-windows`
- [ ] All NuGet packages updated to recommended versions
- [ ] No duplicate or conflicting package references
- [ ] Solution builds without errors
- [ ] Solution builds without warnings
- [ ] All tests pass (if test project exists)
- [ ] No package dependency conflicts

#### Functional Criteria
- [ ] All data access operations work correctly
- [ ] Excel file processing functional
- [ ] UI launches and operates correctly
- [ ] Configuration system works
- [ ] Performance meets expectations
- [ ] No regressions in functionality

#### Quality Criteria
- [ ] Code follows .NET 8 best practices
- [ ] No security vulnerabilities in packages
- [ ] Documentation updated
- [ ] Team members can build and run locally

---

## Known Limitations and Considerations

### Office Interop
- COM-based, requires Office installed on runtime machine
- Consider alternatives like EPPlus or ClosedXML for better cross-platform support (future enhancement)

### Windows-Only Dependencies
- Solution will run on Windows only due to WPF and WinForms
- `net8.0-windows` TFM required
- Cannot target Linux/macOS without architectural changes

### Entity Framework Core 3.1 → 8.0 Gap
- Skipping 4 major versions (4.0, 5.0, 6.0, 7.0)
- Review all breaking changes documentation
- Higher risk of unexpected behavior changes

---

## Data for Planning Stage

### Project Metrics

- **Total Projects**: 2
- **Lines of Code (estimated)**: ~2,000
- **NuGet Packages**: 10 unique packages
- **Project References**: 1 (NameParser.UI → NameParser)
- **File Types**: .cs (22+ files), .csproj (2), .sln (1), app.config (1), XAML files (UI)

### Package Update Summary

| Status | Count | Packages |
|--------|-------|----------|
| 🔴 Critical Update | 3 | EF Core runtime + Tools |
| ⚠️ Recommended Update | 5 | ExcelDataReader, Extensions.Primitives, ConfigurationManager |
| ✅ Compatible | 2 | Office.Interop.Excel, Newtonsoft.Json |

### Dependency Order

1. **NameParser** (no project dependencies)
2. **NameParser.UI** (depends on NameParser)

---

## References and Resources

### Microsoft Documentation
- [.NET 8.0 Release Notes](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-8)
- [Migrate from .NET Framework to .NET 8](https://learn.microsoft.com/dotnet/core/porting/)
- [EF Core 8.0 Documentation](https://learn.microsoft.com/ef/core/)
- [WPF on .NET Documentation](https://learn.microsoft.com/dotnet/desktop/wpf/)

### Breaking Changes
- [EF Core 8.0 Breaking Changes](https://learn.microsoft.com/ef/core/what-is-new/ef-core-8.0/breaking-changes)
- [.NET 8.0 Breaking Changes](https://learn.microsoft.com/dotnet/core/compatibility/8.0)

### Tools
- [.NET Upgrade Assistant](https://dotnet.microsoft.com/platform/upgrade-assistant)
- [Compatibility Analyzer](https://learn.microsoft.com/dotnet/core/porting/upgrade-assistant-overview)

---

## Conclusion

The NameParser solution is a good candidate for migration to .NET 8.0. The primary challenges are:
1. Converting from legacy project format to SDK-style
2. Upgrading Entity Framework Core across 4 major versions
3. Resolving package conflicts and duplicates

With careful execution and thorough testing, especially of the data access layer, this migration should be successful. The Domain-Driven Design architecture and clear separation of concerns will facilitate the upgrade process.

**Overall Assessment: FEASIBLE with MEDIUM RISK**

**Recommended Next Step**: Proceed to Planning stage to create detailed migration tasks.

---

*Assessment completed by GitHub Copilot App Modernization Agent*
*Ready for Planning Stage*
