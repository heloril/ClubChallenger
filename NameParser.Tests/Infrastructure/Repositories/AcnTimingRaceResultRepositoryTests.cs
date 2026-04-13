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

        [Fact]
        public void ParseJsonResults_ChronoraceCleanFormat_ReturnsCorrectResults()
        {
            // Arrange - LIVE6 format with clean array data (no HTML tags)
            var jsonContent = @"{
                ""Groups"": [
                    {
                        ""SlaveRows"": [
                            [""1."",""14263"",""ZALESKI Jules"",""TEAMULIEGE"",""BEL"",""M"","""",""TEAMULIEGE"",""TEAMULIEGE"",""Finish"",""3:28:45"",""-"",""13.250"",""1"",""SEH"",""LINK:https://prod.chronorace.be/mypage/mypage.aspx"","""",""14263"",""diplome.gif"",""https://prod.chronorace.be""],
                            [""2."",""14100"",""DUBOIS Marie"",""RC LIEGE"",""BEL"",""F"","""",""RC LIEGE"",""RC LIEGE"",""Finish"",""3:35:20"",""-"",""12.850"",""1"",""SEF"",""LINK:https://prod.chronorace.be/mypage/mypage.aspx"","""",""14100"",""diplome.gif"",null],
                            [""-"",""14021"",""TUTUNARU Alexandru-Cosmin"","""",""ROU"",""M"","""","""","""",null,null,""-"",""-"","""",""M40"",""LINK:https://prod.chronorace.be/mypage/mypage.aspx"","""",""14021"",""diplome.gif"",null]
                        ],
                        ""MasterRows"": null
                    }
                ],
                ""Count"": 3,
                ""KeepCount"": 3,
                ""PagingAllowed"": true
            }";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, jsonContent);

            try
            {
                // Act
                var results = _repository.GetRaceResults(tempFile, _testMembers);

                // Assert
                Assert.NotNull(results);
                Assert.True(results.Count > 1); // At least header + results

                var resultString = string.Join("\n", results.Values);

                // Check first finisher (position 1)
                Assert.Contains("ZALESKI", resultString);
                Assert.Contains("Jules", resultString);
                Assert.Contains("3:28:45", resultString);
                Assert.Contains("POS;1;", resultString);
                Assert.Contains("13.25", resultString); // Speed
                Assert.Contains("SEH", resultString); // Category
                Assert.Contains("TEAMULIEGE", resultString);

                // Check second finisher (position 2, female)
                Assert.Contains("DUBOIS", resultString);
                Assert.Contains("Marie", resultString);
                Assert.Contains("3:35:20", resultString);
                Assert.Contains("SEF", resultString);

                // Check DNS/DNF participant (should be included but without position)
                Assert.Contains("TUTUNARU", resultString);
                Assert.Contains("Alexandru-Cosmin", resultString);
                // Should NOT have a position since it's "-"
                var tutunuruLines = resultString.Split('\n').Where(l => l.Contains("TUTUNARU"));
                foreach (var line in tutunuruLines)
                {
                    // If it contains TUTUNARU, it should NOT have "POS;" because they didn't finish
                    if (line.Contains("TUTUNARU") && !line.Contains("Header"))
                    {
                        Assert.DoesNotContain("POS;", line);
                    }
                }
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseJsonResults_ChronoraceHtmlFormat_ReturnsCorrectResults()
        {
            // Arrange - Old HTML format with tags
            var jsonContent = @"{
                ""Groups"": [
                    {
                        ""SlaveRows"": [
                            [""46."",""M"",""411"",""<b>DE VOS Nicolas</b><br/><small></small>"",""BEL"",""M"","""","""",""Finish"",""<b>1:36:20</b><br/><small>  13.4km/h</small>"",""12"",""V1H"",""detail:411_3"","""",""411""],
                            [""47."",""F"",""523"",""<b>FETTWEIS Véronique</b><br/><small></small>"",""BEL"",""F"","""","""",""Finish"",""<b>1:37:15</b><br/><small>  13.2km/h</small>"",""8"",""V1F"",""detail:523_3"","""",""523""]
                        ],
                        ""MasterRows"": null
                    }
                ],
                ""Count"": 2
            }";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, jsonContent);

            try
            {
                // Act
                var results = _repository.GetRaceResults(tempFile, _testMembers);

                // Assert
                Assert.NotNull(results);
                Assert.True(results.Count > 1);

                var resultString = string.Join("\n", results.Values);

                // Verify HTML tags were stripped
                Assert.Contains("DE VOS", resultString);
                Assert.Contains("Nicolas", resultString);
                Assert.DoesNotContain("<b>", resultString);
                Assert.DoesNotContain("</b>", resultString);

                // Verify data extraction
                Assert.Contains("1:36:20", resultString);
                Assert.Contains("POS;46;", resultString);
                Assert.Contains("13.4", resultString); // Speed from HTML
                Assert.Contains("V1H", resultString); // Category

                // Check second participant
                Assert.Contains("FETTWEIS", resultString);
                Assert.Contains("Véronique", resultString);
                Assert.Contains("V1F", resultString);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseJsonResults_ChronoraceNullGroups_ReturnsEmptyResults()
        {
            // Arrange - Groups is null (race hasn't started or no results yet)
            var jsonContent = @"{
                ""Groups"": null,
                ""Count"": 0,
                ""KeepCount"": 0,
                ""PagingAllowed"": false
            }";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, jsonContent);

            try
            {
                // Act
                var results = _repository.GetRaceResults(tempFile, _testMembers);

                // Assert
                Assert.NotNull(results);
                Assert.Equal(2, results.Count); // Just header + "no results" message
                var resultString = string.Join("\n", results.Values);
                Assert.Contains("No results available yet", resultString);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }
}

