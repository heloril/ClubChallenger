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
    public class PdfRaceResultRepositoryTests
    {
        private readonly PdfRaceResultRepository _repository;
        private readonly List<Member> _testMembers;

        public PdfRaceResultRepositoryTests()
        {
            _repository = new PdfRaceResultRepository();
            _testMembers = CreateTestMembers();
        }

        #region Test Data Setup

        private List<Member> CreateTestMembers()
        {
            return new List<Member>
            {
                new Member { Id = 1, FirstName = "Jean", LastName = "Dupont", BibNumber = "123" },
                new Member { Id = 2, FirstName = "Marie", LastName = "Martin", BibNumber = "456" },
                new Member { Id = 3, FirstName = "Pierre", LastName = "Bernard", BibNumber = "789" },
                new Member { Id = 4, FirstName = "Sophie", LastName = "Lefebvre", BibNumber = "234" },
                new Member { Id = 5, FirstName = "Luc", LastName = "Moreau", BibNumber = "567" }
            };
        }

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

        #endregion

        #region File Existence Tests

        [Fact]
        public void GetRaceResults_WithNonExistentFile_ShouldThrowFileNotFoundException()
        {
            // Arrange
            var nonExistentPath = "NonExistent.pdf";

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() =>
                _repository.GetRaceResults(nonExistentPath, _testMembers));
        }

        [Fact]
        public void GetRaceResults_WithNullFilePath_ShouldThrowException()
        {
            // Act & Assert
            var exception = Assert.ThrowsAny<Exception>(() =>
                _repository.GetRaceResults(null, _testMembers));
            exception.Should().NotBeNull();
        }

        #endregion

        #region CrossCup Format Tests

        [Fact]
        public void GetRaceResults_WithCrossCupFormat_ShouldParseSuccessfully()
        {
            // Arrange
            var pdfPath = GetTestPdfPath("2026-01-25_Jogging de la CrossCup_Hannut_CJPL_10.20.pdf");

            // Act
            var results = _repository.GetRaceResults(pdfPath, _testMembers);

            // Assert
            results.Should().NotBeEmpty();
            results.Should().ContainKey(0); // Header
            results.Count.Should().BeGreaterThan(2); // Header + at least some results
        }

        [Fact]
        public void GetRaceResults_WithCrossCupFormat_ShouldExtractMetadataFromFilename()
        {
            // Arrange
            var pdfPath = GetTestPdfPath("2026-01-25_Jogging de la CrossCup_Hannut_CJPL_10.20.pdf");

            // Act
            var results = _repository.GetRaceResults(pdfPath, _testMembers);

            // Assert - verify header contains expected metadata
            results[0].Should().Contain("Header");
        }

        [Fact]
        public void GetRaceResults_WithCrossCupFormat5km_ShouldParseSuccessfully()
        {
            // Arrange
            var pdfPath = GetTestPdfPath("2026-01-25_Jogging de la CrossCup_Hannut_CJPL_5.20.pdf");

            // Act
            var results = _repository.GetRaceResults(pdfPath, _testMembers);

            // Assert
            results.Should().NotBeEmpty();
            results.Count.Should().BeGreaterThan(2);
        }

        #endregion

        #region Grand Challenge Format Tests

        [Fact]
        public void GetRaceResults_WithGrandChallengeFormat_ShouldParseSuccessfully()
        {
            // Arrange
            var pdfPath = GetTestPdfPath("20250421SeraingGC.pdf");

            // Act
            var results = _repository.GetRaceResults(pdfPath, _testMembers);

            // Assert
            results.Should().NotBeEmpty();
            results.Should().ContainKey(0);
            results.Count.Should().BeGreaterThan(2);
        }

        [Fact]
        public void GetRaceResults_WithBlancGravierGC_ShouldParseSuccessfully()
        {
            // Arrange
            var pdfPath = GetTestPdfPath("20250511BlancGravierGC.pdf");

            // Act
            var results = _repository.GetRaceResults(pdfPath, _testMembers);

            // Assert
            results.Should().NotBeEmpty();
            results.Count.Should().BeGreaterThan(2);
        }

        #endregion

        #region Jogging d'Hiver Format Tests

        [Fact]
        public void GetRaceResults_WithJoggingDHiverFormat_ShouldParse12kmSuccessfully()
        {
            // Arrange
            var pdfPath = GetTestPdfPath("2026-01-18_Jogging d'Hiver_Sprimont_CJPL_12.00.pdf");

            // Act
            var results = _repository.GetRaceResults(pdfPath, _testMembers);

            // Assert
            results.Should().NotBeEmpty();
            results.Count.Should().BeGreaterThan(2);
        }

        [Fact]
        public void GetRaceResults_WithJoggingDHiverFormat_ShouldParse7kmSuccessfully()
        {
            // Arrange
            var pdfPath = GetTestPdfPath("2026-01-18_Jogging d'Hiver_Sprimont_CJPL_7.00.pdf");

            // Act
            var results = _repository.GetRaceResults(pdfPath, _testMembers);

            // Assert
            results.Should().NotBeEmpty();
            results.Count.Should().BeGreaterThan(2);
        }

        #endregion

        #region 10 Miles Format Tests

        [Fact]
        public void GetRaceResults_With10Miles_ShouldParse16kmSuccessfully()
        {
            // Arrange
            var pdfPath = GetTestPdfPath("2025-11-16_Les 10 Miles_Liège_CJPL_16.90.pdf");

            // Act
            var results = _repository.GetRaceResults(pdfPath, _testMembers);

            // Assert
            results.Should().NotBeEmpty();
            results.Count.Should().BeGreaterThan(2);
        }

        [Fact]
        public void GetRaceResults_With10Miles_ShouldParse7kmSuccessfully()
        {
            // Arrange
            var pdfPath = GetTestPdfPath("2025-11-16_Les 10 Miles_Liège_CJPL_7.30.pdf");

            // Act
            var results = _repository.GetRaceResults(pdfPath, _testMembers);

            // Assert
            results.Should().NotBeEmpty();
            results.Count.Should().BeGreaterThan(2);
        }

        #endregion

        #region Classement Format Tests

        [Fact]
        public void GetRaceResults_WithClassementFormat_ShouldParse10kmSuccessfully()
        {
            // Arrange
            var pdfPath = GetTestPdfPath("Classement-10km-Jogging-de-lAn-Neuf.pdf");

            // Act
            var results = _repository.GetRaceResults(pdfPath, _testMembers);

            // Assert
            results.Should().NotBeEmpty();
            results.Count.Should().BeGreaterThan(2);
        }

        [Fact]
        public void GetRaceResults_WithClassementFormat_ShouldParse5kmSuccessfully()
        {
            // Arrange
            var pdfPath = GetTestPdfPath("Classement-5km-Jogging-de-lAn-Neuf.pdf");

            // Act
            var results = _repository.GetRaceResults(pdfPath, _testMembers);

            // Assert
            results.Should().NotBeEmpty();
            results.Count.Should().BeGreaterThan(2);
        }

        #endregion

        #region Standard Format Tests

        [Fact]
        public void GetRaceResults_WithStandardFormat_ShouldParseSuccessfully()
        {
            // Arrange
            var pdfPath = GetTestPdfPath("Jogging de Boirs 2026.pdf");

            // Act
            var results = _repository.GetRaceResults(pdfPath, _testMembers);

            // Assert
            results.Should().NotBeEmpty();
            results.Count.Should().BeGreaterThan(2);
        }

        #endregion

        #region Data Validation Tests

        [Theory]
        [InlineData("2026-01-25_Jogging de la CrossCup_Hannut_CJPL_10.20.pdf")]
        [InlineData("2026-01-25_Jogging de la CrossCup_Hannut_CJPL_5.20.pdf")]
        [InlineData("2026-01-18_Jogging d'Hiver_Sprimont_CJPL_12.00.pdf")]
        public void GetRaceResults_ShouldContainValidHeader(string filename)
        {
            // Arrange
            var pdfPath = GetTestPdfPath(filename);

            // Act
            var results = _repository.GetRaceResults(pdfPath, _testMembers);

            // Assert
            results[0].Should().Contain("Header");
            results[0].Should().Contain(";");
        }

        [Theory]
        [InlineData("2026-01-25_Jogging de la CrossCup_Hannut_CJPL_10.20.pdf")]
        [InlineData("20250421SeraingGC.pdf")]
        public void GetRaceResults_ParsedEntriesShouldHaveValidFormat(string filename)
        {
            // Arrange
            var pdfPath = GetTestPdfPath(filename);

            // Act
            var results = _repository.GetRaceResults(pdfPath, _testMembers);

            // Assert
            // Skip header (key 0) and reference time if present (key 1)
            var dataEntries = results.Where(r => r.Key >= 2).ToList();
            
            dataEntries.Should().NotBeEmpty();
            
            foreach (var entry in dataEntries)
            {
                entry.Value.Should().Contain(";"); // Should be semicolon-delimited
                var parts = entry.Value.Split(';');
                parts.Length.Should().BeGreaterThan(2); // Should have multiple fields
            }
        }

        [Theory]
        [InlineData("2026-01-25_Jogging de la CrossCup_Hannut_CJPL_10.20.pdf")]
        public void GetRaceResults_ShouldParsePositions(string filename)
        {
            // Arrange
            var pdfPath = GetTestPdfPath(filename);

            // Act
            var results = _repository.GetRaceResults(pdfPath, _testMembers);

            // Assert
            var dataEntries = results.Where(r => r.Key >= 2).ToList();
            dataEntries.Should().NotBeEmpty();
            
            // At least some entries should have position numbers
            dataEntries.Should().Contain(e => e.Value.Contains("1;") || e.Value.Split(';')[0].Contains("1"));
        }

        [Theory]
        [InlineData("2026-01-25_Jogging de la CrossCup_Hannut_CJPL_10.20.pdf")]
        [InlineData("2026-01-18_Jogging d'Hiver_Sprimont_CJPL_12.00.pdf")]
        public void GetRaceResults_ShouldParseTimes(string filename)
        {
            // Arrange
            var pdfPath = GetTestPdfPath(filename);

            // Act
            var results = _repository.GetRaceResults(pdfPath, _testMembers);

            // Assert
            var dataEntries = results.Where(r => r.Key >= 2).ToList();
            dataEntries.Should().NotBeEmpty();
            
            // At least some entries should have time values (format: hh:mm:ss or mm:ss)
            dataEntries.Should().Contain(e => 
                e.Value.Contains(":") && 
                System.Text.RegularExpressions.Regex.IsMatch(e.Value, @"\d{1,2}:\d{2}"));
        }

        #endregion

        #region Performance Tests

        [Fact]
        public void GetRaceResults_ShouldCompleteWithinReasonableTime()
        {
            // Arrange
            var pdfPath = GetTestPdfPath("2026-01-25_Jogging de la CrossCup_Hannut_CJPL_10.20.pdf");
            var maxDuration = TimeSpan.FromSeconds(10);

            // Act
            var startTime = DateTime.Now;
            var results = _repository.GetRaceResults(pdfPath, _testMembers);
            var duration = DateTime.Now - startTime;

            // Assert
            results.Should().NotBeEmpty();
            duration.Should().BeLessThan(maxDuration);
        }

        #endregion

        #region Edge Cases Tests

        [Fact]
        public void GetRaceResults_WithEmptyMemberList_ShouldStillParseResults()
        {
            // Arrange
            var pdfPath = GetTestPdfPath("2026-01-25_Jogging de la CrossCup_Hannut_CJPL_10.20.pdf");
            var emptyMembers = new List<Member>();

            // Act
            var results = _repository.GetRaceResults(pdfPath, emptyMembers);

            // Assert
            results.Should().NotBeEmpty();
            results.Count.Should().BeGreaterThan(1);
        }

        [Fact]
        public void GetRaceResults_MultipleConsecutiveCalls_ShouldProduceConsistentResults()
        {
            // Arrange
            var pdfPath = GetTestPdfPath("2026-01-25_Jogging de la CrossCup_Hannut_CJPL_10.20.pdf");

            // Act
            var results1 = _repository.GetRaceResults(pdfPath, _testMembers);
            var results2 = _repository.GetRaceResults(pdfPath, _testMembers);

            // Assert
            results1.Count.Should().Be(results2.Count);
            
            foreach (var key in results1.Keys)
            {
                results2.Should().ContainKey(key);
                results2[key].Should().Be(results1[key]);
            }
        }

        #endregion

        #region All Test PDFs Integration Test

        [Fact]
        public void GetRaceResults_AllTestPdfs_ShouldParseSuccessfully()
        {
            // Arrange
            var testPdfDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "PDF");
            
            if (!Directory.Exists(testPdfDirectory))
            {
                Assert.Fail($"Test PDF directory not found: {testPdfDirectory}");
            }

            var pdfFiles = Directory.GetFiles(testPdfDirectory, "*.pdf");
            
            if (pdfFiles.Length == 0)
            {
                Assert.Fail($"No PDF files found in: {testPdfDirectory}");
            }

            var results = new Dictionary<string, bool>();
            var errors = new Dictionary<string, string>();

            // Act
            foreach (var pdfFile in pdfFiles)
            {
                var filename = Path.GetFileName(pdfFile);
                try
                {
                    var parsedResults = _repository.GetRaceResults(pdfFile, _testMembers);
                    results[filename] = parsedResults.Count > 2; // More than just header
                }
                catch (Exception ex)
                {
                    results[filename] = false;
                    errors[filename] = ex.Message;
                }
            }

            // Assert
            var failedFiles = results.Where(r => !r.Value).ToList();
            
            if (failedFiles.Any())
            {
                var errorMessage = "Failed to parse the following PDFs:\n";
                foreach (var failed in failedFiles)
                {
                    errorMessage += $"  - {failed.Key}";
                    if (errors.ContainsKey(failed.Key))
                    {
                        errorMessage += $": {errors[failed.Key]}";
                    }
                    errorMessage += "\n";
                }
                Assert.Fail(errorMessage);
            }

            results.Values.All(v => v).Should().BeTrue($"All {pdfFiles.Length} PDFs should parse successfully");
        }

        #endregion
    }
}
