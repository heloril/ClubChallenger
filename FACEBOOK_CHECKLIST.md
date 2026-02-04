# Facebook Integration - Complete Checklist

Use this checklist to track your setup progress.

---

## 📋 Phase 1: Facebook App Setup (30-60 minutes)

### Step 1: Create Facebook App
- [ ] Go to https://developers.facebook.com/
- [ ] Click "My Apps" → "Create App"
- [ ] Select "Business" app type
- [ ] Enter app name: "ClubChallenger" (or your choice)
- [ ] Enter contact email
- [ ] Complete CAPTCHA
- [ ] **Record App ID**: _____________________

### Step 2: Get App Secret
- [ ] Go to Settings → Basic
- [ ] Click "Show" on App Secret
- [ ] Enter Facebook password
- [ ] **Record App Secret**: _____________________

### Step 3: Add Facebook Login
- [ ] Click "Add Product" in left sidebar
- [ ] Find "Facebook Login"
- [ ] Click "Set Up"
- [ ] Go to Facebook Login → Settings
- [ ] Add OAuth Redirect URI: `https://yourdomain.com/signin-facebook`
- [ ] Add OAuth Redirect URI: `https://localhost:7001/signin-facebook`
- [ ] Click "Save Changes"

### Step 4: Request Permissions
- [ ] Go to App Review → Permissions and Features
- [ ] Click "Request Advanced Access" for `pages_show_list`
- [ ] Click "Request Advanced Access" for `pages_read_engagement`
- [ ] Click "Request Advanced Access" for `pages_manage_posts`
- [ ] Provide use case descriptions for each
- [ ] (Optional) Submit for App Review if needed for production

### Step 5: Get Page ID
Choose one method:

**Method 1: Page Settings**
- [ ] Go to your Facebook Page
- [ ] Click Settings
- [ ] Click Page Info
- [ ] **Record Page ID**: _____________________

**Method 2: Graph API**
- [ ] Go to https://developers.facebook.com/tools/explorer/
- [ ] Enter: `YourPageName?fields=id`
- [ ] Click Submit
- [ ] **Record Page ID**: _____________________

### Step 6: Generate Page Access Token

**Get User Token:**
- [ ] Go to https://developers.facebook.com/tools/explorer/
- [ ] Select your app in "Meta App" dropdown
- [ ] Click "Generate Access Token"
- [ ] Select permissions: `pages_show_list`, `pages_read_engagement`, `pages_manage_posts`
- [ ] Click "Generate Access Token"
- [ ] Authorize the permissions
- [ ] **Record Short-Lived User Token**: _____________________

**Get Long-Lived User Token:**
- [ ] Visit this URL in browser (replace values):
```
https://graph.facebook.com/v18.0/oauth/access_token?grant_type=fb_exchange_token&client_id=YOUR_APP_ID&client_secret=YOUR_APP_SECRET&fb_exchange_token=YOUR_SHORT_LIVED_TOKEN
```
- [ ] **Record Long-Lived User Token**: _____________________

**Get Page Access Token:**
- [ ] Visit this URL in browser (use long-lived user token):
```
https://graph.facebook.com/v18.0/me/accounts?access_token=YOUR_LONG_LIVED_USER_TOKEN
```
- [ ] Find your page in the response
- [ ] Copy the `access_token` value for your page
- [ ] **Record Page Access Token**: _____________________

---

## 📋 Phase 2: Application Configuration (5 minutes)

### Development Setup (User Secrets)

- [ ] Open terminal in project directory
- [ ] Run: `cd NameParser.Web`
- [ ] Run: `dotnet user-secrets init`
- [ ] Run: `dotnet user-secrets set "Facebook:AppId" "YOUR_APP_ID"`
- [ ] Run: `dotnet user-secrets set "Facebook:AppSecret" "YOUR_APP_SECRET"`
- [ ] Run: `dotnet user-secrets set "Facebook:PageId" "YOUR_PAGE_ID"`
- [ ] Run: `dotnet user-secrets set "Facebook:PageAccessToken" "YOUR_TOKEN"`
- [ ] Verify: `dotnet user-secrets list`

