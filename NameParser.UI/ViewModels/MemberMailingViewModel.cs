using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.Configuration;
using NameParser.Infrastructure.Data.Models;
using NameParser.Infrastructure.Data;
using NameParser.Infrastructure.Repositories;
using NameParser.UI.Services;
using MimeKit;
using MailKit.Net.Smtp;

namespace NameParser.UI.ViewModels
{
    public class MemberMailingViewModel : ViewModelBase
    {
        private readonly RaceEventRepository _raceEventRepository;
        private readonly RaceRepository _raceRepository;
        private readonly ClassificationRepository _classificationRepository;
        private readonly JsonMemberRepository _memberRepository;
        private readonly IConfiguration _configuration;
        private readonly LocalizationService _localization;

        private DateTime _mailingDate;
        private string _emailSubject;
        private string _emailBody;
        private string _testEmailAddress;
        private string _statusMessage;
        private bool _isSending;

        // Gmail Configuration
        private string _gmailAddress;
        private string _gmailAppPassword;
        private string _smtpServer = "smtp.gmail.com";
        private int _smtpPort = 587;

        public MemberMailingViewModel()
        {
            _raceEventRepository = new RaceEventRepository();
            _raceRepository = new RaceRepository();
            _classificationRepository = new ClassificationRepository();
            _memberRepository = new JsonMemberRepository();
            _localization = LocalizationService.Instance;

            // Load configuration from appsettings.json
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            // Initialize commands BEFORE setting properties that trigger RaiseCanExecuteChanged
            GenerateTemplateCommand = new RelayCommand(ExecuteGenerateTemplate, CanExecuteGenerateTemplate);
            SendTestEmailCommand = new RelayCommand(ExecuteSendTestEmail, CanExecuteSendTestEmail);
            SendToAllMembersCommand = new RelayCommand(ExecuteSendToAllMembers, CanExecuteSendToAllMembers);

            MailingDate = DateTime.Today;

            LoadGmailSettings();
        }

