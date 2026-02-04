using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;
using NameParser.Infrastructure.Data;
using NameParser.Infrastructure.Repositories;
using System.Text;

namespace NameParser.EmailTester;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("==================================================");
        Console.WriteLine("    Challenge Mailing System - Email Tester");
        Console.WriteLine("==================================================");
        Console.WriteLine();

        // Load configuration
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        var emailService = new EmailService(config);

        while (true)
        {
            Console.WriteLine("\nSelect an option:");
            Console.WriteLine("1. Configure Gmail Settings");
            Console.WriteLine("2. Send Test Email");
            Console.WriteLine("3. Generate and Preview Email Template");
            Console.WriteLine("4. Send Email to Multiple Recipients");
            Console.WriteLine("5. View Current Settings");
            Console.WriteLine("0. Exit");
            Console.Write("\nYour choice: ");

            var choice = Console.ReadLine();

            try
            {
                switch (choice)
                {
                    case "1":
                        emailService.ConfigureGmailSettings();
                        break;
                    case "2":
                        await emailService.SendTestEmailAsync();
                        break;
                    case "3":
                        emailService.GenerateAndPreviewTemplate();
                        break;
                    case "4":
                        await emailService.SendToMultipleRecipientsAsync();
                        break;
                    case "5":
                        emailService.ViewCurrentSettings();
                        break;
                    case "0":
                        Console.WriteLine("\nGoodbye!");
                        return;
                    default:
                        Console.WriteLine("\nInvalid choice. Please try again.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nError: {ex.Message}");
                Console.ResetColor();
            }
        }
    }
}

class EmailService
{
    private string _gmailAddress;
    private string _gmailAppPassword;
    private string _smtpServer = "smtp.gmail.com";
    private int _smtpPort = 587;
    private readonly IConfiguration _config;
    private readonly ChallengeRepository _challengeRepository;
    private readonly RaceEventRepository _raceEventRepository;
    private readonly RaceRepository _raceRepository;
    private readonly ClassificationRepository _classificationRepository;

    public EmailService(IConfiguration config)
    {
        _config = config;
        _challengeRepository = new ChallengeRepository();
        _raceEventRepository = new RaceEventRepository();
        _raceRepository = new RaceRepository();
        _classificationRepository = new ClassificationRepository();
        LoadSettings();
    }

    private void LoadSettings()
    {
        _gmailAddress = _config["Gmail:Address"] ?? "";
        _gmailAppPassword = _config["Gmail:AppPassword"] ?? "";
        _smtpServer = _config["Gmail:SmtpServer"] ?? "smtp.gmail.com";
        _smtpPort = int.TryParse(_config["Gmail:SmtpPort"], out var port) ? port : 587;
    }

    public void ConfigureGmailSettings()
    {
        Console.WriteLine("\n--- Configure Gmail Settings ---");
        Console.WriteLine("Note: You must use a Gmail App Password, not your regular password.");
        Console.WriteLine("Instructions: https://support.google.com/accounts/answer/185833");
        Console.WriteLine();

        Console.Write("Gmail Address: ");
        _gmailAddress = Console.ReadLine() ?? "";

        Console.Write("Gmail App Password: ");
        _gmailAppPassword = ReadPassword();

        Console.Write($"SMTP Server [{_smtpServer}]: ");
        var server = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(server))
            _smtpServer = server;

        Console.Write($"SMTP Port [{_smtpPort}]: ");
        var portStr = Console.ReadLine();
        if (int.TryParse(portStr, out var port))
            _smtpPort = port;

