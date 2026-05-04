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
                Assert.Contains("ISMEMBER;1;", resultWithMember); // IsMember flag should be 1 (true)
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
                Assert.Contains(results.Values, r => r.Contains("ISMEMBER;0;")); // IsMember should be 0 (false)
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

        [Fact]
        public void ParseAcnTimingUrl_NewFormat_LIVEKIDS11()
        {
            // Real API format for LIVEKIDS11: ShortNewHtmlFormat (16 columns)
            // [pos, bib, "<b>NAME</b><br/><small>team</small>", country, sex, status, "<b>time</b><br/><small>speed km/h</small>", catPos, cat, sportogr, img, detail, ?, diplome, ?, bib2]
            // https://www.acn-timing.com/?lng=FR#/events/2141599542988686/ctx/20260503_vise/generic/198020_11/home/LIVEKIDS11
            var jsonContent = @"{
                ""Groups"": [
                    {
                        ""SlaveRows"": [
                            [""1."",""5281"",""<b>DELVAUX Loic</b><br/><small>TRAKKS TEAM ELITE</small>"",""BEL"",""M"",""Finish"",""<b>0:14:56</b><br/><small>  20.1km/h</small>"",""1"",""SEH"",""https://www.sportograf.com/en/event/20142/search/5281"",""sporto20.jpg"",""detail:5281"","""","""","""",""5281""],
                            [""2."",""5400"",""<b>MARTIN Sophie</b><br/><small>RIWA</small>"",""BEL"",""F"",""Finish"",""<b>0:15:30</b><br/><small>  19.4km/h</small>"",""1"",""SEF"",""https://www.sportograf.com/en/event/20142/search/5400"",""sporto20.jpg"",""detail:5400"","""","""","""",""5400""],
                            [""3."",""5101"",""<b>DUBOIS Thomas</b><br/><small>ACLO</small>"",""BEL"",""M"",""Finish"",""<b>0:15:45</b><br/><small>  19.1km/h</small>"",""2"",""SEH"",""https://www.sportograf.com/en/event/20142/search/5101"",""sporto20.jpg"",""detail:5101"","""","""","""",""5101""],
                            [""-"",""5999"",""<b>LEFEBVRE Emma</b><br/><small>WACO</small>"",""BEL"",""F"",""DNS"","""","""",""SEF"","""","""","""","""","""","""",""5999""]
                        ],
                        ""MasterRows"": null
                    }
                ],
                ""Count"": 4
            }";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile + ".json", jsonContent);

            try
            {
                var results = _repository.GetRaceResults(tempFile + ".json", _testMembers);

                Assert.NotNull(results);
                Assert.True(results.Count >= 4); // Header + at least 3 finishers + 1 DNS

                var resultString = string.Join("\n", results.Values);

                // Verify HTML tags are stripped from names
                Assert.DoesNotContain("<b>", resultString);
                Assert.Contains("DELVAUX", resultString);
                Assert.Contains("Loic", resultString);

                // Verify positions and times for finishers
                Assert.Contains("POS;1;", resultString);
                Assert.Contains("POS;2;", resultString);
                Assert.Contains("POS;3;", resultString);
                Assert.Contains("14:56", resultString);
                Assert.Contains("15:30", resultString);

                // Verify speed extraction from time HTML
                Assert.Contains("20.10", resultString);

                // Verify category
                Assert.Contains("SEH", resultString);
                Assert.Contains("SEF", resultString);

                // DNS participant included but without position
                Assert.Contains("LEFEBVRE", resultString);
                var lefebvreLines = resultString.Split('\n').Where(l => l.Contains("LEFEBVRE"));
                foreach (var line in lefebvreLines)
                    Assert.DoesNotContain("POS;", line);
            }
            finally
            {
                File.Delete(tempFile + ".json");
            }
        }

        [Fact]
        public void ParseAcnTimingUrl_NewFormat_LIVEKIDS12()
        {
            // Real API format for LIVEKIDS12: ShortNewHtmlFormat (16 columns)
            // https://www.acn-timing.com/?lng=FR#/events/2141599542988686/ctx/20260503_vise/generic/198020_12/home/LIVEKIDS12
            var jsonContent = @"{
                ""Groups"": [
                    {
                        ""SlaveRows"": [
                            [""1."",""10970"",""<b>DEFLANDRE Clement</b><br/><small></small>"",""BEL"",""M"",""Finish"",""<b>0:32:15</b><br/><small>  18.6km/h</small>"",""1"",""SEH"",""https://www.sportograf.com/en/event/20142/search/10970"",""sporto20.jpg"",""detail:10970"","""",""https://prod.chronorace.be/classements/classementpdf.aspx"","""",""10970""],
                            [""2."",""10820"",""<b>LAURENT Clara</b><br/><small>RIWA</small>"",""BEL"",""F"",""Finish"",""<b>0:33:40</b><br/><small>  17.8km/h</small>"",""1"",""SEF"",""https://www.sportograf.com/en/event/20142/search/10820"",""sporto20.jpg"",""detail:10820"","""","""","""",""10820""],
                            [""50."",""10500"",""<b>PEETERS Maxime</b><br/><small>WACO</small>"",""BEL"",""M"",""Finish"",""<b>0:45:00</b><br/><small>  13.3km/h</small>"",""20"",""V1H"",""https://www.sportograf.com/en/event/20142/search/10500"",""sporto20.jpg"",""detail:10500"","""","""","""",""10500""],
                            [""-"",""10001"",""<b>BERNARD Alice</b><br/><small>JSMC</small>"",""BEL"",""F"",""DNF"","""","""",""SEF"","""","""","""","""","""","""",""10001""]
                        ],
                        ""MasterRows"": null
                    }
                ],
                ""Count"": 4
            }";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile + ".json", jsonContent);

            try
            {
                var results = _repository.GetRaceResults(tempFile + ".json", _testMembers);

                Assert.NotNull(results);
                Assert.True(results.Count >= 4);

                var resultString = string.Join("\n", results.Values);

                Assert.DoesNotContain("<b>", resultString);
                Assert.Contains("DEFLANDRE", resultString);
                Assert.Contains("LAURENT", resultString);
                Assert.Contains("POS;1;", resultString);
                Assert.Contains("POS;50;", resultString);
                Assert.Contains("32:15", resultString);
                Assert.Contains("SEH", resultString);
                Assert.Contains("SEF", resultString);
                Assert.Contains("V1H", resultString);

                // DNF participant included but without position
                Assert.Contains("BERNARD", resultString);
                var bernardLines = resultString.Split('\n').Where(l => l.Contains("BERNARD"));
                foreach (var line in bernardLines)
                    Assert.DoesNotContain("POS;", line);
            }
            finally
            {
                File.Delete(tempFile + ".json");
            }
        }

        [Fact]
        public void ParseAcnTimingUrl_NewFormat_LIVE14()
        {
            // Real API format for LIVE14: ExtendedNewHtmlFormat (22 columns)
            // [pos, bib, nameHTML, country, sex, c1,c2,c3,c4,c5,c6, status, timeHTML, catPos, cat, sportogr, img, detail, diplome, pdf, ?, bib2]
            // https://www.acn-timing.com/?lng=FR#/events/2141599542988686/ctx/20260503_vise/generic/197994_14/home/LIVE14
            var jsonContent = @"{
                ""Groups"": [
                    {
                        ""SlaveRows"": [
                            [""1."",""1"",""<b>SIMONS Stef</b><br/><small>KINEPUNT</small>"",""BEL"",""M"",""2"",""2126993"",""2"",""4001853"",""1"",""6321393"",""Finish"",""<b>0:48:25</b><br/><small>  17.4km/h</small>"",""1"",""SEH"",""https://www.sportograf.com/en/event/20142/search/1"",""sporto20.jpg"",""detail:1"",""diplome.gif"",""https://prod.chronorace.be"","""",""1""],
                            [""2."",""202"",""<b>JANSSENS Tom</b><br/><small>JSMC</small>"",""BEL"",""M"",""1"",""222000"",""1"",""300000"",""2"",""500000"",""Finish"",""<b>0:49:10</b><br/><small>  17.1km/h</small>"",""2"",""SEH"",""https://www.sportograf.com/en/event/20142/search/202"",""sporto20.jpg"",""detail:202"",""diplome.gif"","""","""",""202""],
                            [""5."",""305"",""<b>MOREAU Sophie</b><br/><small>RIWA</small>"",""BEL"",""F"",""1"",""100000"",""1"",""200000"",""1"",""300000"",""Finish"",""<b>0:52:30</b><br/><small>  16.0km/h</small>"",""1"",""SEF"",""https://www.sportograf.com/en/event/20142/search/305"",""sporto20.jpg"",""detail:305"",""diplome.gif"","""","""",""305""],
                            [""100."",""3100"",""<b>LAMBERT Jean</b><br/><small>WACO</small>"",""BEL"",""M"",""35"",""700000"",""35"",""1000000"",""35"",""2000000"",""Finish"",""<b>1:15:20</b><br/><small>  11.1km/h</small>"",""35"",""V2H"",""https://www.sportograf.com/en/event/20142/search/3100"",""sporto20.jpg"",""detail:3100"",""diplome.gif"","""","""",""3100""],
                            [""-"",""3201"",""<b>DUPONT Marie</b><br/><small>ACLO</small>"",""BEL"",""F"","""",""-"",""-"",""-"",""-"",""-"",null,null,""-"",""SEF"","""","""","""","""","""","""",""3201""]
                        ],
                        ""MasterRows"": null
                    }
                ],
                ""Count"": 5
            }";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile + ".json", jsonContent);

            try
            {
                var results = _repository.GetRaceResults(tempFile + ".json", _testMembers);

                Assert.NotNull(results);
                Assert.True(results.Count >= 5);

                var resultString = string.Join("\n", results.Values);

                Assert.DoesNotContain("<b>", resultString);

                // Verify categories
                Assert.Contains("SEH", resultString);
                Assert.Contains("SEF", resultString);
                Assert.Contains("V2H", resultString);

                // Verify positions
                Assert.Contains("POS;1;", resultString);
                Assert.Contains("POS;100;", resultString);

                // Verify times
                Assert.Contains("48:25", resultString);
                Assert.Contains("1:15:20", resultString);

                // Verify speed extracted from HTML
                Assert.Contains("17.40", resultString);

                // DNS participant included but without position
                Assert.Contains("DUPONT", resultString);
                var dupontLines = resultString.Split('\n').Where(l => l.Contains("DUPONT") && !l.Contains("Header"));
                foreach (var line in dupontLines)
                    Assert.DoesNotContain("POS;", line);
            }
            finally
            {
                File.Delete(tempFile + ".json");
            }
        }

        [Fact]
        public void ParseAcnTimingUrl_NewFormat_LIVEHALF13()
        {
            // Real API format for LIVEHALF13: ExtendedNewHtmlFormat (21 columns)
            // https://www.acn-timing.com/?lng=FR#/events/2141599542988686/ctx/20260503_vise/generic/198023_13/home/LIVEHALF13
            var jsonContent = @"{
                ""Groups"": [
                    {
                        ""SlaveRows"": [
                            [""1."",""3609"",""<b>PAQUET Amaury</b><br/><small></small>"",""BEL"",""M"",""1"",""222724"",""1"",""1735738"",""1"",""3492241"",""Finish"",""<b>1:10:26</b><br/><small>  18.0km/h</small>"",""1"",""H35"",""https://www.sportograf.com/en/event/20142/search/3609"",""sporto20.jpg"",""diplome.gif"",""https://prod.chronorace.be"","""",""3609""],
                            [""2."",""4002"",""<b>DELFORGE Antoine</b><br/><small>RIWA</small>"",""BEL"",""M"",""2"",""300000"",""2"",""500000"",""2"",""900000"",""Finish"",""<b>1:12:10</b><br/><small>  17.5km/h</small>"",""2"",""H35"","""",""sporto20.jpg"",""diplome.gif"","""","""",""4002""],
                            [""10."",""4010"",""<b>HENRION Sarah</b><br/><small>WACO</small>"",""BEL"",""F"",""1"",""100000"",""1"",""200000"",""1"",""300000"",""Finish"",""<b>1:20:05</b><br/><small>  15.8km/h</small>"",""1"",""SEF"","""",""sporto20.jpg"",""diplome.gif"","""","""",""4010""],
                            [""500."",""4500"",""<b>SCHMITZ Marc</b><br/><small>JSMC</small>"",""BEL"",""M"",""120"",""700000"",""120"",""1000000"",""50"",""2000000"",""Finish"",""<b>2:10:30</b><br/><small>  9.7km/h</small>"",""50"",""V3H"","""",""sporto20.jpg"",""diplome.gif"","""","""",""4500""],
                            [""1000."",""5000"",""<b>FONTAINE Claire</b><br/><small>ACLO</small>"",""BEL"",""F"",""250"",""900000"",""250"",""1500000"",""80"",""2500000"",""Finish"",""<b>2:45:30</b><br/><small>  7.6km/h</small>"",""80"",""V2F"","""",""sporto20.jpg"",""diplome.gif"","""","""",""5000""],
                            [""-"",""5100"",""<b>LEONARD Thomas</b><br/><small>WACO</small>"",""BEL"",""M"","""",""-"",""-"",""-"",""-"",""-"",""DNF"",null,""-"",""V1H"","""","""","""","""","""",""5100""]
                        ],
                        ""MasterRows"": null
                    }
                ],
                ""Count"": 6
            }";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile + ".json", jsonContent);

            try
            {
                var results = _repository.GetRaceResults(tempFile + ".json", _testMembers);

                Assert.NotNull(results);
                Assert.True(results.Count >= 6);

                var resultString = string.Join("\n", results.Values);

                Assert.DoesNotContain("<b>", resultString);

                // Verify categories
                Assert.Contains("H35", resultString);
                Assert.Contains("SEF", resultString);
                Assert.Contains("V3H", resultString);
                Assert.Contains("V2F", resultString);
                Assert.Contains("V1H", resultString);

                // Verify position range (1 to 1000)
                Assert.Contains("POS;1;", resultString);
                Assert.Contains("POS;500;", resultString);
                Assert.Contains("POS;1000;", resultString);

                // Verify time range
                Assert.Contains("1:10:26", resultString);
                Assert.Contains("2:45:30", resultString);

                // Verify speed extracted from HTML
                Assert.Contains("18.00", resultString);

                // DNF participant included but without position
                Assert.Contains("LEONARD", resultString);
                var leonardLines = resultString.Split('\n').Where(l => l.Contains("LEONARD") && !l.Contains("Header"));
                foreach (var line in leonardLines)
                    Assert.DoesNotContain("POS;", line);
            }
            finally
            {
                File.Delete(tempFile + ".json");
            }
        }
    }
}

