# ⚠️ IMPORTANT: Required Setup Steps

## NuGet Package Installation Required

The solution requires Entity Framework 6 to be installed. Please follow these steps:

### Step 1: Install Entity Framework via Package Manager Console

Open **Package Manager Console** in Visual Studio:
- Tools → NuGet Package Manager → Package Manager Console

Run the following commands:

```powershell
# Install Entity Framework for NameParser project
Install-Package EntityFramework -Version 6.4.4 -ProjectName NameParser

# Install Entity Framework for NameParser.UI project  
Install-Package EntityFramework -Version 6.4.4 -ProjectName NameParser.UI
```

### Step 2: Verify Installation

Check that the following references were added:
- EntityFramework.dll
- EntityFramework.SqlServer.dll

### Step 3: Build Solution

After installing Entity Framework:
1. Build → Rebuild Solution
2. Fix any remaining errors
3. Run the application

---

## Alternative: Use NuGet Package Manager UI

1. Right-click **Solution** → **Manage NuGet Packages for Solution**
2. Click **Browse** tab
3. Search for "EntityFramework"
4. Select **EntityFramework 6.4.4**
5. Check both projects: **NameParser** and **NameParser.UI**
6. Click **Install**

---

## What Was Built

All code files are ready and waiting for Entity Framework to be installed:

### Database Layer ✅
- ✅ RaceEntity.cs
- ✅ ClassificationEntity.cs  
- ✅ RaceManagementContext.cs
- ✅ RaceRepository.cs
- ✅ ClassificationRepository.cs

### UI Layer ✅
- ✅ MainViewModel.cs
- ✅ ViewModelBase.cs
- ✅ RelayCommand.cs
- ✅ MainWindow.xaml
- ✅ BooleanToVisibilityConverter.cs

### Configuration ✅
- ✅ App.config (both projects) with connection strings
- ✅ packages.config files

### Documentation ✅
- ✅ UI_USER_GUIDE.md
- ✅ UI_SETUP.md
- ✅ UI_IMPLEMENTATION_SUMMARY.md

---

## After Installing EntityFramework

The build should succeed and you'll have:
- ✅ Complete WPF UI application
- ✅ Database persistence with Entity Framework
- ✅ Upload and process races
- ✅ View and download results
- ✅ Year-based race organization
- ✅ Local SQL database

---

## Complete Setup Sequence

```
1. Install EntityFramework (see above) ← YOU ARE HERE
2. Build Solution
3. Run NameParser.UI
4. Use the application!
```

---

## Why This Is Needed

Entity Framework is a NuGet package that provides:
- Database access via DbContext
- Entity mapping with attributes
- LINQ to SQL queries
- Database migrations
- Code-first approach

It cannot be pre-installed via code files - it must be added through NuGet Package Manager.

---

## Quick Reference

**Install Command:**
```powershell
Install-Package EntityFramework -Version 6.4.4 -ProjectName NameParser
Install-Package EntityFramework -Version 6.4.4 -ProjectName NameParser.UI
```

**Check Installation:**
- Solution Explorer → References → Look for EntityFramework

**Build:**
- Build → Rebuild Solution (Ctrl+Shift+B)

---

Once Entity Framework is installed, everything will build successfully! 🚀
