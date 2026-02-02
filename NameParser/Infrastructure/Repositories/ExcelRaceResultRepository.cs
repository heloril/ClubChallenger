using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using OfficeOpenXml;
using NameParser.Domain.Entities;
using NameParser.Domain.Repositories;

namespace NameParser.Infrastructure.Repositories
{
    public class ExcelRaceResultRepository : IRaceResultRepository
    {
        private const string Separator = ";";
        private const int MaxColumn = 12;
        private const int MaxRow = 6000;
        private const int RaceTimeThresholdMinutes = 15;

        public ExcelRaceResultRepository()
        {
            // Set EPPlus license context (required for EPPlus 5.0+)
            // Use NonCommercial for personal/internal use, or Commercial if you have a license
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public Dictionary<int, string> GetRaceResults(string filePath, List<Member> members)
        {
            var results = new Dictionary<int, string>();

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                foreach (var worksheet in package.Workbook.Worksheets)
                {
                    var worksheetResults = GetWorksheetResults(members, worksheet);
                    foreach (var result in worksheetResults)
                    {
                        results[result.Key] = result.Value;
                    }
                }
            }

            return results;
        }

        private Dictionary<int, string> GetWorksheetResults(List<Member> members, ExcelWorksheet ws)
        {
            var finalResults = new Dictionary<int, string>();
            AddHeader(ws, finalResults);

            // Get reference time to determine race type
            var referenceTime = GetReferenceTime(ws);
            AddReference(ws, finalResults, referenceTime);

            // Find column indices from header
            int positionColumnIndex = FindPositionColumnIndex(ws);
            int teamColumnIndex = FindColumnIndex(ws, new[] { "equipe", "équipe", "team", "club" });
            int speedColumnIndex = FindColumnIndex(ws, new[] { "vitesse", "vit", "vit.", "speed", "km/h" });
            int raceTimeColumnIndex = FindColumnIndex(ws, new[] { "temps", "time", "chrono" });
            int timePerKmColumnIndex = FindColumnIndex(ws, new[] { "t/km", "temps/km", "temps km", "time/km", "pace" });

            bool isTimePerKmRace = referenceTime.HasValue && 
                                   referenceTime.Value.TotalMinutes < RaceTimeThresholdMinutes;

            // Track if we've found position 1 (winner)
            bool winnerFound = false;
            string winnerData = null;
            int winnerId = -1;

            // First, collect all member results
            var memberResults = new List<(int id, string data, int? position)>();

            foreach (Member member in members)
            {
                var results = SearchAndCollectMemberResults(ws, member, referenceTime, positionColumnIndex, teamColumnIndex, speedColumnIndex, raceTimeColumnIndex, timePerKmColumnIndex, isTimePerKmRace);
                foreach (var result in results)
                {
                    memberResults.Add(result);

                    // Check if this is the winner (position 1)
                    if (result.position.HasValue && result.position.Value == 1)
                    {
                        winnerFound = true;
                    }
                }
            }

            // If winner not found in members, search for position 1 in all rows
            if (!winnerFound && positionColumnIndex > 0)
            {
                var winnerResult = FindWinnerRow(ws, positionColumnIndex, teamColumnIndex, speedColumnIndex, raceTimeColumnIndex, timePerKmColumnIndex, isTimePerKmRace);
                if (winnerResult.HasValue)
                {
                    winnerData = winnerResult.Value.data;
                    winnerId = winnerResult.Value.id;
                    memberResults.Insert(0, (winnerId, winnerData, 1));
                }
            }

            // Add all results to final dictionary
            foreach (var result in memberResults.OrderBy(r => r.position ?? int.MaxValue))
            {
                if (!finalResults.ContainsKey(result.id))
                {
                    finalResults.Add(result.id, result.data);
                }
            }

            return finalResults;
        }

        private int FindColumnIndex(ExcelWorksheet ws, string[] columnNames)
        {
            if (ws.Dimension == null)
                return -1;

            // Check header row (row 1) for matching column names
            for (int col = 1; col <= Math.Min(MaxColumn, ws.Dimension.Columns); col++)
            {
                var headerText = ws.Cells[1, col].Text?.Trim().ToLowerInvariant();
                if (!string.IsNullOrEmpty(headerText))
                {
                    foreach (var colName in columnNames)
                    {
                        if (headerText.Equals(colName, StringComparison.OrdinalIgnoreCase) || 
                            headerText.Contains(colName))
                        {
                            return col;
                        }
                    }
                }
            }

            return -1; // Column not found
        }

