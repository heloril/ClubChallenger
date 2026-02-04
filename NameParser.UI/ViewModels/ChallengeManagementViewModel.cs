using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using NameParser.Infrastructure.Data;
using NameParser.Infrastructure.Data.Models;

namespace NameParser.UI.ViewModels
{
    public class ChallengeManagementViewModel : ViewModelBase
    {
        private readonly ChallengeRepository _challengeRepository;
        private readonly RaceEventRepository _raceEventRepository;
        
        private ChallengeEntity _selectedChallenge;
        private string _challengeName;
        private string _challengeDescription;
        private int _challengeYear;
        private DateTime? _startDate;
        private DateTime? _endDate;
        private string _statusMessage;
        private RaceEventEntity _selectedAvailableEvent;
        private RaceEventEntity _selectedAssociatedEvent;

        public ChallengeManagementViewModel()
        {
            _challengeRepository = new ChallengeRepository();
            _raceEventRepository = new RaceEventRepository();

            ChallengeYear = DateTime.Now.Year;

            Challenges = new ObservableCollection<ChallengeEntity>();
            AvailableRaceEvents = new ObservableCollection<RaceEventEntity>();
            AssociatedRaceEvents = new ObservableCollection<RaceEventEntity>();

            CreateChallengeCommand = new RelayCommand(ExecuteCreateChallenge, CanExecuteCreateChallenge);
            UpdateChallengeCommand = new RelayCommand(ExecuteUpdateChallenge, CanExecuteUpdateChallenge);
            DeleteChallengeCommand = new RelayCommand(ExecuteDeleteChallenge, CanExecuteDeleteChallenge);
            AddRaceEventCommand = new RelayCommand(ExecuteAddRaceEvent, CanExecuteAddRaceEvent);
            RemoveRaceEventCommand = new RelayCommand(ExecuteRemoveRaceEvent, CanExecuteRemoveRaceEvent);
            ClearFormCommand = new RelayCommand(ExecuteClearForm);

            LoadChallenges();
            LoadAvailableRaceEvents();
        }

        public ObservableCollection<ChallengeEntity> Challenges { get; }
        public ObservableCollection<RaceEventEntity> AvailableRaceEvents { get; }
        public ObservableCollection<RaceEventEntity> AssociatedRaceEvents { get; }

        public ICommand CreateChallengeCommand { get; }
        public ICommand UpdateChallengeCommand { get; }
        public ICommand DeleteChallengeCommand { get; }
        public ICommand AddRaceEventCommand { get; }
        public ICommand RemoveRaceEventCommand { get; }
        public ICommand ClearFormCommand { get; }

