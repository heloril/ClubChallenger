# Facebook Integration for WPF Application

This guide covers configuring and using Facebook sharing in the ClubChallenger WPF desktop application.

## Overview

The WPF application now includes:
- ✅ **Share race results to Facebook** from the Race Classification tab
- ✅ **Share challenge standings to Facebook** from the Challenger Classification tab
- ✅ **Automatic summary generation** with top finishers
- ✅ **Confirmation dialogs** before posting
- ✅ **Status messages** for success/error feedback

---

## Configuration

### Step 1: Get Facebook Credentials

Follow the main **FACEBOOK_SETUP_GUIDE.md** to:
1. Create your Facebook App
2. Get App ID, App Secret, Page ID, and Page Access Token

### Step 2: Configure App.config

Open `NameParser.UI\App.config` and update the Facebook settings:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <!-- Other sections ... -->
  
  <appSettings>
    <add key="Facebook:AppId" value="YOUR_FACEBOOK_APP_ID" />
    <add key="Facebook:AppSecret" value="YOUR_FACEBOOK_APP_SECRET" />
    <add key="Facebook:PageId" value="YOUR_FACEBOOK_PAGE_ID" />
    <add key="Facebook:PageAccessToken" value="YOUR_PAGE_ACCESS_TOKEN" />
  </appSettings>
  
  <!-- Other sections ... -->
</configuration>
```

**Important Security Notes:**
- ⚠️ **DO NOT commit App.config with real credentials to source control**
- Use a local App.config with actual values
- Add App.config to .gitignore or use App.config transformation
- Consider using encrypted configuration sections for production

---

## Usage

### Share Race Results

1. **Navigate to Race Classification Tab**
2. **Select a race** from the list
3. **Click "📱 Share to Facebook"** button
4. **Confirm** the sharing dialog
5. **Check status message** for success or error

**What gets posted:**
```
🏃 Summer 10K - 2024

Results for Summer 10K (10 km)

🏆 Top 3 Finishers:
🥇 1. John Doe - 00:35:24 (Running Club)
🥈 2. Jane Smith - 00:36:12 (City Athletics)
🥉 3. Mike Johnson - 00:37:45

👥 Total Participants: 156
```

### Share Challenge Standings

1. **Navigate to Challenger Classification Tab**
2. **Select a year** from the dropdown
3. **Click "Load Challengers"** to load standings
4. **Click "📱 Share to Facebook"** button
5. **Confirm** the sharing dialog
6. **Check status message** for success or error

**What gets posted:**
```
🏃 Challenge 2024 Standings

Challenge 2024 Standings

🏆 Top Challengers:
🥇 #1 John Doe - 950 pts (10 races)
🥈 #2 Jane Smith - 920 pts (10 races)
🥉 #3 Mike Johnson - 890 pts (9 races)
🔹 #4 Sarah Williams - 850 pts (9 races)
🔹 #5 Tom Brown - 820 pts (8 races)

👥 Total Challengers: 45
```

---

## UI Elements

### Race Classification Tab Buttons

```
[Refresh] [View] [Reprocess] [Download] [Export] [Export Multiple] [📱 Share to Facebook] [Delete]
```

- **📱 Share to Facebook** - Blue button with Facebook brand color (#1877F2)
- Enabled only when a race is selected
- Shows confirmation dialog before posting

### Challenger Classification Tab Buttons

```
Year: [2024 ▼] [Load Challengers] [📱 Share to Facebook] [Export]
```

- **📱 Share to Facebook** - Blue button with Facebook brand color
- Enabled only when challenge standings are loaded
- Shows confirmation dialog before posting

---

## Features

### Confirmation Dialogs

Before posting, the application shows a confirmation:

**Race Results:**
```
Share 'Summer 10K' results to Facebook?

This will post the race results with top 3 finishers 
to your Facebook page.

[Yes] [No]
```

**Challenge Standings:**
```
Share Challenge 2024 standings to Facebook?

This will post the top challengers to your Facebook page.

