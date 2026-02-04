# Challenge Mailing System - Email Tester

A console application for testing the challenge mailing functionality without using the WPF application.

## Features

- **Configure Gmail Settings**: Set up Gmail address, app password, SMTP server, and port
- **Send Test Email**: Send a test email to a single recipient
- **Generate Email Templates**: Generate and preview email templates from challenge data
- **Send to Multiple Recipients**: Send emails to multiple recipients at once
- **View Current Settings**: Display currently configured Gmail settings

## Prerequisites

1. **.NET 8 SDK** installed
2. **Gmail App Password** (not your regular password)
   - Create one at: https://myaccount.google.com/apppasswords
   - Instructions: https://support.google.com/accounts/answer/185833

## Setup

1. **Build the project:**
   ```bash
   dotnet build
   ```

2. **Run the application:**
   ```bash
   dotnet run --project NameParser.EmailTester
   ```

## Usage

### Option 1: Configure Gmail Settings

First-time setup:
1. Select option `1` from the menu
2. Enter your Gmail address
3. Enter your Gmail App Password (it will be masked with asterisks)
4. Press Enter to keep default SMTP settings or enter custom values
5. Settings are saved to `appsettings.json`

### Option 2: Send Test Email

1. Select option `2`
2. Enter recipient email address
3. Enter email subject
4. Enter HTML body content (or press Enter then type `END` to use sample)
5. Email will be sent

### Option 3: Generate and Preview Email Template

1. Select option `3`
2. Choose a challenge from the list
3. View the generated HTML email template
4. Optionally save to an HTML file

### Option 4: Send to Multiple Recipients

1. Select option `4`
2. Enter email addresses (one per line)
3. Type `DONE` when finished
4. Enter email subject
5. Choose to use sample body or custom content
6. Confirm to send

### Option 5: View Current Settings

View your configured Gmail settings (password is masked).

## Configuration File

The `appsettings.json` file stores your Gmail settings:

```json
{
  "Gmail": {
    "Address": "your-email@gmail.com",
    "AppPassword": "your-app-password",
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587
  }
}
```

**⚠️ Security Warning:** Keep this file secure and never commit it to version control with real credentials.

## Email Template Features

Generated email templates include:

- **Challenge header** with emojis
- **Next race information** with date, location, and distances
- **Latest race results** in HTML tables
- **Current challenge standings** with rankings
- **Styled HTML** with colors and formatting

## Troubleshooting

### Authentication Failed

**Problem:** "Authentication failed" error when sending email.

**Solution:**
- Ensure you're using a Gmail App Password, not your regular password
- Verify your Gmail address is correct
- Check that 2-factor authentication is enabled on your Google account

### Connection Timeout

**Problem:** Connection times out when sending email.

**Solution:**
- Check your internet connection
- Verify SMTP server and port are correct (smtp.gmail.com:587)
- Check if your firewall is blocking outgoing connections

### No Challenges Found

**Problem:** "No challenges found in the database" message.

**Solution:**
- Ensure the NameParser database is properly configured
- Run the main WPF application to populate the database first
- Check that `appsettings.json` in the NameParser project has correct database connection

## Example Usage

```
==================================================
    Challenge Mailing System - Email Tester
==================================================

Select an option:
1. Configure Gmail Settings
2. Send Test Email
3. Generate and Preview Email Template
4. Send to Multiple Recipients
5. View Current Settings
0. Exit

Your choice: 2

--- Send Test Email ---
Recipient Email Address: test@example.com
Email Subject: Test Email

Sending email...
✓ Email sent successfully to test@example.com!
```

## Dependencies

- **MailKit** (4.14.1) - SMTP email sending
- **MimeKit** - Email message construction
- **Microsoft.Extensions.Configuration** - Configuration management
- **NameParser** (project reference) - Database access and repositories

## Notes

- The tester uses the same database as the main WPF application
- Settings are stored in `appsettings.json` in the application directory
- A 500ms delay is added between emails when sending to multiple recipients to avoid rate limiting
- HTML email bodies are fully supported with tables, styling, and formatting
