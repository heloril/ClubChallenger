# URGENT: Add System Reference to NameParser Project

## The Problem

You're getting this error repeatedly:
```
CS0012: The type 'IListSource' is defined in an assembly that is not referenced. 
You must add a reference to assembly 'System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'.
```

## The Solution (Choose ONE method)

### Method 1: Visual Studio UI (EASIEST - DO THIS FIRST)

1. **Close Visual Studio completely**
2. **Reopen Visual Studio**
3. In **Solution Explorer**, right-click the **NameParser** project
4. Select **Add → Reference...**
5. In the **Reference Manager** dialog:
   - Go to **Assemblies** → **Framework**
   - **Check the checkbox** next to **System** (should be at the top of the list)
   - Also check **System.ComponentModel.DataAnnotations**
   - Click **OK**
6. **Save all**
7. **Build → Rebuild Solution**

### Method 2: Manual Edit (If Method 1 doesn't work)

1. **Close Visual Studio**
2. Open **NameParser.csproj** in a text editor (Notepad++, VS Code, etc.)
3. Find this section (around line 35-42):
   ```xml
   <ItemGroup>
     <Reference Include="Microsoft.CSharp" />
     <Reference Include="Newtonsoft.Json">
       <HintPath>lib\Newtonsoft.Json.dll</HintPath>
     </Reference>
     <Reference Include="System.Core" />
     <Reference Include="System.Data" />
     <Reference Include="System.Windows" />
     <Reference Include="System.Windows.Forms" />
   </ItemGroup>
   ```
4. Add these TWO lines right after the Newtonsoft.Json section:
   ```xml
   <Reference Include="System" />
   <Reference Include="System.ComponentModel.DataAnnotations" />
   ```
   
   So it looks like:
   ```xml
   <ItemGroup>
     <Reference Include="Microsoft.CSharp" />
     <Reference Include="Newtonsoft.Json">
       <HintPath>lib\Newtonsoft.Json.dll</HintPath>
     </Reference>
     <Reference Include="System" />
     <Reference Include="System.ComponentModel.DataAnnotations" />
     <Reference Include="System.Core" />
     <Reference Include="System.Data" />
     <Reference Include="System.Windows" />
     <Reference Include="System.Windows.Forms" />
   </ItemGroup>
   ```
5. **Save the file**
6. **Reopen Visual Studio**
7. **Build → Rebuild Solution**

### Method 3: PowerShell Script

1. **Close Visual Studio**
2. Open PowerShell in the **solution directory** (where NameParser.csproj is located)
3. Run:
   ```powershell
   .\fix-references.ps1
   ```
4. **Reopen Visual Studio**
5. **Build → Rebuild Solution**

## After Adding References

You still need to install Entity Framework:

Open **Package Manager Console** in Visual Studio:
```powershell
Install-Package EntityFramework -Version 6.4.4 -ProjectName NameParser
Install-Package EntityFramework -Version 6.4.4 -ProjectName NameParser.UI
```

## Complete Checklist

- [ ] **Step 1**: Close Visual Studio
- [ ] **Step 2**: Add System reference (use Method 1, 2, or 3 above)
- [ ] **Step 3**: Reopen Visual Studio  
- [ ] **Step 4**: Clean Solution (Build → Clean Solution)
- [ ] **Step 5**: Install EntityFramework via Package Manager Console
- [ ] **Step 6**: Rebuild Solution (Build → Rebuild Solution)
- [ ] **Step 7**: Verify no errors ✅

## Why This Happens

The database Entity Framework code uses:
- `System` assembly for core types like `IListSource`
- `System.ComponentModel.DataAnnotations` for attributes like `[Key]`, `[Required]`
- `EntityFramework` package for database operations

These must ALL be referenced for the code to compile.

## If Still Getting Errors

1. Make sure **both** references are added:
   - System ✅
   - System.ComponentModel.DataAnnotations ✅
   
2. Make sure Entity Framework is installed:
   - NameParser project ✅
   - NameParser.UI project ✅

3. Clean and Rebuild:
   - Build → Clean Solution
   - Build → Rebuild Solution

4. Check Solution Explorer → NameParser → References
   - Should see "System" in the list
   - Should see "System.ComponentModel.DataAnnotations" in the list
   - Should see "EntityFramework" in the list (after installing package)

## Visual Confirmation

In **Solution Explorer**, expand **NameParser → References**. You should see:
```
References
├── EntityFramework
├── EntityFramework.SqlServer
├── ExcelDataReader
├── ExcelDataReader.DataSet
├── Microsoft.CSharp
├── Microsoft.Office.Interop.Excel
├── Newtonsoft.Json
├── System                              ← MUST BE HERE
├── System.ComponentModel.DataAnnotations ← MUST BE HERE
├── System.Core
├── System.Data
├── System.Windows
└── System.Windows.Forms
```

---

**Do Method 1 first - it's the easiest and most reliable!**