### Production Setup (Choose One)

**Option A: Environment Variables**
- [ ] Set `Facebook__AppId` environment variable
- [ ] Set `Facebook__AppSecret` environment variable
- [ ] Set `Facebook__PageId` environment variable
- [ ] Set `Facebook__PageAccessToken` environment variable

**Option B: Azure Key Vault**
- [ ] Create Key Vault in Azure
- [ ] Add secret: `Facebook--AppId`
- [ ] Add secret: `Facebook--AppSecret`
- [ ] Add secret: `Facebook--PageId`
- [ ] Add secret: `Facebook--PageAccessToken`
- [ ] Configure app to use Key Vault

**Option C: AWS Secrets Manager**
- [ ] Create secret in AWS Secrets Manager
- [ ] Store as JSON with Facebook configuration
- [ ] Configure app to read from Secrets Manager

---

## 📋 Phase 3: Testing (10 minutes)

### Local Testing

**Build & Run:**
- [ ] Run: `dotnet build`
- [ ] Verify: Build successful
- [ ] Run: `dotnet run --project NameParser.Web`
- [ ] Navigate to: `https://localhost:7001`

**Test Share Functionality:**
- [ ] Navigate to `/Races` page
- [ ] Verify: Races are displayed in table
- [ ] Verify: Each race has a blue "Share" button with Facebook icon
- [ ] Click "Share" button
- [ ] Verify: Confirmation dialog appears
- [ ] Click "OK" to confirm
- [ ] Verify: Success message appears
- [ ] Verify: Message shows Post ID

**Verify on Facebook:**
- [ ] Go to your Facebook Page
- [ ] Verify: New post appears
- [ ] Verify: Post contains race name and distance
- [ ] Verify: Post shows top 3 finishers
- [ ] Verify: Post includes participant count
- [ ] Verify: Post has link back to your site
- [ ] Click link in post
- [ ] Verify: Link works and shows race results

### Error Testing

**Test Invalid Token:**
- [ ] Set invalid access token in configuration
- [ ] Try to share a race
- [ ] Verify: Error message appears
- [ ] Verify: Error is logged

**Test Missing Configuration:**
- [ ] Remove one configuration value
- [ ] Try to share a race
- [ ] Verify: Appropriate error message
- [ ] Restore configuration

---

## 📋 Phase 4: Production Readiness

### Security Checklist
- [ ] Tokens stored securely (not in appsettings.json)
- [ ] No secrets committed to Git
- [ ] HTTPS enabled on production site
- [ ] App in production mode on Facebook
- [ ] Permissions approved by Facebook (if required)
- [ ] Error handling tested
- [ ] Logging configured

### Privacy & Compliance
- [ ] Participant consent obtained for public sharing
- [ ] Privacy policy updated
- [ ] Terms of service reviewed
- [ ] GDPR compliance checked (if applicable)
- [ ] Data retention policy defined

### Performance
- [ ] Rate limiting considered
- [ ] Retry logic implemented (if needed)
- [ ] Timeout handling tested
- [ ] Large race results tested

### Monitoring
- [ ] Logging configured
- [ ] Error alerts set up
- [ ] Success metrics tracked
- [ ] Failed post notifications enabled

---

## 📋 Phase 5: Documentation & Training

### Documentation
- [ ] Read `FACEBOOK_SETUP_GUIDE.md`
- [ ] Read `USER_SECRETS_SETUP.md`
- [ ] Read `FACEBOOK_QUICK_START.md`
- [ ] Read `FACEBOOK_IMPLEMENTATION_SUMMARY.md`
- [ ] Read `FACEBOOK_ARCHITECTURE.md`

