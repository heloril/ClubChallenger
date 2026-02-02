# CRITICAL FIX: Framework Version Mismatch

## 🔴 The Problem

Your build is failing with this error:
```
warning MSB3274: The primary reference "NameParser.exe" could not be resolved 
because it was built against the ".NETFramework,Version=v4.8" framework. 
This is a higher version than the currently targeted framework ".NETFramework,Version=v4.7.2".
```

**Root Cause:**
- NameParser project targets **.NET Framework 4.8**
- NameParser.UI project targets **.NET Framework 4.7.2**

They must match!

---

## ✅ SOLUTION: Upgrade NameParser.UI to .NET Framework 4.8

### Method 1: Visual Studio UI (EASIEST)

1. **In Solution Explorer**, right-click **NameParser.UI** project
2. Select **Properties**
3. In the **Application** tab:
   - Find **Target framework** dropdown
   - Change from **.NET Framework 4.7.2** to **.NET Framework 4.8**
4. Click **Yes** when prompted to reload the project
5. **Save all** (Ctrl+Shift+S)
6. **Build → Rebuild Solution**

---

### Method 2: Manual Edit

1. **Close Visual Studio**
2. Open **`NameParser.UI\NameParser.UI.csproj`** in Notepad
3. Find line 11:
   ```xml
   <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
   ```
4. Change it to:
   ```xml
   <TargetFrameworkVersion>v4.8</TargetFrameworkVersion>
   ```
5. **Save** the file
6. **Reopen Visual Studio**
7. **Build → Rebuild Solution**

---

### Method 3: PowerShell Script

1. **Close Visual Studio**
2. Save this script as **`fix-framework-version.ps1`** in the NameParser.UI folder
3. Run it with PowerShell

```powershell
# fix-framework-version.ps1
$projectFile = "NameParser.UI.csproj"

if (Test-Path $projectFile) {
    $content = Get-Content $projectFile -Raw
    $content = $content -replace '<TargetFrameworkVersion>v4\.7\.2</TargetFrameworkVersion>', '<TargetFrameworkVersion>v4.8</TargetFrameworkVersion>'
    $content | Set-Content $projectFile -NoNewline
    Write-Host "✓ Updated NameParser.UI to target .NET Framework 4.8" -ForegroundColor Green
} else {
    Write-Host "Error: NameParser.UI.csproj not found!" -ForegroundColor Red
}
```

4. **Reopen Visual Studio**
5. **Build → Rebuild Solution**

---

## After Fixing

Once both projects target **.NET Framework 4.8**, you'll also need to:

1. **Add Project Reference**: 
   - Right-click **NameParser.UI** → **Add** → **Reference**
   - Check **Projects** → **NameParser**
   - Click OK

2. **Install Entity Framework** (if not done already):
   ```powershell
   Install-Package EntityFramework -Version 6.4.4 -ProjectName NameParser
   Install-Package EntityFramework -Version 6.4.4 -ProjectName NameParser.UI
   ```

3. **Add System references to NameParser**:
   - Right-click **NameParser** → **Add** → **Reference**
   - Check **System** and **System.ComponentModel.DataAnnotations**

4. **Rebuild Solution**

---

## Quick Checklist

- [ ] Close Visual Studio
- [ ] Change NameParser.UI target framework to 4.8
- [ ] Reopen Visual Studio
- [ ] Add NameParser project reference to NameParser.UI
- [ ] Install EntityFramework in both projects
- [ ] Add System references to NameParser
- [ ] Rebuild Solution
- [ ] Success! ✅

---

**Do Method 1 first - it's the easiest and most reliable!**
