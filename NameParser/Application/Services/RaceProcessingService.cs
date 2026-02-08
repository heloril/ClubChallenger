using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NameParser.Domain.Aggregates;
using NameParser.Domain.Entities;
using NameParser.Domain.Repositories;
using NameParser.Domain.Services;
using NameParser.Domain.ValueObjects;

namespace NameParser.Application.Services
{
    public class RaceProcessingService
    {
        private readonly IMemberRepository _memberRepository;
        private readonly IRaceResultRepository _raceResultRepository;
        private readonly PointsCalculationService _pointsCalculationService;

        public RaceProcessingService(
            IMemberRepository memberRepository,
            IRaceResultRepository raceResultRepository,
            PointsCalculationService pointsCalculationService)
        {
            _memberRepository = memberRepository;
            _raceResultRepository = raceResultRepository;
            _pointsCalculationService = pointsCalculationService;
        }

        public Classification ProcessAllRaces(IEnumerable<string> raceFiles)
        {
            var classification = new Classification();
            var members = _memberRepository.GetMembersWithLastName();

            foreach (var raceFile in raceFiles)
            {
                ProcessSingleRace(raceFile, members, classification);
            }

            return classification;
        }

        public Classification ProcessRaceWithMembers(IEnumerable<string> raceFiles, List<Member> members)
        {
            var classification = new Classification();

            foreach (var raceFile in raceFiles)
            {
                ProcessSingleRace(raceFile, members, classification);
            }

            return classification;
        }

        public Classification ProcessRaceWithMembers(string raceFile, RaceDistance raceDistance, List<Member> members)
        {
            var classification = new Classification();
            ProcessSingleRaceWithRaceInfo(raceFile, raceDistance, members, classification);
            return classification;
        }

        public Classification ProcessRace(
            string filePath,
            string raceName,
            int raceNumber,
            int? year,
            int distanceKm,
            IRaceResultRepository repository)
        {
            var raceDistance = new RaceDistance(raceNumber, raceName, distanceKm);
            var members = _memberRepository.GetMembersWithLastName();
            var classification = new Classification();

            // Process the race results using the provided repository
            var results = repository.GetRaceResults(filePath, members);

            // Phase 1: Extract all race data and find reference time
            var parsedResults = new List<ParsedRaceResult>();
            TimeSpan referenceTime = TimeSpan.FromSeconds(1);
            bool isTimePerKmRace = false;

            foreach (var result in results.OrderBy(c => c.Key))
            {
                var individualResult = result.Value.Split(';');
                var parsedResult = ParseRaceResult(individualResult, result.Value, members, ref isTimePerKmRace);

                if (parsedResult.IsValid)
                {
                    if (parsedResult.IsReferenceTime)
                    {
                        referenceTime = parsedResult.Time;
                    }
                    parsedResults.Add(parsedResult);
                }
            }

            // Phase 2: Calculate points for all participants now that we have the reference time
            foreach (var parsedResult in parsedResults)
            {
                int points = _pointsCalculationService.CalculatePoints(referenceTime, parsedResult.Time);

                foreach (var member in parsedResult.Members)
                {
                    // Use extracted times if available
                    TimeSpan? finalRaceTime = parsedResult.ExtractedRaceTime ??
                                            (isTimePerKmRace ? null : (TimeSpan?)parsedResult.Time);
                    TimeSpan? finalTimePerKm = parsedResult.ExtractedTimePerKm ??
                                             (isTimePerKmRace ? (TimeSpan?)parsedResult.Time : null);

                    // Store complete race data
                    classification.AddOrUpdateResult(
                        member,
                        raceDistance,
                        points,
                        finalRaceTime,
                        finalTimePerKm,
                        parsedResult.Position,
                        parsedResult.Team,
                        parsedResult.Speed,
                        parsedResult.IsMember,
                        parsedResult.Sex,
                        parsedResult.PositionBySex,
                        parsedResult.AgeCategory,
                        parsedResult.PositionByCategory);
                }
            }

            return classification;
        }

        private void ProcessSingleRace(string raceFile, List<Member> members, Classification classification)
        {
            var raceFileName = new RaceFileName(raceFile);
            var raceDistance = new RaceDistance(raceFileName.RaceNumber, raceFileName.RaceName, raceFileName.DistanceKm);

            ProcessRaceResults(raceFile, raceDistance, members, classification);
        }

        private void ProcessSingleRaceWithRaceInfo(string raceFile, RaceDistance raceDistance, List<Member> members, Classification classification)
        {
            // Use the provided race distance object which has the correct distance from the database/UI
            ProcessRaceResults(raceFile, raceDistance, members, classification);
        }

