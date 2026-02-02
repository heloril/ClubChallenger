using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using NameParser.Application.Services;
using NameParser.Domain.Entities;
using NameParser.Domain.Repositories;
using NameParser.Domain.Services;
using NameParser.Infrastructure.Data;
using NameParser.Infrastructure.Data.Models;
using NameParser.Infrastructure.Repositories;
using NameParser.Infrastructure.Services;
using NameParser.UI.Services;

namespace NameParser.UI.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly RaceRepository _raceRepository;
        private readonly ClassificationRepository _classificationRepository;
        private string _selectedFilePath;
        private string _raceName;
        private int _raceNumber;
        private int _year;
        private int _distanceKm;
        private string _statusMessage;
        private bool _isProcessing;
        private RaceEntity _selectedRace;
        private List<object> _selectedRaces;
        private bool _showGeneralClassification;
        private bool _showChallengerClassification;
        private int _selectedYear;
        private bool _isHorsChallenge;
        private bool? _isMemberFilter;
        private bool? _isChallengerFilter;
        private string _selectedLanguage;

        public MainViewModel()
        {
            _raceRepository = new RaceRepository();
            _classificationRepository = new ClassificationRepository();

            Year = DateTime.Now.Year;
            SelectedYear = DateTime.Now.Year;
            DistanceKm = 10;
            RaceNumber = 1;
            ShowGeneralClassification = false;

            // Initialize language
            _selectedLanguage = "en";
            AvailableLanguages = new ObservableCollection<LanguageOption>
            {
                new LanguageOption { Code = "en", DisplayName = "English" },
                new LanguageOption { Code = "fr", DisplayName = "Français" }
            };

            // Subscribe to localization changes
            LocalizationService.Instance.PropertyChanged += (s, e) => OnPropertyChanged(nameof(Localization));

            UploadFileCommand = new RelayCommand(ExecuteUploadFile);
            ProcessRaceCommand = new RelayCommand(ExecuteProcessRace, CanExecuteProcessRace);
            ReprocessRaceCommand = new RelayCommand(ExecuteReprocessRace, CanExecuteReprocessRace);
            DownloadResultCommand = new RelayCommand(ExecuteDownloadResult, CanExecuteDownloadResult);
            ExportForEmailCommand = new RelayCommand(ExecuteExportForEmail, CanExecuteExportForEmail);
            ExportMultipleForEmailCommand = new RelayCommand(ExecuteExportMultipleForEmail, CanExecuteExportMultipleForEmail);
            RefreshRacesCommand = new RelayCommand(ExecuteRefreshRaces);
            DeleteRaceCommand = new RelayCommand(ExecuteDeleteRace, CanExecuteDeleteRace);
            ViewClassificationCommand = new RelayCommand(ExecuteViewClassification, CanExecuteViewClassification);
            ViewGeneralClassificationCommand = new RelayCommand(ExecuteViewGeneralClassification);
            ViewChallengerClassificationCommand = new RelayCommand(ExecuteViewChallengerClassification);
            ExportChallengerClassificationCommand = new RelayCommand(ExecuteExportChallengerClassification, CanExecuteExportChallengerClassification);
            ShowRaceClassificationCommand = new RelayCommand(ExecuteShowRaceClassification);
            ShowOnlyMembersCommand = new RelayCommand(ExecuteShowOnlyMembers);
            ShowOnlyNonMembersCommand = new RelayCommand(ExecuteShowOnlyNonMembers);
            ShowAllCommand = new RelayCommand(ExecuteShowAll);
            ShowOnlyChallengersCommand = new RelayCommand(ExecuteShowOnlyChallengers);
            ShowOnlyNonChallengersCommand = new RelayCommand(ExecuteShowOnlyNonChallengers);

            Years = new ObservableCollection<int>();
            for (int i = 2020; i <= 2030; i++)
            {
                Years.Add(i);
            }

            Races = new ObservableCollection<RaceEntity>();
            Classifications = new ObservableCollection<ClassificationEntity>();
            GeneralClassifications = new ObservableCollection<GeneralClassificationDto>();
            ChallengerClassifications = new ObservableCollection<ChallengerClassificationDto>();

            LoadRaces();
        }

        public ObservableCollection<int> Years { get; }
        public ObservableCollection<RaceEntity> Races { get; }
        public ObservableCollection<ClassificationEntity> Classifications { get; }
        public ObservableCollection<GeneralClassificationDto> GeneralClassifications { get; }
        public ObservableCollection<ChallengerClassificationDto> ChallengerClassifications { get; }

        public string SelectedFilePath
        {
            get => _selectedFilePath;
            set => SetProperty(ref _selectedFilePath, value);
        }

        public string RaceName
        {
            get => _raceName;
            set => SetProperty(ref _raceName, value);
        }

        public int RaceNumber
        {
            get => _raceNumber;
            set => SetProperty(ref _raceNumber, value);
        }

        public int Year
        {
            get => _year;
            set => SetProperty(ref _year, value);
        }

        public int DistanceKm
        {
            get => _distanceKm;
            set => SetProperty(ref _distanceKm, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                SetProperty(ref _isProcessing, value);
                ((RelayCommand)ProcessRaceCommand).RaiseCanExecuteChanged();
            }
        }

        public RaceEntity SelectedRace
        {
            get => _selectedRace;
            set
            {
                SetProperty(ref _selectedRace, value);
                ((RelayCommand)DeleteRaceCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ViewClassificationCommand).RaiseCanExecuteChanged();
                ((RelayCommand)DownloadResultCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ExportForEmailCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ReprocessRaceCommand).RaiseCanExecuteChanged();
            }
        }

        public List<object> SelectedRaces
        {
            get => _selectedRaces;
            set
            {
                SetProperty(ref _selectedRaces, value);
                ((RelayCommand)ExportMultipleForEmailCommand).RaiseCanExecuteChanged();
            }
        }

        public bool ShowGeneralClassification
        {
            get => _showGeneralClassification;
            set => SetProperty(ref _showGeneralClassification, value);
        }

        public bool ShowChallengerClassification
        {
            get => _showChallengerClassification;
            set => SetProperty(ref _showChallengerClassification, value);
        }

        public int SelectedYear
        {
            get => _selectedYear;
            set
            {
                SetProperty(ref _selectedYear, value);
                if (ShowGeneralClassification)
                {
                    LoadGeneralClassification();
                }
                else if (ShowChallengerClassification)
                {
                    LoadChallengerClassification();
                }
            }
        }

        public bool IsHorsChallenge
        {
            get => _isHorsChallenge;
            set => SetProperty(ref _isHorsChallenge, value);
        }

        public bool? IsMemberFilter
        {
            get => _isMemberFilter;
            set
            {
                SetProperty(ref _isMemberFilter, value);
                if (SelectedRace != null)
                {
                    ExecuteViewClassification(null);
                }
            }
        }

        public bool? IsChallengerFilter
        {
            get => _isChallengerFilter;
            set
            {
                SetProperty(ref _isChallengerFilter, value);
                if (SelectedRace != null)
                {
                    ExecuteViewClassification(null);
                }
            }
        }

        public ICommand UploadFileCommand { get; }
        public ICommand ProcessRaceCommand { get; }
        public ICommand ReprocessRaceCommand { get; }
        public ICommand DownloadResultCommand { get; }
        public ICommand ExportForEmailCommand { get; }
        public ICommand ExportMultipleForEmailCommand { get; }
        public ICommand RefreshRacesCommand { get; }
        public ICommand DeleteRaceCommand { get; }
        public ICommand ViewClassificationCommand { get; }
        public ICommand ViewGeneralClassificationCommand { get; }
        public ICommand ViewChallengerClassificationCommand { get; }
        public ICommand ExportChallengerClassificationCommand { get; }
        public ICommand ShowRaceClassificationCommand { get; }
        public ICommand ShowOnlyMembersCommand { get; }
        public ICommand ShowOnlyNonMembersCommand { get; }
        public ICommand ShowAllCommand { get; }
        public ICommand ShowOnlyChallengersCommand { get; }
        public ICommand ShowOnlyNonChallengersCommand { get; }

        // Localization
        public LocalizationService Localization => LocalizationService.Instance;

        public ObservableCollection<LanguageOption> AvailableLanguages { get; }

        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (SetProperty(ref _selectedLanguage, value))
                {
                    LocalizationService.Instance.SetLanguage(value);
                }
            }
        }

        private void ExecuteUploadFile(object parameter)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Race Result Files (*.xlsx;*.pdf)|*.xlsx;*.pdf|Excel Files (*.xlsx)|*.xlsx|PDF Files (*.pdf)|*.pdf|All Files (*.*)|*.*",
                Title = "Select Race Result File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                SelectedFilePath = openFileDialog.FileName;
                var extension = Path.GetExtension(SelectedFilePath).ToLowerInvariant();
                var fileType = extension == ".pdf" ? "PDF" : "Excel";
                StatusMessage = $"{fileType} file selected: {Path.GetFileName(SelectedFilePath)}";
            }
        }

        private bool CanExecuteProcessRace(object parameter)
        {
            // For hors challenge races, year is not required
            bool yearValid = IsHorsChallenge || Year > 0;

            return !IsProcessing && 
                   !string.IsNullOrEmpty(SelectedFilePath) && 
                   !string.IsNullOrEmpty(RaceName) &&
                   yearValid &&
                   RaceNumber > 0 &&
                   DistanceKm > 0;
        }

        private async void ExecuteProcessRace(object parameter)
        {
            IsProcessing = true;
            StatusMessage = "Processing race...";

            try
            {
                await System.Threading.Tasks.Task.Run(() =>
                {
                    var race = new Race(RaceNumber, RaceName, DistanceKm);

                    // Save race with nullable year for hors challenge
                    int? yearToSave = IsHorsChallenge ? null : (int?)Year;
                    _raceRepository.SaveRace(race, yearToSave, SelectedFilePath, IsHorsChallenge);

                    var memberRepository = new JsonMemberRepository("Members.json");
                    var challengerRepository = new JsonMemberRepository("Challenge.json");
                    var memberService = new MemberService(memberRepository, challengerRepository);
                    var allMembers = memberService.GetAllMembersAndChallengers();

                    // Select appropriate parser based on file extension
                    var extension = Path.GetExtension(SelectedFilePath).ToLowerInvariant();
                    IRaceResultRepository raceResultRepository;

                    if (extension == ".pdf")
                    {
                        raceResultRepository = new PdfRaceResultRepository();
                    }
                    else
                    {
                        raceResultRepository = new ExcelRaceResultRepository();
                    }

                    var pointsCalculationService = new PointsCalculationService();

                    var raceProcessingService = new RaceProcessingService(
                        memberRepository,
                        raceResultRepository,
                        pointsCalculationService);

                    // Pass the race object with correct distance (from UI input) instead of parsing from filename
                    var classification = raceProcessingService.ProcessRaceWithMembers(SelectedFilePath, race, allMembers);

                    // Get the saved race - for hors challenge, get all hors challenge races, otherwise get by year
                    List<RaceEntity> races;
                    if (IsHorsChallenge)
                    {
                        races = _raceRepository.GetHorsChallengeRaces();
                    }
                    else
                    {
                        races = _raceRepository.GetRacesByYear(Year);
                    }

                    var savedRace = races.FirstOrDefault(r => r.Name == RaceName && r.RaceNumber == RaceNumber);

                    if (savedRace != null)
                    {
                        _classificationRepository.SaveClassifications(savedRace.Id, classification);
                        _raceRepository.UpdateRaceStatus(savedRace.Id, "Processed");
                    }
                });

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusMessage = "Race processed successfully!";
                    LoadRaces();
                    ClearForm();
                });
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusMessage = $"Error: {ex.Message}";
                    MessageBox.Show($"Error processing race: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private bool CanExecuteReprocessRace(object parameter)
        {
            return !IsProcessing && SelectedRace != null && SelectedRace.FileContent != null && SelectedRace.FileContent.Length > 0;
        }

        private async void ExecuteReprocessRace(object parameter)
        {
            if (SelectedRace == null) return;

            var result = MessageBox.Show(
                $"Are you sure you want to reprocess race '{SelectedRace.Name}'?\nThis will delete existing classifications and reprocess from the stored file.",
                "Confirm Reprocess",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            IsProcessing = true;
            StatusMessage = "Reprocessing race...";

            try
            {
                await System.Threading.Tasks.Task.Run(() =>
                {
                    // Check if the stored file content exists
                    if (SelectedRace.FileContent == null || SelectedRace.FileContent.Length == 0)
                    {
                        throw new InvalidOperationException("No file content stored in database for this race.");
                    }

                    string tempFilePath = null;
                    try
                    {
                        // Write file content from database to temporary file for processing
                        var fileStorageService = new FileStorageService();
                        tempFilePath = fileStorageService.WriteToTempFile(SelectedRace.FileContent, SelectedRace.FileName);

                        // Delete existing classifications for this race
                        _classificationRepository.DeleteClassificationsByRace(SelectedRace.Id);

                        // Create race object from stored data
                        var race = new Race(SelectedRace.RaceNumber, SelectedRace.Name, SelectedRace.DistanceKm);

                        // Get members
                        var memberRepository = new JsonMemberRepository("Members.json");
                        var challengerRepository = new JsonMemberRepository("Challenge.json");
                        var memberService = new MemberService(memberRepository, challengerRepository);
                        var allMembers = memberService.GetAllMembersAndChallengers();

                        // Select appropriate parser based on file extension
                        var extension = SelectedRace.FileExtension.ToLowerInvariant();
                        IRaceResultRepository raceResultRepository;

                        if (extension == ".pdf")
                        {
                            raceResultRepository = new PdfRaceResultRepository();
                        }
                        else
                        {
                            raceResultRepository = new ExcelRaceResultRepository();
                        }

                        var pointsCalculationService = new PointsCalculationService();

                        var raceProcessingService = new RaceProcessingService(
                            memberRepository,
                            raceResultRepository,
                            pointsCalculationService);

                        // Process the race using temporary file path
                        var classification = raceProcessingService.ProcessRaceWithMembers(tempFilePath, race, allMembers);

                        // Save new classifications
                        _classificationRepository.SaveClassifications(SelectedRace.Id, classification);

                        // Update race status
                        _raceRepository.UpdateRaceStatus(SelectedRace.Id, "Processed");
                    }
                    finally
                    {
                        // Clean up temporary file
                        if (!string.IsNullOrEmpty(tempFilePath))
                        {
                            var fileStorageService = new FileStorageService();
                            fileStorageService.DeleteTempFile(tempFilePath);
                        }
                    }
                });

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusMessage = "Race reprocessed successfully!";
                    LoadRaces();
                    MessageBox.Show("Race reprocessed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                });
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusMessage = $"Error: {ex.Message}";
                    MessageBox.Show($"Error reprocessing race: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private bool CanExecuteDownloadResult(object parameter)
        {
            return SelectedRace != null;
        }

        private void ExecuteDownloadResult(object parameter)
        {
            if (SelectedRace == null) return;

            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Text Files (*.txt)|*.txt|CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                    FileName = $"Race_{SelectedRace.Year ?? 0}_{SelectedRace.RaceNumber}_{SelectedRace.Name}_Results.txt",
                    Title = Localization["SaveResultAs"]
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Apply the current filter when exporting
                    var classifications = _classificationRepository.GetClassificationsByRace(SelectedRace.Id, IsMemberFilter, IsChallengerFilter);

                    using (var writer = new StreamWriter(saveFileDialog.FileName))
                    {
                        writer.WriteLine($"{Localization["Race"]}: {SelectedRace.Name}");
                        writer.WriteLine($"{Localization["Year"]}: {(SelectedRace.Year.HasValue ? SelectedRace.Year.ToString() : Localization["HorsChallenge"])}");
                        writer.WriteLine($"{Localization["RaceNumber"]}: {SelectedRace.RaceNumber}");
                        writer.WriteLine($"{Localization["Distance"]}: {SelectedRace.DistanceKm} km");
                        writer.WriteLine($"{Localization["ProcessedDate"]}: {SelectedRace.ProcessedDate}");

                        // Show filter status in export
                        if (IsMemberFilter.HasValue || IsChallengerFilter.HasValue)
                        {
                            var filters = new List<string>();
                            if (IsMemberFilter.HasValue)
                            {
                                filters.Add(IsMemberFilter.Value ? Localization["MembersOnly"] : Localization["NonMembersOnly"]);
                            }
                            if (IsChallengerFilter.HasValue)
                            {
                                filters.Add(IsChallengerFilter.Value ? Localization["ChallengersOnly"] : Localization["NonChallengersOnly"]);
                            }
                            writer.WriteLine($"{Localization["FilterApplied"]} {string.Join(", ", filters)} {Localization["WinnerAlwaysShown"]}");
                        }
                        else
                        {
                            writer.WriteLine($"{Localization["FilterApplied"]} {Localization["AllParticipants"]}");
                        }

                        writer.WriteLine(new string('-', 80));
                        writer.WriteLine($"{Localization["Rank"],-6}{Localization["FirstName"] + " " + Localization["LastName"],-30}{Localization["Points"],-10}{Localization["BonusKM"],-10}");
                        writer.WriteLine(new string('-', 80));

                        int rank = 1;
                        foreach (var classification in classifications)
                        {
                            string fullName = $"{classification.MemberFirstName} {classification.MemberLastName}";
                            writer.WriteLine($"{rank,-6}{fullName,-30}{classification.Points,-10}{classification.BonusKm,-10}");
                            rank++;
                        }
                    }

                    StatusMessage = string.Format(Localization["ResultsSavedTo"], saveFileDialog.FileName);
                    MessageBox.Show(Localization["ExportSuccess"], Localization["ExportComplete"], MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(Localization["ErrorSaving"], ex.Message);
                MessageBox.Show(string.Format(Localization["ErrorSaving"], ex.Message), Localization["Error"], MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanExecuteExportForEmail(object parameter)
        {
            return SelectedRace != null && SelectedRace.Status == "Processed";
        }

        private void ExecuteExportForEmail(object parameter)
        {
            if (SelectedRace == null) return;

            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = Localization["HTMLFiles"],
                    FileName = $"Email_Race_{SelectedRace.Year ?? 0}_{SelectedRace.RaceNumber}_{SelectedRace.Name}.html",
                    Title = Localization["ExportResultsForEmail"]
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Apply the current filter when exporting
                    var classifications = _classificationRepository.GetClassificationsByRace(SelectedRace.Id, IsMemberFilter, IsChallengerFilter);
                    var extension = Path.GetExtension(saveFileDialog.FileName).ToLowerInvariant();

                    if (extension == ".html")
                    {
                        ExportToHtml(saveFileDialog.FileName, classifications);
                    }
                    else
                    {
                        ExportToText(saveFileDialog.FileName, classifications);
                    }

                    StatusMessage = string.Format(Localization["ResultsExportedTo"], saveFileDialog.FileName);
                    MessageBox.Show(Localization["ExportSuccess"], 
                        Localization["ExportComplete"], MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(Localization["ErrorExporting"], ex.Message);
                MessageBox.Show(string.Format(Localization["ErrorExporting"], ex.Message), 
                    Localization["Error"], MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToHtml(string filePath, List<ClassificationEntity> classifications)
        {
            using (var writer = new StreamWriter(filePath))
            {
                // Write HTML header
                writer.WriteLine("<!DOCTYPE html>");
                writer.WriteLine("<html>");
                writer.WriteLine("<head>");
                writer.WriteLine("    <meta charset='utf-8'>");
                writer.WriteLine("    <style>");
                writer.WriteLine("        body { font-family: Arial, sans-serif; }");
                writer.WriteLine("        h1 { color: #2196F3; }");
                writer.WriteLine("        table { border-collapse: collapse; width: 100%; margin: 20px 0; }");
                writer.WriteLine("        th { background-color: #2196F3; color: white; padding: 12px; text-align: left; }");
                writer.WriteLine("        td { padding: 8px; border-bottom: 1px solid #ddd; }");
                writer.WriteLine("        tr:nth-child(even) { background-color: #f2f2f2; }");
                writer.WriteLine("        tr:hover { background-color: #e3f2fd; }");
                writer.WriteLine("        .winner { background-color: #FFD700 !important; font-weight: bold; }");
                writer.WriteLine("        .member { color: #4CAF50; font-weight: bold; }");
                writer.WriteLine("        .info { color: #666; font-style: italic; margin: 10px 0; }");
                writer.WriteLine("        .filter-info { background-color: #FFF9C4; padding: 10px; margin: 10px 0; border-left: 4px solid #FFC107; }");
                writer.WriteLine("    </style>");
                writer.WriteLine("</head>");
                writer.WriteLine("<body>");

                // Write race information
                writer.WriteLine($"    <h1>{SelectedRace.Name}</h1>");
                writer.WriteLine($"    <div class='info'>");
                writer.WriteLine($"        <strong>{Localization["Year"]}:</strong> {(SelectedRace.Year.HasValue ? SelectedRace.Year.ToString() : Localization["HorsChallenge"])} | ");
                writer.WriteLine($"        <strong>{Localization["Distance"]}:</strong> {SelectedRace.DistanceKm} km | ");
                writer.WriteLine($"        <strong>{Localization["RaceNumber"]}:</strong> {SelectedRace.RaceNumber}");
                writer.WriteLine($"    </div>");

                // Show filter info
                if (IsMemberFilter.HasValue || IsChallengerFilter.HasValue)
                {
                    writer.WriteLine($"    <div class='filter-info'>");
                    var filters = new List<string>();
                    if (IsMemberFilter.HasValue)
                    {
                        filters.Add(IsMemberFilter.Value ? Localization["MembersOnly"] : Localization["NonMembersOnly"]);
                    }
                    if (IsChallengerFilter.HasValue)
                    {
                        filters.Add(IsChallengerFilter.Value ? Localization["ChallengersOnly"] : Localization["NonChallengersOnly"]);
                    }
                    writer.WriteLine($"        <strong>⚠️ {Localization["FilterApplied"]}</strong> {string.Join(", ", filters)} {Localization["WinnerAlwaysShown"]}");
                    writer.WriteLine($"    </div>");
                }

                // Write table
                writer.WriteLine("    <table>");
                writer.WriteLine("        <thead>");
                writer.WriteLine("            <tr>");
                writer.WriteLine($"                <th>{Localization["Rank"]}</th>");
                writer.WriteLine($"                <th>{Localization["Position"]}</th>");
                writer.WriteLine($"                <th>{Localization["FirstName"]} {Localization["LastName"]}</th>");
                writer.WriteLine($"                <th>{Localization["Team"]}</th>");
                writer.WriteLine($"                <th>{Localization["RaceTime"]}</th>");
                writer.WriteLine($"                <th>{Localization["TimePerKm"]}</th>");
                writer.WriteLine($"                <th>{Localization["Speed"]}</th>");
                writer.WriteLine($"                <th>{Localization["Points"]}</th>");
                writer.WriteLine($"                <th>{Localization["BonusKM"]}</th>");
                writer.WriteLine("            </tr>");
                writer.WriteLine("        </thead>");
                writer.WriteLine("        <tbody>");

                int rank = 1;
                foreach (var classification in classifications)
                {
                    string rowClass = classification.Position == 1 ? " class='winner'" : "";
                    string nameClass = classification.IsMember ? " class='member'" : "";
                    string fullName = $"{classification.MemberFirstName} {classification.MemberLastName}";

                    writer.WriteLine($"            <tr{rowClass}>");
                    writer.WriteLine($"                <td>{rank}</td>");
                    writer.WriteLine($"                <td>{classification.Position}</td>");
                    writer.WriteLine($"                <td{nameClass}>{fullName}</td>");
                    writer.WriteLine($"                <td>{classification.Team ?? "-"}</td>");
                    writer.WriteLine($"                <td>{FormatTimeSpan(classification.RaceTime)}</td>");
                    writer.WriteLine($"                <td>{FormatTimeSpan(classification.TimePerKm)}</td>");
                    writer.WriteLine($"                <td>{(classification.Speed.HasValue ? classification.Speed.Value.ToString("F2") : "-")}</td>");
                    writer.WriteLine($"                <td><strong>{classification.Points}</strong></td>");
                    writer.WriteLine($"                <td>{classification.BonusKm}</td>");
                    writer.WriteLine("            </tr>");
                    rank++;
                }

                writer.WriteLine("        </tbody>");
                writer.WriteLine("    </table>");
                writer.WriteLine($"    <div class='info'>");
                writer.WriteLine($"        <strong>{Localization["TotalParticipants"]}:</strong> {classifications.Count} | ");
                writer.WriteLine($"        <strong>{Localization["Exported"]}:</strong> {DateTime.Now:yyyy-MM-dd HH:mm}");
                writer.WriteLine($"    </div>");
                writer.WriteLine("</body>");
                writer.WriteLine("</html>");
            }
        }

        private void ExportToText(string filePath, List<ClassificationEntity> classifications)
        {
            using (var writer = new StreamWriter(filePath))
            {
                writer.WriteLine($"╔═══════════════════════════════════════════════════════════════════════════════╗");
                writer.WriteLine($"║  {SelectedRace.Name.ToUpper().PadRight(75)}║");
                writer.WriteLine($"╚═══════════════════════════════════════════════════════════════════════════════╝");
                writer.WriteLine();
                writer.WriteLine($"{Localization["Year"]}: {(SelectedRace.Year.HasValue ? SelectedRace.Year.ToString() : Localization["HorsChallenge"])}");
                writer.WriteLine($"{Localization["Distance"]}: {SelectedRace.DistanceKm} km | {Localization["RaceNumber"]}: {SelectedRace.RaceNumber}");
                writer.WriteLine();

                // Show filter info
                if (IsMemberFilter.HasValue || IsChallengerFilter.HasValue)
                {
                    var filters = new List<string>();
                    if (IsMemberFilter.HasValue)
                    {
                        filters.Add(IsMemberFilter.Value ? Localization["MembersOnly"] : Localization["NonMembersOnly"]);
                    }
                    if (IsChallengerFilter.HasValue)
                    {
                        filters.Add(IsChallengerFilter.Value ? Localization["ChallengersOnly"] : Localization["NonChallengersOnly"]);
                    }
                    writer.WriteLine($"⚠️  {Localization["FilteredView"]}: {string.Join(", ", filters)} ({Localization["WinnerAlwaysIncluded"]})");
                    writer.WriteLine();
                }

                writer.WriteLine(new string('─', 120));
                writer.WriteLine($"{Localization["Rank"],-6}│ {Localization["Position"],-5}│ {Localization["Name"],-30}│ {Localization["Team"],-20}│ {Localization["RaceTime"],-10}│ {Localization["TimePerKm"],-8}│ {Localization["Speed"],-8}│ {Localization["Points"],-7}│ {Localization["Bonus"],6}");
                writer.WriteLine(new string('─', 120));

                int rank = 1;
                foreach (var classification in classifications)
                {
                    string fullName = $"{classification.MemberFirstName} {classification.MemberLastName}";
                    string marker = classification.Position == 1 ? "🏆" : (classification.IsMember ? "✓" : " ");
                    string team = classification.Team ?? "-";
                    string speed = classification.Speed.HasValue ? classification.Speed.Value.ToString("F2") : "-";

                    writer.WriteLine(
                        $"{marker}{rank,-5}│ {classification.Position,-5}│ {fullName,-30}│ {team,-20}│ " +
                        $"{FormatTimeSpan(classification.RaceTime),-10}│ {FormatTimeSpan(classification.TimePerKm),-8}│ " +
                        $"{speed,-8}│ {classification.Points,-7}│ {classification.BonusKm,6}");
                    rank++;
                }

                writer.WriteLine(new string('─', 120));
                writer.WriteLine();
                writer.WriteLine($"{Localization["TotalParticipants"]}: {classifications.Count}");
                writer.WriteLine($"{Localization["Exported"]}: {DateTime.Now:yyyy-MM-dd HH:mm}");
                writer.WriteLine();
                writer.WriteLine($"{Localization["Legend"]}: 🏆 = {Localization["Winner"]} | ✓ = {Localization["ClubMember"]}");
            }
        }

        private string FormatTimeSpan(TimeSpan? timeSpan)
        {
            if (!timeSpan.HasValue)
                return "-";

            if (timeSpan.Value.TotalHours >= 1)
                return timeSpan.Value.ToString(@"h\:mm\:ss");
            else
                return timeSpan.Value.ToString(@"mm\:ss");
        }

        private bool CanExecuteExportMultipleForEmail(object parameter)
        {
            return SelectedRaces != null && 
                   SelectedRaces.Count > 0 && 
                   SelectedRaces.OfType<RaceEntity>().All(r => r.Status == "Processed");
        }

        private void ExecuteExportMultipleForEmail(object parameter)
        {
            if (SelectedRaces == null || SelectedRaces.Count == 0) return;

            var races = SelectedRaces.OfType<RaceEntity>().OrderBy(r => r.Year).ThenBy(r => r.RaceNumber).ToList();

            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "HTML Files (*.html)|*.html|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                    FileName = $"Email_Multiple_Races_{races.Count}_races_{DateTime.Now:yyyyMMdd}.html",
                    Title = "Export Multiple Races for Email"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var extension = Path.GetExtension(saveFileDialog.FileName).ToLowerInvariant();

                    if (extension == ".html")
                    {
                        ExportMultipleToHtml(saveFileDialog.FileName, races);
                    }
                    else
                    {
                        ExportMultipleToText(saveFileDialog.FileName, races);
                    }

                    StatusMessage = $"Exported {races.Count} races to: {saveFileDialog.FileName}";
                    MessageBox.Show($"Successfully exported {races.Count} races!\n\nYou can now copy the content and paste it into your email.", 
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error exporting multiple races: {ex.Message}";
                MessageBox.Show($"Error exporting multiple races: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportMultipleToHtml(string filePath, List<RaceEntity> races)
        {
            using (var writer = new StreamWriter(filePath))
            {
                // Write HTML header
                writer.WriteLine("<!DOCTYPE html>");
                writer.WriteLine("<html>");
                writer.WriteLine("<head>");
                writer.WriteLine("    <meta charset='utf-8'>");
                writer.WriteLine("    <style>");
                writer.WriteLine("        body { font-family: Arial, sans-serif; max-width: 1200px; margin: 0 auto; padding: 20px; }");
                writer.WriteLine("        h1 { color: #2196F3; text-align: center; border-bottom: 3px solid #2196F3; padding-bottom: 10px; }");
                writer.WriteLine("        h2 { color: #1976D2; margin-top: 40px; padding: 10px; background-color: #E3F2FD; border-left: 5px solid #2196F3; }");
                writer.WriteLine("        table { border-collapse: collapse; width: 100%; margin: 20px 0; }");
                writer.WriteLine("        th { background-color: #2196F3; color: white; padding: 12px; text-align: left; position: sticky; top: 0; }");
                writer.WriteLine("        td { padding: 8px; border-bottom: 1px solid #ddd; }");
                writer.WriteLine("        tr:nth-child(even) { background-color: #f2f2f2; }");
                writer.WriteLine("        tr:hover { background-color: #e3f2fd; }");
                writer.WriteLine("        .winner { background-color: #FFD700 !important; font-weight: bold; }");
                writer.WriteLine("        .member { color: #4CAF50; font-weight: bold; }");
                writer.WriteLine("        .race-info { color: #666; font-style: italic; margin: 10px 0; padding: 10px; background-color: #f5f5f5; border-radius: 5px; }");
                writer.WriteLine("        .filter-info { background-color: #FFF9C4; padding: 10px; margin: 10px 0; border-left: 4px solid #FFC107; }");
                writer.WriteLine("        .summary { background-color: #E8F5E9; padding: 15px; margin: 20px 0; border-left: 4px solid #4CAF50; }");
                writer.WriteLine("        .race-section { margin-bottom: 50px; page-break-after: always; }");
                writer.WriteLine("        .total-summary { font-size: 14px; color: #666; text-align: center; margin-top: 30px; padding: 15px; background-color: #FAFAFA; border-top: 2px solid #2196F3; }");
                writer.WriteLine("    </style>");
                writer.WriteLine("</head>");
                writer.WriteLine("<body>");

                // Write title
                writer.WriteLine($"    <h1>{Localization["MultipleRaceResultsExport"]}</h1>");

                // Show filter info at the top
                if (IsMemberFilter.HasValue || IsChallengerFilter.HasValue)
                {
                    writer.WriteLine($"    <div class='filter-info'>");
                    var filters = new List<string>();
                    if (IsMemberFilter.HasValue)
                    {
                        filters.Add(IsMemberFilter.Value ? Localization["MembersOnly"] : Localization["NonMembersOnly"]);
                    }
                    if (IsChallengerFilter.HasValue)
                    {
                        filters.Add(IsChallengerFilter.Value ? Localization["ChallengersOnly"] : Localization["NonChallengersOnly"]);
                    }
                    writer.WriteLine($"        <strong>⚠️ {Localization["GlobalFilterApplied"]}:</strong> {string.Join(", ", filters)} ({Localization["WinnerAlwaysIncluded"]} in each race)");
                    writer.WriteLine($"    </div>");
                }

                // Summary section
                writer.WriteLine($"    <div class='summary'>");
                writer.WriteLine($"        <strong>📊 {Localization["ExportSummary"]}</strong><br/>");
                writer.WriteLine($"        <strong>{Localization["TotalRaces"]}:</strong> {races.Count}<br/>");
                writer.WriteLine($"        <strong>{Localization["Years"]}:</strong> {string.Join(", ", races.Select(r => r.Year.HasValue ? r.Year.ToString() : "HC").Distinct())}<br/>");
                writer.WriteLine($"        <strong>{Localization["TotalDistance"]}:</strong> {races.Sum(r => r.DistanceKm)} km<br/>");
                writer.WriteLine($"        <strong>{Localization["Exported"]}:</strong> {DateTime.Now:yyyy-MM-dd HH:mm}");
                writer.WriteLine($"    </div>");

                // Process each race
                int totalParticipants = 0;
                foreach (var race in races)
                {
                    var classifications = _classificationRepository.GetClassificationsByRace(race.Id, IsMemberFilter, IsChallengerFilter);
                    totalParticipants += classifications.Count;

                    writer.WriteLine($"    <div class='race-section'>");
                    writer.WriteLine($"        <h2>🏁 {race.Name}</h2>");
                    writer.WriteLine($"        <div class='race-info'>");
                    writer.WriteLine($"            <strong>{Localization["Year"]}:</strong> {(race.Year.HasValue ? race.Year.ToString() : Localization["HorsChallenge"])} | ");
                    writer.WriteLine($"            <strong>{Localization["Distance"]}:</strong> {race.DistanceKm} km | ");
                    writer.WriteLine($"            <strong>{Localization["RaceNumber"]}:</strong> {race.RaceNumber} | ");
                    writer.WriteLine($"            <strong>{Localization["Participants"]}:</strong> {classifications.Count}");
                    writer.WriteLine($"        </div>");

                    // Write table for this race
                    writer.WriteLine("        <table>");
                    writer.WriteLine("            <thead>");
                    writer.WriteLine("                <tr>");
                    writer.WriteLine($"                    <th>{Localization["Rank"]}</th>");
                    writer.WriteLine($"                    <th>{Localization["Position"]}</th>");
                    writer.WriteLine($"                    <th>{Localization["Name"]}</th>");
                    writer.WriteLine($"                    <th>{Localization["Team"]}</th>");
                    writer.WriteLine($"                    <th>{Localization["RaceTime"]}</th>");
                    writer.WriteLine($"                    <th>{Localization["TimePerKm"]}</th>");
                    writer.WriteLine($"                    <th>{Localization["Speed"]}</th>");
                    writer.WriteLine($"                    <th>{Localization["Points"]}</th>");
                    writer.WriteLine($"                    <th>{Localization["BonusKM"]}</th>");
                    writer.WriteLine("                </tr>");
                    writer.WriteLine("            </thead>");
                    writer.WriteLine("            <tbody>");

                    int rank = 1;
                    foreach (var classification in classifications)
                    {
                        string rowClass = classification.Position == 1 ? " class='winner'" : "";
                        string nameClass = classification.IsMember ? " class='member'" : "";
                        string fullName = $"{classification.MemberFirstName} {classification.MemberLastName}";

                        writer.WriteLine($"                <tr{rowClass}>");
                        writer.WriteLine($"                    <td>{rank}</td>");
                        writer.WriteLine($"                    <td>{classification.Position}</td>");
                        writer.WriteLine($"                    <td{nameClass}>{fullName}</td>");
                        writer.WriteLine($"                    <td>{classification.Team ?? "-"}</td>");
                        writer.WriteLine($"                    <td>{FormatTimeSpan(classification.RaceTime)}</td>");
                        writer.WriteLine($"                    <td>{FormatTimeSpan(classification.TimePerKm)}</td>");
                        writer.WriteLine($"                    <td>{(classification.Speed.HasValue ? classification.Speed.Value.ToString("F2") : "-")}</td>");
                        writer.WriteLine($"                    <td><strong>{classification.Points}</strong></td>");
                        writer.WriteLine($"                    <td>{classification.BonusKm}</td>");
                        writer.WriteLine("                </tr>");
                        rank++;
                    }

                    writer.WriteLine("            </tbody>");
                    writer.WriteLine("        </table>");
                    writer.WriteLine("    </div>");
                }

                // Final summary
                writer.WriteLine($"    <div class='total-summary'>");
                writer.WriteLine($"        <strong>{Localization["CompleteExportSummary"]}</strong><br/>");
                writer.WriteLine($"        {Localization["TotalRaces"]}: {races.Count} | {Localization["TotalParticipantsAllRaces"]}: {totalParticipants}<br/>");
                writer.WriteLine($"        {Localization["Generated"]}: {DateTime.Now:yyyy-MM-dd HH:mm}");
                writer.WriteLine($"    </div>");

                writer.WriteLine("</body>");
                writer.WriteLine("</html>");
            }
        }

        private void ExportMultipleToText(string filePath, List<RaceEntity> races)
        {
            using (var writer = new StreamWriter(filePath))
            {
                writer.WriteLine($"╔═══════════════════════════════════════════════════════════════════════════════╗");
                writer.WriteLine($"║                     {Localization["MultipleRaceResultsExport"].ToUpper().PadRight(51)}║");
                writer.WriteLine($"╚═══════════════════════════════════════════════════════════════════════════════╝");
                writer.WriteLine();

                // Summary
                writer.WriteLine($"📊 {Localization["ExportSummary"].ToUpper()}");
                writer.WriteLine($"{Localization["TotalRaces"]}: {races.Count}");
                writer.WriteLine($"{Localization["Years"]}: {string.Join(", ", races.Select(r => r.Year.HasValue ? r.Year.ToString() : "HC").Distinct())}");
                writer.WriteLine($"{Localization["TotalDistance"]}: {races.Sum(r => r.DistanceKm)} km");
                writer.WriteLine($"{Localization["Exported"]}: {DateTime.Now:yyyy-MM-dd HH:mm}");
                writer.WriteLine();

                // Show filter info
                if (IsMemberFilter.HasValue || IsChallengerFilter.HasValue)
                {
                    var filters = new List<string>();
                    if (IsMemberFilter.HasValue)
                    {
                        filters.Add(IsMemberFilter.Value ? Localization["MembersOnly"] : Localization["NonMembersOnly"]);
                    }
                    if (IsChallengerFilter.HasValue)
                    {
                        filters.Add(IsChallengerFilter.Value ? Localization["ChallengersOnly"] : Localization["NonChallengersOnly"]);
                    }
                    writer.WriteLine($"⚠️  {Localization["GlobalFilterApplied"].ToUpper()}: {string.Join(", ", filters)} ({Localization["WinnerAlwaysIncluded"]})");
                    writer.WriteLine();
                }

                writer.WriteLine(new string('═', 120));

                // Process each race
                int totalParticipants = 0;
                int raceCount = 0;
                foreach (var race in races)
                {
                    raceCount++;
                    var classifications = _classificationRepository.GetClassificationsByRace(race.Id, IsMemberFilter, IsChallengerFilter);
                    totalParticipants += classifications.Count;

                    writer.WriteLine();
                    writer.WriteLine($"🏁 {Localization["Race"].ToUpper()} {raceCount}/{races.Count}: {race.Name.ToUpper()}");
                    writer.WriteLine($"{Localization["Year"]}: {(race.Year.HasValue ? race.Year.ToString() : Localization["HorsChallenge"])} | {Localization["Distance"]}: {race.DistanceKm} km | {Localization["Race"]} #{race.RaceNumber} | {Localization["Participants"]}: {classifications.Count}");
                    writer.WriteLine(new string('─', 120));
                    writer.WriteLine($"{Localization["Rank"],-6}│ {Localization["Position"],-5}│ {Localization["Name"],-30}│ {Localization["Team"],-20}│ {Localization["RaceTime"],-10}│ {Localization["TimePerKm"],-8}│ {Localization["Speed"],-8}│ {Localization["Points"],-7}│ {Localization["Bonus"],6}");
                    writer.WriteLine(new string('─', 120));

                    int rank = 1;
                    foreach (var classification in classifications)
                    {
                        string fullName = $"{classification.MemberFirstName} {classification.MemberLastName}";
                        string marker = classification.Position == 1 ? "🏆" : (classification.IsMember ? "✓" : " ");
                        string team = classification.Team ?? "-";
                        string speed = classification.Speed.HasValue ? classification.Speed.Value.ToString("F2") : "-";

                        writer.WriteLine(
                            $"{marker}{rank,-5}│ {classification.Position,-5}│ {fullName,-30}│ {team,-20}│ " +
                            $"{FormatTimeSpan(classification.RaceTime),-10}│ {FormatTimeSpan(classification.TimePerKm),-8}│ " +
                            $"{speed,-8}│ {classification.Points,-7}│ {classification.BonusKm,6}");
                        rank++;
                    }

                    writer.WriteLine(new string('─', 120));

                    if (raceCount < races.Count)
                    {
                        writer.WriteLine();
                        writer.WriteLine(new string('═', 120));
                    }
                }

                // Final summary
                writer.WriteLine();
                writer.WriteLine(new string('═', 120));
                writer.WriteLine();
                writer.WriteLine($"{Localization["CompleteExportSummary"].ToUpper()}");
                writer.WriteLine($"{Localization["TotalRaces"]}: {races.Count} | {Localization["TotalParticipantsAllRaces"]}: {totalParticipants}");
                writer.WriteLine($"{Localization["Generated"]}: {DateTime.Now:yyyy-MM-dd HH:mm}");
                writer.WriteLine();
                writer.WriteLine($"{Localization["Legend"]}: 🏆 = {Localization["Winner"]} | ✓ = {Localization["ClubMember"]}");
            }
        }

        private void ExecuteRefreshRaces(object parameter)
        {
            LoadRaces();
            StatusMessage = "Races refreshed.";
        }

        private bool CanExecuteDeleteRace(object parameter)
        {
            return SelectedRace != null;
        }

        private void ExecuteDeleteRace(object parameter)
        {
            if (SelectedRace == null) return;

            var result = MessageBox.Show(
                $"Are you sure you want to delete race '{SelectedRace.Name}'?\nThis will also delete all associated classifications.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _raceRepository.DeleteRace(SelectedRace.Id);
                    StatusMessage = $"Race '{SelectedRace.Name}' deleted successfully.";
                    LoadRaces();
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error deleting race: {ex.Message}";
                    MessageBox.Show($"Error deleting race: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private bool CanExecuteViewClassification(object parameter)
        {
            return SelectedRace != null && SelectedRace.Status == "Processed";
        }

        private void ExecuteViewClassification(object parameter)
        {
            if (SelectedRace == null) return;

            try
            {
                ShowGeneralClassification = false;
                ShowChallengerClassification = false;

                var classifications = _classificationRepository.GetClassificationsByRace(SelectedRace.Id, IsMemberFilter, IsChallengerFilter);

                Classifications.Clear();
                foreach (var classification in classifications)
                {
                    Classifications.Add(classification);
                }

                string filterText = "";
                if (IsMemberFilter.HasValue || IsChallengerFilter.HasValue)
                {
                    var filters = new List<string>();
                    if (IsMemberFilter.HasValue)
                    {
                        filters.Add(IsMemberFilter.Value ? "members only" : "non-members only");
                    }
                    if (IsChallengerFilter.HasValue)
                    {
                        filters.Add(IsChallengerFilter.Value ? "challengers only" : "non-challengers only");
                    }
                    filterText = $" ({string.Join(", ", filters)}, winner always shown)";
                }
                StatusMessage = $"Loaded {classifications.Count} classifications for race '{SelectedRace.Name}'{filterText}.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading classifications: {ex.Message}";
                MessageBox.Show($"Error loading classifications: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteViewGeneralClassification(object parameter)
        {
            try
            {
                ShowGeneralClassification = true;
                LoadGeneralClassification();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading general classification: {ex.Message}";
                MessageBox.Show($"Error loading general classification: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteShowRaceClassification(object parameter)
        {
            ShowGeneralClassification = false;
            ShowChallengerClassification = false;
            Classifications.Clear();
            GeneralClassifications.Clear();
            ChallengerClassifications.Clear();
            StatusMessage = "Switched to race classification view.";
        }

        private void ExecuteViewChallengerClassification(object parameter)
        {
            try
            {
                ShowGeneralClassification = false;
                ShowChallengerClassification = true;
                LoadChallengerClassification();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading challenger classification: {ex.Message}";
                MessageBox.Show($"Error loading challenger classification: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadChallengerClassification()
        {
            try
            {
                var challengerClassifications = _classificationRepository.GetChallengerClassification(SelectedYear);

                ChallengerClassifications.Clear();
                foreach (var classification in challengerClassifications)
                {
                    ChallengerClassifications.Add(classification);
                }

                StatusMessage = $"Loaded challenger classification for year {SelectedYear} ({challengerClassifications.Count} challengers).";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading challenger classification: {ex.Message}";
                MessageBox.Show($"Error loading challenger classification: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadGeneralClassification()
        {
            try
            {
                var generalClassifications = _classificationRepository.GetGeneralClassification(SelectedYear);

                GeneralClassifications.Clear();
                foreach (var classification in generalClassifications)
                {
                    GeneralClassifications.Add(classification);
                }

                StatusMessage = $"Loaded general classification for year {SelectedYear} ({generalClassifications.Count} members).";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading general classification: {ex.Message}";
                MessageBox.Show($"Error loading general classification: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadRaces()
        {
            try
            {
                var races = _raceRepository.GetAllRaces();
                Races.Clear();
                foreach (var race in races)
                {
                    Races.Add(race);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading races: {ex.Message}";
            }
        }

        private void ClearForm()
        {
            SelectedFilePath = string.Empty;
            RaceName = string.Empty;
            RaceNumber = 1;
            DistanceKm = 10;
        }

        private bool CanExecuteExportChallengerClassification(object parameter)
        {
            return ChallengerClassifications != null && ChallengerClassifications.Count > 0;
        }

        private void ExecuteExportChallengerClassification(object parameter)
        {
            if (ChallengerClassifications == null || ChallengerClassifications.Count == 0) return;

            try
            {
                // Ask user if they want summary or detailed export
                var result = MessageBox.Show(
                    $"{Localization["ExportFormatQuestion"]}\n\n" +
                    $"• {Localization["Yes"]}: {Localization["SummaryFormat"]}\n" +
                    $"• {Localization["No"]}: {Localization["DetailedFormat"]}",
                    Localization["SelectExportFormat"],
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Cancel) return;

                bool exportSummary = (result == MessageBoxResult.Yes);

                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "HTML Files (*.html)|*.html|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                    Title = Localization["ExportResultsForEmail"],
                    FileName = $"Challenger_Classification_{(exportSummary ? "Summary" : "Detailed")}_{SelectedYear}_{DateTime.Now:yyyyMMdd}.html"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var extension = Path.GetExtension(saveFileDialog.FileName).ToLowerInvariant();

                    if (extension == ".html" || extension == ".htm")
                    {
                        ExportChallengerClassificationToHtml(saveFileDialog.FileName, exportSummary);
                    }
                    else
                    {
                        ExportChallengerClassificationToText(saveFileDialog.FileName, exportSummary);
                    }

                    StatusMessage = $"Challenger classification exported successfully to {Path.GetFileName(saveFileDialog.FileName)}";
                    MessageBox.Show($"{Localization["ExportSuccess"]}\n\n{Localization["SavedTo"]}: {saveFileDialog.FileName}", 
                        Localization["ExportComplete"], MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error exporting challenger classification: {ex.Message}";
                MessageBox.Show($"Error exporting challenger classification: {ex.Message}", Localization["Error"], MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportChallengerClassificationToHtml(string filePath, bool exportSummary)
        {
            using (var writer = new StreamWriter(filePath))
            {
                // Write HTML header
                writer.WriteLine("<!DOCTYPE html>");
                writer.WriteLine("<html>");
                writer.WriteLine("<head>");
                writer.WriteLine("    <meta charset='utf-8'>");
                writer.WriteLine($"    <title>{Localization["ChallengerClassification"]} {SelectedYear}</title>");
                writer.WriteLine("    <style>");
                writer.WriteLine("        body { font-family: Arial, sans-serif; margin: 20px; }");
                writer.WriteLine("        h1 { color: #2196F3; }");
                writer.WriteLine("        table { border-collapse: collapse; width: 100%; margin-top: 20px; }");
                writer.WriteLine("        th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
                writer.WriteLine("        th { background-color: #2196F3; color: white; }");
                writer.WriteLine("        tr:nth-child(even) { background-color: #f2f2f2; }");
                writer.WriteLine("        .best-race { background-color: #C8E6C9 !important; font-weight: bold; }");
                writer.WriteLine("        .summary { background-color: #E3F2FD; padding: 15px; margin: 20px 0; border-radius: 5px; }");
                writer.WriteLine("        .race-details { margin-top: 10px; font-size: 0.9em; }");
                writer.WriteLine("    </style>");
                writer.WriteLine("</head>");
                writer.WriteLine("<body>");
                writer.WriteLine($"    <h1>🏆 {Localization["ChallengerClassification"]} {SelectedYear} - {(exportSummary ? Localization["Summary"] : Localization["Detailed"])}</h1>");
                writer.WriteLine($"    <div class='summary'>");
                writer.WriteLine($"        <strong>{Localization["TotalChallengers"]}:</strong> {ChallengerClassifications.Count}<br/>");
                writer.WriteLine($"        <strong>{Localization["Generated"]}:</strong> {DateTime.Now:yyyy-MM-dd HH:mm}<br/>");
                writer.WriteLine($"        <strong>{Localization["Note"]}:</strong> {Localization["TotalPointsFormula"]}");
                writer.WriteLine($"    </div>");

                // Main classification table
                writer.WriteLine("    <table>");
                writer.WriteLine("        <thead>");
                writer.WriteLine("            <tr>");
                writer.WriteLine($"                <th>{Localization["RankPoints"]}</th>");
                writer.WriteLine($"                <th>{Localization["RankKMs"]}</th>");
                writer.WriteLine($"                <th>{Localization["Name"]}</th>");
                writer.WriteLine($"                <th>{Localization["Team"]}</th>");
                writer.WriteLine($"                <th>{Localization["Races"]}</th>");
                writer.WriteLine($"                <th>{Localization["Best7Points"]}</th>");
                writer.WriteLine($"                <th>{Localization["BonusKM"]}</th>");
                writer.WriteLine($"                <th>{Localization["TotalPoints"]}</th>");
                writer.WriteLine($"                <th>{Localization["TotalKMs"]}</th>");
                writer.WriteLine("            </tr>");
                writer.WriteLine("        </thead>");
                writer.WriteLine("        <tbody>");

                foreach (var challenger in ChallengerClassifications)
                {
                    writer.WriteLine("            <tr>");
                    writer.WriteLine($"                <td><strong>{challenger.RankByPoints}</strong></td>");
                    writer.WriteLine($"                <td>{challenger.RankByKms}</td>");
                    writer.WriteLine($"                <td>{challenger.ChallengerFirstName} {challenger.ChallengerLastName}</td>");
                    writer.WriteLine($"                <td>{challenger.Team ?? "-"}</td>");
                    writer.WriteLine($"                <td>{challenger.RaceCount}</td>");
                    writer.WriteLine($"                <td>{challenger.Best7RacesPoints}</td>");
                    writer.WriteLine($"                <td>{challenger.TotalBonusKm}</td>");
                    writer.WriteLine($"                <td><strong>{challenger.TotalPoints}</strong></td>");
                    writer.WriteLine($"                <td>{challenger.TotalKilometers}</td>");
                    writer.WriteLine("            </tr>");

                    // Race by race details (only if detailed export)
                    if (!exportSummary && challenger.RaceDetails.Any())
                    {
                        writer.WriteLine("            <tr>");
                        writer.WriteLine("                <td colspan='9' class='race-details'>");
                        writer.WriteLine($"                    <strong>{Localization["RaceByRace"]}:</strong><br/>");
                        writer.WriteLine("                    <table style='width: 100%; margin-top: 5px;'>");
                        writer.WriteLine("                        <tr style='background-color: #f9f9f9;'>");
                        writer.WriteLine("                            <th style='width: 50px;'>#</th>");
                        writer.WriteLine($"                            <th>{Localization["Race"]}</th>");
                        writer.WriteLine($"                            <th style='width: 80px;'>{Localization["Distance"]}</th>");
                        writer.WriteLine($"                            <th style='width: 80px;'>{Localization["Position"]}</th>");
                        writer.WriteLine($"                            <th style='width: 80px;'>{Localization["Points"]}</th>");
                        writer.WriteLine($"                            <th style='width: 80px;'>{Localization["Bonus"]}</th>");
                        writer.WriteLine("                        </tr>");

                        foreach (var race in challenger.RaceDetails)
                        {
                            var rowClass = race.IsInBest7 ? " class='best-race'" : "";
                            writer.WriteLine($"                        <tr{rowClass}>");
                            writer.WriteLine($"                            <td>{race.RaceNumber}</td>");
                            writer.WriteLine($"                            <td>{race.RaceName}</td>");
                            writer.WriteLine($"                            <td>{race.DistanceKm} km</td>");
                            writer.WriteLine($"                            <td>{race.Position?.ToString() ?? "-"}</td>");
                            writer.WriteLine($"                            <td><strong>{race.Points}</strong></td>");
                            writer.WriteLine($"                            <td>{race.BonusKm}</td>");
                            writer.WriteLine("                        </tr>");
                        }

                        writer.WriteLine("                    </table>");
                        writer.WriteLine("                </td>");
                        writer.WriteLine("            </tr>");
                    }
                }

                writer.WriteLine("        </tbody>");
                writer.WriteLine("    </table>");

                if (!exportSummary)
                {
                    writer.WriteLine($"    <div style='margin-top: 20px; color: #666;'>");
                    writer.WriteLine($"        <em>{Localization["GreenRowsLegend"]}</em>");
                    writer.WriteLine($"    </div>");
                }

                writer.WriteLine("</body>");
                writer.WriteLine("</html>");
            }
        }

        private void ExportChallengerClassificationToText(string filePath, bool exportSummary)
        {
            using (var writer = new StreamWriter(filePath))
            {
                writer.WriteLine($"╔═══════════════════════════════════════════════════════════════════════════════╗");
                writer.WriteLine($"║          {Localization["ChallengerClassification"].ToUpper().PadRight(67)}║");
                writer.WriteLine($"║                           {SelectedYear}                                              ║");
                writer.WriteLine($"║          {(exportSummary ? Localization["Summary"] : Localization["Detailed"]).ToUpper().PadRight(67)}║");
                writer.WriteLine($"╚═══════════════════════════════════════════════════════════════════════════════╝");
                writer.WriteLine();
                writer.WriteLine($"{Localization["TotalChallengers"]}: {ChallengerClassifications.Count}");
                writer.WriteLine($"{Localization["Generated"]}: {DateTime.Now:yyyy-MM-dd HH:mm}");
                writer.WriteLine($"{Localization["Note"]}: {Localization["TotalPointsFormula"]}");
                writer.WriteLine();
                writer.WriteLine(new string('═', 120));
                writer.WriteLine($"{Localization["Rank"],-6}│{Localization["Rank"],-6}│{Localization["Name"],-30}│{Localization["Team"],-20}│{Localization["Races"],-7}│{Localization["Best7"],-7}│{Localization["Bonus"],-7}│{Localization["Total"],-7}│{Localization["TotalKMs"],-7}");
                writer.WriteLine($"{Localization["Points"],-6}│{"KMs",-6}│{"",-30}│{"",-20}│{"",-7}│{Localization["Points"],-7}│{"KM",-7}│{Localization["Points"],-7}│{"",-7}");
                writer.WriteLine(new string('─', 120));

                foreach (var challenger in ChallengerClassifications)
                {
                    writer.WriteLine(
                        $"{challenger.RankByPoints,-6}│" +
                        $"{challenger.RankByKms,-6}│" +
                        $"{(challenger.ChallengerFirstName + " " + challenger.ChallengerLastName),-30}│" +
                        $"{(challenger.Team ?? "-"),-20}│" +
                        $"{challenger.RaceCount,-7}│" +
                        $"{challenger.Best7RacesPoints,-7}│" +
                        $"{challenger.TotalBonusKm,-7}│" +
                        $"{challenger.TotalPoints,-7}│" +
                        $"{challenger.TotalKilometers,-7}");

                    // Race by race details (only if detailed export)
                    if (!exportSummary && challenger.RaceDetails.Any())
                    {
                        writer.WriteLine($"  └─ {Localization["RaceByRace"]}:");
                        writer.WriteLine($"     {"#",-4}│{Localization["Race"],-30}│{Localization["Distance"],-6}│{Localization["Position"],-5}│{Localization["Points"],-7}│{Localization["Bonus"],-6}│{Localization["Best7"]}");
                        writer.WriteLine($"     {new string('─', 70)}");

                        foreach (var race in challenger.RaceDetails)
                        {
                            var marker = race.IsInBest7 ? "★" : " ";
                            writer.WriteLine(
                                $"     {race.RaceNumber,-4}│" +
                                $"{race.RaceName,-30}│" +
                                $"{race.DistanceKm + " km",-6}│" +
                                $"{race.Position?.ToString() ?? "-",-5}│" +
                                $"{race.Points,-7}│" +
                                $"{race.BonusKm,-6}│" +
                                $"{marker}");
                        }
                        writer.WriteLine();
                    }
                }

                writer.WriteLine(new string('═', 120));

                if (!exportSummary)
                {
                    writer.WriteLine();
                    writer.WriteLine($"{Localization["Legend"]}: ★ = {Localization["Best7CountedRaces"]}");
                }
            }
        }

        private void ExecuteShowOnlyMembers(object parameter)
        {
            IsMemberFilter = true;
        }

        private void ExecuteShowOnlyNonMembers(object parameter)
        {
            IsMemberFilter = false;
        }

        private void ExecuteShowAll(object parameter)
        {
            IsMemberFilter = null;
            IsChallengerFilter = null;
        }

        private void ExecuteShowOnlyChallengers(object parameter)
        {
            IsChallengerFilter = true;
        }

        private void ExecuteShowOnlyNonChallengers(object parameter)
        {
            IsChallengerFilter = false;
        }
    }

    public class LanguageOption
    {
        public string Code { get; set; }
        public string DisplayName { get; set; }
    }
}
