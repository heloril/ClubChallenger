# Gmail Configuration for WPF and Console Apps

Both the WPF application (`NameParser.UI`) and the Email Tester console app (`NameParser.EmailTester`) now use the same configuration file format: `appsettings.json`.

## Configuration File Location

Each application needs its own `appsettings.json` file in the application directory:

- **WPF App**: `NameParser.UI\appsettings.json` (or in bin\Debug\net8.0-windows after build)
- **Console App**: `NameParser.EmailTester\appsettings.json` (or in bin\Debug\net8.0 after build)

## Setup Instructions

### Option 1: Manual Configuration (Recommended)

1. **Copy the example file:**
   ```bash
   # For WPF App
   copy NameParser.UI\appsettings.example.json NameParser.UI\appsettings.json
   
   # For Console App
   copy NameParser.EmailTester\appsettings.example.json NameParser.EmailTester\appsettings.json
   ```

2. **Edit `appsettings.json` with your credentials:**
   ```json
   {
     "Gmail": {
       "Address": "seraing.athl@gmail.com",
       "AppPassword": "your-app-password-here",
       "SmtpServer": "smtp.gmail.com",
       "SmtpPort": 587
     }
   }
   ```

3. **Get your Gmail App Password:**
   - Go to: https://myaccount.google.com/apppasswords
   - Create a new app password for "Mail"
   - Copy the 16-character password (remove spaces)
   - Paste it into the `AppPassword` field

### Option 2: Using the WPF UI

1. **Launch the WPF application** (`NameParser.UI`)
2. **Navigate to the "Challenge Mailing" tab**
3. **Enter your Gmail settings:**
   - Gmail Address
   - App Password
   - SMTP Server (default: smtp.gmail.com)
   - SMTP Port (default: 587)
4. **Click "Save Settings"** - This creates/updates `appsettings.json`

### Option 3: Using the Console App

1. **Run the Email Tester:** `dotnet run --project NameParser.EmailTester`
2. **Select option 1:** "Configure Gmail Settings"
3. **Enter your credentials** (password input is masked)
4. **Settings are saved** to `appsettings.json`

## Configuration Format

```json
{
  "Gmail": {
    "Address": "your-email@gmail.com",
    "AppPassword": "xxxx xxxx xxxx xxxx",
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587
  }
}
```

### Configuration Properties

| Property | Description | Default |
|----------|-------------|---------|
| `Address` | Your Gmail email address | - |
| `AppPassword` | Gmail App Password (16 characters) | - |
| `SmtpServer` | Gmail SMTP server | smtp.gmail.com |
| `SmtpPort` | SMTP port for TLS | 587 |

## Security Notes

### ⚠️ IMPORTANT: Protect Your Credentials

1. **Never commit `appsettings.json` to Git** with real credentials
   - The file is now in `.gitignore`
   - Use `appsettings.example.json` for templates

2. **Keep App Passwords secure**
   - Don't share them
   - Regenerate if compromised
   - Use different passwords for different applications

3. **File permissions**
   - Ensure `appsettings.json` has restricted permissions
   - On Windows, only your user should have access

## Sharing Settings Between Apps

Both applications can share the same settings by:

1. **Configure one app** (either WPF or Console)
2. **Copy the `appsettings.json` file** to the other app's directory
3. **Or use a shared location** (advanced - requires code changes)

## Troubleshooting

### "Gmail settings not configured"

**Problem:** Application can't find or read `appsettings.json`

**Solution:**
- Verify the file exists in the application directory
- Check the file is named exactly `appsettings.json`
- Ensure JSON format is valid
- Check file permissions

### Settings Not Loading in WPF App

**Problem:** Settings don't load when WPF app starts

**Solution:**
- Rebuild the project (settings are copied to bin directory)
- Check `appsettings.json` exists in `bin\Debug\net8.0-windows`
- Verify the `.csproj` includes the copy rule

### Console App Can't Find Settings

**Problem:** Console app shows empty settings

**Solution:**
- Run from the correct directory
- Or provide full path in configuration builder
- Check `appsettings.json` is in `bin\Debug\net8.0`

## Example: Complete Setup

```bash
# 1. Create configuration files from examples
cd NameParser.UI
copy appsettings.example.json appsettings.json

cd ..\NameParser.EmailTester
copy appsettings.example.json appsettings.json

# 2. Edit both files with your credentials (use notepad or VS Code)
notepad appsettings.json

# 3. Build the solution
cd ..
dotnet build

# 4. Test with console app
cd NameParser.EmailTester
dotnet run

# 5. Or run the WPF app
cd ..\NameParser.UI
dotnet run
```

## Verifying Configuration

### In WPF App:
1. Open the application
2. Go to "Challenge Mailing" tab
3. Check the status message at the bottom
4. Should show: "Gmail settings loaded from appsettings.json (your-email@gmail.com)"

### In Console App:
1. Run the app
2. Select option `5` (View Current Settings)
3. Verify your email address is shown
4. Password should be masked with asterisks

## Migration from Old Configuration

If you were using the old `App.config` approach:

1. **Export settings from WPF UI:**
   - Open the app
   - Enter your settings in the UI
   - Click "Save Settings"
   - This creates the new `appsettings.json`

2. **Manually migrate:**
   - Copy values from old `App.config`
   - Create new `appsettings.json` with the format above

## Advanced: Environment-Specific Settings

You can create environment-specific files:

- `appsettings.json` - Default settings
- `appsettings.Development.json` - Development overrides
- `appsettings.Production.json` - Production settings

Modify the configuration builder in code to load them:

```csharp
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true)
    .Build();
```

## Support

If you encounter issues:

1. Check this documentation
2. Verify JSON syntax at https://jsonlint.com
3. Review the `.gitignore` to ensure files aren't ignored incorrectly
4. Check file exists in the build output directory
5. Look at console output for configuration errors
