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
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

#if MAILKIT_INSTALLED
using MailKit.Net.Smtp;
using MimeKit;
#endif

namespace NameParser.UI.ViewModels
{
    public class ChallengeMailingViewModel : ViewModelBase
    {
        private readonly ChallengeRepository _challengeRepository;
        private readonly RaceEventRepository _raceEventRepository;
        private readonly RaceRepository _raceRepository;
        private readonly ClassificationRepository _classificationRepository;
        private readonly JsonMemberRepository _memberRepository;
        private readonly EmailLogRepository _emailLogRepository;
        private readonly IConfiguration _configuration;
        private readonly LocalizationService _localization;

        private ChallengeEntity _selectedChallenge;
        private string _emailSubject;
        private string _emailBody;
        private string _testEmailAddress;
        private string _statusMessage;
        private bool _isSending;
        private EmailRecipientInfo _selectedRecipient;

        // Gmail Configuration (read-only from appsettings.json)
        private string _gmailAddress;
        private string _gmailAppPassword;
        private string _smtpServer = "smtp.gmail.com";
        private int _smtpPort = 587;

        public ChallengeMailingViewModel()
        {
            _challengeRepository = new ChallengeRepository();
            _raceEventRepository = new RaceEventRepository();
            _raceRepository = new RaceRepository();
            _classificationRepository = new ClassificationRepository();
            _memberRepository = new JsonMemberRepository();
            _emailLogRepository = new EmailLogRepository();
            _localization = LocalizationService.Instance;

            // Load configuration from appsettings.json
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            Challenges = new ObservableCollection<ChallengeEntity>();
            Recipients = new ObservableCollection<EmailRecipientInfo>();

            LoadChallengesCommand = new RelayCommand(ExecuteLoadChallenges);
            GenerateTemplateCommand = new RelayCommand(ExecuteGenerateTemplate, CanExecuteGenerateTemplate);
            SendTestEmailCommand = new RelayCommand(ExecuteSendTestEmail, CanExecuteSendTestEmail);
            SendToAllChallengersCommand = new RelayCommand(ExecuteSendToAllChallengers, CanExecuteSendToAllChallengers);
            LoadRecipientsCommand = new RelayCommand(ExecuteLoadRecipients, CanExecuteLoadRecipients);
            ResendToSelectedCommand = new RelayCommand(ExecuteResendToSelected, CanExecuteResendToSelected);

            LoadChallenges();
            LoadGmailSettings();
        }

        public ObservableCollection<ChallengeEntity> Challenges { get; }
        public ObservableCollection<EmailRecipientInfo> Recipients { get; }

