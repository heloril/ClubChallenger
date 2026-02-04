using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using NameParser.Infrastructure.Data;
using NameParser.Infrastructure.Data.Models;
using NameParser.Infrastructure.Parsers;

namespace NameParser.UI.ViewModels
{
    public class RaceEventManagementViewModel : ViewModelBase
    {
        private readonly RaceEventRepository _raceEventRepository;
        private readonly ChallengeRepository _challengeRepository;
        private readonly RaceEventExcelParser _excelParser;
        
        private RaceEventEntity _selectedRaceEvent;
        private string _eventName;
        private DateTime _eventDate;
        private string _location;
        private string _websiteUrl;
        private string _description;
        private string _statusMessage;
        private string _importFilePath;
        private string _newDistanceKm;
        private RaceEventDistanceEntity _selectedDistance;

        public RaceEventManagementViewModel()
        {
            _raceEventRepository = new RaceEventRepository();
            _challengeRepository = new ChallengeRepository();
            _excelParser = new RaceEventExcelParser();

            EventDate = DateTime.Now;

            RaceEvents = new ObservableCollection<RaceEventEntity>();
            AssociatedChallenges = new ObservableCollection<ChallengeEntity>();
            AssociatedRaces = new ObservableCollection<RaceEntity>();
            AvailableDistances = new ObservableCollection<RaceEventDistanceEntity>();

            CreateEventCommand = new RelayCommand(ExecuteCreateEvent, CanExecuteCreateEvent);
            UpdateEventCommand = new RelayCommand(ExecuteUpdateEvent, CanExecuteUpdateEvent);
            DeleteEventCommand = new RelayCommand(ExecuteDeleteEvent, CanExecuteDeleteEvent);
            ClearFormCommand = new RelayCommand(ExecuteClearForm);
            BrowseImportFileCommand = new RelayCommand(ExecuteBrowseImportFile);
            ImportFromExcelCommand = new RelayCommand(ExecuteImportFromExcel, CanExecuteImportFromExcel);
            AddDistanceCommand = new RelayCommand(ExecuteAddDistance, CanExecuteAddDistance);
            RemoveDistanceCommand = new RelayCommand(ExecuteRemoveDistance, CanExecuteRemoveDistance);

            LoadRaceEvents();
        }

        public ObservableCollection<RaceEventEntity> RaceEvents { get; }
        public ObservableCollection<ChallengeEntity> AssociatedChallenges { get; }
        public ObservableCollection<RaceEntity> AssociatedRaces { get; }
        public ObservableCollection<RaceEventDistanceEntity> AvailableDistances { get; }

        public ICommand CreateEventCommand { get; }
        public ICommand UpdateEventCommand { get; }
        public ICommand DeleteEventCommand { get; }
        public ICommand ClearFormCommand { get; }
        public ICommand BrowseImportFileCommand { get; }
        public ICommand ImportFromExcelCommand { get; }
        public ICommand AddDistanceCommand { get; }
        public ICommand RemoveDistanceCommand { get; }

