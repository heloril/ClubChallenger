using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using NameParser.Infrastructure.Data;
using NameParser.Infrastructure.Data.Models;
using NameParser.Infrastructure.Services;
using System.IO;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using ExcelColor = System.Drawing.Color;
using WordFontSize = DocumentFormat.OpenXml.Wordprocessing.FontSize;

namespace NameParser.UI.ViewModels
{
    public class ChallengeCalendarViewModel : ViewModelBase
    {
        private readonly ChallengeRepository _challengeRepository;
        private readonly RaceEventRepository _raceEventRepository;
        private readonly RaceRepository _raceRepository;
        
        private ChallengeEntity _selectedChallenge;
        private int _selectedYear;
        private string _statusMessage;
        private ObservableCollection<ChallengeCalendarItem> _calendarItems;

        public ChallengeCalendarViewModel()
        {
            _challengeRepository = new ChallengeRepository();
            _raceEventRepository = new RaceEventRepository();
            _raceRepository = new RaceRepository();

            // Set EPPlus license
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            SelectedYear = DateTime.Now.Year;

            Years = new ObservableCollection<int>();
            for (int i = 2020; i <= 2030; i++)
            {
                Years.Add(i);
            }

            Challenges = new ObservableCollection<ChallengeEntity>();
            CalendarItems = new ObservableCollection<ChallengeCalendarItem>();

            LoadChallengesCommand = new RelayCommand(ExecuteLoadChallenges);
            LoadCalendarCommand = new RelayCommand(ExecuteLoadCalendar, CanExecuteLoadCalendar);
            ExportToPdfCommand = new RelayCommand(ExecuteExportToPdf, CanExecuteExport);
            ExportToWordCommand = new RelayCommand(ExecuteExportToWord, CanExecuteExport);
            ExportToExcelCommand = new RelayCommand(ExecuteExportToExcel, CanExecuteExport);

            LoadChallenges();
        }

        public ObservableCollection<int> Years { get; }
        public ObservableCollection<ChallengeEntity> Challenges { get; }
        
        public ObservableCollection<ChallengeCalendarItem> CalendarItems
        {
            get => _calendarItems;
            set => SetProperty(ref _calendarItems, value);
        }

