# SQL Server LocalDB - Connection Issues & Solutions

## ✅ Issue Resolved!

Your LocalDB instance is now **running** and ready to use.

---

## 🔍 What Was The Problem?

The LocalDB automatic instance "MSSQLLocalDB" was not created on your system. This is common when:
- LocalDB was just installed
- Windows was reinstalled
- LocalDB instances were manually deleted
- First time using LocalDB after a .NET/SQL Server update

---

## ✅ Solution Applied

```bash
# 1. Created the LocalDB instance
SqlLocalDB.exe create MSSQLLocalDB

# 2. Started the instance
SqlLocalDB.exe start MSSQLLocalDB

# Result: LocalDB is now running!
```

**Current Status:**
- **Name**: MSSQLLocalDB
- **Version**: 17.0.1000.7
- **State**: ✅ Running
- **Owner**: GAMING1\helori.lamberty
- **Pipe**: np:\\.\pipe\LOCALDB#2EB2689B\tsql\query

---

## 🎯 Verify Your Applications Work

### Web Application (NameParser.Web)

```bash
cd NameParser.Web
dotnet run
```

The application will:
1. Connect to LocalDB
2. Run `context.Database.EnsureCreated()` in `Program.cs`
3. Create the database and tables automatically

### WPF Application (NameParser.UI)

Run the WPF application. It will:
1. Connect to LocalDB
2. Use Entity Framework to create the database
3. Start working with races and classifications

---

## 🔧 Common LocalDB Commands

### Check if LocalDB is installed
```bash
SqlLocalDB.exe info
```

### Check specific instance status
```bash
SqlLocalDB.exe info MSSQLLocalDB
```

### Start LocalDB instance
```bash
SqlLocalDB.exe start MSSQLLocalDB
```

### Stop LocalDB instance
```bash
SqlLocalDB.exe stop MSSQLLocalDB
```

### Delete and recreate instance
```bash
SqlLocalDB.exe delete MSSQLLocalDB
SqlLocalDB.exe create MSSQLLocalDB
SqlLocalDB.exe start MSSQLLocalDB
```

### List all databases
```bash
# Using sqlcmd
sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "SELECT name FROM sys.databases"
```

---

## 🗄️ Your Connection Strings

### Web Application
**File**: `NameParser.Web\appsettings.json`
```json
"ConnectionStrings": {
  "RaceManagementDb": "Data Source=(LocalDB)\\MSSQLLocalDB;Initial Catalog=RaceManagementDb;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False"
}
```

**Database Name**: `RaceManagementDb`

### WPF Application
**File**: `NameParser.UI\App.config`
```xml
<connectionStrings>
  <add name="RaceManagementDb" 
       connectionString="Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=RaceManagement;Integrated Security=True;Connect Timeout=30" 
       providerName="System.Data.SqlClient" />
</connectionStrings>
```

**Database Name**: `RaceManagement`

**Note**: Your Web and WPF apps use **different databases** (RaceManagementDb vs RaceManagement). This is fine if intentional, but you may want to use the same database.

---

## 🐛 Troubleshooting Guide

### Issue 1: "Cannot connect to (LocalDB)\MSSQLLocalDB"

**Symptoms:**
- SqlException: A network-related or instance-specific error
- Error: Cannot open database
- Timeout errors

**Solutions:**

**1. Check if LocalDB is running:**
```bash
SqlLocalDB.exe info MSSQLLocalDB
```

If it shows "State: Stopped":
```bash
SqlLocalDB.exe start MSSQLLocalDB
```

**2. Recreate the instance:**
```bash
SqlLocalDB.exe stop MSSQLLocalDB
SqlLocalDB.exe delete MSSQLLocalDB
SqlLocalDB.exe create MSSQLLocalDB
SqlLocalDB.exe start MSSQLLocalDB
```

**3. Check connection string:**
- Ensure `(LocalDB)\\MSSQLLocalDB` has double backslashes in JSON
- Ensure `(LocalDB)\MSSQLLocalDB` has single backslash in XML

---

### Issue 2: "Database does not exist"

**Symptoms:**
- Cannot open database "RaceManagementDb"
- Database creation fails

**Solutions:**

**For Web App (automatic):**
The `Program.cs` has this code that creates the database:
```csharp
context.Database.EnsureCreated();
```
Just run the application!

**For WPF App (Entity Framework):**
Entity Framework will create the database on first use.

**Manual creation (if needed):**
```bash
# Connect to LocalDB
sqlcmd -S "(localdb)\MSSQLLocalDB"

# Create database
CREATE DATABASE RaceManagementDb;
GO

CREATE DATABASE RaceManagement;
GO
```

---

### Issue 3: LocalDB not installed

**Symptoms:**
- 'SqlLocalDB.exe' is not recognized
- LocalDB is not installed

**Solution:**

Install SQL Server Express LocalDB:
1. Download from: https://www.microsoft.com/en-us/sql-server/sql-server-downloads
2. Choose "Express" edition
3. Select "LocalDB" installation
4. Install and restart

Or install via Visual Studio Installer:
1. Open Visual Studio Installer
2. Modify your installation
3. Check "Data storage and processing" workload
4. Check "SQL Server Express LocalDB"
5. Install