        public RaceEventEntity SelectedRaceEvent
        {
            get => _selectedRaceEvent;
            set
            {
                if (SetProperty(ref _selectedRaceEvent, value))
                {
                    LoadEventDetails();
                    (UpdateEventCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (DeleteEventCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public string EventName
        {
            get => _eventName;
            set
            {
                SetProperty(ref _eventName, value);
                (CreateEventCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public DateTime EventDate
        {
            get => _eventDate;
            set
            {
                SetProperty(ref _eventDate, value);
                (CreateEventCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public string Location
        {
            get => _location;
            set => SetProperty(ref _location, value);
        }

        public string WebsiteUrl
        {
            get => _websiteUrl;
            set => SetProperty(ref _websiteUrl, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string ImportFilePath
        {
            get => _importFilePath;
            set
            {
                SetProperty(ref _importFilePath, value);
                (ImportFromExcelCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public string NewDistanceKm
        {
            get => _newDistanceKm;
            set
            {
                SetProperty(ref _newDistanceKm, value);
                (AddDistanceCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public RaceEventDistanceEntity SelectedDistance
        {
            get => _selectedDistance;
            set
            {
                SetProperty(ref _selectedDistance, value);
                (RemoveDistanceCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        private void LoadRaceEvents()
        {
            RaceEvents.Clear();
            var events = _raceEventRepository.GetAll();
            foreach (var evt in events)
            {
                RaceEvents.Add(evt);
            }
        }

        private void LoadEventDetails()
        {
            if (SelectedRaceEvent != null)
            {
                EventName = SelectedRaceEvent.Name;
                EventDate = SelectedRaceEvent.EventDate;
                Location = SelectedRaceEvent.Location;
                WebsiteUrl = SelectedRaceEvent.WebsiteUrl;
                Description = SelectedRaceEvent.Description;

                LoadAssociatedData();
            }
        }

        private void LoadAssociatedData()
        {
            AssociatedChallenges.Clear();
            AssociatedRaces.Clear();
            AvailableDistances.Clear();

            if (SelectedRaceEvent != null)
            {
                var challenges = _raceEventRepository.GetChallengesByEvent(SelectedRaceEvent.Id);
                foreach (var challenge in challenges)
                {
                    AssociatedChallenges.Add(challenge);
                }

                var races = _raceEventRepository.GetRacesByEvent(SelectedRaceEvent.Id);
                foreach (var race in races)
                {
                    AssociatedRaces.Add(race);
                }

                var distances = _raceEventRepository.GetDistancesByEvent(SelectedRaceEvent.Id);
                foreach (var distance in distances)
                {
                    AvailableDistances.Add(distance);
                }
            }
        }

        private bool CanExecuteCreateEvent(object parameter)
        {
            return !string.IsNullOrWhiteSpace(EventName) && EventDate != DateTime.MinValue;
        }

        private void ExecuteCreateEvent(object parameter)
        {
            try
            {
                var raceEvent = new RaceEventEntity
                {
                    Name = EventName,
                    EventDate = EventDate,
                    Location = Location,
                    WebsiteUrl = WebsiteUrl,
                    Description = Description
                };

                _raceEventRepository.Create(raceEvent);
                LoadRaceEvents();
                ExecuteClearForm(null);
                StatusMessage = "Race event created successfully!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                MessageBox.Show($"Error creating race event: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanExecuteUpdateEvent(object parameter)
        {
            return SelectedRaceEvent != null && !string.IsNullOrWhiteSpace(EventName);
        }

        private void ExecuteUpdateEvent(object parameter)
        {
            try
            {
                SelectedRaceEvent.Name = EventName;
                SelectedRaceEvent.EventDate = EventDate;
                SelectedRaceEvent.Location = Location;
                SelectedRaceEvent.WebsiteUrl = WebsiteUrl;
                SelectedRaceEvent.Description = Description;

                _raceEventRepository.Update(SelectedRaceEvent);
                LoadRaceEvents();
                StatusMessage = "Race event updated successfully!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                MessageBox.Show($"Error updating race event: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanExecuteDeleteEvent(object parameter)
        {
            return SelectedRaceEvent != null;
        }

        private void ExecuteDeleteEvent(object parameter)
        {
            var result = MessageBox.Show(
                $"Are you sure you want to delete race event '{SelectedRaceEvent.Name}'?\nThis will also remove all associations with challenges.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _raceEventRepository.Delete(SelectedRaceEvent.Id);
                    LoadRaceEvents();
                    ExecuteClearForm(null);
                    StatusMessage = "Race event deleted successfully!";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error: {ex.Message}";
                    MessageBox.Show($"Error deleting race event: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExecuteBrowseImportFile(object parameter)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
                Title = "Select Race Events Excel File"
            };

            if (dialog.ShowDialog() == true)
            {
                ImportFilePath = dialog.FileName;
            }
        }

        private bool CanExecuteImportFromExcel(object parameter)
        {
            return !string.IsNullOrWhiteSpace(ImportFilePath) && System.IO.File.Exists(ImportFilePath);
        }

        private void ExecuteImportFromExcel(object parameter)
        {
            try
            {
                var eventsWithDistances = _excelParser.ParseWithDistances(ImportFilePath);
                
                int imported = 0;
                int skipped = 0;
                int totalDistances = 0;

                foreach (var item in eventsWithDistances)
                {
                    try
                    {
                        var eventId = _raceEventRepository.Create(item.raceEvent);
                        imported++;

                        // Add distances to the race event
                        foreach (var distance in item.distances)
                        {
                            try
                            {
                                _raceEventRepository.AddDistance(eventId, distance);
                                totalDistances++;
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Failed to add distance {distance} to event {item.raceEvent.Name}: {ex.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        skipped++;
                        System.Diagnostics.Debug.WriteLine($"Skipped event {item.raceEvent.Name}: {ex.Message}");
                    }
                }

                LoadRaceEvents();
                StatusMessage = $"Import completed! {imported} events imported ({totalDistances} distances), {skipped} skipped.";
                MessageBox.Show(
                    $"Import completed!\n\nImported: {imported} events\nDistances: {totalDistances}\nSkipped: {skipped} events",
                    "Import Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                MessageBox.Show($"Error importing from Excel: {ex.Message}\n\nStack trace: {ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanExecuteAddDistance(object parameter)
        {
            return SelectedRaceEvent != null && 
                   !string.IsNullOrWhiteSpace(NewDistanceKm) && 
                   decimal.TryParse(NewDistanceKm, out decimal distance) && 
                   distance > 0;
        }

        private void ExecuteAddDistance(object parameter)
        {
            try
            {
                if (decimal.TryParse(NewDistanceKm, out decimal distance))
                {
                    _raceEventRepository.AddDistance(SelectedRaceEvent.Id, distance);
                    LoadAssociatedData();
                    NewDistanceKm = string.Empty;
                    StatusMessage = $"Distance {distance} km added successfully!";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                MessageBox.Show($"Error adding distance: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanExecuteRemoveDistance(object parameter)
        {
            return SelectedDistance != null;
        }

        private void ExecuteRemoveDistance(object parameter)
        {
            try
            {
                _raceEventRepository.RemoveDistance(SelectedDistance.Id);
                LoadAssociatedData();
                StatusMessage = $"Distance {SelectedDistance.DistanceKm} km removed successfully!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                MessageBox.Show($"Error removing distance: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteClearForm(object parameter)
        {
            SelectedRaceEvent = null;
            EventName = string.Empty;
            EventDate = DateTime.Now;
            Location = string.Empty;
            WebsiteUrl = string.Empty;
            Description = string.Empty;
            NewDistanceKm = string.Empty;
            AssociatedChallenges.Clear();
            AssociatedRaces.Clear();
            AvailableDistances.Clear();
        }
    }
}
