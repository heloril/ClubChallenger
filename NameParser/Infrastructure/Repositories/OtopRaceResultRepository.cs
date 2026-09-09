using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using NameParser.Domain.Entities;
using NameParser.Domain.Repositories;
using NameParser.Infrastructure.Services;

namespace NameParser.Infrastructure.Repositories
{
	/// <summary>
	/// Repository for extracting race results from O'Top / RaceResult "RRPresenter" web
	/// classification pages (https://web.otop.be/presenters/?event=...&amp;n=...&amp;k=...).
	///
	/// O'Top pages expose several distances (e.g. 21.1 km, 10 km, 5 km) as separate "screens".
	/// Because the presenter URL is identical for every distance, this repository selects the
	/// distance whose parsed km value is nearest to the <see cref="_targetDistanceKm"/> supplied
	/// by the caller (the distance entered in the UI).
	///
	/// Results are converted to the same ";"-keyword row format produced by
	/// <c>AcnTimingRaceResultRepository.ParseChronoraceResults</c> so the rest of the pipeline
	/// can consume them unchanged.
	/// </summary>
	public class OtopRaceResultRepository : IRaceResultRepository
	{
		private readonly double _targetDistanceKm;

		public OtopRaceResultRepository(double targetDistanceKm)
		{
			_targetDistanceKm = targetDistanceKm;
		}

		public static bool IsOtopUrl(string input)
		{
			return !string.IsNullOrWhiteSpace(input) &&
				   input.IndexOf("otop.be", StringComparison.OrdinalIgnoreCase) >= 0;
		}

		public Dictionary<int, string> GetRaceResults(string urlOrFilePath, List<Member> members)
		{
			members ??= new List<Member>();

			if (IsUrl(urlOrFilePath))
			{
				var downloader = new OtopWebDownloader();
				var payloadJson = downloader.GetNearestDistancePayloadJson(urlOrFilePath, _targetDistanceKm);
				return ParsePayloadJson(payloadJson, members);
			}

			if (!File.Exists(urlOrFilePath))
			{
				throw new FileNotFoundException($"O'Top race results file not found: {urlOrFilePath}");
			}

			var content = File.ReadAllText(urlOrFilePath);
			return ParsePayloadJson(content, members);
		}

		/// <summary>
		/// Detects whether the given JSON content is an O'Top normalized payload
		/// (i.e. has a top-level "Results" array), as opposed to chronorace.be data.
		/// </summary>
		public static bool IsOtopPayloadJson(string content)
		{
			if (string.IsNullOrWhiteSpace(content))
				return false;

			var trimmed = content.TrimStart();
			if (!trimmed.StartsWith("{"))
				return false;

			try
			{
				using var doc = JsonDocument.Parse(content);
				return doc.RootElement.ValueKind == JsonValueKind.Object &&
					   doc.RootElement.TryGetProperty("Results", out var results) &&
					   results.ValueKind == JsonValueKind.Array;
			}
			catch (JsonException)
			{
				return false;
			}
		}

		private bool IsUrl(string input)
		{
			return input.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
				   input.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
		}

		private Dictionary<int, string> ParsePayloadJson(string payloadJson, List<Member> members)
		{
			var results = new Dictionary<int, string>();
			var rowId = 1;

			// Header row matching the format used by the other repositories.
			results[rowId++] = "Header;Position;Sex;Bib;Name;Country;Status;Time;Speed;Category;Category Position;";

			using var doc = JsonDocument.Parse(payloadJson);
			var root = doc.RootElement;

			if (!root.TryGetProperty("Results", out var resultsArray) ||
				resultsArray.ValueKind != JsonValueKind.Array)
			{
				return results;
			}

			var sexPositionCounters = new Dictionary<string, int>();

			foreach (var runner in resultsArray.EnumerateArray())
			{
				var row = ConvertRunner(runner, members, sexPositionCounters);
				if (row != null)
				{
					results[rowId++] = row;
				}
			}

			return results;
		}

