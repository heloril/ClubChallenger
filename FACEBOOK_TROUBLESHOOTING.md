# Facebook Integration - Troubleshooting Guide

Quick solutions for common problems.

---

## 🔴 Error: "Invalid OAuth access token"

### Symptoms
- Error message when clicking Share
- Facebook API returns 401 Unauthorized
- Message: "Invalid OAuth access token"

### Causes
1. Page Access Token has expired
2. Using wrong type of token (user token instead of page token)
3. Token was revoked or invalidated
4. Token doesn't belong to the configured Page ID

### Solutions

**Solution 1: Generate New Page Access Token**
```bash
# 1. Go to Graph API Explorer
https://developers.facebook.com/tools/explorer/

# 2. Select your app
# 3. Click "Generate Access Token"
# 4. Select permissions: pages_show_list, pages_read_engagement, pages_manage_posts
# 5. Generate token
# 6. Get page token via: me/accounts
# 7. Update your configuration
dotnet user-secrets set "Facebook:PageAccessToken" "NEW_TOKEN_HERE"
```

**Solution 2: Verify Token**
```bash
# Use Access Token Debugger
https://developers.facebook.com/tools/debug/accesstoken/

# Paste your token
# Check:
# - Is it a Page token? (not User token)
# - Does it have required permissions?
# - Is it expired?
```

---

## 🔴 Error: "Permissions error" or "(#200) Provide valid app ID"

### Symptoms
- Error about missing permissions
- Can't post to page
- Facebook API returns 403 Forbidden

### Causes
1. App doesn't have required permissions
2. Using development mode without being admin/developer
3. Permissions not approved by Facebook
4. Wrong App ID or App Secret

### Solutions

**Solution 1: Check App Permissions**
```bash
# 1. Go to App Dashboard
https://developers.facebook.com/apps/

# 2. Go to App Review → Permissions and Features
# 3. Verify these have "Advanced Access":
#    - pages_show_list
#    - pages_read_engagement
#    - pages_manage_posts
```

**Solution 2: Add Test Users (Development)**
```bash
# If app is in Development mode:
# 1. Go to Roles → Roles
# 2. Add your Facebook account as Admin, Developer, or Tester
# 3. This allows testing without App Review
```

**Solution 3: Submit for App Review**
```bash
# For production use:
# 1. Go to App Review → Permissions and Features
# 2. Click "Request Advanced Access" on each permission
# 3. Provide detailed use case
# 4. Submit for review
# 5. Wait for approval (3-5 business days)
```

---

## 🔴 Error: "Page not found" or Wrong page receiving posts

### Symptoms
- Posts go to wrong Facebook page
- Error: Page doesn't exist
- Empty response from Facebook

### Causes
1. Wrong Page ID configured
2. Page Access Token belongs to different page
3. Page has been deleted or unpublished

### Solutions

**Solution 1: Verify Page ID**
```bash
# Method 1: Via Page Settings
# - Go to your Facebook Page
# - Settings → Page Info
# - Copy Page ID

# Method 2: Via Graph API
# Open in browser:
https://graph.facebook.com/v18.0/YOUR_PAGE_NAME?fields=id

# Update configuration:
dotnet user-secrets set "Facebook:PageId" "CORRECT_PAGE_ID"
```

**Solution 2: Verify Token Matches Page**
```bash
# Get page info using your token:
https://graph.facebook.com/v18.0/me?access_token=YOUR_PAGE_TOKEN

# Response should show the correct page name and ID
# If it shows your personal profile, you're using a User token, not Page token
```

---

## 🔴 Error: "Configuration missing" or "Facebook settings not configured"

### Symptoms
- Application shows configuration error
- Share button doesn't work
- Error in logs about missing settings

### Causes
1. User secrets not set
2. Environment variables not configured
3. Wrong project for user secrets
4. Configuration not loaded

### Solutions