        private int FindPositionColumnIndex(ExcelWorksheet ws)
        {
            if (ws.Dimension == null)
                return -1;

            // Check header row (row 1) for position-related column names
            string[] positionHeaders = { "place", "pl", "pl.", "position", "pos", "pos.", "rang", "classement", "class", "rank" };

            for (int col = 1; col <= Math.Min(MaxColumn, ws.Dimension.Columns); col++)
            {
                var headerText = ws.Cells[1, col].Text?.Trim().ToLowerInvariant();
                if (!string.IsNullOrEmpty(headerText))
                {
                    foreach (var posHeader in positionHeaders)
                    {
                        if (headerText.Equals(posHeader, StringComparison.OrdinalIgnoreCase) || 
                            headerText.Contains(posHeader))
                        {
                            return col;
                        }
                    }
                }
            }

            return -1; // Position column not found
        }

        private (int id, string data, int? position)? FindWinnerRow(ExcelWorksheet ws, int positionColumnIndex, int teamColumnIndex, int speedColumnIndex, int raceTimeColumnIndex, int timePerKmColumnIndex, bool isTimePerKmRace)
        {
            if (ws.Dimension == null || positionColumnIndex <= 0)
                return null;

            int maxRow = Math.Min(ws.Dimension.Rows, MaxRow);

            for (int row = 2; row <= maxRow; row++) // Start from row 2 (skip header)
            {
                var positionText = ws.Cells[row, positionColumnIndex].Text?.Trim();

                // Check if this is position 1
                if (positionText == "1" || positionText == "1.")
                {
                    // Create dummy member for winner
                    var winnerMember = new Member 
                    { 
                        FirstName = ws.Cells[row, positionColumnIndex + 2].Text ?? "Winner", // Approximate
                        LastName = ws.Cells[row, positionColumnIndex + 1].Text ?? "Winner"
                    };

                    return ProcessAndCollectFoundRow(ws, row, winnerMember, isTimePerKmRace, positionColumnIndex, teamColumnIndex, speedColumnIndex, raceTimeColumnIndex, timePerKmColumnIndex, false);
                }
            }

            return null;
        }

        private TimeSpan? GetReferenceTime(ExcelWorksheet ws)
        {
            if (ws.Dimension == null)
                return null;

            int maxRow = Math.Min(ws.Dimension.Rows, MaxRow);

            for (int row = 1; row <= maxRow; row++)
            {
                for (int col = 1; col <= MaxColumn; col++)
                {
                    var cellText = ws.Cells[row, col].Text;

                    if (!string.IsNullOrEmpty(cellText) &&
                        cellText.Contains("TREF", StringComparison.OrdinalIgnoreCase))
                    {
                        // Look for time in the same row
                        for (int c = 1; c <= MaxColumn; c++)
                        {
                            var timeText = ws.Cells[row, c].Text;
                            var time = ParseTime(timeText);
                            if (time.HasValue && time.Value.TotalSeconds > 1)
                            {
                                return time.Value;
                            }
                        }
                    }
                }
            }

            return null;
        }

        private List<(int id, string data, int? position)> SearchAndCollectMemberResults(
            ExcelWorksheet ws, Member member, TimeSpan? referenceTime, int positionColumnIndex, int teamColumnIndex, int speedColumnIndex, int raceTimeColumnIndex, int timePerKmColumnIndex, bool isTimePerKmRace)
        {
            var results = new List<(int, string, int?)>();
            var lastNameNormalized = member.LastName.RemoveDiacritics();

            results.AddRange(FindAndCollectResults(ws, member, lastNameNormalized, referenceTime, positionColumnIndex, teamColumnIndex, speedColumnIndex, raceTimeColumnIndex, timePerKmColumnIndex, isTimePerKmRace));

            if (lastNameNormalized != member.LastName)
            {
                results.AddRange(FindAndCollectResults(ws, member, member.LastName, referenceTime, positionColumnIndex, teamColumnIndex, speedColumnIndex, raceTimeColumnIndex, timePerKmColumnIndex, isTimePerKmRace));
            }

            return results;
        }