[Yes] [No]
```

### Success Messages

**On Success:**
```
✅ Successfully shared to Facebook! Post ID: 123456789_987654321

[Dialog]: Race results shared successfully to Facebook!
Post ID: 123456789_987654321
```

**On Error:**
```
❌ Failed to share to Facebook: Invalid OAuth access token

[Dialog]: Failed to share to Facebook:
Invalid OAuth access token

Please check your Facebook configuration in App.config.
```

### Processing Indicator

While sharing:
- Status message shows: "Sharing to Facebook..."
- IsProcessing flag prevents multiple simultaneous posts
- UI remains responsive

---

## Technical Implementation

### Architecture

```
MainViewModel
    │
    ├─ FacebookService (injected)
    │   ├─ PostRaceResultsAsync()
    │   └─ PostChallengeResultsAsync()
    │
    ├─ ShareRaceToFacebookCommand
    │   ├─ CanExecute: SelectedRace != null
    │   └─ Execute: Build summary & post
    │
    └─ ShareChallengeToFacebookCommand
        ├─ CanExecute: ChallengerClassifications.Count > 0
        └─ Execute: Build summary & post
```

### Configuration Loading

```csharp
// In MainViewModel constructor:
var fbSettings = new FacebookSettings
{
    AppId = ConfigurationManager.AppSettings["Facebook:AppId"] ?? "",
    AppSecret = ConfigurationManager.AppSettings["Facebook:AppSecret"] ?? "",
    PageId = ConfigurationManager.AppSettings["Facebook:PageId"] ?? "",
    PageAccessToken = ConfigurationManager.AppSettings["Facebook:PageAccessToken"] ?? ""
};
_facebookService = new FacebookService(fbSettings);
```

### Async Operations

All Facebook operations are async to keep the UI responsive:

```csharp
private async void ExecuteShareRaceToFacebook(object parameter)
{
    // Show confirmation
    // Set IsProcessing = true
    // Call await _facebookService.PostRaceResultsAsync()
    // Show success/error message
    // Set IsProcessing = false
}
```

---

## Troubleshooting

### Issue: Buttons Are Disabled

**Race Share Button:**
- **Cause**: No race is selected
- **Solution**: Click on a race in the DataGrid

**Challenge Share Button:**
- **Cause**: Challenge standings not loaded
- **Solution**: Click "Load Challengers" first

### Issue: "Configuration Missing" Error

**Symptoms:**
```
Failed to share to Facebook:
Facebook Page Access Token is not configured.
Please update App.config.
```

**Solution:**
1. Open `NameParser.UI\App.config`
2. Verify all Facebook settings are present
3. Replace placeholder values with real credentials
4. Restart the application

### Issue: "Invalid OAuth Access Token"

**Symptoms:**
```
Failed to share to Facebook:
Invalid OAuth access token
```

**Solution:**
1. Your Page Access Token has expired or is invalid
2. Follow FACEBOOK_SETUP_GUIDE.md Step 6 to generate new token
3. Update App.config with new token
4. Restart the application

### Issue: "Permissions Error"

**Symptoms:**
```
Failed to share to Facebook:
(#200) Provide valid app ID
```

**Solution:**
1. Verify App ID and App Secret are correct
2. Check that your app has `pages_manage_posts` permission
3. Add yourself as app admin/developer if in development mode

### Issue: Application Freezes

**Symptoms:**
- UI becomes unresponsive during share
- Application appears to hang

**Cause:**
- Network timeout or slow connection

**Solution:**
- Wait for operation to complete (timeout is 30 seconds)
- Check internet connection
- Verify Facebook API is accessible

---

## Security Best Practices

### 1. Protect App.config

```xml
<!-- DO NOT COMMIT THIS TO GIT -->
<appSettings>
  <add key="Facebook:PageAccessToken" value="REAL_TOKEN_HERE" />
</appSettings>
```

### 2. Use Configuration Transforms

For different environments:

**App.Development.config:**
```xml
<appSettings>
  <add key="Facebook:PageAccessToken" value="DEV_TOKEN" />
</appSettings>
```

**App.Production.config:**
```xml
<appSettings>
  <add key="Facebook:PageAccessToken" value="PROD_TOKEN" />
</appSettings>
```

### 3. Encrypt Sensitive Sections

```csharp
// Encrypt appSettings section
var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
var section = config.GetSection("appSettings");
if (!section.SectionInformation.IsProtected)
{
    section.SectionInformation.ProtectSection("DataProtectionConfigurationProvider");
    config.Save();
}
```

### 4. Use Windows Credential Store (Advanced)

```csharp
// Store token in Windows Credential Manager
using (var cred = new Credential())
{
    cred.Target = "ClubChallenger_Facebook";
    cred.Username = "PageAccessToken";
    cred.Password = "YOUR_TOKEN_HERE";
    cred.Type = CredentialType.Generic;
    cred.PersistanceType = PersistanceType.LocalComputer;
    cred.Save();
}
```

---

## Comparison: Web vs WPF

| Feature | Web App | WPF App |
|---------|---------|---------|
| **Configuration** | User Secrets / Environment Variables | App.config |
| **Share Location** | Races page table row | Tab action buttons |
| **UI Feedback** | Page alert banner | MessageBox dialog |
| **Processing** | HTTP POST handler | Async command |
| **Status** | TempData message | Status message property |
| **Confirmation** | JavaScript confirm | WPF MessageBox |

---

## Known Limitations

1. **No Image Support Yet**
   - Currently only text posts
   - Future: Generate race result graphics

2. **No Link Back to Results**
   - WPF is desktop app (no public URL)
   - Web app can include links
   - Future: Host results on web and include link

3. **Single Page Only**
   - Can only post to one Facebook page
   - Configured in App.config
   - Future: Support multiple pages

4. **Manual Configuration**
   - Must edit App.config manually
   - No UI for configuration
   - Future: Settings window in app

---

## Future Enhancements

### Planned Features

1. **Settings Window**
   - Configure Facebook credentials in UI
   - Test connection button
   - Save to App.config

2. **Image Generation**
   - Create race result graphics
   - Post as Facebook photo
   - Include club logo and branding

3. **Scheduled Posting**
   - Queue posts for later
   - Auto-post at specific times
   - Batch multiple races

4. **Post Templates**
   - Custom message templates
   - Variable substitution
   - Preview before posting

5. **Post History**
   - Track all shared posts
   - View post analytics
   - Re-share previous results

---

## Quick Command Reference

### Test Connection

Add temporary test button in UI:

```csharp
// In MainViewModel
public ICommand TestFacebookConnectionCommand { get; }

private async void ExecuteTestFacebookConnection(object parameter)
{
    var isConnected = await _facebookService.TestConnectionAsync();
    MessageBox.Show(
        isConnected ? "✅ Connected to Facebook!" : "❌ Connection failed",
        "Facebook Test",
        MessageBoxButton.OK,
        isConnected ? MessageBoxImage.Information : MessageBoxImage.Error);
}
```

### Debug Configuration

```csharp
// Check if configuration is loaded
var appId = ConfigurationManager.AppSettings["Facebook:AppId"];
var hasToken = !string.IsNullOrEmpty(
    ConfigurationManager.AppSettings["Facebook:PageAccessToken"]);

MessageBox.Show($"AppId: {appId}\nHas Token: {hasToken}");
```

---

## Support Resources

- **Main Setup Guide**: `FACEBOOK_SETUP_GUIDE.md`
- **Troubleshooting**: `FACEBOOK_TROUBLESHOOTING.md`
- **Architecture**: `FACEBOOK_ARCHITECTURE.md`
- **Web Integration**: `FACEBOOK_QUICK_START.md`

---

## Summary

✅ **Share race results** - One click from Race Classification tab  
✅ **Share challenge standings** - One click from Challenger Classification tab  
✅ **Easy configuration** - Update App.config with credentials  
✅ **User-friendly** - Confirmation dialogs and clear feedback  
✅ **Secure** - Keep credentials out of source control  

**Happy Sharing from your Desktop App! 🖥️📱**
