using System;
using System.Collections.Generic;
using System.IO;
using NameParser.Domain.Entities;
using NameParser.Infrastructure.Repositories;
using Xunit;

namespace NameParser.Tests.Infrastructure.Repositories
{
    public class AcnTimingRaceResultRepositoryTests
    {
        private readonly AcnTimingRaceResultRepository _repository;
        private readonly List<Member> _testMembers;

        public AcnTimingRaceResultRepositoryTests()
        {
            _repository = new AcnTimingRaceResultRepository();
            _testMembers = new List<Member>
            {
                new Member { FirstName = "Jean", LastName = "Dupont" },
                new Member { FirstName = "Marie", LastName = "Martin" },
                new Member { FirstName = "Pierre", LastName = "Bernard" }
            };
        }

        [Fact]
        public void ParseJsonResults_ValidJsonArray_ReturnsCorrectResults()
        {
            // Arrange
            var jsonContent = @"[
                {
                    ""position"": 1,
                    ""firstName"": ""Jean"",
                    ""lastName"": ""Dupont"",
                    ""time"": ""01:45:30"",
                    ""team"": ""Club Challenger"",
                    ""speed"": ""14.5"",
                    ""sex"": ""H""
                },
                {
                    ""position"": 2,
                    ""firstName"": ""Sophie"",
                    ""lastName"": ""Leclerc"",
                    ""time"": ""01:50:15"",
                    ""team"": ""Running Team"",
                    ""speed"": ""13.8"",
                    ""sex"": ""D""
                }
            ]";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, jsonContent);

            try
            {
                // Act
                var results = _repository.GetRaceResults(tempFile, _testMembers);

                // Assert
                Assert.NotNull(results);
                Assert.True(results.Count > 1); // At least header + 1 result
                Assert.Contains(results.Values, r => r.Contains("Jean") && r.Contains("Dupont"));
                Assert.Contains(results.Values, r => r.Contains("Sophie") && r.Contains("Leclerc"));
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseJsonResults_WithResultsProperty_ReturnsCorrectResults()
        {
            // Arrange
            var jsonContent = @"{
                ""results"": [
                    {
                        ""rank"": 1,
                        ""prenom"": ""Pierre"",
                        ""nom"": ""Bernard"",
                        ""temps"": ""02:15:45"",
                        ""club"": ""Athletic Club"",
                        ""vitesse"": ""12.3""
                    }
                ]
            }";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, jsonContent);

            try
            {
                // Act
                var results = _repository.GetRaceResults(tempFile, _testMembers);

                // Assert
                Assert.NotNull(results);
                Assert.Contains(results.Values, r => r.Contains("Pierre") && r.Contains("Bernard"));
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseJsonResults_MatchesMember_SetsMemberFlagToTrue()
        {
            // Arrange
            var jsonContent = @"[
                {
                    ""position"": 1,
                    ""firstName"": ""Jean"",
                    ""lastName"": ""Dupont"",
                    ""time"": ""01:45:30""
                }
            ]";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, jsonContent);

            try
            {
                // Act
                var results = _repository.GetRaceResults(tempFile, _testMembers);

                // Assert
                var resultWithMember = string.Join("\n", results.Values);
                Assert.Contains("True", resultWithMember); // IsMember flag should be True
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseHtmlResults_ValidHtmlTable_ReturnsResults()
        {
            // Arrange
            var htmlContent = @"
                <html>
                <body>
                    <table>
                        <tr><th>Position</th><th>Nom</th><th>Temps</th></tr>
                        <tr><td>1</td><td>Jean Dupont (Club Challenger)</td><td>01:45:30</td></tr>
                        <tr><td>2</td><td>Sophie Leclerc (Running Team)</td><td>01:50:15</td></tr>
                    </table>
                </body>
                </html>";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, htmlContent);

            try
            {
                // Act
                var results = _repository.GetRaceResults(tempFile, _testMembers);

                // Assert
                Assert.NotNull(results);
                Assert.True(results.Count > 0);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseJsonResults_WithDifferentTimeFormats_ParsesCorrectly()
        {
            // Arrange
            var jsonContent = @"[
                {
                    ""position"": 1,
                    ""firstName"": ""Test"",
                    ""lastName"": ""User"",
                    ""time"": ""1:45:30""
                },
                {
                    ""position"": 2,
                    ""firstName"": ""Test2"",
                    ""lastName"": ""User2"",
                    ""time"": ""45:30""
                }
            ]";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, jsonContent);

            try
            {
                // Act
                var results = _repository.GetRaceResults(tempFile, _testMembers);

                // Assert
                Assert.NotNull(results);
                Assert.Contains(results.Values, r => r.Contains("1:45:30"));
                Assert.Contains(results.Values, r => r.Contains("45:30"));
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseJsonResults_WithCategoryInformation_ExtractsCategory()
        {
            // Arrange
            var jsonContent = @"[
                {
                    ""position"": 1,
                    ""firstName"": ""Jean"",
                    ""lastName"": ""Dupont"",
                    ""time"": ""01:45:30"",
                    ""category"": ""Senior H"",
                    ""categoryPosition"": 1,
                    ""sex"": ""H"",
                    ""sexPosition"": 1
                }
            ]";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, jsonContent);

            try
            {
                // Act
                var results = _repository.GetRaceResults(tempFile, _testMembers);

                // Assert
                var resultString = string.Join("\n", results.Values);
                Assert.Contains("Senior H", resultString);
                Assert.Contains("H", resultString);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void GetRaceResults_InvalidJsonFile_ThrowsException()
        {
            // Arrange
            var invalidJson = "{ invalid json content }}}";
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, invalidJson);

            try
            {
                // Act & Assert
                Assert.Throws<Exception>(() => _repository.GetRaceResults(tempFile, _testMembers));
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void GetRaceResults_FileNotFound_ThrowsFileNotFoundException()
        {
            // Arrange
            var nonExistentFile = "non_existent_file.json";

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => _repository.GetRaceResults(nonExistentFile, _testMembers));
        }

        [Fact]
        public void ParseJsonResults_EmptyMembersList_StillReturnsResults()
        {
            // Arrange
            var jsonContent = @"[
                {
                    ""position"": 1,
                    ""firstName"": ""Test"",
                    ""lastName"": ""User"",
                    ""time"": ""01:45:30""
                }
            ]";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, jsonContent);

            try
            {
                // Act
                var results = _repository.GetRaceResults(tempFile, new List<Member>());

                // Assert
                Assert.NotNull(results);
                Assert.Contains(results.Values, r => r.Contains("Test") && r.Contains("User"));
                Assert.Contains(results.Values, r => r.Contains("False")); // IsMember should be False
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }
}
