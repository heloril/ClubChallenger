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
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace NameParser.UI.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly RaceRepository _raceRepository;
        private readonly ClassificationRepository _classificationRepository;
        private readonly RaceEventRepository _raceEventRepository;
        private readonly ChallengeRepository _challengeRepository;
        private readonly FacebookService _facebookService;
        private string _selectedFilePath;
        private string _raceName;
        private int _raceNumber;
        private int _year;
        private int _distanceKm;
        private string _statusMessage;
        private bool _isProcessing;
        private RaceEntity _selectedRace;
        private List<object> _selectedRaces;
        private RaceEventEntity _selectedRaceEventForClassification;
        private ObservableCollection<RaceEntity> _racesInSelectedEvent;
        private bool _showChallengerClassification;
        private int _selectedYear;
        private ChallengeEntity _selectedChallengeForClassification;
        private bool _isHorsChallenge;
        private bool? _isMemberFilter;
        private bool? _isChallengerFilter;
        private string _selectedLanguage;
        private RaceEventEntity _selectedRaceEvent;
        private RaceEventEntity _selectedUploadRaceEvent;
        private RaceDistanceUploadModel _selectedDistanceUpload;
        private int _nextRaceNumber = 1;

        public MainViewModel()
        {
            _raceRepository = new RaceRepository();
            _classificationRepository = new ClassificationRepository();
            _raceEventRepository = new RaceEventRepository();
            _challengeRepository = new ChallengeRepository();

            // Initialize Facebook Service
            var fbSettings = new FacebookSettings
            {
                AppId = System.Configuration.ConfigurationManager.AppSettings["Facebook:AppId"] ?? "",
                AppSecret = System.Configuration.ConfigurationManager.AppSettings["Facebook:AppSecret"] ?? "",
                PageId = System.Configuration.ConfigurationManager.AppSettings["Facebook:PageId"] ?? "",
                PageAccessToken = System.Configuration.ConfigurationManager.AppSettings["Facebook:PageAccessToken"] ?? ""
            };
            _facebookService = new FacebookService(fbSettings);

            Year = DateTime.Now.Year;
            SelectedYear = DateTime.Now.Year;
            DistanceKm = 10;
            RaceNumber = 1;

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
            DownloadMultipleResultsCommand = new RelayCommand(ExecuteDownloadMultipleResults, CanExecuteDownloadMultipleResults);
            ExportForEmailCommand = new RelayCommand(ExecuteExportForEmail, CanExecuteExportForEmail);
            ExportMultipleForEmailCommand = new RelayCommand(ExecuteExportMultipleForEmail, CanExecuteExportMultipleForEmail);
            RefreshRacesCommand = new RelayCommand(ExecuteRefreshRaces);
            DeleteRaceCommand = new RelayCommand(ExecuteDeleteRace, CanExecuteDeleteRace);
            ViewClassificationCommand = new RelayCommand(ExecuteViewClassification, CanExecuteViewClassification);
            ViewChallengerClassificationCommand = new RelayCommand(ExecuteViewChallengerClassification);
            ExportChallengerClassificationCommand = new RelayCommand(ExecuteExportChallengerClassification, CanExecuteExportChallengerClassification);

            // Challenger Classification Export commands
            ExportChallengerSummaryHtmlCommand = new RelayCommand(ExecuteExportChallengerSummaryHtml, CanExecuteExportChallengerClassification);
            ExportChallengerSummaryExcelCommand = new RelayCommand(ExecuteExportChallengerSummaryExcel, CanExecuteExportChallengerClassification);
            ExportChallengerSummaryWordCommand = new RelayCommand(ExecuteExportChallengerSummaryWord, CanExecuteExportChallengerClassification);
            ExportChallengerDetailedHtmlCommand = new RelayCommand(ExecuteExportChallengerDetailedHtml, CanExecuteExportChallengerClassification);
            ExportChallengerDetailedExcelCommand = new RelayCommand(ExecuteExportChallengerDetailedExcel, CanExecuteExportChallengerClassification);
            ExportChallengerDetailedWordCommand = new RelayCommand(ExecuteExportChallengerDetailedWord, CanExecuteExportChallengerClassification);

            // Export commands for Race Classification
            ExportToHtmlCommand = new RelayCommand(ExecuteExportToHtml, CanExecuteExport);
            ExportToExcelCommand = new RelayCommand(ExecuteExportToExcel, CanExecuteExport);
            ExportToWordCommand = new RelayCommand(ExecuteExportToWord, CanExecuteExport);
            ExportSummaryCommand = new RelayCommand(ExecuteExportSummary, CanExecuteExport);

            ShowRaceClassificationCommand = new RelayCommand(ExecuteShowRaceClassification);
            ShowOnlyMembersCommand = new RelayCommand(ExecuteShowOnlyMembers);
            ShowOnlyNonMembersCommand = new RelayCommand(ExecuteShowOnlyNonMembers);
            ShowAllCommand = new RelayCommand(ExecuteShowAll);
            ShowOnlyChallengersCommand = new RelayCommand(ExecuteShowOnlyChallengers);
            ShowOnlyNonChallengersCommand = new RelayCommand(ExecuteShowOnlyNonChallengers);
            ShareRaceToFacebookCommand = new RelayCommand(ExecuteShareRaceToFacebook, CanExecuteShareRaceToFacebook);
            ShareChallengeToFacebookCommand = new RelayCommand(ExecuteShareChallengeToFacebook, CanExecuteShareChallengeToFacebook);
            BrowseDistanceFileCommand = new RelayCommand<RaceDistanceUploadModel>(ExecuteBrowseDistanceFile);
            ProcessAllDistancesCommand = new RelayCommand(ExecuteProcessAllDistances, CanExecuteProcessAllDistances);

            Years = new ObservableCollection<int>();
            for (int i = 2020; i <= 2030; i++)
            {
                Years.Add(i);
            }

            Races = new ObservableCollection<RaceEntity>();
            Classifications = new ObservableCollection<ClassificationEntity>();
            ChallengerClassifications = new ObservableCollection<ChallengerClassificationDto>();
            ChallengesForClassification = new ObservableCollection<ChallengeEntity>();
            RaceEventsForSelection = new ObservableCollection<RaceEventEntity>();
            AvailableDistancesForUpload = new ObservableCollection<RaceDistanceUploadModel>();
            RacesInSelectedEvent = new ObservableCollection<RaceEntity>();

            // Initialize child ViewModels for new tabs
            ChallengeManagementViewModel = new ChallengeManagementViewModel();
            RaceEventManagementViewModel = new RaceEventManagementViewModel();
            ChallengeCalendarViewModel = new ChallengeCalendarViewModel();
            ChallengeMailingViewModel = new ChallengeMailingViewModel();

            LoadRaces();
            LoadRaceEventsForSelection();
            LoadChallengesForClassification();
        }

        public ObservableCollection<int> Years { get; }
        public ObservableCollection<RaceEntity> Races { get; }
        public ObservableCollection<ClassificationEntity> Classifications { get; }
        public ObservableCollection<ChallengerClassificationDto> ChallengerClassifications { get; }
        public ObservableCollection<ChallengeEntity> ChallengesForClassification { get; }
        public ObservableCollection<RaceEventEntity> RaceEventsForSelection { get; }
        public ObservableCollection<RaceDistanceUploadModel> AvailableDistancesForUpload { get; }
        public ObservableCollection<RaceEntity> RacesInSelectedEvent { get; private set; }

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
                ((RelayCommand)DownloadMultipleResultsCommand).RaiseCanExecuteChanged();
            }
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
            }
        }

        public ChallengeEntity SelectedChallengeForClassification
        {
            get => _selectedChallengeForClassification;
            set
            {
                if (SetProperty(ref _selectedChallengeForClassification, value))
                {
                    if (ShowChallengerClassification && value != null)
                    {
                        LoadChallengerClassification();
                    }
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
                if (SelectedRaceEventForClassification != null)
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
                if (SelectedRaceEventForClassification != null)
                {
                    ExecuteViewClassification(null);
                }
            }
        }

        public RaceEventEntity SelectedRaceEventForClassification
        {
            get => _selectedRaceEventForClassification;
            set
            {
                if (SetProperty(ref _selectedRaceEventForClassification, value))
                {
                    LoadRacesForSelectedEvent();
                    ((RelayCommand)ViewClassificationCommand)?.RaiseCanExecuteChanged();
                    ((RelayCommand)ReprocessRaceCommand)?.RaiseCanExecuteChanged();
                    ((RelayCommand)ExportForEmailCommand)?.RaiseCanExecuteChanged();
                    ((RelayCommand)ShareRaceToFacebookCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public RaceEventEntity SelectedRaceEvent
        {
            get => _selectedRaceEvent;
            set => SetProperty(ref _selectedRaceEvent, value);
        }

        public RaceEventEntity SelectedUploadRaceEvent
        {
            get => _selectedUploadRaceEvent;
            set
            {
                if (SetProperty(ref _selectedUploadRaceEvent, value))
                {
                    LoadAvailableDistancesForUpload();
                    ((RelayCommand)ProcessAllDistancesCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public RaceDistanceUploadModel SelectedDistanceUpload
        {
            get => _selectedDistanceUpload;
            set => SetProperty(ref _selectedDistanceUpload, value);
        }

        public ICommand UploadFileCommand { get; }
        public ICommand ProcessRaceCommand { get; }
        public ICommand ReprocessRaceCommand { get; }
        public ICommand DownloadMultipleResultsCommand { get; }
        public ICommand ExportForEmailCommand { get; }
        public ICommand ExportMultipleForEmailCommand { get; }
        public ICommand RefreshRacesCommand { get; }
        public ICommand DeleteRaceCommand { get; }
        public ICommand ViewClassificationCommand { get; }
        public ICommand ViewChallengerClassificationCommand { get; }
        public ICommand ExportChallengerClassificationCommand { get; }

        // Challenger Classification Export commands
        public ICommand ExportChallengerSummaryHtmlCommand { get; }
        public ICommand ExportChallengerSummaryExcelCommand { get; }
        public ICommand ExportChallengerSummaryWordCommand { get; }
        public ICommand ExportChallengerDetailedHtmlCommand { get; }
        public ICommand ExportChallengerDetailedExcelCommand { get; }
        public ICommand ExportChallengerDetailedWordCommand { get; }

        public ICommand ShowRaceClassificationCommand { get; }
        public ICommand ShowOnlyMembersCommand { get; }
        public ICommand ShowOnlyNonMembersCommand { get; }
        public ICommand ShowAllCommand { get; }
        public ICommand ShowOnlyChallengersCommand { get; }
        public ICommand ShowOnlyNonChallengersCommand { get; }
        public ICommand ShareRaceToFacebookCommand { get; }
        public ICommand ShareChallengeToFacebookCommand { get; }
        public ICommand BrowseDistanceFileCommand { get; }
        public ICommand ProcessAllDistancesCommand { get; }

        // Export commands for Race Classification
        public ICommand ExportToHtmlCommand { get; }
        public ICommand ExportToExcelCommand { get; }
        public ICommand ExportToWordCommand { get; }
        public ICommand ExportSummaryCommand { get; }

        // Child ViewModels for new tabs
        public ChallengeManagementViewModel ChallengeManagementViewModel { get; }
        public RaceEventManagementViewModel RaceEventManagementViewModel { get; }
        public ChallengeCalendarViewModel ChallengeCalendarViewModel { get; }
        public ChallengeMailingViewModel ChallengeMailingViewModel { get; }

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
                    var raceDistance = new RaceDistance(RaceNumber, RaceName, DistanceKm);

                    // Save race with nullable year for hors challenge
                    int? yearToSave = IsHorsChallenge ? null : (int?)Year;
                    int? raceEventId = SelectedRaceEvent?.Id;
                    _raceRepository.SaveRace(raceDistance, yearToSave, SelectedFilePath, IsHorsChallenge, raceEventId);

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

                    // Pass the race distance object with correct distance (from UI input) instead of parsing from filename
                    var classification = raceProcessingService.ProcessRaceWithMembers(SelectedFilePath, raceDistance, allMembers);

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
            return !IsProcessing && SelectedRaceEventForClassification != null && 
                   RacesInSelectedEvent.Any(r => r.FileContent != null && r.FileContent.Length > 0);
        }

        private async void ExecuteReprocessRace(object parameter)
        {
            if (SelectedRaceEventForClassification == null) return;

            var racesToReprocess = RacesInSelectedEvent.Where(r => r.FileContent != null && r.FileContent.Length > 0).ToList();

            if (!racesToReprocess.Any())
            {
                MessageBox.Show("No races with stored files found in this event.", "No Races", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to reprocess all {racesToReprocess.Count} race(s) in event '{SelectedRaceEventForClassification.Name}'?\n\n" +
                $"This will delete existing classifications and reprocess from the stored files.\n\n" +
                $"Races: {string.Join(", ", racesToReprocess.Select(r => $"{r.DistanceKm}km"))}",
                "Confirm Reprocess",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            IsProcessing = true;
            StatusMessage = "Reprocessing races...";

            int successCount = 0;
            int failCount = 0;
            var errors = new List<string>();

            try
            {
                foreach (var race in racesToReprocess)
                {
                    try
                    {
                        await System.Threading.Tasks.Task.Run(() =>
                        {
                            // Check if the stored file content exists
                            if (race.FileContent == null || race.FileContent.Length == 0)
                            {
                                throw new InvalidOperationException($"No file content stored in database for {race.DistanceKm}km race.");
                            }

                            string tempFilePath = null;
                            try
                            {
                                // Write file content from database to temporary file for processing
                                var fileStorageService = new FileStorageService();
                                tempFilePath = fileStorageService.WriteToTempFile(race.FileContent, race.FileName);

                                // Delete existing classifications for this race
                                _classificationRepository.DeleteClassificationsByRace(race.Id);

                                // Create race distance object from stored data
                                var raceDistance = new RaceDistance(race.RaceNumber, race.Name, race.DistanceKm);

                                // Get members
                                var memberRepository = new JsonMemberRepository("Members.json");
                                var challengerRepository = new JsonMemberRepository("Challenge.json");
                                var memberService = new MemberService(memberRepository, challengerRepository);
                                var allMembers = memberService.GetAllMembersAndChallengers();

                                // Select appropriate parser based on file extension
                                var extension = race.FileExtension.ToLowerInvariant();
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
                                var classification = raceProcessingService.ProcessRaceWithMembers(tempFilePath, raceDistance, allMembers);

                                // Save new classifications
                                _classificationRepository.SaveClassifications(race.Id, classification);

                                // Update race status
                                _raceRepository.UpdateRaceStatus(race.Id, "Processed");
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

                        successCount++;
                        StatusMessage = $"Reprocessed {successCount}/{racesToReprocess.Count} races...";
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        errors.Add($"{race.DistanceKm}km: {ex.Message}");
                    }
                }

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    LoadRaces();
                    LoadRacesForSelectedEvent();

                    if (failCount == 0)
                    {
                        StatusMessage = $"All {successCount} race(s) reprocessed successfully!";
                        MessageBox.Show($"All races reprocessed successfully!\n\nProcessed: {successCount} race(s)", 
                            "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        StatusMessage = $"Reprocessed {successCount} race(s), {failCount} failed.";
                        MessageBox.Show(
                            $"Reprocessing completed with errors:\n\n" +
                            $"Successful: {successCount}\n" +
                            $"Failed: {failCount}\n\n" +
                            $"Errors:\n" + string.Join("\n", errors),
                            "Partial Success",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusMessage = $"Error: {ex.Message}";
                    MessageBox.Show($"Error reprocessing races: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private bool CanExecuteExportForEmail(object parameter)
        {
            return SelectedRaceEventForClassification != null && 
                   RacesInSelectedEvent.Any(r => r.Status == "Processed");
        }

        private bool CanExecuteDownloadMultipleResults(object parameter)
        {
            return SelectedRaces != null && 
                   SelectedRaces.Count > 0 && 
                   SelectedRaces.OfType<RaceEntity>().All(r => r.Status == "Processed");
        }

        private void ExecuteDownloadMultipleResults(object parameter)
        {
            if (SelectedRaces == null || SelectedRaces.Count == 0) return;

            var races = SelectedRaces.OfType<RaceEntity>().OrderBy(r => r.Year).ThenBy(r => r.RaceNumber).ToList();

            try
            {
                // Ask user to select export format
                var formatDialog = new Window
                {
                    Title = "Select Export Format",
                    Width = 400,
                    Height = 250,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.NoResize
                };

                var panel = new System.Windows.Controls.StackPanel { Margin = new Thickness(20) };

                panel.Children.Add(new System.Windows.Controls.TextBlock 
                { 
                    Text = "Choose the export format for the race results:",
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 0, 15),
                    TextWrapping = TextWrapping.Wrap
                });

                var docxButton = new System.Windows.Controls.Button 
                { 
                    Content = "Word Document (.docx) - Formatted Document", 
                    Margin = new Thickness(0, 5, 0, 5),
                    Padding = new Thickness(10, 8, 10, 8),
                    Tag = "docx",
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(33, 150, 243)),
                    Foreground = System.Windows.Media.Brushes.White
                };

                var csvButton = new System.Windows.Controls.Button 
                { 
                    Content = "CSV File (.csv) - Excel Compatible", 
                    Margin = new Thickness(0, 5, 0, 5),
                    Padding = new Thickness(10, 8, 10, 8),
                    Tag = "csv"
                };

                var txtButton = new System.Windows.Controls.Button 
                { 
                    Content = "Text File (.txt) - Plain Text", 
                    Margin = new Thickness(0, 5, 0, 5),
                    Padding = new Thickness(10, 8, 10, 8),
                    Tag = "txt"
                };

                var cancelButton = new System.Windows.Controls.Button 
                { 
                    Content = "Cancel", 
                    Margin = new Thickness(0, 15, 0, 0),
                    Padding = new Thickness(10, 8, 10, 8),
                    Tag = "cancel"
                };

                string selectedFormat = null;

                docxButton.Click += (s, e) => { selectedFormat = "docx"; formatDialog.Close(); };
                csvButton.Click += (s, e) => { selectedFormat = "csv"; formatDialog.Close(); };
                txtButton.Click += (s, e) => { selectedFormat = "txt"; formatDialog.Close(); };
                cancelButton.Click += (s, e) => { formatDialog.Close(); };

                panel.Children.Add(docxButton);
                panel.Children.Add(csvButton);
                panel.Children.Add(txtButton);
                panel.Children.Add(cancelButton);

                formatDialog.Content = panel;
                formatDialog.ShowDialog();

                if (string.IsNullOrEmpty(selectedFormat))
                    return;

                // Set up save file dialog based on format
                var saveFileDialog = new SaveFileDialog
                {
                    Title = "Save Race Results"
                };

                if (selectedFormat == "txt")
                {
                    saveFileDialog.Filter = "Text Files (*.txt)|*.txt";
                    saveFileDialog.FileName = $"Multiple_Races_{races.Count}_races_{DateTime.Now:yyyyMMdd}.txt";
                }
                else if (selectedFormat == "docx")
                {
                    saveFileDialog.Filter = "Word Document (*.docx)|*.docx";
                    saveFileDialog.FileName = $"Multiple_Races_{races.Count}_races_{DateTime.Now:yyyyMMdd}.docx";
                }
                else if (selectedFormat == "csv")
                {
                    saveFileDialog.Filter = "CSV Files (*.csv)|*.csv";
                    saveFileDialog.FileName = $"Multiple_Races_{races.Count}_races_{DateTime.Now:yyyyMMdd}.csv";
                }

                if (saveFileDialog.ShowDialog() == true)
                {
                    if (selectedFormat == "txt")
                    {
                        ExportMultipleRacesForEmail(saveFileDialog.FileName, races);
                    }
                    else if (selectedFormat == "docx")
                    {
                        ExportMultipleRacesToDocx(saveFileDialog.FileName, races);
                    }
                    else if (selectedFormat == "csv")
                    {
                        ExportMultipleRacesToCsv(saveFileDialog.FileName, races);
                    }

                    StatusMessage = $"Exported {races.Count} races to: {saveFileDialog.FileName}";
                    MessageBox.Show($"Successfully exported {races.Count} races!\n\nFile saved to:\n{saveFileDialog.FileName}", 
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error exporting multiple races: {ex.Message}";
                MessageBox.Show($"Error exporting multiple races: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportMultipleRacesForEmail(string filePath, List<RaceEntity> races)
        {
            using (var writer = new StreamWriter(filePath))
            {
                writer.WriteLine(new string('=', 80));
                writer.WriteLine($"                     {Localization["MultipleRaceResultsExport"].ToUpper()}");
                writer.WriteLine(new string('=', 80));
                writer.WriteLine();

                // Summary
                writer.WriteLine(Localization["ExportSummary"].ToUpper());
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
                    writer.WriteLine($"** {Localization["FilterApplied"].ToUpper()}: {string.Join(", ", filters)} ({Localization["WinnerAlwaysIncluded"]})");
                    writer.WriteLine();
                }

                writer.WriteLine(new string('=', 150));

                // Process each race
                int totalParticipants = 0;
                int raceCount = 0;
                foreach (var race in races)
                {
                    raceCount++;
                    var classifications = _classificationRepository.GetClassificationsByRace(race.Id, IsMemberFilter, IsChallengerFilter);
                    totalParticipants += classifications.Count;

                    writer.WriteLine();
                    writer.WriteLine($"{Localization["Race"].ToUpper()} {raceCount}/{races.Count}: {race.Name.ToUpper()}");
                    writer.WriteLine($"{Localization["Year"]}: {(race.Year.HasValue ? race.Year.ToString() : Localization["HorsChallenge"])} | {Localization["Distance"]}: {race.DistanceKm} km | {Localization["Race"]} #{race.RaceNumber} | {Localization["Participants"]}: {classifications.Count}");
                    writer.WriteLine(new string('-', 150));

                    // Build dynamic header based on available data
                    var headerParts = new List<string>();
                    headerParts.Add($"{Localization["Rank"],-7}| ");
                    headerParts.Add($"{Localization["Position"],-5}| ");
                    headerParts.Add($"{Localization["Name"],-30}| ");

                    // Check if any classification has these optional fields
                    bool hasTeam = classifications.Any(c => !string.IsNullOrWhiteSpace(c.Team));
                    bool hasRaceTime = classifications.Any(c => c.RaceTime.HasValue);
                    bool hasTimePerKm = classifications.Any(c => c.TimePerKm.HasValue);
                    bool hasSpeed = classifications.Any(c => c.Speed.HasValue);
                    bool hasSex = classifications.Any(c => !string.IsNullOrWhiteSpace(c.Sex));
                    bool hasPositionBySex = classifications.Any(c => c.PositionBySex.HasValue);
                    bool hasAgeCategory = classifications.Any(c => !string.IsNullOrWhiteSpace(c.AgeCategory));
                    bool hasPositionByCategory = classifications.Any(c => c.PositionByCategory.HasValue);

                    if (hasTeam) headerParts.Add($"{Localization["Team"],-20}| ");
                    if (hasRaceTime) headerParts.Add($"{Localization["RaceTime"],-10}| ");
                    if (hasTimePerKm) headerParts.Add($"{Localization["TimePerKm"],-8}| ");
                    if (hasSpeed) headerParts.Add($"{Localization["Speed"],-8}| ");
                    if (hasSex) headerParts.Add($"{Localization["Sex"],-5}| ");
                    if (hasPositionBySex) headerParts.Add($"{Localization["PositionBySex"],-6}| ");
                    if (hasAgeCategory) headerParts.Add($"{Localization["AgeCategory"],-15}| ");
                    if (hasPositionByCategory) headerParts.Add($"{Localization["PositionByCategory"],-6}");

                    writer.WriteLine(string.Join("", headerParts));
                    writer.WriteLine(new string('-', 150));

                    int rank = 1;
                    foreach (var classification in classifications)
                    {
                        var rowParts = new List<string>();
                        string marker = classification.Position == 1 ? "[W]" : (classification.IsMember ? "[M]" : "   ");
                        rowParts.Add($"{marker}{rank,-4}| ");
                        rowParts.Add($"{classification.Position,-5}| ");

                        string fullName = $"{classification.MemberFirstName} {classification.MemberLastName}";
                        rowParts.Add($"{fullName,-30}| ");

                        if (hasTeam) rowParts.Add($"{(classification.Team ?? "-"),-20}| ");
                        if (hasRaceTime) rowParts.Add($"{(classification.RaceTime.HasValue ? FormatTimeSpan(classification.RaceTime) : "-"),-10}| ");
                        if (hasTimePerKm) rowParts.Add($"{(classification.TimePerKm.HasValue ? FormatTimeSpan(classification.TimePerKm) : "-"),-8}| ");
                        if (hasSpeed) rowParts.Add($"{(classification.Speed.HasValue ? classification.Speed.Value.ToString("F2") : "-"),-8}| ");
                        if (hasSex) rowParts.Add($"{(classification.Sex ?? "-"),-5}| ");
                        if (hasPositionBySex) rowParts.Add($"{(classification.PositionBySex?.ToString() ?? "-"),-6}| ");
                        if (hasAgeCategory) rowParts.Add($"{(classification.AgeCategory ?? "-"),-15}| ");
                        if (hasPositionByCategory) rowParts.Add($"{(classification.PositionByCategory?.ToString() ?? "-"),-6}");

                        writer.WriteLine(string.Join("", rowParts));
                        rank++;
                    }

                    writer.WriteLine(new string('-', 150));

                    if (raceCount < races.Count)
                    {
                        writer.WriteLine();
                        writer.WriteLine(new string('=', 150));
                    }
                }

                // Final summary
                writer.WriteLine();
                writer.WriteLine(new string('=', 150));
                writer.WriteLine();
                writer.WriteLine(Localization["CompleteExportSummary"].ToUpper());
                writer.WriteLine($"{Localization["TotalRaces"]}: {races.Count} | {Localization["TotalParticipantsAllRaces"]}: {totalParticipants}");
                writer.WriteLine($"{Localization["Generated"]}: {DateTime.Now:yyyy-MM-dd HH:mm}");
                writer.WriteLine();
                writer.WriteLine($"{Localization["Legend"]}: [W] = {Localization["Winner"]} | [M] = {Localization["ClubMember"]}");
            }
        }

        private void ExportMultipleRacesToCsv(string filePath, List<RaceEntity> races)
        {
            using (var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8))
            {
                // Process each race
                foreach (var race in races)
                {
                    var classifications = _classificationRepository.GetClassificationsByRace(race.Id, IsMemberFilter, IsChallengerFilter);

                    // Race header section
                    writer.WriteLine($"\"{Localization["Race"]}\",\"{race.Name}\"");
                    writer.WriteLine($"\"{Localization["Year"]}\",\"{(race.Year.HasValue ? race.Year.ToString() : Localization["HorsChallenge"])}\"");
                    writer.WriteLine($"\"{Localization["Distance"]}\",\"{race.DistanceKm} km\"");
                    writer.WriteLine($"\"{Localization["RaceNumber"]}\",\"{race.RaceNumber}\"");
                    writer.WriteLine($"\"{Localization["Participants"]}\",\"{classifications.Count}\"");
                    writer.WriteLine(); // Empty line

                    // Check if any classification has these optional fields
                    bool hasTeam = classifications.Any(c => !string.IsNullOrWhiteSpace(c.Team));
                    bool hasRaceTime = classifications.Any(c => c.RaceTime.HasValue);
                    bool hasTimePerKm = classifications.Any(c => c.TimePerKm.HasValue);
                    bool hasSpeed = classifications.Any(c => c.Speed.HasValue);
                    bool hasSex = classifications.Any(c => !string.IsNullOrWhiteSpace(c.Sex));
                    bool hasPositionBySex = classifications.Any(c => c.PositionBySex.HasValue);
                    bool hasAgeCategory = classifications.Any(c => !string.IsNullOrWhiteSpace(c.AgeCategory));
                    bool hasPositionByCategory = classifications.Any(c => c.PositionByCategory.HasValue);

                    // CSV Header with all available columns
                    var headers = new List<string> { Localization["Rank"], Localization["Position"], Localization["FirstName"], Localization["LastName"] };
                    if (hasTeam) headers.Add(Localization["Team"]);
                    if (hasRaceTime) headers.Add(Localization["RaceTime"]);
                    if (hasTimePerKm) headers.Add(Localization["TimePerKm"]);
                    if (hasSpeed) headers.Add(Localization["Speed"]);
                    if (hasSex) headers.Add(Localization["Sex"]);
                    if (hasPositionBySex) headers.Add(Localization["PositionBySex"]);
                    if (hasAgeCategory) headers.Add(Localization["AgeCategory"]);
                    if (hasPositionByCategory) headers.Add(Localization["PositionByCategory"]);

                    writer.WriteLine(string.Join(",", headers.Select(h => $"\"{h}\"")));

                    // Data rows
                    int rank = 1;
                    foreach (var classification in classifications)
                    {
                        var row = new List<string>
                        {
                            rank.ToString(),
                            classification.Position.ToString(),
                            $"\"{classification.MemberFirstName}\"",
                            $"\"{classification.MemberLastName}\""
                        };

                        if (hasTeam) row.Add($"\"{classification.Team ?? ""}\"");
                        if (hasRaceTime) row.Add($"\"{(classification.RaceTime.HasValue ? FormatTimeSpan(classification.RaceTime) : "")}\"");
                        if (hasTimePerKm) row.Add($"\"{(classification.TimePerKm.HasValue ? FormatTimeSpan(classification.TimePerKm) : "")}\"");
                        if (hasSpeed) row.Add(classification.Speed.HasValue ? classification.Speed.Value.ToString("F2") : "");
                        if (hasSex) row.Add($"\"{classification.Sex ?? ""}\"");
                        if (hasPositionBySex) row.Add(classification.PositionBySex?.ToString() ?? "");
                        if (hasAgeCategory) row.Add($"\"{classification.AgeCategory ?? ""}\"");
                        if (hasPositionByCategory) row.Add(classification.PositionByCategory?.ToString() ?? "");

                        writer.WriteLine(string.Join(",", row));
                        rank++;
                    }

                    // Add spacing between races
                    writer.WriteLine();
                    writer.WriteLine();
                }

                // Summary section at the end
                writer.WriteLine($"\"{Localization["ExportSummary"].ToUpper()}\"");
                writer.WriteLine($"\"{Localization["TotalRaces"]}\",\"{races.Count}\"");
                writer.WriteLine($"\"{Localization["TotalParticipantsAllRaces"]}\",\"{races.Sum(r => _classificationRepository.GetClassificationsByRace(r.Id, IsMemberFilter, IsChallengerFilter).Count)}\"");
                writer.WriteLine($"\"{Localization["Generated"]}\",\"{DateTime.Now:yyyy-MM-dd HH:mm}\"");
            }
        }

        private void ExportMultipleRacesToDocx(string filePath, List<RaceEntity> races)
        {
            using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
            {
                // Add a main document part
                MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();
                mainPart.Document = new Document();
                Body body = mainPart.Document.AppendChild(new Body());

                // Title
                Paragraph title = body.AppendChild(new Paragraph());
                Run titleRun = title.AppendChild(new Run());
                titleRun.AppendChild(new Text(Localization["MultipleRaceResultsExport"].ToUpper()));
                ApplyParagraphFormatting(title, true, true, 32);

                // Date
                Paragraph date = body.AppendChild(new Paragraph());
                Run dateRun = date.AppendChild(new Run());
                dateRun.AppendChild(new Text(DateTime.Now.ToString("yyyy-MM-dd HH:mm")));
                ApplyParagraphFormatting(date, false, true, 24);

                // Summary Box
                body.AppendChild(CreateSummaryParagraph(Localization["ExportSummary"]));
                body.AppendChild(CreateSummaryParagraph($"{Localization["TotalRaces"]}: {races.Count}"));
                body.AppendChild(CreateSummaryParagraph($"{Localization["Years"]}: {string.Join(", ", races.Select(r => r.Year.HasValue ? r.Year.ToString() : "HC").Distinct())}"));
                body.AppendChild(CreateSummaryParagraph($"{Localization["TotalDistance"]}: {races.Sum(r => r.DistanceKm)} km"));

                // Filter info
                if (IsMemberFilter.HasValue || IsChallengerFilter.HasValue)
                {
                    var filters = new List<string>();
                    if (IsMemberFilter.HasValue)
                        filters.Add(IsMemberFilter.Value ? Localization["MembersOnly"] : Localization["NonMembersOnly"]);
                    if (IsChallengerFilter.HasValue)
                        filters.Add(IsChallengerFilter.Value ? Localization["ChallengersOnly"] : Localization["NonChallengersOnly"]);

                    body.AppendChild(CreateSummaryParagraph($"{Localization["FilterApplied"]}: {string.Join(", ", filters)} ({Localization["WinnerAlwaysIncluded"]})"));
                }

                body.AppendChild(new Paragraph()); // Empty line

                // Process each race
                int raceCount = 0;
                int totalParticipants = 0;
                foreach (var race in races)
                {
                    raceCount++;
                    var classifications = _classificationRepository.GetClassificationsByRace(race.Id, IsMemberFilter, IsChallengerFilter);
                    totalParticipants += classifications.Count;

                    // Add page break if not first race
                    if (raceCount > 1)
                    {
                        Paragraph pageBreak = body.AppendChild(new Paragraph());
                        pageBreak.AppendChild(new Run(new Break() { Type = BreakValues.Page }));
                    }

                    // Race Header
                    Paragraph raceHeader = body.AppendChild(new Paragraph());
                    Run raceHeaderRun = raceHeader.AppendChild(new Run());
                    raceHeaderRun.AppendChild(new Text($"{Localization["Race"]} {raceCount}/{races.Count}: {race.Name.ToUpper()}"));
                    ApplyParagraphFormatting(raceHeader, true, false, 28);
                    ApplyRunColor(raceHeaderRun, "2196F3"); // Blue

                    // Race Info
                    Paragraph raceInfo = body.AppendChild(new Paragraph());
                    Run raceInfoRun = raceInfo.AppendChild(new Run());
                    raceInfoRun.AppendChild(new Text($"{Localization["Year"]}: {(race.Year.HasValue ? race.Year.ToString() : Localization["HorsChallenge"])} | {Localization["Distance"]}: {race.DistanceKm} km | {Localization["Race"]} #{race.RaceNumber} | {Localization["Participants"]}: {classifications.Count}"));

                    body.AppendChild(new Paragraph()); // Empty line

                    // Check for optional fields
                    bool hasTeam = classifications.Any(c => !string.IsNullOrWhiteSpace(c.Team));
                    bool hasRaceTime = classifications.Any(c => c.RaceTime.HasValue);
                    bool hasTimePerKm = classifications.Any(c => c.TimePerKm.HasValue);
                    bool hasSpeed = classifications.Any(c => c.Speed.HasValue);
                    bool hasSex = classifications.Any(c => !string.IsNullOrWhiteSpace(c.Sex));
                    bool hasPositionBySex = classifications.Any(c => c.PositionBySex.HasValue);
                    bool hasAgeCategory = classifications.Any(c => !string.IsNullOrWhiteSpace(c.AgeCategory));
                    bool hasPositionByCategory = classifications.Any(c => c.PositionByCategory.HasValue);

                    // Create table
                    Table table = new Table();

                    // Table properties
                    TableProperties tblProps = new TableProperties(
                        new TableBorders(
                            new TopBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                            new BottomBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                            new LeftBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                            new RightBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                            new InsideHorizontalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                            new InsideVerticalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 }),
                        new TableWidth() { Width = "5000", Type = TableWidthUnitValues.Pct });
                    table.AppendChild(tblProps);

                    // Table header row
                    TableRow headerRow = new TableRow();
                    headerRow.Append(CreateHeaderCell(Localization["Rank"]));
                    headerRow.Append(CreateHeaderCell(Localization["Position"]));
                    headerRow.Append(CreateHeaderCell(Localization["FirstName"]));
                    headerRow.Append(CreateHeaderCell(Localization["LastName"]));
                    if (hasTeam) headerRow.Append(CreateHeaderCell(Localization["Team"]));
                    if (hasRaceTime) headerRow.Append(CreateHeaderCell(Localization["RaceTime"]));
                    if (hasTimePerKm) headerRow.Append(CreateHeaderCell(Localization["TimePerKm"]));
                    if (hasSpeed) headerRow.Append(CreateHeaderCell(Localization["Speed"]));
                    if (hasSex) headerRow.Append(CreateHeaderCell(Localization["Sex"]));
                    if (hasPositionBySex) headerRow.Append(CreateHeaderCell(Localization["PositionBySex"]));
                    if (hasAgeCategory) headerRow.Append(CreateHeaderCell(Localization["AgeCategory"]));
                    if (hasPositionByCategory) headerRow.Append(CreateHeaderCell(Localization["PositionByCategory"]));
                    table.Append(headerRow);

                    // Data rows
                    int rank = 1;
                    foreach (var classification in classifications)
                    {
                        TableRow dataRow = new TableRow();
                        bool isWinner = classification.Position == 1;

                        dataRow.Append(CreateDataCell(rank.ToString(), isWinner));
                        dataRow.Append(CreateDataCell(classification.Position.ToString(), isWinner));
                        dataRow.Append(CreateDataCell(classification.MemberFirstName, isWinner));
                        dataRow.Append(CreateDataCell(classification.MemberLastName, isWinner));
                        if (hasTeam) dataRow.Append(CreateDataCell(classification.Team ?? "-", isWinner));
                        if (hasRaceTime) dataRow.Append(CreateDataCell(classification.RaceTime.HasValue ? FormatTimeSpan(classification.RaceTime) : "-", isWinner));
                        if (hasTimePerKm) dataRow.Append(CreateDataCell(classification.TimePerKm.HasValue ? FormatTimeSpan(classification.TimePerKm) : "-", isWinner));
                        if (hasSpeed) dataRow.Append(CreateDataCell(classification.Speed.HasValue ? classification.Speed.Value.ToString("F2") : "-", isWinner));
                        if (hasSex) dataRow.Append(CreateDataCell(classification.Sex ?? "-", isWinner));
                        if (hasPositionBySex) dataRow.Append(CreateDataCell(classification.PositionBySex?.ToString() ?? "-", isWinner));
                        if (hasAgeCategory) dataRow.Append(CreateDataCell(classification.AgeCategory ?? "-", isWinner));
                        if (hasPositionByCategory) dataRow.Append(CreateDataCell(classification.PositionByCategory?.ToString() ?? "-", isWinner));

                        table.Append(dataRow);
                        rank++;
                    }

                    body.Append(table);
                    body.AppendChild(new Paragraph()); // Empty line
                }

                // Final Summary
                body.AppendChild(CreateSummaryParagraph(Localization["ExportComplete"].ToUpper(), true));
                body.AppendChild(CreateSummaryParagraph($"{Localization["TotalRaces"]}: {races.Count} | {Localization["TotalParticipantsAllRaces"]}: {totalParticipants}"));
                body.AppendChild(CreateSummaryParagraph($"{Localization["Generated"]}: {DateTime.Now:yyyy-MM-dd HH:mm}"));

                mainPart.Document.Save();
            }
        }

        private Paragraph CreateSummaryParagraph(string text, bool isBold = false)
        {
            Paragraph para = new Paragraph();
            Run run = para.AppendChild(new Run());
            run.AppendChild(new Text(text));
            if (isBold)
            {
                run.RunProperties = new RunProperties(new Bold());
            }
            return para;
        }

        private void ApplyParagraphFormatting(Paragraph paragraph, bool isBold, bool isCentered, int fontSize)
        {
            ParagraphProperties paragraphProperties = new ParagraphProperties();

            if (isCentered)
            {
                paragraphProperties.Append(new Justification() { Val = JustificationValues.Center });
            }

            paragraph.ParagraphProperties = paragraphProperties;

            if (paragraph.Elements<Run>().Any())
            {
                RunProperties runProperties = new RunProperties();
                if (isBold)
                {
                    runProperties.Append(new Bold());
                }
                runProperties.Append(new DocumentFormat.OpenXml.Wordprocessing.FontSize() { Val = fontSize.ToString() });

                foreach (var run in paragraph.Elements<Run>())
                {
                    run.RunProperties = (RunProperties)runProperties.CloneNode(true);
                }
            }
        }

        private void ApplyRunColor(Run run, string hexColor)
        {
            if (run.RunProperties == null)
            {
                run.RunProperties = new RunProperties();
            }
            run.RunProperties.Append(new Color() { Val = hexColor });
        }

        private TableCell CreateHeaderCell(string text)
        {
            TableCell cell = new TableCell();

            // Cell properties with blue background
            TableCellProperties cellProperties = new TableCellProperties(
                new Shading() 
                { 
                    Val = ShadingPatternValues.Clear, 
                    Fill = "2196F3" 
                });
            cell.Append(cellProperties);

            // Paragraph with white text
            Paragraph para = cell.AppendChild(new Paragraph());
            Run run = para.AppendChild(new Run());
            run.AppendChild(new Text(text));

            // Format: Bold and white color
            RunProperties runProps = new RunProperties(
                new Bold(),
                new Color() { Val = "FFFFFF" }
            );
            run.RunProperties = runProps;

            return cell;
        }

        private TableCell CreateDataCell(string text, bool isWinner)
        {
            TableCell cell = new TableCell();

            // Apply gold background for winners
            if (isWinner)
            {
                TableCellProperties cellProperties = new TableCellProperties(
                    new Shading() 
                    { 
                        Val = ShadingPatternValues.Clear, 
                        Fill = "FFD700" // Gold color
                    });
                cell.Append(cellProperties);
            }

            Paragraph para = cell.AppendChild(new Paragraph());
            Run run = para.AppendChild(new Run());
            run.AppendChild(new Text(text));

            if (isWinner)
            {
                run.RunProperties = new RunProperties(new Bold());
            }

            return cell;
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
            return SelectedRaceEventForClassification != null && 
                   RacesInSelectedEvent.Any(r => r.Status == "Processed");
        }

        private void ExecuteViewClassification(object parameter)
        {
            if (SelectedRaceEventForClassification == null) return;

            try
            {
                ShowChallengerClassification = false;

                Classifications.Clear();

                // Load classifications for all races in the selected event, grouped by distance
                var allClassifications = new List<ClassificationEntity>();
                foreach (var race in RacesInSelectedEvent.Where(r => r.Status == "Processed").OrderBy(r => r.DistanceKm))
                {
                    var raceClassifications = _classificationRepository.GetClassificationsByRace(race.Id, IsMemberFilter, IsChallengerFilter);

                    // Add a separator/header indicator for each distance
                    foreach (var classification in raceClassifications)
                    {
                        Classifications.Add(classification);
                    }
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

                int processedRaces = RacesInSelectedEvent.Count(r => r.Status == "Processed");
                StatusMessage = $"Loaded classifications for {processedRaces} race(s) from event '{SelectedRaceEventForClassification.Name}'{filterText}.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading classifications: {ex.Message}";
                MessageBox.Show($"Error loading classifications: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteShowRaceClassification(object parameter)
        {
            ShowChallengerClassification = false;
            Classifications.Clear();
            ChallengerClassifications.Clear();
            StatusMessage = "Switched to race classification view.";
        }

        private void ExecuteViewChallengerClassification(object parameter)
        {
            try
            {
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
                if (SelectedChallengeForClassification == null)
                {
                    StatusMessage = "Please select a challenge to view classifications.";
                    ChallengerClassifications.Clear();
                    return;
                }

                var challengerClassifications = _classificationRepository.GetChallengerClassification(SelectedChallengeForClassification.Year);

                ChallengerClassifications.Clear();
                foreach (var classification in challengerClassifications)
                {
                    ChallengerClassifications.Add(classification);
                }

                StatusMessage = $"Loaded challenger classification for '{SelectedChallengeForClassification.Name}' ({challengerClassifications.Count} challengers).";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading challenger classification: {ex.Message}";
                MessageBox.Show($"Error loading challenger classification: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadChallengesForClassification()
        {
            try
            {
                var challenges = _challengeRepository.GetAll();
                ChallengesForClassification.Clear();
                foreach (var challenge in challenges.OrderByDescending(c => c.Year).ThenBy(c => c.Name))
                {
                    ChallengesForClassification.Add(challenge);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading challenges: {ex.Message}";
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

        private void LoadRaceEventsForSelection()
        {
            try
            {
                var events = _raceEventRepository.GetAll();
                RaceEventsForSelection.Clear();
                foreach (var evt in events)
                {
                    RaceEventsForSelection.Add(evt);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading race events: {ex.Message}";
            }
        }

        private void LoadRacesForSelectedEvent()
        {
            RacesInSelectedEvent.Clear();
            Classifications.Clear();

            if (SelectedRaceEventForClassification == null)
            {
                StatusMessage = "No race event selected.";
                return;
            }

            try
            {
                var races = _raceRepository.GetRacesByRaceEvent(SelectedRaceEventForClassification.Id);
                foreach (var race in races)
                {
                    RacesInSelectedEvent.Add(race);
                }

                StatusMessage = $"Loaded {races.Count} race(s) for event '{SelectedRaceEventForClassification.Name}' (ordered by distance).";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading races for event: {ex.Message}";
                MessageBox.Show($"Error loading races for event: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            return SelectedChallengeForClassification != null && 
                   ChallengerClassifications != null && 
                   ChallengerClassifications.Count > 0;
        }

        private void ExecuteExportChallengerClassification(object parameter)
        {
            if (SelectedChallengeForClassification == null || 
                ChallengerClassifications == null || 
                ChallengerClassifications.Count == 0) return;

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
                    FileName = $"Challenger_Classification_{SelectedChallengeForClassification.Name.Replace(" ", "_")}_{(exportSummary ? "Summary" : "Detailed")}_{DateTime.Now:yyyyMMdd}.html"
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
            var challengeName = SelectedChallengeForClassification?.Name ?? "Challenge";
            var challengeYear = SelectedChallengeForClassification?.Year ?? SelectedYear;

            using (var writer = new StreamWriter(filePath))
            {
                // Write HTML header
                writer.WriteLine("<!DOCTYPE html>");
                writer.WriteLine("<html>");
                writer.WriteLine("<head>");
                writer.WriteLine("    <meta charset='utf-8'>");
                writer.WriteLine($"    <title>{challengeName} - {Localization["ChallengerClassification"]} {challengeYear}</title>");
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
                writer.WriteLine($"    <h1>🏆 {challengeName} - {Localization["ChallengerClassification"]} {challengeYear}</h1>");
                writer.WriteLine($"    <h2>{(exportSummary ? Localization["Summary"] : Localization["Detailed"])}</h2>");
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
            var challengeName = SelectedChallengeForClassification?.Name ?? "Challenge";
            var challengeYear = SelectedChallengeForClassification?.Year ?? SelectedYear;

            using (var writer = new StreamWriter(filePath))
            {
                writer.WriteLine($"╔═══════════════════════════════════════════════════════════════════════════════╗");
                writer.WriteLine($"║          {challengeName.ToUpper().PadRight(67)}║");
                writer.WriteLine($"║          {Localization["ChallengerClassification"].ToUpper().PadRight(67)}║");
                writer.WriteLine($"║                           {challengeYear}                                              ║");
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

        // Facebook Sharing Methods
        private bool CanExecuteShareRaceToFacebook(object parameter)
        {
            return SelectedRaceEventForClassification != null && 
                   RacesInSelectedEvent.Any(r => r.Status == "Processed");
        }

        private async void ExecuteShareRaceToFacebook(object parameter)
        {
            if (SelectedRaceEventForClassification == null) return;

            var processedRaces = RacesInSelectedEvent.Where(r => r.Status == "Processed").ToList();
            if (!processedRaces.Any())
            {
                MessageBox.Show("No processed races found in this event.", "No Races", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var result = MessageBox.Show(
                    $"Share '{SelectedRaceEventForClassification.Name}' results to Facebook?\n\n" +
                    "This will create TWO posts:\n" +
                    "1. Full race event results with top 3 finishers for each distance\n" +
                    "2. Challenger results (all challengers participating)",
                    "Share to Facebook",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;

                StatusMessage = "Sharing to Facebook...";
                IsProcessing = true;

                // Get all classifications for all races in the event
                var allClassifications = new List<ClassificationEntity>();
                foreach (var race in processedRaces.OrderBy(r => r.DistanceKm))
                {
                    var raceClassifications = _classificationRepository.GetClassificationsByRace(race.Id, null, null);
                    allClassifications.AddRange(raceClassifications);
                }

                // Post 1: Full Results Summary (grouped by distance)
                var fullResultsSummary = BuildFullRaceEventResultsSummary(SelectedRaceEventForClassification, processedRaces, allClassifications);

                // Post 2: Challenger Results (ALL challengers, regardless of points)
                var challengerResults = allClassifications.Where(c => c.IsChallenger).ToList();
                var challengerSummary = BuildRaceEventChallengerResultsSummary(SelectedRaceEventForClassification, challengerResults);

                // Post both to Facebook
                var results = await _facebookService.PostRaceWithLatestResultsAsync(
                    $"{SelectedRaceEventForClassification.Name} - {SelectedRaceEventForClassification.EventDate:dd/MM/yyyy}",
                    fullResultsSummary,
                    challengerSummary);

                // Check results
                var successCount = results.Count(r => r.Success);

                if (successCount == results.Count)
                {
                    StatusMessage = $"✅ Successfully shared both posts to Facebook! " +
                        $"Post 1 ID: {results[0].PostId}, Post 2 ID: {results[1].PostId}";
                    MessageBox.Show(
                        $"Both race event results shared successfully to Facebook!\n\n" +
                        $"Post 1 (Full Results) ID: {results[0].PostId}\n" +
                        $"Post 2 (Challengers) ID: {results[1].PostId}",
                        "Facebook Share Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else if (successCount > 0)
                {
                    var errors = string.Join("\n", results.Where(r => !r.Success).Select(r => r.ErrorMessage));
                    StatusMessage = $"⚠️ Partially successful: {successCount} of {results.Count} posts shared.";
                    MessageBox.Show(
                        $"Partially successful: {successCount} of {results.Count} posts shared.\n\nErrors:\n{errors}",
                        "Facebook Share Partial Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                else
                {
                    var errors = string.Join("\n", results.Select(r => r.ErrorMessage));
                    StatusMessage = $"❌ Failed to share to Facebook: {errors}";
                    MessageBox.Show(
                        $"Failed to share to Facebook:\n\n{errors}\n\nPlease check your Facebook configuration in App.config.",
                        "Facebook Share Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error sharing to Facebook: {ex.Message}";
                MessageBox.Show(
                    $"Error sharing to Facebook:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private string BuildFullRaceEventResultsSummary(RaceEventEntity raceEvent, List<RaceEntity> races, List<ClassificationEntity> allClassifications)
        {
            var summary = $"📊 Full Results for {raceEvent.Name}\n";
            summary += $"📅 {raceEvent.EventDate:dd/MM/yyyy}\n\n";

            foreach (var race in races.OrderBy(r => r.DistanceKm))
            {
                var raceClassifications = allClassifications.Where(c => c.RaceId == race.Id).OrderBy(c => c.Position).ToList();

                summary += $"🏃 {race.DistanceKm}km Race\n";
                summary += "🏆 Top 3:\n";

                var topResults = raceClassifications.Take(3).ToList();
                for (int i = 0; i < topResults.Count && i < 3; i++)
                {
                    var result = topResults[i];
                    var medal = i == 0 ? "🥇" : i == 1 ? "🥈" : "🥉";
                    summary += $"{medal} {result.Position}. {result.MemberFirstName} {result.MemberLastName}";

                    if (result.RaceTime.HasValue)
                    {
                        summary += $" - {result.RaceTime.Value:hh\\:mm\\:ss}";
                    }
                    summary += "\n";
                }
                summary += $"👥 {raceClassifications.Count} participants\n\n";
            }

            var totalParticipants = allClassifications.Select(c => new { c.MemberFirstName, c.MemberLastName }).Distinct().Count();
            summary += $"📈 Total unique participants: {totalParticipants}";

            return summary;
        }

        private string BuildRaceEventChallengerResultsSummary(RaceEventEntity raceEvent, List<ClassificationEntity> challengerResults)
        {
            var summary = $"⭐ Challenger Results - {raceEvent.Name}\n";
            summary += $"📅 {raceEvent.EventDate:dd/MM/yyyy}\n";
            summary += $"All Challengers Participating in This Event\n\n";

            // Group by challenger and sum points
            var challengerGroups = challengerResults
                .GroupBy(c => new { c.MemberFirstName, c.MemberLastName })
                .Select(g => new
                {
                    FirstName = g.Key.MemberFirstName,
                    LastName = g.Key.MemberLastName,
                    TotalPoints = g.Sum(c => c.Points),
                    Races = g.Count()
                })
                .OrderByDescending(c => c.TotalPoints)
                .ToList();

            if (challengerGroups.Count == 0)
            {
                summary += "No challengers participated in this event.\n";
                return summary;
            }

            summary += "🎯 Top Challengers:\n";
            var topCount = Math.Min(10, challengerGroups.Count);

            for (int i = 0; i < topCount; i++)
            {
                var challenger = challengerGroups[i];
                summary += $"⭐ {challenger.FirstName} {challenger.LastName}: {challenger.TotalPoints} pts ({challenger.Races} race{(challenger.Races > 1 ? "s" : "")})\n";
            }

            if (challengerGroups.Count > topCount)
            {
                summary += $"\n... and {challengerGroups.Count - topCount} more challengers!\n";
            }

            summary += $"\n📈 Total challengers: {challengerGroups.Count}";

            return summary;
        }

        private bool CanExecuteShareChallengeToFacebook(object parameter)
        {
            return ChallengerClassifications != null && ChallengerClassifications.Count > 0;
        }

        private async void ExecuteShareChallengeToFacebook(object parameter)
        {
            if (ChallengerClassifications == null || ChallengerClassifications.Count == 0) return;

            try
            {
                var result = MessageBox.Show(
                    $"Share Challenge {SelectedYear} standings to Facebook?\n\nThis will post the top challengers to your Facebook page.",
                    "Share to Facebook",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;

                StatusMessage = "Sharing challenge standings to Facebook...";
                IsProcessing = true;

                // Build summary
                var summary = BuildChallengeSummary();

                // Post to Facebook
                var fbResult = await _facebookService.PostChallengeResultsAsync(
                    $"Challenge {SelectedYear} Standings",
                    summary,
                    null);

                if (fbResult.Success)
                {
                    StatusMessage = $"✅ Successfully shared challenge standings to Facebook! Post ID: {fbResult.PostId}";
                    MessageBox.Show(
                        $"Challenge standings shared successfully to Facebook!\n\nPost ID: {fbResult.PostId}",
                        "Facebook Share Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    StatusMessage = $"❌ Failed to share to Facebook: {fbResult.ErrorMessage}";
                    MessageBox.Show(
                        $"Failed to share to Facebook:\n\n{fbResult.ErrorMessage}\n\nPlease check your Facebook configuration in App.config.",
                        "Facebook Share Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error sharing challenge to Facebook: {ex.Message}";
                MessageBox.Show(
                    $"Error sharing challenge to Facebook:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private string BuildRaceSummary(RaceEntity race, List<ClassificationEntity> classifications)
        {
            var summary = $"Results for {race.Name} ({race.DistanceKm} km)\n\n";
            summary += "🏆 Top 3 Finishers:\n";

            var topResults = classifications.OrderBy(c => c.Position).Take(3).ToList();

            for (int i = 0; i < topResults.Count && i < 3; i++)
            {
                var result = topResults[i];
                var medal = i == 0 ? "🥇" : i == 1 ? "🥈" : "🥉";
                summary += $"{medal} {result.Position}. {result.MemberFirstName} {result.MemberLastName}";

                if (result.RaceTime.HasValue)
                {
                    summary += $" - {result.RaceTime.Value:hh\\:mm\\:ss}";
                }

                if (!string.IsNullOrEmpty(result.Team))
                {
                    summary += $" ({result.Team})";
                }

                summary += "\n";
            }

            summary += $"\n👥 Total Participants: {classifications.Count}";

            return summary;
        }

        private string BuildFullResultsSummary(RaceEntity race, List<ClassificationEntity> allClassifications)
        {
            var summary = $"📊 Full Challenge Results for {race.Name} ({race.DistanceKm} km)\n\n";

            var topResults = allClassifications.OrderBy(c => c.Position).Take(3).ToList();
            summary += "🏆 Top 3 Overall:\n";

            for (int i = 0; i < topResults.Count && i < 3; i++)
            {
                var result = topResults[i];
                var medal = i == 0 ? "🥇" : i == 1 ? "🥈" : "🥉";
                summary += $"{medal} {result.Position}. {result.MemberFirstName} {result.MemberLastName}";

                if (result.RaceTime.HasValue)
                {
                    summary += $" - {result.RaceTime.Value:hh\\:mm\\:ss}";
                }

                summary += "\n";
            }

            var totalParticipants = allClassifications.Count;
            var membersCount = allClassifications.Count(c => c.IsMember);
            var challengersCount = allClassifications.Count(c => c.IsChallenger);

            summary += $"\n👥 Total Participants: {totalParticipants}";
            summary += $"\n🏃 Members: {membersCount}";
            summary += $"\n⭐ Challengers: {challengersCount}";

            return summary;
        }

        private string BuildChallengerResultsSummary(RaceEntity race, List<ClassificationEntity> results)
        {
            var summary = $"⭐ Challenger Results - {race.Name} ({race.DistanceKm} km)\n";
            summary += $"All Challengers Participating in This Race\n\n";

            var sortedResults = results.OrderByDescending(c => c.Points).ThenBy(c => c.Position).ToList();

            if (sortedResults.Count == 0)
            {
                summary += "No challengers participated in this race.\n";
                return summary;
            }

            summary += "🎯 Top Challengers:\n";
            var topCount = Math.Min(10, sortedResults.Count);

            for (int i = 0; i < topCount; i++)
            {
                var result = sortedResults[i];
                var position = result.Position.HasValue ? $"#{result.Position}" : "-";

                summary += $"⭐ {result.MemberFirstName} {result.MemberLastName}: {result.Points} pts ({position})";

                if (result.RaceTime.HasValue)
                {
                    summary += $" - {result.RaceTime.Value:hh\\:mm\\:ss}";
                }

                summary += "\n";
            }

            if (sortedResults.Count > topCount)
            {
                summary += $"\n... and {sortedResults.Count - topCount} more challengers!\n";
            }

            summary += $"\n📈 Total challengers: {sortedResults.Count}";

            return summary;
        }

        private string BuildChallengeSummary()
        {
            if (SelectedChallengeForClassification == null)
            {
                return "No challenge selected.";
            }

            var summary = $"{SelectedChallengeForClassification.Name} - {SelectedChallengeForClassification.Year} Standings\n\n";
            summary += "🏆 Top Challengers (with >0 points):\n";

            // Filter to only show challengers with points > 0
            var challengersWithPoints = ChallengerClassifications.Where(c => c.TotalPoints > 0).ToList();
            var topChallengers = challengersWithPoints.OrderBy(c => c.RankByPoints).Take(5).ToList();

            for (int i = 0; i < topChallengers.Count && i < 5; i++)
            {
                var challenger = topChallengers[i];
                var medal = i == 0 ? "🥇" : i == 1 ? "🥈" : i == 2 ? "🥉" : "🔹";
                summary += $"{medal} #{challenger.RankByPoints} {challenger.ChallengerFirstName} {challenger.ChallengerLastName}";
                summary += $" - {challenger.TotalPoints} pts ({challenger.RaceCount} races)\n";
            }

            summary += $"\n👥 Total Challengers with points: {challengersWithPoints.Count}";

            return summary;
        }

        // Race Event-Based Upload Methods

        private void LoadAvailableDistancesForUpload()
        {
            AvailableDistancesForUpload.Clear();

            if (SelectedUploadRaceEvent != null)
            {
                var distances = _raceEventRepository.GetDistancesByEvent(SelectedUploadRaceEvent.Id);

                // Calculate next race number
                _nextRaceNumber = CalculateNextRaceNumber(SelectedUploadRaceEvent.EventDate.Year);

                foreach (var distance in distances)
                {
                    AvailableDistancesForUpload.Add(new RaceDistanceUploadModel(distance));
                }

                StatusMessage = $"Loaded {distances.Count} distances for {SelectedUploadRaceEvent.Name}. Next race number: {_nextRaceNumber}";
            }
        }

        private int CalculateNextRaceNumber(int year)
        {
            var existingRaces = _raceRepository.GetRacesByYear(year);
            if (existingRaces == null || !existingRaces.Any())
                return 1;

            return existingRaces.Max(r => r.RaceNumber) + 1;
        }

        private void ExecuteBrowseDistanceFile(RaceDistanceUploadModel distanceUpload)
        {
            if (distanceUpload == null) return;

            var openFileDialog = new OpenFileDialog
            {
                Filter = "Race Result Files (*.xlsx;*.pdf)|*.xlsx;*.pdf|Excel Files (*.xlsx)|*.xlsx|PDF Files (*.pdf)|*.pdf|All Files (*.*)|*.*",
                Title = $"Select Result File for {distanceUpload.DistanceKm} km"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                distanceUpload.FilePath = openFileDialog.FileName;
                distanceUpload.StatusMessage = "Ready to process";
                ((RelayCommand)ProcessAllDistancesCommand)?.RaiseCanExecuteChanged();

                var extension = Path.GetExtension(openFileDialog.FileName).ToLowerInvariant();
                var fileType = extension == ".pdf" ? "PDF" : "Excel";
                StatusMessage = $"{fileType} file selected for {distanceUpload.DistanceKm} km";
            }
        }

        private bool CanExecuteProcessAllDistances(object parameter)
        {
            return !IsProcessing && 
                   SelectedUploadRaceEvent != null &&
                   AvailableDistancesForUpload.Any(d => d.HasFile);
        }

        private async void ExecuteProcessAllDistances(object parameter)
        {
            if (SelectedUploadRaceEvent == null) return;

            var distancesWithFiles = AvailableDistancesForUpload.Where(d => d.HasFile).ToList();
            if (!distancesWithFiles.Any())
            {
                MessageBox.Show("Please select at least one file to process.", "No Files Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Process {distancesWithFiles.Count} distance(s) for {SelectedUploadRaceEvent.Name}?\n\n" +
                $"Race Event: {SelectedUploadRaceEvent.Name}\n" +
                $"Date: {SelectedUploadRaceEvent.EventDate:dd/MM/yyyy}\n" +
                $"Year: {SelectedUploadRaceEvent.EventDate.Year}\n" +
                $"Race Number: {_nextRaceNumber}\n" +
                $"Distances: {string.Join(", ", distancesWithFiles.Select(d => $"{d.DistanceKm} km"))}\n",
                "Confirm Processing",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            IsProcessing = true;
            int successCount = 0;
            int failCount = 0;
            var errors = new List<string>();

            try
            {
                foreach (var distanceUpload in distancesWithFiles)
                {
                    distanceUpload.StatusMessage = "Processing...";
                    StatusMessage = $"Processing {distanceUpload.DistanceKm} km...";

                    try
                    {
                        await System.Threading.Tasks.Task.Run(() =>
                        {
                            var raceDistance = new RaceDistance(_nextRaceNumber, SelectedUploadRaceEvent.Name, (int)distanceUpload.DistanceKm);

                            // Save race
                            int year = SelectedUploadRaceEvent.EventDate.Year;
                            _raceRepository.SaveRace(raceDistance, year, distanceUpload.FilePath, false, SelectedUploadRaceEvent.Id);

                            // Get members
                            var memberRepository = new JsonMemberRepository("Members.json");
                            var challengerRepository = new JsonMemberRepository("Challenge.json");
                            var memberService = new MemberService(memberRepository, challengerRepository);
                            var allMembers = memberService.GetAllMembersAndChallengers();

                            // Select appropriate parser
                            var extension = Path.GetExtension(distanceUpload.FilePath).ToLowerInvariant();
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

                            // Process race
                            var classification = raceProcessingService.ProcessRaceWithMembers(distanceUpload.FilePath, raceDistance, allMembers);

                            // Save classifications
                            var races = _raceRepository.GetRacesByYear(year);
                            var savedRace = races.FirstOrDefault(r => r.Name == SelectedUploadRaceEvent.Name && 
                                                                      r.RaceNumber == _nextRaceNumber && 
                                                                      r.DistanceKm == (int)distanceUpload.DistanceKm);

                            if (savedRace != null)
                            {
                                _classificationRepository.SaveClassifications(savedRace.Id, classification);
                                _raceRepository.UpdateRaceStatus(savedRace.Id, "Processed");
                            }
                        });

                        distanceUpload.StatusMessage = "✓ Processed";
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        distanceUpload.StatusMessage = $"✗ Error: {ex.Message}";
                        errors.Add($"{distanceUpload.DistanceKm} km: {ex.Message}");
                        failCount++;
                    }
                }

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    LoadRaces();

                    if (failCount == 0)
                    {
                        StatusMessage = $"Successfully processed {successCount} distance(s)!";
                        MessageBox.Show(
                            $"All races processed successfully!\n\n" +
                            $"Processed: {successCount} distance(s)\n" +
                            $"Race Event: {SelectedUploadRaceEvent.Name}\n" +
                            $"Race Number: {_nextRaceNumber}",
                            "Success",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        // Clear the form
                        SelectedUploadRaceEvent = null;
                        AvailableDistancesForUpload.Clear();
                    }
                    else
                    {
                        StatusMessage = $"Processed {successCount} distance(s), {failCount} failed.";
                        MessageBox.Show(
                            $"Processing completed with errors:\n\n" +
                            $"Successful: {successCount}\n" +
                            $"Failed: {failCount}\n\n" +
                            $"Errors:\n" + string.Join("\n", errors),
                            "Partial Success",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusMessage = $"Error: {ex.Message}";
                    MessageBox.Show($"Error processing races: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
            finally
            {
                IsProcessing = false;
            }
        }

        // Export Methods for Race Classification

        private bool CanExecuteExport(object parameter)
        {
            return SelectedRaceEventForClassification != null &&
                   RacesInSelectedEvent != null &&
                   RacesInSelectedEvent.Count > 0;
        }

        private void ExecuteExportToHtml(object parameter)
        {
            if (!CanExecuteExport(parameter)) return;

            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "HTML Files (*.html)|*.html|All Files (*.*)|*.*",
                    DefaultExt = "html",
                    FileName = $"{SelectedRaceEventForClassification.Name.Replace(" ", "_")}_Results_{DateTime.Now:yyyyMMdd}.html"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    ExportRaceEventToHtml(saveFileDialog.FileName);
                    StatusMessage = $"Exported to HTML: {Path.GetFileName(saveFileDialog.FileName)}";
                    MessageBox.Show($"Results exported successfully!\n\nFile: {saveFileDialog.FileName}", 
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error exporting to HTML: {ex.Message}";
                MessageBox.Show($"Error exporting to HTML: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteExportToExcel(object parameter)
        {
            if (!CanExecuteExport(parameter)) return;

            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
                    DefaultExt = "xlsx",
                    FileName = $"{SelectedRaceEventForClassification.Name.Replace(" ", "_")}_Results_{DateTime.Now:yyyyMMdd}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    ExportRaceEventToExcel(saveFileDialog.FileName);
                    StatusMessage = $"Exported to Excel: {Path.GetFileName(saveFileDialog.FileName)}";
                    MessageBox.Show($"Results exported successfully!\n\nFile: {saveFileDialog.FileName}", 
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error exporting to Excel: {ex.Message}";
                MessageBox.Show($"Error exporting to Excel: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteExportToWord(object parameter)
        {
            if (!CanExecuteExport(parameter)) return;

            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Word Documents (*.docx)|*.docx|All Files (*.*)|*.*",
                    DefaultExt = "docx",
                    FileName = $"{SelectedRaceEventForClassification.Name.Replace(" ", "_")}_Results_{DateTime.Now:yyyyMMdd}.docx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    ExportRaceEventToWord(saveFileDialog.FileName);
                    StatusMessage = $"Exported to Word: {Path.GetFileName(saveFileDialog.FileName)}";
                    MessageBox.Show($"Results exported successfully!\n\nFile: {saveFileDialog.FileName}", 
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error exporting to Word: {ex.Message}";
                MessageBox.Show($"Error exporting to Word: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteExportSummary(object parameter)
        {
            if (!CanExecuteExport(parameter)) return;

            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                    DefaultExt = "txt",
                    FileName = $"{SelectedRaceEventForClassification.Name.Replace(" ", "_")}_Summary_{DateTime.Now:yyyyMMdd}.txt"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    ExportRaceEventSummary(saveFileDialog.FileName);
                    StatusMessage = $"Exported summary: {Path.GetFileName(saveFileDialog.FileName)}";
                    MessageBox.Show($"Summary exported successfully!\n\nFile: {saveFileDialog.FileName}", 
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error exporting summary: {ex.Message}";
                MessageBox.Show($"Error exporting summary: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportRaceEventToHtml(string filePath)
        {
            using (var writer = new StreamWriter(filePath))
            {
                // Build filter description
                var filterDesc = BuildFilterDescription();

                writer.WriteLine("<!DOCTYPE html>");
                writer.WriteLine("<html>");
                writer.WriteLine("<head>");
                writer.WriteLine("    <meta charset='utf-8'>");
                writer.WriteLine($"    <title>{SelectedRaceEventForClassification.Name} - Results</title>");
                writer.WriteLine("    <style>");
                writer.WriteLine("        body { font-family: Arial, sans-serif; margin: 20px; }");
                writer.WriteLine("        h1 { color: #2196F3; }");
                writer.WriteLine("        h2 { color: #FF9800; margin-top: 30px; }");
                writer.WriteLine("        table { border-collapse: collapse; width: 100%; margin-top: 20px; font-size: 12px; }");
                writer.WriteLine("        th, td { border: 1px solid #ddd; padding: 6px; text-align: left; }");
                writer.WriteLine("        th { background-color: #2196F3; color: white; font-weight: bold; }");
                writer.WriteLine("        tr:nth-child(even) { background-color: #f2f2f2; }");
                writer.WriteLine("        .event-info { background-color: #E3F2FD; padding: 15px; margin: 20px 0; border-radius: 5px; }");
                writer.WriteLine("        .member { background-color: #C8E6C9; }");
                writer.WriteLine("        .challenger { font-weight: bold; }");
                writer.WriteLine("    </style>");
                writer.WriteLine("</head>");
                writer.WriteLine("<body>");
                writer.WriteLine($"    <h1>🏁 {SelectedRaceEventForClassification.Name}</h1>");
                writer.WriteLine($"    <div class='event-info'>");
                writer.WriteLine($"        <strong>Date:</strong> {SelectedRaceEventForClassification.EventDate:dd/MM/yyyy}<br/>");
                writer.WriteLine($"        <strong>Location:</strong> {SelectedRaceEventForClassification.Location ?? "-"}<br/>");
                if (!string.IsNullOrEmpty(filterDesc))
                {
                    writer.WriteLine($"        <strong>Filter:</strong> {filterDesc}<br/>");
                }
                writer.WriteLine($"        <strong>Generated:</strong> {DateTime.Now:yyyy-MM-dd HH:mm}<br/>");
                writer.WriteLine($"    </div>");

                foreach (var race in RacesInSelectedEvent.OrderBy(r => r.DistanceKm))
                {
                    // Apply filters
                    var classifications = _classificationRepository.GetClassificationsByRace(race.Id, IsMemberFilter, IsChallengerFilter);

                    writer.WriteLine($"    <h2>{race.DistanceKm} km - Race #{race.RaceNumber} ({classifications.Count} participants)</h2>");
                    writer.WriteLine("    <table>");
                    writer.WriteLine("        <thead>");
                    writer.WriteLine("            <tr>");
                    writer.WriteLine("                <th>Pos</th>");
                    writer.WriteLine("                <th>First Name</th>");
                    writer.WriteLine("                <th>Last Name</th>");
                    writer.WriteLine("                <th>Sex</th>");
                    writer.WriteLine("                <th>Pos/Sex</th>");
                    writer.WriteLine("                <th>Category</th>");
                    writer.WriteLine("                <th>Pos/Cat</th>");
                    writer.WriteLine("                <th>Team</th>");
                    writer.WriteLine("                <th>Points</th>");
                    writer.WriteLine("                <th>Time</th>");
                    writer.WriteLine("                <th>Time/km</th>");
                    writer.WriteLine("                <th>Speed</th>");
                    writer.WriteLine("                <th>Member</th>");
                    writer.WriteLine("                <th>Challenger</th>");
                    writer.WriteLine("                <th>Bonus KM</th>");
                    writer.WriteLine("            </tr>");
                    writer.WriteLine("        </thead>");
                    writer.WriteLine("        <tbody>");

                    foreach (var c in classifications.OrderBy(x => x.Position))
                    {
                        var rowClass = c.IsMember ? " class='member'" : "";
                        if (c.IsChallenger) rowClass += " challenger";

                        writer.WriteLine($"            <tr{rowClass}>");
                        writer.WriteLine($"                <td>{c.Position}</td>");
                        writer.WriteLine($"                <td>{c.MemberFirstName}</td>");
                        writer.WriteLine($"                <td>{c.MemberLastName}</td>");
                        writer.WriteLine($"                <td>{c.Sex ?? "-"}</td>");
                        writer.WriteLine($"                <td>{c.PositionBySex?.ToString() ?? "-"}</td>");
                        writer.WriteLine($"                <td>{c.AgeCategory ?? "-"}</td>");
                        writer.WriteLine($"                <td>{c.PositionByCategory?.ToString() ?? "-"}</td>");
                        writer.WriteLine($"                <td>{c.Team ?? "-"}</td>");
                        writer.WriteLine($"                <td>{c.Points}</td>");
                        writer.WriteLine($"                <td>{(c.RaceTime.HasValue ? c.RaceTime.Value.ToString(@"hh\:mm\:ss") : "-")}</td>");
                        writer.WriteLine($"                <td>{(c.TimePerKm.HasValue ? c.TimePerKm.Value.ToString(@"mm\:ss") : "-")}</td>");
                        writer.WriteLine($"                <td>{(c.Speed.HasValue ? c.Speed.Value.ToString("F2") : "-")} km/h</td>");
                        writer.WriteLine($"                <td>{(c.IsMember ? "✓" : "")}</td>");
                        writer.WriteLine($"                <td>{(c.IsChallenger ? "✓" : "")}</td>");
                        writer.WriteLine($"                <td>{c.BonusKm}</td>");
                        writer.WriteLine("            </tr>");
                    }

                    writer.WriteLine("        </tbody>");
                    writer.WriteLine("    </table>");
                }

                writer.WriteLine("</body>");
                writer.WriteLine("</html>");
            }
        }

        private void ExportRaceEventToExcel(string filePath)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var filterDesc = BuildFilterDescription();

                foreach (var race in RacesInSelectedEvent.OrderBy(r => r.DistanceKm))
                {
                    var worksheet = package.Workbook.Worksheets.Add($"{race.DistanceKm}km");
                    // Apply filters
                    var classifications = _classificationRepository.GetClassificationsByRace(race.Id, IsMemberFilter, IsChallengerFilter);

                    // Title
                    worksheet.Cells[1, 1].Value = $"{SelectedRaceEventForClassification.Name} - {race.DistanceKm} km";
                    worksheet.Cells[1, 1, 1, 15].Merge = true;
                    worksheet.Cells[1, 1].Style.Font.Size = 16;
                    worksheet.Cells[1, 1].Style.Font.Bold = true;

                    // Event info
                    int infoRow = 2;
                    worksheet.Cells[infoRow, 1].Value = $"Date: {SelectedRaceEventForClassification.EventDate:dd/MM/yyyy} | Race #{race.RaceNumber}";
                    if (!string.IsNullOrEmpty(filterDesc))
                    {
                        worksheet.Cells[infoRow, 1].Value += $" | Filter: {filterDesc}";
                    }
                    worksheet.Cells[infoRow, 1, infoRow, 15].Merge = true;

                    // Headers
                    int headerRow = 4;
                    int col = 1;
                    worksheet.Cells[headerRow, col++].Value = "Position";
                    worksheet.Cells[headerRow, col++].Value = "First Name";
                    worksheet.Cells[headerRow, col++].Value = "Last Name";
                    worksheet.Cells[headerRow, col++].Value = "Sex";
                    worksheet.Cells[headerRow, col++].Value = "Pos/Sex";
                    worksheet.Cells[headerRow, col++].Value = "Category";
                    worksheet.Cells[headerRow, col++].Value = "Pos/Cat";
                    worksheet.Cells[headerRow, col++].Value = "Team";
                    worksheet.Cells[headerRow, col++].Value = "Points";
                    worksheet.Cells[headerRow, col++].Value = "Time";
                    worksheet.Cells[headerRow, col++].Value = "Time/km";
                    worksheet.Cells[headerRow, col++].Value = "Speed";
                    worksheet.Cells[headerRow, col++].Value = "Member";
                    worksheet.Cells[headerRow, col++].Value = "Challenger";
                    worksheet.Cells[headerRow, col++].Value = "Bonus KM";

                    using (var range = worksheet.Cells[headerRow, 1, headerRow, 15])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                    }

                    // Data
                    int row = headerRow + 1;
                    foreach (var c in classifications.OrderBy(x => x.Position))
                    {
                        col = 1;
                        worksheet.Cells[row, col++].Value = c.Position;
                        worksheet.Cells[row, col++].Value = c.MemberFirstName;
                        worksheet.Cells[row, col++].Value = c.MemberLastName;
                        worksheet.Cells[row, col++].Value = c.Sex ?? "-";
                        worksheet.Cells[row, col++].Value = c.PositionBySex?.ToString() ?? "-";
                        worksheet.Cells[row, col++].Value = c.AgeCategory ?? "-";
                        worksheet.Cells[row, col++].Value = c.PositionByCategory?.ToString() ?? "-";
                        worksheet.Cells[row, col++].Value = c.Team ?? "-";
                        worksheet.Cells[row, col++].Value = c.Points;
                        worksheet.Cells[row, col++].Value = c.RaceTime.HasValue ? c.RaceTime.Value.ToString(@"hh\:mm\:ss") : "-";
                        worksheet.Cells[row, col++].Value = c.TimePerKm.HasValue ? c.TimePerKm.Value.ToString(@"mm\:ss") : "-";
                        worksheet.Cells[row, col++].Value = c.Speed.HasValue ? c.Speed.Value.ToString("F2") : "-";
                        worksheet.Cells[row, col++].Value = c.IsMember ? "✓" : "";
                        worksheet.Cells[row, col++].Value = c.IsChallenger ? "✓" : "";
                        worksheet.Cells[row, col++].Value = c.BonusKm;

                        // Highlight members
                        if (c.IsMember)
                        {
                            using (var rowRange = worksheet.Cells[row, 1, row, 15])
                            {
                                rowRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                                rowRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);
                            }
                        }

                        // Bold challengers
                        if (c.IsChallenger)
                        {
                            using (var rowRange = worksheet.Cells[row, 1, row, 15])
                            {
                                rowRange.Style.Font.Bold = true;
                            }
                        }

                        row++;
                    }

                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                }

                package.SaveAs(new FileInfo(filePath));
            }
        }

        private void ExportRaceEventToWord(string filePath)
        {
            using (var document = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
            {
                var mainPart = document.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = mainPart.Document.AppendChild(new Body());

                var filterDesc = BuildFilterDescription();

                // Title
                var titlePara = body.AppendChild(new Paragraph());
                var titleRun = titlePara.AppendChild(new Run());
                titleRun.AppendChild(new Text($"{SelectedRaceEventForClassification.Name} - Results"));

                // Event info
                var infoPara = body.AppendChild(new Paragraph());
                var infoRun = infoPara.AppendChild(new Run());
                var infoText = $"Date: {SelectedRaceEventForClassification.EventDate:dd/MM/yyyy}";
                if (!string.IsNullOrEmpty(filterDesc))
                {
                    infoText += $" | Filter: {filterDesc}";
                }
                infoRun.AppendChild(new Text(infoText));

                foreach (var race in RacesInSelectedEvent.OrderBy(r => r.DistanceKm))
                {
                    // Apply filters
                    var classifications = _classificationRepository.GetClassificationsByRace(race.Id, IsMemberFilter, IsChallengerFilter);

                    // Distance header
                    var distPara = body.AppendChild(new Paragraph());
                    var distRun = distPara.AppendChild(new Run());
                    distRun.AppendChild(new Text($"{race.DistanceKm} km - Race #{race.RaceNumber} ({classifications.Count} participants)"));

                    // Create table
                    var table = new Table();
                    var headerRow = new TableRow();
                    AddWordTableCell(headerRow, "Pos");
                    AddWordTableCell(headerRow, "First Name");
                    AddWordTableCell(headerRow, "Last Name");
                    AddWordTableCell(headerRow, "Sex");
                    AddWordTableCell(headerRow, "Pos/Sex");
                    AddWordTableCell(headerRow, "Category");
                    AddWordTableCell(headerRow, "Pos/Cat");
                    AddWordTableCell(headerRow, "Team");
                    AddWordTableCell(headerRow, "Points");
                    AddWordTableCell(headerRow, "Time");
                    AddWordTableCell(headerRow, "Time/km");
                    AddWordTableCell(headerRow, "Speed");
                    AddWordTableCell(headerRow, "Mbr");
                    AddWordTableCell(headerRow, "Chl");
                    AddWordTableCell(headerRow, "Bonus");
                    table.AppendChild(headerRow);

                    foreach (var c in classifications.OrderBy(x => x.Position))
                    {
                        var dataRow = new TableRow();
                        AddWordTableCell(dataRow, c.Position.ToString());
                        AddWordTableCell(dataRow, c.MemberFirstName);
                        AddWordTableCell(dataRow, c.MemberLastName);
                        AddWordTableCell(dataRow, c.Sex ?? "-");
                        AddWordTableCell(dataRow, c.PositionBySex?.ToString() ?? "-");
                        AddWordTableCell(dataRow, c.AgeCategory ?? "-");
                        AddWordTableCell(dataRow, c.PositionByCategory?.ToString() ?? "-");
                        AddWordTableCell(dataRow, c.Team ?? "-");
                        AddWordTableCell(dataRow, c.Points.ToString());
                        AddWordTableCell(dataRow, c.RaceTime.HasValue ? c.RaceTime.Value.ToString(@"hh\:mm\:ss") : "-");
                        AddWordTableCell(dataRow, c.TimePerKm.HasValue ? c.TimePerKm.Value.ToString(@"mm\:ss") : "-");
                        AddWordTableCell(dataRow, c.Speed.HasValue ? c.Speed.Value.ToString("F2") : "-");
                        AddWordTableCell(dataRow, c.IsMember ? "✓" : "");
                        AddWordTableCell(dataRow, c.IsChallenger ? "✓" : "");
                        AddWordTableCell(dataRow, c.BonusKm.ToString());
                        table.AppendChild(dataRow);
                    }

                    body.AppendChild(table);
                    body.AppendChild(new Paragraph()); // Spacing
                }

                mainPart.Document.Save();
            }
        }

        private void AddWordTableCell(TableRow row, string text)
        {
            var cell = new TableCell();
            var para = new Paragraph();
            var run = new Run();
            run.AppendChild(new Text(text));
            para.AppendChild(run);
            cell.AppendChild(para);
            row.AppendChild(cell);
        }

        private string BuildFilterDescription()
        {
            var filters = new List<string>();

            if (IsMemberFilter.HasValue)
            {
                filters.Add(IsMemberFilter.Value ? "Members only" : "Non-members only");
            }

            if (IsChallengerFilter.HasValue)
            {
                filters.Add(IsChallengerFilter.Value ? "Challengers only" : "Non-challengers only");
            }

            if (filters.Count > 0)
            {
                filters.Add("Winner always shown");
                return string.Join(", ", filters);
            }

            return string.Empty;
        }

        private void ExportRaceEventSummary(string filePath)
        {
            using (var writer = new StreamWriter(filePath))
            {
                var filterDesc = BuildFilterDescription();

                writer.WriteLine($"═══════════════════════════════════════════════════════════");
                writer.WriteLine($"  {SelectedRaceEventForClassification.Name.ToUpper()}");
                writer.WriteLine($"═══════════════════════════════════════════════════════════");
                writer.WriteLine();
                writer.WriteLine($"Date: {SelectedRaceEventForClassification.EventDate:dd/MM/yyyy}");
                writer.WriteLine($"Location: {SelectedRaceEventForClassification.Location ?? "-"}");
                if (!string.IsNullOrEmpty(filterDesc))
                {
                    writer.WriteLine($"Filter: {filterDesc}");
                }
                writer.WriteLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}");
                writer.WriteLine();

                foreach (var race in RacesInSelectedEvent.OrderBy(r => r.DistanceKm))
                {
                    // Apply filters
                    var classifications = _classificationRepository.GetClassificationsByRace(race.Id, IsMemberFilter, IsChallengerFilter);

                    writer.WriteLine($"───────────────────────────────────────────────────────────");
                    writer.WriteLine($"🏃 {race.DistanceKm} km - Race #{race.RaceNumber}");
                    writer.WriteLine($"───────────────────────────────────────────────────────────");
                    writer.WriteLine($"Total Participants: {classifications.Count}");

                    var memberCount = classifications.Count(c => c.IsMember);
                    var challengerCount = classifications.Count(c => c.IsChallenger);
                    if (memberCount > 0) writer.WriteLine($"Members: {memberCount}");
                    if (challengerCount > 0) writer.WriteLine($"Challengers: {challengerCount}");

                    writer.WriteLine();
                    writer.WriteLine("🏆 Top 10:");
                    writer.WriteLine();

                    var topResults = classifications.OrderBy(c => c.Position).Take(10).ToList();
                    for (int i = 0; i < topResults.Count; i++)
                    {
                        var c = topResults[i];
                        var medal = i == 0 ? "🥇" : i == 1 ? "🥈" : i == 2 ? "🥉" : "  ";
                        var time = c.RaceTime.HasValue ? c.RaceTime.Value.ToString(@"hh\:mm\:ss") : "N/A";
                        var memberIndicator = c.IsMember ? "👤" : "  ";
                        var challengerIndicator = c.IsChallenger ? "⭐" : "  ";
                        writer.WriteLine($"{medal} {c.Position,3}. {c.MemberFirstName} {c.MemberLastName,-25} {time} {memberIndicator}{challengerIndicator}");
                    }

                    writer.WriteLine();
                }

                writer.WriteLine($"═══════════════════════════════════════════════════════════");
                writer.WriteLine();
                writer.WriteLine("Legend:");
                writer.WriteLine("  👤 = Club Member");
                writer.WriteLine("  ⭐ = Challenger");
            }
        }

        // Challenger Classification Export Methods

        private void ExecuteExportChallengerSummaryHtml(object parameter)
        {
            if (!CanExecuteExportChallengerClassification(parameter)) return;

            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "HTML Files (*.html)|*.html|All Files (*.*)|*.*",
                    DefaultExt = "html",
                    FileName = $"{SelectedChallengeForClassification.Name.Replace(" ", "_")}_Summary_{DateTime.Now:yyyyMMdd}.html"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    ExportChallengerSummaryToHtml(saveFileDialog.FileName);
                    StatusMessage = $"Summary exported to HTML: {Path.GetFileName(saveFileDialog.FileName)}";
                    MessageBox.Show($"Summary exported successfully!\n\nFile: {saveFileDialog.FileName}",
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error exporting summary: {ex.Message}";
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteExportChallengerSummaryExcel(object parameter)
        {
            if (!CanExecuteExportChallengerClassification(parameter)) return;

            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
                    DefaultExt = "xlsx",
                    FileName = $"{SelectedChallengeForClassification.Name.Replace(" ", "_")}_Summary_{DateTime.Now:yyyyMMdd}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    ExportChallengerSummaryToExcel(saveFileDialog.FileName);
                    StatusMessage = $"Summary exported to Excel: {Path.GetFileName(saveFileDialog.FileName)}";
                    MessageBox.Show($"Summary exported successfully!\n\nFile: {saveFileDialog.FileName}",
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error exporting summary: {ex.Message}";
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteExportChallengerSummaryWord(object parameter)
        {
            if (!CanExecuteExportChallengerClassification(parameter)) return;

            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Word Documents (*.docx)|*.docx|All Files (*.*)|*.*",
                    DefaultExt = "docx",
                    FileName = $"{SelectedChallengeForClassification.Name.Replace(" ", "_")}_Summary_{DateTime.Now:yyyyMMdd}.docx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    ExportChallengerSummaryToWord(saveFileDialog.FileName);
                    StatusMessage = $"Summary exported to Word: {Path.GetFileName(saveFileDialog.FileName)}";
                    MessageBox.Show($"Summary exported successfully!\n\nFile: {saveFileDialog.FileName}",
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error exporting summary: {ex.Message}";
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteExportChallengerDetailedHtml(object parameter)
        {
            if (!CanExecuteExportChallengerClassification(parameter)) return;

            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "HTML Files (*.html)|*.html|All Files (*.*)|*.*",
                    DefaultExt = "html",
                    FileName = $"{SelectedChallengeForClassification.Name.Replace(" ", "_")}_Detailed_{DateTime.Now:yyyyMMdd}.html"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    ExportChallengerClassificationToHtml(saveFileDialog.FileName, false); // false = detailed
                    StatusMessage = $"Detailed view exported to HTML: {Path.GetFileName(saveFileDialog.FileName)}";
                    MessageBox.Show($"Detailed view exported successfully!\n\nFile: {saveFileDialog.FileName}",
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error exporting detailed view: {ex.Message}";
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteExportChallengerDetailedExcel(object parameter)
        {
            if (!CanExecuteExportChallengerClassification(parameter)) return;

            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
                    DefaultExt = "xlsx",
                    FileName = $"{SelectedChallengeForClassification.Name.Replace(" ", "_")}_Detailed_{DateTime.Now:yyyyMMdd}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    ExportChallengerDetailedToExcel(saveFileDialog.FileName);
                    StatusMessage = $"Detailed view exported to Excel: {Path.GetFileName(saveFileDialog.FileName)}";
                    MessageBox.Show($"Detailed view exported successfully!\n\nFile: {saveFileDialog.FileName}",
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error exporting detailed view: {ex.Message}";
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteExportChallengerDetailedWord(object parameter)
        {
            if (!CanExecuteExportChallengerClassification(parameter)) return;

            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Word Documents (*.docx)|*.docx|All Files (*.*)|*.*",
                    DefaultExt = "docx",
                    FileName = $"{SelectedChallengeForClassification.Name.Replace(" ", "_")}_Detailed_{DateTime.Now:yyyyMMdd}.docx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    ExportChallengerDetailedToWord(saveFileDialog.FileName);
                    StatusMessage = $"Detailed view exported to Word: {Path.GetFileName(saveFileDialog.FileName)}";
                    MessageBox.Show($"Detailed view exported successfully!\n\nFile: {saveFileDialog.FileName}",
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error exporting detailed view: {ex.Message}";
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Summary Export Implementations

        private void ExportChallengerSummaryToHtml(string filePath)
        {
            var challengeName = SelectedChallengeForClassification?.Name ?? "Challenge";
            var challengeYear = SelectedChallengeForClassification?.Year ?? SelectedYear;

            using (var writer = new StreamWriter(filePath))
            {
                writer.WriteLine("<!DOCTYPE html>");
                writer.WriteLine("<html>");
                writer.WriteLine("<head>");
                writer.WriteLine("    <meta charset='utf-8'>");
                writer.WriteLine($"    <title>{challengeName} - Summary</title>");
                writer.WriteLine("    <style>");
                writer.WriteLine("        body { font-family: Arial, sans-serif; margin: 20px; }");
                writer.WriteLine("        h1 { color: #FF9800; }");
                writer.WriteLine("        table { border-collapse: collapse; width: 100%; margin-top: 20px; }");
                writer.WriteLine("        th, td { border: 1px solid #ddd; padding: 10px; text-align: left; }");
                writer.WriteLine("        th { background-color: #FF9800; color: white; font-weight: bold; }");
                writer.WriteLine("        tr:nth-child(even) { background-color: #f2f2f2; }");
                writer.WriteLine("        tr:hover { background-color: #FFE0B2; }");
                writer.WriteLine("        .rank { font-weight: bold; font-size: 18px; color: #FF9800; }");
                writer.WriteLine("        .summary { background-color: #FFF3E0; padding: 15px; margin: 20px 0; border-radius: 5px; }");
                writer.WriteLine("    </style>");
                writer.WriteLine("</head>");
                writer.WriteLine("<body>");
                writer.WriteLine($"    <h1>🏆 {challengeName} - Summary</h1>");
                writer.WriteLine($"    <div class='summary'>");
                writer.WriteLine($"        <strong>Year:</strong> {challengeYear}<br/>");
                writer.WriteLine($"        <strong>Total Challengers:</strong> {ChallengerClassifications.Count}<br/>");
                writer.WriteLine($"        <strong>Generated:</strong> {DateTime.Now:yyyy-MM-dd HH:mm}");
                writer.WriteLine($"    </div>");

                writer.WriteLine("    <table>");
                writer.WriteLine("        <thead>");
                writer.WriteLine("            <tr>");
                writer.WriteLine("                <th>Rank</th>");
                writer.WriteLine("                <th>Name</th>");
                writer.WriteLine("                <th>Total Points</th>");
                writer.WriteLine("                <th>Total Races</th>");
                writer.WriteLine("                <th>Total KMs</th>");
                writer.WriteLine("            </tr>");
                writer.WriteLine("        </thead>");
                writer.WriteLine("        <tbody>");

                foreach (var challenger in ChallengerClassifications)
                {
                    writer.WriteLine("            <tr>");
                    writer.WriteLine($"                <td class='rank'>#{challenger.RankByPoints}</td>");
                    writer.WriteLine($"                <td><strong>{challenger.ChallengerFirstName} {challenger.ChallengerLastName}</strong></td>");
                    writer.WriteLine($"                <td>{challenger.TotalPoints}</td>");
                    writer.WriteLine($"                <td>{challenger.RaceCount}</td>");
                    writer.WriteLine($"                <td>{challenger.TotalKilometers} km</td>");
                    writer.WriteLine("            </tr>");
                }

                writer.WriteLine("        </tbody>");
                writer.WriteLine("    </table>");
                writer.WriteLine("</body>");
                writer.WriteLine("</html>");
            }
        }

        private void ExportChallengerSummaryToExcel(string filePath)
        {
            var challengeName = SelectedChallengeForClassification?.Name ?? "Challenge";
            var challengeYear = SelectedChallengeForClassification?.Year ?? SelectedYear;

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Summary");

                // Title
                worksheet.Cells[1, 1].Value = $"{challengeName} - Summary";
                worksheet.Cells[1, 1, 1, 5].Merge = true;
                worksheet.Cells[1, 1].Style.Font.Size = 18;
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Info
                worksheet.Cells[2, 1].Value = $"Year: {challengeYear}";
                worksheet.Cells[3, 1].Value = $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}";

                // Headers
                int headerRow = 5;
                worksheet.Cells[headerRow, 1].Value = "Rank";
                worksheet.Cells[headerRow, 2].Value = "Name";
                worksheet.Cells[headerRow, 3].Value = "Total Points";
                worksheet.Cells[headerRow, 4].Value = "Total Races";
                worksheet.Cells[headerRow, 5].Value = "Total KMs";

                using (var range = worksheet.Cells[headerRow, 1, headerRow, 5])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Orange);
                    range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                // Data
                int row = headerRow + 1;
                foreach (var challenger in ChallengerClassifications)
                {
                    worksheet.Cells[row, 1].Value = challenger.RankByPoints;
                    worksheet.Cells[row, 2].Value = $"{challenger.ChallengerFirstName} {challenger.ChallengerLastName}";
                    worksheet.Cells[row, 3].Value = challenger.TotalPoints;
                    worksheet.Cells[row, 4].Value = challenger.RaceCount;
                    worksheet.Cells[row, 5].Value = challenger.TotalKilometers;

                    // Highlight top 3
                    if (challenger.RankByPoints <= 3)
                    {
                        using (var rowRange = worksheet.Cells[row, 1, row, 5])
                        {
                            rowRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            rowRange.Style.Fill.BackgroundColor.SetColor(
                                challenger.RankByPoints == 1 ? System.Drawing.Color.Gold :
                                challenger.RankByPoints == 2 ? System.Drawing.Color.Silver :
                                System.Drawing.Color.FromArgb(205, 127, 50)); // Bronze
                            rowRange.Style.Font.Bold = true;
                        }
                    }

                    row++;
                }

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                worksheet.Column(2).Width = 25; // Name column wider

                package.SaveAs(new FileInfo(filePath));
            }
        }

        private void ExportChallengerSummaryToWord(string filePath)
        {
            var challengeName = SelectedChallengeForClassification?.Name ?? "Challenge";
            var challengeYear = SelectedChallengeForClassification?.Year ?? SelectedYear;

            using (var document = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
            {
                var mainPart = document.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = mainPart.Document.AppendChild(new Body());

                // Title
                var titlePara = body.AppendChild(new Paragraph());
                var titleRun = titlePara.AppendChild(new Run());
                titleRun.AppendChild(new Text($"{challengeName} - Summary"));
                var titleProps = titleRun.AppendChild(new RunProperties());
                titleProps.AppendChild(new Bold());
                titleProps.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.FontSize { Val = "32" });

                // Info
                var infoPara = body.AppendChild(new Paragraph());
                var infoRun = infoPara.AppendChild(new Run());
                infoRun.AppendChild(new Text($"Year: {challengeYear} | Generated: {DateTime.Now:yyyy-MM-dd HH:mm}"));

                body.AppendChild(new Paragraph()); // Spacing

                // Table
                var table = new Table();

                // Header row
                var headerRow = new TableRow();
                AddWordTableCell(headerRow, "Rank");
                AddWordTableCell(headerRow, "Name");
                AddWordTableCell(headerRow, "Total Points");
                AddWordTableCell(headerRow, "Total Races");
                AddWordTableCell(headerRow, "Total KMs");
                table.AppendChild(headerRow);

                // Data rows
                foreach (var challenger in ChallengerClassifications)
                {
                    var dataRow = new TableRow();
                    AddWordTableCell(dataRow, $"#{challenger.RankByPoints}");
                    AddWordTableCell(dataRow, $"{challenger.ChallengerFirstName} {challenger.ChallengerLastName}");
                    AddWordTableCell(dataRow, challenger.TotalPoints.ToString());
                    AddWordTableCell(dataRow, challenger.RaceCount.ToString());
                    AddWordTableCell(dataRow, $"{challenger.TotalKilometers} km");
                    table.AppendChild(dataRow);
                }

                body.AppendChild(table);
                mainPart.Document.Save();
            }
        }

        // Detailed Export Implementations

        private void ExportChallengerDetailedToExcel(string filePath)
        {
            var challengeName = SelectedChallengeForClassification?.Name ?? "Challenge";
            var challengeYear = SelectedChallengeForClassification?.Year ?? SelectedYear;

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                foreach (var challenger in ChallengerClassifications)
                {
                    var safeName = $"{challenger.ChallengerFirstName} {challenger.ChallengerLastName}";
                    // Excel worksheet names have max 31 chars and can't contain special chars
                    safeName = new string(safeName.Take(31).Where(c => !@":\/?*[]".Contains(c)).ToArray());

                    var worksheet = package.Workbook.Worksheets.Add(safeName);

                    // Challenger info
                    worksheet.Cells[1, 1].Value = $"{challenger.ChallengerFirstName} {challenger.ChallengerLastName}";
                    worksheet.Cells[1, 1].Style.Font.Size = 16;
                    worksheet.Cells[1, 1].Style.Font.Bold = true;

                    worksheet.Cells[2, 1].Value = $"Rank: #{challenger.RankByPoints}";
                    worksheet.Cells[3, 1].Value = $"Total Points: {challenger.TotalPoints}";
                    worksheet.Cells[4, 1].Value = $"Total Races: {challenger.RaceCount}";
                    worksheet.Cells[5, 1].Value = $"Total KMs: {challenger.TotalKilometers}";

                    // Race details headers
                    int headerRow = 7;
                    worksheet.Cells[headerRow, 1].Value = "Race #";
                    worksheet.Cells[headerRow, 2].Value = "Race Name";
                    worksheet.Cells[headerRow, 3].Value = "Distance";
                    worksheet.Cells[headerRow, 4].Value = "Position";
                    worksheet.Cells[headerRow, 5].Value = "Points";
                    worksheet.Cells[headerRow, 6].Value = "Bonus";
                    worksheet.Cells[headerRow, 7].Value = "In Best 7";

                    using (var range = worksheet.Cells[headerRow, 1, headerRow, 7])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                    }

                    // Race details data
                    int row = headerRow + 1;
                    foreach (var raceDetail in challenger.RaceDetails)
                    {
                        worksheet.Cells[row, 1].Value = raceDetail.RaceNumber;
                        worksheet.Cells[row, 2].Value = raceDetail.RaceName;
                        worksheet.Cells[row, 3].Value = $"{raceDetail.DistanceKm} km";
                        worksheet.Cells[row, 4].Value = raceDetail.Position;
                        worksheet.Cells[row, 5].Value = raceDetail.Points;
                        worksheet.Cells[row, 6].Value = raceDetail.BonusKm;
                        worksheet.Cells[row, 7].Value = raceDetail.IsInBest7 ? "✓" : "";

                        if (raceDetail.IsInBest7)
                        {
                            using (var rowRange = worksheet.Cells[row, 1, row, 7])
                            {
                                rowRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                                rowRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);
                                rowRange.Style.Font.Bold = true;
                            }
                        }

                        row++;
                    }

                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                }

                package.SaveAs(new FileInfo(filePath));
            }
        }

        private void ExportChallengerDetailedToWord(string filePath)
        {
            var challengeName = SelectedChallengeForClassification?.Name ?? "Challenge";
            var challengeYear = SelectedChallengeForClassification?.Year ?? SelectedYear;

            using (var document = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
            {
                var mainPart = document.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = mainPart.Document.AppendChild(new Body());

                // Title
                var titlePara = body.AppendChild(new Paragraph());
                var titleRun = titlePara.AppendChild(new Run());
                titleRun.AppendChild(new Text($"{challengeName} - Detailed Results"));
                var titleProps = titleRun.AppendChild(new RunProperties());
                titleProps.AppendChild(new Bold());
                titleProps.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.FontSize { Val = "32" });

                foreach (var challenger in ChallengerClassifications)
                {
                    // Challenger header
                    var chalPara = body.AppendChild(new Paragraph());
                    var chalRun = chalPara.AppendChild(new Run());
                    chalRun.AppendChild(new Text($"#{challenger.RankByPoints} - {challenger.ChallengerFirstName} {challenger.ChallengerLastName}"));
                    var chalProps = chalRun.AppendChild(new RunProperties());
                    chalProps.AppendChild(new Bold());
                    chalProps.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.FontSize { Val = "24" });

                    // Stats
                    var statsPara = body.AppendChild(new Paragraph());
                    var statsRun = statsPara.AppendChild(new Run());
                    statsRun.AppendChild(new Text($"Points: {challenger.TotalPoints} | Races: {challenger.RaceCount} | KMs: {challenger.TotalKilometers}"));

                    // Race details table
                    var table = new Table();
                    var headerRow = new TableRow();
                    AddWordTableCell(headerRow, "#");
                    AddWordTableCell(headerRow, "Race");
                    AddWordTableCell(headerRow, "Dist");
                    AddWordTableCell(headerRow, "Pos");
                    AddWordTableCell(headerRow, "Pts");
                    AddWordTableCell(headerRow, "Bonus");
                    AddWordTableCell(headerRow, "Best7");
                    table.AppendChild(headerRow);

                    foreach (var rd in challenger.RaceDetails)
                    {
                        var dataRow = new TableRow();
                        AddWordTableCell(dataRow, rd.RaceNumber.ToString());
                        AddWordTableCell(dataRow, rd.RaceName);
                        AddWordTableCell(dataRow, $"{rd.DistanceKm}km");
                        AddWordTableCell(dataRow, rd.Position.ToString());
                        AddWordTableCell(dataRow, rd.Points.ToString());
                        AddWordTableCell(dataRow, rd.BonusKm.ToString());
                        AddWordTableCell(dataRow, rd.IsInBest7 ? "✓" : "");
                        table.AppendChild(dataRow);
                    }

                    body.AppendChild(table);
                    body.AppendChild(new Paragraph()); // Spacing
                }

                mainPart.Document.Save();
            }
        }
    }

    public class LanguageOption
    {
        public string Code { get; set; }
        public string DisplayName { get; set; }
    }
}
