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
using NameParser.Domain.Entities;
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
        private readonly DatabaseMemberRepository _databaseMemberRepository;
        private readonly RecentRaceVmaRepository _recentRaceVmaRepository;
        private readonly NameParser.Application.Services.VmaExtrapolationService _vmaExtrapolationService;
        private readonly EmailTemplateRepository _emailTemplateRepository;
        private readonly IConfiguration _configuration;
        private readonly LocalizationService _localization;

        private DateTime _mailingDate;
        private string _emailSubject;
        private string _emailBody;
        private string _testEmailAddress;
        private string _statusMessage;
        private bool _isSending;

        // Optional communication block
        private bool _includeCommunication;
        private string _communicationTitle = "📢 Communication";
        private string _communicationText;
        private bool _communicationAtTop = true;

        // Templates
        private string _templateName;
        private EmailTemplateItem _selectedTemplate;

        // Preview / mail-merge
        private int _previewIndex;
        private string _previewHtml;
        private string _previewFilterType = "Tous";
        private string _previewFilterText;

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
            _databaseMemberRepository = new DatabaseMemberRepository();
            _recentRaceVmaRepository = new RecentRaceVmaRepository();
            _vmaExtrapolationService = new NameParser.Application.Services.VmaExtrapolationService(_databaseMemberRepository);
            _emailTemplateRepository = new EmailTemplateRepository();
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

            SaveTemplateCommand = new RelayCommand(ExecuteSaveTemplate, _ => !string.IsNullOrWhiteSpace(TemplateName));
            LoadTemplateCommand = new RelayCommand(ExecuteLoadTemplate, _ => SelectedTemplate != null);
            DeleteTemplateCommand = new RelayCommand(ExecuteDeleteTemplate, _ => SelectedTemplate != null);
            RefreshTemplatesCommand = new RelayCommand(_ => LoadTemplates());

            RefreshPreviewCommand = new RelayCommand(_ => LoadPreviewRecipients());
            PreviousPreviewCommand = new RelayCommand(_ => MovePreview(-1), _ => PreviewRecipients.Count > 0 && PreviewIndex > 0);
            NextPreviewCommand = new RelayCommand(_ => MovePreview(1), _ => PreviewRecipients.Count > 0 && PreviewIndex < PreviewRecipients.Count - 1);
            SendCurrentPreviewCommand = new RelayCommand(ExecuteSendCurrentPreview, _ => CanSendCurrentPreview());
            SendToFilteredPreviewCommand = new RelayCommand(ExecuteSendToFilteredPreview, _ => CanSendToFilteredPreview());

            MailingDate = DateTime.Today;

            LoadGmailSettings();
            LoadTemplates();
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

        /// <summary>When true, a communication block is inserted around the training blocks.</summary>
        public bool IncludeCommunication
        {
            get => _includeCommunication;
            set => SetProperty(ref _includeCommunication, value);
        }

        public string CommunicationTitle
        {
            get => _communicationTitle;
            set => SetProperty(ref _communicationTitle, value);
        }

        public string CommunicationText
        {
            get => _communicationText;
            set => SetProperty(ref _communicationText, value);
        }

        /// <summary>True = communication block above the training blocks, false = below.</summary>
        public bool CommunicationAtTop
        {
            get => _communicationAtTop;
            set => SetProperty(ref _communicationAtTop, value);
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

        // ----- Templates -----

        /// <summary>
        /// Saved email templates available for loading.
        /// </summary>
        public ObservableCollection<EmailTemplateItem> SavedTemplates { get; } = new ObservableCollection<EmailTemplateItem>();

        public string TemplateName
        {
            get => _templateName;
            set
            {
                if (SetProperty(ref _templateName, value))
                    ((RelayCommand)SaveTemplateCommand).RaiseCanExecuteChanged();
            }
        }

        public EmailTemplateItem SelectedTemplate
        {
            get => _selectedTemplate;
            set
            {
                if (SetProperty(ref _selectedTemplate, value))
                {
                    if (value != null)
                        TemplateName = value.Name;
                    ((RelayCommand)LoadTemplateCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)DeleteTemplateCommand).RaiseCanExecuteChanged();
                }
            }
        }

        // ----- Preview / mail-merge -----

        /// <summary>
        /// Recipients shown in the preview screen. The first item is the raw template
        /// (no member resolution); the others are the members with an email address.
        /// </summary>
        public ObservableCollection<PreviewRecipient> PreviewRecipients { get; } = new ObservableCollection<PreviewRecipient>();

        public int PreviewIndex
        {
            get => _previewIndex;
            set
            {
                if (SetProperty(ref _previewIndex, value))
                {
                    RefreshPreviewHtml();
                    RaisePreviewCommandsCanExecute();
                    OnPropertyChanged(nameof(SelectedPreviewRecipient));
                    OnPropertyChanged(nameof(PreviewPositionText));
                }
            }
        }

        public PreviewRecipient SelectedPreviewRecipient
        {
            get => PreviewIndex >= 0 && PreviewIndex < PreviewRecipients.Count ? PreviewRecipients[PreviewIndex] : null;
            set
            {
                if (value != null)
                {
                    var idx = PreviewRecipients.IndexOf(value);
                    if (idx >= 0 && idx != PreviewIndex)
                        PreviewIndex = idx;
                }
            }
        }

        public string PreviewPositionText =>
            PreviewRecipients.Count == 0 ? "0 / 0" : $"{PreviewIndex + 1} / {PreviewRecipients.Count}";

        /// <summary>Number of actual recipients (excludes the raw template entry).</summary>
        public int FilteredRecipientCount =>
            PreviewRecipients.Count(r => !r.IsRawTemplate && !string.IsNullOrWhiteSpace(r.Email));

        /// <summary>Label shown on the "send to filtered recipients" button, with the count.</summary>
        public string SendToFilteredButtonText =>
            $"📤 Envoyer aux {FilteredRecipientCount} destinataire(s)";

        /// <summary>Available filter options for the preview recipients list.</summary>
        public ObservableCollection<string> PreviewFilterTypes { get; } =
            new ObservableCollection<string> { "Tous", "Membres", "Challengers" };

        /// <summary>Selected recipient type filter: "Tous", "Membres" or "Challengers".</summary>
        public string PreviewFilterType
        {
            get => _previewFilterType;
            set
            {
                if (SetProperty(ref _previewFilterType, value))
                    LoadPreviewRecipients();
            }
        }

        /// <summary>Free-text filter applied on the recipient name / email.</summary>
        public string PreviewFilterText
        {
            get => _previewFilterText;
            set
            {
                if (SetProperty(ref _previewFilterText, value))
                    LoadPreviewRecipients();
            }
        }

        /// <summary>
        /// Resolved HTML shown in the preview WebView2 for the current recipient.
        /// </summary>
        public string PreviewHtml
        {
            get => _previewHtml;
            private set => SetProperty(ref _previewHtml, value);
        }

        public ICommand GenerateTemplateCommand { get; }
        public ICommand SendTestEmailCommand { get; }
        public ICommand SendToAllMembersCommand { get; }
        public ICommand SaveTemplateCommand { get; }
        public ICommand LoadTemplateCommand { get; }
        public ICommand DeleteTemplateCommand { get; }
        public ICommand RefreshTemplatesCommand { get; }
        public ICommand RefreshPreviewCommand { get; }
        public ICommand PreviousPreviewCommand { get; }
        public ICommand NextPreviewCommand { get; }
        public ICommand SendCurrentPreviewCommand { get; }
        public ICommand SendToFilteredPreviewCommand { get; }

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

            // Optional communication block (above the training blocks)
            if (IncludeCommunication && CommunicationAtTop)
                sb.AppendLine(BuildCommunicationBlock());

            // Training of the week - simplified cards (less nesting)
            sb.AppendLine("<h2 style='color: #2196F3;'>💪 Entraînements de la Semaine</h2>");

            // Mardi
            sb.AppendLine("<div style='background-color: #FFFFFF; padding: 12px 14px; border-radius: 8px; margin: 10px 0; border-left: 4px solid #FF9800; box-shadow: 0 1px 3px rgba(0,0,0,0.1);'>");
            sb.AppendLine("<p style='margin: 0 0 4px 0; font-size: 15px; font-weight: bold; color: #FF9800;'>📅 Mardi 17h00 <span style='font-size: 13px; font-weight: normal; color: #777;'>· Rendez-vous à la piste</span></p>");
            sb.AppendLine("<p style='margin: 4px 0; color: #333; font-size: 14px;'>📋 <strong>Entraînement :</strong> [Programme à définir]</p>");
            sb.AppendLine(PaceTablePlaceholder);
            sb.AppendLine("</div>");

            // Jeudi
            sb.AppendLine("<div style='background-color: #FFFFFF; padding: 12px 14px; border-radius: 8px; margin: 10px 0; border-left: 4px solid #4CAF50; box-shadow: 0 1px 3px rgba(0,0,0,0.1);'>");
            sb.AppendLine("<p style='margin: 0 0 4px 0; font-size: 15px; font-weight: bold; color: #4CAF50;'>🏃 Jeudi 16h30 - 17h00</p>");
            sb.AppendLine("<p style='margin: 4px 0; color: #777; font-size: 13px;'>Départ : 16h30 au parking de la piste, ou 17h00 à la barrière du bois de la Vecquée · 8 à 13 km dans les bois.</p>");
            sb.AppendLine("<p style='margin: 4px 0; color: #333; font-size: 14px;'>📋 <strong>Entraînement :</strong> [Programme à définir]</p>");
            sb.AppendLine(PaceTablePlaceholder);
            sb.AppendLine("</div>");

            // Optional communication block (below the training blocks)
            if (IncludeCommunication && !CommunicationAtTop)
                sb.AppendLine(BuildCommunicationBlock());

            // Placeholder for the per-recipient VMA block (resolved in BuildBodyForRecipient).
            sb.AppendLine(VmaBlockPlaceholder);

            // Placeholder for the per-recipient training-partners block (athletes with a close VMA).
            sb.AppendLine(PartnersBlockPlaceholder);

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

                var testVma = GetMemberVmaByEmail(TestEmailAddress);
                if (testVma == null || testVma.Vma <= 0)
                {
                    StatusMessage = $"Warning: no VMA found for {TestEmailAddress} in the database; the recap will not be resolved.";
                }

                await SendEmailAsync(TestEmailAddress, EmailSubject, BuildBodyForRecipient(testVma));

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
                        IsChallenger = m.IsChallenger ?? false,
                        Vma = m.Vma
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

                // VMA is stored in the database (VmaHistory), not in Members.json.
                // Build an email -> selected VMA lookup (tested first, then extrapolated,
                // most recent of each) so each recipient's recap is resolved.
                var vmaByEmail = new Dictionary<string, VmaSelection>(StringComparer.OrdinalIgnoreCase);
                foreach (var dbMember in _databaseMemberRepository.GetAll())
                {
                    var dbEmail = CleanEmailAddress(dbMember.Email);
                    if (!string.IsNullOrWhiteSpace(dbEmail) && !vmaByEmail.ContainsKey(dbEmail))
                        vmaByEmail[dbEmail] = SelectVma(dbMember.FirstName, dbMember.LastName);
                }

                int successCount = 0;
                int failCount = 0;
                var errors = new List<string>();

                foreach (var emailInfo in memberEmails)
                {
                    try
                    {
                        StatusMessage = $"Sending to {emailInfo.Email}... ({successCount + failCount + 1}/{memberEmails.Count})";
                        var recipientVma = vmaByEmail.TryGetValue(emailInfo.Email, out var dbVma) ? dbVma : null;
                        await SendEmailAsync(emailInfo.Email, EmailSubject, BuildBodyForRecipient(recipientVma), emailInfo.IsChallenger);
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
            public double? Vma { get; set; }
        }

        // ------------------------------------------------------------------
        //  Templates (save / load / delete)
        // ------------------------------------------------------------------

        private void LoadTemplates()
        {
            try
            {
                var previouslySelectedId = SelectedTemplate?.Id;
                SavedTemplates.Clear();
                foreach (var t in _emailTemplateRepository.GetAll())
                {
                    SavedTemplates.Add(new EmailTemplateItem
                    {
                        Id = t.Id,
                        Name = t.Name,
                        Subject = t.Subject,
                        Body = t.Body
                    });
                }

                if (previouslySelectedId.HasValue)
                    SelectedTemplate = SavedTemplates.FirstOrDefault(t => t.Id == previouslySelectedId.Value);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Impossible de charger les templates : {ex.Message}";
            }
        }

        private void ExecuteSaveTemplate(object parameter)
        {
            if (string.IsNullOrWhiteSpace(TemplateName))
                return;

            try
            {
                var saved = _emailTemplateRepository.Save(TemplateName, EmailSubject, EmailBody);
                LoadTemplates();
                SelectedTemplate = SavedTemplates.FirstOrDefault(t => t.Id == saved.Id);
                StatusMessage = $"✅ Template « {saved.Name} » enregistré.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Erreur à l'enregistrement du template : {ex.Message}";
                MessageBox.Show($"Erreur à l'enregistrement du template :\n\n{ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteLoadTemplate(object parameter)
        {
            if (SelectedTemplate == null)
                return;

            EmailSubject = SelectedTemplate.Subject;
            EmailBody = SelectedTemplate.Body;
            ((RelayCommand)SendToAllMembersCommand).RaiseCanExecuteChanged();
            StatusMessage = $"Template « {SelectedTemplate.Name} » chargé dans l'éditeur.";
        }

        private void ExecuteDeleteTemplate(object parameter)
        {
            if (SelectedTemplate == null)
                return;

            var name = SelectedTemplate.Name;
            var result = MessageBox.Show($"Supprimer le template « {name} » ?",
                "Confirmer", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                _emailTemplateRepository.Delete(SelectedTemplate.Id);
                LoadTemplates();
                SelectedTemplate = null;
                StatusMessage = $"Template « {name} » supprimé.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Erreur à la suppression : {ex.Message}";
            }
        }

        // ------------------------------------------------------------------
        //  Preview / mail-merge (per-member resolved preview)
        // ------------------------------------------------------------------

        /// <summary>
        /// Rebuilds the list of preview recipients: a first "raw template" entry,
        /// then every database member with an email address (resolved VMA).
        /// </summary>
        public void LoadPreviewRecipients()
        {
            try
            {
                PreviewRecipients.Clear();

                // First item: the raw template (no member resolution).
                PreviewRecipients.Add(new PreviewRecipient
                {
                    DisplayName = "Template (brut)",
                    IsRawTemplate = true
                });

                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var filterText = (_previewFilterText ?? string.Empty).Trim();
                foreach (var member in _databaseMemberRepository.GetAll())
                {
                    var email = CleanEmailAddress(member.Email);
                    if (string.IsNullOrWhiteSpace(email) || !seen.Add(email))
                        continue;

                    // Recipient type filter.
                    if (_previewFilterType == "Membres" && !member.IsMember)
                        continue;
                    if (_previewFilterType == "Challengers" && !member.IsChallenger)
                        continue;

                    var name = $"{member.FirstName} {member.LastName}".Trim();

                    // Free-text filter on name / email.
                    if (!string.IsNullOrWhiteSpace(filterText) &&
                        name.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) < 0 &&
                        email.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    PreviewRecipients.Add(new PreviewRecipient
                    {
                        DisplayName = string.IsNullOrWhiteSpace(name) ? email : $"{name} <{email}>",
                        FirstName = member.FirstName,
                        LastName = member.LastName,
                        Email = email,
                        IsMember = member.IsMember,
                        IsChallenger = member.IsChallenger
                    });
                }

                OnPropertyChanged(nameof(PreviewPositionText));
                OnPropertyChanged(nameof(FilteredRecipientCount));
                OnPropertyChanged(nameof(SendToFilteredButtonText));
                // Reset to the first recipient and refresh the rendered HTML.
                PreviewIndex = 0;
                RefreshPreviewHtml();
                RaisePreviewCommandsCanExecute();
                OnPropertyChanged(nameof(SelectedPreviewRecipient));

                StatusMessage = $"{PreviewRecipients.Count - 1} destinataire(s) chargé(s) pour la prévisualisation.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Impossible de charger les destinataires : {ex.Message}";
            }
        }

        private void MovePreview(int delta)
        {
            var target = PreviewIndex + delta;
            if (target >= 0 && target < PreviewRecipients.Count)
                PreviewIndex = target;
        }

        private void RefreshPreviewHtml()
        {
            var recipient = SelectedPreviewRecipient;
            if (recipient == null)
            {
                PreviewHtml = string.Empty;
                return;
            }

            try
            {
                VmaSelection vma = recipient.IsRawTemplate
                    ? null
                    : SelectVma(recipient.FirstName, recipient.LastName);

                PreviewHtml = BuildBodyForRecipient(vma);
            }
            catch (Exception ex)
            {
                PreviewHtml = $"<p style='color:red;'>Erreur de prévisualisation : {System.Net.WebUtility.HtmlEncode(ex.Message)}</p>";
            }
        }

        private void RaisePreviewCommandsCanExecute()
        {
            ((RelayCommand)PreviousPreviewCommand).RaiseCanExecuteChanged();
            ((RelayCommand)NextPreviewCommand).RaiseCanExecuteChanged();
            ((RelayCommand)SendCurrentPreviewCommand).RaiseCanExecuteChanged();
        }

        private bool CanSendCurrentPreview()
        {
            var recipient = SelectedPreviewRecipient;
            return recipient != null
                && !recipient.IsRawTemplate
                && !string.IsNullOrWhiteSpace(recipient.Email)
                && !string.IsNullOrWhiteSpace(EmailSubject)
                && !string.IsNullOrWhiteSpace(EmailBody)
                && !string.IsNullOrWhiteSpace(GmailAddress)
                && !string.IsNullOrWhiteSpace(GmailAppPassword);
        }

        private async void ExecuteSendCurrentPreview(object parameter)
        {
            var recipient = SelectedPreviewRecipient;
            if (recipient == null || recipient.IsRawTemplate || string.IsNullOrWhiteSpace(recipient.Email))
                return;

            try
            {
                IsSending = true;
                StatusMessage = $"Envoi à {recipient.Email}...";

                var vma = SelectVma(recipient.FirstName, recipient.LastName);
                await SendEmailAsync(recipient.Email, EmailSubject, BuildBodyForRecipient(vma));

                StatusMessage = $"✅ Email envoyé à {recipient.Email}.";
                MessageBox.Show($"Email envoyé à {recipient.Email}.",
                    "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Erreur d'envoi : {ex.Message}";
                MessageBox.Show($"Erreur d'envoi :\n\n{ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsSending = false;
            }
        }

        private bool CanSendToFilteredPreview()
        {
            return !IsSending
                && PreviewRecipients.Any(r => !r.IsRawTemplate && !string.IsNullOrWhiteSpace(r.Email))
                && !string.IsNullOrWhiteSpace(EmailSubject)
                && !string.IsNullOrWhiteSpace(EmailBody)
                && !string.IsNullOrWhiteSpace(GmailAddress)
                && !string.IsNullOrWhiteSpace(GmailAppPassword);
        }

        /// <summary>
        /// Sends the email to every recipient currently shown in the filtered preview list
        /// (honoring the selected type and text filters), resolving each one's VMA block.
        /// </summary>
        private async void ExecuteSendToFilteredPreview(object parameter)
        {
            var recipients = PreviewRecipients
                .Where(r => !r.IsRawTemplate && !string.IsNullOrWhiteSpace(r.Email))
                .ToList();

            if (recipients.Count == 0)
            {
                MessageBox.Show("Aucun destinataire ne correspond au filtre sélectionné.",
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var preview = string.Join("\n", recipients.Take(10).Select(r => r.DisplayName));
            var result = MessageBox.Show(
                $"Cet envoi concerne {recipients.Count} destinataire(s) (filtre : {_previewFilterType}).\n\n{preview}" +
                (recipients.Count > 10 ? $"\n... et {recipients.Count - 10} de plus" : "") +
                "\n\nVoulez-vous continuer ?",
                "Confirmer l'envoi",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                IsSending = true;

                int successCount = 0;
                int failCount = 0;
                var errors = new List<string>();

                foreach (var recipient in recipients)
                {
                    try
                    {
                        StatusMessage = $"Envoi à {recipient.Email}... ({successCount + failCount + 1}/{recipients.Count})";
                        var vma = SelectVma(recipient.FirstName, recipient.LastName);
                        await SendEmailAsync(recipient.Email, EmailSubject, BuildBodyForRecipient(vma), recipient.IsChallenger);
                        successCount++;

                        // Delay to avoid rate limiting.
                        await System.Threading.Tasks.Task.Delay(5000);
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        errors.Add($"{recipient.Email}: {ex.Message}");
                    }
                }

                StatusMessage = $"✅ Envoi terminé : {successCount} réussi(s), {failCount} échec(s).";
                var summary = $"Envoi terminé.\n\nRéussis : {successCount}\nÉchecs : {failCount}";
                if (errors.Any())
                    summary += "\n\nErreurs :\n" + string.Join("\n", errors.Take(10));
                MessageBox.Show(summary, "Résultat de l'envoi", MessageBoxButton.OK,
                    failCount > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Erreur d'envoi : {ex.Message}";
                MessageBox.Show($"Erreur d'envoi :\n\n{ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsSending = false;
            }
        }

        /// <summary>
        /// Lightweight view item for a saved email template shown in the templates list.
        /// </summary>
        public sealed class EmailTemplateItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Subject { get; set; }
            public string Body { get; set; }
        }

        /// <summary>
        /// A single preview recipient: either the raw template or a resolvable member.
        /// </summary>
        public sealed class PreviewRecipient
        {
            public string DisplayName { get; set; }
            public bool IsRawTemplate { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            public bool IsMember { get; set; }
            public bool IsChallenger { get; set; }
        }

        /// <summary>
        /// Marker inserted in the generated template; replaced per recipient by the
        /// dedicated VMA block (calculated / extrapolated VMA + races).
        /// </summary>
        private const string VmaBlockPlaceholder = "<!--VMA_BLOCK-->";

        /// <summary>
        /// Marker inserted after each day's training programme; replaced per recipient
        /// by a formatted pace table resolving the "% VMA" tokens of that day's programme.
        /// </summary>
        private const string PaceTablePlaceholder = "<!--PACE_TABLE-->";

        /// <summary>
        /// Marker inserted after the VMA block; replaced per recipient by a list of up to
        /// three athletes whose VMA is the closest, to form training groups.
        /// </summary>
        private const string PartnersBlockPlaceholder = "<!--PARTNERS_BLOCK-->";

        /// <summary>
        /// Cached roster of members with their reference VMA (tested first, then
        /// extrapolated), used to suggest training partners with a close VMA.
        /// </summary>
        private List<MemberVmaInfo> _vmaRoster;

        /// <summary>
        /// Resolves the email body for a recipient: injects the dedicated VMA block and,
        /// for each day's training programme, a formatted pace table (one row per distinct
        /// "% VMA" of that day) instead of resolving every token inline ("sapin de Noël").
        /// When the recipient has no VMA, the pace/VMA placeholders are simply removed.
        /// </summary>
        private string BuildBodyForRecipient(VmaSelection vma)
        {
            var body = EmailBody ?? string.Empty;

            // Inject the dedicated VMA block (or remove the placeholder when no data).
            body = body.Replace(VmaBlockPlaceholder, BuildVmaBlock(vma));

            // Inject the training-partners block (athletes with a close VMA).
            body = body.Replace(PartnersBlockPlaceholder, BuildPartnersBlock(vma));

            // Resolve each day's pace table from the programme text that precedes its
            // placeholder (segment between the previous placeholder and this one).
            body = ResolvePaceTables(body, vma);

            return body;
        }

        /// <summary>
        /// Replaces every <see cref="PaceTablePlaceholder"/> with a pace table built from
        /// the "% VMA" tokens found in the text segment that precedes it. Segments are
        /// delimited by the previous placeholder (or the start of the body).
        /// </summary>
        private string ResolvePaceTables(string body, VmaSelection vma)
        {
            if (string.IsNullOrEmpty(body) || body.IndexOf(PaceTablePlaceholder, StringComparison.Ordinal) < 0)
                return body;

            var sb = new StringBuilder();
            int searchStart = 0;
            int segmentStart = 0;

            while (true)
            {
                int idx = body.IndexOf(PaceTablePlaceholder, searchStart, StringComparison.Ordinal);
                if (idx < 0)
                {
                    sb.Append(body.Substring(segmentStart));
                    break;
                }

                // Text of this day's programme (used to detect the "% VMA" tokens).
                var segment = body.Substring(segmentStart, idx - segmentStart);

                // Append the segment, then the resolved pace table in place of the marker.
                sb.Append(segment);
                sb.Append(NameParser.Application.Services.TrainingPaceCalculator.BuildPaceTableHtml(
                    segment,
                    vma?.Vma,
                    vmaDate: vma?.RecordedDate,
                    isExtrapolated: vma?.IsExtrapolated ?? false));

                int next = idx + PaceTablePlaceholder.Length;
                segmentStart = next;
                searchStart = next;
            }

            return sb.ToString();
        }

        /// <summary>
        /// Builds an optional communication block (title + free text) inserted above or
        /// below the training blocks depending on the configuration.
        /// </summary>
        private string BuildCommunicationBlock()
        {
            var title = string.IsNullOrWhiteSpace(CommunicationTitle) ? "📢 Communication" : CommunicationTitle.Trim();
            var text = string.IsNullOrWhiteSpace(CommunicationText)
                ? "[Message à définir]"
                : CommunicationText.Trim().Replace("\r\n", "<br/>").Replace("\n", "<br/>");

            var sb = new StringBuilder();
            sb.AppendLine("<div style='background-color: #FFFDE7; padding: 15px; border-radius: 8px; margin: 12px 0; border-left: 4px solid #FBC02D;'>");
            sb.AppendLine($"<p style='margin: 0 0 6px 0; font-size: 16px; font-weight: bold; color: #F9A825;'>{System.Net.WebUtility.HtmlEncode(title)}</p>");
            sb.AppendLine($"<div style='color: #555; font-size: 14px;'>{text}</div>");
            sb.AppendLine("</div>");
            return sb.ToString();
        }

        /// <summary>
        /// Builds the dedicated VMA HTML block shown between the training blocks and the
        /// previous-week results: calculated (tested) VMA, extrapolated (recent-races) VMA
        /// and the list of races used as a basis. Returns an empty string when the
        /// recipient has no VMA data.
        /// </summary>
        private static string BuildVmaBlock(VmaSelection vma)
        {
            if (vma == null || (!vma.CalculatedVma.HasValue && !vma.ExtrapolatedVma.HasValue && !vma.AllTimeExtrapolatedVma.HasValue))
                return string.Empty;

            var fr = CultureInfo.GetCultureInfo("fr-FR");

            string CardValue(double? value, DateTime? date)
            {
                if (!value.HasValue)
                    return "<span style='font-size: 22px; font-weight: bold;'>-</span>";
                var line = $"<span style='font-size: 26px; font-weight: bold;'>{value.Value.ToString("0.0", fr)}</span> <span style='font-size: 13px;'>km/h</span>";
                if (date.HasValue)
                    line += $"<br/><span style='font-size: 12px; opacity: 0.85;'>le {date.Value.ToString("dd/MM/yyyy", fr)}</span>";
                return line;
            }

            string Card(string title, string value, string background)
            {
                return
                    $"<div style=\"display: inline-block; vertical-align: top; box-sizing: border-box; width: 48%; min-width: 180px; margin: 0 1% 12px 0; padding: 14px 16px; border-radius: 10px; color: #ffffff; background: {background}; font-family: Arial, sans-serif;\">" +
                    $"<div style='font-size: 13px; text-transform: uppercase; letter-spacing: .5px; opacity: .9; margin-bottom: 6px;'>{title}</div>" +
                    $"<div>{value}</div>" +
                    "</div>";
            }

            var races = vma.Races != null && vma.Races.Count > 0
                ? string.Join(", ", vma.Races)
                : "-";

            var sb = new StringBuilder();
            sb.AppendLine("<h2 style='color: #9C27B0;'>⚡ Ta VMA</h2>");
            sb.AppendLine("<div style='margin: 15px 0; max-width: 520px;'>");
            sb.AppendLine(Card("VMA calculée", CardValue(vma.CalculatedVma, vma.CalculatedDate), "#9C27B0"));
            var extrapolatedTitle = vma.ExtrapolatedVma.HasValue ? "VMA extrapolée (récente) *" : "VMA extrapolée (récente)";
            sb.AppendLine(Card(extrapolatedTitle, CardValue(vma.ExtrapolatedVma, vma.ExtrapolatedDate), "#7E57C2"));
            if (vma.AllTimeExtrapolatedVma.HasValue)
            {
                var allTimeSub = vma.AllTimeRaceCount > 0
                    ? $"sur {vma.AllTimeRaceCount} course(s)"
                    : null;
                var allTimeValue = $"<span style='font-size: 26px; font-weight: bold;'>{vma.AllTimeExtrapolatedVma.Value.ToString("0.0", fr)}</span> <span style='font-size: 13px;'>km/h</span>";
                if (allTimeSub != null)
                    allTimeValue += $"<br/><span style='font-size: 12px; opacity: 0.85;'>{allTimeSub}</span>";
                sb.AppendLine(Card("VMA extrapolée (tout temps) *", allTimeValue, "#5E35B1"));
            }
            sb.AppendLine("</div>");
            sb.AppendLine($"<p style='margin: 0 0 8px 0; font-family: Arial, sans-serif; font-size: 13px; color: #555;'><strong>Courses :</strong> {races}</p>");

            if (vma.ExtrapolatedVma.HasValue || vma.AllTimeExtrapolatedVma.HasValue)
            {
                sb.AppendLine("<p style='margin: 0 0 4px 0; font-family: Arial, sans-serif; font-size: 11px; color: #999; font-style: italic;'>* VMA extrapolée estimée à partir de tes temps de course : « récente » sur tes courses des 3 derniers mois, « tout temps » sur l'ensemble de tes courses.</p>");
            }

            if (!vma.CalculatedVma.HasValue)
            {
                sb.AppendLine("<p style='margin: 0 0 15px 0; font-family: Arial, sans-serif; font-size: 11px; color: #999; font-style: italic;'>💡 Tu n'as pas encore de VMA testée : n'hésite pas à me communiquer ta VMA pour des allures plus précises.</p>");
            }
            else
            {
                sb.AppendLine("<div style='margin: 0 0 15px 0;'></div>");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Builds the "training partners" block: up to three athletes whose reference VMA
        /// is the closest to the recipient's, to encourage shared training sessions.
        /// Returns an empty string when the recipient has no VMA or no close partner.
        /// </summary>
        private string BuildPartnersBlock(VmaSelection vma)
        {
            if (vma == null || vma.Vma <= 0 ||
                string.IsNullOrWhiteSpace(vma.FirstName) && string.IsNullOrWhiteSpace(vma.LastName))
                return string.Empty;

            List<MemberVmaInfo> roster;
            try
            {
                roster = _vmaRoster ??= _databaseMemberRepository.GetLatestVmasPreferMeasured();
            }
            catch
            {
                return string.Empty;
            }

            if (roster == null || roster.Count == 0)
                return string.Empty;

            bool IsSelf(MemberVmaInfo m) =>
                string.Equals(m.FirstName ?? string.Empty, vma.FirstName ?? string.Empty, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(m.LastName ?? string.Empty, vma.LastName ?? string.Empty, StringComparison.OrdinalIgnoreCase);

            var partners = roster
                .Where(m => m.Vma > 0 && !IsSelf(m))
                .OrderBy(m => Math.Abs(m.Vma - vma.Vma))
                .ThenBy(m => m.LastName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(m => m.FirstName, StringComparer.OrdinalIgnoreCase)
                .Take(5)
                .ToList();

            if (partners.Count == 0)
                return string.Empty;

            var fr = CultureInfo.GetCultureInfo("fr-FR");

            var items = partners.Select(p =>
            {
                var name = $"{p.FirstName} {p.LastName}".Trim();
                return $"<strong>{System.Net.WebUtility.HtmlEncode(name)}</strong> ({p.Vma.ToString("0.0", fr)} kmh)";
            });

            var sb = new StringBuilder();
            sb.AppendLine(
                "<p style='margin: 0 0 15px 0; font-family: Arial, sans-serif; font-size: 13px; color: #555;'>" +
                "<strong style='color: #00897B;'>🤝 Partenaires d'entraînement :</strong> " +
                string.Join(" ", items) +
                "</p>");

            return sb.ToString();
        }
        /// available, otherwise the most recent extrapolated VMA, together with its date.
        /// Also carries the data needed to build the dedicated VMA block (tested VMA,
        /// recent-races extrapolated VMA and the races it is based on).
        /// </summary>
        private sealed class VmaSelection
        {
            public double Vma { get; set; }
            public DateTime RecordedDate { get; set; }
            public bool IsExtrapolated { get; set; }

            // Identity of the recipient this selection belongs to (used to build the
            // training-partners block based on athletes with a close VMA).
            public string FirstName { get; set; }
            public string LastName { get; set; }

            // Data for the dedicated VMA block.
            public double? CalculatedVma { get; set; }
            public DateTime? CalculatedDate { get; set; }
            public double? ExtrapolatedVma { get; set; }
            public DateTime? ExtrapolatedDate { get; set; }
            // Extrapolated VMA computed over all the member's races (not only the recent ones),
            // to stay consistent with the "all-time" VMA history shown elsewhere.
            public double? AllTimeExtrapolatedVma { get; set; }
            public int AllTimeRaceCount { get; set; }
            public List<string> Races { get; set; } = new List<string>();
        }

        /// <summary>
        /// Picks the VMA to use: the most recent tested value first, then the most recent
        /// extrapolated value. Returns null when the member has no VMA history.
        /// </summary>
        private VmaSelection SelectVma(string firstName, string lastName)
        {
            var history = _databaseMemberRepository.GetVmaHistory(firstName, lastName);

            var tested = history?
                .Where(h => !h.IsExtrapolated)
                .OrderByDescending(h => h.RecordedDate)
                .ThenByDescending(h => h.Id)
                .FirstOrDefault();

            var extrapolated = history?
                .Where(h => h.IsExtrapolated)
                .OrderByDescending(h => h.RecordedDate)
                .ThenByDescending(h => h.Id)
                .FirstOrDefault();

            // Recent-races VMA (estimate over the last races) + the races used as basis.
            double? recentRacesVma = null;
            DateTime? recentRacesDate = null;
            var races = new List<string>();
            try
            {
                var recentLog = _recentRaceVmaRepository.GetForMember(firstName, lastName);
                if (recentLog != null)
                {
                    if (recentLog.AverageVma.HasValue && recentLog.AverageVma.Value > 0)
                    {
                        recentRacesVma = recentLog.AverageVma.Value;
                        recentRacesDate = recentLog.CreatedDate;
                    }

                    if (recentLog.Lines != null)
                    {
                        races = recentLog.Lines
                            .Where(l => !string.IsNullOrWhiteSpace(l.RaceName))
                            .Select(l => l.RaceName.Trim())
                            .ToList();
                    }
                }
            }
            catch
            {
                // Recent-races table may not exist yet; ignore and fall back to history.
            }

            // The extrapolated VMA shown in the block prefers the recent-races estimate,
            // otherwise the most recent extrapolated history entry.
            double? blockExtrapolated = recentRacesVma
                ?? (extrapolated != null && extrapolated.Vma > 0 ? (double?)extrapolated.Vma : null);
            DateTime? blockExtrapolatedDate = recentRacesVma.HasValue
                ? recentRacesDate
                : (extrapolated != null && extrapolated.Vma > 0 ? (DateTime?)extrapolated.RecordedDate : null);
            double? blockCalculated = tested != null && tested.Vma > 0 ? (double?)tested.Vma : null;
            DateTime? blockCalculatedDate = tested != null && tested.Vma > 0 ? (DateTime?)tested.RecordedDate : null;

            // All-time extrapolated VMA: same estimation used by the VMA screen
            // (regression on the member's best average race speed over all races), so the
            // value shown here matches the VMA history. We also expose the race count.
            double? allTimeExtrapolated = null;
            int allTimeRaceCount = 0;
            try
            {
                var (allTimeVma, raceCount) = _vmaExtrapolationService.ExtrapolateVmaWithRaceCount(firstName, lastName);
                if (allTimeVma.HasValue && allTimeVma.Value > 0)
                {
                    allTimeExtrapolated = allTimeVma.Value;
                    allTimeRaceCount = raceCount;
                }
            }
            catch
            {
                // Classification/race tables may be unavailable; ignore the all-time estimate.
            }

            // The reference VMA used to resolve "% VMA" paces (mardi/jeudi), per rule:
            // 1) tested value; 2) else the 3-month recent-races value when it is based on
            // more than 3 races; 3) else the all-time extrapolated value.
            double? referenceVma = null;
            DateTime? referenceDate = null;
            bool referenceIsExtrapolated = false;

            if (tested != null && tested.Vma > 0)
            {
                referenceVma = tested.Vma;
                referenceDate = tested.RecordedDate;
                referenceIsExtrapolated = false;
            }
            else if (recentRacesVma.HasValue && recentRacesVma.Value > 0 && races.Count > 3)
            {
                referenceVma = recentRacesVma.Value;
                referenceDate = recentRacesDate;
                referenceIsExtrapolated = true;
            }
            else if (allTimeExtrapolated.HasValue && allTimeExtrapolated.Value > 0)
            {
                referenceVma = allTimeExtrapolated.Value;
                referenceDate = null;
                referenceIsExtrapolated = true;
            }

            if (!referenceVma.HasValue && !blockCalculated.HasValue && !blockExtrapolated.HasValue)
                return null;

            return new VmaSelection
            {
                Vma = referenceVma ?? 0,
                RecordedDate = referenceDate ?? DateTime.Now,
                IsExtrapolated = referenceIsExtrapolated,
                FirstName = firstName,
                LastName = lastName,
                CalculatedVma = blockCalculated,
                CalculatedDate = blockCalculatedDate,
                ExtrapolatedVma = blockExtrapolated,
                ExtrapolatedDate = blockExtrapolatedDate,
                AllTimeExtrapolatedVma = allTimeExtrapolated,
                AllTimeRaceCount = allTimeRaceCount,
                Races = races
            };
        }

        /// <summary>
        /// Looks up a member's VMA by email address (from the database), so the test email
        /// is resolved against the real member whose address is used for the test.
        /// </summary>
        private VmaSelection GetMemberVmaByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            var cleaned = CleanEmailAddress(email);
            if (string.IsNullOrWhiteSpace(cleaned))
                return null;

            var member = _databaseMemberRepository.GetAll()
                .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m.Email) &&
                                     string.Equals(CleanEmailAddress(m.Email), cleaned, StringComparison.OrdinalIgnoreCase));

            if (member == null)
                return null;

            return SelectVma(member.FirstName, member.LastName);
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
