using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using NameParser.Domain.Entities;
using NameParser.Infrastructure.Repositories;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace NameParser.UI.ViewModels
{
	public class MemberManagementViewModel : ViewModelBase
	{
		private readonly JsonMemberRepository _memberRepository;
		private readonly List<Member> _allMembers = new List<Member>();

		private Member _selectedMember;
		private string _firstName;
		private string _lastName;
		private string _email;
		private bool _isMember = true;
		private bool _isChallenger;
		private string _statusMessage;
		private string _searchText;
		private bool _showMembers = true;
		private bool _showChallengers = true;

		// Tracks the original identity of the selected member for updates
		private string _originalFirstName;
		private string _originalLastName;

		public MemberManagementViewModel()
		{
			_memberRepository = new JsonMemberRepository();

			ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

			Members = new ObservableCollection<Member>();

			AddMemberCommand = new RelayCommand(ExecuteAddMember, CanExecuteAddMember);
			UpdateMemberCommand = new RelayCommand(ExecuteUpdateMember, CanExecuteUpdateMember);
			DeleteMemberCommand = new RelayCommand(ExecuteDeleteMember, CanExecuteDeleteMember);
			ClearFormCommand = new RelayCommand(_ => ClearForm());
			RefreshCommand = new RelayCommand(_ => LoadMembers());
			ExportMembersCommand = new RelayCommand(_ => ExportToExcel(onlyMembers: true, onlyChallengers: false, "Members"));
			ExportChallengersCommand = new RelayCommand(_ => ExportToExcel(onlyMembers: false, onlyChallengers: true, "Challengers"));

			LoadMembers();
		}

		public ObservableCollection<Member> Members { get; }

		public ICommand AddMemberCommand { get; }
		public ICommand UpdateMemberCommand { get; }
		public ICommand DeleteMemberCommand { get; }
		public ICommand ClearFormCommand { get; }
		public ICommand RefreshCommand { get; }
		public ICommand ExportMembersCommand { get; }
		public ICommand ExportChallengersCommand { get; }

		public Member SelectedMember
		{
			get => _selectedMember;
			set
			{
				if (SetProperty(ref _selectedMember, value))
				{
					LoadMemberDetails();
					(UpdateMemberCommand as RelayCommand)?.RaiseCanExecuteChanged();
					(DeleteMemberCommand as RelayCommand)?.RaiseCanExecuteChanged();
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
			_originalFirstName = null;
			_originalLastName = null;
		}

		private bool CanExecuteAddMember(object parameter)
		{
			return !string.IsNullOrWhiteSpace(LastName);
		}

		private void ExecuteAddMember(object parameter)
		{
			try
			{
				var member = new Member(FirstName?.Trim(), LastName?.Trim(), Email?.Trim(), IsMember, IsChallenger);
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
				var member = new Member(FirstName?.Trim(), LastName?.Trim(), Email?.Trim(), IsMember, IsChallenger);
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

					using (var header = worksheet.Cells[1, 1, 1, 5])
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