        public ChallengeEntity SelectedChallenge
        {
            get => _selectedChallenge;
            set
            {
                if (SetProperty(ref _selectedChallenge, value))
                {
                    ((RelayCommand)LoadCalendarCommand)?.RaiseCanExecuteChanged();
                    ((RelayCommand)ExportToPdfCommand)?.RaiseCanExecuteChanged();
                    ((RelayCommand)ExportToWordCommand)?.RaiseCanExecuteChanged();
                    ((RelayCommand)ExportToExcelCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public int SelectedYear
        {
            get => _selectedYear;
            set => SetProperty(ref _selectedYear, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ICommand LoadChallengesCommand { get; }
        public ICommand LoadCalendarCommand { get; }
        public ICommand ExportToPdfCommand { get; }
        public ICommand ExportToWordCommand { get; }
        public ICommand ExportToExcelCommand { get; }

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
                foreach (var challenge in challenges.OrderByDescending(c => c.Year))
                {
                    Challenges.Add(challenge);
                }

                StatusMessage = $"Loaded {challenges.Count} challenge(s)";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading challenges: {ex.Message}";
                MessageBox.Show($"Error loading challenges: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanExecuteLoadCalendar(object parameter)
        {
            return SelectedChallenge != null;
        }

        private void ExecuteLoadCalendar(object parameter)
        {
            if (SelectedChallenge == null) return;

            try
            {
                // Get race events for this challenge
                var raceEvents = _challengeRepository.GetRaceEventsForChallenge(SelectedChallenge.Id);
                
                CalendarItems.Clear();

                int raceNumber = 1;
                
                // Order by event date
                foreach (var raceEvent in raceEvents.OrderBy(re => re.EventDate))
                {
                    // Get all races for this event
                    var races = _raceRepository.GetRacesByRaceEvent(raceEvent.Id);
                    
                    // Get unique race number (should be the same for all distances)
                    var raceNum = races.FirstOrDefault()?.RaceNumber ?? 0;
                    
                    var item = new ChallengeCalendarItem
                    {
                        RaceNumber = raceNum > 0 ? raceNum : raceNumber,
                        EventDate = raceEvent.EventDate,
                        EventName = raceEvent.Name,
                        Location = raceEvent.Location,
                        Distances = string.Join(", ", races.OrderBy(r => r.DistanceKm).Select(r => $"{r.DistanceKm}km")),
                        Website = raceEvent.WebsiteUrl,
                        Status = races.Any(r => r.Status == "Processed") ? "Processed" : 
                                races.Any() ? "Uploaded" : "Pending",
                        RaceCount = races.Count,
                        ProcessedCount = races.Count(r => r.Status == "Processed")
                    };

                    CalendarItems.Add(item);
                    raceNumber++;
                }

                StatusMessage = $"Loaded calendar for '{SelectedChallenge.Name}' - {CalendarItems.Count} race event(s)";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading calendar: {ex.Message}";
                MessageBox.Show($"Error loading calendar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanExecuteExport(object parameter)
        {
            return SelectedChallenge != null && CalendarItems != null && CalendarItems.Count > 0;
        }

        private void ExecuteExportToPdf(object parameter)
        {
            MessageBox.Show(
                "PDF export functionality requires iTextSharp or similar library.\n\n" +
                "This feature will export the challenge calendar to a PDF document.\n\n" +
                "To implement:\n" +
                "1. Install iText7 NuGet package\n" +
                "2. Generate PDF with calendar table\n" +
                "3. Include race dates, numbers, and status",
                "PDF Export",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ExecuteExportToWord(object parameter)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Word Documents (*.docx)|*.docx",
                    DefaultExt = "docx",
                    FileName = $"{SelectedChallenge.Name.Replace(" ", "_")}_Calendar_{DateTime.Now:yyyyMMdd}.docx"
                };

                if (saveFileDialog.ShowDialog() != true) return;

                using (var document = WordprocessingDocument.Create(saveFileDialog.FileName, WordprocessingDocumentType.Document))
                {
                    var mainPart = document.AddMainDocumentPart();
                    mainPart.Document = new Document();
                    var body = mainPart.Document.AppendChild(new Body());

                    // Title
                    var titlePara = body.AppendChild(new Paragraph());
                    var titleRun = titlePara.AppendChild(new Run());
                    titleRun.AppendChild(new Text($"{SelectedChallenge.Name} - Challenge Calendar"));
                    var titleProps = titleRun.AppendChild(new RunProperties());
                    titleProps.AppendChild(new Bold());
                    titleProps.AppendChild(new WordFontSize { Val = "32" });

                    // Subtitle
                    var subtitlePara = body.AppendChild(new Paragraph());
                    var subtitleRun = subtitlePara.AppendChild(new Run());
                    subtitleRun.AppendChild(new Text($"Year: {SelectedChallenge.Year}"));
                    var subtitleProps = subtitleRun.AppendChild(new RunProperties());
                    subtitleProps.AppendChild(new WordFontSize { Val = "24" });

                    // Empty line
                    body.AppendChild(new Paragraph());

                    // Create table
                    var table = new Table();
                    
                    // Table properties
                    var tableProps = new TableProperties(
                        new TableBorders(
                            new TopBorder { Val = BorderValues.Single, Size = 4 },
                            new BottomBorder { Val = BorderValues.Single, Size = 4 },
                            new LeftBorder { Val = BorderValues.Single, Size = 4 },
                            new RightBorder { Val = BorderValues.Single, Size = 4 },
                            new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                            new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 }
                        )
                    );
                    table.AppendChild(tableProps);

                    // Header row
                    var headerRow = new TableRow();
                    AddTableCell(headerRow, "Race #", true);
                    AddTableCell(headerRow, "Date", true);
                    AddTableCell(headerRow, "Event Name", true);
                    AddTableCell(headerRow, "Location", true);
                    AddTableCell(headerRow, "Distances", true);
                    AddTableCell(headerRow, "Status", true);
                    table.AppendChild(headerRow);

                    // Data rows
                    foreach (var item in CalendarItems)
                    {
                        var dataRow = new TableRow();
                        AddTableCell(dataRow, item.RaceNumber.ToString());
                        AddTableCell(dataRow, item.EventDate.ToString("dd/MM/yyyy"));
                        AddTableCell(dataRow, item.EventName);
                        AddTableCell(dataRow, item.Location ?? "-");
                        AddTableCell(dataRow, item.Distances);
                        AddTableCell(dataRow, item.Status);
                        table.AppendChild(dataRow);
                    }

                    body.AppendChild(table);

                    // Summary
                    body.AppendChild(new Paragraph());
                    var summaryPara = body.AppendChild(new Paragraph());
                    var summaryRun = summaryPara.AppendChild(new Run());
                    summaryRun.AppendChild(new Text($"Total Race Events: {CalendarItems.Count}"));

                    mainPart.Document.Save();
                }

                StatusMessage = $"Calendar exported to Word: {saveFileDialog.FileName}";
                MessageBox.Show(
                    $"Calendar exported successfully!\n\nFile: {saveFileDialog.FileName}",
                    "Export Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error exporting to Word: {ex.Message}";
                MessageBox.Show($"Error exporting to Word: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddTableCell(TableRow row, string text, bool isHeader = false)
        {
            var cell = new TableCell();
            var para = new Paragraph();
            var run = new Run();
            run.AppendChild(new Text(text));
            
            if (isHeader)
            {
                var runProps = new RunProperties();
                runProps.AppendChild(new Bold());
                run.PrependChild(runProps);
            }
            
            para.AppendChild(run);
            cell.AppendChild(para);
            row.AppendChild(cell);
        }

        private void ExecuteExportToExcel(object parameter)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx",
                    DefaultExt = "xlsx",
                    FileName = $"{SelectedChallenge.Name.Replace(" ", "_")}_Calendar_{DateTime.Now:yyyyMMdd}.xlsx"
                };

                if (saveFileDialog.ShowDialog() != true) return;

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Challenge Calendar");

                    // Title
                    worksheet.Cells[1, 1].Value = $"{SelectedChallenge.Name} - Challenge Calendar";
                    worksheet.Cells[1, 1, 1, 6].Merge = true;
                    worksheet.Cells[1, 1].Style.Font.Size = 16;
                    worksheet.Cells[1, 1].Style.Font.Bold = true;
                    worksheet.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    // Subtitle
                    worksheet.Cells[2, 1].Value = $"Year: {SelectedChallenge.Year}";
                    worksheet.Cells[2, 1, 2, 6].Merge = true;
                    worksheet.Cells[2, 1].Style.Font.Size = 12;
                    worksheet.Cells[2, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    // Headers
                    int headerRow = 4;
                    worksheet.Cells[headerRow, 1].Value = "Race #";
                    worksheet.Cells[headerRow, 2].Value = "Date";
                    worksheet.Cells[headerRow, 3].Value = "Event Name";
                    worksheet.Cells[headerRow, 4].Value = "Location";
                    worksheet.Cells[headerRow, 5].Value = "Distances";
                    worksheet.Cells[headerRow, 6].Value = "Status";

                    // Style headers
                    using (var range = worksheet.Cells[headerRow, 1, headerRow, 6])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(ExcelColor.LightBlue);
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Medium);
                    }

                    // Data
                    int row = headerRow + 1;
                    foreach (var item in CalendarItems)
                    {
                        worksheet.Cells[row, 1].Value = item.RaceNumber;
                        worksheet.Cells[row, 2].Value = item.EventDate.ToString("dd/MM/yyyy");
                        worksheet.Cells[row, 3].Value = item.EventName;
                        worksheet.Cells[row, 4].Value = item.Location ?? "-";
                        worksheet.Cells[row, 5].Value = item.Distances;
                        worksheet.Cells[row, 6].Value = item.Status;
                        
                        // Color code status
                        var statusCell = worksheet.Cells[row, 6];
                        statusCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        switch (item.Status)
                        {
                            case "Processed":
                                statusCell.Style.Fill.BackgroundColor.SetColor(ExcelColor.LightGreen);
                                break;
                            case "Uploaded":
                                statusCell.Style.Fill.BackgroundColor.SetColor(ExcelColor.LightYellow);
                                break;
                            case "Pending":
                                statusCell.Style.Fill.BackgroundColor.SetColor(ExcelColor.LightCoral);
                                break;
                        }

                        row++;
                    }

                    // Borders for data
                    using (var range = worksheet.Cells[headerRow, 1, row - 1, 6])
                    {
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    }

                    // Summary
                    row += 2;
                    worksheet.Cells[row, 1].Value = "Summary:";
                    worksheet.Cells[row, 1].Style.Font.Bold = true;
                    row++;
                    worksheet.Cells[row, 1].Value = $"Total Race Events: {CalendarItems.Count}";
                    row++;
                    worksheet.Cells[row, 1].Value = $"Processed: {CalendarItems.Count(i => i.Status == "Processed")}";
                    row++;
                    worksheet.Cells[row, 1].Value = $"Pending: {CalendarItems.Count(i => i.Status == "Pending")}";

                    // Auto-fit columns
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    // Set minimum widths
                    worksheet.Column(1).Width = Math.Max(worksheet.Column(1).Width, 10);
                    worksheet.Column(2).Width = Math.Max(worksheet.Column(2).Width, 12);
                    worksheet.Column(3).Width = Math.Max(worksheet.Column(3).Width, 30);
                    worksheet.Column(4).Width = Math.Max(worksheet.Column(4).Width, 20);
                    worksheet.Column(5).Width = Math.Max(worksheet.Column(5).Width, 15);
                    worksheet.Column(6).Width = Math.Max(worksheet.Column(6).Width, 12);

                    package.SaveAs(new FileInfo(saveFileDialog.FileName));
                }

                StatusMessage = $"Calendar exported to Excel: {saveFileDialog.FileName}";
                MessageBox.Show(
                    $"Calendar exported successfully!\n\nFile: {saveFileDialog.FileName}",
                    "Export Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error exporting to Excel: {ex.Message}";
                MessageBox.Show($"Error exporting to Excel: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class ChallengeCalendarItem : ViewModelBase
    {
        private int _raceNumber;
        private DateTime _eventDate;
        private string _eventName;
        private string _location;
        private string _distances;
        private string _website;
        private string _status;
        private int _raceCount;
        private int _processedCount;

        public int RaceNumber
        {
            get => _raceNumber;
            set => SetProperty(ref _raceNumber, value);
        }

        public DateTime EventDate
        {
            get => _eventDate;
            set => SetProperty(ref _eventDate, value);
        }

        public string EventName
        {
            get => _eventName;
            set => SetProperty(ref _eventName, value);
        }

        public string Location
        {
            get => _location;
            set => SetProperty(ref _location, value);
        }

        public string Distances
        {
            get => _distances;
            set => SetProperty(ref _distances, value);
        }

        public string Website
        {
            get => _website;
            set => SetProperty(ref _website, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public int RaceCount
        {
            get => _raceCount;
            set => SetProperty(ref _raceCount, value);
        }

        public int ProcessedCount
        {
            get => _processedCount;
            set => SetProperty(ref _processedCount, value);
        }
    }
}
