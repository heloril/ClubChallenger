using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using NameParser.Domain.Entities;
using NameParser.Domain.Repositories;

namespace NameParser.Infrastructure.Repositories
{
    /// <summary>
    /// Repository for extracting race results from PDF files.
    /// Parses PDF race classification documents and extracts participant data including
    /// position, names, times, teams, speeds, and other race metrics.
    /// Supports multiple formats with automatic detection and parsing.
    /// </summary>
    public class PdfRaceResultRepository : IRaceResultRepository
    {
        private const string Separator = ";";
        private const int RaceTimeThresholdMinutes = 15;

        private RaceMetadata _raceMetadata;
        private readonly List<IPdfFormatParser> _formatParsers;

        public PdfRaceResultRepository()
        {
            // Initialize format parsers in priority order (most specific first)
            _formatParsers = new List<IPdfFormatParser>
            {
                new GrandChallengeFormatParser(),
                new FrenchColumnFormatParser(),
                new CrossCupFormatParser(),
                new StandardFormatParser()
            };
        }

        public Dictionary<int, string> GetRaceResults(string filePath, List<Member> members)
        {
            var results = new Dictionary<int, string>();

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"PDF file not found: {filePath}");
            }

            // Extract metadata from filename (e.g., "2026-01-25_Jogging de la CrossCup_Hannut_CJPL_10.20.pdf")
            _raceMetadata = ExtractMetadataFromFilename(filePath);

            // Extract text from PDF
            string pdfText = ExtractTextFromPdf(filePath);

            // Parse the text to extract race results
            var parsedResults = ParsePdfText(pdfText, members);

            // Add header
            results.Add(0, CreateHeader());

            // Add reference time if found
            var referenceTime = ExtractReferenceTime(pdfText);
            if (referenceTime.HasValue)
            {
                results.Add(1, CreateReferenceEntry(referenceTime.Value));
            }

            // Add all parsed results
            int id = 2;
            foreach (var result in parsedResults.OrderBy(r => r.Position ?? int.MaxValue))
            {
                results.Add(id++, result.ToDelimitedString());
            }

