# Facebook Integration - Quick Reference Card

## 🚀 Setup (One Time)

### 1. Get Facebook Credentials
```
1. Go to https://developers.facebook.com/
2. Create App → Get App ID & App Secret
3. Get your Facebook Page ID
4. Generate Page Access Token
   (See FACEBOOK_SETUP_GUIDE.md for details)
```

### 2. Configure Your Applications

#### Web Application
```bash
cd NameParser.Web
dotnet user-secrets set "Facebook:AppId" "YOUR_APP_ID"
dotnet user-secrets set "Facebook:AppSecret" "YOUR_APP_SECRET"
dotnet user-secrets set "Facebook:PageId" "YOUR_PAGE_ID"
dotnet user-secrets set "Facebook:PageAccessToken" "YOUR_TOKEN"
```

#### WPF Application
Edit `NameParser.UI\App.config`:
```xml
<appSettings>
  <add key="Facebook:AppId" value="YOUR_APP_ID" />
  <add key="Facebook:AppSecret" value="YOUR_APP_SECRET" />
  <add key="Facebook:PageId" value="YOUR_PAGE_ID" />
  <add key="Facebook:PageAccessToken" value="YOUR_TOKEN" />
</appSettings>
```

---

## 📱 Daily Usage

### Web Application

**Share Race Results:**
```
1. Navigate to /Races page
2. Find race in table
3. Click blue "Share" button (Facebook icon)
4. Confirm
5. ✅ Check your Facebook page!
```

**What Gets Posted:**
- 🏃 Race name and year
- 🏆 Top 3 finishers with times
- 👥 Total participants
- 🔗 Link back to full results

---

### WPF Application

**Share Race Results:**
```
1. Open Race Classification tab
2. Select a race from list
3. Click "📱 Share to Facebook" button
4. Confirm dialog
5. ✅ Check your Facebook page!
```

**Share Challenge Standings:**
```
1. Open Challenger Classification tab
2. Select year and click "Load Challengers"
3. Click "📱 Share to Facebook" button
4. Confirm dialog
5. ✅ Check your Facebook page!
```

**What Gets Posted:**
- 🏃 Challenge title or race name
- 🏆 Top finishers (3 for races, 5 for challenge)
- 👥 Total participants/challengers

---

## 🔧 Quick Troubleshooting

| Problem | Quick Fix |
|---------|-----------|
| **Button disabled** | Select a race / Load data first |
| **"Invalid token"** | Regenerate Page Access Token |
| **"Configuration missing"** | Check credentials in config files |
| **"Permissions error"** | Add yourself as app admin/developer |
| **Post not appearing** | Verify Page ID matches token |

**Detailed help:** See `FACEBOOK_TROUBLESHOOTING.md`

---

## 📊 Sample Post

```
🏃 Summer 10K Championship - 2024

Results for Summer 10K Championship (10 km)

🏆 Top 3 Finishers:
🥇 1. John Doe - 00:35:24 (Running Club)
🥈 2. Jane Smith - 00:36:12 (City Athletics)
🥉 3. Mike Johnson - 00:37:45

👥 Total Participants: 156

🔗 View full results: https://yoursite.com/Races?raceId=42
(Link only in Web app posts)
```

---

## 🔐 Security Reminders

### Web App
- ✅ Use User Secrets (development)
- ✅ Use Environment Variables (production)
- ❌ Never commit secrets to Git

### WPF App
- ✅ Keep App.config out of source control
- ✅ Consider encrypting appSettings section
- ❌ Never commit real credentials

---

## 📚 Documentation

| Need Help With... | Read This |
|-------------------|-----------|
| **First time setup** | `FACEBOOK_SETUP_GUIDE.md` |
| **WPF configuration** | `FACEBOOK_WPF_GUIDE.md` |
| **Web configuration** | `FACEBOOK_QUICK_START.md` |
| **Tracking progress** | `FACEBOOK_CHECKLIST.md` |
| **Troubleshooting** | `FACEBOOK_TROUBLESHOOTING.md` |
| **Architecture details** | `FACEBOOK_ARCHITECTURE.md` |

---

## ⚡ Command Cheat Sheet

### Web App Commands
```bash
# Set user secrets
dotnet user-secrets set "Facebook:PageAccessToken" "YOUR_TOKEN"

# List secrets
dotnet user-secrets list

# Run application
dotnet run

# Build
dotnet build
```

### Test Connection
```bash
# Use Graph API Explorer
https://developers.facebook.com/tools/explorer/

# Test your token
https://developers.facebook.com/tools/debug/accesstoken/
```

---

## 🎯 Quick Access URLs

| Resource | URL |
|----------|-----|
| **Facebook Developers** | https://developers.facebook.com/ |
| **App Dashboard** | https://developers.facebook.com/apps/ |
| **Graph API Explorer** | https://developers.facebook.com/tools/explorer/ |
| **Token Debugger** | https://developers.facebook.com/tools/debug/accesstoken/ |
| **Your Races (Web)** | https://localhost:7001/Races |

---

## 🔄 Update Token (When Expired)

### Generate New Page Access Token
```
1. Go to Graph API Explorer
2. Select your app
3. Click "Generate Access Token"
4. Select permissions: pages_manage_posts
5. Get me/accounts
6. Copy new page token
7. Update configuration (Web or WPF)
8. Restart application
```

### Long-Lived Token (Never Expires)
```
https://graph.facebook.com/v18.0/oauth/access_token
  ?grant_type=fb_exchange_token
  &client_id=YOUR_APP_ID
  &client_secret=YOUR_APP_SECRET
  &fb_exchange_token=SHORT_LIVED_TOKEN

Then: GET me/accounts with long-lived token
Result: Page token that never expires!
```

---

## ✅ Pre-Post Checklist

Before sharing:
- [ ] Facebook App created and configured
- [ ] Page Access Token is valid and never-expiring
- [ ] Configuration updated in both apps (if using both)
- [ ] Tested with one race successfully
- [ ] Participant consent obtained (if required)
- [ ] Facebook page is correct

---

## 🎨 Customization

### Change Post Format
**Web:** Edit `BuildRaceSummary()` in `Races.cshtml.cs`  
**WPF:** Edit `BuildRaceSummary()` in `MainViewModel.cs`

### Add Images (Future)
```csharp
byte[] imageData = GenerateRaceResultsImage(race);
await _facebookService.PostRaceResultsAsync(
    raceName, summary, imageData);
```

---

## 📞 Support

**Issues?** Check:
1. Token is valid (Token Debugger)
2. Permissions are correct (App Dashboard)
3. Configuration is loaded (check logs)
4. Facebook API is accessible (network)

**Still stuck?**
- Review `FACEBOOK_TROUBLESHOOTING.md`
- Check Facebook Developer Docs
- Test in Graph API Explorer

---

## 🎉 Quick Win!

**Most Common Issue: Invalid Token**
```bash
# Solution (takes 2 minutes):
1. Go to Graph API Explorer
2. Generate new token
3. Get me/accounts
4. Copy page token
5. Update config
6. Restart app
7. Share! ✅
```

---

**Print this card and keep it handy! 📋**