        public DateTime MailingDate
        {
            get => _mailingDate;
            set
            {
                if (SetProperty(ref _mailingDate, value))
                {
                    ((RelayCommand)GenerateTemplateCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public string EmailSubject
        {
            get => _emailSubject;
            set => SetProperty(ref _emailSubject, value);
        }

        public string EmailBody
        {
            get => _emailBody;
            set => SetProperty(ref _emailBody, value);
        }

        public string TestEmailAddress
        {
            get => _testEmailAddress;
            set
            {
                if (SetProperty(ref _testEmailAddress, value))
                {
                    ((RelayCommand)SendTestEmailCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsSending
        {
            get => _isSending;
            set => SetProperty(ref _isSending, value);
        }

        public string GmailAddress
        {
            get => _gmailAddress;
            set => SetProperty(ref _gmailAddress, value);
        }

        public string GmailAppPassword
        {
            get => _gmailAppPassword;
            set => SetProperty(ref _gmailAppPassword, value);
        }

        public string SmtpServer
        {
            get => _smtpServer;
            set => SetProperty(ref _smtpServer, value);
        }

        public int SmtpPort
        {
            get => _smtpPort;
            set => SetProperty(ref _smtpPort, value);
        }

        public ICommand GenerateTemplateCommand { get; }
        public ICommand SendTestEmailCommand { get; }
        public ICommand SendToAllMembersCommand { get; }

        private bool CanExecuteGenerateTemplate(object parameter)
        {
            // Allow generation even without training text (it will just be empty in the template)
            return true;
        }

        private void ExecuteGenerateTemplate(object parameter)
        {
            try
            {
                StatusMessage = "Generating email template...";
                var template = GenerateEmailTemplate();
                EmailSubject = template.Subject;
                EmailBody = template.Body;
                StatusMessage = "✅ Email template generated successfully! Click '🔄 Load Template' to view it in the editor.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Error generating template: {ex.Message}";
                MessageBox.Show($"Error generating template: {ex.Message}\n\nStack trace:\n{ex.StackTrace}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private (string Subject, string Body) GenerateEmailTemplate()
        {
            var sb = new StringBuilder();
            var culture = _localization.CurrentCulture;

            // Get start and end of current week (Monday to Sunday)
            var startOfWeek = MailingDate.AddDays(-(int)MailingDate.DayOfWeek + (int)DayOfWeek.Monday);
            if (MailingDate.DayOfWeek == DayOfWeek.Sunday)
                startOfWeek = startOfWeek.AddDays(-7);
            var endOfWeek = startOfWeek.AddDays(6);

            // Get start and end of previous week
            var startOfPreviousWeek = startOfWeek.AddDays(-7);
            var endOfPreviousWeek = startOfWeek.AddDays(0);

            // Get all race events
            var allRaceEvents = _raceEventRepository.GetAll();

            // Get races from previous week (for results)
            var previousWeekRaces = allRaceEvents
                .Where(re => re.EventDate >= startOfPreviousWeek && re.EventDate <= endOfPreviousWeek)
                .OrderBy(re => re.EventDate)
                .ToList();

            // Get races from current week (for calendar)
            var currentWeekRaces = allRaceEvents
                .Where(re => re.EventDate >= startOfWeek && re.EventDate <= endOfWeek)
                .OrderBy(re => re.EventDate)
                .ToList();

            // Subject
            var subject = $"Entraînements de la semaine - {MailingDate.ToString("dd/MM/yyyy", culture)}";

            sb.AppendLine("<div style='font-family: Arial, sans-serif; max-width: 800px; margin: 0 auto;'>");

            // Header
            sb.AppendLine("<h1 style='color: #FF9800;'>🏃 Newsletter / Entraînements de la semaine</h1>");
            sb.AppendLine($"<p style='font-size: 14px; color: #666;'>Semaine du {startOfWeek.ToString("dd MMMM yyyy", culture)} au {endOfWeek.ToString("dd MMMM yyyy", culture)}</p>");
            sb.AppendLine("<hr style='border: 1px solid #FF9800;'/>");

            // Training of the week - with nice formatting
            sb.AppendLine("<h2 style='color: #2196F3;'>💪 Entraînements de la Semaine</h2>");
            sb.AppendLine("<div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 20px; border-radius: 10px; margin: 15px 0; box-shadow: 0 4px 6px rgba(0,0,0,0.1);'>");

            // Mardi
            sb.AppendLine("<div style='background-color: rgba(255,255,255,0.95); padding: 15px; border-radius: 8px; margin-bottom: 12px; border-left: 4px solid #FF9800;'>");
            sb.AppendLine("<div style='display: flex; align-items: center; margin-bottom: 8px;'>");
            sb.AppendLine("<span style='font-size: 24px; margin-right: 12px;'>📅</span>");
            sb.AppendLine("<strong style='font-size: 16px; color: #FF9800;'>Mardi 17h00</strong>");
            sb.AppendLine("</div>");
            sb.AppendLine("<div style='margin-left: 40px;'>");
            sb.AppendLine("<p style='margin: 5px 0; color: #555;'><strong>Lieu :</strong> Rendez-vous à la piste avec Fernand</p>");
            sb.AppendLine("<p style='margin: 5px 0; color: #555; font-style: bold;'><strong>[Programme à définir]</strong></p>");
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");

            // Jeudi
            sb.AppendLine("<div style='background-color: rgba(255,255,255,0.95); padding: 15px; border-radius: 8px; border-left: 4px solid #4CAF50;'>");
            sb.AppendLine("<div style='display: flex; align-items: center; margin-bottom: 8px;'>");
            sb.AppendLine("<span style='font-size: 24px; margin-right: 12px;'>🏃</span>");
            sb.AppendLine("<strong style='font-size: 16px; color: #4CAF50;'>Jeudi 16h30 - 17h00</strong>");
            sb.AppendLine("</div>");
            sb.AppendLine("<div style='margin-left: 40px;'>");
            sb.AppendLine("<p style='margin: 5px 0; color: #555;'><strong>Départ :</strong></p>");
            sb.AppendLine("<ul style='margin: 5px 0 5px 20px; color: #555;'>");
            sb.AppendLine("<li>16h30 au parking de la piste, ou</li>");
            sb.AppendLine("<li>17h00 à la barrière du bois de la Vecquée</li>");
            sb.AppendLine("</ul>");
            sb.AppendLine("<p style='margin: 5px 0; color: #555;'><strong>Activité :</strong> Entraînements accompagnés de 8 à 13 km dans les bois environnants</p>");
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");

            sb.AppendLine("</div>");

            // Results of the week
            sb.AppendLine("<h2 style='color: #4CAF50;'>🏆 Résultats de la Semaine Précédente</h2>");
            if (previousWeekRaces.Any())
            {
                foreach (var raceEvent in previousWeekRaces)
                {
                    sb.AppendLine($"<h3>{raceEvent.Name} - {raceEvent.EventDate.ToString("dd/MM/yyyy", culture)}</h3>");
                    
                    var races = _raceRepository.GetRacesByRaceEvent(raceEvent.Id);
                    
                    foreach (var race in races)
                    {
                        // Format distance with 1 decimal place
                        sb.AppendLine($"<h4 style='color: #FF9800;'>{race.DistanceKm.ToString("0.0", CultureInfo.InvariantCulture)} km</h4>");

                        // Get ALL classifications without any filtering (null, null means no filters applied)
                        var allClassifications = _classificationRepository.GetClassificationsByRace(race.Id, null, null)
                            .OrderBy(c => c.Position)
                            .ToList();

                        // Filter: Keep only club members + the first (winner) as reference if not a member
                        var classifications = new List<ClassificationEntity>();

                        if (allClassifications.Any())
                        {
                            // Get the winner (first position) as reference
                            var winner = allClassifications.FirstOrDefault(c => c.Position == 1);

                            // Get all club members
                            var members = allClassifications
                                .Where(c => c.IsMember)
                                .ToList();

                            // If the winner is not a member, add it as reference at the top
                            if (winner != null && !winner.IsMember)
                            {
                                classifications.Add(winner);
                            }

                            // Add all members (including the winner if they are a member)
                            classifications.AddRange(members);
                        }

                        if (classifications.Any())
                        {
                            // Localized headers
                            var hdrPosition = _localization["Position"] ?? "Position";
                            var hdrFirstName = _localization["FirstName"] ?? "First Name";
                            var hdrLastName = _localization["LastName"] ?? "Last Name";
                            var hdrRaceTime = _localization["RaceTime"] ?? "Race Time";
                            var hdrTimePerKm = _localization["TimePerKm"] ?? "Time/km";
                            var hdrSpeed = _localization["Speed"] ?? "Speed (km/h)";
                            var hdrCategory = _localization["Category"] ?? "Category";
                            var hdrPosByCategory = _localization["PosByCategory"] ?? "Pos/Cat";

                            sb.AppendLine("<table style='width: 100%; border-collapse: collapse; margin-bottom: 20px;'>");
                            sb.AppendLine("<thead>");
                            sb.AppendLine("<tr style='background-color: #FF9800; color: white;'>");
                            sb.AppendLine($"<th style='padding: 8px; text-align: left;'>{hdrPosition}</th>");
                            sb.AppendLine($"<th style='padding: 8px; text-align: left;'>{hdrFirstName}</th>");
                            sb.AppendLine($"<th style='padding: 8px; text-align: left;'>{hdrLastName}</th>");
                            sb.AppendLine($"<th style='padding: 8px; text-align: left;'>{hdrRaceTime}</th>");
                            sb.AppendLine($"<th style='padding: 8px; text-align: left;'>{hdrTimePerKm}</th>");
                            sb.AppendLine($"<th style='padding: 8px; text-align: left;'>{hdrSpeed}</th>");
                            sb.AppendLine($"<th style='padding: 8px; text-align: left;'>{hdrCategory}</th>");
                            sb.AppendLine($"<th style='padding: 8px; text-align: left;'>{hdrPosByCategory}</th>");
                            sb.AppendLine("</tr>");
                            sb.AppendLine("</thead>");
                            sb.AppendLine("<tbody>");

                            foreach (var c in classifications)
                            {
                                var rowStyle = (c.Position ?? 0) % 2 == 0 ? "background-color: #f2f2f2;" : "";

                                // Format names: FirstName as PascalCase, LastName as UPPERCASE
                                var formattedFirstName = string.IsNullOrWhiteSpace(c.MemberFirstName) 
                                    ? "-" 
                                    : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(c.MemberFirstName.ToLower());
                                var formattedLastName = string.IsNullOrWhiteSpace(c.MemberLastName) 
                                    ? "-" 
                                    : c.MemberLastName.ToUpper();

                                sb.AppendLine($"<tr style='{rowStyle}'>");
                                sb.AppendLine($"<td style='padding: 8px;'>{(c.Position.HasValue ? c.Position.ToString() : "-")}</td>");
                                sb.AppendLine($"<td style='padding: 8px;'>{formattedFirstName}</td>");
                                sb.AppendLine($"<td style='padding: 8px;'>{formattedLastName}</td>");
                                sb.AppendLine($"<td style='padding: 8px;'>{(c.RaceTime.HasValue ? c.RaceTime.Value.ToString(@"hh\:mm\:ss") : "-")}</td>");
                                sb.AppendLine($"<td style='padding: 8px;'>{(c.TimePerKm.HasValue ? c.TimePerKm.Value.ToString(@"hh\:mm\:ss") : "-")}</td>");
                                sb.AppendLine($"<td style='padding: 8px;'>{(c.Speed.HasValue ? c.Speed.Value.ToString("F2", CultureInfo.InvariantCulture) : "-")}</td>");
                                sb.AppendLine($"<td style='padding: 8px;'>{(c.AgeCategory ?? "-")}</td>");
                                sb.AppendLine($"<td style='padding: 8px;'>{(c.PositionByCategory.HasValue ? c.PositionByCategory.ToString() : "-")}</td>");
                                sb.AppendLine("</tr>");
                            }

                            sb.AppendLine("</tbody>");
                            sb.AppendLine("</table>");
                        }
                        else
                        {
                            sb.AppendLine("<p>Aucun résultat disponible pour les membres du club.</p>");
                        }
                    }
                }
            }
            else
            {
                sb.AppendLine("<p>Aucune course n'a eu lieu la semaine dernière.</p>");
            }

            // Calendar
            sb.AppendLine("<h2 style='color: #9C27B0;'>📅 Calendrier de la Semaine</h2>");
            if (currentWeekRaces.Any())
            {
                sb.AppendLine("<table style='width: 100%; border-collapse: collapse; margin-bottom: 20px;'>");
                sb.AppendLine("<thead>");
                sb.AppendLine("<tr style='background-color: #9C27B0; color: white;'>");
                sb.AppendLine("<th style='padding: 8px; text-align: left;'>Date</th>");
                sb.AppendLine("<th style='padding: 8px; text-align: left;'>Événement</th>");
                sb.AppendLine("<th style='padding: 8px; text-align: left;'>Lieu</th>");
                sb.AppendLine("<th style='padding: 8px; text-align: left;'>Distances</th>");
                sb.AppendLine("</tr>");
                sb.AppendLine("</thead>");
                sb.AppendLine("<tbody>");

                foreach (var raceEvent in currentWeekRaces)
                {
                    // Get available distances from the RaceEvent configuration (pre-configured distances)
                    var availableDistances = _raceEventRepository.GetDistancesByEvent(raceEvent.Id);

                    // Debug logging
                    System.Diagnostics.Debug.WriteLine($"[MAILING] Race Event: '{raceEvent.Name}' (ID: {raceEvent.Id})");
                    System.Diagnostics.Debug.WriteLine($"[MAILING]   Pre-configured distances: {availableDistances.Count}");

                    // If no pre-configured distances, fall back to actual race distances from past editions
                    if (!availableDistances.Any())
                    {
                        System.Diagnostics.Debug.WriteLine($"[MAILING]   No pre-configured distances, checking past races...");
                        var existingRaces = _raceRepository.GetRacesByRaceEvent(raceEvent.Id);
                        System.Diagnostics.Debug.WriteLine($"[MAILING]   Found {existingRaces.Count} past races");

                        if (existingRaces.Any())
                        {
                            // Convert RaceEntity distances to RaceEventDistanceEntity format for display
                            availableDistances = existingRaces
                                .Select(r => new NameParser.Infrastructure.Data.Models.RaceEventDistanceEntity
                                {
                                    DistanceKm = r.DistanceKm
                                })
                                .GroupBy(d => d.DistanceKm) // Remove duplicates
                                .Select(g => g.First())
                                .ToList();

                            System.Diagnostics.Debug.WriteLine($"[MAILING]   Using {availableDistances.Count} distance(s) from past races: {string.Join(", ", availableDistances.Select(d => d.DistanceKm))}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[MAILING]   No past races found - will display 'A confirmer'");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[MAILING]   Using pre-configured distances: {string.Join(", ", availableDistances.Select(d => d.DistanceKm))}");
                    }

                    // Format distances with 1 decimal place
                    var distanceStr = availableDistances.Any() 
                        ? string.Join(", ", availableDistances.OrderBy(d => d.DistanceKm).Select(d => $"{d.DistanceKm.ToString("0.0", CultureInfo.InvariantCulture)} km")) 
                        : "À confirmer";

                    System.Diagnostics.Debug.WriteLine($"[MAILING]   Final distance string: '{distanceStr}'");

                    sb.AppendLine("<tr>");
                    sb.AppendLine($"<td style='padding: 8px;'>{raceEvent.EventDate.ToString("dd/MM/yyyy", culture)}</td>");
                    sb.AppendLine($"<td style='padding: 8px;'><strong>{raceEvent.Name}</strong></td>");
                    sb.AppendLine($"<td style='padding: 8px;'>{raceEvent.Location ?? "À confirmer"}</td>");
                    sb.AppendLine($"<td style='padding: 8px;'>{distanceStr}</td>");
                    sb.AppendLine("</tr>");

                    if (!string.IsNullOrEmpty(raceEvent.WebsiteUrl))
                    {
                        sb.AppendLine("<tr>");
                        sb.AppendLine($"<td colspan='4' style='padding: 4px 8px;'><a href='{raceEvent.WebsiteUrl}'>🌐 Site Web</a></td>");
                        sb.AppendLine("</tr>");
                    }
                }

                sb.AppendLine("</tbody>");
                sb.AppendLine("</table>");
            }
            else
            {
                sb.AppendLine("<p>Aucune course prévue cette semaine.</p>");
            }

            // Footer
            sb.AppendLine("<hr style='border: 1px solid #FF9800; margin-top: 30px;'/>");
            sb.AppendLine("<p style='font-size: 12px; color: #666;'>Continuez à courir! 🏃💪</p>");
            
            sb.AppendLine("</div>");

            return (subject, sb.ToString());
        }

        private bool CanExecuteSendTestEmail(object parameter)
        {
            return !string.IsNullOrWhiteSpace(TestEmailAddress) && 
                   !string.IsNullOrWhiteSpace(EmailSubject) && 
                   !string.IsNullOrWhiteSpace(EmailBody) &&
                   !string.IsNullOrWhiteSpace(GmailAddress) &&
                   !string.IsNullOrWhiteSpace(GmailAppPassword);
        }

        private async void ExecuteSendTestEmail(object parameter)
        {
            try
            {
                IsSending = true;
                StatusMessage = "Sending test email...";

                await SendEmailAsync(TestEmailAddress, EmailSubject, EmailBody);

                StatusMessage = $"Test email sent successfully to {TestEmailAddress}!";
                MessageBox.Show($"Test email sent successfully to {TestEmailAddress}!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error sending test email: {ex.Message}";
                MessageBox.Show($"Error sending test email:\n\n{ex.Message}\n\nMake sure you're using a Gmail App Password (not your regular password).", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsSending = false;
            }
        }

        private bool CanExecuteSendToAllMembers(object parameter)
        {
            return !string.IsNullOrWhiteSpace(EmailSubject) && 
                   !string.IsNullOrWhiteSpace(EmailBody) &&
                   !string.IsNullOrWhiteSpace(GmailAddress) &&
                   !string.IsNullOrWhiteSpace(GmailAppPassword);
        }

        private async void ExecuteSendToAllMembers(object parameter)
        {
            try
            {
                var memberJsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Members.json");

                if (!File.Exists(memberJsonPath))
                {
                    MessageBox.Show("Members.json file not found. Please make sure the file exists in the application directory.", 
                        "File Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var jsonContent = File.ReadAllText(memberJsonPath);
                var members = System.Text.Json.JsonSerializer.Deserialize<List<MemberDto>>(jsonContent);

                if (members == null || !members.Any())
                {
                    MessageBox.Show("No members found in Members.json.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Clean and categorize email addresses
                var memberEmails = members
                    .Where(m => !string.IsNullOrWhiteSpace(m.Email))
                    .Select(m => new
                    {
                        Email = CleanEmailAddress(m.Email),
                        IsChallenger = m.IsChallenger ?? false
                    })
                    .Where(m => !string.IsNullOrWhiteSpace(m.Email)) // Filter out invalid emails after cleaning
                    .GroupBy(m => m.Email, StringComparer.OrdinalIgnoreCase) // Group by email to remove duplicates
                    .Select(g => g.First()) // Take first of each duplicate group
                    .ToList();

                if (!memberEmails.Any())
                {
                    MessageBox.Show("No members with valid email addresses found in Members.json.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var emailList = string.Join("\n", memberEmails.Take(10).Select(m => $"{m.Email} ({(m.IsChallenger ? "Challenger" : "Member")}"));
                var result = MessageBox.Show(
                    $"This will send the email to {memberEmails.Count} member(s) from Members.json.\n\nRecipients:\n{emailList}" +
                    (memberEmails.Count > 10 ? $"\n... and {memberEmails.Count - 10} more" : "") +
                    "\n\nAre you sure you want to continue?",
                    "Confirm Send",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;

                IsSending = true;
                int successCount = 0;
                int failCount = 0;
                var errors = new List<string>();

                foreach (var emailInfo in memberEmails)
                {
                    try
                    {
                        StatusMessage = $"Sending to {emailInfo.Email}... ({successCount + failCount + 1}/{memberEmails.Count})";
                        await SendEmailAsync(emailInfo.Email, EmailSubject, EmailBody, emailInfo.IsChallenger);
                        successCount++;

                        // Delay to avoid rate limiting
                        await System.Threading.Tasks.Task.Delay(5000);
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        errors.Add($"{emailInfo.Email}: {ex.Message}");
                    }
                }

                StatusMessage = $"Sent {successCount} email(s), {failCount} failed.";

                var message = $"Email sending complete!\n\nSuccessful: {successCount}\nFailed: {failCount}";
                if (errors.Any())
                {
                    message += $"\n\nErrors:\n{string.Join("\n", errors.Take(5))}";
                    if (errors.Count > 5) message += $"\n... and {errors.Count - 5} more errors";
                }

                MessageBox.Show(message, "Send Complete", MessageBoxButton.OK, 
                    failCount > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error sending emails: {ex.Message}";
                MessageBox.Show($"Error sending emails: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsSending = false;
            }
        }

        /// <summary>
        /// Cleans an email address by removing trailing/leading whitespace, commas, semicolons, and other invalid characters.
        /// </summary>
        private string CleanEmailAddress(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            // Remove whitespace
            email = email.Trim();

            // Remove trailing commas, semicolons, and other common delimiters
            email = email.TrimEnd(',', ';', ':', ' ', '\t', '\r', '\n');
            email = email.TrimStart(',', ';', ':', ' ', '\t', '\r', '\n');

            // Basic email validation - must contain @ and a domain
            if (!email.Contains('@') || email.Length < 5)
                return null;

            // Remove any spaces within the email (invalid)
            email = email.Replace(" ", "");

            return email;
        }

        private class MemberDto
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            public bool? IsMember { get; set; }
            public bool? IsChallenger { get; set; }
        }

        private async System.Threading.Tasks.Task SendEmailAsync(string toEmail, string subject, string body, bool isChallenger = false)
        {
            var message = new MimeMessage();

            // Use different sender name based on recipient type
            var senderName = isChallenger ? "Challenge Lucien Campeggio" : "Ser Athl (Hors-Stade)";
            message.From.Add(new MailboxAddress(senderName, _gmailAddress));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = body
            };
            message.Body = bodyBuilder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(_smtpServer, _smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_gmailAddress, _gmailAppPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }

        private void LoadGmailSettings()
        {
            try
            {
                _gmailAddress = _configuration["Gmail:Address"] ?? "";
                _gmailAppPassword = _configuration["Gmail:AppPassword"] ?? "";
                _smtpServer = _configuration["Gmail:SmtpServer"] ?? "smtp.gmail.com";

                if (int.TryParse(_configuration["Gmail:SmtpPort"], out int port))
                {
                    _smtpPort = port;
                }
                else
                {
                    _smtpPort = 587;
                }

                if (!string.IsNullOrWhiteSpace(_gmailAddress))
                {
                    StatusMessage = $"Ready to send emails from {_gmailAddress}";
                }
                else
                {
                    StatusMessage = "⚠️ Gmail settings not configured. Please edit appsettings.json in the application directory.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading settings: {ex.Message}";
            }
        }
    }
}
