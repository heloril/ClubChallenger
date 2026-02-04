# Facebook Integration Setup Guide

This guide will walk you through setting up Facebook OAuth and Graph API integration to share race results to your Facebook page.

## Prerequisites

- A Facebook account
- A Facebook Page (for your running club/organization)
- Admin access to the Facebook Page
- Your ASP.NET application deployed with a public URL (or use ngrok for local testing)

---

## Step 1: Create a Facebook App

1. **Go to Facebook Developers Portal**
   - Visit: https://developers.facebook.com/
   - Click "My Apps" in the top right
   - Click "Create App"

2. **Select App Type**
   - Choose: **"Business"** (or "Other" if Business is not available)
   - Click "Next"

3. **Provide App Details**
   - **App Name**: Enter a name (e.g., "ClubChallenger Results")
   - **App Contact Email**: Your email address
   - Click "Create App"

4. **Complete Security Check**
   - Complete the CAPTCHA verification

5. **Navigate to App Dashboard**
   - You should now see your App Dashboard
   - Note your **App ID** (displayed at the top)

---

## Step 2: Get Your App Secret

1. **In the App Dashboard**
   - Go to **Settings > Basic** (left sidebar)
   - Find **App Secret**
   - Click "Show" and enter your Facebook password
   - **Copy and save this value securely** - you'll need it later

---

## Step 3: Add Facebook Login Product

1. **Add Products**
   - In the left sidebar, click **"Add Product"** (+ icon)
   - Find **"Facebook Login"**
   - Click **"Set Up"**

2. **Configure OAuth Settings**
   - Go to **Facebook Login > Settings** (left sidebar)
   - Under **"Valid OAuth Redirect URIs"**, add:
     ```
     https://yourdomain.com/signin-facebook
     https://localhost:7001/signin-facebook
     ```
   - Replace `yourdomain.com` with your actual domain
   - Click **"Save Changes"**

---

## Step 4: Request Permissions

1. **Go to App Review > Permissions and Features**
   - Click **"Request Advanced Access"** for the following permissions:
     - `pages_show_list` - To read list of pages you manage
     - `pages_read_engagement` - To read page data
     - `pages_manage_posts` - To publish posts on behalf of the page
     - `pages_manage_engagement` - To manage page engagement

2. **Provide Use Case**
   - For each permission, Facebook will ask for a use case
   - Example response: "This app allows our running club to automatically share race results and challenge standings to our Facebook page to keep our community informed."

3. **App Review Process**
   - Some permissions may require **App Review** by Facebook
   - This process can take several days
   - You'll need to provide screenshots and explanations of how you use the permissions
   - **For Development/Testing**: You can test with admin/developer accounts without approval

---

## Step 5: Get Your Page ID