### Team Training
- [ ] Share documentation with team
- [ ] Demo share functionality
- [ ] Explain when to share results
- [ ] Review privacy guidelines
- [ ] Document troubleshooting steps

---

## 📋 Phase 6: Go Live!

### Pre-Launch
- [ ] All tests passing
- [ ] Production config verified
- [ ] Backup access tokens stored securely
- [ ] Rollback plan prepared
- [ ] Support contacts identified

### Launch
- [ ] Deploy to production
- [ ] Test with one race
- [ ] Verify post appears on Facebook
- [ ] Announce feature to users
- [ ] Monitor for issues

### Post-Launch
- [ ] Monitor logs for errors
- [ ] Track user adoption
- [ ] Collect feedback
- [ ] Address any issues promptly
- [ ] Document lessons learned

---

## 📋 Optional Enhancements (Future)

### Phase 7: Advanced Features
- [ ] Implement image generation for race results
- [ ] Add challenge standings sharing
- [ ] Enable auto-posting on race processing
- [ ] Add scheduled posting
- [ ] Implement Twitter/X integration
- [ ] Add Instagram sharing
- [ ] Create custom post templates
- [ ] Build admin panel for post management
- [ ] Add post analytics dashboard
- [ ] Implement A/B testing for posts

---

## 🎯 Quick Reference

### What You Need

| Item | Where to Find | Example |
|------|---------------|---------|
| App ID | Facebook App Dashboard | 1234567890123456 |
| App Secret | Settings → Basic | abc123def456... |
| Page ID | Page Settings or Graph API | 9876543210 |
| Page Token | Graph API me/accounts | EAABsbCS1iHgBO... |

### Key URLs

| Purpose | URL |
|---------|-----|
| Developer Dashboard | https://developers.facebook.com/apps/ |
| Graph API Explorer | https://developers.facebook.com/tools/explorer/ |
| Access Token Debugger | https://developers.facebook.com/tools/debug/accesstoken/ |
| Your Races Page | https://localhost:7001/Races |

### Quick Commands

```bash
# Initialize secrets
cd NameParser.Web
dotnet user-secrets init

# Set secrets (replace with your values)
dotnet user-secrets set "Facebook:AppId" "YOUR_APP_ID"
dotnet user-secrets set "Facebook:AppSecret" "YOUR_APP_SECRET"
dotnet user-secrets set "Facebook:PageId" "YOUR_PAGE_ID"
dotnet user-secrets set "Facebook:PageAccessToken" "YOUR_TOKEN"

# Verify secrets
dotnet user-secrets list

# Build and run
dotnet build
dotnet run
```

---

## ✅ Completion Status

Mark your overall progress:

- [ ] Phase 1: Facebook App Setup (COMPLETE)
- [ ] Phase 2: Application Configuration (COMPLETE)
- [ ] Phase 3: Testing (COMPLETE)
- [ ] Phase 4: Production Readiness (COMPLETE)
- [ ] Phase 5: Documentation & Training (COMPLETE)
- [ ] Phase 6: Go Live (COMPLETE)

**🎉 All Done! You're ready to share race results on Facebook!**

---

## 📞 Need Help?

### Resources
- **Setup Guide**: `FACEBOOK_SETUP_GUIDE.md`
- **Quick Start**: `FACEBOOK_QUICK_START.md`
- **Architecture**: `FACEBOOK_ARCHITECTURE.md`
- **Facebook Docs**: https://developers.facebook.com/docs/

### Common Issues
1. **Invalid token** → Regenerate Page Access Token
2. **Permissions error** → Check app permissions in Facebook
3. **Button not working** → Check browser console for errors
4. **Post not appearing** → Verify Page ID matches token

### Support Contacts
- Facebook Developer Support: https://developers.facebook.com/support/
- Graph API Status: https://developers.facebook.com/status/

---

**Last Updated**: [Date of Implementation]  
**Version**: 1.0  
**Status**: ✅ Ready for Production