---

### Issue 4: Permission denied

**Symptoms:**
- Access denied
- User does not have permission

**Solutions:**

**1. Run as administrator (temporary):**
- Right-click your application
- "Run as administrator"

**2. Fix LocalDB permissions:**
```bash
# Stop and delete instance
SqlLocalDB.exe stop MSSQLLocalDB
SqlLocalDB.exe delete MSSQLLocalDB

# Recreate with current user
SqlLocalDB.exe create MSSQLLocalDB
SqlLocalDB.exe start MSSQLLocalDB
```

---

### Issue 5: Connection timeout

**Symptoms:**
- Timeout expired
- Connection timeout after 30 seconds

**Solutions:**

**1. Increase timeout:**
```
# In connection string:
Connect Timeout=60
```

**2. Check if LocalDB is responsive:**
```bash
SqlLocalDB.exe info MSSQLLocalDB
```

**3. Restart LocalDB:**
```bash
SqlLocalDB.exe stop MSSQLLocalDB
SqlLocalDB.exe start MSSQLLocalDB
```

---

### Issue 6: "Named Pipes Provider error"

**Symptoms:**
- Named Pipes Provider, error: 40
- Could not open a connection to SQL Server

**Solution:**

Check the instance pipe name:
```bash
SqlLocalDB.exe info MSSQLLocalDB
```

Current pipe: `np:\\.\pipe\LOCALDB#2EB2689B\tsql\query`

The pipe name changes each time LocalDB restarts, but the connection string `(localdb)\MSSQLLocalDB` automatically resolves it.

If still failing, recreate the instance (see Issue 1, Solution 2).

---

## 🔄 Synchronize Databases (Optional)

If you want both Web and WPF to use the **same database**:

### Option 1: Make WPF use Web's database

Edit `NameParser.UI\App.config`:
```xml
<add name="RaceManagementDb" 
     connectionString="Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=RaceManagementDb;Integrated Security=True;Connect Timeout=30" 
     providerName="System.Data.SqlClient" />
```
Change `Initial Catalog=RaceManagement` to `Initial Catalog=RaceManagementDb`

### Option 2: Make Web use WPF's database

Edit `NameParser.Web\appsettings.json`:
```json
"ConnectionStrings": {
  "RaceManagementDb": "Data Source=(LocalDB)\\MSSQLLocalDB;Initial Catalog=RaceManagement;..."
}
```
Change `Initial Catalog=RaceManagementDb` to `Initial Catalog=RaceManagement`

**Recommendation**: Use **RaceManagementDb** for both (Web app name is more descriptive).

---

## 📊 Database Management

### View your databases
```bash
sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "SELECT name, database_id, create_date FROM sys.databases"
```

### Backup database
```bash
sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "BACKUP DATABASE RaceManagementDb TO DISK='C:\Backups\RaceManagementDb.bak'"
```

### Restore database
```bash
sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "RESTORE DATABASE RaceManagementDb FROM DISK='C:\Backups\RaceManagementDb.bak' WITH REPLACE"
```

### Delete database
```bash
sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "DROP DATABASE RaceManagementDb"
```

---

## 🎓 Understanding LocalDB

**What is LocalDB?**
- Lightweight version of SQL Server Express
- Runs on-demand (not as a Windows service)
- Perfect for development
- Full SQL Server T-SQL compatibility
- Minimal management required

**LocalDB vs SQL Server Express:**
- LocalDB: Starts when needed, stops when idle
- SQL Server: Always running as a service
- LocalDB: Easier for development
- SQL Server Express: Better for shared environments

**LocalDB versions:**
- LocalDB 2022: Version 16.x
- LocalDB 2019: Version 15.x
- LocalDB 2017: Version 14.x (your current: v17.0)

---

## ✅ Quick Health Check

Run this to verify everything is working:

```bash
# 1. Check LocalDB status
SqlLocalDB.exe info MSSQLLocalDB

# Expected: State: Running

# 2. Test connection
sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "SELECT @@VERSION"

# Should return SQL Server version

# 3. List databases
sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "SELECT name FROM sys.databases"

# Should show your databases
```

---

## 🚀 You're Ready!

Your LocalDB is now:
- ✅ Created
- ✅ Started
- ✅ Running
- ✅ Ready for your applications

**Next steps:**
1. Run your Web application: `dotnet run` in NameParser.Web
2. Run your WPF application
3. Both will create their databases automatically on first run

**Database locations:**
- Web: `RaceManagementDb` database
- WPF: `RaceManagement` database
- Physical files: `C:\Users\helori.lamberty\` (LocalDB default location)

---

## 📞 Still Having Issues?

If problems persist:
1. Check Windows Event Viewer for SQL Server errors
2. Verify .NET 8 SDK is installed: `dotnet --version`
3. Ensure Entity Framework tools are installed
4. Check firewall/antivirus isn't blocking LocalDB
5. Try connecting with SQL Server Management Studio (SSMS):
   - Server: `(localdb)\MSSQLLocalDB`
   - Authentication: Windows Authentication

---

**LocalDB Status**: ✅ **Running and Ready!**

**Happy Coding! 🎉**