        public ChallengeEntity SelectedChallenge
        {
            get => _selectedChallenge;
            set
            {
                if (SetProperty(ref _selectedChallenge, value))
                {
                    LoadChallengeDetails();
                    (UpdateChallengeCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (DeleteChallengeCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public string ChallengeName
        {
            get => _challengeName;
            set
            {
                SetProperty(ref _challengeName, value);
                (CreateChallengeCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public string ChallengeDescription
        {
            get => _challengeDescription;
            set => SetProperty(ref _challengeDescription, value);
        }

        public int ChallengeYear
        {
            get => _challengeYear;
            set
            {
                SetProperty(ref _challengeYear, value);
                (CreateChallengeCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public DateTime? StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }

        public DateTime? EndDate
        {
            get => _endDate;
            set => SetProperty(ref _endDate, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public RaceEventEntity SelectedAvailableEvent
        {
            get => _selectedAvailableEvent;
            set
            {
                SetProperty(ref _selectedAvailableEvent, value);
                (AddRaceEventCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public RaceEventEntity SelectedAssociatedEvent
        {
            get => _selectedAssociatedEvent;
            set
            {
                SetProperty(ref _selectedAssociatedEvent, value);
                (RemoveRaceEventCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        private void LoadChallenges()
        {
            Challenges.Clear();
            var challenges = _challengeRepository.GetAll();
            foreach (var challenge in challenges)
            {
                Challenges.Add(challenge);
            }
        }

        private void LoadAvailableRaceEvents()
        {
            AvailableRaceEvents.Clear();
            var events = _raceEventRepository.GetAll();
            foreach (var evt in events)
            {
                AvailableRaceEvents.Add(evt);
            }
        }

        private void LoadChallengeDetails()
        {
            if (SelectedChallenge != null)
            {
                ChallengeName = SelectedChallenge.Name;
                ChallengeDescription = SelectedChallenge.Description;
                ChallengeYear = SelectedChallenge.Year;
                StartDate = SelectedChallenge.StartDate;
                EndDate = SelectedChallenge.EndDate;

                LoadAssociatedRaceEvents();
            }
        }

        private void LoadAssociatedRaceEvents()
        {
            AssociatedRaceEvents.Clear();
            if (SelectedChallenge != null)
            {
                var events = _challengeRepository.GetRaceEventsByChallenge(SelectedChallenge.Id);
                foreach (var evt in events)
                {
                    AssociatedRaceEvents.Add(evt);
                }
            }
        }

        private bool CanExecuteCreateChallenge(object parameter)
        {
            return !string.IsNullOrWhiteSpace(ChallengeName) && ChallengeYear > 0;
        }

        private void ExecuteCreateChallenge(object parameter)
        {
            try
            {
                var challenge = new ChallengeEntity
                {
                    Name = ChallengeName,
                    Description = ChallengeDescription,
                    Year = ChallengeYear,
                    StartDate = StartDate,
                    EndDate = EndDate
                };

                _challengeRepository.Create(challenge);
                LoadChallenges();
                ExecuteClearForm(null);
                StatusMessage = "Challenge created successfully!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                MessageBox.Show($"Error creating challenge: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanExecuteUpdateChallenge(object parameter)
        {
            return SelectedChallenge != null && !string.IsNullOrWhiteSpace(ChallengeName);
        }

        private void ExecuteUpdateChallenge(object parameter)
        {
            try
            {
                SelectedChallenge.Name = ChallengeName;
                SelectedChallenge.Description = ChallengeDescription;
                SelectedChallenge.Year = ChallengeYear;
                SelectedChallenge.StartDate = StartDate;
                SelectedChallenge.EndDate = EndDate;

                _challengeRepository.Update(SelectedChallenge);
                LoadChallenges();
                StatusMessage = "Challenge updated successfully!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                MessageBox.Show($"Error updating challenge: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanExecuteDeleteChallenge(object parameter)
        {
            return SelectedChallenge != null;
        }

        private void ExecuteDeleteChallenge(object parameter)
        {
            var result = MessageBox.Show(
                $"Are you sure you want to delete challenge '{SelectedChallenge.Name}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _challengeRepository.Delete(SelectedChallenge.Id);
                    LoadChallenges();
                    ExecuteClearForm(null);
                    StatusMessage = "Challenge deleted successfully!";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error: {ex.Message}";
                    MessageBox.Show($"Error deleting challenge: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private bool CanExecuteAddRaceEvent(object parameter)
        {
            return SelectedChallenge != null && SelectedAvailableEvent != null;
        }

        private void ExecuteAddRaceEvent(object parameter)
        {
            try
            {
                _challengeRepository.AssociateRaceEvent(
                    SelectedChallenge.Id, 
                    SelectedAvailableEvent.Id, 
                    AssociatedRaceEvents.Count);
                
                LoadAssociatedRaceEvents();
                StatusMessage = $"Race event '{SelectedAvailableEvent.Name}' added to challenge!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                MessageBox.Show($"Error adding race event: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanExecuteRemoveRaceEvent(object parameter)
        {
            return SelectedChallenge != null && SelectedAssociatedEvent != null;
        }

        private void ExecuteRemoveRaceEvent(object parameter)
        {
            try
            {
                _challengeRepository.DisassociateRaceEvent(
                    SelectedChallenge.Id, 
                    SelectedAssociatedEvent.Id);
                
                LoadAssociatedRaceEvents();
                StatusMessage = $"Race event '{SelectedAssociatedEvent.Name}' removed from challenge!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                MessageBox.Show($"Error removing race event: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteClearForm(object parameter)
        {
            SelectedChallenge = null;
            ChallengeName = string.Empty;
            ChallengeDescription = string.Empty;
            ChallengeYear = DateTime.Now.Year;
            StartDate = null;
            EndDate = null;
            AssociatedRaceEvents.Clear();
        }
    }
}
