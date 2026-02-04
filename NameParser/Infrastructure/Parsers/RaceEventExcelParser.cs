using System;
using System.Collections.Generic;
using System.IO;
using OfficeOpenXml;
using NameParser.Infrastructure.Data.Models;

namespace NameParser.Infrastructure.Parsers
{
    public class RaceEventExcelParser
    {
        public RaceEventExcelParser()
        {
            // Set EPPlus license context
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        /// <summary>
        /// Parses race events from Excel file (format based on Challenge Lucien 26.xlsx)
        /// Expected columns: Date | Race Name | Location | Website | Description
        /// </summary>
        public List<RaceEventEntity> Parse(string filePath)
        {
            var raceEvents = new List<RaceEventEntity>();

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets[0]; // First worksheet
                int rowCount = worksheet.Dimension?.Rows ?? 0;

                // Skip header row
                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        var dateValue = worksheet.Cells[row, 1].Value;
                        var name = worksheet.Cells[row, 2].Text?.Trim();
                        var location = worksheet.Cells[row, 3].Text?.Trim();
                        var website = worksheet.Cells[row, 4].Text?.Trim();
                        var description = worksheet.Cells[row, 5].Text?.Trim();

                        var eventDate = ParseDate(dateValue);

                        // Only add if we have at least a name and date
                        if (!string.IsNullOrWhiteSpace(name) && eventDate > DateTime.MinValue)
                        {
                            raceEvents.Add(new RaceEventEntity
                            {
                                EventDate = eventDate,
                                Name = name,
                                Location = location,
                                WebsiteUrl = website,
                                Description = description
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error parsing row {row}: {ex.Message}");
                    }
                }
            }

            return raceEvents;
        }

        /// <summary>
        /// Parses race events with their distances from Excel file
        /// Expected format: Date | Race Name | Distance (km) | Location | Website | Description
        /// Multiple rows with same date/name = multiple distances for same event
        /// </summary>
        public List<(RaceEventEntity raceEvent, List<decimal> distances)> ParseWithDistances(string filePath)
        {
            var groupedEvents = new Dictionary<string, (RaceEventEntity raceEvent, List<decimal> distances)>();

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets[0]; // First worksheet
                int rowCount = worksheet.Dimension?.Rows ?? 0;

                // Skip header row
                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        var dateValue = worksheet.Cells[row, 1].Value;
                        var name = worksheet.Cells[row, 2].Text?.Trim();
                        var distanceValue = worksheet.Cells[row, 3].Value;
                        var location = worksheet.Cells[row, 4].Text?.Trim();
                        var website = worksheet.Cells[row, 5].Text?.Trim();
                        var description = worksheet.Cells[row, 6].Text?.Trim();

                        var eventDate = ParseDate(dateValue);

                        if (string.IsNullOrWhiteSpace(name) || eventDate <= DateTime.MinValue)
                            continue;

                        var key = $"{eventDate:yyyyMMdd}_{name}";

                        if (!groupedEvents.ContainsKey(key))
                        {
                            groupedEvents[key] = (
                                new RaceEventEntity
                                {
                                    EventDate = eventDate,
                                    Name = name,
                                    Location = location,
                                    WebsiteUrl = website,
                                    Description = description
                                },
                                new List<decimal>()
                            );
                        }

                        // Parse and add distance (supports decimal values)
                        decimal distance = ParseDistance(distanceValue);
                        if (distance > 0 && !groupedEvents[key].distances.Contains(distance))
                        {
                            groupedEvents[key].distances.Add(distance);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error parsing row {row}: {ex.Message}");
                    }
                }
            }

            return new List<(RaceEventEntity, List<decimal>)>(groupedEvents.Values);
        }

        private decimal ParseDistance(object distanceValue)
        {
            if (distanceValue == null)
                return 0;

            // If it's already a number
            if (distanceValue is double dbl)
                return (decimal)dbl;

            if (distanceValue is int intVal)
                return intVal;

            if (distanceValue is decimal dec)
                return dec;

            // Try parsing as string
            if (decimal.TryParse(distanceValue.ToString().Replace(",", "."), 
                System.Globalization.NumberStyles.Any, 
                System.Globalization.CultureInfo.InvariantCulture, 
                out decimal result))
            {
                return result;
            }

            return 0;
        }

        private DateTime ParseDate(object dateValue)
        {
            if (dateValue == null)
                return DateTime.MinValue;

            // If it's already a DateTime
            if (dateValue is DateTime dt)
                return dt;

            // If it's a double (Excel date format)
            if (dateValue is double dbl)
            {
                try
                {
                    return DateTime.FromOADate(dbl);
                }
                catch
                {
                    return DateTime.MinValue;
                }
            }

            // Try parsing as string
            if (DateTime.TryParse(dateValue.ToString(), out DateTime date))
            {
                return date;
            }

            return DateTime.MinValue;
        }
    }
}
