# User Secrets Configuration Template

This file shows you how to configure Facebook settings securely using .NET User Secrets.

## Setup Instructions

### 1. Initialize User Secrets

Run this command in the NameParser.Web project directory:

```bash
cd NameParser.Web
dotnet user-secrets init
```

This will add a UserSecretsId to your .csproj file.

### 2. Set Facebook Configuration

Replace the placeholder values with your actual Facebook credentials:

```bash
# App ID from Facebook App Dashboard
dotnet user-secrets set "Facebook:AppId" "YOUR_FACEBOOK_APP_ID_HERE"

# App Secret from Facebook App Settings
dotnet user-secrets set "Facebook:AppSecret" "YOUR_FACEBOOK_APP_SECRET_HERE"

# Your Facebook Page ID (numeric)
dotnet user-secrets set "Facebook:PageId" "YOUR_FACEBOOK_PAGE_ID_HERE"

# Long-lived Page Access Token
dotnet user-secrets set "Facebook:PageAccessToken" "YOUR_PAGE_ACCESS_TOKEN_HERE"
```

### 3. Verify Configuration

List all secrets to verify they're set correctly:

```bash
dotnet user-secrets list
```

You should see:
```
Facebook:AppId = YOUR_APP_ID
Facebook:AppSecret = YOUR_APP_SECRET
Facebook:PageId = YOUR_PAGE_ID
Facebook:PageAccessToken = YOUR_TOKEN
```

### 4. Remove Secrets (if needed)

To remove a specific secret:
```bash
dotnet user-secrets remove "Facebook:AppId"
```

To clear all secrets:
```bash
dotnet user-secrets clear
```

## Production Deployment

For production environments, use one of these approaches:

### Option 1: Environment Variables

Set these environment variables on your server:

```bash
Facebook__AppId=your_app_id
Facebook__AppSecret=your_app_secret
Facebook__PageId=your_page_id
Facebook__PageAccessToken=your_token
```

Note: Use double underscores `__` to represent nested configuration.

### Option 2: Azure Key Vault

If deploying to Azure, use Azure Key Vault:

1. Create a Key Vault in Azure
2. Add secrets with these names:
   - `Facebook--AppId`
   - `Facebook--AppSecret`
   - `Facebook--PageId`
   - `Facebook--PageAccessToken`

3. Configure your app to use Key Vault in Program.cs

### Option 3: AWS Secrets Manager

If deploying to AWS, use AWS Secrets Manager:

1. Create a secret in AWS Secrets Manager
2. Store as JSON:
```json
{
  "Facebook:AppId": "your_app_id",
  "Facebook:AppSecret": "your_app_secret",
  "Facebook:PageId": "your_page_id",
  "Facebook:PageAccessToken": "your_token"
}
```

3. Configure your app to read from Secrets Manager

## Security Notes

⚠️ **NEVER commit these values to source control!**

- User secrets are stored locally outside the project directory
- They are NOT included in Git commits
- Each developer needs to set up their own user secrets
- Production environments should use environment variables or key vaults

## Troubleshooting

### Secrets not loading?

Check if user secrets are properly initialized:
```bash
dotnet user-secrets list --project NameParser.Web
```

### Still seeing placeholder values?

Make sure:
1. User secrets are set in the correct project (NameParser.Web)
2. The app is running in Development environment
3. The UserSecretsId is in the .csproj file
4. You've restarted the application after setting secrets
