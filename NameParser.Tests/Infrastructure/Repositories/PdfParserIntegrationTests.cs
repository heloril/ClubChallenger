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
                new object[] { "2026-01-25_Jogging de la CrossCup_Hannut_CJPL_10.20.pdf", "CrossCup 10km", 10.20, 110 },
                new object[] { "2026-01-25_Jogging de la CrossCup_Hannut_CJPL_5.20.pdf", "CrossCup 5km", 5.20, 84 },
                
                // Jogging d'Hiver races
                new object[] { "2026-01-18_Jogging d'Hiver_Sprimont_CJPL_12.00.pdf", "Jogging d'Hiver 12km", 12.00, 175 },
                new object[] { "2026-01-18_Jogging d'Hiver_Sprimont_CJPL_7.00.pdf", "Jogging d'Hiver 7km", 7.00, 175 },
                
                // Les Collines de Cointe races
                new object[] { "2026-02-01_Les Collines de Cointe_Liège_CJPL_5.00.pdf", "Les Collines de Cointe 5km", 5.00, 156 },
                new object[] { "2026-02-01_Les Collines de Cointe_Liège_CJPL_10.00.pdf", "Les Collines de Cointe 10km", 10.00, 262 },
                
                // Grand Challenge races
                new object[] { "20250421SeraingGC.pdf", "Grand Challenge Seraing", 10.00, 279 },
                new object[] { "20250511BlancGravierGC.pdf", "Grand Challenge Blanc Gravier", 10.00, 205 },
                
                // Jogging de l'An Neuf races
                new object[] { "Classement-10km-Jogging-de-lAn-Neuf.pdf", "Jogging de l'An Neuf 10km", 10.00, 354 },
                new object[] { "Classement-5km-Jogging-de-lAn-Neuf.pdf", "Jogging de l'An Neuf 5km", 5.00, 190 },
                
                // Les 10 Miles races
                new object[] { "2025-11-16_Les 10 Miles_Liège_CJPL_16.90.pdf", "Les 10 Miles 16.9km", 16.90, 217 },
                new object[] { "2025-11-16_Les 10 Miles_Liège_CJPL_7.30.pdf", "Les 10 Miles 7.3km", 7.30, 163 },
                
                // Jogging de Boirs (combined classifications)
                //new object[] { "Jogging de Boirs 2026.pdf", "Jogging de Boirs", 10.00, 126 } // 78 + 48 = 126
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
            int expectedResultCount)
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

        [Fact]
        public void Integration_ParseAllPdfs_ShouldSucceed()
        {
            // Arrange
            var repository = new PdfRaceResultRepository();
            var members = GetTestMembers();
            var testData = PdfTestData.ToList();
            
            var results = new List<(string filename, bool success, int expected, int actual, string error)>();

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
                
                var filePath = GetTestPdfPath(filename);

                try
                {
                    if (!File.Exists(filePath))
                    {
                        results.Add((filename, false, expectedCount, 0, "File not found"));
                        continue;
                    }

                    var parsed = repository.GetRaceResults(filePath, members);
                    var actualCount = parsed.Count(kvp => kvp.Key >= 2);
                    var success = actualCount == expectedCount;
                    
                    results.Add((filename, success, expectedCount, actualCount, null));
                }
                catch (Exception ex)
                {
                    results.Add((filename, false, expectedCount, 0, ex.Message));
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
            int expectedResultCount)
        {
            // Arrange
            var filePath = Path.Combine(_testFilesPath, filename);
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
            int expectedResultCount)
        {
            // Arrange
            var filePath = Path.Combine(_testFilesPath, filename);
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
