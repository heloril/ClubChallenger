using NameParser.Domain.Entities;
using NameParser.Infrastructure.Repositories;
using Xunit.Abstractions;

namespace NameParser.Tests.Infrastructure.Repositories
{
    /// <summary>
    /// Integration tests for PDF parsing that verify the expected number of classification results.
    /// These tests ensure that each PDF file is parsed correctly and produces the expected number of entries.
    /// </summary>
    public class PdfParserIntegrationTests
    {
        private readonly ITestOutputHelper _output;
        private readonly string _testFilesPath;

        public PdfParserIntegrationTests(ITestOutputHelper output)
        {
            _output = output;
            
            // Get path to test files (TestFiles directory in test project)
            var currentDirectory = Directory.GetCurrentDirectory();
            _testFilesPath = Path.Combine(currentDirectory, "TestFiles");
            
            _output.WriteLine($"Test files path: {_testFilesPath}");
        }

        /// <summary>
        /// Test data: filename, expected race name, expected distance, expected result count
        /// </summary>
        public static IEnumerable<object[]> PdfTestData =>
            new List<object[]>
            {
                // CrossCup races
                new object[] { "2026-01-25_Jogging de la CrossCup_Hannut_CJPL_10.20.pdf", "CrossCup 10km", 10.20, 110, "OtopFormatParser" },
                new object[] { "2026-01-25_Jogging de la CrossCup_Hannut_CJPL_5.20.pdf", "CrossCup 5km", 5.20, 84 , "OtopFormatParser"},
                
                // Jogging d'Hiver races
                new object[] { "2026-01-18_Jogging d'Hiver_Sprimont_CJPL_12.00.pdf", "Jogging d'Hiver 12km", 12.00, 175, "OtopFormatParser" },
                new object[] { "2026-01-18_Jogging d'Hiver_Sprimont_CJPL_7.00.pdf", "Jogging d'Hiver 7km", 7.00, 175, "OtopFormatParser" },
                
                // Les Collines de Cointe races
                new object[] { "2026-02-01_Les Collines de Cointe_Liège_CJPL_5.00.pdf", "Les Collines de Cointe 5km", 5.00, 156, "OtopFormatParser" },
                new object[] { "2026-02-01_Les Collines de Cointe_Liège_CJPL_10.00.pdf", "Les Collines de Cointe 10km", 10.00, 262, "OtopFormatParser" },
                
                // Grand Challenge races
                new object[] { "20250421SeraingGC.pdf", "Grand Challenge Seraing", 10.00, 279, "GoalTimingFormatParser" },
                new object[] { "20250511BlancGravierGC.pdf", "Grand Challenge Blanc Gravier", 10.00, 205, "GoalTimingFormatParser" },
                
                // Jogging de l'An Neuf races
                new object[] { "Classement-10km-Jogging-de-lAn-Neuf.pdf", "Jogging de l'An Neuf 10km", 10.00, 354, "GlobalPacingFormatParser" },
                new object[] { "Classement-5km-Jogging-de-lAn-Neuf.pdf", "Jogging de l'An Neuf 5km", 5.00, 190, "GlobalPacingFormatParser" },
                
                // Les 10 Miles races
                new object[] { "2025-11-16_Les 10 Miles_Liège_CJPL_16.90.pdf", "Les 10 Miles 16.9km", 16.90, 217, "OtopFormatParser" },
                new object[] { "2025-11-16_Les 10 Miles_Liège_CJPL_7.30.pdf", "Les 10 Miles 7.3km", 7.30, 163, "OtopFormatParser" },
                
                // Jogging de Boirs (combined classifications)
                //new object[] { "Jogging de Boirs 2026.pdf", "Jogging de Boirs", 10.00, 126 } // 78 + 48 = 126

                //new OtopFormatParser(),
                //new GlobalPacingFormatParser(),
                //new ChallengeLaMeuseFormatParser(),
                //new GoalTimingFormatParser(),
                 new object[] { "La Zatopek en Famille 6.5kms.pdf", "La Zatopek en Famille 10km", 6.50, 286, "ChallengeLaMeuseFormatParser" },
                 new object[] { "La Zatopek en Famille 10kms.pdf", "La Zatopek en Famille 5km", 10.00, 469, "ChallengeLaMeuseFormatParser" },
                 new object[] { "La Zatopek en Famille 21kms.pdf", "La Zatopek en Famille 5km", 21.00, 288, "ChallengeLaMeuseFormatParser" },
            };