1. **Find Your Facebook Page ID**
   
   **Method 1: Via Page Settings**
   - Go to your Facebook Page
   - Click "Settings" (if you're an admin)
   - Click "Page Info" (left sidebar)
   - The Page ID is displayed

   **Method 2: Via URL**
   - Go to your Facebook Page
   - Look at the URL: `https://www.facebook.com/YourPageName`
   - Right-click the page and "View Page Source"
   - Search for `"pageID"` - you'll find a numeric ID

   **Method 3: Via Graph API**
   - Visit: `https://www.facebook.com/YourPageName?fields=id`
   - The numeric ID will be displayed

---

## Step 6: Generate Page Access Token

This is the most important step!

1. **Go to Graph API Explorer**
   - Visit: https://developers.facebook.com/tools/explorer/

2. **Select Your App**
   - In the top-right "Meta App" dropdown, select your created app

3. **Generate User Access Token**
   - Click "Generate Access Token"
   - Log in with your Facebook account if prompted
   - **Select Permissions**: Check the following:
     - `pages_show_list`
     - `pages_read_engagement`
     - `pages_manage_posts`
   - Click "Generate Access Token"
   - Authorize the permissions

4. **Get Your Pages**
   - In the query field, enter: `me/accounts`
   - Click "Submit"
   - You'll see a list of pages you manage
   - Find your page in the response and **copy the `access_token` value**
   - This is your **Page Access Token**

5. **Get Long-Lived Page Access Token (Important!)**
   
   The token from Graph Explorer expires in ~1 hour. You need a long-lived token.

   **Option A: Using Access Token Tool**
   - Visit: https://developers.facebook.com/tools/accesstoken/
   - You'll see your app and user tokens
   - Click "Extend" on the User Token to get a 60-day token
   - Then use that to get the page token again via `me/accounts`

   **Option B: Using Graph API (Recommended)**
   
   Run this in your browser or Postman:
   ```
   https://graph.facebook.com/v18.0/oauth/access_token?grant_type=fb_exchange_token&client_id=YOUR_APP_ID&client_secret=YOUR_APP_SECRET&fb_exchange_token=YOUR_SHORT_LIVED_USER_TOKEN
   ```
   
   Replace:
   - `YOUR_APP_ID` - Your App ID
   - `YOUR_APP_SECRET` - Your App Secret
   - `YOUR_SHORT_LIVED_USER_TOKEN` - The user token from Graph Explorer
   
   This gives you a long-lived user access token (60 days).
   
   Then use this long-lived user token to get the page token:
   ```
   https://graph.facebook.com/v18.0/me/accounts?access_token=LONG_LIVED_USER_TOKEN
   ```
   
   The `access_token` in the response for your page is a **never-expiring page access token** (as long as the app permissions don't change)!

---

## Step 7: Configure Your Application

1. **Open `appsettings.json`** in your project

2. **Update Facebook Settings**
   ```json
   {
     "Facebook": {
       "AppId": "YOUR_APP_ID_HERE",
       "AppSecret": "YOUR_APP_SECRET_HERE",
       "PageId": "YOUR_PAGE_ID_HERE",
       "PageAccessToken": "YOUR_PAGE_ACCESS_TOKEN_HERE"
     }
   }
   ```

3. **For Production: Use User Secrets or Environment Variables**
   
   **Never commit these values to source control!**
   
   **Using User Secrets (Recommended for Development):**
   ```bash
   dotnet user-secrets init --project NameParser.Web
   dotnet user-secrets set "Facebook:AppId" "YOUR_APP_ID" --project NameParser.Web
   dotnet user-secrets set "Facebook:AppSecret" "YOUR_APP_SECRET" --project NameParser.Web
   dotnet user-secrets set "Facebook:PageId" "YOUR_PAGE_ID" --project NameParser.Web
   dotnet user-secrets set "Facebook:PageAccessToken" "YOUR_TOKEN" --project NameParser.Web
   ```

   **Using Environment Variables (Recommended for Production):**
   - Set environment variables on your server:
     - `Facebook__AppId`
     - `Facebook__AppSecret`
     - `Facebook__PageId`
     - `Facebook__PageAccessToken`

---

## Step 8: Test the Integration

1. **Run Your Application**
   ```bash
   cd NameParser.Web
   dotnet run
   ```

2. **Navigate to Races Page**
   - Go to: `https://localhost:7001/Races` (or your configured port)

3. **Share a Race Result**
   - Find a race in your list
   - Click the blue **"Share"** button (with Facebook icon)
   - Confirm the action
   - Check your Facebook Page - the results should be posted!

4. **Troubleshooting**
   - Check browser console for errors
   - Check application logs for exceptions
   - Verify all tokens and IDs are correct
   - Ensure your app has the required permissions

---

## Step 9: App Review (For Production)

If you're using this in production and need to post as other users (not just your admin account), you'll need to submit your app for review:

1. **Prepare Your App for Review**
   - Add a Privacy Policy URL in **App Settings > Basic**
   - Add App Icon (1024x1024 pixels)
   - Add a clear description of what your app does

2. **Submit for Review**
   - Go to **App Review > Permissions and Features**
   - Click "Request Advanced Access" on required permissions
   - Provide detailed explanations and screenshots
   - Submit for review

3. **Wait for Approval**
   - Facebook typically reviews within 3-5 business days
   - Respond promptly to any questions

---

## Security Best Practices

1. **Protect Your Tokens**
   - Never commit tokens to Git
   - Use environment variables or user secrets
   - Rotate tokens periodically

2. **Monitor Token Expiration**
   - Page tokens should never expire, but user tokens do
   - Implement error handling for expired tokens
   - Set up alerts for failed posts

3. **Limit Permissions**
   - Only request permissions you actually need
   - Review permissions regularly

4. **Rate Limiting**
   - Facebook has rate limits on API calls
   - Don't post too frequently
   - Implement retry logic with exponential backoff

---

## API Reference

### Posting to Facebook

The `FacebookService` class handles all Facebook interactions:

```csharp
// Post race results
var result = await _facebookService.PostRaceResultsAsync(
    raceName: "Summer 10K Championship",
    raceUrl: "https://yoursite.com/races/123",
    summary: "Race summary with top finishers...",
    imageData: null // Optional: byte array of image
);

// Post challenge results
var result = await _facebookService.PostChallengeResultsAsync(
    challengeTitle: "Annual Running Challenge",
    challengeUrl: "https://yoursite.com/challenge/2024",
    summary: "Challenge standings...",
    imageData: null
);

// Test connection
bool isConnected = await _facebookService.TestConnectionAsync();
```

### Response Object

```csharp
public class FacebookPostResponse
{
    public bool Success { get; set; }
    public string? PostId { get; set; }
    public string? PostUrl { get; set; }
    public string? ErrorMessage { get; set; }
}
```

---

## Common Issues & Solutions

### Issue: "Invalid OAuth access token"
**Solution**: Your access token has expired or is invalid. Generate a new long-lived token.

### Issue: "Permissions error"
**Solution**: Ensure your app has requested and been granted the necessary permissions. Check App Review status.

### Issue: "Page not found"
**Solution**: Verify your Page ID is correct. Make sure you're using the numeric Page ID, not the page name.

### Issue: "App not approved"
**Solution**: For testing, add your Facebook account as an app admin/developer/tester. Approved permissions aren't needed for testing.

### Issue: Posts not appearing on page
**Solution**: 
- Check if the post was actually created (check the returned Post ID)
- Verify you're posting to the correct page
- Check Facebook Activity Log on your page
- Ensure the access token belongs to the correct page

---

## Testing with Ngrok (Local Development)

If you want to test with public URLs before deploying:

1. **Install Ngrok**
   - Download from: https://ngrok.com/download

2. **Run Your Application**
   ```bash
   dotnet run
   ```

3. **Start Ngrok**
   ```bash
   ngrok http https://localhost:7001
   ```

4. **Update Facebook App Settings**
   - Use the ngrok URL (e.g., `https://abc123.ngrok.io`) in your OAuth redirect URIs

---

## Monitoring & Maintenance

1. **Set Up Logging**
   - The `FacebookService` logs all operations
   - Monitor logs for failed posts
   - Set up alerts for errors

2. **Token Refresh**
   - Page tokens shouldn't expire, but monitor for changes
   - Implement a refresh mechanism if needed

3. **API Version Updates**
   - Facebook Graph API versions are deprecated periodically
   - Currently using v18.0
   - Update to newer versions as needed

---

## Additional Features (Future Enhancements)

Consider implementing:
- Image generation for race results (chart/graphic)
- Scheduled posting
- Posting to multiple social media platforms
- User engagement analytics
- Auto-posting when races are processed
- Custom message templates

---

## Support & Resources

- **Facebook Developers Docs**: https://developers.facebook.com/docs/
- **Graph API Explorer**: https://developers.facebook.com/tools/explorer/
- **Access Token Debugger**: https://developers.facebook.com/tools/debug/accesstoken/
- **App Dashboard**: https://developers.facebook.com/apps/

---

## Quick Reference: All Required IDs/Tokens

Make sure you have all of these configured:

- ✅ **App ID**: Found in App Dashboard
- ✅ **App Secret**: Found in App Settings > Basic
- ✅ **Page ID**: Found in Page Settings or via Graph API
- ✅ **Page Access Token**: Generated via Graph API Explorer and me/accounts

---

**You're all set! Click the Facebook Share button on any race to post results to your page. 🎉**