**Solution 1: Verify User Secrets**
```bash
# Check if secrets exist:
cd NameParser.Web
dotnet user-secrets list

# Should show:
# Facebook:AppId = YOUR_APP_ID
# Facebook:AppSecret = YOUR_APP_SECRET
# Facebook:PageId = YOUR_PAGE_ID
# Facebook:PageAccessToken = YOUR_TOKEN

# If empty, set them:
dotnet user-secrets set "Facebook:AppId" "YOUR_APP_ID"
dotnet user-secrets set "Facebook:AppSecret" "YOUR_APP_SECRET"
dotnet user-secrets set "Facebook:PageId" "YOUR_PAGE_ID"
dotnet user-secrets set "Facebook:PageAccessToken" "YOUR_TOKEN"
```

**Solution 2: Verify Project Has UserSecretsId**
```bash
# Check NameParser.Web/NameParser.Web.csproj
# Should contain:
# <UserSecretsId>...</UserSecretsId>

# If missing, run:
dotnet user-secrets init
```

**Solution 3: Restart Application**
```bash
# Configuration is loaded at startup
# Restart your application after setting secrets
dotnet run
```

---

## 🔴 Button Click Does Nothing

### Symptoms
- Click Share button, nothing happens
- No error message
- No post created

### Causes
1. JavaScript error in browser
2. Form not submitting
3. Network error
4. Silent exception

### Solutions

**Solution 1: Check Browser Console**
```javascript
// Open browser Developer Tools (F12)
// Go to Console tab
// Look for errors
// Common issues:
// - "Form submission prevented"
// - "Network request failed"
// - CORS errors
```

**Solution 2: Check Network Tab**
```javascript
// In Developer Tools:
// 1. Go to Network tab
// 2. Click Share button
// 3. Look for POST request to /Races?handler=ShareToFacebook
// 4. Check response status and body
```

**Solution 3: Check Application Logs**
```bash
# Look in console output for errors
# Check for exceptions during OnPostShareToFacebookAsync
```

---

## 🔴 Post Created But Not Visible on Page

### Symptoms
- Success message shown
- Post ID returned
- But post not visible on Facebook page

### Causes
1. Posted to wrong page
2. Post filtered by Facebook
3. Page visibility settings
4. Content policy violation

### Solutions

**Solution 1: Check Page Activity Log**
```bash
# 1. Go to your Facebook Page
# 2. Click "Manage Page" (left sidebar)
# 3. Click "Activity Log"
# 4. Look for recent posts
# 5. Check if post was created but hidden
```

**Solution 2: Verify Post ID**
```bash
# Use Graph API to check post:
https://graph.facebook.com/v18.0/POST_ID?access_token=YOUR_TOKEN

# If post exists, you'll see details
# If error, post wasn't created or was deleted
```

**Solution 3: Check Page Publishing Status**
```bash
# 1. Go to Page Settings
# 2. Check "Page Visibility"
# 3. Ensure page is Published (not Unpublished)
# 4. Check if there are content restrictions
```

---

## 🔴 Rate Limit Exceeded

### Symptoms
- Error: "Application request limit reached"
- Error code: 4, 17, 32, or 613
- Posts fail after several successful ones

### Causes
1. Too many API calls in short time
2. Hitting Facebook's rate limits
3. Shared rate limit with other apps

### Solutions

**Solution 1: Wait and Retry**
```bash
# Facebook rate limits reset over time
# Wait 1 hour before retrying
# Don't spam retry attempts
```

**Solution 2: Implement Rate Limiting**
```csharp
// Add to FacebookService:
private static DateTime _lastPostTime = DateTime.MinValue;
private static readonly TimeSpan MinTimeBetweenPosts = TimeSpan.FromMinutes(5);

public async Task<FacebookPostResponse> PostRaceResultsAsync(...)
{
    var timeSinceLastPost = DateTime.UtcNow - _lastPostTime;
    if (timeSinceLastPost < MinTimeBetweenPosts)
    {
        var waitTime = MinTimeBetweenPosts - timeSinceLastPost;
        await Task.Delay(waitTime);
    }
    
    // ... rest of method
    
    _lastPostTime = DateTime.UtcNow;
}
```

---

## 🔴 Build Errors

### Error: "FacebookService not found"

**Solution:**
```bash
# Verify file exists:
ls NameParser.Web/Services/FacebookService.cs

# If missing, the file wasn't created properly
# Check FACEBOOK_IMPLEMENTATION_SUMMARY.md for file contents
```