        private string GetTestPdfPath(string filename)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var pdfPath = Path.Combine(baseDir, "TestData", "PDF", filename);

            if (!File.Exists(pdfPath))
            {
                throw new FileNotFoundException($"Test PDF file not found: {pdfPath}");
            }

            return pdfPath;
        }

        [Theory]
        [MemberData(nameof(PdfTestData))]
        public void ParsePdf_ShouldProduceExpectedNumberOfResults(
            string filename, 
            string expectedRaceName, 
            double expectedDistance, 
            int expectedResultCount,
            string expectedParserName)
        {
            // Arrange
            var filePath = GetTestPdfPath(filename);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(
                    $"Test PDF file not found: {filePath}. " +
                    $"Please ensure the file exists in the TestFiles directory.");
            }

            var repository = new PdfRaceResultRepository();
            var members = GetTestMembers(); // Empty list - testing parsing only

            // Act
            Dictionary<int, string> results;
            try
            {
                results = repository.GetRaceResults(filePath, members);
            }
            catch (Exception ex)
            {
                _output.WriteLine($"❌ ERROR parsing {filename}:");
                _output.WriteLine($"   {ex.Message}");
                throw;
            }

            // Remove header and reference entries (they have IDs 0 and 1)
            var classificationResults = results
                .Where(kvp => kvp.Key >= 2) // Skip header (0) and reference (1)
                .ToList();

            var actualCount = classificationResults.Count;

            // Log details
            _output.WriteLine($"📄 File: {filename}");
            _output.WriteLine($"📊 Race: {expectedRaceName}");
            _output.WriteLine($"🔍 Expected Parser: {expectedParserName}");
            _output.WriteLine($"📏 Distance: {expectedDistance} km");
            _output.WriteLine($"✅ Expected results: {expectedResultCount}");
            _output.WriteLine($"📈 Actual results: {actualCount}");

            if (actualCount != expectedResultCount)
            {
                var difference = actualCount - expectedResultCount;
                var sign = difference > 0 ? "+" : "";
                _output.WriteLine($"⚠️  Difference: {sign}{difference}");

                // Log some sample entries for debugging
                _output.WriteLine("");
                _output.WriteLine("Sample entries:");
                var samples = classificationResults.Take(5);
                foreach (var sample in samples)
                {
                    _output.WriteLine($"  [{sample.Key}] {sample.Value}");
                }

                if (classificationResults.Count > 5)
                {
                    _output.WriteLine($"  ... and {classificationResults.Count - 5} more entries");
                }
            }
            else
            {
                _output.WriteLine("✓ Result count matches expected!");
            }

            // Validate column coverage for the expected parser
            _output.WriteLine("");
            _output.WriteLine("📋 Column Coverage Analysis:");
            ValidateParserColumns(classificationResults, expectedParserName, _output);

            _output.WriteLine("");

