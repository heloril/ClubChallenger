# Fix for IListSource Error

## Problem
You're seeing this error:
```
The type 'IListSource' is defined in an assembly that is not referenced. 
You must add a reference to assembly 'System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'.
```

## Solution: Add Missing Assembly References

### Step 1: Add References to NameParser Project

**Via Visual Studio:**
1. In **Solution Explorer**, right-click on **NameParser** project
2. Select **Add** → **Reference**
3. In the Reference Manager, check the **Assemblies** → **Framework** section
4. Make sure these are checked:
   - ✅ **System** (if not checked, check it)
   - ✅ **System.ComponentModel.DataAnnotations** (if not checked, check it)
5. Click **OK**

### Step 2: Manually Edit NameParser.csproj (If Above Doesn't Work)

1. **Unload the project**: Right-click **NameParser** → **Unload Project**
2. **Edit the project file**: Right-click **NameParser** → **Edit NameParser.csproj**
3. Find the `<ItemGroup>` section with references (around line 35-42)
4. Add these two lines:
   ```xml
   <Reference Include="System" />
   <Reference Include="System.ComponentModel.DataAnnotations" />
   ```

It should look like this:
```xml
<ItemGroup>
  <Reference Include="Microsoft.CSharp" />
  <Reference Include="Newtonsoft.Json">
    <HintPath>lib\Newtonsoft.Json.dll</HintPath>
  </Reference>
  <Reference Include="System" />                              <!-- ADD THIS -->
  <Reference Include="System.ComponentModel.DataAnnotations" /> <!-- ADD THIS -->
  <Reference Include="System.Core" />
  <Reference Include="System.Data" />
  <Reference Include="System.Windows" />
  <Reference Include="System.Windows.Forms" />
</ItemGroup>
```

5. Save the file
6. **Reload the project**: Right-click **NameParser** → **Reload Project**

### Step 3: Install Entity Framework (Still Required)

Even after adding these references, you still need Entity Framework. Open **Package Manager Console**:

```powershell
Install-Package EntityFramework -Version 6.4.4 -ProjectName NameParser
Install-Package EntityFramework -Version 6.4.4 -ProjectName NameParser.UI
```

### Step 4: Add Project Reference from UI to NameParser

Make sure **NameParser.UI** references **NameParser**:
1. Right-click **NameParser.UI** → **Add** → **Reference**
2. Check **Projects** → **Solution** → **NameParser**
3. Click **OK**

### Step 5: Rebuild

1. **Build** → **Clean Solution**
2. **Build** → **Rebuild Solution**

---

## Why This Happened

The database layer code uses:
- **System** assembly for basic types like `IListSource`
- **System.ComponentModel.DataAnnotations** for attributes like `[Key]`, `[Required]`, `[MaxLength]`
- **EntityFramework** package for `DbContext`, `DbSet`, etc.

These references were missing from the NameParser project.

---

## Quick Checklist

- [ ] Added `System` reference to NameParser
- [ ] Added `System.ComponentModel.DataAnnotations` reference to NameParser  
- [ ] Installed EntityFramework in NameParser (`Install-Package EntityFramework -Version 6.4.4`)
- [ ] Installed EntityFramework in NameParser.UI (`Install-Package EntityFramework -Version 6.4.4`)
- [ ] NameParser.UI has project reference to NameParser
- [ ] Clean and Rebuild Solution
- [ ] All errors resolved ✅

---

## If You Still Get Errors

If you continue to see Entity Framework-related errors after adding System references, you MUST install Entity Framework via NuGet Package Manager. There is no workaround for this - Entity Framework cannot be added by modifying code files alone.

---

## Alternative: Temporary Workaround

If you want to test the UI without Entity Framework, you can temporarily:
1. Comment out all database-related code in `MainViewModel.cs`
2. Remove the database repository usage
3. Test the UI layout only

But for full functionality, Entity Framework is required.