        private List<(int id, string data, int? position)> FindAndCollectResults(
            ExcelWorksheet ws, Member member, string searchTerm, TimeSpan? referenceTime, int positionColumnIndex, int teamColumnIndex, int speedColumnIndex, int raceTimeColumnIndex, int timePerKmColumnIndex, bool isTimePerKmRace)
        {
            var results = new List<(int, string, int?)>();

            if (ws.Dimension == null)
                return results;

            int maxRow = Math.Min(ws.Dimension.Rows, MaxRow);

            for (int row = 1; row <= maxRow; row++)
            {
                for (int col = 1; col <= MaxColumn; col++)
                {
                    var cell = ws.Cells[row, col];
                    var cellValue = cell.Text;

                    if (!string.IsNullOrEmpty(cellValue) &&
                        cellValue.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        var result = ProcessAndCollectFoundRow(ws, row, member, isTimePerKmRace, positionColumnIndex, teamColumnIndex, speedColumnIndex, raceTimeColumnIndex, timePerKmColumnIndex, true);
                        if (result.HasValue)
                        {
                            results.Add(result.Value);
                        }
                        break;
                    }
                }
            }

            return results;
        }

        private (int id, string data, int? position)? ProcessAndCollectFoundRow(
            ExcelWorksheet ws, int row, Member member, bool isTimePerKmRace, int positionColumnIndex, int teamColumnIndex, int speedColumnIndex, int raceTimeColumnIndex, int timePerKmColumnIndex, bool isMember)
        {
            StringBuilder rowData = new StringBuilder();
            int id = 0;
            TimeSpan? memberTime = null;
            int? position = null;
            string team = null;
            double? speed = null;

            rowData.Append(isMember ? "TMEM;" : "TWINNER;");

            // Extract position from the position column if found
            if (positionColumnIndex > 0)
            {
                var positionText = ws.Cells[row, positionColumnIndex].Text?.Trim();
                if (!string.IsNullOrEmpty(positionText))
                {
                    positionText = positionText.TrimEnd('.');
                    if (int.TryParse(positionText, out int pos))
                    {
                        position = pos;
                    }
                }
            }

            // Extract team
            if (teamColumnIndex > 0)
            {
                team = ws.Cells[row, teamColumnIndex].Text?.Trim();
            }

            // Extract speed
            if (speedColumnIndex > 0)
            {
                var speedText = ws.Cells[row, speedColumnIndex].Text?.Trim();
                if (!string.IsNullOrEmpty(speedText))
                {
                    // Try to parse speed (may have "km/h" suffix)
                    speedText = speedText.Replace("km/h", "").Replace(",", ".").Trim();
                    if (double.TryParse(speedText, NumberStyles.Any, CultureInfo.InvariantCulture, out double spd))
                    {
                        speed = spd;
                    }
                }
            }

            // Extract race time from specific column if found
            TimeSpan? raceTime = null;
            TimeSpan? timePerKmFromColumn = null;

            if (raceTimeColumnIndex > 0)
            {
                var timeText = ws.Cells[row, raceTimeColumnIndex].Text;
                var parsedTime = ParseTime(timeText);
                if (parsedTime.HasValue)
                {
                    raceTime = parsedTime.Value;
                    if (!memberTime.HasValue)
                    {
                        memberTime = parsedTime.Value;
                    }
                }
            }

            // Extract time per km from specific column if found
            if (timePerKmColumnIndex > 0)
            {
                var timeText = ws.Cells[row, timePerKmColumnIndex].Text;
                var parsedTime = ParseTime(timeText);
                if (parsedTime.HasValue)
                {
                    timePerKmFromColumn = parsedTime.Value;
                    // For time/km races, use this as the main time
                    if (isTimePerKmRace && !memberTime.HasValue)
                    {
                        memberTime = parsedTime.Value;
                    }
                }
            }

            for (int col = 1; col <= MaxColumn; col++)
            {
                var cell = ws.Cells[row, col];
                var cellText = cell.Text;

                if (col == 1 && int.TryParse(cellText.Replace(".", ""), out int parsedId))
                {
                    id = parsedId;
                }

                // Try to parse time from cell if not already found
                if (!memberTime.HasValue)
                {
                    var parsedTime = ParseTime(cellText);
                    if (parsedTime.HasValue)
                    {
                        memberTime = parsedTime.Value;
                    }
                }

                rowData.Append(cellText + ";");
            }

            // Add race type indicator
            rowData.Append($"RACETYPE;{(isTimePerKmRace ? "TIME_PER_KM" : "RACE_TIME")};");

            // Add race time if found from specific column
            if (raceTime.HasValue)
            {
                rowData.Append($"RACETIME;{raceTime.Value:hh\\:mm\\:ss};");
            }

            // Add time per km if found from specific column
            if (timePerKmFromColumn.HasValue)
            {
                rowData.Append($"TIMEPERKM;{timePerKmFromColumn.Value:mm\\:ss};");
            }

            // Add position if found
            if (position.HasValue)
            {
                rowData.Append($"POS;{position};");
            }

            // Add team if found
            if (!string.IsNullOrEmpty(team))
            {
                rowData.Append($"TEAM;{team};");
            }

            // Add speed if found
            if (speed.HasValue)
            {
                rowData.Append($"SPEED;{speed.Value:F2};");
            }

            // Add member flag
            rowData.Append($"ISMEMBER;{(isMember ? "1" : "0")};");

            string rowDataString = rowData.ToString().RemoveDiacritics();
            if (isMember)
            {
                if (rowDataString.Contains(member.FirstName.RemoveDiacritics(), StringComparison.InvariantCultureIgnoreCase))
                {
                    return (id, rowData.ToString(), position);
                }
                return null;
            }
            else
            {
                return (id, rowData.ToString(), position);
            }
        }