        SaveSettings();
        
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n✓ Settings saved successfully!");
        Console.ResetColor();
    }

    private void SaveSettings()
    {
        var settingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
        var settings = new
        {
            Gmail = new
            {
                Address = _gmailAddress,
                AppPassword = _gmailAppPassword,
                SmtpServer = _smtpServer,
                SmtpPort = _smtpPort
            }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(settingsPath, json);
    }

    public void ViewCurrentSettings()
    {
        Console.WriteLine("\n--- Current Gmail Settings ---");
        Console.WriteLine($"Gmail Address: {_gmailAddress}");
        Console.WriteLine($"App Password: {new string('*', Math.Min(_gmailAppPassword.Length, 16))}");
        Console.WriteLine($"SMTP Server: {_smtpServer}");
        Console.WriteLine($"SMTP Port: {_smtpPort}");
    }

    public async Task SendTestEmailAsync()
    {
        if (string.IsNullOrWhiteSpace(_gmailAddress) || string.IsNullOrWhiteSpace(_gmailAppPassword))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n⚠ Gmail settings not configured. Please configure settings first (Option 1).");
            Console.ResetColor();
            return;
        }

        Console.WriteLine("\n--- Send Test Email ---");
        Console.Write("Recipient Email Address: ");
        var toEmail = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(toEmail))
        {
            Console.WriteLine("Invalid email address.");
            return;
        }

        Console.Write("Email Subject: ");
        var subject = Console.ReadLine() ?? "Test Email";

        Console.WriteLine("\nEmail Body (HTML):");
        Console.WriteLine("(Type your HTML content, then press Enter on an empty line, then type 'END' and press Enter)");
        var bodyLines = new List<string>();
        string line;
        while ((line = Console.ReadLine()) != "END")
        {
            if (line == null) break;
            bodyLines.Add(line);
        }
        var body = string.Join("\n", bodyLines);

        if (string.IsNullOrWhiteSpace(body))
        {
            body = GenerateSampleEmailBody();
            Console.WriteLine("\nUsing sample email body...");
        }

        Console.WriteLine("\nSending email...");
        await SendEmailAsync(toEmail, subject, body);
        
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✓ Email sent successfully to {toEmail}!");
        Console.ResetColor();
    }

    public void GenerateAndPreviewTemplate()
    {
        Console.WriteLine("\n--- Generate Email Template ---");

        var challenges = _challengeRepository.GetAll().OrderByDescending(c => c.Year).ToList();
        
        if (!challenges.Any())
        {
            Console.WriteLine("No challenges found in the database.");
            return;
        }

        Console.WriteLine("\nAvailable Challenges:");
        for (int i = 0; i < challenges.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {challenges[i].Name} ({challenges[i].Year})");
        }

        Console.Write("\nSelect challenge (number): ");
        if (!int.TryParse(Console.ReadLine(), out var index) || index < 1 || index > challenges.Count)
        {
            Console.WriteLine("Invalid selection.");
            return;
        }

        var selectedChallenge = challenges[index - 1];
        var template = GenerateEmailTemplate(selectedChallenge);

        Console.WriteLine("\n=== EMAIL PREVIEW ===");
        Console.WriteLine($"\nSubject: {template.Subject}");
        Console.WriteLine("\n--- Body (HTML) ---");
        Console.WriteLine(template.Body);
        Console.WriteLine("\n=== END PREVIEW ===");

        Console.Write("\nSave to file? (y/n): ");
        if (Console.ReadLine()?.ToLower() == "y")
        {
            var filename = $"email_template_{DateTime.Now:yyyyMMdd_HHmmss}.html";
            File.WriteAllText(filename, template.Body);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ Saved to {filename}");
            Console.ResetColor();
        }
    }

    public async Task SendToMultipleRecipientsAsync()
    {
        if (string.IsNullOrWhiteSpace(_gmailAddress) || string.IsNullOrWhiteSpace(_gmailAppPassword))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n⚠ Gmail settings not configured. Please configure settings first (Option 1).");
            Console.ResetColor();
            return;
        }

        Console.WriteLine("\n--- Send to Multiple Recipients ---");
        Console.WriteLine("Enter email addresses (one per line). Type 'DONE' when finished:");

        var recipients = new List<string>();
        string email;
        while ((email = Console.ReadLine()) != "DONE")
        {
            if (string.IsNullOrWhiteSpace(email)) continue;
            if (email.Contains("@"))
            {
                recipients.Add(email.Trim());
                Console.WriteLine($"  Added: {email}");
            }
        }

        if (!recipients.Any())
        {
            Console.WriteLine("No recipients added.");
            return;
        }

        Console.Write("\nEmail Subject: ");
        var subject = Console.ReadLine() ?? "Challenge Update";

        Console.Write("\nUse sample email body? (y/n): ");
        var body = Console.ReadLine()?.ToLower() == "y" 
            ? GenerateSampleEmailBody() 
            : "<p>Your custom message here</p>";

        Console.WriteLine($"\n⚠ About to send {recipients.Count} email(s).");
        Console.Write("Continue? (y/n): ");
        if (Console.ReadLine()?.ToLower() != "y")
        {
            Console.WriteLine("Cancelled.");
            return;
        }

        int success = 0;
        int failed = 0;

        foreach (var recipient in recipients)
        {
            try
            {
                Console.Write($"Sending to {recipient}... ");
                await SendEmailAsync(recipient, subject, body);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓");
                Console.ResetColor();
                success++;
                await Task.Delay(500); // Avoid rate limiting
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ ({ex.Message})");
                Console.ResetColor();
                failed++;
            }
        }

        Console.WriteLine($"\n=== Summary ===");
        Console.WriteLine($"Successful: {success}");
        Console.WriteLine($"Failed: {failed}");
    }

    private async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Challenge Administrator", _gmailAddress));
        message.To.Add(new MailboxAddress("", toEmail));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = body
        };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_smtpServer, _smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_gmailAddress, _gmailAppPassword);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    private (string Subject, string Body) GenerateEmailTemplate(Infrastructure.Data.Models.ChallengeEntity challenge)
    {
        var sb = new StringBuilder();
        var today = DateTime.Today;

        var challengeRaceEvents = _challengeRepository.GetRaceEventsForChallenge(challenge.Id)
            .OrderBy(re => re.EventDate)
            .ToList();

        var nextRace = challengeRaceEvents.FirstOrDefault(re => re.EventDate >= today);
        var previousRace = challengeRaceEvents.LastOrDefault(re => re.EventDate < today);

        var subject = $"{challenge.Name} - Update {today:dd/MM/yyyy}";

        sb.AppendLine($"<h1 style='color: #FF9800;'>🏃 {challenge.Name}</h1>");
        sb.AppendLine($"<p style='font-size: 14px; color: #666;'>Challenge Update - {today:dd MMMM yyyy}</p>");
        sb.AppendLine("<hr style='border: 1px solid #FF9800;'/>");

        if (nextRace != null)
        {
            sb.AppendLine("<h2 style='color: #2196F3;'>📅 Next Race</h2>");
            sb.AppendLine("<div style='background-color: #E3F2FD; padding: 15px; border-radius: 5px; margin: 10px 0;'>");
            sb.AppendLine($"<h3 style='margin: 0;'>{nextRace.Name}</h3>");
            sb.AppendLine($"<p><strong>📍 Date:</strong> {nextRace.EventDate:dddd, dd MMMM yyyy}</p>");
            sb.AppendLine($"<p><strong>📍 Location:</strong> {nextRace.Location ?? "TBA"}</p>");
            sb.AppendLine("</div>");
        }

        if (previousRace != null)
        {
            sb.AppendLine("<h2 style='color: #4CAF50;'>🏆 Latest Results</h2>");
            sb.AppendLine($"<h3>{previousRace.Name} - {previousRace.EventDate:dd/MM/yyyy}</h3>");

            var previousRaces = _raceRepository.GetRacesByRaceEvent(previousRace.Id);
            
            foreach (var race in previousRaces.Take(1))
            {
                var classifications = _classificationRepository.GetClassificationsByRace(race.Id, null, true)
                    .OrderBy(c => c.Position)
                    .Take(10)
                    .ToList();

                if (classifications.Any())
                {
                    sb.AppendLine("<table style='width: 100%; border-collapse: collapse; margin-bottom: 20px;'>");
                    sb.AppendLine("<thead>");
                    sb.AppendLine("<tr style='background-color: #FF9800; color: white;'>");
                    sb.AppendLine("<th style='padding: 8px; text-align: left;'>Pos</th>");
                    sb.AppendLine("<th style='padding: 8px; text-align: left;'>Name</th>");
                    sb.AppendLine("<th style='padding: 8px; text-align: left;'>Time</th>");
                    sb.AppendLine("<th style='padding: 8px; text-align: left;'>Points</th>");
                    sb.AppendLine("</tr>");
                    sb.AppendLine("</thead>");
                    sb.AppendLine("<tbody>");

                    foreach (var c in classifications)
                    {
                        var rowStyle = c.Position % 2 == 0 ? "background-color: #f2f2f2;" : "";
                        sb.AppendLine($"<tr style='{rowStyle}'>");
                        sb.AppendLine($"<td style='padding: 8px;'>{c.Position}</td>");
                        sb.AppendLine($"<td style='padding: 8px;'>{c.MemberFirstName} {c.MemberLastName}</td>");
                        sb.AppendLine($"<td style='padding: 8px;'>{(c.RaceTime.HasValue ? c.RaceTime.Value.ToString(@"hh\:mm\:ss") : "-")}</td>");
                        sb.AppendLine($"<td style='padding: 8px;'><strong>{c.Points}</strong></td>");
                        sb.AppendLine("</tr>");
                    }

                    sb.AppendLine("</tbody>");
                    sb.AppendLine("</table>");
                }
            }
        }

        sb.AppendLine("<hr style='border: 1px solid #FF9800; margin-top: 30px;'/>");
        sb.AppendLine("<p style='font-size: 12px; color: #666;'>Keep up the great work! See you at the next race! 🏃💪</p>");

        return (subject, sb.ToString());
    }

    private string GenerateSampleEmailBody()
    {
        return @"
<h1 style='color: #FF9800;'>🏃 Challenge Test Email</h1>
<p>This is a test email from the Challenge Mailing System.</p>

<h2 style='color: #2196F3;'>Sample Table</h2>
<table style='width: 100%; border-collapse: collapse;'>
  <thead>
    <tr style='background-color: #FF9800; color: white;'>
      <th style='padding: 8px; text-align: left;'>Position</th>
      <th style='padding: 8px; text-align: left;'>Name</th>
      <th style='padding: 8px; text-align: left;'>Points</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td style='padding: 8px;'>1</td>
      <td style='padding: 8px;'><strong>John Doe</strong></td>
      <td style='padding: 8px;'>100</td>
    </tr>
    <tr style='background-color: #f2f2f2;'>
      <td style='padding: 8px;'>2</td>
      <td style='padding: 8px;'><strong>Jane Smith</strong></td>
      <td style='padding: 8px;'>95</td>
    </tr>
  </tbody>
</table>

<p style='font-size: 12px; color: #666;'>This is a test message.</p>";
    }

    private string ReadPassword()
    {
        var password = new StringBuilder();
        ConsoleKeyInfo key;

        do
        {
            key = Console.ReadKey(true);

            if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
            {
                password.Append(key.KeyChar);
                Console.Write("*");
            }
            else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
            {
                password.Remove(password.Length - 1, 1);
                Console.Write("\b \b");
            }
        }
        while (key.Key != ConsoleKey.Enter);

        Console.WriteLine();
        return password.ToString();
    }
}