            // Assert
            actualCount.Should().Be(expectedResultCount, 
                $"PDF '{filename}' should parse exactly {expectedResultCount} classification results, " +
                $"but parsed {actualCount}. This indicates potential issues with:\n" +
                $"  - Missing entries (if actual < expected)\n" +
                $"  - Duplicate entries or parsing errors (if actual > expected)\n" +
                $"  - Page breaks not handled correctly\n" +
                $"  - Header/footer rows being included as data\n" +
                $"Check the test output above for details.");
        }

        private void ValidateParserColumns(List<KeyValuePair<int, string>> results, string parserName, ITestOutputHelper output)
        {
            if (results.Count == 0)
            {
                output.WriteLine("⚠️  No results to validate");
                return;
            }

            // Define expected columns for each parser
            var expectedColumns = GetExpectedColumnsForParser(parserName);

            // Parse results and count field presence
            var fieldStats = new Dictionary<string, (int present, int total)>();
            foreach (var column in expectedColumns)
            {
                fieldStats[column] = (0, results.Count);
            }

            foreach (var result in results)
            {
                var fields = ParseDelimitedResult(result.Value);

                foreach (var column in expectedColumns)
                {
                    if (IsFieldPresent(fields, column))
                    {
                        var (present, total) = fieldStats[column];
                        fieldStats[column] = (present + 1, total);
                    }
                }
            }

            // Report statistics
            output.WriteLine($"   Parser: {parserName}");
            output.WriteLine("   Expected columns and coverage:");

            var allColumnsFilled = true;
            foreach (var column in expectedColumns)
            {
                var (present, total) = fieldStats[column];
                var percentage = total > 0 ? (double)present / total * 100 : 0;
                var status = percentage >= 90 ? "✓" : "⚠";

                output.WriteLine($"   {status} {column,-20} {present,4}/{total,-4} ({percentage,5:F1}%)");

                if (percentage < 90)
                {
                    allColumnsFilled = false;
                }
            }

            if (!allColumnsFilled)
            {
                output.WriteLine("");
                output.WriteLine("   ⚠️  Some columns have less than 90% coverage!");
                output.WriteLine("   This may indicate:");
                output.WriteLine("      - Column detection issues in the parser");
                output.WriteLine("      - Missing data in the PDF");
                output.WriteLine("      - Column mapping errors");
            }
            else
            {
                output.WriteLine("   ✓ All expected columns have good coverage (≥90%)");
            }
        }

        private List<string> GetExpectedColumnsForParser(string parserName)
        {
            return parserName switch
            {
                "OtopFormatParser" => new List<string>
                {
                    "Position", "FirstName", "LastName", "Sex", "PositionBySex",
                    "AgeCategory", "PositionByCategory", "RaceTime", "Speed", "TimePerKm"
                },
                "GlobalPacingFormatParser" => new List<string>
                {
                    "Position", "FirstName", "LastName", "Sex", "PositionBySex",
                    "AgeCategory", "PositionByCategory", "Team", "RaceTime", "Speed", "TimePerKm"
                },
                "ChallengeLaMeuseFormatParser" => new List<string>
                {
                    "Position", "FirstName", "LastName", "RaceTime", "Speed",
                    "TimePerKm", "Team", "AgeCategory", "PositionByCategory"
                },
                "GoalTimingFormatParser" => new List<string>
                {
                    "Position", "FirstName", "LastName", "Sex", "Team",
                    "AgeCategory", "PositionByCategory", "RaceTime", "TimePerKm", "Speed"
                },
                _ => new List<string> { "Position", "FirstName", "LastName" } // Minimal for unknown parsers
            };
        }

        private Dictionary<string, string> ParseDelimitedResult(string delimitedResult)
        {
            var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var parts = delimitedResult.Split(';');

            // Format: TYPE;Position;LastName;FirstName;Time;...;KEY;VALUE;KEY;VALUE;...
            // First extract fixed positions (after TYPE)
            if (parts.Length > 1) fields["Position"] = parts[1];
            if (parts.Length > 2) fields["LastName"] = parts[2];
            if (parts.Length > 3) fields["FirstName"] = parts[3];
            if (parts.Length > 4) fields["Time"] = parts[4];

            // Then parse KEY;VALUE pairs (starting from index 5 or where RACETYPE appears)
            for (int i = 5; i < parts.Length - 1; i++)
            {
                var key = parts[i];
                var value = i + 1 < parts.Length ? parts[i + 1] : "";

                // Recognized keys
                if (key.Equals("RACETYPE", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("RACETIME", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("TIMEPERKM", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("POS", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("TEAM", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("SPEED", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("SEX", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("POSITIONSEX", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("CATEGORY", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("POSITIONCAT", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("ISMEMBER", StringComparison.OrdinalIgnoreCase))
                {
                    if (!fields.ContainsKey(key))
                    {
                        fields[key] = value;
                    }
                    i++; // Skip the value in next iteration
                }
            }

            // Map fields to standard names
            if (fields.TryGetValue("RACETIME", out var raceTime))
                fields["RaceTime"] = raceTime;
            if (fields.TryGetValue("TIMEPERKM", out var timePerKm))
                fields["TimePerKm"] = timePerKm;
            if (fields.TryGetValue("SPEED", out var speed))
                fields["Speed"] = speed;
            if (fields.TryGetValue("TEAM", out var team))
                fields["Team"] = team;
            if (fields.TryGetValue("SEX", out var sex))
                fields["Sex"] = sex;
            if (fields.TryGetValue("POSITIONSEX", out var posSex))
                fields["PositionBySex"] = posSex;
            if (fields.TryGetValue("CATEGORY", out var category))
                fields["AgeCategory"] = category;
            if (fields.TryGetValue("POSITIONCAT", out var posCat))
                fields["PositionByCategory"] = posCat;

            return fields;
        }

        private bool IsFieldPresent(Dictionary<string, string> fields, string fieldName)
        {
            // Try to get the field value
            if (!fields.TryGetValue(fieldName, out var value))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(value))
                return false;

            // Check for default/invalid values based on field type
            switch (fieldName)
            {
                case "Position":
                    // Position should be a positive number
                    return int.TryParse(value, out int pos) && pos > 0;

                case "FirstName":
                case "LastName":
                    // Names should not be "Unknown"
                    return value != "Unknown" && value.Length > 0;

                case "RaceTime":
                case "TimePerKm":
                    // Times should not be 00:00:00
                    return value != "00:00:00" && value != "0:00:00" && value != "0:00";

                case "Speed":
                    // Speed should be a positive number
                    return double.TryParse(value, out double speed) && speed > 0;

                case "Sex":
                    // Sex should be M or F
                    return value == "M" || value == "F";

                case "AgeCategory":
                    // Category should not be empty
                    return value.Length > 0;

                case "PositionBySex":
                case "PositionByCategory":
                    // Position should be a positive number
                    return int.TryParse(value, out int p) && p > 0;

                case "Team":
                    // Team should not be empty
                    return value.Length > 0;

                default:
                    // For unknown fields, just check if not empty
                    return value.Length > 0;
            }
        }

        [Fact]
        public void Integration_ParseAllPdfs_ShouldSucceed()
        {
            // Arrange
            var repository = new PdfRaceResultRepository();
            var members = GetTestMembers();
            var testData = PdfTestData.ToList();

            var results = new List<(string filename, bool success, int expected, int actual, string error, string parser)>();

            _output.WriteLine("========================================");
            _output.WriteLine("PDF PARSING INTEGRATION TEST SUMMARY");
            _output.WriteLine("========================================");
            _output.WriteLine("");

            // Act - Parse all PDFs
            foreach (var data in testData)
            {
                var filename = (string)data[0];
                var expectedRaceName = (string)data[1];
                var expectedDistance = (double)data[2];
                var expectedCount = (int)data[3];
                var expectedParser = data.Length > 4 ? (string)data[4] : "Unknown";

                var filePath = GetTestPdfPath(filename);

                try
                {
                    if (!File.Exists(filePath))
                    {
                        results.Add((filename, false, expectedCount, 0, "File not found", expectedParser));
                        continue;
                    }

                    var parsed = repository.GetRaceResults(filePath, members);
                    var actualCount = parsed.Count(kvp => kvp.Key >= 2);
                    var success = actualCount == expectedCount;

                    results.Add((filename, success, expectedCount, actualCount, null, expectedParser));
                }
                catch (Exception ex)
                {
                    results.Add((filename, false, expectedCount, 0, ex.Message, expectedParser));
                }
            }

            // Report
            var successCount = results.Count(r => r.success);
            var totalCount = results.Count;
            var successRate = (double)successCount / totalCount * 100;

            foreach (var result in results)
            {
                var status = result.success ? "✓ PASS" : "✗ FAIL";
                var statusColor = result.success ? "GREEN" : "RED";

                _output.WriteLine($"{status} | {result.filename}");
                _output.WriteLine($"       Parser: {result.parser}");
                _output.WriteLine($"       Expected: {result.expected}, Actual: {result.actual}");

                if (!result.success && result.error != null)
                {
                    _output.WriteLine($"       Error: {result.error}");
                }

                _output.WriteLine("");
            }

            _output.WriteLine("========================================");
            _output.WriteLine($"RESULTS: {successCount}/{totalCount} tests passed ({successRate:F1}%)");
            _output.WriteLine("========================================");
            _output.WriteLine("");

            // Assert
            successCount.Should().Be(totalCount, 
                $"All {totalCount} PDFs should parse with the expected result counts. " +
                $"Currently {successCount} pass and {totalCount - successCount} fail. " +
                $"Review the summary above for details on which files need attention.");
        }

        [Theory]
        [MemberData(nameof(PdfTestData))]
        public void ParsedResults_ShouldHaveValidPositions(
            string filename,
            string expectedRaceName,
            double expectedDistance,
            int expectedResultCount,
            string expectedParserName)
        {
            // Arrange
            var filePath = GetTestPdfPath(filename);
            if (!File.Exists(filePath))
            {
                // Skip if file doesn't exist (will be caught by main test)
                return;
            }

            var repository = new PdfRaceResultRepository();
            var members = GetTestMembers();

            // Act
            var results = repository.GetRaceResults(filePath, members);
            var classificationResults = results
                .Where(kvp => kvp.Key >= 2)
                .Select(kvp => kvp.Value)
                .ToList();

            // Assert - Check that results have position numbers
            var resultsWithPosition = classificationResults
                .Count(r => r.Contains("POS;") && !r.Contains("POS;0;"));

            _output.WriteLine($"📄 {filename}");
            _output.WriteLine($"🔍 Parser: {expectedParserName}");
            _output.WriteLine($"   Total results: {classificationResults.Count}");
            _output.WriteLine($"   With positions: {resultsWithPosition}");

            resultsWithPosition.Should().BeGreaterThan(0, 
                $"PDF '{filename}' should have results with valid position numbers");

            // At least 90% of results should have positions
            var positionRate = (double)resultsWithPosition / classificationResults.Count * 100;
            positionRate.Should().BeGreaterThan(90, 
                $"At least 90% of results should have valid positions in '{filename}'");
        }

        [Theory]
        [MemberData(nameof(PdfTestData))]
        public void ParsedResults_ShouldHaveValidNames(
            string filename,
            string expectedRaceName,
            double expectedDistance,
            int expectedResultCount,
            string expectedParserName)
        {
            // Arrange
            var filePath = GetTestPdfPath(filename);
            if (!File.Exists(filePath))
            {
                return;
            }

            var repository = new PdfRaceResultRepository();
            var members = GetTestMembers();

            // Act
            var results = repository.GetRaceResults(filePath, members);
            var classificationResults = results
                .Where(kvp => kvp.Key >= 2)
                .Select(kvp => kvp.Value)
                .ToList();

            // Assert - Check that results have names
            var resultsWithNames = classificationResults
                .Count(r => !r.Contains(";Unknown;Unknown;"));

            _output.WriteLine($"📄 {filename}");
            _output.WriteLine($"🔍 Parser: {expectedParserName}");
            _output.WriteLine($"   Total results: {classificationResults.Count}");
            _output.WriteLine($"   With valid names: {resultsWithNames}");

            // At least 95% of results should have valid names
            var nameRate = (double)resultsWithNames / classificationResults.Count * 100;
            nameRate.Should().BeGreaterThan(95, 
                $"At least 95% of results should have valid names in '{filename}'");
        }

        private List<Member> GetTestMembers()
        {
            // Return empty list for integration tests - we're testing parsing, not member matching
            return new List<Member>();
        }
    }
}
