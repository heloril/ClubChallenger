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
        private readonly IConfiguration _configuration;
        private readonly LocalizationService _localization;

        private ChallengeEntity _selectedChallenge;
        private string _emailSubject;
        private string _emailBody;
        private string _testEmailAddress;
        private string _statusMessage;
        private bool _isSending;

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
            _localization = LocalizationService.Instance;

            // Load configuration from appsettings.json
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            Challenges = new ObservableCollection<ChallengeEntity>();

            LoadChallengesCommand = new RelayCommand(ExecuteLoadChallenges);
            GenerateTemplateCommand = new RelayCommand(ExecuteGenerateTemplate, CanExecuteGenerateTemplate);
            SendTestEmailCommand = new RelayCommand(ExecuteSendTestEmail, CanExecuteSendTestEmail);
            SendToAllChallengersCommand = new RelayCommand(ExecuteSendToAllChallengers, CanExecuteSendToAllChallengers);

            LoadChallenges();
            LoadGmailSettings();
        }

        public ObservableCollection<ChallengeEntity> Challenges { get; }

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

        private bool CanExecuteGenerateTemplate(object parameter)
        {
            return SelectedChallenge != null;
        }

        private void ExecuteGenerateTemplate(object parameter)
        {
            try
            {
                var template = GenerateEmailTemplate();
                EmailSubject = template.Subject;
                EmailBody = template.Body;
                StatusMessage = "Email template generated successfully!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error generating template: {ex.Message}";
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

                // Get distances for next race
                var nextEventRaces = _raceRepository.GetRacesByRaceEvent(nextRace.Id);
                if (nextEventRaces.Any())
                {
                    var distances = nextEventRaces.Select(r => r.DistanceKm).Distinct().OrderBy(d => d);
                    sb.AppendLine($"<p><strong>🏃 {(isFrench ? "Distances" : "Distances")}:</strong> {string.Join(", ", distances.Select(d => $"{d} km"))}</p>");
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
                    var raceDistances = _raceRepository.GetRacesByRaceEvent(race.Id)
                        .Select(r => r.DistanceKm)
                        .Distinct()
                        .OrderBy(d => d)
                        .ToList();
                    var distanceStr = raceDistances.Any() ? string.Join(", ", raceDistances.Select(d => $"{d}km")) : (isFrench ? "À confirmer" : "TBA");
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

            // Current Challenge Standings - AFFICHER TOUS LES CHALLENGERS
            sb.AppendLine($"<h2 style='color: #FF9800;'>🏆 {(isFrench ? "Classement Actuel du Challenge" : "Current Challenge Standings")}</h2>");

            var challengerClassifications = _classificationRepository.GetChallengerClassification(SelectedChallenge.Year)
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
                    sb.AppendLine($"<td style='padding: 8px;'><strong>{c.ChallengerFirstName} {c.ChallengerLastName}</strong></td>");
                    sb.AppendLine($"<td style='padding: 8px;'><strong>{c.TotalPoints}</strong></td>");
                    sb.AppendLine($"<td style='padding: 8px;'>{c.RaceCount}</td>");
                    sb.AppendLine($"<td style='padding: 8px;'>{c.TotalKilometers}</td>");
                    sb.AppendLine("</tr>");
                }

                sb.AppendLine("</tbody>");
                sb.AppendLine("</table>");
            }

            // Footer
            sb.AppendLine("<hr style='border: 1px solid #FF9800; margin-top: 30px;'/>");
            sb.AppendLine($"<p style='font-size: 12px; color: #666;'>{(isFrench ? "Continuez le beau travail ! À bientôt à la prochaine course ! 🏃💪" : "Keep up the great work! See you at the next race! 🏃💪")}</p>");

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
            try
            {
                // Get all challengers with emails
                var challengerClassifications = _classificationRepository.GetChallengerClassification(SelectedChallenge.Year);
                var challengerEmails = new List<string>();

                foreach (var challenger in challengerClassifications)
                {
                    var member = _memberRepository.GetMemberByName(challenger.ChallengerFirstName, challenger.ChallengerLastName);
                    if (member != null && !string.IsNullOrWhiteSpace(member.Email))
                    {
                        challengerEmails.Add(member.Email);
                    }
                }

                if (!challengerEmails.Any())
                {
                    MessageBox.Show("No challengers with email addresses found.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var result = MessageBox.Show(
                    $"This will send the email to {challengerEmails.Count} challenger(s).\n\nRecipients:\n{string.Join("\n", challengerEmails.Take(10))}" +
                    (challengerEmails.Count > 10 ? $"\n... and {challengerEmails.Count - 10} more" : "") +
                    "\n\nAre you sure you want to continue?",
                    "Confirm Send",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;

                IsSending = true;
                int successCount = 0;
                int failCount = 0;
                var errors = new List<string>();

                foreach (var email in challengerEmails)
                {
                    try
                    {
                        StatusMessage = $"Sending to {email}... ({successCount + failCount + 1}/{challengerEmails.Count})";
                        await SendEmailAsync(email, EmailSubject, EmailBody);
                        successCount++;
                        
                        // Small delay to avoid rate limiting
                        await System.Threading.Tasks.Task.Delay(500);
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        errors.Add($"{email}: {ex.Message}");
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

        private async System.Threading.Tasks.Task SendEmailAsync(string toEmail, string subject, string body)
        {
//#if MAILKIT_INSTALLED
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Challenge Administrator", _gmailAddress));
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
//#else
            //await System.Threading.Tasks.Task.CompletedTask;
            //throw new NotImplementedException(
            //    "MailKit package is not installed.\n\n" +
            //    "To enable email functionality, install MailKit:\n" +
            //    "Install-Package MailKit -ProjectName NameParser.UI\n\n" +
            //    "See CHALLENGE_MAILING_INSTALLATION_GUIDE.md for complete setup instructions.");
//#endif
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
