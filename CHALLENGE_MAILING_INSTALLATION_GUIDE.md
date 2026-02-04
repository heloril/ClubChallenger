# Challenge Mailing Feature - Installation Guide

## Required NuGet Packages

To use the Challenge Mailing feature, you need to install the following NuGet package:

### MailKit (Recommended for Gmail)
```
Install-Package MailKit
```

**Why MailKit?**
- Full support for modern authentication
- Works perfectly with Gmail App Passwords
- Better than built-in SmtpClient
- Async/await support
- More reliable and secure

## Installation Steps

### Visual Studio Package Manager Console:
```powershell
Install-Package MailKit -ProjectName NameParser.UI
```

### .NET CLI:
```bash
dotnet add NameParser.UI package MailKit
```

### Visual Studio NuGet Package Manager:
1. Right-click on `NameParser.UI` project
2. Select "Manage NuGet Packages"
3. Search for "MailKit"
4. Click "Install"

## Gmail App Password Setup

### Important: You MUST use an App Password, not your regular Gmail password

1. Go to https://myaccount.google.com/apppasswords
2. Sign in to your Google account
3. Select app: "Mail"
4. Select device: "Windows Computer"
5. Click "Generate"
6. Copy the 16-character password
7. Use this password in the Challenge Mailing tab

### Enable 2-Factor Authentication (Required)
App Passwords require 2FA to be enabled on your Google account:
1. Go to https://myaccount.google.com/security
2. Enable "2-Step Verification"
3. Then you can create App Passwords

## Configuration

After installation, configure Gmail settings in the Challenge Mailing tab:

1. **Gmail Address**: your.email@gmail.com
2. **App Password**: xxxx xxxx xxxx xxxx (16 characters)
3. **SMTP Server**: smtp.gmail.com (default)
4. **SMTP Port**: 587 (default)

Click "💾 Save Settings" to persist configuration.

## Features

### Email Template Generation
Auto-generates professional HTML email with:
- Next race details
- Upcoming races summary (3 races)
- Previous race results (top 10 per distance)
- Current challenge standings (top 10)

### Send Test Email
- Send preview to yourself before mass mailing
- Verify formatting and content
- Check Gmail configuration

### Send to All Challengers
- Automatically retrieves challenger emails from Members.json
- Sends to all challengers with email addresses
- Progress feedback during sending
- Error reporting

## Troubleshooting

### "Authentication failed"
- Make sure you're using App Password, not regular password
- Verify 2FA is enabled
- Generate a new App Password

### "Connection refused"
- Check SMTP server: smtp.gmail.com
- Check SMTP port: 587
- Check firewall/antivirus settings

### "No challengers with email addresses found"
- Add email addresses to Members.json
- Format: `{"FirstName": "John", "LastName": "Doe", "Email": "john@example.com", "IsMember": true, "IsChallenger": true}`

### "Template generation failed"
- Ensure challenge is selected
- Verify race events exist for challenge
- Check database connectivity

## Alternative: Built-in SmtpClient

If you prefer not to use MailKit, you can use System.Net.Mail.SmtpClient:

**Note:** Built-in SmtpClient has known issues with Gmail and may not work reliably with App Passwords. MailKit is strongly recommended.

## Security Notes

1. **App Passwords are safer** than using your main password
2. **Never commit passwords** to source control
3. **Store passwords securely** in app.config (encrypted)
4. **Revoke App Passwords** you no longer use
5. **Monitor sent emails** in Gmail Sent folder

## Email Best Practices

1. **Always send test first** before mass mailing
2. **Check spam** - test emails might go to spam initially
3. **Limit frequency** - don't spam your challengers
4. **Respect unsubscribes** - provide opt-out mechanism
5. **Preview on mobile** - many users read on phones

## Member Email Management

Edit `Members.json` to add/update email addresses:

```json
[
  {
    "FirstName": "John",
    "LastName": "Doe",
    "Email": "john.doe@example.com",
    "IsMember": true,
    "IsChallenger": true
  },
  {
    "FirstName": "Jane",
    "LastName": "Smith",
    "Email": "jane.smith@example.com",
    "IsMember": true,
    "IsChallenger": true
  }
]
```

## Rate Limiting

Gmail has sending limits:
- **500 emails/day** for regular Gmail
- **2000 emails/day** for Google Workspace

The system adds 500ms delay between emails to avoid rate limiting.

---

**Package Version:** MailKit 4.x or later
**Minimum .NET Version:** .NET 8.0
**Status:** Ready for use after package installation
