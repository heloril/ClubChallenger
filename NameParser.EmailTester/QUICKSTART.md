# Quick Start Guide - Email Tester Console App

## Running the Email Tester

### Method 1: Using Visual Studio

1. **Set as Startup Project:**
   - Right-click on `NameParser.EmailTester` in Solution Explorer
   - Select "Set as Startup Project"
   - Press `F5` or click "Start"

2. **Or run alongside main app:**
   - Right-click solution → Properties → Startup Project
   - Select "Multiple startup projects"
   - Set both `NameParser.UI` and `NameParser.EmailTester` to "Start"

### Method 2: Using Command Line

```bash
# From solution root directory
cd NameParser.EmailTester
dotnet run
```

### Method 3: Using PowerShell (from solution root)

```powershell
dotnet run --project NameParser.EmailTester
```

## First Time Setup (Quick)

1. Run the application
2. Select option `1` (Configure Gmail Settings)
3. Enter your details:
   - Gmail address: `your-email@gmail.com`
   - App password: (get from https://myaccount.google.com/apppasswords)
   - SMTP server: Press Enter for default `smtp.gmail.com`
   - SMTP port: Press Enter for default `587`
4. Settings saved! ✓

## Quick Test

After setup:
1. Select option `2` (Send Test Email)
2. Enter your email address
3. Type a subject: `Test Email`
4. Press Enter on empty line, type `END`, press Enter (uses sample email)
5. Check your inbox!

## Example Session

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

Your choice: 1

--- Configure Gmail Settings ---
Note: You must use a Gmail App Password, not your regular password.
Instructions: https://support.google.com/accounts/answer/185833

Gmail Address: myemail@gmail.com
Gmail App Password: ****************
SMTP Server [smtp.gmail.com]: 
SMTP Port [587]: 

✓ Settings saved successfully!

Your choice: 2

--- Send Test Email ---
Recipient Email Address: test@example.com
Email Subject: Test from Challenge System

Email Body (HTML):
(Type your HTML content, then press Enter on an empty line, then type 'END' and press Enter)
END

Using sample email body...

Sending email...
✓ Email sent successfully to test@example.com!
```

## Common Commands

| Option | Description | Use Case |
|--------|-------------|----------|
| 1 | Configure Gmail | First-time setup or change credentials |
| 2 | Send Test Email | Quick test of email functionality |
| 3 | Generate Template | Preview email from challenge data |
| 4 | Send Multiple | Bulk email testing |
| 5 | View Settings | Check current configuration |

## Tips

- **Test first**: Always use option 2 to send a test email before bulk sending
- **Preview templates**: Use option 3 to see generated emails before sending
- **Save to file**: When previewing templates, save to HTML file to view in browser
- **Check spam**: First emails might land in spam folder
- **Rate limiting**: The app adds 500ms delay between emails automatically

## Getting Gmail App Password

1. Go to: https://myaccount.google.com/apppasswords
2. Select "Mail" and your device
3. Click "Generate"
4. Copy the 16-character password
5. Use this in the Email Tester (NOT your regular Gmail password)

## Troubleshooting

### "Gmail settings not configured"
→ Run option 1 first to configure settings

### "Authentication failed"
→ Make sure you're using an App Password, not regular password
→ Enable 2-factor authentication on Google account first

### "No challenges found"
→ Run the main WPF app first to populate the database

### "Connection refused"
→ Check firewall settings
→ Verify SMTP server is smtp.gmail.com and port is 587

## What's Next?

After testing with the console app:
- Go back to the WPF application
- Use the "Challenge Mailing" tab
- All settings are shared between console and WPF app
- Generate real email templates from your challenge data
- Send to actual challengers!

## Security Note

⚠️ The `appsettings.json` file contains your Gmail credentials. 

**Do NOT:**
- Commit it to version control with real credentials
- Share it publicly
- Send it to others

**Consider:**
- Add `appsettings.json` to `.gitignore`
- Use environment variables for production
- Keep backups of your App Password separately
