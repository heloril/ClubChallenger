using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using NameParser.Domain.Entities;
using NameParser.Infrastructure.Data;
using NameParser.Infrastructure.Data.Models;
using NameParser.Infrastructure.Repositories;
using NameParser.Application.Services;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace NameParser.UI.ViewModels
{
	public class MemberManagementViewModel : ViewModelBase
	{
		private readonly DatabaseMemberRepository _memberRepository;
		private readonly VmaExtrapolationService _vmaExtrapolationService;
		private readonly VmaExtrapolationLogRepository _extrapolationLogRepository = new VmaExtrapolationLogRepository();
		private readonly RecentRaceVmaRepository _recentRaceVmaRepository = new RecentRaceVmaRepository();
		private readonly List<Member> _allMembers = new List<Member>();
		private bool _isExtrapolating;

		private Member _selectedMember;
		private string _firstName;
		private string _lastName;
		private string _email;
		private bool _isMember = true;
		private bool _isChallenger;
		private string _vma;
		private string _statusMessage;
		private string _searchText;
		private bool _showMembers = true;
		private bool _showChallengers = true;

		// Tracks the original identity of the selected member for updates
		private string _originalFirstName;
		private string _originalLastName;

		public MemberManagementViewModel()
		{
			_memberRepository = new DatabaseMemberRepository();
			_vmaExtrapolationService = new VmaExtrapolationService(_memberRepository);

			ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

			Members = new ObservableCollection<Member>();
			VmaTimeline = new ObservableCollection<VmaTimelineItem>();

			AddMemberCommand = new RelayCommand(ExecuteAddMember, CanExecuteAddMember);
			UpdateMemberCommand = new RelayCommand(ExecuteUpdateMember, CanExecuteUpdateMember);
			DeleteMemberCommand = new RelayCommand(ExecuteDeleteMember, CanExecuteDeleteMember);
			ClearFormCommand = new RelayCommand(_ => ClearForm());
			RefreshCommand = new RelayCommand(_ => LoadMembers());
			ExportMembersCommand = new RelayCommand(_ => ExportToExcel(onlyMembers: true, onlyChallengers: false, "Members"));
			ExportChallengersCommand = new RelayCommand(_ => ExportToExcel(onlyMembers: false, onlyChallengers: true, "Challengers"));
			ExtrapolateVmaCommand = new RelayCommand(ExecuteExtrapolateVma, CanExecuteExtrapolateVma);
			ExtrapolateAllVmaCommand = new RelayCommand(ExecuteExtrapolateAllVma, _ => !_isExtrapolating);
			RecalculateRecentRacesVmaCommand = new RelayCommand(ExecuteRecalculateRecentRacesVma, _ => !_isExtrapolating);
			DeleteVmaRecordCommand = new RelayCommand(ExecuteDeleteVmaRecord, p => p is VmaTimelineItem item && item.RecordId.HasValue);

			LoadMembers();
		}

		public ObservableCollection<Member> Members { get; }

		public ObservableCollection<VmaTimelineItem> VmaTimeline { get; }

		public ICommand AddMemberCommand { get; }
		public ICommand UpdateMemberCommand { get; }
		public ICommand DeleteMemberCommand { get; }
		public ICommand ClearFormCommand { get; }
		public ICommand RefreshCommand { get; }
		public ICommand ExportMembersCommand { get; }
		public ICommand ExportChallengersCommand { get; }
		public ICommand ExtrapolateVmaCommand { get; }
		public ICommand ExtrapolateAllVmaCommand { get; }
		public ICommand RecalculateRecentRacesVmaCommand { get; }
		public ICommand DeleteVmaRecordCommand { get; }

		public Member SelectedMember
		{
			get => _selectedMember;
			set
			{
				if (SetProperty(ref _selectedMember, value))
				{
					LoadMemberDetails();
					LoadVmaTimeline();
					(UpdateMemberCommand as RelayCommand)?.RaiseCanExecuteChanged();
					(DeleteMemberCommand as RelayCommand)?.RaiseCanExecuteChanged();
					(ExtrapolateVmaCommand as RelayCommand)?.RaiseCanExecuteChanged();
				}
			}
		}

		public string FirstName
		{
			get => _firstName;
			set => SetProperty(ref _firstName, value);
		}

		public string LastName
		{
			get => _lastName;
			set
			{
				SetProperty(ref _lastName, value);
				(AddMemberCommand as RelayCommand)?.RaiseCanExecuteChanged();
				(UpdateMemberCommand as RelayCommand)?.RaiseCanExecuteChanged();
			}
		}

		public string Email
		{
			get => _email;
			set => SetProperty(ref _email, value);
		}

		public bool IsMember
		{
			get => _isMember;
			set => SetProperty(ref _isMember, value);
		}

		public bool IsChallenger
		{
			get => _isChallenger;
			set => SetProperty(ref _isChallenger, value);
		}

		public string Vma
		{
			get => _vma;
			set => SetProperty(ref _vma, value);
		}

		private static double? ParseVma(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
				return null;

			var normalized = text.Trim().Replace(',', '.');
			if (double.TryParse(normalized, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var value))
				return value;

			return null;
		}

		public string StatusMessage
		{
			get => _statusMessage;
			set => SetProperty(ref _statusMessage, value);
		}

		public string SearchText
		{
			get => _searchText;
			set
			{
				if (SetProperty(ref _searchText, value))
				{
					ApplyFilter();
				}
			}
		}

		public bool ShowMembers
		{
			get => _showMembers;
			set
			{
				if (SetProperty(ref _showMembers, value))
				{
					ApplyFilter();
				}
			}
		}

		public bool ShowChallengers
		{
			get => _showChallengers;
			set
			{
				if (SetProperty(ref _showChallengers, value))
				{
					ApplyFilter();
				}
			}
		}

		private void ApplyFilter()
		{
			IEnumerable<Member> filtered = _allMembers;

			// Category filter: when both are unchecked, show everyone (no category restriction)
			if (ShowMembers || ShowChallengers)
			{
				filtered = filtered.Where(m =>
					(ShowMembers && m.IsMember) || (ShowChallengers && m.IsChallenger));
			}

			if (!string.IsNullOrWhiteSpace(SearchText))
			{
				var term = SearchText.Trim();
				filtered = filtered.Where(m =>
					(m.FirstName?.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
					|| (m.LastName?.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
					|| (m.Email?.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0));
			}

			Members.Clear();
			foreach (var member in filtered)
			{
				Members.Add(member);
			}
		}

		private void LoadMembers()
		{
			try
			{
				var members = _memberRepository.GetAll()
					.OrderBy(m => m.LastName, StringComparer.OrdinalIgnoreCase)
					.ThenBy(m => m.FirstName, StringComparer.OrdinalIgnoreCase)
					.ToList();

				_allMembers.Clear();
				_allMembers.AddRange(members);
				ApplyFilter();

				StatusMessage = $"{Members.Count} member(s) loaded.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error loading members: {ex.Message}";
				MessageBox.Show(StatusMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void LoadMemberDetails()
		{
			if (SelectedMember != null)
			{
				FirstName = SelectedMember.FirstName;
				LastName = SelectedMember.LastName;
				Email = SelectedMember.Email;
				IsMember = SelectedMember.IsMember;
				IsChallenger = SelectedMember.IsChallenger;
				Vma = SelectedMember.Vma?.ToString(System.Globalization.CultureInfo.InvariantCulture);

				_originalFirstName = SelectedMember.FirstName;
				_originalLastName = SelectedMember.LastName;
			}
		}

		private void ClearForm()
		{
			SelectedMember = null;
			FirstName = string.Empty;
			LastName = string.Empty;
			Email = string.Empty;
			IsMember = true;
			IsChallenger = false;
			Vma = null;
			_originalFirstName = null;
			_originalLastName = null;
			VmaTimeline.Clear();
		}

		private void LoadVmaTimeline()
		{
			VmaTimeline.Clear();

			if (SelectedMember == null)
				return;

			try
			{
				var items = new List<VmaTimelineItem>();

				var history = _memberRepository.GetVmaHistory(SelectedMember.FirstName, SelectedMember.LastName);
				foreach (var record in history)
				{
					items.Add(new VmaTimelineItem
					{
						Date = record.RecordedDate,
						Vma = record.Vma,
						Type = record.IsExtrapolated ? "VMA extrapolée" : "VMA testée",
						Details = string.Empty,
						RecordId = record.Id
					});
				}

				var logs = _extrapolationLogRepository.GetLogsForMember(SelectedMember.FirstName, SelectedMember.LastName);
				foreach (var log in logs)
				{
					items.Add(new VmaTimelineItem
					{
						Date = log.CreatedDate,
						Vma = log.PredictedVma,
						Type = log.IsSuccess ? $"Extrapolation ({log.Method})" : "Extrapolation (échec)",
						Details = log.Details,
						RecordId = null
					});
				}

				try
				{
					var recentRaceLog = _recentRaceVmaRepository.GetForMember(SelectedMember.FirstName, SelectedMember.LastName);
					if (recentRaceLog != null)
					{
						items.Add(new VmaTimelineItem
						{
							Date = recentRaceLog.CreatedDate,
							Vma = recentRaceLog.AverageVma,
							Type = "VMA 5 dernières courses",
							Details = recentRaceLog.Details,
							RecordId = null
						});

						foreach (var line in recentRaceLog.Lines.OrderByDescending(l => l.RaceDate))
						{
							var distance = line.DistanceKm.HasValue ? $" ({line.DistanceKm} km)" : string.Empty;
							items.Add(new VmaTimelineItem
							{
								Date = line.RaceDate ?? recentRaceLog.CreatedDate,
								Vma = line.EstimatedVma,
								Type = "VMA course",
								Details = $"{line.RaceName}{distance} — vitesse {line.Speed.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)} km/h",
								RecordId = null
							});
						}
					}
				}
				catch
				{
					// Les tables "VMA dernières courses" peuvent ne pas encore exister (migration non appliquée) :
					// on ignore sans empêcher l'affichage du reste de l'historique VMA.
				}

				foreach (var item in items.OrderByDescending(i => i.Date))
				{
					VmaTimeline.Add(item);
				}
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error loading VMA timeline: {ex.Message}";
			}
		}

		private bool CanExecuteExtrapolateVma(object parameter)
		{
			return SelectedMember != null && !_isExtrapolating;
		}

		private async void ExecuteExtrapolateVma(object parameter)
		{
			if (SelectedMember == null || _isExtrapolating)
				return;

			_isExtrapolating = true;
			(ExtrapolateVmaCommand as RelayCommand)?.RaiseCanExecuteChanged();

			try
			{
				var firstName = SelectedMember.FirstName;
				var lastName = SelectedMember.LastName;

				// Run the DB-bound extrapolation and persistence off the UI thread so the
				// window stays responsive while the calibration data is queried.
				var predicted = await Task.Run(() =>
				{
					var value = _vmaExtrapolationService.ExtrapolateVma(firstName, lastName);
					if (value.HasValue)
						_memberRepository.AddVmaRecord(firstName, lastName, value.Value, isExtrapolated: true);
					return value;
				});

				if (!predicted.HasValue)
				{
					LoadVmaTimeline();
					StatusMessage = $"Impossible d'extrapoler la VMA de '{SelectedMember.GetFullName()}' (données de course ou de calibration insuffisantes).";
					MessageBox.Show(StatusMessage, "Extrapolation VMA", MessageBoxButton.OK, MessageBoxImage.Information);
					return;
				}

				StatusMessage = $"VMA extrapolée pour '{SelectedMember.GetFullName()}' : {predicted.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)} km/h.";

				LoadMembers();
				var refreshed = _allMembers.FirstOrDefault(m =>
					string.Equals(m.FirstName ?? string.Empty, firstName ?? string.Empty, StringComparison.OrdinalIgnoreCase)
					&& string.Equals(m.LastName ?? string.Empty, lastName ?? string.Empty, StringComparison.OrdinalIgnoreCase));
				if (refreshed != null)
				{
					SelectedMember = refreshed;
				}
				else
				{
					LoadVmaTimeline();
				}
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error extrapolating VMA: {ex.Message}";
				MessageBox.Show(StatusMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
			finally
			{
				_isExtrapolating = false;
				(ExtrapolateVmaCommand as RelayCommand)?.RaiseCanExecuteChanged();
			}
		}

		private async void ExecuteExtrapolateAllVma(object parameter)
		{
			if (_isExtrapolating)
				return;

			var confirm = MessageBox.Show(
				"Extrapoler automatiquement la VMA de tous les membres ayant plus d'une course ?\n" +
				"Les VMA testées restent toujours les données de référence, et seules les meilleures courses (50 %) de chaque membre sont utilisées.",
				"Extrapolation VMA globale",
				MessageBoxButton.YesNo,
				MessageBoxImage.Question);

			if (confirm != MessageBoxResult.Yes)
				return;

			_isExtrapolating = true;
			(ExtrapolateVmaCommand as RelayCommand)?.RaiseCanExecuteChanged();
			(ExtrapolateAllVmaCommand as RelayCommand)?.RaiseCanExecuteChanged();

			try
			{
				var firstName = SelectedMember?.FirstName;
				var lastName = SelectedMember?.LastName;

				var count = await Task.Run(() => _vmaExtrapolationService.ExtrapolateAllVma(minRaces: 2));

				StatusMessage = $"Extrapolation VMA globale terminée : {count} membre(s) extrapolé(s).";
				MessageBox.Show(StatusMessage, "Extrapolation VMA globale", MessageBoxButton.OK, MessageBoxImage.Information);

				LoadMembers();
				if (firstName != null || lastName != null)
				{
					var refreshed = _allMembers.FirstOrDefault(m =>
						string.Equals(m.FirstName ?? string.Empty, firstName ?? string.Empty, StringComparison.OrdinalIgnoreCase)
						&& string.Equals(m.LastName ?? string.Empty, lastName ?? string.Empty, StringComparison.OrdinalIgnoreCase));
					if (refreshed != null)
					{
						SelectedMember = refreshed;
					}
				}
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error extrapolating all VMAs: {ex.Message}";
				MessageBox.Show(StatusMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
			finally
			{
				_isExtrapolating = false;
				(ExtrapolateVmaCommand as RelayCommand)?.RaiseCanExecuteChanged();
				(ExtrapolateAllVmaCommand as RelayCommand)?.RaiseCanExecuteChanged();
			}
		}

		private async void ExecuteRecalculateRecentRacesVma(object parameter)
		{
			if (_isExtrapolating)
				return;

			var confirm = MessageBox.Show(
				"Recalculer la VMA estimée sur les 5 dernières courses (3 derniers mois) de tous les membres ?",
				"VMA sur dernières courses",
				MessageBoxButton.YesNo,
				MessageBoxImage.Question);

			if (confirm != MessageBoxResult.Yes)
				return;

			_isExtrapolating = true;
			(ExtrapolateVmaCommand as RelayCommand)?.RaiseCanExecuteChanged();
			(ExtrapolateAllVmaCommand as RelayCommand)?.RaiseCanExecuteChanged();
			(RecalculateRecentRacesVmaCommand as RelayCommand)?.RaiseCanExecuteChanged();

			try
			{
				var count = await Task.Run(() => _vmaExtrapolationService.RecomputeAllRecentRacesVma());

				StatusMessage = $"VMA sur dernières courses recalculée : {count} membre(s) estimé(s).";
				MessageBox.Show(StatusMessage, "VMA sur dernières courses", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error recalculating recent-races VMA: {ex.Message}";
				MessageBox.Show(StatusMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
			finally
			{
				_isExtrapolating = false;
				(ExtrapolateVmaCommand as RelayCommand)?.RaiseCanExecuteChanged();
				(ExtrapolateAllVmaCommand as RelayCommand)?.RaiseCanExecuteChanged();
				(RecalculateRecentRacesVmaCommand as RelayCommand)?.RaiseCanExecuteChanged();
			}
		}

		private void ExecuteDeleteVmaRecord(object parameter)
		{
			if (!(parameter is VmaTimelineItem item) || !item.RecordId.HasValue)
				return;

			var result = MessageBox.Show(
				$"Supprimer cette entrée VMA ({item.VmaDisplay} km/h du {item.Date:dd/MM/yyyy}) ?",
				"Confirmer la suppression",
				MessageBoxButton.YesNo,
				MessageBoxImage.Question);

			if (result != MessageBoxResult.Yes)
				return;

			try
			{
				_memberRepository.DeleteVmaRecord(item.RecordId.Value);
				StatusMessage = "Entrée VMA supprimée.";

				var firstName = SelectedMember?.FirstName;
				var lastName = SelectedMember?.LastName;

				LoadMembers();
				if (firstName != null || lastName != null)
				{
					var refreshed = _allMembers.FirstOrDefault(m =>
						string.Equals(m.FirstName ?? string.Empty, firstName ?? string.Empty, StringComparison.OrdinalIgnoreCase)
						&& string.Equals(m.LastName ?? string.Empty, lastName ?? string.Empty, StringComparison.OrdinalIgnoreCase));
					if (refreshed != null)
					{
						SelectedMember = refreshed;
					}
				}
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error deleting VMA record: {ex.Message}";
				MessageBox.Show(StatusMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private bool CanExecuteAddMember(object parameter)
		{
			return !string.IsNullOrWhiteSpace(LastName);
		}

		private void ExecuteAddMember(object parameter)
		{
			try
			{
				var member = new Member(FirstName?.Trim(), LastName?.Trim(), Email?.Trim(), IsMember, IsChallenger, ParseVma(Vma));
				_memberRepository.Add(member);

				StatusMessage = $"Member '{member.GetFullName()}' added.";
				LoadMembers();
				ClearForm();
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error adding member: {ex.Message}";
				MessageBox.Show(StatusMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private bool CanExecuteUpdateMember(object parameter)
		{
			return SelectedMember != null && !string.IsNullOrWhiteSpace(LastName);
		}

		private void ExecuteUpdateMember(object parameter)
		{
			try
			{
				var member = new Member(FirstName?.Trim(), LastName?.Trim(), Email?.Trim(), IsMember, IsChallenger, ParseVma(Vma));
				_memberRepository.Update(_originalFirstName, _originalLastName, member);

				StatusMessage = $"Member '{member.GetFullName()}' updated.";
				LoadMembers();
				ClearForm();
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error updating member: {ex.Message}";
				MessageBox.Show(StatusMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private bool CanExecuteDeleteMember(object parameter)
		{
			return SelectedMember != null;
		}

		private void ExecuteDeleteMember(object parameter)
		{
			if (SelectedMember == null)
				return;

			var result = MessageBox.Show(
				$"Are you sure you want to delete '{SelectedMember.GetFullName()}'?",
				"Confirm Deletion",
				MessageBoxButton.YesNo,
				MessageBoxImage.Question);

			if (result != MessageBoxResult.Yes)
				return;

			try
			{
				_memberRepository.Remove(SelectedMember);
				StatusMessage = $"Member '{SelectedMember.GetFullName()}' deleted.";
				LoadMembers();
				ClearForm();
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error deleting member: {ex.Message}";
				MessageBox.Show(StatusMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void ExportToExcel(bool onlyMembers, bool onlyChallengers, string label)
		{
			try
			{
				var data = _memberRepository.GetAll().AsEnumerable();
				if (onlyMembers)
					data = data.Where(m => m.IsMember);
				if (onlyChallengers)
					data = data.Where(m => m.IsChallenger);

				var list = data
					.OrderBy(m => m.LastName, StringComparer.OrdinalIgnoreCase)
					.ThenBy(m => m.FirstName, StringComparer.OrdinalIgnoreCase)
					.ToList();

				if (list.Count == 0)
				{
					StatusMessage = $"No {label.ToLowerInvariant()} to export.";
					MessageBox.Show(StatusMessage, "Export", MessageBoxButton.OK, MessageBoxImage.Information);
					return;
				}

				var saveFileDialog = new SaveFileDialog
				{
					Filter = "Excel Files (*.xlsx)|*.xlsx",
					FileName = $"{label}_{DateTime.Now:yyyyMMdd}.xlsx"
				};

				if (saveFileDialog.ShowDialog() != true)
					return;

				using (var package = new ExcelPackage())
				{
					var worksheet = package.Workbook.Worksheets.Add(label);

					worksheet.Cells[1, 1].Value = "First Name";
					worksheet.Cells[1, 2].Value = "Last Name";
					worksheet.Cells[1, 3].Value = "Email";
					worksheet.Cells[1, 4].Value = "Member";
					worksheet.Cells[1, 5].Value = "Challenger";
					worksheet.Cells[1, 6].Value = "VMA";

					using (var header = worksheet.Cells[1, 1, 1, 6])
					{
						header.Style.Font.Bold = true;
						header.Style.Fill.PatternType = ExcelFillStyle.Solid;
						header.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
					}

					var row = 2;
					foreach (var member in list)
					{
						worksheet.Cells[row, 1].Value = member.FirstName;
						worksheet.Cells[row, 2].Value = member.LastName;
						worksheet.Cells[row, 3].Value = member.Email;
						worksheet.Cells[row, 4].Value = member.IsMember ? "Yes" : "No";
						worksheet.Cells[row, 5].Value = member.IsChallenger ? "Yes" : "No";
						worksheet.Cells[row, 6].Value = member.Vma;
						row++;
					}

					worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

					package.SaveAs(new FileInfo(saveFileDialog.FileName));
				}

				StatusMessage = $"Exported {list.Count} {label.ToLowerInvariant()} to: {saveFileDialog.FileName}";
				MessageBox.Show(
					$"Successfully exported {list.Count} {label.ToLowerInvariant()}!\n\nFile saved to:\n{saveFileDialog.FileName}",
					"Export Complete",
					MessageBoxButton.OK,
					MessageBoxImage.Information);
			}
			catch (Exception ex)
			{
				StatusMessage = $"Error exporting {label.ToLowerInvariant()}: {ex.Message}";
				MessageBox.Show(StatusMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
	}
}
