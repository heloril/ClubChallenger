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
            // Pattern-based parsers - more reliable
            _formatParsers = new List<IPdfFormatParser>
            {
                new ChallengeLaMeuseFormatParser(),  // Zatopek - very specific
                new GlobalPacingFormatParser(),       // Has specific "Clas." markers
                new GoalTimingFormatParser(),         // Has "Rank" marker
                new OtopFormatParser(),               // CJPL races
                new FrenchColumnFormatParser(),       // Generic French format
                new CJPLFormatParser(),               // Another CJPL variant
                new ChallengeCondrusienFormatParser(), // Grand Challenge variant
                new StandardFormatParser()            // Fallback - always matches
            };
        }

            // Normalize category/token for robust matching: remove diacritics, uppercase and collapse spaces
            private static string NormalizeCategoryToken(string input)
            {
                if (string.IsNullOrWhiteSpace(input))
                    return input;

                // Remove diacritics
                var normalized = input.Normalize(System.Text.NormalizationForm.FormD);
                var sb = new StringBuilder();
                foreach (var ch in normalized)
                {
                    var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                    if (uc != UnicodeCategory.NonSpacingMark)
                        sb.Append(ch);
                }

                var result = sb.ToString().Normalize(System.Text.NormalizationForm.FormC).ToUpperInvariant();
                // Collapse spaces
                result = Regex.Replace(result, @"\s+", " ").Trim();
                return result;
            }

        // Canonical category mapping and helpers are intentionally applied only in the specialized parsers

        // ===== OTOP FORMAT PARSER =====
        // Specific parser for Otop timing system PDFs
        // Columns: Place | Dos. | Nom | Prénom | Sexe | Pl./S. | Catég. | Pl./C. | Temps | Vitesse | Moy. | Points | Jetons
        private class OtopFormatParser : BasePdfFormatParser
        {
            private static readonly HashSet<string> _validCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Senior H", "Moins16 H", "Espoir H", "Veteran 1", "Moins16 D", "Vétéran 2", 
                "Veteran 3", "Ainée 1", "Espoir D", "Senior D", "Ainée 3", "Vétéran 4", "Ainée 2", "Ainée 4"
            };

            private Dictionary<string, int> _columnPositions;
            private bool _headerParsed = false;

            public override bool CanParse(string pdfText, RaceMetadata metadata)
            {
                var lower = pdfText.ToLowerInvariant();

                // STRONG indicators for Otop format (very specific)
                bool hasStrongOtopIndicators = (lower.Contains("pl./s.") && lower.Contains("pl./c.")) ||
                                               lower.Contains("otop timing") ||
                                               lower.Contains("www.otop.be");

                if (hasStrongOtopIndicators)
                    return true;

                // MEDIUM indicators - check for typical Otop column combination
                bool hasMediumOtopIndicators = lower.Contains("catég.") && 
                                              lower.Contains("prénom") &&
                                              lower.Contains("dos") &&
                                              lower.Contains("sexe");

                if (hasMediumOtopIndicators)
                    return true;

                // Check if filename indicates CJPL (which typically uses Otop)
                if (metadata?.Category?.Equals("CJPL", StringComparison.OrdinalIgnoreCase) == true)
                {
                    // Must have basic structure
                    return lower.Contains("nom") && lower.Contains("temps");
                }

                return false;
            }

            public override string GetFormatName() => "Otop Format";

            public override ParsedPdfResult ParseLine(string line, List<Member> members)
            {
                var result = new ParsedPdfResult();

                // Extract position (required) - must be at start of line
                var posMatch = Regex.Match(line, PositionPattern);
                if (!posMatch.Success || !int.TryParse(posMatch.Groups[1].Value, out int position))
                    return null;

                result.Position = position;
                var workingLine = line.Substring(posMatch.Length).Trim();
                var nameSource = workingLine;

                // Extract times first (to isolate them from name)
                var timeMatches = Regex.Matches(workingLine, TimePattern);
                foreach (Match tm in timeMatches)
                {
                    var parsed = ParseTime(tm.Value);
                    if (parsed.HasValue)
                    {
                        if (!result.RaceTime.HasValue && parsed.Value.TotalMinutes > RaceTimeThresholdMinutes)
                        {
                            result.RaceTime = parsed.Value;
                        }
                        else if (!result.TimePerKm.HasValue && parsed.Value.TotalMinutes < RaceTimeThresholdMinutes)
                        {
                            result.TimePerKm = parsed.Value;
                        }

                        // Remove the matched time
                        workingLine = workingLine.Replace(tm.Value, " ").Trim();
                    }
                }

                // Extract speed if present
                var speedMatch = Regex.Match(workingLine, SpeedPattern);
                if (speedMatch.Success)
                {
                    var parsedSpeed = ParseSpeed(speedMatch.Groups[1].Value);
                    if (parsedSpeed.HasValue)
                    {
                        result.Speed = parsedSpeed.Value;
                        workingLine = workingLine.Replace(speedMatch.Value, " ").Trim();
                    }
                }

                // Extract team from parentheses/brackets if present
                var teamMatch = Regex.Match(workingLine, @"\((.*?)\)|\[(.*?)\]");
                if (teamMatch.Success)
                {
                    result.Team = !string.IsNullOrEmpty(teamMatch.Groups[1].Value)
                        ? teamMatch.Groups[1].Value
                        : teamMatch.Groups[2].Value;
                    workingLine = workingLine.Replace(teamMatch.Value, " ").Trim();
                }

                // Try to extract category info from remaining text
                ExtractCategoryFromText(workingLine, result);

                // Set full name from the REMAINING text after extractions
                result.FullName = CleanExtractedName(workingLine);

                if (string.IsNullOrWhiteSpace(result.FullName))
                {
                    // fallback to name source cleaned
                    result.FullName = CleanExtractedName(nameSource);
                }

                // Final fallback
                if (string.IsNullOrWhiteSpace(result.FullName))
                    result.FullName = nameSource;

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

                // Validate that we have valid names (not just numbers)
                if (!IsValidName(result.FirstName) || !IsValidName(result.LastName))
                {
                    System.Diagnostics.Debug.WriteLine($"Otop: Rejected result with invalid name - FirstName: '{result.FirstName}', LastName: '{result.LastName}' at position {result.Position}");
                    return null;
                }

                return result.Position.HasValue && !string.IsNullOrWhiteSpace(result.FullName) ? result : null;
            }

            private bool IsHeaderRow(string line)
            {
                var lower = line.ToLowerInvariant();
                // More lenient: just need place/pl and nom (name)
                return (lower.Contains("place") || lower.Contains(" pl.") || lower.Contains(" pl ")) && 
                       lower.Contains("nom");
            }

            private Dictionary<string, int> DetectColumnPositions(string headerLine)
            {
                var positions = new Dictionary<string, int>();
                var lowerHeader = headerLine.ToLowerInvariant();

                var columnMappings = new Dictionary<string, string[]>
                {
                    { "position", new[] { "place", "pl.", "pl " } },
                    { "bib", new[] { "dos.", "dos", "dossard" } },
                    { "lastname", new[] { " nom", "nom " } },  // Space before/after to avoid matching in other words
                    { "firstname", new[] { "prénom", "prenom" } },
                    { "sex", new[] { "sexe" } },
                    { "positionsex", new[] { "pl./s.", "pl. s", "pl.s", "pl/s", "pl s" } },
                    { "category", new[] { "catég.", "categ.", "catégorie", " cat " } },
                    { "positioncat", new[] { "pl./c.", "pl. c", "pl.c", "pl/c", "pl c" } },
                    { "time", new[] { "temps" } },
                    { "speed", new[] { "vitesse", "km/h" } },
                    { "pace", new[] { "moy.", "moy", "min/km", "allure" } },
                    { "points", new[] { "points" } },
                    { "jetons", new[] { "jetons" } }
                };

                foreach (var mapping in columnMappings)
                {
                    foreach (var keyword in mapping.Value)
                    {
                        var index = lowerHeader.IndexOf(keyword);
                        if (index >= 0)
                        {
                            // For better matching, check boundaries only for short keywords
                            bool validMatch = true;
                            if (keyword.Length <= 4 && index > 0)
                            {
                                // Check if previous character is a letter (word boundary check)
                                var prevChar = lowerHeader[index - 1];
                                if (char.IsLetter(prevChar))
                                {
                                    validMatch = false;
                                }
                            }

                            if (validMatch && !positions.ContainsKey(mapping.Key))
                            {
                                positions[mapping.Key] = index;
                                break;
                            }
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Otop: Detected {positions.Count} columns");
                foreach (var pos in positions.OrderBy(p => p.Value))
                {
                    System.Diagnostics.Debug.WriteLine($"  {pos.Key}: {pos.Value}");
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
                        var posText = ExtractColumnValue(line, "position");
                        if (!string.IsNullOrWhiteSpace(posText) && int.TryParse(posText.TrimEnd('.', ','), out int position))
                            result.Position = position;
                        else
                            return null;
                    }

                    // Extract last name (required)
                    string lastName = null;
                    if (_columnPositions.ContainsKey("lastname"))
                    {
                        lastName = ExtractColumnValue(line, "lastname");
                    }

                    // Extract first name (required)
                    string firstName = null;
                    if (_columnPositions.ContainsKey("firstname"))
                    {
                        firstName = ExtractColumnValue(line, "firstname");
                    }

                    // Combine names - be lenient, accept even if one is missing
                    if (!string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName))
                    {
                        result.FullName = $"{firstName} {lastName}";
                        result.FirstName = firstName;
                        result.LastName = lastName;
                    }
                    else if (!string.IsNullOrEmpty(firstName))
                    {
                        result.FullName = firstName;
                        result.FirstName = firstName;
                        result.LastName = firstName;
                    }
                    else if (!string.IsNullOrEmpty(lastName))
                    {
                        result.FullName = lastName;
                        result.FirstName = lastName;
                        result.LastName = lastName;
                    }
                    else
                    {
                        // No name found
                        return null;
                    }

                    // Extract sex
                    if (_columnPositions.ContainsKey("sex"))
                    {
                        var sexText = ExtractColumnValue(line, "sex")?.Trim().ToUpperInvariant();
                        if (!string.IsNullOrEmpty(sexText))
                        {
                            if (sexText == "M" || sexText == "H")
                                result.Sex = "M";
                            else if (sexText == "F" || sexText == "D")
                                result.Sex = "F";
                        }
                    }

                    // Extract position by sex
                    if (_columnPositions.ContainsKey("positionsex"))
                    {
                        var posSexText = ExtractColumnValue(line, "positionsex");
                        if (int.TryParse(posSexText, out int posSex))
                            result.PositionBySex = posSex;
                    }

                    // Extract category
                    if (_columnPositions.ContainsKey("category"))
                    {
                        var catText = ExtractColumnValue(line, "category")?.Trim();
                        if (!string.IsNullOrWhiteSpace(catText))
                        {
                            // Accept category as-is (valid categories list is for reference only)
                            result.AgeCategory = catText;
                        }
                    }

                    // Extract position by category
                    if (_columnPositions.ContainsKey("positioncat"))
                    {
                        var posCatText = ExtractColumnValue(line, "positioncat");
                        if (int.TryParse(posCatText, out int posCat))
                            result.PositionByCategory = posCat;
                    }

                    // Extract race time
                    if (_columnPositions.ContainsKey("time"))
                    {
                        var timeText = ExtractColumnValue(line, "time");
                        var parsedTime = ParseTime(timeText);
                        if (parsedTime.HasValue)
                        {
                            // Accept any time - filter later if needed
                            if (parsedTime.Value.TotalMinutes > RaceTimeThresholdMinutes)
                                result.RaceTime = parsedTime.Value;
                            else if (parsedTime.Value.TotalMinutes > 0)
                                result.TimePerKm = parsedTime.Value; // Might be pace
                        }
                    }

                    // Extract speed
                    if (_columnPositions.ContainsKey("speed"))
                    {
                        var speedText = ExtractColumnValue(line, "speed");
                        var parsedSpeed = ParseSpeed(speedText);
                        if (parsedSpeed.HasValue)
                            result.Speed = parsedSpeed.Value;
                    }

                    // Extract pace (Moy. = min/km)
                    if (_columnPositions.ContainsKey("pace"))
                    {
                        var paceText = ExtractColumnValue(line, "pace");
                        var parsedPace = ParseTime(paceText);
                        if (parsedPace.HasValue && parsedPace.Value.TotalMinutes < RaceTimeThresholdMinutes)
                            result.TimePerKm = parsedPace.Value;
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
                        result.IsMember = false;
                    }

                    return result.Position.HasValue && !string.IsNullOrWhiteSpace(result.FullName) ? result : null;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Otop parser error: {ex.Message}");
                    return null;
                }
            }

            private string ExtractColumnValue(string line, string columnKey)
            {
                if (!_columnPositions.ContainsKey(columnKey))
                    return null;

                var startPos = _columnPositions[columnKey];
                var endPos = GetNextColumnPosition(startPos);

                if (startPos >= line.Length)
                    return string.Empty;

                if (endPos == int.MaxValue || endPos > line.Length)
                    return line.Substring(startPos).Trim();
                else
                {
                    var length = Math.Min(endPos - startPos, line.Length - startPos);
                    return line.Substring(startPos, length).Trim();
                }
            }

            private int GetNextColumnPosition(int currentPosition)
            {
                var nextPositions = _columnPositions.Values.Where(p => p > currentPosition).OrderBy(p => p);
                return nextPositions.Any() ? nextPositions.First() : int.MaxValue;
            }
        }

        // ===== GLOBAL PACING FORMAT PARSER =====
        // Specific parser for Global Pacing timing system PDFs
        // Columns: Pl. | Dos | Nom | Sexe | Clas.Sexe | Cat | Clas.Cat | Club | Vitesse | min/km | Temps | Points
        private class GlobalPacingFormatParser : BasePdfFormatParser
        {
            private static readonly HashSet<string> _validCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Sen", "Hom", "V1", "V2", "Dam", "Esp G", "Esp F", "V3", "A2", "A1", "A3", "V4"
            };

            private Dictionary<string, int> _columnPositions;
            private bool _headerParsed = false;

            public override bool CanParse(string pdfText, RaceMetadata metadata)
            {
                var lower = pdfText.ToLowerInvariant();

                // STRONG indicators for Global Pacing format
                bool hasStrongIndicators = lower.Contains("global pacing") ||
                                          lower.Contains("globalpacing") ||
                                          lower.Contains("www.globalpacing");

                if (hasStrongIndicators)
                    return true;

                // MEDIUM indicators - typical Global Pacing column combination
                // GlobalPacing uses "Clas.Sexe" and "Clas.Cat" which are very specific
                bool hasMediumIndicators = (lower.Contains("clas.sexe") && lower.Contains("clas.cat")) ||
                                          (lower.Contains("clas.sexe") && lower.Contains("pl.")) ||
                                          (lower.Contains("clas.cat") && lower.Contains("pl."));

                if (hasMediumIndicators)
                    return true;

                // Check filename pattern for Global Pacing (Classement-XXkm-RaceName)
                var fileName = metadata?.RaceName?.ToLowerInvariant() ?? "";
                if (fileName.StartsWith("classement") && fileName.Contains("km"))
                {
                    // GlobalPacing often uses this pattern
                    return lower.Contains("pl.") && lower.Contains("nom");
                }

                return false;
            }

            public override string GetFormatName() => "Global Pacing Format";

            public override ParsedPdfResult ParseLine(string line, List<Member> members)
            {
                var result = new ParsedPdfResult();

                // Extract position (required)
                var posMatch = Regex.Match(line, PositionPattern);
                if (!posMatch.Success || !int.TryParse(posMatch.Groups[1].Value, out int position))
                    return null;

                result.Position = position;
                var workingLine = line.Substring(posMatch.Length).Trim();
                var nameSource = workingLine;

                // Global Pacing format check: "LASTNAME, Firstname" - handle BEFORE any cleaning
                // Look for comma pattern early to preserve the name format
                bool isCommaFormat = workingLine.Contains(",");
                string rawFirstName = null;
                string rawLastName = null;

                if (isCommaFormat)
                {
                    // Try to extract comma-separated name before removing times/speeds
                    var commaIndex = workingLine.IndexOf(',');
                    if (commaIndex > 0)
                    {
                        // Extract everything before comma as last name (may include bib number)
                        var beforeComma = workingLine.Substring(0, commaIndex).Trim();

                        // Remove leading bib number if present (1-4 digits at start)
                        var bibPattern = @"^\d{1,4}\s+";
                        beforeComma = Regex.Replace(beforeComma, bibPattern, "").Trim();

                        rawLastName = beforeComma;

                        // Look for the end of the first name (after comma, before next field)
                        var afterComma = workingLine.Substring(commaIndex + 1).TrimStart();

                        // Find where name ends - stop at:
                        // - Time pattern (HH:MM:SS or MM:SS)
                        // - Number (likely position, bib, or other field)
                        // - Two or more spaces (column separator)
                        var nameEndMatch = Regex.Match(afterComma, @"^([A-Za-zÀ-ÿ\s\-']+?)(?=\s{2,}|\d{1,2}:\d{2}|\s+\d+\s+|$)");
                        if (nameEndMatch.Success)
                        {
                            rawFirstName = nameEndMatch.Groups[1].Value.Trim();
                        }
                        else
                        {
                            // Fallback: take everything up to first digit or time
                            var fallbackMatch = Regex.Match(afterComma, @"^([A-Za-zÀ-ÿ\s\-']+)");
                            if (fallbackMatch.Success)
                            {
                                rawFirstName = fallbackMatch.Groups[1].Value.Trim();
                            }
                        }

                        System.Diagnostics.Debug.WriteLine($"GlobalPacing comma format detected - LastName: '{rawLastName}', FirstName: '{rawFirstName}'");
                    }
                }

                // Extract times first
                var timeMatches = Regex.Matches(workingLine, TimePattern);
                foreach (Match tm in timeMatches)
                {
                    var parsed = ParseTime(tm.Value);
                    if (parsed.HasValue)
                    {
                        if (!result.RaceTime.HasValue && parsed.Value.TotalMinutes > RaceTimeThresholdMinutes)
                        {
                            result.RaceTime = parsed.Value;
                        }
                        else if (!result.TimePerKm.HasValue && parsed.Value.TotalMinutes < RaceTimeThresholdMinutes)
                        {
                            result.TimePerKm = parsed.Value;
                        }

                        workingLine = workingLine.Replace(tm.Value, " ").Trim();
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
                        workingLine = workingLine.Replace(speedMatch.Value, " ").Trim();
                    }
                }

                // Extract team from parentheses/brackets
                var teamMatch = Regex.Match(workingLine, @"\((.*?)\)|\[(.*?)\]");
                if (teamMatch.Success)
                {
                    result.Team = !string.IsNullOrEmpty(teamMatch.Groups[1].Value)
                        ? teamMatch.Groups[1].Value
                        : teamMatch.Groups[2].Value;
                    workingLine = workingLine.Replace(teamMatch.Value, " ").Trim();
                }

                // Try to extract explicit club/team tokens
                if (string.IsNullOrWhiteSpace(result.Team))
                {
                    var clubRegex = new Regex(@"\b(?:club|équipe|equipe|team)[:\s\-]*([A-Za-zÀ-ÿ0-9\-\&\s]{2,60})", RegexOptions.IgnoreCase);
                    var clubMatch = clubRegex.Match(workingLine);
                    if (clubMatch.Success)
                    {
                        var teamText = clubMatch.Groups[1].Value.Trim().TrimEnd('.', ',', ';', ':');
                        if (!string.IsNullOrWhiteSpace(teamText))
                        {
                            result.Team = teamText;
                            workingLine = workingLine.Replace(clubMatch.Value, " ").Trim();
                        }
                    }
                }

                // Extract category info - GlobalPacing specific pattern handling
                // Handle patterns like "Dam 8", "Hom 7", "A2 7" where category and position are adjacent
                var categoryPositionPattern = @"\b(HOM|DAM|SEN|V[1-4]|A[1-3]|D[1-3]|ESP[HFGD]?|ES[HFGD])\s+(\d{1,3})\b";
                var categoryMatch = Regex.Match(workingLine, categoryPositionPattern, RegexOptions.IgnoreCase);

                if (categoryMatch.Success && result.AgeCategory == null)
                {
                    result.AgeCategory = categoryMatch.Groups[1].Value.ToUpperInvariant();
                    if (int.TryParse(categoryMatch.Groups[2].Value, out int catPos))
                    {
                        result.PositionByCategory = catPos;
                    }
                    // Remove the matched pattern from working line
                    workingLine = workingLine.Replace(categoryMatch.Value, " ").Trim();

                    System.Diagnostics.Debug.WriteLine($"GlobalPacing: Extracted category '{result.AgeCategory}' with position {result.PositionByCategory} from pattern");
                }

                // Standard category extraction as fallback
                ExtractCategoryFromText(workingLine, result);

                // Set full name - use raw extracted names if we found comma format
                if (!string.IsNullOrWhiteSpace(rawFirstName) && !string.IsNullOrWhiteSpace(rawLastName))
                {
                    // Validate that names are not just numbers
                    bool firstNameIsNumber = rawFirstName.All(c => char.IsDigit(c) || char.IsWhiteSpace(c));
                    bool lastNameIsNumber = rawLastName.All(c => char.IsDigit(c) || char.IsWhiteSpace(c));

                    if (!firstNameIsNumber && !lastNameIsNumber)
                    {
                        result.FirstName = rawFirstName;
                        result.LastName = rawLastName;
                        result.FullName = $"{rawFirstName} {rawLastName}";
                        result.IsMember = false;

                        // Try to match member
                        var matchedMember = FindMatchingMember(members, result.FullName);
                        if (matchedMember != null)
                        {
                            result.FirstName = matchedMember.FirstName;
                            result.LastName = matchedMember.LastName;
                            result.IsMember = true;
                        }

                        return result.Position.HasValue && !string.IsNullOrWhiteSpace(result.FullName) ? result : null;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"GlobalPacing: Rejected numeric name - FirstName: '{rawFirstName}', LastName: '{rawLastName}'");
                    }
                }

                // Fallback: Standard name extraction if comma format wasn't found
                result.FullName = CleanExtractedName(workingLine);

                if (string.IsNullOrWhiteSpace(result.FullName))
                {
                    result.FullName = CleanExtractedName(nameSource);
                }

                if (string.IsNullOrWhiteSpace(result.FullName))
                    result.FullName = nameSource;

                // Check for comma in cleaned name as last resort
                if (result.FullName.Contains(","))
                {
                    var commaParts = result.FullName.Split(new[] { ',' }, 2);
                    if (commaParts.Length == 2)
                    {
                        result.LastName = commaParts[0].Trim();
                        result.FirstName = commaParts[1].Trim();
                        result.FullName = $"{result.FirstName} {result.LastName}";
                    }
                }

                // Standard name processing
                var member = FindMatchingMember(members, result.FullName);
                if (member != null)
                {
                    result.FirstName = member.FirstName;
                    result.LastName = member.LastName;
                    result.IsMember = true;
                }
                else
                {
                    // Only extract name parts if we don't already have them
                    if (string.IsNullOrWhiteSpace(result.FirstName) || string.IsNullOrWhiteSpace(result.LastName))
                    {
                        var nameParts = ExtractNameParts(result.FullName);
                        result.FirstName = nameParts.firstName;
                        result.LastName = nameParts.lastName;
                    }
                    result.IsMember = false;
                }

                // Final validation: reject if firstName or lastName are just numbers
                if (!string.IsNullOrWhiteSpace(result.FirstName) && result.FirstName.All(c => char.IsDigit(c) || char.IsWhiteSpace(c)))
                {
                    System.Diagnostics.Debug.WriteLine($"GlobalPacing: Rejected result with numeric FirstName: '{result.FirstName}' at position {result.Position}");
                    return null;
                }
                if (!string.IsNullOrWhiteSpace(result.LastName) && result.LastName.All(c => char.IsDigit(c) || char.IsWhiteSpace(c)))
                {
                    System.Diagnostics.Debug.WriteLine($"GlobalPacing: Rejected result with numeric LastName: '{result.LastName}' at position {result.Position}");
                    return null;
                }

                return result.Position.HasValue && !string.IsNullOrWhiteSpace(result.FullName) ? result : null;
            }

            private bool IsHeaderRow(string line)
            {
                var lower = line.ToLowerInvariant();
                return (lower.Contains("pl.") && lower.Contains("nom")) || 
                       (lower.Contains("clas.sexe") && lower.Contains("clas.cat"));
            }

            private Dictionary<string, int> DetectColumnPositions(string headerLine)
            {
                var positions = new Dictionary<string, int>();
                var lowerHeader = headerLine.ToLowerInvariant();

                var columnMappings = new Dictionary<string, string[]>
                {
                    { "position", new[] { "pl.", "pl ", "place" } },
                    { "bib", new[] { "dos", "dossard" } },
                    { "name", new[] { " nom", "nom " } },
                    { "sex", new[] { "sexe" } },
                    { "positionsex", new[] { "clas.sexe", "clas. sexe", "cl.sexe", "clas sexe" } },
                    { "category", new[] { " cat", "catégorie" } },
                    { "positioncat", new[] { "clas.cat", "clas. cat", "cl.cat", "clas cat" } },
                    { "team", new[] { "club", "équipe" } },
                    { "speed", new[] { "vitesse" } },
                    { "pace", new[] { "min/km", "allure" } },
                    { "time", new[] { "temps" } },
                    { "points", new[] { "points" } }
                };

                foreach (var mapping in columnMappings)
                {
                    foreach (var keyword in mapping.Value)
                    {
                        var index = lowerHeader.IndexOf(keyword);
                        if (index >= 0)
                        {
                            // For better matching with short keywords
                            bool validMatch = true;
                            if (keyword.Length <= 4 && index > 0)
                            {
                                var prevChar = lowerHeader[index - 1];
                                if (char.IsLetter(prevChar))
                                {
                                    validMatch = false;
                                }
                            }

                            if (validMatch && !positions.ContainsKey(mapping.Key))
                            {
                                positions[mapping.Key] = index;
                                break;
                            }
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"GlobalPacing: Detected {positions.Count} columns");
                foreach (var pos in positions.OrderBy(p => p.Value))
                {
                    System.Diagnostics.Debug.WriteLine($"  {pos.Key}: {pos.Value}");
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
                        var posText = ExtractColumnValue(line, "position");
                        if (!string.IsNullOrWhiteSpace(posText) && int.TryParse(posText.TrimEnd('.', ','), out int position))
                            result.Position = position;
                        else
                            return null;
                    }

                    // Extract name (required) - Format: "LASTNAME, Firstname"
                    if (_columnPositions.ContainsKey("name"))
                    {
                        var nameText = ExtractColumnValue(line, "name");
                        if (string.IsNullOrWhiteSpace(nameText))
                            return null;

                        result.FullName = nameText;

                        // Parse "LASTNAME, Firstname" format
                        if (nameText.Contains(","))
                        {
                            var commaIndex = nameText.IndexOf(',');
                            result.LastName = nameText.Substring(0, commaIndex).Trim();

                            // Extract first name - everything after comma up to next delimiter
                            var afterComma = nameText.Substring(commaIndex + 1).TrimStart();

                            // Remove any trailing numbers/fields that might have leaked in
                            var firstNameMatch = Regex.Match(afterComma, @"^([A-Za-zÀ-ÿ\s\-']+)");
                            if (firstNameMatch.Success)
                            {
                                result.FirstName = firstNameMatch.Groups[1].Value.Trim();
                            }
                            else
                            {
                                result.FirstName = afterComma.Trim();
                            }

                            // Update FullName to be "FirstName LastName" format
                            result.FullName = $"{result.FirstName} {result.LastName}";

                            System.Diagnostics.Debug.WriteLine($"GlobalPacing (columns) parsed name - LastName: '{result.LastName}', FirstName: '{result.FirstName}'");
                        }
                        else
                        {
                            // Fallback: use name parts extraction
                            var nameParts = ExtractNameParts(nameText);
                            result.FirstName = nameParts.firstName;
                            result.LastName = nameParts.lastName;
                        }
                    }
                    else
                    {
                        return null;
                    }

                    // Extract sex
                    if (_columnPositions.ContainsKey("sex"))
                    {
                        var sexText = ExtractColumnValue(line, "sex")?.Trim().ToUpperInvariant();
                        if (!string.IsNullOrEmpty(sexText))
                        {
                            if (sexText == "M" || sexText == "H")
                                result.Sex = "M";
                            else if (sexText == "F" || sexText == "D")
                                result.Sex = "F";
                        }
                    }

                    // Extract position by sex
                    if (_columnPositions.ContainsKey("positionsex"))
                    {
                        var posSexText = ExtractColumnValue(line, "positionsex");
                        if (int.TryParse(posSexText, out int posSex))
                            result.PositionBySex = posSex;
                    }

                    // Extract category
                    if (_columnPositions.ContainsKey("category"))
                    {
                        var catText = ExtractColumnValue(line, "category")?.Trim();
                        if (!string.IsNullOrWhiteSpace(catText))
                        {
                            // Accept category as-is (valid categories list is for reference only)
                            result.AgeCategory = catText;
                        }
                    }

                    // Extract position by category
                    if (_columnPositions.ContainsKey("positioncat"))
                    {
                        var posCatText = ExtractColumnValue(line, "positioncat");
                        if (int.TryParse(posCatText, out int posCat))
                            result.PositionByCategory = posCat;
                    }

                    // Extract team
                    if (_columnPositions.ContainsKey("team"))
                    {
                        result.Team = ExtractColumnValue(line, "team");
                    }

                    // Extract race time
                    if (_columnPositions.ContainsKey("time"))
                    {
                        var timeText = ExtractColumnValue(line, "time");
                        var parsedTime = ParseTime(timeText);
                        if (parsedTime.HasValue)
                        {
                            // Accept any time - filter later if needed
                            if (parsedTime.Value.TotalMinutes > RaceTimeThresholdMinutes)
                                result.RaceTime = parsedTime.Value;
                            else if (parsedTime.Value.TotalMinutes > 0)
                                result.TimePerKm = parsedTime.Value; // Might be pace
                        }
                    }

                    // Extract speed
                    if (_columnPositions.ContainsKey("speed"))
                    {
                        var speedText = ExtractColumnValue(line, "speed");
                        var parsedSpeed = ParseSpeed(speedText);
                        if (parsedSpeed.HasValue)
                            result.Speed = parsedSpeed.Value;
                    }

                    // Extract pace (min/km)
                    if (_columnPositions.ContainsKey("pace"))
                    {
                        var paceText = ExtractColumnValue(line, "pace");
                        var parsedPace = ParseTime(paceText);
                        if (parsedPace.HasValue && parsedPace.Value.TotalMinutes < RaceTimeThresholdMinutes)
                            result.TimePerKm = parsedPace.Value;
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
                        result.IsMember = false;
                    }

                    // Final validation: reject if firstName or lastName are just numbers
                    if (!string.IsNullOrWhiteSpace(result.FirstName) && result.FirstName.All(c => char.IsDigit(c) || char.IsWhiteSpace(c)))
                    {
                        System.Diagnostics.Debug.WriteLine($"GlobalPacing (columns): Rejected result with numeric FirstName: '{result.FirstName}' at position {result.Position}");
                        return null;
                    }
                    if (!string.IsNullOrWhiteSpace(result.LastName) && result.LastName.All(c => char.IsDigit(c) || char.IsWhiteSpace(c)))
                    {
                        System.Diagnostics.Debug.WriteLine($"GlobalPacing (columns): Rejected result with numeric LastName: '{result.LastName}' at position {result.Position}");
                        return null;
                    }

                    return result.Position.HasValue && !string.IsNullOrWhiteSpace(result.FullName) ? result : null;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"GlobalPacing parser error: {ex.Message}");
                    return null;
                }
            }

            private string ExtractColumnValue(string line, string columnKey)
            {
                if (!_columnPositions.ContainsKey(columnKey))
                    return null;

                var startPos = _columnPositions[columnKey];
                var endPos = GetNextColumnPosition(startPos);

                if (startPos >= line.Length)
                    return string.Empty;

                if (endPos == int.MaxValue || endPos > line.Length)
                    return line.Substring(startPos).Trim();
                else
                {
                    var length = Math.Min(endPos - startPos, line.Length - startPos);
                    return line.Substring(startPos, length).Trim();
                }
            }

            private int GetNextColumnPosition(int currentPosition)
            {
                var nextPositions = _columnPositions.Values.Where(p => p > currentPosition).OrderBy(p => p);
                return nextPositions.Any() ? nextPositions.First() : int.MaxValue;
            }
        }

        // ===== CHALLENGE LA MEUSE FORMAT PARSER =====
        // Specific parser for "Challenge La Meuse" PDFs (e.g., "La Zatopek en Famille")
        // Columns: Pos. | Nom | Dos. | Temps | Vitesse | Allure | Club | Catégorie | P.Ca | D.Cha
        private class ChallengeLaMeuseFormatParser : BasePdfFormatParser
        {
            // Canonical category mapping (normalized -> canonical exact label) used ONLY by this parser
            private static readonly Dictionary<string, string> _canonicalCategories = CreateCanonicalCategoryMap();

            private static Dictionary<string, string> CreateCanonicalCategoryMap()
            {
                var list = new[]
                {
                    "Séniors",
                    "Vétérans 1",
                    "Espoirs Garçons",
                    "Vétérans 2",
                    "Espoirs Filles",
                    "Ainées 2",
                    "Vétérans 3",
                    "Dames",
                    "Ainées 1",
                    "Ainées 3",
                    "Vétérans 4",
                    "Ainées 4"
                };

                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var c in list)
                {
                    var norm = NormalizeCategoryToken(c);
                    if (!dict.ContainsKey(norm))
                        dict[norm] = c;
                }

                return dict;
            }

            private static string MapToCanonicalCategoryLocal(string token)
            {
                if (string.IsNullOrWhiteSpace(token)) return null;
                var norm = NormalizeCategoryToken(token);
                if (_canonicalCategories.TryGetValue(norm, out var canon))
                    return canon;
                return null;
            }

            private static void ResolveCanonicalCategoryFromLineLocal(string line, ParsedPdfResult result)
            {
                if (string.IsNullOrWhiteSpace(line) || result == null)
                    return;

                // Try to find any canonical label in the normalized line and set exact label
                var normalizedLine = NormalizeCategoryToken(line);
                foreach (var kv in _canonicalCategories)
                {
                    var canonNorm = kv.Key;
                    var pattern = $"\\b{Regex.Escape(canonNorm)}\\b\\s*(\\d{{1,3}})?";
                    var match = Regex.Match(normalizedLine, pattern, RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        result.AgeCategory = kv.Value; // exact canonical label
                        if (match.Groups.Count > 1 && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
                        {
                            if (int.TryParse(match.Groups[1].Value, out int posCat2))
                                result.PositionByCategory = posCat2;
                        }
                        return;
                    }
                }
            }
            public override bool CanParse(string pdfText, RaceMetadata metadata)
            {
                var lower = pdfText.ToLowerInvariant();
                var fileName = metadata?.RaceName?.ToLowerInvariant() ?? string.Empty;

                // Detect by presence of Zatopek keyword or typical abbreviated header labels
                bool hasZatopekKeyword = lower.Contains("zatopek") || fileName.Contains("zatopek");
                bool hasPCaHeader = lower.Contains("p.ca") || lower.Contains("p ca");

                if (!hasZatopekKeyword && !hasPCaHeader)
                    return false;

                // Verify minimum required columns are present
                var requiredColumns = new[] { "pos", "nom", "temps", "catégorie" };
                int foundColumns = requiredColumns.Count(col => lower.Contains(col));

                return foundColumns >= 3; // At least 3 out of 4 required columns
            }

            public override string GetFormatName() => "Zatopek Format";

            public override ParsedPdfResult ParseLine(string line, List<Member> members)
            {
                var result = new ParsedPdfResult();

                // Position at start required
                var posMatch = Regex.Match(line, PositionPattern);
                if (!posMatch.Success || !int.TryParse(posMatch.Groups[1].Value, out int position))
                    return null;

                result.Position = position;
                var workingLine = line.Substring(posMatch.Length).Trim();
                var nameSource = workingLine;

                // Extract times first
                var timeMatches = Regex.Matches(workingLine, TimePattern);
                foreach (Match tm in timeMatches)
                {
                    var parsed = ParseTime(tm.Value);
                    if (parsed.HasValue)
                    {
                        if (!result.RaceTime.HasValue && parsed.Value.TotalMinutes > RaceTimeThresholdMinutes)
                        {
                            result.RaceTime = parsed.Value;
                        }
                        else if (!result.TimePerKm.HasValue && parsed.Value.TotalMinutes < RaceTimeThresholdMinutes)
                        {
                            result.TimePerKm = parsed.Value;
                        }

                        // Remove the matched time to simplify remaining parsing
                        workingLine = workingLine.Replace(tm.Value, " ").Trim();
                    }
                }
                // Extract speed if present
                var speedMatch = Regex.Match(workingLine, SpeedPattern);
                if (speedMatch.Success)
                {
                    var parsedSpeed = ParseSpeed(speedMatch.Groups[1].Value);
                    if (parsedSpeed.HasValue)
                    {
                        result.Speed = parsedSpeed.Value;
                        workingLine = workingLine.Replace(speedMatch.Value, " ").Trim();
                    }
                }

                // Extract team from parentheses/brackets if present
                var teamMatch = Regex.Match(workingLine, @"\((.*?)\)|\[(.*?)\]");
                if (teamMatch.Success)
                {
                    result.Team = !string.IsNullOrEmpty(teamMatch.Groups[1].Value)
                        ? teamMatch.Groups[1].Value
                        : teamMatch.Groups[2].Value;
                    workingLine = workingLine.Replace(teamMatch.Value, " ").Trim();
                }

                // Try to extract explicit club/team tokens (e.g. "Club: XYZ", "Equipe - ABC", "Team: ABC")
                if (string.IsNullOrWhiteSpace(result.Team))
                {
                    var clubRegex = new Regex(@"\b(?:club|équipe|equipe|team|soci[eé]t[eé])[:\s\-]*([A-Za-zÀ-ÿ0-9\-\&\s]{2,60})", RegexOptions.IgnoreCase);
                    var clubMatch = clubRegex.Match(workingLine);
                    if (clubMatch.Success)
                    {
                        var teamText = clubMatch.Groups[1].Value.Trim().TrimEnd('.', ',', ';', ':');
                        if (!string.IsNullOrWhiteSpace(teamText))
                        {
                            result.Team = teamText;
                            workingLine = workingLine.Replace(clubMatch.Value, " ").Trim();
                        }
                    }
                }

                // Fallback: if there are well-separated columns (multiple 2+ spaces), last column is often the club
                if (string.IsNullOrWhiteSpace(result.Team))
                {
                    var cols = Regex.Split(workingLine, @"\s{2,}")
                                    .Select(p => p.Trim())
                                    .Where(p => !string.IsNullOrEmpty(p))
                                    .ToArray();
                    if (cols.Length > 1)
                    {
                        var lastCol = cols.Last();
                        // Heuristic: club contains letters and is not just a small token
                        if (lastCol.Any(char.IsLetter) && lastCol.Length > 2)
                        {
                            result.Team = lastCol.Trim().TrimEnd('.', ',', ';', ':');
                            // remove lastCol from workingLine
                            var idx = workingLine.LastIndexOf(lastCol, StringComparison.Ordinal);
                            if (idx >= 0)
                                workingLine = workingLine.Substring(0, idx).Trim();
                        }
                    }
                }

                // Try explicit P.Ca pattern (e.g., "P.Ca 12" or "P.Ca:12")
                var pcaMatch = Regex.Match(workingLine, @"\bP\.?\s*\.??\s*ca\.?\s*[:\-]?\s*(\d{1,3})\b", RegexOptions.IgnoreCase);
                if (pcaMatch.Success && int.TryParse(pcaMatch.Groups[1].Value, out int posCat))
                {
                    result.PositionByCategory = posCat;
                    // remove the matched token
                    workingLine = workingLine.Replace(pcaMatch.Value, " ").Trim();
                }

                // If category exists but PositionByCategory not yet set, try to find a number immediately after category token
                if (!result.PositionByCategory.HasValue && !string.IsNullOrWhiteSpace(result.AgeCategory))
                {
                    var afterCatPattern = Regex.Escape(result.AgeCategory) + @"\s+(\d{1,3})\b";
                    var afterCatMatch = Regex.Match(workingLine, afterCatPattern, RegexOptions.IgnoreCase);
                    if (afterCatMatch.Success && int.TryParse(afterCatMatch.Groups[1].Value, out int posCatAdj))
                    {
                        result.PositionByCategory = posCatAdj;
                        workingLine = workingLine.Replace(afterCatMatch.Value, " ").Trim();
                    }
                }

                // Also accept alternative forms like "P Ca" or "P/Ca" without dots (already partially handled,
                // but add a broader check to capture loose usages)
                if (!result.PositionByCategory.HasValue)
                {
                    var pcaLoose = Regex.Match(workingLine, @"\bP\s*[/\\\.]?\s*Ca\s*[:\-]?\s*(\d{1,3})\b", RegexOptions.IgnoreCase);
                    if (pcaLoose.Success && int.TryParse(pcaLoose.Groups[1].Value, out int posCat2))
                    {
                        result.PositionByCategory = posCat2;
                        workingLine = workingLine.Replace(pcaLoose.Value, " ").Trim();
                    }
                }

                // Try to extract explicit category label like "Catégorie: SH" or "Cat: V1"
                if (string.IsNullOrWhiteSpace(result.AgeCategory))
                {
                    var catRegex = new Regex(@"\bcat(?:egor?ie)?[:\s\-]*([A-Za-zÀ-ÿ0-9\s\-]{1,20})\b", RegexOptions.IgnoreCase);
                    var catMatch = catRegex.Match(workingLine);
                    if (catMatch.Success)
                    {
                        var catText = catMatch.Groups[1].Value.Trim().TrimEnd('.', ',', ';', ':');
                        if (!string.IsNullOrWhiteSpace(catText) && Regex.IsMatch(catText, @"^[A-Za-zÀ-ÿ0-9\s\-]{1,20}$"))
                        {
                            result.AgeCategory = catText;
                            workingLine = workingLine.Replace(catMatch.Value, " ").Trim();
                        }
                    }
                }

                // Use CANONICAL category extraction - this is specific to ChallengeLaMeuse format
                // Try to find canonical categories in the remaining text
                ResolveCanonicalCategoryFromLineLocal(workingLine, result);

                // After removals, what's left should be the participant name (possibly with category markers)
                result.FullName = CleanExtractedName(workingLine);

                if (string.IsNullOrWhiteSpace(result.FullName))
                {
                    // fallback to name source cleaned
                    result.FullName = CleanExtractedName(nameSource);
                }

                // Final fallbacks
                if (string.IsNullOrWhiteSpace(result.FullName))
                    result.FullName = nameSource;

                // Member matching / name splitting
                var matched = FindMatchingMember(members, result.FullName);
                if (matched != null)
                {
                    result.FirstName = matched.FirstName;
                    result.LastName = matched.LastName;
                    result.IsMember = true;
                }
                else
                {
                    var np = ExtractNameParts(result.FullName);
                    result.FirstName = np.firstName;
                    result.LastName = np.lastName;
                    result.IsMember = false;
                }

                // Validate that we have valid names (not just numbers)
                if (!IsValidName(result.FirstName) || !IsValidName(result.LastName))
                {
                    System.Diagnostics.Debug.WriteLine($"ChallengeLaMeuse: Rejected result with invalid name - FirstName: '{result.FirstName}', LastName: '{result.LastName}' at position {result.Position}");
                    return null;
                }

                return result.Position.HasValue && !string.IsNullOrWhiteSpace(result.FullName) ? result : null;
            }
        }

        // ===== GOAL TIMING FORMAT PARSER =====
        // Specific parser for Goal Timing system PDFs
        // Columns: Rank | Dos | [empty] | Nom Prenom | Sexe | Club | Cat | Pl/Cat | Temps | T/Km | Vitesse | Points
        private class GoalTimingFormatParser : BasePdfFormatParser
        {
            private static readonly HashSet<string> _validCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "SH", "V1", "V2", "SD", "V3", "ESH", "A2", "A1", "V4", "ESF", "V5", "A3", "A4", "A5"
            };

            private Dictionary<string, int> _columnPositions;
            private bool _headerParsed = false;

            public override bool CanParse(string pdfText, RaceMetadata metadata)
            {
                var lower = pdfText.ToLowerInvariant();

                // STRONG indicators for Goal Timing format
                bool hasStrongIndicators = lower.Contains("goal timing") ||
                                          lower.Contains("goaltiming") ||
                                          lower.Contains("www.goaltiming");

                if (hasStrongIndicators)
                    return true;

                // MEDIUM indicators - typical Goal Timing uses "Rank" instead of "Pl."
                bool hasMediumIndicators = lower.Contains("rank") && 
                                          (lower.Contains("pl/cat") || lower.Contains("t/km"));

                if (hasMediumIndicators)
                    return true;

                // Check filename pattern for Goal Timing (Grand Challenge uses Goal Timing)
                var fileName = metadata?.RaceName?.ToLowerInvariant() ?? "";
                if ((fileName.Contains("gc") || fileName.Contains("grand challenge") || 
                     fileName.Contains("seraing") || fileName.Contains("gravier")) &&
                    lower.Contains("rank"))
                {
                    return true;
                }

                return false;
            }

            public override string GetFormatName() => "Goal Timing Format";

            public override ParsedPdfResult ParseLine(string line, List<Member> members)
            {
                var result = new ParsedPdfResult();

                // Extract position (required)
                var posMatch = Regex.Match(line, PositionPattern);
                if (!posMatch.Success || !int.TryParse(posMatch.Groups[1].Value, out int position))
                    return null;

                result.Position = position;
                var workingLine = line.Substring(posMatch.Length).Trim();
                var nameSource = workingLine;

                // Extract times first
                var timeMatches = Regex.Matches(workingLine, TimePattern);
                foreach (Match tm in timeMatches)
                {
                    var parsed = ParseTime(tm.Value);
                    if (parsed.HasValue)
                    {
                        if (!result.RaceTime.HasValue && parsed.Value.TotalMinutes > RaceTimeThresholdMinutes)
                        {
                            result.RaceTime = parsed.Value;
                        }
                        else if (!result.TimePerKm.HasValue && parsed.Value.TotalMinutes < RaceTimeThresholdMinutes)
                        {
                            result.TimePerKm = parsed.Value;
                        }

                        workingLine = workingLine.Replace(tm.Value, " ").Trim();
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
                        workingLine = workingLine.Replace(speedMatch.Value, " ").Trim();
                    }
                }

                // Extract team from parentheses/brackets
                var teamMatch = Regex.Match(workingLine, @"\((.*?)\)|\[(.*?)\]");
                if (teamMatch.Success)
                {
                    result.Team = !string.IsNullOrEmpty(teamMatch.Groups[1].Value)
                        ? teamMatch.Groups[1].Value
                        : teamMatch.Groups[2].Value;
                    workingLine = workingLine.Replace(teamMatch.Value, " ").Trim();
                }

                // Try to extract explicit club/team tokens
                if (string.IsNullOrWhiteSpace(result.Team))
                {
                    var clubRegex = new Regex(@"\b(?:club|équipe|equipe|team)[:\s\-]*([A-Za-zÀ-ÿ0-9\-\&\s]{2,60})", RegexOptions.IgnoreCase);
                    var clubMatch = clubRegex.Match(workingLine);
                    if (clubMatch.Success)
                    {
                        var teamText = clubMatch.Groups[1].Value.Trim().TrimEnd('.', ',', ';', ':');
                        if (!string.IsNullOrWhiteSpace(teamText))
                        {
                            result.Team = teamText;
                            workingLine = workingLine.Replace(clubMatch.Value, " ").Trim();
                        }
                    }
                }

                // Extract category info
                ExtractCategoryFromText(workingLine, result);

                // Set full name
                result.FullName = CleanExtractedName(workingLine);

                if (string.IsNullOrWhiteSpace(result.FullName))
                {
                    result.FullName = CleanExtractedName(nameSource);
                }

                if (string.IsNullOrWhiteSpace(result.FullName))
                    result.FullName = nameSource;

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

                // Validate that we have valid names (not just numbers)
                if (!IsValidName(result.FirstName) || !IsValidName(result.LastName))
                {
                    System.Diagnostics.Debug.WriteLine($"GoalTiming: Rejected result with invalid name - FirstName: '{result.FirstName}', LastName: '{result.LastName}' at position {result.Position}");
                    return null;
                }

                return result.Position.HasValue && !string.IsNullOrWhiteSpace(result.FullName) ? result : null;
            }

            private bool IsHeaderRow(string line)
            {
                var lower = line.ToLowerInvariant();
                return lower.Contains("rank") && (lower.Contains("nom") || lower.Contains("prenom"));
            }

            private Dictionary<string, int> DetectColumnPositions(string headerLine)
            {
                var positions = new Dictionary<string, int>();
                var lowerHeader = headerLine.ToLowerInvariant();

                var columnMappings = new Dictionary<string, string[]>
                {
                    { "position", new[] { "rank", "rang", " pl." } },
                    { "bib", new[] { "dos", "dossard" } },
                    { "name", new[] { "nom prenom", "nom prénom", " nom" } },
                    { "sex", new[] { "sexe" } },
                    { "team", new[] { "club", "équipe" } },
                    { "category", new[] { " cat", "catégorie" } },
                    { "positioncat", new[] { "pl/cat", "pl. cat", "clas.cat" } },
                    { "time", new[] { "temps" } },
                    { "pace", new[] { "t/km", "min/km", "allure" } },
                    { "speed", new[] { "vitesse" } },
                    { "points", new[] { "points" } }
                };

                foreach (var mapping in columnMappings)
                {
                    foreach (var keyword in mapping.Value)
                    {
                        var index = lowerHeader.IndexOf(keyword);
                        if (index >= 0)
                        {
                            // For better matching with short keywords
                            bool validMatch = true;
                            if (keyword.Length <= 4 && index > 0)
                            {
                                var prevChar = lowerHeader[index - 1];
                                if (char.IsLetter(prevChar))
                                {
                                    validMatch = false;
                                }
                            }

                            if (validMatch && !positions.ContainsKey(mapping.Key))
                            {
                                positions[mapping.Key] = index;
                                break;
                            }
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"GoalTiming: Detected {positions.Count} columns");
                foreach (var pos in positions.OrderBy(p => p.Value))
                {
                    System.Diagnostics.Debug.WriteLine($"  {pos.Key}: {pos.Value}");
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
                        var posText = ExtractColumnValue(line, "position");
                        if (!string.IsNullOrWhiteSpace(posText) && int.TryParse(posText.TrimEnd('.', ','), out int position))
                            result.Position = position;
                        else
                            return null;
                    }

                    // Extract name (required) - Format: "LASTNAME Firstname"
                    if (_columnPositions.ContainsKey("name"))
                    {
                        var nameText = ExtractColumnValue(line, "name");
                        if (string.IsNullOrWhiteSpace(nameText))
                            return null;

                        result.FullName = nameText;

                        // Parse "LASTNAME Firstname" format
                        var parts = nameText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2)
                        {
                            // First part is typically LASTNAME (uppercase), rest is firstname
                            if (IsAllCaps(parts[0]))
                            {
                                result.LastName = parts[0];
                                result.FirstName = string.Join(" ", parts.Skip(1));
                            }
                            else
                            {
                                // Fallback: use standard extraction
                                var nameParts = ExtractNameParts(nameText);
                                result.FirstName = nameParts.firstName;
                                result.LastName = nameParts.lastName;
                            }
                        }
                        else
                        {
                            result.FirstName = nameText;
                            result.LastName = nameText;
                        }
                    }
                    else
                    {
                        return null;
                    }

                    // Extract sex (H = Male, F = Female)
                    if (_columnPositions.ContainsKey("sex"))
                    {
                        var sexText = ExtractColumnValue(line, "sex")?.Trim().ToUpperInvariant();
                        if (!string.IsNullOrEmpty(sexText))
                        {
                            if (sexText == "H" || sexText == "M")
                                result.Sex = "M";
                            else if (sexText == "F" || sexText == "D")
                                result.Sex = "F";
                        }
                    }

                    // Extract team
                    if (_columnPositions.ContainsKey("team"))
                    {
                        result.Team = ExtractColumnValue(line, "team");
                    }

                    // Extract category
                    if (_columnPositions.ContainsKey("category"))
                    {
                        var catText = ExtractColumnValue(line, "category")?.Trim();
                        if (!string.IsNullOrWhiteSpace(catText))
                        {
                            // Accept category as-is (valid categories list is for reference only)
                            result.AgeCategory = catText;
                        }
                    }

                    // Extract position by category
                    if (_columnPositions.ContainsKey("positioncat"))
                    {
                        var posCatText = ExtractColumnValue(line, "positioncat");
                        if (int.TryParse(posCatText, out int posCat))
                            result.PositionByCategory = posCat;
                    }

                    // Extract race time
                    if (_columnPositions.ContainsKey("time"))
                    {
                        var timeText = ExtractColumnValue(line, "time");
                        var parsedTime = ParseTime(timeText);
                        if (parsedTime.HasValue)
                        {
                            // Accept any time - filter later if needed
                            if (parsedTime.Value.TotalMinutes > RaceTimeThresholdMinutes)
                                result.RaceTime = parsedTime.Value;
                            else if (parsedTime.Value.TotalMinutes > 0)
                                result.TimePerKm = parsedTime.Value; // Might be pace
                        }
                    }

                    // Extract pace (T/Km)
                    if (_columnPositions.ContainsKey("pace"))
                    {
                        var paceText = ExtractColumnValue(line, "pace");
                        var parsedPace = ParseTime(paceText);
                        if (parsedPace.HasValue && parsedPace.Value.TotalMinutes < RaceTimeThresholdMinutes)
                            result.TimePerKm = parsedPace.Value;
                    }

                    // Extract speed
                    if (_columnPositions.ContainsKey("speed"))
                    {
                        var speedText = ExtractColumnValue(line, "speed");
                        var parsedSpeed = ParseSpeed(speedText);
                        if (parsedSpeed.HasValue)
                            result.Speed = parsedSpeed.Value;
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
                        result.IsMember = false;
                    }

                    return result.Position.HasValue && !string.IsNullOrWhiteSpace(result.FullName) ? result : null;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"GoalTiming parser error: {ex.Message}");
                    return null;
                }
            }

            private string ExtractColumnValue(string line, string columnKey)
            {
                if (!_columnPositions.ContainsKey(columnKey))
                    return null;

                var startPos = _columnPositions[columnKey];
                var endPos = GetNextColumnPosition(startPos);

                if (startPos >= line.Length)
                    return string.Empty;

                if (endPos == int.MaxValue || endPos > line.Length)
                    return line.Substring(startPos).Trim();
                else
                {
                    var length = Math.Min(endPos - startPos, line.Length - startPos);
                    return line.Substring(startPos, length).Trim();
                }
            }

            private int GetNextColumnPosition(int currentPosition)
            {
                var nextPositions = _columnPositions.Values.Where(p => p > currentPosition).OrderBy(p => p);
                return nextPositions.Any() ? nextPositions.First() : int.MaxValue;
            }

            private bool IsAllCaps(string text)
            {
                if (string.IsNullOrWhiteSpace(text))
                    return false;
                if (!text.Any(char.IsLetter))
                    return false;
                return text.Where(char.IsLetter).All(char.IsUpper);
            }
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

            // Deduplicate results by position - keep the most complete entry for each position
            var deduplicatedResults = DeduplicateByPosition(parsedResults);

            // Filter out non-representative results (race time < 10 minutes - likely parsing errors or pace times)
            var filteredResults = FilterNonRepresentativeResults(deduplicatedResults);

            // Add header
            results.Add(0, CreateHeader());

            // Add reference time if found
            var referenceTime = ExtractReferenceTime(pdfText);
            if (referenceTime.HasValue)
            {
                results.Add(1, CreateReferenceEntry(referenceTime.Value));
            }

            // Add all deduplicated parsed results
            int id = 2;
            foreach (var result in filteredResults.OrderBy(r => r.Position ?? int.MaxValue))
            {
                results.Add(id++, result.ToDelimitedString());
            }

            return results;
        }

        private List<ParsedPdfResult> FilterNonRepresentativeResults(List<ParsedPdfResult> results)
        {
            if (results.Count == 0)
                return results;

            const double MinRaceTimeMinutes = 10.0; // Minimum realistic race time is 10 minutes

            var filtered = results
                .Where(r =>
                {
                    // Keep if no race time (might have other valuable data)
                    if (!r.RaceTime.HasValue)
                        return true;

                    // Remove if race time is exactly 00:00:00 (invalid/missing time)
                    if (r.RaceTime.Value.TotalSeconds == 0)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"Filtered out result with zero race time: Position {r.Position}, " +
                            $"Name: {r.FullName}, RaceTime: 00:00:00 (invalid time)");
                        return false;
                    }

                    // Remove if race time is less than 10 minutes (likely parsing error)
                    if (r.RaceTime.Value.TotalMinutes < MinRaceTimeMinutes)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"Filtered out non-representative result: Position {r.Position}, " +
                            $"Name: {r.FullName}, RaceTime: {r.RaceTime.Value:mm\\:ss} " +
                            $"(< {MinRaceTimeMinutes} min threshold)");
                        return false;
                    }

                    return true;
                })
                .ToList();

            var filteredCount = results.Count - filtered.Count;
            if (filteredCount > 0)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Filtered {filteredCount} non-representative results (zero time or < {MinRaceTimeMinutes} minutes) " +
                    $"({results.Count} -> {filtered.Count})");
            }

            return filtered;
        }

        private List<ParsedPdfResult> DeduplicateByPosition(List<ParsedPdfResult> results)
        {
            if (results.Count == 0)
                return results;

            // Group by position and keep the most complete entry for each
            var deduplicated = results
                .Where(r => r.Position.HasValue)
                .GroupBy(r => r.Position.Value)
                .Select(group =>
                {
                    if (group.Count() == 1)
                        return group.First();

                    // Multiple entries with same position - log and merge
                    System.Diagnostics.Debug.WriteLine($"Duplicate position {group.Key} found ({group.Count()} entries) - merging to most complete entry");

                    // Select the most complete entry based on scoring:
                    // - Has RaceTime: +10 points
                    // - Has Speed: +5 points
                    // - Has TimePerKm: +5 points
                    // - Has Team: +3 points
                    // - Has Sex: +2 points
                    // - Has AgeCategory: +2 points
                    // - Has valid name (not "Unknown"): +20 points
                    var bestEntry = group
                        .OrderByDescending(r =>
                        {
                            int score = 0;
                            if (r.RaceTime.HasValue) score += 10;
                            if (r.Speed.HasValue) score += 5;
                            if (r.TimePerKm.HasValue) score += 5;
                            if (!string.IsNullOrEmpty(r.Team)) score += 3;
                            if (!string.IsNullOrEmpty(r.Sex)) score += 2;
                            if (!string.IsNullOrEmpty(r.AgeCategory)) score += 2;
                            if (!string.IsNullOrEmpty(r.FirstName) && r.FirstName != "Unknown") score += 20;
                            if (!string.IsNullOrEmpty(r.LastName) && r.LastName != "Unknown") score += 20;
                            return score;
                        })
                        .First();

                    System.Diagnostics.Debug.WriteLine($"  Selected entry: {bestEntry.FullName} (score-based selection)");
                    return bestEntry;
                })
                .ToList();

            // Add back results without positions (shouldn't happen but handle gracefully)
            var resultsWithoutPosition = results.Where(r => !r.Position.HasValue).ToList();
            if (resultsWithoutPosition.Any())
            {
                System.Diagnostics.Debug.WriteLine($"Found {resultsWithoutPosition.Count} results without position - keeping all");
                deduplicated.AddRange(resultsWithoutPosition);
            }

            var duplicateCount = results.Count - deduplicated.Count;
            if (duplicateCount > 0)
            {
                System.Diagnostics.Debug.WriteLine($"Deduplication: removed {duplicateCount} duplicate entries ({results.Count} -> {deduplicated.Count})");
            }

            return deduplicated;
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
                        // Try smart table extraction first
                        string pageText = TrySmartTableExtraction(pdfDocument.GetPage(page));

                        // If smart extraction failed or produced poor results, fallback to standard
                        if (string.IsNullOrWhiteSpace(pageText))
                        {
                            ITextExtractionStrategy strategy = new LocationTextExtractionStrategy();
                            pageText = PdfTextExtractor.GetTextFromPage(pdfDocument.GetPage(page), strategy);
                            System.Diagnostics.Debug.WriteLine($"Page {page}: Using standard extraction (smart extraction failed quality checks)");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Page {page}: Using smart table extraction");
                        }

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

        private string TrySmartTableExtraction(iText.Kernel.Pdf.PdfPage page)
        {
            try
            {
                var smartStrategy = new SmartTableExtractionStrategy();
                var extractedText = PdfTextExtractor.GetTextFromPage(page, smartStrategy);

                // Get the reconstructed table-aware text
                var reconstructedText = smartStrategy.GetReconstructedText();

                if (string.IsNullOrWhiteSpace(reconstructedText))
                {
                    return null;
                }

                // Validate the extraction quality against standard extraction
                var standardStrategy = new LocationTextExtractionStrategy();
                var standardText = PdfTextExtractor.GetTextFromPage(page, standardStrategy);

                var smartLines = reconstructedText.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
                var standardLines = standardText.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();

                // Quality checks
                bool qualityChecksPassed = true;

                // 1. Line count shouldn't increase dramatically (max 15% more)
                if (smartLines.Length > standardLines.Length * 1.15)
                {
                    System.Diagnostics.Debug.WriteLine($"Smart extraction rejected: too many lines ({smartLines.Length} vs {standardLines.Length})");
                    qualityChecksPassed = false;
                }

                // 2. Should find table structure (rows with position numbers)
                var rowsWithNumbers = smartLines.Count(l => Regex.IsMatch(l, @"^\s*\d{1,4}\s+"));
                if (rowsWithNumbers < 5 && standardLines.Length > 20)
                {
                    System.Diagnostics.Debug.WriteLine($"Smart extraction rejected: insufficient table rows detected ({rowsWithNumbers})");
                    qualityChecksPassed = false;
                }

                // 3. Total character count shouldn't differ by more than 25%
                var smartCharCount = string.Concat(smartLines).Length;
                var standardCharCount = string.Concat(standardLines).Length;
                if (standardCharCount > 0)
                {
                    var charDiffPercent = Math.Abs(smartCharCount - standardCharCount) / (double)standardCharCount * 100;

                    if (charDiffPercent > 25)
                    {
                        System.Diagnostics.Debug.WriteLine($"Smart extraction rejected: character count difference too high ({charDiffPercent:F1}%)");
                        qualityChecksPassed = false;
                    }
                }

                if (!qualityChecksPassed)
                {
                    return null; // Signal to use standard extraction
                }

                System.Diagnostics.Debug.WriteLine($"Smart extraction passed quality checks: {smartLines.Length} lines, {rowsWithNumbers} data rows");
                return reconstructedText;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Smart table extraction failed: {ex.Message}");
                return null;
            }
        }

        // Smart Table Extraction Strategy - Handles tabular race results with intelligent column detection
        private class SmartTableExtractionStrategy : ITextExtractionStrategy
        {
            private class TextChunk
            {
                public string Text { get; set; }
                public float X { get; set; }
                public float Y { get; set; }
                public float Width { get; set; }
                public float FontSize { get; set; }
            }

            private readonly List<TextChunk> _textChunks = new List<TextChunk>();
            private readonly HashSet<EventType> _supportedEvents = new HashSet<EventType> { EventType.RENDER_TEXT };

            public void EventOccurred(iText.Kernel.Pdf.Canvas.Parser.Data.IEventData data, EventType type)
            {
                if (type == EventType.RENDER_TEXT)
                {
                    var renderInfo = (iText.Kernel.Pdf.Canvas.Parser.Data.TextRenderInfo)data;
                    var text = renderInfo.GetText();

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        var baseline = renderInfo.GetBaseline();
                        var startPoint = baseline.GetStartPoint();
                        var endPoint = baseline.GetEndPoint();
                        var fontSize = renderInfo.GetFontSize();

                        _textChunks.Add(new TextChunk
                        {
                            Text = text.Trim(),
                            X = startPoint.Get(0),
                            Y = startPoint.Get(1),
                            Width = endPoint.Get(0) - startPoint.Get(0),
                            FontSize = fontSize
                        });
                    }
                }
            }

            public ICollection<EventType> GetSupportedEvents()
            {
                return _supportedEvents;
            }

            public string GetResultantText()
            {
                return GetReconstructedText();
            }

            public string GetReconstructedText()
            {
                if (_textChunks.Count == 0)
                    return null;

                // Step 1: Group chunks by Y coordinate (rows) with adaptive tolerance
                var rows = GroupIntoRows(_textChunks);

                // Step 2: Detect column boundaries across all rows
                var columnBoundaries = DetectColumnBoundaries(rows);

                // Step 3: Build text with proper column alignment
                var sb = new StringBuilder();
                int processedRows = 0;

                foreach (var row in rows.OrderByDescending(r => r.Key))
                {
                    var rowChunks = row.Value.OrderBy(c => c.X).ToList();

                    // Skip rows that are too short (likely page numbers or footers)
                    var rowText = string.Join("", rowChunks.Select(c => c.Text));
                    if (rowText.Length < 3)
                        continue;

                    // Build row with column-aware spacing
                    var lineBuilder = new StringBuilder();
                    for (int i = 0; i < rowChunks.Count; i++)
                    {
                        var chunk = rowChunks[i];
                        lineBuilder.Append(chunk.Text);

                        // Add appropriate spacing to next chunk
                        if (i < rowChunks.Count - 1)
                        {
                            var nextChunk = rowChunks[i + 1];
                            var gap = nextChunk.X - (chunk.X + chunk.Width);

                            // Determine if this is a column boundary
                            if (IsColumnBoundary(chunk.X + chunk.Width, nextChunk.X, columnBoundaries))
                            {
                                lineBuilder.Append("  "); // Double space for column separation
                            }
                            else if (gap > 1.0f)
                            {
                                lineBuilder.Append(" "); // Single space for word separation
                            }
                            // else: adjacent text, no space
                        }
                    }

                    sb.AppendLine(lineBuilder.ToString());
                    processedRows++;

                    // Safety limit
                    if (processedRows > 1000)
                    {
                        System.Diagnostics.Debug.WriteLine("Smart extraction exceeded 1000 rows, stopping");
                        break;
                    }
                }

                return sb.ToString();
            }

            private Dictionary<float, List<TextChunk>> GroupIntoRows(List<TextChunk> chunks)
            {
                var rows = new Dictionary<float, List<TextChunk>>();

                // Adaptive Y-tolerance based on median font size
                var medianFontSize = chunks.OrderBy(c => c.FontSize).Skip(chunks.Count / 2).FirstOrDefault()?.FontSize ?? 10f;
                var yTolerance = medianFontSize * 0.3f; // 30% of font size

                foreach (var chunk in chunks)
                {
                    // Find existing row within tolerance
                    var matchingRow = rows.Keys.FirstOrDefault(y => Math.Abs(y - chunk.Y) < yTolerance);

                    if (matchingRow != default(float))
                    {
                        rows[matchingRow].Add(chunk);
                    }
                    else
                    {
                        rows[chunk.Y] = new List<TextChunk> { chunk };
                    }
                }

                return rows;
            }

            private List<float> DetectColumnBoundaries(Dictionary<float, List<TextChunk>> rows)
            {
                var boundaries = new List<float>();

                // Collect all gap positions across rows
                var gapPositions = new List<float>();

                foreach (var row in rows.Values)
                {
                    var sortedChunks = row.OrderBy(c => c.X).ToList();
                    for (int i = 0; i < sortedChunks.Count - 1; i++)
                    {
                        var gap = sortedChunks[i + 1].X - (sortedChunks[i].X + sortedChunks[i].Width);

                        // Significant gaps (> 5 units) might be column boundaries
                        if (gap > 5.0f)
                        {
                            var boundaryPos = sortedChunks[i].X + sortedChunks[i].Width + (gap / 2);
                            gapPositions.Add(boundaryPos);
                        }
                    }
                }

                // Cluster gap positions to find consistent column boundaries
                if (gapPositions.Count > 0)
                {
                    var sortedGaps = gapPositions.OrderBy(g => g).ToList();
                    var clusters = new List<List<float>>();
                    var currentCluster = new List<float> { sortedGaps[0] };

                    for (int i = 1; i < sortedGaps.Count; i++)
                    {
                        if (sortedGaps[i] - sortedGaps[i - 1] < 10.0f) // Within 10 units = same boundary
                        {
                            currentCluster.Add(sortedGaps[i]);
                        }
                        else
                        {
                            if (currentCluster.Count >= 3) // At least 3 occurrences to be valid
                            {
                                boundaries.Add(currentCluster.Average());
                            }
                            currentCluster = new List<float> { sortedGaps[i] };
                        }
                    }

                    // Don't forget last cluster
                    if (currentCluster.Count >= 3)
                    {
                        boundaries.Add(currentCluster.Average());
                    }
                }

                return boundaries;
            }

            private bool IsColumnBoundary(float startX, float endX, List<float> boundaries)
            {
                // Check if any boundary falls between start and end
                const float tolerance = 15.0f;
                return boundaries.Any(b => b > startX - tolerance && b < endX + tolerance);
            }
        }

        private List<ParsedPdfResult> ParsePdfText(string pdfText, List<Member> members)
        {
            var results = new List<ParsedPdfResult>();
            var lines = pdfText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // Try each parser until one successfully detects the format
            IPdfFormatParser selectedParser = null;
            System.Diagnostics.Debug.WriteLine($"=== Testing parsers for: {_raceMetadata?.RaceName ?? "Unknown"} ===");

            foreach (var parser in _formatParsers)
            {
                bool canParse = parser.CanParse(pdfText, _raceMetadata);
                System.Diagnostics.Debug.WriteLine($"  {parser.GetFormatName()}: {(canParse ? "YES" : "no")}");

                if (canParse)
                {
                    selectedParser = parser;
                    break;
                }
            }

            if (selectedParser == null)
            {
                // Fallback to standard parser
                selectedParser = _formatParsers[_formatParsers.Count - 1];
                System.Diagnostics.Debug.WriteLine($"  No parser matched - using fallback: {selectedParser.GetFormatName()}");
            }

            System.Diagnostics.Debug.WriteLine($"=== Selected: {selectedParser.GetFormatName()} ===");

            // Parse all lines with the selected parser
            int lineNumber = 0;
            int successfulParses = 0;
            int skippedHeaders = 0;
            int skippedDsq = 0;
            int failedParses = 0;
            string orphanedTimeLine = null; // Buffer for times on separate lines

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

                // Check if this line is an orphaned time (time + points without position)
                // Pattern: "00:22:26 898" or "00:22:26"
                if (IsOrphanedTimeLine(trimmedLine))
                {
                    orphanedTimeLine = trimmedLine;
                    continue;
                }

                // If we have an orphaned time from previous line, prepend it
                var lineToProcess = trimmedLine;
                if (orphanedTimeLine != null)
                {
                    lineToProcess = orphanedTimeLine + " " + trimmedLine;
                    orphanedTimeLine = null; // Clear buffer
                }

                // Try to parse the line FIRST (parser may need to see headers to detect columns)
                var result = selectedParser.ParseLine(lineToProcess, members);
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

        private bool IsOrphanedTimeLine(string line)
        {
            // Check if line contains ONLY time and possibly points, no position number at start
            var trimmed = line.Trim();

            // If line starts with a number 1-4 digits, it's NOT an orphaned time (it's a position)
            if (trimmed.Length > 0 && char.IsDigit(trimmed[0]))
            {
                var posMatch = Regex.Match(trimmed, @"^(\d{1,4})[\s\.\,]");
                if (posMatch.Success)
                {
                    return false; // This has a position, not orphaned
                }
            }

            // Check if line matches time pattern (with optional points)
            // Pattern: "00:22:26 898" or "00:22:26" or "00:21:37 936"
            var orphanedTimePattern = @"^(\d{1,2}:\d{2}:\d{2}|\d{1,2}:\d{2})\s*(\d+)?$";
            return Regex.IsMatch(trimmed, orphanedTimePattern);
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
                "Catégorie", "P.Ca",
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

                    // Check for multi-word categories FIRST (before single-word)
                    // This handles cases like "Senior H", "Veteran 2", "Moins16 H"
                    if (result.AgeCategory == null && i < parts.Length - 1)
                    {
                        var combined = $"{trimmed} {parts[i + 1]}";

                        // Check if it's a valid multi-word category
                        if (IsValidCategoryPhrase(combined))
                        {
                            // Check if there's a position number after the category
                            string candidate = combined;
                            int? trailingNum = null;

                            if (i + 2 < parts.Length && Regex.IsMatch(parts[i + 2], "^\\d{1,3}$"))
                            {
                                if (int.TryParse(parts[i + 2], out int parsed))
                                {
                                    trailingNum = parsed;
                                    // Don't add the number to the category name, it's the position
                                }
                            }

                            result.AgeCategory = candidate;
                            if (trailingNum.HasValue)
                            {
                                result.PositionByCategory = trailingNum.Value;
                                i += 2; // Skip the sex marker and position number
                            }
                            else
                            {
                                i++; // Skip the sex marker
                            }

                            extractionCount++;
                            System.Diagnostics.Debug.WriteLine($"    Extracted AgeCategory (multi-word): {result.AgeCategory}");
                            if (trailingNum.HasValue)
                                System.Diagnostics.Debug.WriteLine($"    Extracted PositionByCategory: {trailingNum.Value}");

                            continue;
                        }

                        // Check for category + number pattern (e.g., "Veteran 2")
                        // where the number IS part of the category, not the position
                        if (Regex.IsMatch(parts[i + 1], "^\\d{1}$") && IsValidCategoryCode(trimmed))
                        {
                            var catWithNum = combined;

                            // Check if there's ANOTHER number after this (that would be the position)
                            int? positionNum = null;
                            if (i + 2 < parts.Length && Regex.IsMatch(parts[i + 2], "^\\d{1,3}$"))
                            {
                                if (int.TryParse(parts[i + 2], out int parsed))
                                {
                                    positionNum = parsed;
                                }
                            }

                            result.AgeCategory = catWithNum;
                            if (positionNum.HasValue)
                            {
                                result.PositionByCategory = positionNum.Value;
                                i += 2; // Skip number and position
                            }
                            else
                            {
                                i++; // Skip the number
                            }

                            extractionCount++;
                            System.Diagnostics.Debug.WriteLine($"    Extracted AgeCategory (with number): {result.AgeCategory}");
                            if (positionNum.HasValue)
                                System.Diagnostics.Debug.WriteLine($"    Extracted PositionByCategory: {positionNum.Value}");

                            continue;
                        }
                    }

                    // Check for category codes - comprehensive list
                    if (result.AgeCategory == null && IsValidCategoryCode(trimmed))
                    {
                        // Build candidate including possible adjacent number/sex
                        string candidate = trimmed;
                        int? adjacentNumber = null;

                        // Check for pattern: CategoryCode + Sex + Position (e.g., "SH 12")
                        if (i + 1 < parts.Length && Regex.IsMatch(parts[i + 1], "^\\d{1,3}$"))
                        {
                            if (int.TryParse(parts[i + 1], out int parsed))
                            {
                                adjacentNumber = parsed;
                            }
                        }

                        result.AgeCategory = candidate;
                        if (adjacentNumber.HasValue)
                        {
                            result.PositionByCategory = adjacentNumber.Value;
                            i++; // consume number
                        }

                        extractionCount++;
                        System.Diagnostics.Debug.WriteLine($"    Extracted AgeCategory: {result.AgeCategory}");
                        if (adjacentNumber.HasValue)
                            System.Diagnostics.Debug.WriteLine($"    Extracted PositionByCategory: {adjacentNumber.Value}");

                        continue;
                    }
                }

                if (extractionCount > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"  ExtractCategoryFromText: extracted {extractionCount} items from '{originalText}'");
                }
            }

            protected bool IsValidCategoryCode(string code)
            {
                if (string.IsNullOrWhiteSpace(code))
                    return false;

                var upper = code.ToUpperInvariant().Replace("É", "E").Replace("È", "E");

                // Senior categories
                if (Regex.IsMatch(upper, @"^S[HMFD]$")) // SH, SM, SD, SF
                    return true;
                if (Regex.IsMatch(upper, @"^SEN[HFD]?$")) // SEN, SENH, SENF, SEND
                    return true;

                // Generic gender categories
                if (upper == "HOM" || upper == "DAM") // Hommes, Dames
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
                if (Regex.IsMatch(upper, @"^ESP[HFGD]?$")) // ESP, ESPH, ESPF, ESPG, ESPD
                    return true;
                if (Regex.IsMatch(upper, @"^ES[HFGD]$")) // ESH, ESF, ESG, ESD
                    return true;
                if (Regex.IsMatch(upper, @"^JUN[HFD]?$")) // JUN, JUNH, JUNF, JUND
                    return true;
                if (Regex.IsMatch(upper, @"^CAD[HFD]?$")) // CAD, CADH, CADF, CADD
                    return true;
                if (upper == "SCO" || upper == "BEN" || upper == "PUP" || upper == "MIN")
                    return true;

                // Youth categories with Moins prefix (e.g., "Moins16", "Moins14")
                if (Regex.IsMatch(upper, @"^MOINS\d{2}$")) // MOINS16, MOINS14, MOINS12
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

            protected bool IsValidCategoryPhrase(string phrase)
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
                if (Regex.IsMatch(upper, @"^ESPOIR\s+[HFGD]?$")) // "Espoir H", "Espoir F", "Espoir G", "Espoir D"
                    return true;
                if (Regex.IsMatch(upper, @"^ESP\s+[HFGD]$")) // "Esp H", "Esp F", "Esp G", "Esp D"
                    return true;
                if (Regex.IsMatch(upper, @"^JUNIOR\s+[HFD]?$")) // "Junior H", "Junior F", "Junior D"
                    return true;
                if (Regex.IsMatch(upper, @"^CADET\s+[HFD]?$")) // "Cadet H", "Cadet F", "Cadet D"
                    return true;
                if (Regex.IsMatch(upper, @"^MASTER\s+\d{2}\+?$")) // "Master 40+", "Master 45"
                    return true;
                if (Regex.IsMatch(upper, @"^WOMEN\s+\d{2}\+?$")) // "Women 40+", "Women 45"
                    return true;

                // Youth categories with "Moins" prefix and sex marker
                if (Regex.IsMatch(upper, @"^MOINS\d{2}\s+[HFD]$")) // "Moins16 H", "Moins14 D", etc.
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
                    // Skip bib numbers at the start (typically 1-4 digits)
                    if (nameParts.Count == 0 && Regex.IsMatch(part, @"^\d{1,4}$"))
                        continue;

                    // Stop if we hit a standalone number (position/bib)
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

                // Filter out any parts that are just numbers (bib numbers)
                parts = parts.Where(p => !IsJustNumber(p)).ToArray();

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

            private bool IsJustNumber(string text)
            {
                if (string.IsNullOrWhiteSpace(text))
                    return false;

                // Check if the entire string is just digits (possibly with whitespace)
                return text.All(c => char.IsDigit(c) || char.IsWhiteSpace(c));
            }

            protected bool IsValidName(string name)
            {
                if (string.IsNullOrWhiteSpace(name))
                    return false;

                // Name must contain at least one letter
                if (!name.Any(char.IsLetter))
                    return false;

                // Name should not be just a number
                if (IsJustNumber(name))
                    return false;

                return true;
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

                // Don't match if this looks like GlobalPacing format (avoid false positives)
                if (lowerText.Contains("clas.sexe") || lowerText.Contains("clas.cat") || 
                    lowerText.Contains("global pacing") || lowerText.Contains("globalpacing"))
                    return false;

                // Don't match Classement-XXkm pattern (typically GlobalPacing)
                var fileName = metadata?.RaceName?.ToLowerInvariant() ?? "";
                if (fileName.StartsWith("classement") && fileName.Contains("km"))
                    return false;

                // Standard French column format detection
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
                // Must contain "nom" and some form of a position/header marker to be a header
                // Accept various short forms used in different PDFs (pl., pl, p., pos, place, rang, P.Ca etc.)
                var hasName = lowerLine.Contains("nom");
                var positionKeywords = new[] { "pl.", "pl ", "p.", "p ", "pos", "place", "rang", "p.ca", "p ca", "p/ca" };
                var hasPositionKeyword = positionKeywords.Any(k => lowerLine.Contains(k));

                return hasName && hasPositionKeyword;
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
                    // Accept common short forms such as "P.Ca" (position par catégorie) used in some organisers' PDFs
                    { "positioncat", new[] { "pl./c.", "pl./cat.", "pl. cat", "pl.cat", "clas. cat", "clas.cat", "pos.cat", "classement cat", "cl.cat", "pos/cat", "p.ca", "p ca", "p.ca.", "p/ca" } },
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
                    string rawName = null;
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
                        rawName = ExtractColumnValue(line, nameStart, nameEnd);

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

                    // Heuristic: if header did not provide category/positioncat but the line has clear columns,
                    // try to detect category (full-word like "Séniors", "Dames", "Vétérans 3", "Ainées 1")
                    // and the P.Ca which is usually the next column after category.
                    if (!_columnPositions.ContainsKey("category") || !_columnPositions.ContainsKey("positioncat"))
                    {
                        try
                        {
                            var splitCols = Regex.Split(line, @"\s{2,}")
                                                 .Select(p => p.Trim())
                                                 .Where(p => !string.IsNullOrEmpty(p))
                                                 .ToArray();

                            // Find index of name column occurrence (match by approximate equality)
                            int nameIndex = -1;
                            for (int ci = 0; ci < splitCols.Length; ci++)
                            {
                                // if rawName appears within column text
                                if (splitCols[ci].IndexOf(rawName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    CleanExtractedName(splitCols[ci]).Equals(result.FullName, StringComparison.OrdinalIgnoreCase))
                                {
                                    nameIndex = ci;
                                    break;
                                }
                            }

                            if (nameIndex >= 0)
                            {
                                // Candidate category is next column
                                if (nameIndex + 1 < splitCols.Length && string.IsNullOrWhiteSpace(result.AgeCategory))
                                {
                                    var candidateCat = splitCols[nameIndex + 1];
                                    // Normalize and check for known full-word categories
                                    var norm = NormalizeCategoryToken(candidateCat);

                                    // If candidate contains trailing number, extract it
                                    var trailingMatch = Regex.Match(candidateCat, "^(.*?)[\\s\\-]*(\\d{1,3})$", RegexOptions.IgnoreCase);
                                    if (trailingMatch.Success)
                                    {
                                        // Preserve full category including the trailing number (e.g. "Vétérans 1")
                                        result.AgeCategory = candidateCat.Trim();
                                        var num = trailingMatch.Groups[2].Value;
                                        if (int.TryParse(num, out int parsedPosCat))
                                            result.PositionByCategory = parsedPosCat;
                                    }
                                    else
                                    {
                                        // If candidate looks like a base category (no number), check following column for number or gender qualifier
                                        bool matched = false;

                                        if (Regex.IsMatch(norm, @"^(SENIORS?|DAMES?|VETERANS?|VET|AINEES?|ESPOIRS?)$", RegexOptions.IgnoreCase) ||
                                            IsValidCategoryCode(candidateCat) || IsValidCategoryPhrase(candidateCat))
                                        {
                                            // Look ahead for a number (P.Ca) or qualifier like 'Garçons'/'Filles'
                                            if (nameIndex + 2 < splitCols.Length)
                                            {
                                                var nextToken = splitCols[nameIndex + 2];
                                                // If next token is digits -> position by category
                                                var digits = Regex.Match(nextToken, "^(\\d{1,3})$");
                                                if (digits.Success && int.TryParse(digits.Groups[1].Value, out int parsedPosCat2))
                                                {
                                                    result.AgeCategory = candidateCat.Trim();
                                                    result.PositionByCategory = parsedPosCat2;
                                                    matched = true;
                                                }
                                                else
                                                {
                                                    // Maybe combined qualifier (e.g., "Espoirs" + "Garçons")
                                                    var combined = candidateCat + " " + nextToken;
                                                    var normCombined = NormalizeCategoryToken(combined);
                                                    if (Regex.IsMatch(normCombined, @"^(ESPOIRS?\s+(GARCON|GARCONS|GARÇON|GARÇONS|FILLE|FILLES))$", RegexOptions.IgnoreCase) ||
                                                        IsValidCategoryPhrase(combined))
                                                    {
                                                        result.AgeCategory = combined.Trim();
                                                        matched = true;
                                                        // If following token after qualifier is digits, pick it as PositionByCategory
                                                        if (nameIndex + 3 < splitCols.Length)
                                                        {
                                                            var maybeNum = splitCols[nameIndex + 3];
                                                            var m2 = Regex.Match(maybeNum, "^(\\d{1,3})$");
                                                            if (m2.Success && int.TryParse(m2.Groups[1].Value, out int parsedPosCat3))
                                                            {
                                                                result.PositionByCategory = parsedPosCat3;
                                                            }
                                                        }
                                                    }
                                                }
                                            }

                                            if (!matched)
                                            {
                                                // Accept candidateCat as category even without number
                                                result.AgeCategory = candidateCat.Trim().TrimEnd('.', ',', ';', ':');
                                            }
                                        }
                                    }
                                }

                                // If P.Ca is just after category, it's usually the following column
                                if (nameIndex + 2 < splitCols.Length && !result.PositionByCategory.HasValue)
                                {
                                    var candidatePosCat = splitCols[nameIndex + 2];
                                    var digits = Regex.Match(candidatePosCat, @"^(\d{1,3})$");
                                    if (digits.Success && int.TryParse(digits.Groups[1].Value, out int parsedPosCat))
                                    {
                                        result.PositionByCategory = parsedPosCat;
                                    }
                                }

                                // If team not set, last column is often team/club
                                if (string.IsNullOrWhiteSpace(result.Team) && splitCols.Length - 1 > nameIndex)
                                {
                                    var last = splitCols.Last();
                                    if (!string.IsNullOrWhiteSpace(last) && !Regex.IsMatch(last, TimePattern) && !Regex.IsMatch(last, "^\\d+$"))
                                    {
                                        // Treat 'Non communique' (and variants) as missing
                                        if (!Regex.IsMatch(last, @"non\s*communiqu[eé]|non\s*communiqu?", RegexOptions.IgnoreCase))
                                        {
                                            result.Team = last.Trim().TrimEnd('.', ',', ';', ':');
                                        }
                                    }
                                }
                            }
                        }
                        catch { /* non-fatal heuristic */ }
                    }

                    // Extract team (optional)
                    if (_columnPositions.ContainsKey("team"))
                    {
                        var teamStart = _columnPositions["team"];
                        var teamEnd = GetNextColumnPosition(teamStart);
                        result.Team = ExtractColumnValue(line, teamStart, teamEnd);
                        if (!string.IsNullOrWhiteSpace(result.Team))
                        {
                            // Clean common trailing punctuation and markers
                            result.Team = result.Team.Trim().TrimEnd('.', ',', ';', ':');
                        }
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
                            // Clean category text
                            catText = catText.Trim().TrimEnd('.', ',', ';', ':');

                            // If category includes an adjacent number (e.g. "Vétérans 3" or "Ainées 1"),
                            // extract the number as PositionByCategory and keep only the category text
                            var catNumberMatch = Regex.Match(catText, @"^(.*?)[\s\-]*(\d{1,3})$", RegexOptions.IgnoreCase);
                            if (catNumberMatch.Success)
                            {
                                // Preserve the category text as-is (including the number) so we store e.g. "Vétérans 1"
                                result.AgeCategory = catText;
                                var num = catNumberMatch.Groups[2].Value;
                                if (int.TryParse(num, out int parsedPosCat))
                                {
                                    result.PositionByCategory = parsedPosCat;
                                }
                            }
                            else
                            {
                                // No trailing number, but category might still contain digits elsewhere
                                var digitMatch = Regex.Match(catText, @"(\d{1,3})");
                                if (digitMatch.Success)
                                {
                                    // If digits are found inside category text, preserve the full category (including number)
                                    result.AgeCategory = catText;
                                    var num = digitMatch.Groups[1].Value;
                                    if (int.TryParse(num, out int parsedPosCat))
                                    {
                                        result.PositionByCategory = parsedPosCat;
                                    }
                                }
                                else
                                {
                            // Preserve extracted category text as-is for non-LaMeuse parsers
                            result.AgeCategory = catText;
                                }
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
        private class CJPLFormatParser : BasePdfFormatParser
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
        private class ChallengeCondrusienFormatParser : BasePdfFormatParser
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

        // EXPERIMENTAL: Table-aware text extraction strategy (currently disabled)
        /*
        // Table-aware text extraction strategy
        private class TableAwareTextExtractionStrategy : LocationTextExtractionStrategy
        {
            private class TextChunk
            {
                public string Text { get; set; }
                public float X { get; set; }
                public float Y { get; set; }
                public float Width { get; set; }
            }

            private readonly List<TextChunk> _textChunks = new List<TextChunk>();

            public override void EventOccurred(iText.Kernel.Pdf.Canvas.Parser.Data.IEventData data, iText.Kernel.Pdf.Canvas.Parser.EventType type)
            {
                base.EventOccurred(data, type);

                if (type == iText.Kernel.Pdf.Canvas.Parser.EventType.RENDER_TEXT)
                {
                    var renderInfo = (iText.Kernel.Pdf.Canvas.Parser.Data.TextRenderInfo)data;
                    var text = renderInfo.GetText();

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        var baseline = renderInfo.GetBaseline();
                        var startPoint = baseline.GetStartPoint();
                        var endPoint = baseline.GetEndPoint();

                        _textChunks.Add(new TextChunk
                        {
                            Text = text,
                            X = startPoint.Get(0),
                            Y = startPoint.Get(1),
                            Width = endPoint.Get(0) - startPoint.Get(0)
                        });
                    }
                }
            }

            public string GetReconstructedText()
            {
                if (_textChunks.Count == 0)
                    return null;

                // Group chunks by Y coordinate (same row) with strict tolerance
                const float yTolerance = 1.5f; // Stricter tolerance
                var rows = new List<List<TextChunk>>();

                foreach (var chunk in _textChunks.OrderByDescending(c => c.Y))
                {
                    var existingRow = rows.FirstOrDefault(r => 
                        Math.Abs(r[0].Y - chunk.Y) < yTolerance);

                    if (existingRow != null)
                    {
                        existingRow.Add(chunk);
                    }
                    else
                    {
                        rows.Add(new List<TextChunk> { chunk });
                    }
                }

                // Build text from rows
                var sb = new StringBuilder();
                int lineCount = 0;

                foreach (var row in rows)
                {
                    // Sort chunks in row by X coordinate (left to right)
                    var sortedChunks = row.OrderBy(c => c.X).ToList();

                    // Skip rows with too few characters (noise)
                    var rowText = string.Join("", sortedChunks.Select(c => c.Text));
                    if (rowText.Length < 2)
                        continue;

                    // Detect if chunks should be separated or joined
                    for (int i = 0; i < sortedChunks.Count; i++)
                    {
                        var chunk = sortedChunks[i];
                        sb.Append(chunk.Text);

                        // Add space if next chunk is far enough away
                        if (i < sortedChunks.Count - 1)
                        {
                            var nextChunk = sortedChunks[i + 1];
                            var gap = nextChunk.X - (chunk.X + chunk.Width);

                            // If gap is significant, add space(s)
                            if (gap > 2.0f)
                            {
                                // Single space for most gaps
                                sb.Append(' ');
                            }
                        }
                    }

                    sb.AppendLine();
                    lineCount++;

                    // Safety check: if too many lines, something might be wrong
                    if (lineCount > 500)
                    {
                        System.Diagnostics.Debug.WriteLine("Table reconstruction exceeded 500 lines, stopping");
                        break;
                    }
                }

                return sb.ToString();
            }
        }
        */
    }
}