### Error: "Type or namespace FacebookSettings not found"

**Solution:**
```csharp
// Ensure FacebookSettings is in the same file as FacebookService
// Or add using statement:
using NameParser.Web.Services;
```

### Error: "HttpClient not registered"

**Solution:**
```csharp
// In Program.cs, ensure this line exists:
builder.Services.AddHttpClient<FacebookService>();
```

---

## 🔴 Deployment Issues

### Production: Settings Not Loaded

**Solution for Azure:**
```bash
# Set Application Settings in Azure Portal:
# Configuration → Application Settings → New application setting

# Add:
# Facebook__AppId = YOUR_APP_ID
# Facebook__AppSecret = YOUR_APP_SECRET
# Facebook__PageId = YOUR_PAGE_ID
# Facebook__PageAccessToken = YOUR_TOKEN

# Note: Use double underscores __
```

**Solution for IIS:**
```xml
<!-- In web.config: -->
<configuration>
  <appSettings>
    <add key="Facebook__AppId" value="YOUR_APP_ID" />
    <add key="Facebook__AppSecret" value="YOUR_APP_SECRET" />
    <add key="Facebook__PageId" value="YOUR_PAGE_ID" />
    <add key="Facebook__PageAccessToken" value="YOUR_TOKEN" />
  </appSettings>
</configuration>
```

**Solution for Linux:**
```bash
# Set environment variables:
export Facebook__AppId="YOUR_APP_ID"
export Facebook__AppSecret="YOUR_APP_SECRET"
export Facebook__PageId="YOUR_PAGE_ID"
export Facebook__PageAccessToken="YOUR_TOKEN"

# Or add to /etc/environment
```

---

## 🔍 Diagnostic Commands

### Test Facebook Connection
```csharp
// Add this to Races.cshtml.cs for testing:
public async Task<IActionResult> OnGetTestFacebookAsync()
{
    var isConnected = await _facebookService.TestConnectionAsync();
    StatusMessage = isConnected 
        ? "✅ Facebook connection successful!" 
        : "❌ Facebook connection failed!";
    return RedirectToPage();
}
```

### Debug Token
```bash
# Check token details:
https://developers.facebook.com/tools/debug/accesstoken/

# Paste your token and verify:
# - Token Type: Should be "Page"
# - Expires: Should be "Never"
# - App ID: Should match your app
# - Scopes: Should include pages_manage_posts
```

### Check API Response
```csharp
// Add logging in FacebookService:
_logger.LogInformation($"Facebook API Response: {responseContent}");
```

---

## 📞 Still Need Help?

### 1. Enable Detailed Logging
```json
// In appsettings.json:
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "NameParser.Web.Services": "Debug"
    }
  }
}
```

### 2. Check These Files
- Application logs
- Browser console (F12)
- Network tab in Developer Tools
- Facebook Activity Log

### 3. Use These Tools
- **Graph API Explorer**: https://developers.facebook.com/tools/explorer/
- **Access Token Debugger**: https://developers.facebook.com/tools/debug/accesstoken/
- **API Status Page**: https://developers.facebook.com/status/

### 4. Review Documentation
- **Setup Guide**: FACEBOOK_SETUP_GUIDE.md
- **Architecture**: FACEBOOK_ARCHITECTURE.md
- **Facebook Docs**: https://developers.facebook.com/docs/

### 5. Contact Support
- **Facebook Developer Support**: https://developers.facebook.com/support/
- **Community Forum**: https://developers.facebook.com/community/

---

## 🎯 Quick Fixes Summary

| Problem | Quick Fix |
|---------|-----------|
| Invalid token | Regenerate Page Access Token |
| Permissions error | Check app permissions in Dashboard |
| Button not working | Check browser console (F12) |
| Configuration missing | Run `dotnet user-secrets list` |
| Wrong page | Verify Page ID matches token |
| Rate limited | Wait 1 hour, implement delays |
| Build error | Verify all files created |
| Production not working | Check environment variables |

---

**Remember**: Most issues are related to token configuration or permissions!

**Pro Tip**: Use the Graph API Explorer and Token Debugger to diagnose issues before debugging your code.