		private string ConvertRunner(JsonElement runner, List<Member> members, Dictionary<string, int> sexPositionCounters)
		{
			string firstName = GetString(runner, "FirstName");
			string lastName = GetString(runner, "LastName");
			string club = GetString(runner, "Club");
			string sex = GetString(runner, "Sex");
			string category = GetString(runner, "Category");
			string timeStr = GetString(runner, "Time");
			string speedStr = GetString(runner, "Speed");

			int? position = GetInt(runner, "Position");
			int? categoryPosition = GetInt(runner, "CategoryPosition");

			var fullName = $"{lastName} {firstName}".Trim();
			if (string.IsNullOrWhiteSpace(fullName))
				return null;

			// Sex position (only counts finishers with a known sex).
			int? sexPosition = null;
			if (position.HasValue && position.Value > 0 && !string.IsNullOrWhiteSpace(sex))
			{
				if (!sexPositionCounters.ContainsKey(sex))
					sexPositionCounters[sex] = 0;
				sexPositionCounters[sex]++;
				sexPosition = sexPositionCounters[sex];
			}

			// Try to match with a member (reuse normalized comparison).
			var matchedMember = FindMatchingMember(members, fullName, firstName, lastName);
			bool isMember = matchedMember != null;
			if (matchedMember != null)
			{
				firstName = matchedMember.FirstName;
				lastName = matchedMember.LastName;
			}

			var raceTime = ParseTime(timeStr);
			double? speed = ParseSpeed(speedStr);

			var sb = new StringBuilder();
			sb.Append(isMember ? "TMEM;" : "TWINNER;");

			if (raceTime.HasValue)
				sb.Append($"RACETIME;{FormatTimeSpan(raceTime.Value)};");

			if (position.HasValue && position.Value > 0)
				sb.Append($"POS;{position.Value};");

			if (speed.HasValue)
				sb.Append($"SPEED;{speed.Value.ToString("0.0", CultureInfo.InvariantCulture)};");

			if (!string.IsNullOrWhiteSpace(sex))
				sb.Append($"SEX;{sex};");

			if (sexPosition.HasValue)
				sb.Append($"POSITIONSEX;{sexPosition.Value};");

			if (!string.IsNullOrWhiteSpace(category))
				sb.Append($"CATEGORY;{category};");

			if (categoryPosition.HasValue && categoryPosition.Value > 0)
				sb.Append($"POSITIONCAT;{categoryPosition.Value};");

			if (!string.IsNullOrWhiteSpace(club))
				sb.Append($"TEAM;{club};");

			sb.Append($"ISMEMBER;{(isMember ? "1" : "0")};");
			sb.Append($"{firstName};{lastName}");

			return sb.ToString();
		}

		private static string GetString(JsonElement element, string propertyName)
		{
			if (element.TryGetProperty(propertyName, out var prop))
			{
				if (prop.ValueKind == JsonValueKind.String)
					return prop.GetString() ?? string.Empty;
				if (prop.ValueKind == JsonValueKind.Number)
					return prop.ToString();
			}
			return string.Empty;
		}

		private static int? GetInt(JsonElement element, string propertyName)
		{
			if (element.TryGetProperty(propertyName, out var prop))
			{
				if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var value))
					return value;
				if (prop.ValueKind == JsonValueKind.String &&
					int.TryParse(prop.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
					return parsed;
			}
			return null;
		}

		private Member FindMatchingMember(List<Member> members, string fullName, string firstName, string lastName)
		{
			if (members == null || members.Count == 0)
				return null;

			if (!string.IsNullOrWhiteSpace(fullName))
			{
				var normalized = NormalizeName(fullName);
				foreach (var member in members)
				{
					var memberFullName = NormalizeName($"{member.FirstName} {member.LastName}");
					if (normalized.Equals(memberFullName, StringComparison.OrdinalIgnoreCase))
						return member;

					// O'Top orders names "LASTNAME Firstname"; also try that arrangement.
					var memberReversed = NormalizeName($"{member.LastName} {member.FirstName}");
					if (normalized.Equals(memberReversed, StringComparison.OrdinalIgnoreCase))
						return member;
				}
			}

			if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName))
			{
				foreach (var member in members)
				{
					if (NormalizeName(member.FirstName).Equals(NormalizeName(firstName), StringComparison.OrdinalIgnoreCase) &&
						NormalizeName(member.LastName).Equals(NormalizeName(lastName), StringComparison.OrdinalIgnoreCase))
						return member;
				}
			}

			return null;
		}

		private string NormalizeName(string name)
		{
			if (string.IsNullOrWhiteSpace(name))
				return string.Empty;

			var normalized = name.Normalize(NormalizationForm.FormD);
			var result = new StringBuilder();
			foreach (var ch in normalized)
			{
				if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
					result.Append(ch);
			}
			return Regex.Replace(result.ToString(), @"\s+", " ").Trim();
		}

		private TimeSpan? ParseTime(string timeString)
		{
			if (string.IsNullOrWhiteSpace(timeString) || timeString == "-")
				return null;

			timeString = timeString.Trim();

			if (TimeSpan.TryParse(timeString, CultureInfo.InvariantCulture, out TimeSpan result))
				return result;

			// Try custom format MM'SS"
			var customMatch = Regex.Match(timeString, @"(\d+)'(\d+)""");
			if (customMatch.Success)
			{
				int minutes = int.Parse(customMatch.Groups[1].Value);
				int seconds = int.Parse(customMatch.Groups[2].Value);
				return new TimeSpan(0, minutes, seconds);
			}

			return null;
		}

		private double? ParseSpeed(string speedString)
		{
			if (string.IsNullOrWhiteSpace(speedString) || speedString == "-")
				return null;

			var cleaned = Regex.Replace(speedString, @"[^\d.,]", string.Empty).Replace(',', '.');
			if (double.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out double speed) &&
				speed >= 0.0 && speed <= 60.0)
			{
				return speed;
			}
			return null;
		}

		private string FormatTimeSpan(TimeSpan time)
		{
			return time.Hours > 0
				? $"{(int)time.TotalHours}:{time.Minutes:D2}:{time.Seconds:D2}"
				: $"{time.Minutes}:{time.Seconds:D2}";
		}
	}
}
