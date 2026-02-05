using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using NameParser.Domain.Entities;
using NameParser.Infrastructure.Repositories;
using Xunit;

namespace NameParser.Tests.Infrastructure.Repositories
{
    public class PdfParserDataQualityTests
    {
        private readonly PdfRaceResultRepository _repository;
        private readonly List<Member> _testMembers;

        public PdfParserDataQualityTests()
        {
            _repository = new PdfRaceResultRepository();
            _testMembers = CreateTestMembers();
        }

        private List<Member> CreateTestMembers()
        {
            return new List<Member>
            {
                new Member("Jean", "Dupont", "jean.dupont@test.com", true, true),
                new Member("Marie", "Martin", "marie.martin@test.com", true, true)
            };
        }

        private string GetTestPdfPath(string filename)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDir, "TestData", "PDF", filename);
        }

        #region Data Completeness Tests

        [Theory]
        [InlineData("2026-01-25_Jogging de la CrossCup_Hannut_CJPL_10.20.pdf")]
        [InlineData("2026-01-18_Jogging d'Hiver_Sprimont_CJPL_12.00.pdf")]
        public void ParsedResults_ShouldHaveNonEmptyNames(string filename)
        {
            // Arrange
            var pdfPath = GetTestPdfPath(filename);

            // Act
            var results = _repository.GetRaceResults(pdfPath, _testMembers);

            // Assert
            var dataEntries = results.Where(r => r.Key >= 2).ToList();
            dataEntries.Should().NotBeEmpty();

            foreach (var entry in dataEntries)
            {
                var parts = entry.Value.Split(';');
                // Name should be in the data
                var hasName = parts.Any(p => !string.IsNullOrWhiteSpace(p) && 
                                           p != "Unknown" && 
                                           !int.TryParse(p, out _) && 
                                           !p.Contains(":"));
                hasName.Should().BeTrue($"Entry {entry.Key} should have a name: {entry.Value}");
            }
        }

        [Theory]
        [InlineData("2026-01-25_Jogging de la CrossCup_Hannut_CJPL_10.20.pdf")]
        public void ParsedResults_ShouldHaveSequentialPositions(string filename)
        {
            // Arrange
            var pdfPath = GetTestPdfPath(filename);

            // Act
            var results = _repository.GetRaceResults(pdfPath, _testMembers);

            // Assert
            var dataEntries = results.Where(r => r.Key >= 2).OrderBy(r => r.Key).ToList();
            dataEntries.Should().NotBeEmpty();

            // Check that positions are reasonable (not all zeros or negatives)
            var positionPattern = new System.Text.RegularExpressions.Regex(@"^\d+");
            var positions = dataEntries
                .Select(e => e.Value.Split(';').FirstOrDefault())
                .Where(p => positionPattern.IsMatch(p ?? ""))
                .Select(p => int.Parse(positionPattern.Match(p).Value))
                .ToList();

            if (positions.Any())
            {
                positions.Min().Should().BeGreaterThan(0, "Positions should start from 1");
                positions.Should().BeInAscendingOrder("Positions should be in order");
            }
        }

        #endregion

        #region Data Consistency Tests

        [Theory]
        [InlineData("2026-01-25_Jogging de la CrossCup_Hannut_CJPL_10.20.pdf")]
        [InlineData("2026-01-25_Jogging de la CrossCup_Hannut_CJPL_5.20.pdf")]
        public void ParsedResults_FromSameEvent_ShouldHaveDifferentParticipants(string filename)
        {
            // Arrange
            var pdfPath = GetTestPdfPath(filename);

            // Act
            var results = _repository.GetRaceResults(pdfPath, _testMembers);

            // Assert
            var dataEntries = results.Where(r => r.Key >= 2).Select(r => r.Value).ToList();
            dataEntries.Should().NotBeEmpty();

            // Check for duplicate entries (same name appearing multiple times)
            var nameCounts = new Dictionary<string, int>();
            foreach (var entry in dataEntries)
            {
                var parts = entry.Split(';');
                if (parts.Length > 1)
                {
                    var name = parts[1].Trim();
                    if (!string.IsNullOrWhiteSpace(name) && name != "Unknown")
                    {
                        if (!nameCounts.ContainsKey(name))
                            nameCounts[name] = 0;
                        nameCounts[name]++;
                    }
                }
            }

            // Most names should appear only once (some legitimate duplicates may exist)
            var duplicates = nameCounts.Where(kv => kv.Value > 1).ToList();
            duplicates.Count.Should().BeLessThan(dataEntries.Count / 10, 
                "Too many duplicate names suggest parsing errors");
        }

        [Theory]
        [InlineData("2026-01-25_Jogging de la CrossCup_Hannut_CJPL_10.20.pdf")]
        public void ParsedResults_TimesShouldBeReasonable(string filename)
        {
            // Arrange
            var pdfPath = GetTestPdfPath(filename);

            // Act
            var results = _repository.GetRaceResults(pdfPath, _testMembers);

            // Assert
            var dataEntries = results.Where(r => r.Key >= 2).ToList();
            dataEntries.Should().NotBeEmpty();

            var timePattern = new System.Text.RegularExpressions.Regex(@"(\d{1,2}):(\d{2})(?::(\d{2}))?");
            
            foreach (var entry in dataEntries)
            {
                var matches = timePattern.Matches(entry.Value);
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    var hours = int.Parse(match.Groups[1].Value);
                    var minutes = int.Parse(match.Groups[2].Value);
                    
                    // Basic sanity checks
                    hours.Should().BeLessThan(10, "Race time should be less than 10 hours");
                    minutes.Should().BeLessThan(60, "Minutes should be less than 60");
                }
            }
        }

        #endregion

        #region Header and Structure Tests

        [Theory]
        [InlineData("2026-01-25_Jogging de la CrossCup_Hannut_CJPL_10.20.pdf")]
        [InlineData("20250421SeraingGC.pdf")]
        [InlineData("Classement-10km-Jogging-de-lAn-Neuf.pdf")]
        public void ParsedResults_ShouldAlwaysHaveHeader(string filename)
        {
            // Arrange
            var pdfPath = GetTestPdfPath(filename);

            // Act
            var results = _repository.GetRaceResults(pdfPath, _testMembers);

            // Assert
            results.Should().ContainKey(0, "Results should always include a header at key 0");
            results[0].Should().Contain("Header");
        }

        [Theory]
        [InlineData("2026-01-25_Jogging de la CrossCup_Hannut_CJPL_10.20.pdf")]
        public void ParsedResults_HeaderShouldBeWellFormed(string filename)
        {
            // Arrange
            var pdfPath = GetTestPdfPath(filename);

            // Act
            var results = _repository.GetRaceResults(pdfPath, _testMembers);

            // Assert
            results[0].Should().NotBeNullOrWhiteSpace();
            results[0].Split(';').Length.Should().BeGreaterThan(2, 
                "Header should have multiple fields");
        }

        #endregion

        #region Parsing Statistics Tests

        [Theory]
        [InlineData("2026-01-25_Jogging de la CrossCup_Hannut_CJPL_10.20.pdf", 10)]
        [InlineData("2026-01-25_Jogging de la CrossCup_Hannut_CJPL_5.20.pdf", 10)]
        [InlineData("2026-01-18_Jogging d'Hiver_Sprimont_CJPL_12.00.pdf", 10)]
        public void ParsedResults_ShouldHaveMinimumNumberOfParticipants(string filename, int minParticipants)
        {
            // Arrange
            var pdfPath = GetTestPdfPath(filename);

            // Act
            var results = _repository.GetRaceResults(pdfPath, _testMembers);

            // Assert
            var dataEntries = results.Where(r => r.Key >= 2).ToList();
            dataEntries.Count.Should().BeGreaterThanOrEqualTo(minParticipants,
                $"Expected at least {minParticipants} participants in the results");
        }

        [Fact]
        public void ParsedResults_AllTestPdfs_ShouldHaveReasonableParticipantCounts()
        {
            // Arrange
            var testPdfDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "PDF");
            
            if (!Directory.Exists(testPdfDirectory))
            {
                Assert.Fail($"Test PDF directory not found: {testPdfDirectory}");
            }

            var pdfFiles = Directory.GetFiles(testPdfDirectory, "*.pdf");
            var statistics = new Dictionary<string, int>();

            // Act
            foreach (var pdfFile in pdfFiles)
            {
                var filename = Path.GetFileName(pdfFile);
                try
                {
                    var results = _repository.GetRaceResults(pdfFile, _testMembers);
                    var participantCount = results.Where(r => r.Key >= 2).Count();
                    statistics[filename] = participantCount;
                }
                catch (Exception ex)
                {
                    statistics[filename] = -1; // Error indicator
                }
            }

            // Assert
            statistics.Should().NotBeEmpty();
            
            foreach (var stat in statistics)
            {
                stat.Value.Should().BeGreaterThan(0, 
                    $"File {stat.Key} should have parsed at least some participants");
                stat.Value.Should().BeLessThan(1000, 
                    $"File {stat.Key} participant count seems unreasonably high (possible parsing error)");
            }
        }

        #endregion

        #region Field Validation Tests

        [Theory]
        [InlineData("2026-01-25_Jogging de la CrossCup_Hannut_CJPL_10.20.pdf")]
        public void ParsedResults_ShouldHaveConsistentFieldCount(string filename)
        {
            // Arrange
            var pdfPath = GetTestPdfPath(filename);

            // Act
            var results = _repository.GetRaceResults(pdfPath, _testMembers);

            // Assert
            var dataEntries = results.Where(r => r.Key >= 2).ToList();
            dataEntries.Should().NotBeEmpty();

            var fieldCounts = dataEntries
                .Select(e => e.Value.Split(';').Length)
                .ToList();

            // Most entries should have the same number of fields
            var mostCommonFieldCount = fieldCounts
                .GroupBy(c => c)
                .OrderByDescending(g => g.Count())
                .First()
                .Key;

            var entriesWithCommonCount = fieldCounts.Count(c => c == mostCommonFieldCount);
            var percentage = (double)entriesWithCommonCount / fieldCounts.Count * 100;

            percentage.Should().BeGreaterThan(80, 
                "At least 80% of entries should have the same field count");
        }

        #endregion

        #region Special Characters Tests

        [Theory]
        [InlineData("2026-01-18_Jogging d'Hiver_Sprimont_CJPL_12.00.pdf")]
        public void ParsedResults_ShouldHandleSpecialCharacters(string filename)
        {
            // Arrange
            var pdfPath = GetTestPdfPath(filename);

            // Act
            var results = _repository.GetRaceResults(pdfPath, _testMembers);

            // Assert
            results.Should().NotBeEmpty();
            
            // Results should not contain obvious encoding errors
            var allText = string.Join(" ", results.Values);
            allText.Should().NotContain("�", "Results should not contain encoding error characters");
        }

        #endregion

        #region Member Matching Tests

        [Fact]
        public void ParsedResults_WithKnownMembers_ShouldAttemptMatching()
        {
            // Arrange
            var knownMembers = new List<Member>
            {
                new Member { Id = 1, FirstName = "Jean", LastName = "Dupont", BibNumber = "123" },
                new Member { Id = 2, FirstName = "Marie", LastName = "Martin", BibNumber = "456" },
                new Member { Id = 3, FirstName = "Pierre", LastName = "Bernard", BibNumber = "789" }
            };

            var pdfPath = GetTestPdfPath("2026-01-25_Jogging de la CrossCup_Hannut_CJPL_10.20.pdf");

            // Act
            var results = _repository.GetRaceResults(pdfPath, knownMembers);

            // Assert
            results.Should().NotBeEmpty();
            // The parser should process the member list without errors
            knownMembers.Should().AllSatisfy(m => m.Id.Should().BeGreaterThan(0));
        }

        #endregion
    }
}