        public ChallengeEntity SelectedChallenge
        {
            get => _selectedChallenge;
            set
            {
                if (SetProperty(ref _selectedChallenge, value))
                {
                    ((RelayCommand)GenerateTemplateCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)SendTestEmailCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)SendToAllChallengersCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)LoadRecipientsCommand).RaiseCanExecuteChanged();
                    ExecuteLoadRecipients(null);
                }
            }
        }

        public EmailRecipientInfo SelectedRecipient
        {
            get => _selectedRecipient;
            set
            {
                if (SetProperty(ref _selectedRecipient, value))
                {
                    ((RelayCommand)ResendToSelectedCommand).RaiseCanExecuteChanged();
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

        public ICommand LoadChallengesCommand { get; }
        public ICommand GenerateTemplateCommand { get; }
        public ICommand SendTestEmailCommand { get; }
        public ICommand SendToAllChallengersCommand { get; }
        public ICommand SaveGmailSettingsCommand { get; }
        public ICommand LoadRecipientsCommand { get; }
        public ICommand ResendToSelectedCommand { get; }

        private void ExecuteLoadChallenges(object parameter)
        {
            LoadChallenges();
        }

        private void LoadChallenges()
        {
            try
            {
                var challenges = _challengeRepository.GetAll();
                Challenges.Clear();
                foreach (var challenge in challenges.OrderByDescending(c => c.Year).ThenBy(c => c.Name))
                {
                    Challenges.Add(challenge);
                }
                StatusMessage = $"Loaded {Challenges.Count} challenge(s).";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading challenges: {ex.Message}";
            }
        }

        private bool CanExecuteLoadRecipients(object parameter)
        {
            return SelectedChallenge != null;
        }

        private void ExecuteLoadRecipients(object parameter)
        {
            try
            {
                Recipients.Clear();

                // Read challengers from Challenge.json
                var challengeJsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Challenge.json");

                if (!File.Exists(challengeJsonPath))
                {
                    StatusMessage = "Challenge.json file not found.";
                    return;
                }

                var jsonContent = File.ReadAllText(challengeJsonPath);
                var challengers = System.Text.Json.JsonSerializer.Deserialize<List<ChallengerRegistration>>(jsonContent);

                if (challengers == null || !challengers.Any())
                {
                    StatusMessage = "No challengers found in Challenge.json.";
                    return;
                }

                // Get unique emails
                var challengerEmails = challengers
                    .Where(c => !string.IsNullOrWhiteSpace(c.Email))
                    .GroupBy(c => c.Email.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(g => new
                    {
                        Email = g.Key,
                        Name = $"{g.First().FirstName} {g.First().LastName}"
                    })
                    .ToList();

                // Get last email log for each recipient
                foreach (var challenger in challengerEmails)
                {
                    var lastLog = _emailLogRepository.GetLastEmailLog(challenger.Email, "Challenge", SelectedChallenge.Id);

                    var recipientInfo = new EmailRecipientInfo
                    {
                        Email = challenger.Email,
                        Name = challenger.Name,
                        Status = lastLog == null ? "Pending" : (lastLog.IsSuccess ? "Sent" : "Failed"),
                        LastSentDate = lastLog?.SentDate,
                        LastError = lastLog?.ErrorMessage
                    };

                    Recipients.Add(recipientInfo);
                }

                StatusMessage = $"Loaded {Recipients.Count} recipient(s). " +
                               $"Sent: {Recipients.Count(r => r.Status == "Sent")}, " +
                               $"Failed: {Recipients.Count(r => r.Status == "Failed")}, " +
                               $"Pending: {Recipients.Count(r => r.Status == "Pending")}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading recipients: {ex.Message}";
            }
        }

        private bool CanExecuteResendToSelected(object parameter)
        {
            return SelectedRecipient != null && 
                   !string.IsNullOrWhiteSpace(EmailSubject) && 
                   !string.IsNullOrWhiteSpace(EmailBody) &&
                   !string.IsNullOrWhiteSpace(GmailAddress) &&
                   !string.IsNullOrWhiteSpace(GmailAppPassword) &&
                   !IsSending;
        }

        private async void ExecuteResendToSelected(object parameter)
        {
            if (SelectedRecipient == null) return;

            var result = MessageBox.Show(
                $"Resend email to {SelectedRecipient.Name} ({SelectedRecipient.Email})?",
                "Confirm Resend",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            string pdfPath = null;
            try
            {
                IsSending = true;
                SelectedRecipient.Status = "Sending";
                SelectedRecipient.IsSending = true;
                StatusMessage = $"Generating PDF and sending to {SelectedRecipient.Email}...";

                // Generate PDF
                pdfPath = GenerateDetailedClassificationPdf();

                // Send email
                await SendEmailAsync(SelectedRecipient.Email, EmailSubject, EmailBody, pdfPath, isTest: false);

                SelectedRecipient.Status = "Sent";
                SelectedRecipient.LastSentDate = DateTime.Now;
                SelectedRecipient.LastError = null;
                StatusMessage = $"✅ Email successfully sent to {SelectedRecipient.Email}!";
                MessageBox.Show($"Email successfully sent to {SelectedRecipient.Name}!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                SelectedRecipient.Status = "Failed";
                SelectedRecipient.LastError = ex.Message;
                StatusMessage = $"❌ Error sending to {SelectedRecipient.Email}: {ex.Message}";
                MessageBox.Show($"Error sending email:\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SelectedRecipient.IsSending = false;

                // Clean up temp PDF file
                if (!string.IsNullOrEmpty(pdfPath) && File.Exists(pdfPath))
                {
                    try
                    {
                        File.Delete(pdfPath);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
                IsSending = false;
            }
        }

        private bool CanExecuteGenerateTemplate(object parameter)
        {
            return SelectedChallenge != null;
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
                MessageBox.Show($"Error generating template: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private (string Subject, string Body) GenerateEmailTemplate()
        {
            var sb = new StringBuilder();
            var today = DateTime.Today;
            var isFrench = _localization.CurrentCulture.TwoLetterISOLanguageName == "fr";

            // Get all race events for this challenge using proper repository method
            var challengeRaceEvents = _challengeRepository.GetRaceEventsForChallenge(SelectedChallenge.Id)
                .OrderBy(re => re.EventDate)
                .ToList();

            // Find next race (first race after today)
            var nextRace = challengeRaceEvents.FirstOrDefault(re => re.EventDate >= today);

            // Find previous race (last race before today)
            var previousRace = challengeRaceEvents.LastOrDefault(re => re.EventDate < today);

            // Find 3 races after next race
            var upcomingRaces = challengeRaceEvents
                .Where(re => nextRace != null && re.EventDate > nextRace.EventDate)
                .Take(3)
                .ToList();

            // Subject
            var subject = $"{SelectedChallenge.Name} - {(isFrench ? "Mise à jour" : "Update")} {today.ToString(isFrench ? "dd/MM/yyyy" : "MM/dd/yyyy")}";

            // Header
            sb.AppendLine($"<h1 style='color: #FF9800;'>🏃 {SelectedChallenge.Name}</h1>");
            sb.AppendLine($"<p style='font-size: 14px; color: #666;'>{(isFrench ? "Mise à jour du Challenge" : "Challenge Update")} - {today.ToString(isFrench ? "dd MMMM yyyy" : "MMMM dd, yyyy", isFrench ? CultureInfo.GetCultureInfo("fr-FR") : CultureInfo.InvariantCulture)}</p>");
            sb.AppendLine("<hr style='border: 1px solid #FF9800;'/>");

            // Next Race Section
            if (nextRace != null)
            {
                sb.AppendLine($"<h2 style='color: #2196F3;'>📅 {(isFrench ? "Prochaine Course" : "Next Race")}</h2>");
                sb.AppendLine("<div style='background-color: #E3F2FD; padding: 15px; border-radius: 5px; margin: 10px 0;'>");
                sb.AppendLine($"<h3 style='margin: 0;'>{nextRace.Name}</h3>");
                sb.AppendLine($"<p><strong>📍 {(isFrench ? "Date" : "Date")}:</strong> {nextRace.EventDate.ToString(isFrench ? "dddd dd MMMM yyyy" : "dddd, MMMM dd yyyy", isFrench ? CultureInfo.GetCultureInfo("fr-FR") : CultureInfo.InvariantCulture)}</p>");
                sb.AppendLine($"<p><strong>📍 {(isFrench ? "Lieu" : "Location")}:</strong> {nextRace.Location ?? (isFrench ? "À confirmer" : "TBA")}</p>");

                // Get distances for next race - Check RaceEventDistances first, then fall back to past races
                var nextRaceDistances = _raceEventRepository.GetDistancesByEvent(nextRace.Id);

                if (!nextRaceDistances.Any())
                {
                    var nextEventRaces = _raceRepository.GetRacesByRaceEvent(nextRace.Id);
                    if (nextEventRaces.Any())
                    {
                        // Convert to RaceEventDistanceEntity format
                        nextRaceDistances = nextEventRaces
                            .Select(r => new RaceEventDistanceEntity
                            {
                                DistanceKm = r.DistanceKm
                            })
                            .GroupBy(d => d.DistanceKm)
                            .Select(g => g.First())
                            .ToList();
                    }
                }

                if (nextRaceDistances.Any())
                {
                    var distances = nextRaceDistances.Select(d => d.DistanceKm).Distinct().OrderBy(d => d);
                    sb.AppendLine($"<p><strong>🏃 {(isFrench ? "Distances" : "Distances")}:</strong> {string.Join(", ", distances.Select(d => $"{d.ToString("0.0", CultureInfo.InvariantCulture)} km"))}</p>");
                }

                if (!string.IsNullOrEmpty(nextRace.WebsiteUrl))
                {
                    sb.AppendLine($"<p><strong>🌐 {(isFrench ? "Site Web" : "Website")}:</strong> <a href='{nextRace.WebsiteUrl}'>{nextRace.WebsiteUrl}</a></p>");
                }

                if (!string.IsNullOrEmpty(nextRace.Description))
                {
                    sb.AppendLine($"<p>{nextRace.Description}</p>");
                }

                sb.AppendLine("</div>");
            }
            else
            {
                sb.AppendLine($"<h2 style='color: #2196F3;'>📅 {(isFrench ? "Prochaine Course" : "Next Race")}</h2>");
                sb.AppendLine($"<p>{(isFrench ? "Aucune course à venir prévue pour le moment." : "No upcoming races scheduled at this time.")}</p>");
            }

            // Upcoming Races Summary
            if (upcomingRaces.Any())
            {
                sb.AppendLine($"<h2 style='color: #2196F3;'>📆 {(isFrench ? "À Venir" : "Coming Soon")}</h2>");
                sb.AppendLine("<ul style='list-style-type: none; padding: 0;'>");

                foreach (var race in upcomingRaces)
                {
                    // Get available distances from RaceEventDistances first (pre-configured)
                    var availableDistances = _raceEventRepository.GetDistancesByEvent(race.Id);

                    // If no pre-configured distances, fall back to actual race distances from past editions
                    if (!availableDistances.Any())
                    {
                        var existingRaces = _raceRepository.GetRacesByRaceEvent(race.Id);
                        if (existingRaces.Any())
                        {
                            // Convert RaceEntity distances to RaceEventDistanceEntity format
                            availableDistances = existingRaces
                                .Select(r => new RaceEventDistanceEntity
                                {
                                    DistanceKm = r.DistanceKm
                                })
                                .GroupBy(d => d.DistanceKm) // Remove duplicates
                                .Select(g => g.First())
                                .ToList();
                        }
                    }

                    // Format distances with proper spacing and decimal
                    var distanceStr = availableDistances.Any() 
                        ? string.Join(", ", availableDistances.OrderBy(d => d.DistanceKm).Select(d => $"{d.DistanceKm.ToString("0.0", CultureInfo.InvariantCulture)} km"))
                        : (isFrench ? "À confirmer" : "TBA");

                    sb.AppendLine($"<li style='padding: 5px 0;'>• <strong>{race.Name}</strong> - {race.EventDate:dd/MM/yyyy} - {distanceStr}</li>");
                }

                sb.AppendLine("</ul>");
            }

            // Previous Race Results - TOUS LES CHALLENGERS
            if (previousRace != null)
            {
                sb.AppendLine($"<h2 style='color: #4CAF50;'>🏆 {(isFrench ? "Derniers Résultats" : "Latest Results")}</h2>");
                sb.AppendLine($"<h3>{previousRace.Name} - {previousRace.EventDate:dd/MM/yyyy}</h3>");

                var previousRaces = _raceRepository.GetRacesByRaceEvent(previousRace.Id);

                foreach (var race in previousRaces)
                {
                    sb.AppendLine($"<h4 style='color: #FF9800;'>{race.DistanceKm} km</h4>");

                    var classifications = _classificationRepository.GetClassificationsByRace(race.Id, null, true) // Only challengers
                        .OrderBy(c => c.Position)
                        .ToList(); // TOUS les challengers, pas seulement 10

                    if (classifications.Any())
                    {
                        sb.AppendLine("<table style='width: 100%; border-collapse: collapse; margin-bottom: 20px;'>");
                        sb.AppendLine("<thead>");
                        sb.AppendLine("<tr style='background-color: #FF9800; color: white;'>");
                        sb.AppendLine($"<th style='padding: 8px; text-align: left;'>{(isFrench ? "Pos" : "Pos")}</th>");
                        sb.AppendLine($"<th style='padding: 8px; text-align: left;'>{(isFrench ? "Nom" : "Name")}</th>");
                        sb.AppendLine($"<th style='padding: 8px; text-align: left;'>{(isFrench ? "Temps" : "Time")}</th>");
                        sb.AppendLine($"<th style='padding: 8px; text-align: left;'>{(isFrench ? "Points" : "Points")}</th>");
                        sb.AppendLine("</tr>");
                        sb.AppendLine("</thead>");
                        sb.AppendLine("<tbody>");

                        foreach (var c in classifications)
                        {
                            var rowStyle = c.Position % 2 == 0 ? "background-color: #f2f2f2;" : "";
                            sb.AppendLine($"<tr style='{rowStyle}'>");
                            sb.AppendLine($"<td style='padding: 8px;'>{c.Position}</td>");
                            sb.AppendLine($"<td style='padding: 8px;'>{c.MemberFirstName} {c.MemberLastName.ToUpper()}</td>");
                            sb.AppendLine($"<td style='padding: 8px;'>{(c.RaceTime.HasValue ? c.RaceTime.Value.ToString(@"hh\:mm\:ss") : "-")}</td>");
                            sb.AppendLine($"<td style='padding: 8px;'><strong>{c.Points}</strong></td>");
                            sb.AppendLine("</tr>");
                        }

                        sb.AppendLine("</tbody>");
                        sb.AppendLine("</table>");
                    }
                }
            }

            // Current Challenge Standings - AFFICHER TOUS LES CHALLENGERS
            sb.AppendLine($"<h2 style='color: #FF9800;'>🏆 {(isFrench ? "Classement Actuel du Challenge" : "Current Challenge Standings")}</h2>");

            var challengerClassifications = _classificationRepository.GetChallengerClassificationByChallenge(SelectedChallenge.Id)
                .OrderBy(c => c.RankByPoints)
                .ToList(); // TOUS les challengers, pas seulement les 10 premiers

            if (challengerClassifications.Any())
            {
                sb.AppendLine("<table style='width: 100%; border-collapse: collapse;'>");
                sb.AppendLine("<thead>");
                sb.AppendLine("<tr style='background-color: #FF9800; color: white;'>");
                sb.AppendLine($"<th style='padding: 8px; text-align: left;'>{(isFrench ? "Rang" : "Rank")}</th>");
                sb.AppendLine($"<th style='padding: 8px; text-align: left;'>{(isFrench ? "Nom" : "Name")}</th>");
                sb.AppendLine($"<th style='padding: 8px; text-align: left;'>{(isFrench ? "Points" : "Points")}</th>");
                sb.AppendLine($"<th style='padding: 8px; text-align: left;'>{(isFrench ? "Courses" : "Races")}</th>");
                sb.AppendLine($"<th style='padding: 8px; text-align: left;'>{(isFrench ? "KMs" : "KMs")}</th>");
                sb.AppendLine("</tr>");
                sb.AppendLine("</thead>");
                sb.AppendLine("<tbody>");

                foreach (var c in challengerClassifications)
                {
                    var rowStyle = c.RankByPoints % 2 == 0 ? "background-color: #f2f2f2;" : "";
                    var medal = c.RankByPoints == 1 ? "🥇" : c.RankByPoints == 2 ? "🥈" : c.RankByPoints == 3 ? "🥉" : "";

                    sb.AppendLine($"<tr style='{rowStyle}'>");
                    sb.AppendLine($"<td style='padding: 8px;'>{medal} #{c.RankByPoints}</td>");
                    sb.AppendLine($"<td style='padding: 8px;'><strong>{c.ChallengerFirstName} {c.ChallengerLastName.ToUpper()}</strong></td>");
                    sb.AppendLine($"<td style='padding: 8px;'><strong>{c.TotalPoints}</strong></td>");
                    sb.AppendLine($"<td style='padding: 8px;'>{c.RaceCount}</td>");
                    sb.AppendLine($"<td style='padding: 8px;'>{c.TotalKilometers}</td>");
                    sb.AppendLine("</tr>");
                }

                sb.AppendLine("</tbody>");
                sb.AppendLine("</table>");
            }

            // Link to detailed classification
            sb.AppendLine("<div style='background-color: #E3F2FD; padding: 15px; border-radius: 5px; margin: 20px 0;'>");
            sb.AppendLine($"<p style='margin: 0; font-size: 14px;'>");
            sb.AppendLine($"<strong>📎 {(isFrench ? "Classement Détaillé" : "Detailed Rankings")}</strong><br/>");
            sb.AppendLine($"{(isFrench ? "Le classement complet avec le détail course par course de chaque challenger est disponible en pièce jointe (PDF)." : "The complete rankings with race-by-race details for each challenger is available as an attachment (PDF).")}");
            sb.AppendLine("</p>");
            sb.AppendLine("</div>");

            // Footer
            sb.AppendLine("<hr style='border: 1px solid #FF9800; margin-top: 30px;'/>");
            sb.AppendLine($"<p style='font-size: 12px; color: #666;'>{(isFrench ? "Bravo à tous ! À bientôt à la prochaine course ! 🏃💪" : "Keep up the great work! See you at the next race! 🏃💪")}</p>");

            return (subject, sb.ToString());
        }

        private bool CanExecuteSendTestEmail(object parameter)
        {
            return SelectedChallenge != null && 
                   !string.IsNullOrWhiteSpace(TestEmailAddress) && 
                   !string.IsNullOrWhiteSpace(EmailSubject) && 
                   !string.IsNullOrWhiteSpace(EmailBody) &&
                   !string.IsNullOrWhiteSpace(GmailAddress) &&
                   !string.IsNullOrWhiteSpace(GmailAppPassword);
        }

        private async void ExecuteSendTestEmail(object parameter)
        {
            string pdfPath = null;
            try
            {
                IsSending = true;
                StatusMessage = "Generating PDF attachment...";

                // Generate PDF
                pdfPath = GenerateDetailedClassificationPdf();

                StatusMessage = "Sending test email with PDF attachment...";

                await SendEmailAsync(TestEmailAddress, EmailSubject, EmailBody, pdfPath, isTest: true);

                StatusMessage = $"Test email sent successfully to {TestEmailAddress}!";
                MessageBox.Show($"Test email sent successfully to {TestEmailAddress} with PDF attachment!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error sending test email: {ex.Message}";
                MessageBox.Show($"Error sending test email:\n\n{ex.Message}\n\nMake sure you're using a Gmail App Password (not your regular password).", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Clean up temp PDF file
                if (!string.IsNullOrEmpty(pdfPath) && File.Exists(pdfPath))
                {
                    try
                    {
                        File.Delete(pdfPath);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
                IsSending = false;
            }
        }

        private bool CanExecuteSendToAllChallengers(object parameter)
        {
            return SelectedChallenge != null && 
                   !string.IsNullOrWhiteSpace(EmailSubject) && 
                   !string.IsNullOrWhiteSpace(EmailBody) &&
                   !string.IsNullOrWhiteSpace(GmailAddress) &&
                   !string.IsNullOrWhiteSpace(GmailAppPassword);
        }

        private async void ExecuteSendToAllChallengers(object parameter)
        {
            string pdfPath = null;
            try
            {
                // Read all challengers directly from Challenge.json
                var challengeJsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Challenge.json");

                if (!File.Exists(challengeJsonPath))
                {
                    MessageBox.Show("Challenge.json file not found. Please make sure the file exists in the application directory.", 
                        "File Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Read and parse Challenge.json
                var jsonContent = File.ReadAllText(challengeJsonPath);
                var challengers = System.Text.Json.JsonSerializer.Deserialize<List<ChallengerRegistration>>(jsonContent);

                if (challengers == null || !challengers.Any())
                {
                    MessageBox.Show("No challengers found in Challenge.json.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Extract unique emails (avoid duplicates)
                var challengerEmails = challengers
                    .Where(c => !string.IsNullOrWhiteSpace(c.Email))
                    .Select(c => c.Email.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (!challengerEmails.Any())
                {
                    MessageBox.Show("No challengers with email addresses found in Challenge.json.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var result = MessageBox.Show(
                    $"This will send the email with PDF attachment to {challengerEmails.Count} challenger(s) from Challenge.json.\n\nRecipients:\n{string.Join("\n", challengerEmails.Take(10))}" +
                    (challengerEmails.Count > 10 ? $"\n... and {challengerEmails.Count - 10} more" : "") +
                    "\n\nAre you sure you want to continue?",
                    "Confirm Send",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;

                IsSending = true;
                StatusMessage = "Generating PDF attachment...";

                // Generate PDF once for all emails
                pdfPath = GenerateDetailedClassificationPdf();

                int successCount = 0;
                int failCount = 0;
                var errors = new List<string>();

                foreach (var email in challengerEmails)
                {
                    try
                    {
                        StatusMessage = $"Sending to {email}... ({successCount + failCount + 1}/{challengerEmails.Count})";
                        await SendEmailAsync(email, EmailSubject, EmailBody, pdfPath, isTest: false);
                        successCount++;

                        // Small delay to avoid rate limiting
                        await System.Threading.Tasks.Task.Delay(5000);
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        errors.Add($"{email}: {ex.Message}");
                    }
                }

                // Refresh the recipients list to show updated status
                ExecuteLoadRecipients(null);

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
                // Clean up temp PDF file
                if (!string.IsNullOrEmpty(pdfPath) && File.Exists(pdfPath))
                {
                    try
                    {
                        File.Delete(pdfPath);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
                IsSending = false;
            }
        }

        // Helper class to deserialize Challenge.json
        private class ChallengerRegistration
        {
            public string Timestamp { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            public string Téléphone { get; set; }
            public string Team { get; set; }
        }

        private async System.Threading.Tasks.Task SendEmailAsync(string toEmail, string subject, string body, string attachmentPath = null, bool isTest = false)
        {
            try
            {
//#if MAILKIT_INSTALLED
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Challenge Lucien Campeggio", _gmailAddress));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = body
                };

                // Add attachment if provided
                if (!string.IsNullOrEmpty(attachmentPath) && File.Exists(attachmentPath))
                {
                    bodyBuilder.Attachments.Add(attachmentPath);
                }

                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(_smtpServer, _smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(_gmailAddress, _gmailAppPassword);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                // Log successful email
                _emailLogRepository.LogEmail(
                    emailType: "Challenge",
                    challengeId: SelectedChallenge?.Id,
                    recipientEmail: toEmail,
                    recipientName: null,
                    subject: subject,
                    isSuccess: true,
                    errorMessage: null,
                    isTest: isTest
                );
//#else
            //await System.Threading.Tasks.Task.CompletedTask;
            //throw new NotImplementedException(
            //    "MailKit package is not installed.\n\n" +
            //    "To enable email functionality, install MailKit:\n" +
            //    "Install-Package MailKit -ProjectName NameParser.UI\n\n" +
            //    "See CHALLENGE_MAILING_INSTALLATION_GUIDE.md for complete setup instructions.");
//#endif
            }
            catch (Exception ex)
            {
                // Log failed email
                _emailLogRepository.LogEmail(
                    emailType: "Challenge",
                    challengeId: SelectedChallenge?.Id,
                    recipientEmail: toEmail,
                    recipientName: null,
                    subject: subject,
                    isSuccess: false,
                    errorMessage: ex.Message,
                    isTest: isTest
                );
                throw;
            }
        }

        private string GenerateDetailedClassificationPdf()
        {
            try
            {
                if (SelectedChallenge == null)
                {
                    throw new InvalidOperationException("No challenge selected");
                }

                var challengerClassifications = _classificationRepository.GetChallengerClassificationByChallenge(SelectedChallenge.Id)
                    .OrderBy(c => c.RankByPoints)
                    .ToList();

                // Create temp directory if it doesn't exist
                var tempDir = Path.Combine(Path.GetTempPath(), "ChallengeMailingPdfs");
                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                }

                var fileName = $"Classement_{SelectedChallenge.Name.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                var filePath = Path.Combine(tempDir, fileName);

                QuestPDF.Settings.License = LicenseType.Community;

                QuestPDF.Fluent.Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(9));

                        page.Header()
                            .Column(column =>
                            {
                                column.Item().Text($"🏆 {SelectedChallenge.Name} - Classement Détaillé")
                                    .FontSize(18)
                                    .Bold()
                                    .FontColor(Colors.Orange.Darken1);

                                column.Item().PaddingTop(5).Text($"Année: {SelectedChallenge.Year}")
                                    .FontSize(10);

                                column.Item().Text($"Généré le: {DateTime.Now:dd/MM/yyyy HH:mm}")
                                    .FontSize(8)
                                    .FontColor(Colors.Grey.Darken1);
                            });

                        page.Content()
                            .PaddingTop(0.5f, Unit.Centimetre)
                            .Column(column =>
                            {
                                foreach (var challenger in challengerClassifications)
                                {
                                    column.Item().PageBreak();

                                    // Challenger header
                                    column.Item()
                                        .PaddingBottom(10)
                                        .Row(row =>
                                        {
                                            row.RelativeItem().Column(col =>
                                            {
                                                col.Item().Text($"#{challenger.RankByPoints} - {challenger.ChallengerFirstName} {challenger.ChallengerLastName.ToUpper()}")
                                                    .FontSize(14)
                                                    .Bold()
                                                    .FontColor(Colors.Orange.Darken1);

                                                col.Item().PaddingTop(5).Text(text =>
                                                {
                                                    text.Span("Points: ").Bold();
                                                    text.Span($"{challenger.TotalPoints}").FontColor(Colors.Orange.Darken1).Bold();
                                                    text.Span("  |  ");
                                                    text.Span("Courses: ").Bold();
                                                    text.Span($"{challenger.RaceCount}");
                                                    text.Span("  |  ");
                                                    text.Span("Total KMs: ").Bold();
                                                    text.Span($"{challenger.TotalKilometers} km");
                                                });

                                                if (!string.IsNullOrEmpty(challenger.Team))
                                                {
                                                    col.Item().Text($"Équipe: {challenger.Team}")
                                                        .Italic()
                                                        .FontColor(Colors.Grey.Darken1);
                                                }
                                            });
                                        });

                                    // Race details table
                                    column.Item().Table(table =>
                                    {
                                        // Define columns
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.ConstantColumn(30);  // Race #
                                            columns.RelativeColumn(2);   // Race Name
                                            columns.ConstantColumn(50);  // Distance
                                            columns.ConstantColumn(40);  // Position
                                            columns.ConstantColumn(40);  // Points
                                            columns.ConstantColumn(40);  // Bonus
                                            columns.ConstantColumn(45);  // In Best 7
                                        });

                                        // Header
                                        table.Header(header =>
                                        {
                                            header.Cell().Element(HeaderCellStyle).Text("#").Bold().FontSize(8);
                                            header.Cell().Element(HeaderCellStyle).Text("Course").Bold().FontSize(8);
                                            header.Cell().Element(HeaderCellStyle).Text("Dist.").Bold().FontSize(8);
                                            header.Cell().Element(HeaderCellStyle).Text("Pos.").Bold().FontSize(8);
                                            header.Cell().Element(HeaderCellStyle).Text("Pts").Bold().FontSize(8);
                                            header.Cell().Element(HeaderCellStyle).Text("Bonus").Bold().FontSize(8);
                                            header.Cell().Element(HeaderCellStyle).Text("Best 7").Bold().FontSize(8);

                                            static IContainer HeaderCellStyle(IContainer container)
                                            {
                                                return container
                                                    .Background(Colors.Blue.Lighten3)
                                                    .Padding(3)
                                                    .BorderBottom(1)
                                                    .BorderColor(Colors.Blue.Darken1);
                                            }
                                        });

                                        // Data rows
                                        foreach (var raceDetail in challenger.RaceDetails)
                                        {
                                            var bgColor = raceDetail.IsInBest7 ? Colors.Green.Lighten4 : Colors.White;

                                            table.Cell().Background(bgColor).Padding(3).Text(raceDetail.RaceNumber.ToString()).FontSize(8);
                                            table.Cell().Background(bgColor).Padding(3).Text(raceDetail.RaceName).FontSize(8);
                                            table.Cell().Background(bgColor).Padding(3).Text($"{raceDetail.DistanceKm} km").FontSize(8);
                                            table.Cell().Background(bgColor).Padding(3).Text(raceDetail.Position.ToString()).FontSize(8);

                                            var pointsCell = table.Cell().Background(bgColor).Padding(3).Text(raceDetail.Points.ToString()).FontSize(8);
                                            if (raceDetail.IsInBest7)
                                            {
                                                pointsCell.Bold();
                                            }

                                            table.Cell().Background(bgColor).Padding(3).Text(raceDetail.BonusKm.ToString()).FontSize(8);
                                            table.Cell().Background(bgColor).Padding(3).Text(raceDetail.IsInBest7 ? "✓" : "").FontSize(8);
                                        }
                                    });

                                    column.Item().PaddingTop(10); // Spacing between challengers
                                }
                            });

                        page.Footer()
                            .AlignCenter()
                            .Text(text =>
                            {
                                text.Span("Page ");
                                text.CurrentPageNumber();
                                text.Span(" / ");
                                text.TotalPages();
                            });
                    });
                })
                .GeneratePdf(filePath);

                return filePath;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error generating PDF: {ex.Message}";
                throw;
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
