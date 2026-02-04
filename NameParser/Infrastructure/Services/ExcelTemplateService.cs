using System;
using System.IO;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace NameParser.Infrastructure.Services
{
    public class ExcelTemplateService
    {
        public ExcelTemplateService()
        {
            // Set EPPlus license context
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public void GenerateRaceEventTemplate(string filePath)
        {
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Race Events");

                // Set up header row with styling
                worksheet.Row(1).Height = 25;

                // Add headers
                worksheet.Cells[1, 1].Value = "Date";
                worksheet.Cells[1, 2].Value = "Race Name";
                worksheet.Cells[1, 3].Value = "Distance (km)";
                worksheet.Cells[1, 4].Value = "Location";
                worksheet.Cells[1, 5].Value = "Website";
                worksheet.Cells[1, 6].Value = "Description";

                // Style header row
                using (var range = worksheet.Cells[1, 1, 1, 6])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Font.Size = 12;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                }

                // Add instructions in row 2 with light gray background
                worksheet.Cells[2, 1].Value = "dd/MM/yyyy";
                worksheet.Cells[2, 2].Value = "Required";
                worksheet.Cells[2, 3].Value = "Decimal (e.g., 10, 21.1, 42.195)";
                worksheet.Cells[2, 4].Value = "Optional";
                worksheet.Cells[2, 5].Value = "Optional (full URL)";
                worksheet.Cells[2, 6].Value = "Optional";

                using (var range = worksheet.Cells[2, 1, 2, 6])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    range.Style.Font.Italic = true;
                    range.Style.Font.Size = 10;
                }

                // Add example rows
                worksheet.Cells[3, 1].Value = DateTime.Now.ToString("dd/MM/yyyy");
                worksheet.Cells[3, 2].Value = "Marathon de Paris";
                worksheet.Cells[3, 3].Value = 10;
                worksheet.Cells[3, 4].Value = "Paris, France";
                worksheet.Cells[3, 5].Value = "https://www.harmonyparismarathon.com";
                worksheet.Cells[3, 6].Value = "Famous marathon through Paris streets";

                worksheet.Cells[4, 1].Value = DateTime.Now.ToString("dd/MM/yyyy");
                worksheet.Cells[4, 2].Value = "Marathon de Paris";
                worksheet.Cells[4, 3].Value = 21.1;
                worksheet.Cells[4, 4].Value = "Paris, France";
                worksheet.Cells[4, 5].Value = "https://www.harmonyparismarathon.com";
                worksheet.Cells[4, 6].Value = "Half marathon distance";

                worksheet.Cells[5, 1].Value = DateTime.Now.ToString("dd/MM/yyyy");
                worksheet.Cells[5, 2].Value = "Marathon de Paris";
                worksheet.Cells[5, 3].Value = 42.195;
                worksheet.Cells[5, 4].Value = "Paris, France";
                worksheet.Cells[5, 5].Value = "https://www.harmonyparismarathon.com";
                worksheet.Cells[5, 6].Value = "Full marathon distance";

                // Add note section below the examples
                worksheet.Cells[7, 1].Value = "NOTES:";
                worksheet.Cells[7, 1].Style.Font.Bold = true;
                worksheet.Cells[7, 1].Style.Font.Size = 11;

                worksheet.Cells[8, 1].Value = "• Multiple rows with the same Race Name and Date will create one event with multiple distances";
                worksheet.Cells[9, 1].Value = "• Date format must be dd/MM/yyyy (e.g., 15/04/2024)";
                worksheet.Cells[10, 1].Value = "• Distance can be decimal (e.g., 10, 21.1, 42.195)";
                worksheet.Cells[11, 1].Value = "• Race Name and Distance are required fields";
                worksheet.Cells[12, 1].Value = "• Delete the example rows (3-5) before importing your data";

                // Style the notes
                for (int i = 8; i <= 12; i++)
                {
                    worksheet.Cells[i, 1].Style.Font.Size = 10;
                    worksheet.Cells[i, 1].Style.WrapText = true;
                    worksheet.Cells[i, 1, i, 6].Merge = true;
                }

                // Set column widths
                worksheet.Column(1).Width = 15;  // Date
                worksheet.Column(2).Width = 30;  // Race Name
                worksheet.Column(3).Width = 18;  // Distance
                worksheet.Column(4).Width = 25;  // Location
                worksheet.Column(5).Width = 40;  // Website
                worksheet.Column(6).Width = 40;  // Description

                // Add borders to data area
                using (var range = worksheet.Cells[1, 1, 5, 6])
                {
                    range.Style.Border.Top.Style = ExcelBorderStyle.Medium;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Medium;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Medium;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Medium);
                }

                // Save the package
                package.SaveAs(new FileInfo(filePath));
            }
        }

        public void GenerateRaceResultsTemplate(string filePath, decimal distanceKm)
        {
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add($"Results {distanceKm}km");

                // Set up header row with styling
                worksheet.Row(1).Height = 25;

                // Add headers (matching the expected format)
                worksheet.Cells[1, 1].Value = "Position";
                worksheet.Cells[1, 2].Value = "Name";
                worksheet.Cells[1, 3].Value = "Time";
                worksheet.Cells[1, 4].Value = "Team";
                worksheet.Cells[1, 5].Value = "Category";
                worksheet.Cells[1, 6].Value = "Sex";

                // Style header row
                using (var range = worksheet.Cells[1, 1, 1, 6])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Font.Size = 12;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGreen);
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                }

                // Add instructions
                worksheet.Cells[2, 1].Value = "1, 2, 3...";
                worksheet.Cells[2, 2].Value = "Full Name";
                worksheet.Cells[2, 3].Value = "HH:MM:SS or MM:SS";
                worksheet.Cells[2, 4].Value = "Optional";
                worksheet.Cells[2, 5].Value = "Optional (e.g., SEN-M)";
                worksheet.Cells[2, 6].Value = "M/F";

                using (var range = worksheet.Cells[2, 1, 2, 6])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    range.Style.Font.Italic = true;
                    range.Style.Font.Size = 10;
                }

                // Add example rows
                worksheet.Cells[3, 1].Value = 1;
                worksheet.Cells[3, 2].Value = "John Doe";
                worksheet.Cells[3, 3].Value = "00:45:30";
                worksheet.Cells[3, 4].Value = "Running Club";
                worksheet.Cells[3, 5].Value = "SEN-M";
                worksheet.Cells[3, 6].Value = "M";

                worksheet.Cells[4, 1].Value = 2;
                worksheet.Cells[4, 2].Value = "Jane Smith";
                worksheet.Cells[4, 3].Value = "00:47:15";
                worksheet.Cells[4, 4].Value = "Speed Runners";
                worksheet.Cells[4, 5].Value = "SEN-F";
                worksheet.Cells[4, 6].Value = "F";

                worksheet.Cells[5, 1].Value = 3;
                worksheet.Cells[5, 2].Value = "Bob Johnson";
                worksheet.Cells[5, 3].Value = "00:48:20";
                worksheet.Cells[5, 4].Value = "Marathon Team";
                worksheet.Cells[5, 5].Value = "V1-M";
                worksheet.Cells[5, 6].Value = "M";

                // Add notes
                worksheet.Cells[7, 1].Value = "NOTES:";
                worksheet.Cells[7, 1].Style.Font.Bold = true;
                worksheet.Cells[7, 1].Style.Font.Size = 11;

                worksheet.Cells[8, 1].Value = $"• This template is for {distanceKm}km race results";
                worksheet.Cells[9, 1].Value = "• Position and Name are required fields";
                worksheet.Cells[10, 1].Value = "• Time format: HH:MM:SS (e.g., 01:23:45) or MM:SS (e.g., 45:30)";
                worksheet.Cells[11, 1].Value = "• Delete the example rows (3-5) before entering your data";
                worksheet.Cells[12, 1].Value = "• The system will automatically match names with club members";

                // Style notes
                for (int i = 8; i <= 12; i++)
                {
                    worksheet.Cells[i, 1].Style.Font.Size = 10;
                    worksheet.Cells[i, 1].Style.WrapText = true;
                    worksheet.Cells[i, 1, i, 6].Merge = true;
                }

                // Set column widths
                worksheet.Column(1).Width = 12;  // Position
                worksheet.Column(2).Width = 30;  // Name
                worksheet.Column(3).Width = 15;  // Time
                worksheet.Column(4).Width = 25;  // Team
                worksheet.Column(5).Width = 15;  // Category
                worksheet.Column(6).Width = 10;  // Sex

                // Add borders
                using (var range = worksheet.Cells[1, 1, 5, 6])
                {
                    range.Style.Border.Top.Style = ExcelBorderStyle.Medium;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Medium;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Medium;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Medium);
                }

                package.SaveAs(new FileInfo(filePath));
            }
        }
    }
}