            return results;
        }

        private string ExtractTextFromPdf(string filePath)
        {
            try
            {
                using (PdfReader pdfReader = new PdfReader(filePath))
                using (PdfDocument pdfDocument = new PdfDocument(pdfReader))
                {
                    StringBuilder text = new StringBuilder();

                    for (int page = 1; page <= pdfDocument.GetNumberOfPages(); page++)
                    {
                        ITextExtractionStrategy strategy = new LocationTextExtractionStrategy();
                        string pageText = PdfTextExtractor.GetTextFromPage(pdfDocument.GetPage(page), strategy);
                        text.AppendLine(pageText);
                    }

                    return text.ToString();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to extract text from PDF: {ex.Message}", ex);
            }
        }

        private List<ParsedPdfResult> ParsePdfText(string pdfText, List<Member> members)
        {
            var results = new List<ParsedPdfResult>();
            var lines = pdfText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // Try each parser until one successfully detects the format
            IPdfFormatParser selectedParser = null;
            foreach (var parser in _formatParsers)
            {
                if (parser.CanParse(pdfText, _raceMetadata))
                {
                    selectedParser = parser;
                    break;
                }
            }

            if (selectedParser == null)
            {
                // Fallback to standard parser
                selectedParser = _formatParsers[_formatParsers.Count - 1];
            }

            // Parse all lines with the selected parser
            int lineNumber = 0;
            int successfulParses = 0;
            int skippedHeaders = 0;
            int skippedDsq = 0;
            int failedParses = 0;

            foreach (var line in lines)
            {
                lineNumber++;
                var trimmedLine = line.Trim();

                // Skip empty lines
                if (string.IsNullOrWhiteSpace(trimmedLine))
                {
                    continue;
                }

                // Skip disqualified entries (DSQ, DNF, DNS, etc.)
                if (IsDisqualifiedLine(trimmedLine))
                {
                    skippedDsq++;
                    continue;
                }

                // Try to parse the line FIRST (parser may need to see headers to detect columns)
                var result = selectedParser.ParseLine(trimmedLine, members);
                if (result != null)
                {
                    results.Add(result);
                    successfulParses++;
                }
                else
                {
                    // If parser returned null, check if it's a header line (generic check)
                    if (IsHeaderLine(trimmedLine))
                    {
                        skippedHeaders++;
                    }
                    else
                    {
                        failedParses++;
                        // Log failed lines for debugging
                        if (failedParses <= 10)  // Only log first 10 failures to avoid spam
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to parse line {lineNumber}: {trimmedLine}");
                        }
                    }
                }
            }

            // Log parsing statistics
            System.Diagnostics.Debug.WriteLine($"Parsing complete using {selectedParser.GetFormatName()}:");
            System.Diagnostics.Debug.WriteLine($"  Total lines: {lines.Length}");
            System.Diagnostics.Debug.WriteLine($"  Successful parses: {successfulParses}");
            System.Diagnostics.Debug.WriteLine($"  Failed parses: {failedParses}");
            System.Diagnostics.Debug.WriteLine($"  Skipped headers: {skippedHeaders}");
            System.Diagnostics.Debug.WriteLine($"  Skipped DSQ/DNF: {skippedDsq}");

            // Log category field statistics
            var withSex = results.Count(r => !string.IsNullOrEmpty(r.Sex));
            var withPosSex = results.Count(r => r.PositionBySex.HasValue);
            var withCategory = results.Count(r => !string.IsNullOrEmpty(r.AgeCategory));
            var withPosCat = results.Count(r => r.PositionByCategory.HasValue);
            var withRaceTime = results.Count(r => r.RaceTime.HasValue);
            var withSpeed = results.Count(r => r.Speed.HasValue);

            System.Diagnostics.Debug.WriteLine($"  Category Data:");
            System.Diagnostics.Debug.WriteLine($"    Sex: {withSex}/{results.Count}");
            System.Diagnostics.Debug.WriteLine($"    PositionBySex: {withPosSex}/{results.Count}");
            System.Diagnostics.Debug.WriteLine($"    AgeCategory: {withCategory}/{results.Count}");
            System.Diagnostics.Debug.WriteLine($"    PositionByCategory: {withPosCat}/{results.Count}");
            System.Diagnostics.Debug.WriteLine($"  Other Data:");
            System.Diagnostics.Debug.WriteLine($"    RaceTime: {withRaceTime}/{results.Count}");
            System.Diagnostics.Debug.WriteLine($"    Speed: {withSpeed}/{results.Count}");

            // If we got very few results, something might be wrong with parsing
            if (results.Count < 5 && lines.Length > 20)
            {
                System.Diagnostics.Debug.WriteLine($"WARNING: Only parsed {results.Count} results from {lines.Length} lines!");
            }

            return results;
        }

        private bool IsHeaderLine(string line)
        {
            // If line starts with a number, it's likely a data line, not a header
            var trimmed = line.Trim();
            if (trimmed.Length > 0 && char.IsDigit(trimmed[0]))
            {
                // Check if it's a position number (1-4 digits followed by space or dot)
                var posMatch = Regex.Match(trimmed, @"^(\d{1,4})[\s\.\,]");
                if (posMatch.Success)
                {
                    // This is a data line, not a header
                    return false;
                }
            }

            // Common header keywords (French and English)
            var headerKeywords = new[] 
            { 
                "classement", "classification", "résultats", "results", 
                "place", "position", "nom", "name", "temps", "time", 
                "vitesse", "speed", "équipe", "team", "club",
                "pos", "pl", "pl.", "rang", "dos", "min/km", "cat", "catégorie",
                "dossard", "bib"
            };

            var lowerLine = line.ToLowerInvariant();

            // Check for specific header patterns that are definitely headers
            if (lowerLine.Contains("pl.") && lowerLine.Contains("nom") && lowerLine.Contains("temps"))
                return true;

            // If line contains multiple header keywords, it's likely a header
            int keywordCount = headerKeywords.Count(k => lowerLine.Contains(k));

            // More strict: need at least 3 keywords to be considered a header
            return keywordCount >= 3;
        }

        private bool IsDisqualifiedLine(string line)
        {
            var lowerLine = line.ToLowerInvariant();

            // Check for disqualification indicators
            return lowerLine.Contains("dsq") || 
                   lowerLine.Contains("disqualifié") || 
                   lowerLine.Contains("disqualified") ||
                   lowerLine.Contains("dnf") ||  // Did Not Finish
                   lowerLine.Contains("dns") ||  // Did Not Start
                   lowerLine.Contains("abandon");  // Abandoned
        }

        private TimeSpan? ExtractReferenceTime(string pdfText)
        {
            // Look for "TREF" or "temps de référence" patterns
            var trefPattern = @"(?:TREF|temps\s+de\s+r[eé]f[eé]rence)[:\s]*(\d{1,2}:\d{2}:\d{2}|\d{1,2}:\d{2})";
            var match = Regex.Match(pdfText, trefPattern, RegexOptions.IgnoreCase);

            if (match.Success)
            {
                return ParseTime(match.Groups[1].Value);
            }

            return null;
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

        private string CreateHeader()
        {
            return "Header;Position;Name;Time;Team;Speed;";
        }

        private string CreateReferenceEntry(TimeSpan referenceTime)
        {
            bool isTimePerKmRace = referenceTime.TotalMinutes < RaceTimeThresholdMinutes;
            return $"TREF;{referenceTime:hh\\:mm\\:ss};RACETYPE;{(isTimePerKmRace ? "TIME_PER_KM" : "RACE_TIME")};";
        }

        private RaceMetadata ExtractMetadataFromFilename(string filePath)
        {
            var metadata = new RaceMetadata();
            var fileName = Path.GetFileNameWithoutExtension(filePath);

            // Pattern 1: "2026-01-25_Jogging de la CrossCup_Hannut_CJPL_10.20.pdf"
            // Parts: Date_RaceName_Location_Category_Distance
            var parts = fileName.Split('_');

            if (parts.Length >= 2)
            {
                // Try to parse date (first part)
                if (DateTime.TryParseExact(parts[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    metadata.RaceDate = date;
                }

                // Race name (second part, could be multiple parts before location)
                metadata.RaceName = parts.Length > 1 ? parts[1] : null;

                // Location (third part)
                metadata.Location = parts.Length > 2 ? parts[2] : null;

                // Category (fourth part, e.g., CJPL)
                metadata.Category = parts.Length > 3 ? parts[3] : null;

                // Distance (last part, e.g., 10.20 km)
                if (parts.Length > 4)
                {
                    var distanceText = parts[parts.Length - 1].Replace(',', '.');
                    if (double.TryParse(distanceText, NumberStyles.Any, CultureInfo.InvariantCulture, out var distance))
                    {
                        metadata.DistanceKm = distance;
                    }
                }
            }
            else
            {
                // Pattern 2: "20250421SeraingGC.pdf" or "Classement-10km-RaceName.pdf"
                // Try to extract date from start (YYYYMMDD format)
                if (fileName.Length >= 8 && fileName.Substring(0, 8).All(char.IsDigit))
                {
                    var dateStr = fileName.Substring(0, 8);
                    if (DateTime.TryParseExact(dateStr, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                    {
                        metadata.RaceDate = date;

                        // Rest of filename is race name
                        var remaining = fileName.Substring(8);

                        // Check if it ends with "GC" (Grand Challenge)
                        if (remaining.ToUpperInvariant().EndsWith("GC"))
                        {
                            metadata.Category = "GC";
                            metadata.RaceName = remaining.Substring(0, remaining.Length - 2);
                        }
                        else
                        {
                            metadata.RaceName = remaining;
                        }
                    }
                }
                else if (fileName.StartsWith("Classement-"))
                {
                    // Pattern: "Classement-10km-RaceName.pdf"
                    var classementParts = fileName.Split('-');
                    if (classementParts.Length >= 3)
                    {
                        // Extract distance
                        var distancePart = classementParts[1].ToLowerInvariant();
                        var distanceMatch = Regex.Match(distancePart, @"(\d+(?:[\.,]\d+)?)km");
                        if (distanceMatch.Success)
                        {
                            var distanceText = distanceMatch.Groups[1].Value.Replace(',', '.');
                            if (double.TryParse(distanceText, NumberStyles.Any, CultureInfo.InvariantCulture, out var distance))
                            {
                                metadata.DistanceKm = distance;
                            }
                        }

                        // Race name is everything after distance
                        metadata.RaceName = string.Join("-", classementParts.Skip(2));
                    }
                }
                else
                {
                    // Just use filename as race name
                    metadata.RaceName = fileName;
                }
            }

            return metadata;
        }

        private class RaceMetadata
        {
            public DateTime? RaceDate { get; set; }
            public string RaceName { get; set; }
            public string Location { get; set; }
            public string Category { get; set; }
            public double? DistanceKm { get; set; }
        }

        private enum PdfFormatType
        {
            Standard,
            CrossCup,
            FrenchColumnFormat
        }

        // ===== Format Parser Interface and Implementations =====

        private interface IPdfFormatParser
        {
            bool CanParse(string pdfText, RaceMetadata metadata);
            ParsedPdfResult ParseLine(string line, List<Member> members);
            string GetFormatName();
        }

        private abstract class BasePdfFormatParser : IPdfFormatParser
        {
            protected const string PositionPattern = @"^(\d+)[\s\.]+";
            protected const string TimePattern = @"(\d{1,2}:\d{2}:\d{2}|\d{1,2}:\d{2})";
            protected const string SpeedPattern = @"(\d+[\.,]\d+)\s*(?:km/h)?";

            public abstract bool CanParse(string pdfText, RaceMetadata metadata);
            public abstract ParsedPdfResult ParseLine(string line, List<Member> members);
            public abstract string GetFormatName();

            protected double? ParseSpeed(string speedText)
            {
                if (string.IsNullOrWhiteSpace(speedText))
                    return null;

                // Store original for debugging
                var originalSpeedText = speedText;

                // Replace comma with period for decimal
                speedText = speedText.Replace(',', '.');

                // Remove km/h suffix if present
                speedText = Regex.Replace(speedText, @"\s*km/h.*", "", RegexOptions.IgnoreCase).Trim();

                // Remove any non-numeric characters except decimal point and minus sign
                speedText = Regex.Replace(speedText, @"[^\d\.\-]", "");

                // Handle multiple decimal points - keep only the first one
                var decimalIndex = speedText.IndexOf('.');
                if (decimalIndex >= 0)
                {
                    var afterFirst = speedText.Substring(decimalIndex + 1);
                    if (afterFirst.Contains('.'))
                    {
                        speedText = speedText.Substring(0, decimalIndex + 1) + afterFirst.Replace(".", "");
                    }
                }

                if (!string.IsNullOrWhiteSpace(speedText) && 
                    double.TryParse(speedText, NumberStyles.Any, CultureInfo.InvariantCulture, out double speed))
                {
                    // Check if the value is too large (likely missing decimal point)
                    // For example, 1700 should be 17.00, 1500 should be 15.00
                    if (speed >= 100 && speed < 10000)
                    {
                        // Likely missing decimal point - divide by 100
                        speed = speed / 100.0;
                        System.Diagnostics.Debug.WriteLine($"Speed adjusted from {speed * 100:F0} to {speed:F2} km/h. Original: '{originalSpeedText}'");
                    }
                    // Also check for values like 170 which should be 17.0
                    else if (speed > 30 && speed < 100)
                    {
                        // Likely missing decimal point - divide by 10
                        speed = speed / 10.0;
                        System.Diagnostics.Debug.WriteLine($"Speed adjusted from {speed * 10:F0} to {speed:F2} km/h. Original: '{originalSpeedText}'");
                    }

                    // Validate speed is plausible for running (between 0 and 30 km/h)
                    // World record marathon pace is ~21 km/h, walking is ~5 km/h
                    if (speed >= 0.0 && speed <= 30.0)
                    {
                        return speed;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Speed {speed:F2} km/h is out of plausible range (0-30 km/h). Original: '{originalSpeedText}'");
                        return null;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Failed to parse speed from '{originalSpeedText}' (cleaned: '{speedText}')");
                return null;
            }

            protected TimeSpan? ParseTime(string timeText)
            {
                if (string.IsNullOrWhiteSpace(timeText))
                    return null;

                // Clean the time text - remove any non-time characters
                timeText = timeText.Trim();

                // Remove any leading/trailing non-digit/colon characters
                timeText = Regex.Replace(timeText, @"^[^\d:]+|[^\d:]+$", "");

                if (string.IsNullOrWhiteSpace(timeText))
                    return null;

                // Try format: h:mm:ss (e.g., "1:23:45")
                if (TimeSpan.TryParseExact(timeText, @"h\:mm\:ss", CultureInfo.InvariantCulture, out var time1))
                    return time1;

                // Try format: hh:mm:ss (e.g., "01:23:45")
                if (TimeSpan.TryParseExact(timeText, @"hh\:mm\:ss", CultureInfo.InvariantCulture, out var time2))
                    return time2;

                // Try format: m:ss (e.g., "3:45" - common for pace/min per km)
                if (TimeSpan.TryParseExact(timeText, @"m\:ss", CultureInfo.InvariantCulture, out var time3))
                    return time3;

                // Try format: mm:ss (e.g., "03:45")
                if (TimeSpan.TryParseExact(timeText, @"mm\:ss", CultureInfo.InvariantCulture, out var time4))
                    return time4;

                // Try standard TimeSpan.Parse as last resort
                if (TimeSpan.TryParse(timeText, CultureInfo.InvariantCulture, out var time5))
                    return time5;

                // If all parsing failed, log it for debugging
                System.Diagnostics.Debug.WriteLine($"Failed to parse time: '{timeText}'");
                return null;
            }

            protected Member FindMatchingMember(List<Member> members, string fullName)
            {
                if (string.IsNullOrWhiteSpace(fullName))
                    return null;

                var normalizedFullName = fullName.RemoveDiacritics().ToLowerInvariant();

                return members.FirstOrDefault(m =>
                {
                    var normalizedFirst = m.FirstName.RemoveDiacritics().ToLowerInvariant();
                    var normalizedLast = m.LastName.RemoveDiacritics().ToLowerInvariant();

                    return normalizedFullName.Contains(normalizedFirst) &&
                           normalizedFullName.Contains(normalizedLast);
                });
            }

            protected void ExtractCategoryFromText(string text, ParsedPdfResult result)
            {
                if (string.IsNullOrWhiteSpace(text))
                    return;

                var parts = text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                // Debug: log what we're trying to extract from
                var originalText = text;
                int extractionCount = 0;

                for (int i = 0; i < parts.Length; i++)
                {
                    var trimmed = parts[i].Trim();
                    var upper = trimmed.ToUpperInvariant();

                    // Skip if it's clearly not a category
                    if (string.IsNullOrEmpty(trimmed))
                        continue;

                    // Skip numbers that look like positions or bib numbers
                    if (Regex.IsMatch(trimmed, @"^\d{1,4}$"))
                    {
                        // Could be position by sex or category - check context
                        if (int.TryParse(trimmed, out int posNum) && posNum < 500)
                        {
                            // If we just found a category, this might be position by category
                            if (result.PositionByCategory == null && result.AgeCategory != null)
                            {
                                result.PositionByCategory = posNum;
                                extractionCount++;
                                System.Diagnostics.Debug.WriteLine($"    Extracted PositionByCategory: {posNum}");
                            }
                            // If we just found sex, this might be position by sex
                            else if (result.PositionBySex == null && result.Sex != null)
                            {
                                result.PositionBySex = posNum;
                                extractionCount++;
                                System.Diagnostics.Debug.WriteLine($"    Extracted PositionBySex: {posNum}");
                            }
                        }
                        continue;
                    }

                    // Skip times
                    if (Regex.IsMatch(trimmed, @"^\d{1,2}:\d{2}"))
                        continue;

                    // Skip speeds
                    if (Regex.IsMatch(trimmed, @"^\d+[\.,]\d+$"))
                        continue;

                    // Extract sex (single character M, F, H, D)
                    if (result.Sex == null && Regex.IsMatch(upper, @"^[MFHD]$"))
                    {
                        if (upper == "M" || upper == "H")
                            result.Sex = "M";
                        else if (upper == "F" || upper == "D")
                            result.Sex = "F";
                        extractionCount++;
                        System.Diagnostics.Debug.WriteLine($"    Extracted Sex: {result.Sex} from '{trimmed}'");
                        continue;
                    }

                    // Check for category codes - comprehensive list
                    if (result.AgeCategory == null && IsValidCategoryCode(trimmed))
                    {
                        result.AgeCategory = trimmed;
                        extractionCount++;
                        System.Diagnostics.Debug.WriteLine($"    Extracted AgeCategory: {trimmed}");
                        continue;
                    }

                    // Check for multi-word categories (e.g., "Senior H", "Veteran 1")
                    if (result.AgeCategory == null && i < parts.Length - 1)
                    {
                        var combined = $"{trimmed} {parts[i + 1]}";
                        if (IsValidCategoryPhrase(combined))
                        {
                            result.AgeCategory = combined;
                            extractionCount++;
                            System.Diagnostics.Debug.WriteLine($"    Extracted AgeCategory (multi-word): {combined}");
                            i++; // Skip next part as we consumed it
                            continue;
                        }
                    }
                }

                if (extractionCount > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"  ExtractCategoryFromText: extracted {extractionCount} items from '{originalText}'");
                }
            }

            private bool IsValidCategoryCode(string code)
            {
                if (string.IsNullOrWhiteSpace(code))
                    return false;

                var upper = code.ToUpperInvariant().Replace("É", "E").Replace("È", "E");

                // Senior categories
                if (Regex.IsMatch(upper, @"^S[HMFD]$")) // SH, SM, SD, SF
                    return true;
                if (Regex.IsMatch(upper, @"^SEN[HFD]?$")) // SEN, SENH, SENF, SEND
                    return true;

                // Veteran categories (Men)
                if (Regex.IsMatch(upper, @"^V[1-4]$")) // V1, V2, V3, V4
                    return true;
                if (Regex.IsMatch(upper, @"^VET[1-3H]?$")) // VET, VET1, VET2, VET3, VETH
                    return true;

                // Veteran/Ainée categories (Women)
                if (Regex.IsMatch(upper, @"^[DA][1-3]$")) // D1, D2, D3, A1, A2, A3
                    return true;
                if (Regex.IsMatch(upper, @"^AINEE[1-3]?$")) // AINEE, AINEE1, AINEE2, AINEE3
                    return true;
                if (upper == "VETF")
                    return true;

                // Youth/Junior categories
                if (Regex.IsMatch(upper, @"^ESP[HFG]?$")) // ESP, ESPH, ESPF, ESPG
                    return true;
                if (Regex.IsMatch(upper, @"^ES[HFG]$")) // ESH, ESF, ESG
                    return true;
                if (Regex.IsMatch(upper, @"^JUN[HF]?$")) // JUN, JUNH, JUNF
                    return true;
                if (Regex.IsMatch(upper, @"^CAD[HF]?$")) // CAD, CADH, CADF
                    return true;
                if (upper == "SCO" || upper == "BEN" || upper == "PUP" || upper == "MIN")
                    return true;

                // Master categories (alternative system)
                if (Regex.IsMatch(upper, @"^M\d{2}$")) // M35, M40, M45, etc.
                    return true;
                if (Regex.IsMatch(upper, @"^W\d{2}$")) // W35, W40, W45, etc.
                    return true;

                // Other
                if (upper == "HAN" || upper == "HAND" || upper == "REC" || upper == "FUN" || upper == "WAL")
                    return true;

                return false;
            }

            private bool IsValidCategoryPhrase(string phrase)
            {
                if (string.IsNullOrWhiteSpace(phrase))
                    return false;

                var upper = phrase.ToUpperInvariant().Replace("É", "E").Replace("È", "E");

                // Multi-word categories
                if (Regex.IsMatch(upper, @"^SENIOR\s+[HFD]$")) // "Senior H", "Senior F", "Senior D"
                    return true;
                if (Regex.IsMatch(upper, @"^VETERAN\s+[1-4]$")) // "Veteran 1", "Veteran 2", etc.
                    return true;
                if (Regex.IsMatch(upper, @"^AINEE\s+[1-3]$")) // "Ainée 1", "Ainée 2", "Ainée 3"
                    return true;
                if (Regex.IsMatch(upper, @"^ESPOIR\s+[HFG]?$")) // "Espoir H", "Espoir F", "Espoir G"
                    return true;
                if (Regex.IsMatch(upper, @"^JUNIOR\s+[HF]?$")) // "Junior H", "Junior F"
                    return true;
                if (Regex.IsMatch(upper, @"^CADET\s+[HF]?$")) // "Cadet H", "Cadet F"
                    return true;
                if (Regex.IsMatch(upper, @"^MASTER\s+\d{2}\+?$")) // "Master 40+", "Master 45"
                    return true;
                if (Regex.IsMatch(upper, @"^WOMEN\s+\d{2}\+?$")) // "Women 40+", "Women 45"
                    return true;

                return false;
            }

            protected string CleanExtractedName(string rawName)
            {
                if (string.IsNullOrWhiteSpace(rawName))
                    return rawName;

                // Split into parts and keep only the name parts (stop at numbers/markers)
                var parts = rawName.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                var nameParts = new List<string>();

                foreach (var part in parts)
                {
                    // Stop if we hit a standalone number
                    if (Regex.IsMatch(part, @"^\d+$"))
                        break;

                    // Stop if we hit a category marker (A1, B2, V1, etc.)
                    if (Regex.IsMatch(part, @"^[A-Z]{1,2}\d*$") && IsValidCategoryCode(part))
                        break;

                    // Stop if we hit a gender marker alone
                    if (Regex.IsMatch(part, @"^[MFHDmfhd]$"))
                        break;

                    // Stop if we hit a time pattern
                    if (Regex.IsMatch(part, @"^\d{1,2}:\d{2}"))
                        break;

                    // Stop if we hit a speed pattern (decimal number)
                    if (Regex.IsMatch(part, @"^\d+[\.,]\d+$"))
                        break;

                    // This looks like a name part, add it
                    nameParts.Add(part);

                    // Safety: stop after collecting 4 parts (should be enough for any name)
                    if (nameParts.Count >= 4)
                        break;
                }

                return string.Join(" ", nameParts);
            }

            protected (string firstName, string lastName) ExtractNameParts(string fullName)
            {
                if (string.IsNullOrWhiteSpace(fullName))
                    return ("Unknown", "Unknown");

                // Clean up the name - remove common non-name elements
                fullName = CleanFullName(fullName);

                // Clean up the name
                fullName = fullName.Trim();

                // Remove common prefixes/suffixes
                fullName = RemoveCommonAffixes(fullName);

                var parts = fullName.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length == 0)
                    return ("Unknown", "Unknown");

                if (parts.Length == 1)
                    return (parts[0], parts[0]);

                if (parts.Length == 2)
                {
                    // Check if first part is all caps (likely last name)
                    if (IsAllCaps(parts[0]) && !IsAllCaps(parts[1]))
                        return (parts[1], parts[0]);  // LASTNAME FirstName

                    // Check if second part is all caps (likely last name)
                    if (IsAllCaps(parts[1]) && !IsAllCaps(parts[0]))
                        return (parts[0], parts[1]);  // FirstName LASTNAME

                    // Check for particles (de, van, von, etc.)
                    if (IsParticle(parts[0]))
                        return (parts[1], parts[0] + " " + parts[1]);  // de LastName -> FirstName: LastName, LastName: de LastName

                    // Default: first as first name, second as last name
                    return (parts[0], parts[1]);
                }

                // Multiple parts - more complex logic
                return ExtractMultiPartName(parts);
            }

            private string CleanFullName(string fullName)
            {
                if (string.IsNullOrWhiteSpace(fullName))
                    return fullName;

                // Remove time patterns like :18:30, :24:00, :25, etc.
                fullName = Regex.Replace(fullName, @":\d+(?::\d+)?", " ", RegexOptions.IgnoreCase);

                // Remove speed patterns like ".5 km/h", "10.5 km/h", etc.
                fullName = Regex.Replace(fullName, @"\d+\.?\d*\s*km/h", " ", RegexOptions.IgnoreCase);

                // Remove standalone decimal numbers at the end like ".6", ".5"
                fullName = Regex.Replace(fullName, @"\.\d+\s*$", " ");

                // Remove team prefixes (Team, Equipe, Club followed by name)
                fullName = Regex.Replace(fullName, @"\b(?:Team|Equipe|Équipe|Club)\s+\w+(?:\s+\w+)?\b", " ", RegexOptions.IgnoreCase);

                // Remove age categories like "Espoir H", "Senior F", etc.
                fullName = Regex.Replace(fullName, @"\b(?:Espoir|Senior|Junior|Cadet|Master|Veteran|Veterans|V\d+|M\d+|W\d+)\s+[HFhfDd]?\b", " ", RegexOptions.IgnoreCase);

                // Remove gender markers (single letters like M, F, H, D)
                fullName = Regex.Replace(fullName, @"\s+[MFHDmfhd]\s+", " ");
                fullName = Regex.Replace(fullName, @"^[MFHDmfhd]\s+", "");
                fullName = Regex.Replace(fullName, @"\s+[MFHDmfhd]$", "");

                // Remove category codes (like A1, B2, V1, V2, S1, SH, SF, ESP, etc.)
                fullName = Regex.Replace(fullName, @"\s+[A-Z]{1,4}\d*\s+", " ");
                fullName = Regex.Replace(fullName, @"\s+[A-Z]{1,4}\d*$", "");

                // Remove standalone numbers (except those that might be part of names)
                fullName = Regex.Replace(fullName, @"\s+\d{1,5}(?!\w)", " ");
                fullName = Regex.Replace(fullName, @"^\d{1,5}\s+", "");

                // Remove time patterns (hh:mm:ss or mm:ss) that might have leaked in
                fullName = Regex.Replace(fullName, @"\s+\d{1,2}:\d{2}(?::\d{2})?\s*", " ");

                // Remove speed patterns (decimal numbers that look like speeds)
                fullName = Regex.Replace(fullName, @"\s+\d+[\.,]\d+\s*", " ");

                // Remove trailing/leading dots and other punctuation
                fullName = Regex.Replace(fullName, @"^[\.\,\;\:]+\s*", "");
                fullName = Regex.Replace(fullName, @"\s*[\.\,\;\:]+$", "");

                // Clean up multiple spaces
                fullName = Regex.Replace(fullName, @"\s{2,}", " ");

                return fullName.Trim();
            }

            private (string firstName, string lastName) ExtractMultiPartName(string[] parts)
            {
                // Check if last part is all caps (likely last name)
                if (IsAllCaps(parts[parts.Length - 1]))
                    return (string.Join(" ", parts.Take(parts.Length - 1)), parts[parts.Length - 1]);

                // Check if first part is all caps (likely last name with particles)
                if (IsAllCaps(parts[0]))
                {
                    // Check if there are particles after
                    var lastNameParts = new List<string> { parts[0] };
                    int i = 1;

                    // Collect particles and additional last name parts
                    while (i < parts.Length && (IsParticle(parts[i]) || IsAllCaps(parts[i])))
                    {
                        lastNameParts.Add(parts[i]);
                        i++;
                    }

                    if (i < parts.Length)
                    {
                        // Everything after particles is first name
                        return (string.Join(" ", parts.Skip(i)), string.Join(" ", lastNameParts));
                    }
                }

                // Check for particles in the middle (e.g., "Jean de Backer")
                for (int i = 1; i < parts.Length - 1; i++)
                {
                    if (IsParticle(parts[i]))
                    {
                        // Everything before particle is first name, everything from particle is last name
                        return (string.Join(" ", parts.Take(i)), string.Join(" ", parts.Skip(i)));
                    }
                }

                // Check for hyphenated first names (e.g., "Jean-Pierre Dupont")
                if (parts[0].Contains('-'))
                {
                    // Likely compound first name, rest is last name
                    return (parts[0], string.Join(" ", parts.Skip(1)));
                }

                // Default: first part is first name, rest is last name
                return (parts[0], string.Join(" ", parts.Skip(1)));
            }

            private bool IsAllCaps(string text)
            {
                if (string.IsNullOrWhiteSpace(text))
                    return false;

                // Must have at least one letter
                if (!text.Any(char.IsLetter))
                    return false;

                // All letters must be uppercase
                return text.Where(char.IsLetter).All(char.IsUpper);
            }

            private bool IsParticle(string text)
            {
                if (string.IsNullOrWhiteSpace(text))
                    return false;

                var lowerText = text.ToLowerInvariant();
                var particles = new[] { "de", "van", "von", "del", "der", "den", "le", "la", "du", "d'", "di", "da", "dos", "das" };

                return particles.Contains(lowerText);
            }

            private string RemoveCommonAffixes(string name)
            {
                // Remove common titles and suffixes
                var prefixes = new[] { "mr", "mr.", "mrs", "mrs.", "ms", "ms.", "dr", "dr.", "prof", "prof." };
                var suffixes = new[] { "jr", "jr.", "sr", "sr.", "ii", "iii", "iv" };

                var lowerName = name.ToLowerInvariant();

                foreach (var prefix in prefixes)
                {
                    if (lowerName.StartsWith(prefix + " "))
                    {
                        name = name.Substring(prefix.Length + 1).Trim();
                        break;
                    }
                }

                foreach (var suffix in suffixes)
                {
                    if (lowerName.EndsWith(" " + suffix))
                    {
                        name = name.Substring(0, name.Length - suffix.Length - 1).Trim();
                        break;
                    }
                }

                return name;
            }
        }

        private class FrenchColumnFormatParser : BasePdfFormatParser
        {
            private Dictionary<string, int> _columnPositions;
            private bool _headerParsed = false;

            public override bool CanParse(string pdfText, RaceMetadata metadata)
            {
                var lowerText = pdfText.ToLowerInvariant();
                return (lowerText.Contains("pl.") || lowerText.Contains("pl ")) &&
                       lowerText.Contains("dos") &&
                       lowerText.Contains("nom") &&
                       (lowerText.Contains("vitesse") || lowerText.Contains("temps")) &&
                       lowerText.Contains("min/km");
            }

            public override string GetFormatName() => "French Column Format";

            public override ParsedPdfResult ParseLine(string line, List<Member> members)
            {
                // If we haven't parsed the header yet, try to detect column positions
                if (!_headerParsed && IsHeaderRow(line))
                {
                    _columnPositions = DetectColumnPositions(line);
                    _headerParsed = true;
                    return null; // Skip header row itself
                }

                // If we have column positions, use column-based parsing
                if (_columnPositions != null && _columnPositions.Count > 0)
                {
                    return ParseLineUsingColumns(line, members);
                }

                // Fallback to original parsing if no columns detected
                return ParseLineLegacy(line, members);
            }

            private bool IsHeaderRow(string line)
            {
                var lowerLine = line.ToLowerInvariant();
                // Must contain at least Pl. and Nom to be a header
                return (lowerLine.Contains("pl.") || lowerLine.Contains("pl ")) && 
                       lowerLine.Contains("nom");
            }

            private Dictionary<string, int> DetectColumnPositions(string headerLine)
            {
                var positions = new Dictionary<string, int>();
                var lowerHeader = headerLine.ToLowerInvariant();

                // Find positions of key columns
                var columnMappings = new Dictionary<string, string[]>
                {
                    { "position", new[] { "pl.", "pl ", "place", "pos", "classement", "clas" } },
                    { "bib", new[] { "dos", "dossard", "bib", "n°", "num" } },
                    { "name", new[] { "nom", "name", "participant" } },
                    { "sex", new[] { "sexe", "sex", "s.", "s ", "genre" } },
                    { "positionsex", new[] { "pl./s.", "pl. sexe", "pl.sexe", "clas.sexe", "clas. sexe", "pos.sexe", "classement sexe", "cl.s", "pos/sexe" } },
                    { "category", new[] { "cat.", "cat ", "catég.", "catégorie", "categ.", "category", "cat°" } },
                    { "positioncat", new[] { "pl./c.", "pl./cat.", "pl. cat", "pl.cat", "clas. cat", "clas.cat", "pos.cat", "classement cat", "cl.cat", "pos/cat" } },
                    { "team", new[] { "club", "équipe", "equipe", "team", "société", "societe" } },
                    { "speed", new[] { "vitesse", "speed", "km/h", "allure" } },
                    { "time", new[] { "temps", "time", "chrono" } },
                    { "pace", new[] { "min/km", "allure", "tempo", "pace" } }
                };

                foreach (var mapping in columnMappings)
                {
                    foreach (var keyword in mapping.Value)
                    {
                        var index = lowerHeader.IndexOf(keyword);
                        if (index >= 0)
                        {
                            // For more accurate detection, make sure we're at the start or after a delimiter
                            if (index == 0 || !char.IsLetter(lowerHeader[index - 1]))
                            {
                                // Only set if not already found (prefer first match)
                                if (!positions.ContainsKey(mapping.Key))
                                {
                                    positions[mapping.Key] = index;
                                }
                                break;
                            }
                        }
                    }
                }

                // Debug logging
                if (positions.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Detected {positions.Count} columns:");
                    foreach (var col in positions.OrderBy(p => p.Value))
                    {
                        System.Diagnostics.Debug.WriteLine($"  {col.Key}: position {col.Value}");
                    }
                }

                return positions;
            }

            private ParsedPdfResult ParseLineUsingColumns(string line, List<Member> members)
            {
                var result = new ParsedPdfResult();

                try
                {
                    // Extract position (required)
                    if (_columnPositions.ContainsKey("position"))
                    {
                        var posStart = _columnPositions["position"];
                        var posEnd = GetNextColumnPosition(posStart);
                        var posText = ExtractColumnValue(line, posStart, posEnd);

                        if (!string.IsNullOrWhiteSpace(posText))
                        {
                            posText = posText.TrimEnd('.', ',', ':', ';');
                            if (int.TryParse(posText, out int position))
                            {
                                result.Position = position;
                            }
                            else
                            {
                                // Not a valid position number - might be header or footer
                                return null;
                            }
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else
                    {
                        return null; // No position column found
                    }

                    // Extract name (required)
                    if (_columnPositions.ContainsKey("name"))
                    {
                        var nameStart = _columnPositions["name"];
                        var nameEnd = GetNextColumnPosition(nameStart);
                        var rawName = ExtractColumnValue(line, nameStart, nameEnd);

                        if (string.IsNullOrWhiteSpace(rawName))
                        {
                            return null; // Name is required
                        }

                        // Clean the name more aggressively - stop at first number or category marker
                        // This handles cases where column boundaries overlap
                        result.FullName = CleanExtractedName(rawName);

                        if (string.IsNullOrWhiteSpace(result.FullName))
                        {
                            return null; // Name is required
                        }
                    }
                    else
                    {
                        return null; // No name column found
                    }

                    // Extract team (optional)
                    if (_columnPositions.ContainsKey("team"))
                    {
                        var teamStart = _columnPositions["team"];
                        var teamEnd = GetNextColumnPosition(teamStart);
                        result.Team = ExtractColumnValue(line, teamStart, teamEnd);
                    }

                    // Extract speed (optional)
                    if (_columnPositions.ContainsKey("speed"))
                    {
                        var speedStart = _columnPositions["speed"];
                        var speedEnd = GetNextColumnPosition(speedStart);
                        var speedText = ExtractColumnValue(line, speedStart, speedEnd);

                        if (!string.IsNullOrWhiteSpace(speedText))
                        {
                            var parsedSpeed = ParseSpeed(speedText);
                            if (parsedSpeed.HasValue)
                            {
                                result.Speed = parsedSpeed.Value;
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"Position {result.Position}: Could not parse speed from '{speedText}'");
                            }
                        }
                    }

                    // Extract time (optional but common)
                    if (_columnPositions.ContainsKey("time"))
                    {
                        var timeStart = _columnPositions["time"];
                        var timeEnd = GetNextColumnPosition(timeStart);
                        var timeText = ExtractColumnValue(line, timeStart, timeEnd);

                        if (!string.IsNullOrWhiteSpace(timeText))
                        {
                            var parsedTime = ParseTime(timeText);
                            if (parsedTime.HasValue)
                            {
                                // Race time should be > 15 minutes
                                if (parsedTime.Value.TotalMinutes > RaceTimeThresholdMinutes)
                                {
                                    result.RaceTime = parsedTime.Value;
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"Position {result.Position}: Time '{timeText}' = {parsedTime.Value.TotalMinutes:F2} min is too short for race time (< 15 min)");
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"Position {result.Position}: Failed to parse time '{timeText}'");
                            }
                        }
                    }

                    // Extract pace (min/km) (optional)
                    if (_columnPositions.ContainsKey("pace"))
                    {
                        var paceStart = _columnPositions["pace"];
                        var paceEnd = GetNextColumnPosition(paceStart);
                        var paceText = ExtractColumnValue(line, paceStart, paceEnd);

                        if (!string.IsNullOrWhiteSpace(paceText))
                        {
                            // Clean pace text - it might be in format "m:ss" or "mm:ss"
                            paceText = paceText.Trim();

                            var parsedPace = ParseTime(paceText);
                            if (parsedPace.HasValue)
                            {
                                // Pace should be under 15 minutes per km (reasonable for running)
                                if (parsedPace.Value.TotalMinutes < RaceTimeThresholdMinutes)
                                {
                                    result.TimePerKm = parsedPace.Value;
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"Position {result.Position}: Pace '{paceText}' = {parsedPace.Value.TotalMinutes:F2} min is too high (>= 15 min)");
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"Position {result.Position}: Failed to parse pace '{paceText}'");
                            }
                        }
                    }

                    // Extract sex (optional)
                    if (_columnPositions.ContainsKey("sex"))
                    {
                        var sexStart = _columnPositions["sex"];
                        var sexEnd = GetNextColumnPosition(sexStart);
                        var sexText = ExtractColumnValue(line, sexStart, sexEnd);

                        if (!string.IsNullOrWhiteSpace(sexText))
                        {
                            sexText = sexText.Trim().ToLowerInvariant();
                            // Accept m, f, h (homme), d (dame)
                            if (sexText == "m" || sexText == "h" || sexText.StartsWith("mas") || sexText.StartsWith("hom"))
                                result.Sex = "M";
                            else if (sexText == "f" || sexText == "d" || sexText.StartsWith("fem") || sexText.StartsWith("dam"))
                                result.Sex = "F";
                        }
                    }

                    // Extract position by sex (optional)
                    if (_columnPositions.ContainsKey("positionsex"))
                    {
                        var posSexStart = _columnPositions["positionsex"];
                        var posSexEnd = GetNextColumnPosition(posSexStart);
                        var posSexText = ExtractColumnValue(line, posSexStart, posSexEnd);

                        if (!string.IsNullOrWhiteSpace(posSexText))
                        {
                            // Remove any non-digit characters except at start
                            posSexText = Regex.Replace(posSexText.Trim(), @"[^\d]", "");
                            if (int.TryParse(posSexText, out int posSex))
                            {
                                result.PositionBySex = posSex;
                            }
                        }
                    }

                    // Extract age category (optional)
                    if (_columnPositions.ContainsKey("category"))
                    {
                        var catStart = _columnPositions["category"];
                        var catEnd = GetNextColumnPosition(catStart);
                        var catText = ExtractColumnValue(line, catStart, catEnd);

                        if (!string.IsNullOrWhiteSpace(catText))
                        {
                            // Clean category text - remove numbers that look like positions
                            catText = catText.Trim();
                            // Common categories: SH, SD, V1, V2, V3, ESF, A1, A2, A3, etc.
                            // Keep only text that looks like a category (2-20 chars, letters and numbers)
                            if (Regex.IsMatch(catText, @"^[A-Za-zÀ-ÿ\d\s\-]{1,20}$"))
                            {
                                result.AgeCategory = catText;
                            }
                        }
                    }

                    // Extract position per category (optional)
                    if (_columnPositions.ContainsKey("positioncat"))
                    {
                        var posCatStart = _columnPositions["positioncat"];
                        var posCatEnd = GetNextColumnPosition(posCatStart);
                        var posCatText = ExtractColumnValue(line, posCatStart, posCatEnd);

                        if (!string.IsNullOrWhiteSpace(posCatText))
                        {
                            // Remove any non-digit characters
                            posCatText = Regex.Replace(posCatText.Trim(), @"[^\d]", "");
                            if (int.TryParse(posCatText, out int posCat))
                            {
                                result.PositionByCategory = posCat;
                            }
                        }
                    }

                    // Match member
                    var matchedMember = FindMatchingMember(members, result.FullName);
                    if (matchedMember != null)
                    {
                        result.FirstName = matchedMember.FirstName;
                        result.LastName = matchedMember.LastName;
                        result.IsMember = true;
                    }
                    else
                    {
                        var namePartsExtracted = ExtractNameParts(result.FullName);
                        result.FirstName = namePartsExtracted.firstName;
                        result.LastName = namePartsExtracted.lastName;
                        result.IsMember = false;
                    }

                    // Valid if we have position and name
                    return result.Position.HasValue && !string.IsNullOrWhiteSpace(result.FullName) ? result : null;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error parsing line: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"  Line content: {line}");
                    return null;
                }
            }

            private int GetNextColumnPosition(int currentPosition)
            {
                // Find the next column position after the current one
                var nextPositions = _columnPositions.Values.Where(p => p > currentPosition).OrderBy(p => p);
                return nextPositions.Any() ? nextPositions.First() : int.MaxValue;
            }

            private string ExtractColumnValue(string line, int startPos, int endPos)
            {
                if (startPos >= line.Length)
                    return string.Empty;

                if (endPos == int.MaxValue || endPos > line.Length)
                {
                    // Extract from startPos to end of line
                    return line.Substring(startPos).Trim();
                }
                else
                {
                    // Extract between startPos and endPos
                    var length = Math.Min(endPos - startPos, line.Length - startPos);
                    return line.Substring(startPos, length).Trim();
                }
            }

            private ParsedPdfResult ParseLineLegacy(string line, List<Member> members)
            {
                // Original parsing logic as fallback
                var result = new ParsedPdfResult();

                var parts = Regex.Split(line, @"\s{2,}")
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrEmpty(p))
                    .ToArray();

                if (parts.Length < 2)
                {
                    parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                }

                if (parts.Length < 2)
                    return null;

                var positionText = parts[0].TrimEnd('.', ',', ':', ';');
                if (!int.TryParse(positionText, out int position))
                    return null;
                result.Position = position;

                int currentIndex = 1;

                if (currentIndex < parts.Length && 
                    int.TryParse(parts[currentIndex], out int bibNumber) && 
                    bibNumber < 10000)
                {
                    currentIndex++;
                }

                if (currentIndex >= parts.Length)
                    return null;

                var nameParts = new List<string>();
                while (currentIndex < parts.Length)
                {
                    var part = parts[currentIndex];

                    if (part.Contains(':'))
                        break;

                    if (Regex.IsMatch(part, @"^\d+[\.,]\d+$"))
                        break;

                    nameParts.Add(part);
                    currentIndex++;

                    if (nameParts.Count >= 2 && currentIndex < parts.Length)
                    {
                        var nextPart = parts[currentIndex];
                        if (nextPart.Contains(':') || Regex.IsMatch(nextPart, @"^\d+[\.,]\d+$"))
                            break;
                        if (nextPart.Length > 15 && !nextPart.All(c => char.IsUpper(c) || char.IsWhiteSpace(c)))
                            break;
                    }
                }

                if (nameParts.Count == 0)
                    return null;

                result.FullName = string.Join(" ", nameParts);

                var teamParts = new List<string>();
                while (currentIndex < parts.Length)
                {
                    var part = parts[currentIndex];

                    if (Regex.IsMatch(part, @"^\d+[\.,]\d+$"))
                    {
                        var parsedSpeed = ParseSpeed(part);
                        if (parsedSpeed.HasValue)
                            result.Speed = parsedSpeed.Value;
                        currentIndex++;
                        break;
                    }
                    else if (Regex.IsMatch(part, @"^\d{1,2}:\d{2}"))
                    {
                        break;
                    }
                    else
                    {
                        teamParts.Add(part);
                        currentIndex++;
                    }
                }

                if (teamParts.Count > 0)
                    result.Team = string.Join(" ", teamParts);

                while (currentIndex < parts.Length)
                {
                    var part = parts[currentIndex];

                    if (Regex.IsMatch(part, TimePattern))
                    {
                        var parsedTime = ParseTime(part);
                        if (parsedTime.HasValue)
                        {
                            // Race time should be > 15 minutes, pace < 15 minutes
                            if (!result.RaceTime.HasValue && parsedTime.Value.TotalMinutes > RaceTimeThresholdMinutes)
                            {
                                result.RaceTime = parsedTime.Value;
                            }
                            else if (!result.TimePerKm.HasValue && parsedTime.Value.TotalMinutes < RaceTimeThresholdMinutes)
                            {
                                result.TimePerKm = parsedTime.Value;
                            }
                        }
                    }
                    else if (!result.Speed.HasValue && Regex.IsMatch(part, @"^\d+[\.,]\d+$"))
                    {
                        var parsedSpeed = ParseSpeed(part);
                        if (parsedSpeed.HasValue)
                            result.Speed = parsedSpeed.Value;
                    }

                    currentIndex++;
                }

                var matchedMember = FindMatchingMember(members, result.FullName);
                if (matchedMember != null)
                {
                    result.FirstName = matchedMember.FirstName;
                    result.LastName = matchedMember.LastName;
                    result.IsMember = true;
                }
                else
                {
                    var namePartsExtracted = ExtractNameParts(result.FullName);
                    result.FirstName = namePartsExtracted.firstName;
                    result.LastName = namePartsExtracted.lastName;
                    result.IsMember = false;
                }

                return result.Position.HasValue && !string.IsNullOrWhiteSpace(result.FullName) ? result : null;
            }
        }

        // CrossCupFormatParser class
        private class CrossCupFormatParser : BasePdfFormatParser
        {
            public override bool CanParse(string pdfText, RaceMetadata metadata)
            {
                var lowerText = pdfText.ToLowerInvariant();
                return lowerText.Contains("cjpl") ||
                       lowerText.Contains("crosscup") ||
                       lowerText.Contains("cross cup") ||
                       (metadata?.Category?.ToLowerInvariant().Contains("cjpl") ?? false);
            }

            public override string GetFormatName() => "CrossCup/CJPL Format";

            public override ParsedPdfResult ParseLine(string line, List<Member> members)
            {
                var result = new ParsedPdfResult();

                // Extract position
                var posMatch = Regex.Match(line, PositionPattern);
                if (!posMatch.Success || !int.TryParse(posMatch.Groups[1].Value, out int position))
                    return null;
                result.Position = position;

                var workingLine = line.Substring(posMatch.Length).Trim();

                // SAVE the original line for name extraction (before removing everything)
                var nameSource = workingLine;

                // Extract times
                var timeMatches = Regex.Matches(workingLine, TimePattern);
                foreach (Match timeMatch in timeMatches)
                {
                    var parsedTime = ParseTime(timeMatch.Value);
                    if (parsedTime.HasValue)
                    {
                        // Race time should be > 15 minutes, pace < 15 minutes
                        if (!result.RaceTime.HasValue && parsedTime.Value.TotalMinutes > RaceTimeThresholdMinutes)
                        {
                            result.RaceTime = parsedTime.Value;
                            workingLine = workingLine.Replace(timeMatch.Value, "").Trim();
                        }
                        else if (!result.TimePerKm.HasValue && parsedTime.Value.TotalMinutes < RaceTimeThresholdMinutes)
                        {
                            result.TimePerKm = parsedTime.Value;
                            workingLine = workingLine.Replace(timeMatch.Value, "").Trim();
                        }
                    }
                }

                // Extract speed
                var speedMatch = Regex.Match(workingLine, SpeedPattern);
                if (speedMatch.Success)
                {
                    var parsedSpeed = ParseSpeed(speedMatch.Groups[1].Value);
                    if (parsedSpeed.HasValue)
                    {
                        result.Speed = parsedSpeed.Value;
                        workingLine = workingLine.Replace(speedMatch.Value, "").Trim();
                    }
                }

                // Extract team
                var teamMatch = Regex.Match(workingLine, @"\((.*?)\)|\[(.*?)\]");
                if (teamMatch.Success)
                {
                    result.Team = !string.IsNullOrEmpty(teamMatch.Groups[1].Value)
                        ? teamMatch.Groups[1].Value
                        : teamMatch.Groups[2].Value;
                    workingLine = workingLine.Replace(teamMatch.Value, "").Trim();
                }

                // Try to extract category info from remaining text
                ExtractCategoryFromText(workingLine, result);

                // Set full name from the REMAINING text after extractions
                result.FullName = workingLine.Trim();

                // Clean the name FIRST
                result.FullName = CleanExtractedName(result.FullName);

                // NOW check if name is empty after cleaning - if so, use original source
                if (string.IsNullOrWhiteSpace(result.FullName))
                {
                    result.FullName = CleanExtractedName(nameSource);
                }

                // Final fallback if still empty
                if (string.IsNullOrWhiteSpace(result.FullName))
                {
                    result.FullName = nameSource;
                }

                // Match member
                var matchedMember = FindMatchingMember(members, result.FullName);
                if (matchedMember != null)
                {
                    result.FirstName = matchedMember.FirstName;
                    result.LastName = matchedMember.LastName;
                    result.IsMember = true;
                }
                else
                {
                    var nameParts = ExtractNameParts(result.FullName);
                    result.FirstName = nameParts.firstName;
                    result.LastName = nameParts.lastName;
                    result.IsMember = false;
                }

                // Valid if we have position and name (time is optional)
                return result.Position.HasValue && !string.IsNullOrWhiteSpace(result.FullName) ? result : null;
            }
        }

        // StandardFormatParser class
        private class StandardFormatParser : BasePdfFormatParser
        {
            public override bool CanParse(string pdfText, RaceMetadata metadata)
            {
                return true; // Always can parse (fallback)
            }

            public override string GetFormatName() => "Standard Format";

            public override ParsedPdfResult ParseLine(string line, List<Member> members)
            {
                var result = new ParsedPdfResult();

                // Extract position
                var posMatch = Regex.Match(line, PositionPattern);
                if (!posMatch.Success || !int.TryParse(posMatch.Groups[1].Value, out int position))
                    return null;
                result.Position = position;

                // Extract times
                var timeMatches = Regex.Matches(line, TimePattern);
                foreach (Match timeMatch in timeMatches)
                {
                    var parsedTime = ParseTime(timeMatch.Value);
                    if (parsedTime.HasValue)
                    {
                        // Race time should be > 15 minutes, pace < 15 minutes
                        if (!result.RaceTime.HasValue && parsedTime.Value.TotalMinutes > RaceTimeThresholdMinutes)
                        {
                            result.RaceTime = parsedTime.Value;
                        }
                        else if (!result.TimePerKm.HasValue && parsedTime.Value.TotalMinutes < RaceTimeThresholdMinutes)
                        {
                            result.TimePerKm = parsedTime.Value;
                        }
                    }
                }

                // Extract speed
                var speedMatch = Regex.Match(line, SpeedPattern);
                if (speedMatch.Success)
                {
                    var parsedSpeed = ParseSpeed(speedMatch.Groups[1].Value);
                    if (parsedSpeed.HasValue)
                        result.Speed = parsedSpeed.Value;
                }

                // Extract name and team
                var workingLine = line.Substring(posMatch.Length).Trim();

                // SAVE the original line for name extraction (before removing everything)
                var nameSource = workingLine;

                workingLine = Regex.Replace(workingLine, @"\d{1,2}:\d{2}:\d{2}", "");
                workingLine = Regex.Replace(workingLine, @"\d{1,2}:\d{2}", "");
                workingLine = Regex.Replace(workingLine, @"\d+[\.,]\d+\s*(?:km/h)?", "");
                workingLine = workingLine.Trim();

                // Extract team
                var teamMatch = Regex.Match(workingLine, @"\((.*?)\)|\[(.*?)\]");
                if (teamMatch.Success)
                {
                    result.Team = !string.IsNullOrEmpty(teamMatch.Groups[1].Value)
                        ? teamMatch.Groups[1].Value
                        : teamMatch.Groups[2].Value;
                    workingLine = workingLine.Replace(teamMatch.Value, "").Trim();
                }

                // Try to extract category info from remaining text
                ExtractCategoryFromText(workingLine, result);

                // Set full name from the REMAINING text after extractions
                result.FullName = workingLine.Trim();

                // Clean the name FIRST
                result.FullName = CleanExtractedName(result.FullName);

                // NOW check if name is empty after cleaning - if so, use original source
                if (string.IsNullOrWhiteSpace(result.FullName))
                {
                    result.FullName = CleanExtractedName(nameSource);
                }

                // Final fallback if still empty
                if (string.IsNullOrWhiteSpace(result.FullName))
                {
                    result.FullName = nameSource;
                }

                // Match member
                var matchedMember = FindMatchingMember(members, result.FullName);
                if (matchedMember != null)
                {
                    result.FirstName = matchedMember.FirstName;
                    result.LastName = matchedMember.LastName;
                    result.IsMember = true;
                }
                else
                {
                    var nameParts = ExtractNameParts(result.FullName);
                    result.FirstName = nameParts.firstName;
                    result.LastName = nameParts.lastName;
                    result.IsMember = false;
                }

                // Valid if we have position and name (time is optional)
                return result.Position.HasValue && !string.IsNullOrWhiteSpace(result.FullName) ? result : null;
            }
        }

        // GrandChallengeFormatParser class
        private class GrandChallengeFormatParser : BasePdfFormatParser
        {
            public override bool CanParse(string pdfText, RaceMetadata metadata)
            {
                var lowerText = pdfText.ToLowerInvariant();

                // Check for "GC" in filename or text
                var fileName = metadata?.RaceName?.ToLowerInvariant() ?? "";

                return lowerText.Contains("grand challenge") ||
                       lowerText.Contains("grande challenge") ||
                       fileName.Contains("gc") ||
                       lowerText.Contains("seraing") ||
                       lowerText.Contains("blanc gravier") ||
                       lowerText.Contains("blancgravier");
            }

            public override string GetFormatName() => "Grand Challenge Format";

            public override ParsedPdfResult ParseLine(string line, List<Member> members)
            {
                var result = new ParsedPdfResult();

                // Grand Challenge format examples (can vary):
                // "1    DUPONT Jean        00:35:25    AC Hannut    16.95"
                // "1. DUPONT Jean 00:35:25 AC Hannut 16.95"

                // Try to match position at start
                var posMatch = Regex.Match(line, PositionPattern);
                if (!posMatch.Success || !int.TryParse(posMatch.Groups[1].Value, out int position))
                    return null;
                result.Position = position;

                var workingLine = line.Substring(posMatch.Length).Trim();

                // SAVE the original line for name extraction (before removing everything)
                var nameSource = workingLine;

                // Extract times first (to avoid confusion with name)
                var timeMatches = Regex.Matches(workingLine, TimePattern);
                foreach (Match timeMatch in timeMatches)
                {
                    var parsedTime = ParseTime(timeMatch.Value);
                    if (parsedTime.HasValue)
                    {
                        // Race time should be > 15 minutes, pace < 15 minutes
                        if (!result.RaceTime.HasValue && parsedTime.Value.TotalMinutes > RaceTimeThresholdMinutes)
                        {
                            result.RaceTime = parsedTime.Value;
                            workingLine = workingLine.Replace(timeMatch.Value, "").Trim();
                        }
                        else if (!result.TimePerKm.HasValue && parsedTime.Value.TotalMinutes < RaceTimeThresholdMinutes)
                        {
                            result.TimePerKm = parsedTime.Value;
                            workingLine = workingLine.Replace(timeMatch.Value, "").Trim();
                        }
                    }
                }

                // Extract speed
                var speedMatch = Regex.Match(workingLine, SpeedPattern);
                if (speedMatch.Success)
                {
                    var parsedSpeed = ParseSpeed(speedMatch.Groups[1].Value);
                    if (parsedSpeed.HasValue)
                    {
                        result.Speed = parsedSpeed.Value;
                        workingLine = workingLine.Replace(speedMatch.Value, "").Trim();
                    }
                }

                // What remains could be name + team
                // Try to split by multiple spaces (like French column format)
                var parts = Regex.Split(workingLine, @"\s{2,}")
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrEmpty(p))
                    .ToArray();

                if (parts.Length == 0)
                {
                    // Fall back to single space split if no multi-space found
                    parts = workingLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                }

                if (parts.Length >= 1)
                {
                    // First part is likely the name
                    result.FullName = parts[0];

                    // If there are more parts, last one might be team
                    if (parts.Length > 1)
                    {
                        // Check if last part looks like a team (contains letters, not just numbers)
                        var lastPart = parts[parts.Length - 1];
                        if (lastPart.Any(char.IsLetter))
                        {
                            result.Team = lastPart;
                        }

                        // Middle parts might be additional name parts or team
                        if (parts.Length > 2)
                        {
                            // If we haven't identified a team yet, join all but first as team
                            if (string.IsNullOrEmpty(result.Team))
                            {
                                result.Team = string.Join(" ", parts.Skip(1));
                            }
                        }

                        // Try to extract category info from all parts (except first which is name)
                        ExtractCategoryFromText(string.Join(" ", parts.Skip(1)), result);
                    }

                    // ALSO try to extract category from the name part itself (might be embedded)
                    // This handles cases like "DUPONT Jean SH" or "DUPONT Jean M"
                    ExtractCategoryFromText(result.FullName, result);

                    // Clean the name after category extraction to remove any category markers
                    result.FullName = CleanExtractedName(result.FullName);

                    // Check if name is empty after cleaning - if so, use original source
                    if (string.IsNullOrWhiteSpace(result.FullName))
                    {
                        result.FullName = CleanExtractedName(nameSource);
                    }
                }
                else
                {
                    // No parts found after splitting, use original source
                    result.FullName = nameSource;
                    // Try to extract category from the full name
                    ExtractCategoryFromText(result.FullName, result);
                    result.FullName = CleanExtractedName(result.FullName);
                }

                // Final safety check: if name is still empty, use original without cleaning
                if (string.IsNullOrWhiteSpace(result.FullName))
                {
                    result.FullName = nameSource;
                }

                // Match member
                var matchedMember = FindMatchingMember(members, result.FullName);
                if (matchedMember != null)
                {
                    result.FirstName = matchedMember.FirstName;
                    result.LastName = matchedMember.LastName;
                    result.IsMember = true;
                }
                else
                {
                    var nameParts = ExtractNameParts(result.FullName);
                    result.FirstName = nameParts.firstName;
                    result.LastName = nameParts.lastName;
                    result.IsMember = false;
                }

                // Valid if we have position and name (time is optional)
                return result.Position.HasValue && !string.IsNullOrWhiteSpace(result.FullName) ? result : null;
            }
        }

        // ParsedPdfResult class - used to hold parsed data before conversion
        private class ParsedPdfResult
        {
            public int? Position { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string FullName { get; set; }
            public TimeSpan? RaceTime { get; set; }
            public TimeSpan? TimePerKm { get; set; }
            public string Team { get; set; }
            public double? Speed { get; set; }
            public string Sex { get; set; }
            public int? PositionBySex { get; set; }
            public string AgeCategory { get; set; }
            public int? PositionByCategory { get; set; }
            public bool IsMember { get; set; }

            public string ToDelimitedString()
            {
                var sb = new StringBuilder();

                // Type prefix
                sb.Append(IsMember ? "TMEM;" : "TWINNER;");

                // ID (position)
                sb.Append($"{Position};");

                // Name
                sb.Append($"{LastName};{FirstName};");

                // Race time (if available)
                if (RaceTime.HasValue)
                {
                    sb.Append($"{RaceTime.Value:hh\\:mm\\:ss};");
                }
                else
                {
                    // Use a default time if not available
                    sb.Append("00:00:00;");
                }

                // Race type
                bool isTimePerKmRace = TimePerKm.HasValue && RaceTime.HasValue && 
                                       TimePerKm.Value.TotalMinutes < RaceTimeThresholdMinutes;
                sb.Append($"RACETYPE;{(isTimePerKmRace ? "TIME_PER_KM" : "RACE_TIME")};");

                // Race time (explicit)
                if (RaceTime.HasValue)
                {
                    sb.Append($"RACETIME;{RaceTime.Value:hh\\:mm\\:ss};");
                }

                // Time per km (explicit)
                if (TimePerKm.HasValue)
                {
                    sb.Append($"TIMEPERKM;{TimePerKm.Value:mm\\:ss};");
                }

                // Position
                if (Position.HasValue)
                {
                    sb.Append($"POS;{Position};");
                }

                // Team
                if (!string.IsNullOrEmpty(Team))
                {
                    sb.Append($"TEAM;{Team};");
                }

                // Speed
                if (Speed.HasValue)
                {
                    sb.Append($"SPEED;{Speed.Value:F2};");
                }

                // Sex
                if (!string.IsNullOrEmpty(Sex))
                {
                    sb.Append($"SEX;{Sex};");
                }

                // Position by Sex
                if (PositionBySex.HasValue)
                {
                    sb.Append($"POSITIONSEX;{PositionBySex};");
                }

                // Age Category
                if (!string.IsNullOrEmpty(AgeCategory))
                {
                    sb.Append($"CATEGORY;{AgeCategory};");
                }

                // Position by Category
                if (PositionByCategory.HasValue)
                {
                    sb.Append($"POSITIONCAT;{PositionByCategory};");
                }

                // Member flag
                sb.Append($"ISMEMBER;{(IsMember ? "1" : "0")};");

                return sb.ToString();
            }
        }
    }
}