        private TimeSpan? ParseTime(string timeText)
        {
            if (string.IsNullOrWhiteSpace(timeText))
                return null;

            // Try format: h:mm:ss
            if (TimeSpan.TryParseExact(timeText, @"h\:mm\:ss", CultureInfo.InvariantCulture, out var time1))
                return time1;

            // Try format: hh:mm:ss
            if (TimeSpan.TryParseExact(timeText, @"hh\:mm\:ss", CultureInfo.InvariantCulture, out var time2))
                return time2;

            // Try format: mm:ss
            if (TimeSpan.TryParseExact(timeText, @"mm\:ss", CultureInfo.InvariantCulture, out var time3))
                return time3;

            // Try format: m:ss
            if (TimeSpan.TryParseExact(timeText, @"m\:ss", CultureInfo.InvariantCulture, out var time4))
                return time4;

            // Try standard TimeSpan.Parse
            if (TimeSpan.TryParse(timeText, CultureInfo.InvariantCulture, out var time5))
                return time5;

            return null;
        }

        private static void AddHeader(ExcelWorksheet ws, Dictionary<int, string> finalResults)
        {
            StringBuilder headerData = new StringBuilder();
            headerData.Append("Header;");

            for (int col = 1; col <= MaxColumn; col++)
            {
                var cell = ws.Cells[1, col];
                headerData.Append(cell.Text + ";");
            }

            finalResults.Add(0, headerData.ToString());
        }

        private static void AddReference(ExcelWorksheet ws, Dictionary<int, string> finalResults, TimeSpan? referenceTime)
        {
            if (ws.Dimension == null)
                return;

            int maxRow = Math.Min(ws.Dimension.Rows, MaxRow);
            bool isTimePerKmRace = referenceTime.HasValue && 
                                   referenceTime.Value.TotalMinutes < RaceTimeThresholdMinutes;

            for (int row = 1; row <= maxRow; row++)
            {
                for (int col = 1; col <= MaxColumn; col++)
                {
                    var cell = ws.Cells[row, col];
                    var cellText = cell.Text;

                    if (!string.IsNullOrEmpty(cellText) &&
                        cellText.Contains("TREF", StringComparison.OrdinalIgnoreCase))
                    {
                        StringBuilder rowData = new StringBuilder();
                        rowData.Append("TREF;");

                        for (int c = 1; c <= MaxColumn; c++)
                        {
                            rowData.Append(ws.Cells[row, c].Text + ";");
                        }

                        // Add race type indicator
                        rowData.Append($"RACETYPE;{(isTimePerKmRace ? "TIME_PER_KM" : "RACE_TIME")};" );

                        finalResults.Add(1, rowData.ToString());
                        return;
                    }
                }
            }
        }
    }
}