        private void ProcessRaceResults(string raceFile, RaceDistance raceDistance, List<Member> members, Classification classification)
        {
            var results = _raceResultRepository.GetRaceResults(raceFile, members);

            // Phase 1: Extract all race data and find reference time
            var parsedResults = new List<ParsedRaceResult>();
            TimeSpan referenceTime = TimeSpan.FromSeconds(1);
            bool isTimePerKmRace = false;

            foreach (var result in results.OrderBy(c => c.Key))
            {
                var individualResult = result.Value.Split(';');
                var parsedResult = ParseRaceResult(individualResult, result.Value, members, ref isTimePerKmRace);

                if (parsedResult.IsValid)
                {
                    if (parsedResult.IsReferenceTime)
                    {
                        referenceTime = parsedResult.Time;
                    }
                    parsedResults.Add(parsedResult);
                }
            }

            // Phase 2: Calculate points for all participants now that we have the reference time
            foreach (var parsedResult in parsedResults)
            {
                int points = _pointsCalculationService.CalculatePoints(referenceTime, parsedResult.Time);

                foreach (var member in parsedResult.Members)
                {
                    // Use extracted times if available
                    TimeSpan? finalRaceTime = parsedResult.ExtractedRaceTime ??
                                            (isTimePerKmRace ? null : (TimeSpan?)parsedResult.Time);
                    TimeSpan? finalTimePerKm = parsedResult.ExtractedTimePerKm ??
                                             (isTimePerKmRace ? (TimeSpan?)parsedResult.Time : null);

                    // Store complete race data
                    classification.AddOrUpdateResult(
                        member,
                        raceDistance,
                        points,
                        finalRaceTime,
                        finalTimePerKm,
                        parsedResult.Position,
                        parsedResult.Team,
                        parsedResult.Speed,
                        parsedResult.IsMember,
                        parsedResult.Sex,
                        parsedResult.PositionBySex,
                        parsedResult.AgeCategory,
                        parsedResult.PositionByCategory);

                    // Log for debugging
                    if (parsedResult.Position.HasValue)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"{member.FirstName} {member.LastName} - " +
                            $"Position: {parsedResult.Position}, Time: {parsedResult.Time:hh\\:mm\\:ss}, " +
                            $"Points: {points}, Speed: {parsedResult.Speed:F2} km/h, " +
                            $"Team: {parsedResult.Team ?? "N/A"}, IsMember: {parsedResult.IsMember}, " +
                            $"Sex: {parsedResult.Sex ?? "N/A"}, Cat: {parsedResult.AgeCategory ?? "N/A"}");
                    }
                }
            }
        }

        private ParsedRaceResult ParseRaceResult(string[] individualResult, string resultValue, List<Member> members, ref bool isTimePerKmRace)
        {
            var result = new ParsedRaceResult();

            // Extract metadata
            for (int i = 0; i < individualResult.Length - 1; i++)
            {
                if (individualResult[i].Equals("POS", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(individualResult[i + 1], out int pos))
                    {
                        result.Position = pos;
                    }
                }

                if (individualResult[i].Equals("RACETYPE", StringComparison.OrdinalIgnoreCase))
                {
                    isTimePerKmRace = individualResult[i + 1].Equals("TIME_PER_KM", StringComparison.OrdinalIgnoreCase);
                }

                if (individualResult[i].Equals("TEAM", StringComparison.OrdinalIgnoreCase))
                {
                    result.Team = individualResult[i + 1];
                }

                if (individualResult[i].Equals("SPEED", StringComparison.OrdinalIgnoreCase))
                {
                    // Handle both comma and period as decimal separators
                    var speedText = individualResult[i + 1]?.Replace(',', '.');
                    if (!string.IsNullOrWhiteSpace(speedText) && 
                        double.TryParse(speedText, NumberStyles.Any, CultureInfo.InvariantCulture, out double spd))
                    {
                        // Validate speed is within plausible range (0-30 km/h)
                        if (spd >= 0.0 && spd <= 30.0)
                        {
                            result.Speed = spd;
                        }
                    }
                }

                if (individualResult[i].Equals("ISMEMBER", StringComparison.OrdinalIgnoreCase))
                {
                    result.IsMember = individualResult[i + 1] == "1";
                }

                if (individualResult[i].Equals("RACETIME", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryParseTime(individualResult[i + 1], out TimeSpan rt))
                    {
                        result.ExtractedRaceTime = rt;
                    }
                }

                if (individualResult[i].Equals("TIMEPERKM", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryParseTime(individualResult[i + 1], out TimeSpan tpk))
                    {
                        result.ExtractedTimePerKm = tpk;
                    }
                }

                if (individualResult[i].Equals("SEX", StringComparison.OrdinalIgnoreCase))
                {
                    result.Sex = individualResult[i + 1];
                }

                if (individualResult[i].Equals("POSITIONSEX", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(individualResult[i + 1], out int posBySex))
                    {
                        result.PositionBySex = posBySex;
                    }
                }

                if (individualResult[i].Equals("CATEGORY", StringComparison.OrdinalIgnoreCase))
                {
                    result.AgeCategory = individualResult[i + 1];
                }

                if (individualResult[i].Equals("POSITIONCAT", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(individualResult[i + 1], out int posByCat))
                    {
                        result.PositionByCategory = posByCat;
                    }
                }
            }

            // Find the time value
            for (int i = 0; i < individualResult.Length; i++)
            {
                if (TryParseTime(individualResult[i], out TimeSpan memberTime) &&
                    memberTime > TimeSpan.Zero &&  // Skip zero times
                    _pointsCalculationService.IsValidRaceTime(memberTime))
                {
                    result.Time = memberTime;

                    // Find matching members
                    result.Members = FindMatchingMembers(members, resultValue);

                    // If no matching members but is winner, create a dummy member entry
                    if (result.Members.Count == 0 && !result.IsMember && individualResult[0].Equals("TWINNER", StringComparison.OrdinalIgnoreCase))
                    {
                        var nameParts = ExtractNameFromResult(individualResult);
                        var winnerMember = new Member
                        {
                            FirstName = nameParts.firstName,
                            LastName = nameParts.lastName,
                            Email = "winner@external.com"
                        };
                        result.Members.Add(winnerMember);
                    }

                    result.IsValid = result.Members.Count > 0;
                    break;
                }
            }

            // Fallback: If no valid time found in array, use extracted times for points calculation
            if (result.Time == TimeSpan.Zero || !result.IsValid)
            {
                TimeSpan? timeForPoints = null;

                // Prefer extracted race time, fall back to time per km
                if (result.ExtractedRaceTime.HasValue && result.ExtractedRaceTime.Value > TimeSpan.Zero)
                {
                    timeForPoints = result.ExtractedRaceTime.Value;
                }
                else if (result.ExtractedTimePerKm.HasValue && result.ExtractedTimePerKm.Value > TimeSpan.Zero)
                {
                    timeForPoints = result.ExtractedTimePerKm.Value;
                }

                if (timeForPoints.HasValue)
                {
                    result.Time = timeForPoints.Value;

                    // Find matching members if not already done
                    if (result.Members.Count == 0)
                    {
                        result.Members = FindMatchingMembers(members, resultValue);

                        // If no matching members but is winner, create a dummy member entry
                        if (result.Members.Count == 0 && !result.IsMember && individualResult[0].Equals("TWINNER", StringComparison.OrdinalIgnoreCase))
                        {
                            var nameParts = ExtractNameFromResult(individualResult);
                            var winnerMember = new Member
                            {
                                FirstName = nameParts.firstName,
                                LastName = nameParts.lastName,
                                Email = "winner@external.com"
                            };
                            result.Members.Add(winnerMember);
                        }
                    }

                    result.IsValid = result.Members.Count > 0;
                }
            }

            return result;
        }

        private class ParsedRaceResult
        {
            public TimeSpan Time { get; set; }
            public bool IsReferenceTime { get { return Position == 1; } }
            public bool IsValid { get; set; }
            public List<Member> Members { get; set; } = new List<Member>();
            public int? Position { get; set; }
            public string Team { get; set; }
            public double? Speed { get; set; }
            public string Sex { get; set; }
            public int? PositionBySex { get; set; }
            public string AgeCategory { get; set; }
            public int? PositionByCategory { get; set; }
            public bool IsMember { get; set; } = true;
            public TimeSpan? ExtractedRaceTime { get; set; }
            public TimeSpan? ExtractedTimePerKm { get; set; }
        }

        private (string firstName, string lastName) ExtractNameFromResult(string[] individualResult)
        {
            // Try to extract name from the result data
            // Typical format: TWINNER;1;LastName;FirstName;... or TWINNER;1;FullName with noise;...
            string firstName = "Winner";
            string lastName = "External";

            // Collect potential name parts
            var nameParts = new List<string>();

            for (int i = 0; i < individualResult.Length; i++)
            {
                var part = individualResult[i]?.Trim();
                if (!string.IsNullOrEmpty(part) &&
                    !part.Equals("TWINNER", StringComparison.OrdinalIgnoreCase) &&
                    !part.Equals("TMEM", StringComparison.OrdinalIgnoreCase) &&
                    !part.All(char.IsDigit) &&
                    part.Length > 2)
                {
                    // Clean the part to remove noise
                    var cleanedPart = CleanNameField(part);

                    if (!string.IsNullOrWhiteSpace(cleanedPart) && cleanedPart.Length > 1)
                    {
                        nameParts.Add(cleanedPart);

                        // Stop after collecting 2 good name parts (should be enough)
                        if (nameParts.Count >= 2)
                            break;
                    }
                }
            }

            // Parse the collected name parts
            if (nameParts.Count == 0)
            {
                return (firstName, lastName);
            }
            else if (nameParts.Count == 1)
            {
                // Single part - might need to split if it contains space
                var cleaned = nameParts[0].Trim();
                var words = cleaned.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                if (words.Length >= 2)
                {
                    // Has multiple words - split into first and last
                    firstName = words[0];
                    lastName = string.Join(" ", words.Skip(1));
                }
                else
                {
                    // Single word - use as last name
                    lastName = cleaned;
                }
            }
            else
            {
                // Two or more parts collected
                // First cleaned part is likely firstname, second is lastname
                var firstCleaned = nameParts[0].Trim();
                var secondCleaned = nameParts[1].Trim();

                // Check if first part has multiple words
                var firstWords = firstCleaned.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                if (firstWords.Length >= 2)
                {
                    // First part has multiple words - split it
                    firstName = firstWords[0];
                    lastName = string.Join(" ", firstWords.Skip(1));
                }
                else
                {
                    // Use first part as firstname, second as lastname
                    firstName = firstCleaned;
                    lastName = secondCleaned;
                }
            }

            return (firstName, lastName);
        }

        private string CleanNameField(string nameField)
        {
            if (string.IsNullOrWhiteSpace(nameField))
                return string.Empty;

            var cleaned = nameField.Trim();

            // Remove time patterns like :18:30, :24:00, :25, etc.
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @":\d+(?::\d+)?", " ");

            // Remove speed patterns like ".5 km/h", "10.5 km/h", etc.
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\d+\.?\d*\s*km/h", " ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Remove standalone decimal numbers at the end like ".6", ".5"
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\.\d+\s*$", " ");

            // Remove trailing numbers/digits (like "100", "1000")
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+\d+\s*$", " ");

            // Remove Team/Equipe/Club followed by name (e.g., "Team Berneau")
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\b(?:Team|Equipe|Équipe|Club)\s+\w+(?:\s+\w+)?\b", " ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Remove age categories like "Espoir H", "Senior F", etc. (word + space + single letter)
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\b(?:Espoir|Senior|Junior|Cadet|Master|Veteran|Veterans|V\d+|M\d+|W\d+)\s+[HFhfDd]?\b", " ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Remove standalone single letters at the end (like category markers H, F, M, D)
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+[HFMDhfmd]\s*$", " ");

            // Remove category codes at the end (like A1, V1, SH, ESP, etc.)
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+[A-Z]{1,4}\d*\s*$", " ");

            // Clean up multiple spaces
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+", " ");

            return cleaned.Trim();
        }

        private bool TryParseTime(string timeText, out TimeSpan time)
        {
            time = TimeSpan.Zero;

            if (string.IsNullOrWhiteSpace(timeText))
                return false;

            // Try format: h:mm:ss
            if (TimeSpan.TryParseExact(timeText, @"h\:mm\:ss", CultureInfo.InvariantCulture, out time))
                return true;

            // Try format: hh:mm:ss
            if (TimeSpan.TryParseExact(timeText, @"hh\:mm\:ss", CultureInfo.InvariantCulture, out time))
                return true;

            // Try format: mm:ss
            if (TimeSpan.TryParseExact(timeText, @"mm\:ss", CultureInfo.InvariantCulture, out time))
                return true;

            // Try format: m:ss
            if (TimeSpan.TryParseExact(timeText, @"m\:ss", CultureInfo.InvariantCulture, out time))
                return true;

            // Try standard TimeSpan.Parse
            if (TimeSpan.TryParse(timeText, CultureInfo.InvariantCulture, out time))
                return true;

            return false;
        }

        private List<Member> FindMatchingMembers(List<Member> members, string resultValue)
        {
            return members.Where(member =>
                resultValue.RemoveDiacritics().Contains(member.FirstName.RemoveDiacritics(), StringComparison.InvariantCultureIgnoreCase) &&
                resultValue.RemoveDiacritics().Contains(member.LastName.RemoveDiacritics(), StringComparison.InvariantCultureIgnoreCase))
                .ToList();
        }
    }
}
