# How to Get a Facebook Page Access Token

## The Problem
You likely have a **USER Access Token** but need a **PAGE Access Token** to post to your Facebook page.

## Quick Steps

### 1. Go to Graph API Explorer
🔗 https://developers.facebook.com/tools/explorer/

### 2. Select Your App
- In the dropdown at the top, select: **"Challenge Lucien 2026"** (or your app name)

### 3. Add Permissions
Click "Permissions" and add:
- ✅ `pages_manage_posts` (Required to create posts)
- ✅ `pages_read_engagement` (Recommended for reading page data)

### 4. Generate User Token
- Click **"Generate Access Token"**
- Log in and grant the permissions
- You'll see a token appear - **this is a USER token** (not what we need yet!)

### 5. Get the PAGE Token
This is the critical step most people miss:

1. Look for a dropdown that says **"User or Page"** or shows your name
2. Click it and select your page: **"Challenge Lucien 2026"**
3. The token in the text box will **change** - this is now a PAGE token!
4. **Copy this new token** (not the user token from before)

### 6. Make it Long-Lived
Short-lived tokens expire in 1 hour. To make it long-lived:

#### Option A: Using Graph API Explorer
1. Go to: https://developers.facebook.com/tools/accesstoken/
2. Find your page token
3. Click "Extend Access Token"
4. Copy the new long-lived token (lasts 60 days)

#### Option B: Using API Call
```bash
https://graph.facebook.com/v18.0/oauth/access_token?grant_type=fb_exchange_token&client_id=YOUR_APP_ID&client_secret=YOUR_APP_SECRET&fb_exchange_token=YOUR_SHORT_LIVED_TOKEN
```

#### Option C: Get a Non-Expiring Token (Best!)
1. Go to: https://developers.facebook.com/tools/explorer/
2. Select your app
3. In the "Get Token" dropdown → "Get Page Access Token"
4. Select your page and permissions
5. This generates a non-expiring page token (as long as you remain admin)

### 7. Update Your Configuration
```bash
# Using User Secrets (recommended)
dotnet user-secrets set "Facebook:PageAccessToken" "YOUR_NEW_PAGE_TOKEN_HERE"

# Or update appsettings.json
{
  "Facebook": {
    "PageAccessToken": "YOUR_NEW_PAGE_TOKEN_HERE"
  }
}
```

### 8. Test Again
```bash
cd NameParser.ConfigChecker
dotnet run
```

## How to Verify You Have the Right Token

✅ **Correct PAGE Token:**
- Token Type: `PAGE`
- Has `pages_manage_posts` scope
- Tied to your specific page

❌ **Wrong USER Token:**
- Token Type: `USER`
- Has user permissions like `public_profile`
- Can't post to pages on behalf of the page

## Visual Guide

```
Graph API Explorer
┌─────────────────────────────────────────┐
│ App: Challenge Lucien 2026        ▼     │
│                                          │
│ User Token (Initial):                   │
│ [EAA...xyz]  ← Don't use this!          │
│                                          │
│ Select: [Challenge Lucien 2026 Page] ▼  │ ← Click here!
│                                          │
│ Page Token (After selecting page):      │
│ [EAA...abc]  ← Use this one!            │
└─────────────────────────────────────────┘
```

## Common Mistakes

❌ Using the initial user token instead of switching to page token
❌ Not selecting the page from the dropdown
❌ Using a short-lived token that expires quickly
❌ Missing the `pages_manage_posts` permission

## Need More Help?

If you're still having issues:
1. Make sure you're an admin of the Facebook page
2. Verify the app has access to the page in Meta Business Suite
3. Check that the page is published (not in draft mode)
4. Try generating a completely new token from scratch

## References

- [Facebook Page Access Tokens](https://developers.facebook.com/docs/pages/access-tokens)
- [Graph API Explorer](https://developers.facebook.com/tools/explorer/)
- [Access Token Debugger](https://developers.facebook.com/tools/debug/accesstoken/)
