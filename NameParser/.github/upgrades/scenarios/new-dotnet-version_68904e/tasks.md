# NameParser .NET 8.0 Upgrade Tasks

## Overview

This document tracks the execution of the NameParser solution upgrade from .NET Framework 4.8 to .NET 8.0. Both projects will be upgraded simultaneously in a single atomic operation, followed by validation.

**Progress**: 3/3 tasks complete (100%) ![0%](https://progress-bar.xyz/100)

---

## Tasks

### [✓] TASK-001: Verify prerequisites *(Completed: 2026-02-01 14:16)*
**References**: Plan §Phase 0: Prerequisites

- [✓] (1) Verify .NET 8.0 SDK installed per Plan §Prerequisites
- [✓] (2) SDK version 8.0 or higher detected (**Verify**)
- [✓] (3) Verify SQL Server version is 2012 or higher (required for EF Core 8.0)
- [✓] (4) SQL Server version meets minimum requirements (**Verify**)

---

### [✓] TASK-002: Atomic framework and package upgrade *(Completed: 2026-02-01 14:18)*
**References**: Plan §Phase 1: Atomic Upgrade, Plan §Detailed Execution Steps, Plan §Package Update Reference, Plan §Breaking Changes Catalog

- [✓] (1) Convert NameParser.csproj and NameParser.UI.csproj to SDK-style format per Plan §Step 1: Convert Projects to SDK-Style
- [✓] (2) Both projects use SDK-style format (**Verify**)
- [✓] (3) Update TargetFramework to net8.0-windows in both projects per Plan §Step 2
- [✓] (4) Both projects target net8.0-windows (**Verify**)
- [✓] (5) Update all package references per Plan §Package Update Reference (key updates: EF Core 3.1.32 → 8.0.11, Microsoft.EntityFrameworkCore.SqlServer 3.1.32 → 8.0.11, Microsoft.EntityFrameworkCore.Tools 10.0.2 → 8.0.11, deduplicate Microsoft.Extensions.Primitives to single 8.0.0 version, ExcelDataReader 3.8.0 → 3.8.2, convert Newtonsoft.Json to PackageReference 13.0.3)
- [✓] (6) All packages updated to target versions, duplicates removed (**Verify**)
- [✓] (7) Restore dependencies with dotnet restore NameParser.sln
- [✓] (8) All dependencies restored successfully (**Verify**)
- [✓] (9) Build solution and fix all compilation errors per Plan §Breaking Changes Catalog (focus areas: EF Core 3.1 → 8.0 query behavior changes, obsolete API replacements, namespace changes, configuration updates)
- [✓] (10) Solution builds with 0 errors (**Verify**)

---

### [✓] TASK-003: Final commit *(Completed: 2026-02-01 14:19)*
**References**: Plan §Source Control Strategy

- [✓] (1) Commit all changes with message: "Upgrade solution to .NET 8.0"

---





