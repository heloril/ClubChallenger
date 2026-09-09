using System.Collections.Generic;
using System.IO;
using System.Linq;
using NameParser.Domain.Entities;
using NameParser.Infrastructure.Repositories;
using NameParser.Infrastructure.Services;
using Xunit;

namespace NameParser.Tests.Infrastructure.Repositories
{
	public class OtopRaceResultRepositoryTests
	{
		private readonly List<Member> _testMembers = new List<Member>
		{
			new Member { FirstName = "Jean", LastName = "Dupont" },
			new Member { FirstName = "Marie", LastName = "Martin" }
		};

		[Theory]
		[InlineData("21,1 km", 21.1)]
		[InlineData("21.1km", 21.1)]
		[InlineData("10 km", 10)]
		[InlineData("Semi-marathon 21.1 km", 21.1)]
		[InlineData("5 KM", 5)]
		[InlineData("Marathon", 0)]
		public void ParseDistanceKm_ParsesLeadingNumber(string title, double expected)
		{
			var actual = OtopWebDownloader.ParseDistanceKm(title);
			Assert.Equal(expected, actual, 3);
		}

		[Fact]
		public void IsOtopUrl_DetectsOtopHost()
		{
			Assert.True(OtopRaceResultRepository.IsOtopUrl("https://web.otop.be/presenters/?event=CAPSS&n=x&k=y"));
			Assert.False(OtopRaceResultRepository.IsOtopUrl("https://www.acn-timing.com/#/events/1/ctx/a/generic/b/home/LIVE1"));
		}

		[Fact]
		public void IsOtopPayloadJson_DetectsResultsArray()
		{
			var otopJson = @"{ ""Event"": ""CAPSS"", ""Distance"": ""10 km"", ""Results"": [] }";
			var chronoraceJson = @"{ ""Groups"": [] }";

			Assert.True(OtopRaceResultRepository.IsOtopPayloadJson(otopJson));
			Assert.False(OtopRaceResultRepository.IsOtopPayloadJson(chronoraceJson));
			Assert.False(OtopRaceResultRepository.IsOtopPayloadJson(""));
		}

		[Fact]
		public void GetRaceResults_ConvertsPayloadToKeywordRows_AndMatchesMembers()
		{
			var payload = @"{
				""Event"": ""CAPSS"",
				""Distance"": ""10 km"",
				""DistanceKm"": 10,
				""Results"": [
					{
						""Position"": 1,
						""Bib"": ""101"",
						""LastName"": ""Dupont"",
						""FirstName"": ""Jean"",
						""Club"": ""Club Challenger"",
						""Sex"": ""M"",
						""Category"": ""SEH"",
						""CategoryPosition"": 1,
						""Time"": ""0:42:15"",
						""Speed"": ""14.2"",
						""Pace"": ""4:13""
					},
					{
						""Position"": 2,
						""Bib"": ""102"",
						""LastName"": ""Leclerc"",
						""FirstName"": ""Sophie"",
						""Club"": ""Running Team"",
						""Sex"": ""F"",
						""Category"": ""SEF"",
						""CategoryPosition"": 1,
						""Time"": ""0:48:00"",
						""Speed"": ""12.5"",
						""Pace"": ""4:48""
					}
				]
			}";

			var tempFile = Path.GetTempFileName();
			File.WriteAllText(tempFile, payload);

			try
			{
				var repository = new OtopRaceResultRepository(10);
				var results = repository.GetRaceResults(tempFile, _testMembers);

				// Header + 2 rows
				Assert.Equal(3, results.Count);

				var memberRow = results[2];
				Assert.StartsWith("TMEM;", memberRow);
				Assert.Contains("RACETIME;42:15;", memberRow);
				Assert.Contains("POS;1;", memberRow);
				Assert.Contains("SEX;M;", memberRow);
				Assert.Contains("CATEGORY;SEH;", memberRow);
				Assert.Contains("ISMEMBER;1;", memberRow);
				Assert.EndsWith("Jean;Dupont", memberRow);

				var nonMemberRow = results[3];
				Assert.StartsWith("TWINNER;", nonMemberRow);
				Assert.Contains("ISMEMBER;0;", nonMemberRow);
			}
			finally
			{
				File.Delete(tempFile);
			}
		}
	}
}
