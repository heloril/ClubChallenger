# Facebook Configuration Checker

A console application to validate and test your Facebook API configuration.

## Features

✅ **Configuration Validation**
- Checks if all required Facebook settings are present
- Shows masked values for sensitive data (tokens/secrets)

✅ **API Connection Tests**
1. **Page Information** - Verifies access to your Facebook page
2. **Token Permissions** - Checks if required permissions are granted
3. **Token Expiry** - Shows when your access token expires

## Usage

### Option 1: Using User Secrets (Recommended)

```bash
cd NameParser.ConfigChecker

# Set each value
dotnet user-secrets set "Facebook:AppId" "your-app-id"
dotnet user-secrets set "Facebook:AppSecret" "your-app-secret"
dotnet user-secrets set "Facebook:PageId" "your-page-id"
dotnet user-secrets set "Facebook:PageAccessToken" "your-page-access-token"

# Run the checker
dotnet run
```

### Option 2: Using appsettings.json

Edit `appsettings.json` and add your values:

```json
{
  "Facebook": {
    "AppId": "your-app-id",
    "AppSecret": "your-app-secret",
    "PageId": "your-page-id",
    "PageAccessToken": "your-page-access-token"
  }
}
```

Then run:
```bash
dotnet run
```

### Option 3: Using Environment Variables

```bash
# Windows PowerShell
$env:Facebook__AppId="your-app-id"
$env:Facebook__AppSecret="your-app-secret"
$env:Facebook__PageId="your-page-id"
$env:Facebook__PageAccessToken="your-page-access-token"

dotnet run
```

## Sample Output

```
=== Facebook Configuration Checker ===

📋 Configuration Status:
   AppId: ✅ Set (3891227247844729)
   AppSecret: ✅ Set (b011...bfd4)
   PageId: ✅ Set (979182415278149)
   PageAccessToken: ✅ Set (EAA3...YZBEM)

🔄 Testing Facebook API connection...

Test 1: Fetching Page Information
   Status: ✅ Success
   Page Name: Your Page Name
   Category: Sports & Recreation

Test 2: Checking Access Token Permissions
   Status: ✅ Success
   Granted: pages_manage_posts, pages_read_engagement

Test 3: Checking Access Token Expiry
   Status: ✅ Success
   Token is valid
   ✅ Token does not expire (long-lived or permanent)

=== Summary ===
✅ All tests passed! Facebook configuration is working correctly.
```

## Required Permissions

Your Page Access Token must have these permissions:
- `pages_manage_posts` - To create posts on your page
- `pages_read_engagement` - To read page information

## Troubleshooting

### Token Expired
If you see "Token is not valid", you need to generate a new Page Access Token:
1. Go to [Facebook Graph API Explorer](https://developers.facebook.com/tools/explorer/)
2. Select your app and generate a new Page Access Token
3. Update your configuration with the new token

### Missing Permissions
If permissions are missing:
1. Generate a new token with the required permissions
2. Make sure to select your page when generating the token

### Cannot Access Page
If you can't access page information:
1. Verify your PageId is correct
2. Make sure your token is for the correct page
3. Check that your app has access to the page

## Exit Codes

- `0` - All tests passed
- `1` - Configuration incomplete or tests failed
