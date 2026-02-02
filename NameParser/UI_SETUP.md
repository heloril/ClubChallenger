# UI Setup Instructions

## Quick Setup

### 1. NuGet Packages Required

For **NameParser** project:
```
- EntityFramework (6.4.4)
- Newtonsoft.Json (13.0.3)  
- ExcelDataReader (already installed)
```

For **NameParser.UI** project:
```
- EntityFramework (6.4.4)
- Newtonsoft.Json (13.0.3)
```

### 2. Install NuGet Packages

Open Package Manager Console in Visual Studio and run:

```powershell
# For NameParser project
Install-Package EntityFramework -Version 6.4.4 -ProjectName NameParser
Install-Package Newtonsoft.Json -Version 13.0.3 -ProjectName NameParser

# For NameParser.UI project
Install-Package EntityFramework -Version 6.4.4 -ProjectName NameParser.UI
Install-Package Newtonsoft.Json -Version 13.0.3 -ProjectName NameParser.UI
```

### 3. Add Project References

In **NameParser.UI** project, add reference to **NameParser** project:
- Right-click NameParser.UI → Add → Reference
- Projects → Solution → Check "NameParser"
- Click OK

### 4. Required System Components

- **SQL Server LocalDB**: Should be installed with Visual Studio
  - If not, download from: https://aka.ms/sqlexpress
  
- **Microsoft Office Interop**: For Excel file reading
  - Already referenced via COM in NameParser project

### 5. Build Solution

1. Right-click Solution → Restore NuGet Packages
2. Build → Build Solution (Ctrl+Shift+B)
3. Resolve any errors

### 6. Set Startup Project

- Right-click **NameParser.UI** → Set as Startup Project
- Or keep both projects and choose which to run

## Project Structure After Setup

```
Solution 'NameParser'
├── NameParser (Console + Library)
│   ├── Application/
│   ├── Domain/
│   ├── Infrastructure/
│   │   ├── Data/                    ← NEW: Database layer
│   │   │   ├── Models/
│   │   │   │   ├── RaceEntity.cs
│   │   │   │   └── ClassificationEntity.cs
│   │   │   ├── RaceManagementContext.cs
│   │   │   ├── RaceRepository.cs
│   │   │   └── ClassificationRepository.cs
│   │   ├── Repositories/
│   │   └── Services/
│   ├── Presentation/
│   ├── app.config                   ← UPDATED: With EF config
│   └── packages.config
│
└── NameParser.UI (WPF Application)
    ├── ViewModels/                  ← NEW: MVVM ViewModels
    │   ├── MainViewModel.cs
    │   ├── ViewModelBase.cs
    │   └── RelayCommand.cs
    ├── MainWindow.xaml              ← UPDATED: New UI
    ├── MainWindow.xaml.cs           ← UPDATED: Simplified
    ├── App.xaml
    ├── App.config                   ← UPDATED: With EF config
    └── packages.config              ← NEW: Package references
```

## Configuration Files

### App.config (Both Projects)

Both projects now include:
- Entity Framework configuration
- Connection string to LocalDB
- Provider configuration

Connection string:
```xml
<add name="RaceManagementDb" 
     connectionString="Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=|DataDirectory|\RaceManagement.mdf;Integrated Security=True;Connect Timeout=30" 
     providerName="System.Data.SqlClient" />
```

## Running the Application

### Option 1: Run UI (Recommended)
1. Set **NameParser.UI** as startup project
2. Press F5 or click Start
3. WPF window opens with full UI

### Option 2: Run Console
1. Set **NameParser** as startup project
2. Press F5
3. Console application runs (original functionality)

## Database

### Automatic Creation
- Database created automatically on first run
- Location: `NameParser.UI\bin\Debug\RaceManagement.mdf`
- No manual setup required

### Viewing Database
1. Open **SQL Server Object Explorer** in Visual Studio
2. Expand **(localdb)\MSSQLLocalDB**
3. Find **RaceManagement** database
4. View tables: Races, Classifications

### Database File Location
```
NameParser.UI\bin\Debug\
    ├── RaceManagement.mdf      ← Database file
    ├── RaceManagement_log.ldf  ← Log file
    └── Members.json            ← Required: Member data
```

## Important Files

### Must Have
- **Members.json**: In NameParser.UI\bin\Debug\ directory
  ```json
  [
    {
      "FirstName": "John",
      "LastName": "Doe",
      "Email": "john.doe@example.com"
    }
  ]
  ```

### Optional
- **Excel files**: For processing races
- Format: `<number>.<km>.<name>.xlsx`
- Example: `1.10.Marathon.xlsx`

## Troubleshooting Build

### Error: EntityFramework not found
**Solution**: 
```powershell
Install-Package EntityFramework -Version 6.4.4
```

### Error: Cannot find type 'DbContext'
**Solution**: Add using statement
```csharp
using System.Data.Entity;
```

### Error: Excel COM not registered
**Solution**: 
- Install Microsoft Office
- Or install Office Primary Interop Assemblies (PIA)

### Error: Members.json not found
**Solution**:
- Copy Members.json to output directory
- Or set "Copy to Output Directory" = "Copy always"

### Error: Database connection failed
**Solution**:
- Check SQL Server LocalDB is installed
- Run: `sqllocaldb info` in command prompt
- If not found, install SQL Server Express LocalDB

## Building for Release

1. Change to Release configuration
2. Build Solution
3. Output in: `NameParser.UI\bin\Release\`
4. Copy required files:
   - NameParser.UI.exe
   - NameParser.dll
   - All DLL dependencies
   - App.config
   - Members.json
   - Create empty RaceManagement.mdf (will auto-create)

## Deployment Checklist

- [ ] .NET Framework 4.8 installed on target machine
- [ ] SQL Server LocalDB installed (or SQL Server Express)
- [ ] Microsoft Office or Excel runtime (for Excel reading)
- [ ] Members.json file in application directory
- [ ] App.config with correct connection string
- [ ] Write permissions for database file creation

## Next Steps

After successful setup:
1. Read **UI_USER_GUIDE.md** for usage instructions
2. Test with sample Excel file
3. Verify database creation
4. Process a test race
5. Download results to verify functionality

## Support Resources

- **Entity Framework**: https://docs.microsoft.com/ef/
- **WPF**: https://docs.microsoft.com/dotnet/desktop/wpf/
- **MVVM Pattern**: https://docs.microsoft.com/archive/msdn-magazine/2009/february/patterns-wpf-apps-with-the-model-view-viewmodel-design-pattern

---

**Setup Time**: ~10 minutes
**Difficulty**: Beginner to Intermediate
**Prerequisites**: Visual Studio 2019+ with .NET Framework 4.8
