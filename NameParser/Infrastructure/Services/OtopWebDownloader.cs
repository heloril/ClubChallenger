using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace NameParser.Infrastructure.Services
{
	/// <summary>
	/// Downloads race results from an O'Top / RaceResult "RRPresenter" web classification page
	/// (e.g. https://web.otop.be/presenters/?event=CAPSS&amp;n=...&amp;k=...).
	///
	/// These pages are JavaScript single-page apps: the runner data is NOT in the HTML.
	/// Instead it is served by a JSON API:
	///   - Config:  /api/presenter/config?eventid={event}&amp;name={n}&amp;key={k}&amp;lang=fr
	///   - Window:  /api/presenter/window?eventid={event}&amp;name={n}&amp;key={k}&amp;lang=fr&amp;screen={i}&amp;index={w}
	///
	/// Each contest/distance is exposed as a separate "screen". This downloader enumerates the
	/// screens from the config and writes ONE normalized results file per distance.
	/// </summary>
	public class OtopWebDownloader
	{
		private static readonly HttpClient _httpClient = new HttpClient();

		// Column indexes within each row of the window "Data" array.
		private const int ColBib = 0;
		private const int ColPosition = 2;
		private const int ColName = 3;
		private const int ColClub = 5;
		private const int ColSex = 6;
		private const int ColCategory = 8;
		private const int ColCategoryPosition = 9;
		private const int ColTime = 10;
		private const int ColSpeed = 12;
		private const int ColPace = 13;

		static OtopWebDownloader()
		{
			_httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
			_httpClient.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");
			_httpClient.DefaultRequestHeaders.Add("Accept-Language", "fr-FR,fr;q=0.9,en;q=0.8");
			_httpClient.Timeout = TimeSpan.FromSeconds(30);
		}

		/// <summary>
		/// Downloads every distance found on the given O'Top presenter page and writes one
		/// normalized JSON file per distance.
		/// </summary>
		/// <param name="url">The O'Top presenter page URL.</param>
		/// <param name="outputDirectory">Optional target directory. Defaults to current directory.</param>
		/// <returns>The list of files written (one per distance).</returns>
		public async Task<IReadOnlyList<string>> DownloadAllDistancesAsync(string url, string outputDirectory = null)
		{
			if (string.IsNullOrWhiteSpace(url))
				throw new ArgumentException("URL cannot be empty", nameof(url));

			var info = ParsePresenterUrl(url);
			outputDirectory ??= Directory.GetCurrentDirectory();
			Directory.CreateDirectory(outputDirectory);

			var config = await GetConfigAsync(info);
			var screens = config.RootElement.GetProperty("Screens");

			var eventTitle = config.RootElement.TryGetProperty("Title", out var titleEl)
				? titleEl.GetString()
				: info.EventId;

			var writtenFiles = new List<string>();

			for (int screenIndex = 0; screenIndex < screens.GetArrayLength(); screenIndex++)
			{
				var screen = screens[screenIndex];

				if (screen.TryGetProperty("Disabled", out var disabledEl) && disabledEl.GetBoolean())
					continue;

				var distanceTitle = screen.TryGetProperty("Title", out var scrTitleEl)
					? scrTitleEl.GetString()
					: $"screen{screenIndex}";

				var listWindowIndex = FindResultListWindowIndex(screen);
				if (listWindowIndex < 0)
				{
					Console.WriteLine($"  Skipping screen {screenIndex} ('{distanceTitle}') - no results list found.");
					continue;
				}

				var results = await GetResultsAsync(info, screenIndex, listWindowIndex);

				var fileName = BuildFileName(eventTitle, distanceTitle);
				var filePath = Path.Combine(outputDirectory, fileName);

				var payload = new
				{
					Event = eventTitle,
					Distance = distanceTitle,
					Source = url,
					DownloadedAt = DateTime.Now,
					Count = results.Count,
					Results = results
				};

				var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
				{
					WriteIndented = true,
					Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
				});

				await File.WriteAllTextAsync(filePath, json, new UTF8Encoding(false));
				Console.WriteLine($"  Distance '{distanceTitle}': {results.Count} runners -> {fileName}");
				writtenFiles.Add(filePath);
			}

			return writtenFiles;
		}

		/// <summary>Synchronous wrapper for <see cref="DownloadAllDistancesAsync"/>.</summary>
		public IReadOnlyList<string> DownloadAllDistances(string url, string outputDirectory = null)
		{
			return DownloadAllDistancesAsync(url, outputDirectory).GetAwaiter().GetResult();
		}

		/// <summary>
		/// Enumerates the distances (screens) available on the given O'Top presenter page.
		/// Each entry carries the screen title, the parsed distance in km (0 if not parseable),
		/// and the indices needed to fetch its results.
		/// </summary>
		public async Task<IReadOnlyList<OtopDistanceInfo>> GetDistancesAsync(string url)
		{
			if (string.IsNullOrWhiteSpace(url))
				throw new ArgumentException("URL cannot be empty", nameof(url));

			var info = ParsePresenterUrl(url);
			using var config = await GetConfigAsync(info);
			var screens = config.RootElement.GetProperty("Screens");

			var eventTitle = config.RootElement.TryGetProperty("Title", out var titleEl)
				? titleEl.GetString()
				: info.EventId;

			var distances = new List<OtopDistanceInfo>();

			for (int screenIndex = 0; screenIndex < screens.GetArrayLength(); screenIndex++)
			{
				var screen = screens[screenIndex];

				if (screen.TryGetProperty("Disabled", out var disabledEl) && disabledEl.GetBoolean())
					continue;

				var distanceTitle = screen.TryGetProperty("Title", out var scrTitleEl)
					? scrTitleEl.GetString()
					: $"screen{screenIndex}";

				var listWindowIndex = FindResultListWindowIndex(screen);
				if (listWindowIndex < 0)
					continue;

				distances.Add(new OtopDistanceInfo
				{
					EventTitle = eventTitle,
					Title = distanceTitle,
					Km = ParseDistanceKm(distanceTitle),
					ScreenIndex = screenIndex,
					ListWindowIndex = listWindowIndex
				});
			}

			return distances;
		}

		/// <summary>
		/// Downloads the results for the distance whose parsed km value is nearest to
		/// <paramref name="targetKm"/> and returns a normalized payload JSON string
		/// (same shape as the per-distance files produced by <see cref="DownloadAllDistancesAsync"/>).
		/// </summary>
		public async Task<string> GetNearestDistancePayloadJsonAsync(string url, double targetKm)
		{
			var distances = await GetDistancesAsync(url);
			if (distances.Count == 0)
				throw new InvalidOperationException("No distances with a results list were found on the O'Top page.");

			var info = ParsePresenterUrl(url);

			// Prefer distances with a parseable km value; pick the nearest to the target.
			var parseable = distances.Where(d => d.Km > 0).ToList();
			OtopDistanceInfo selected = parseable.Count > 0
				? parseable.OrderBy(d => Math.Abs(d.Km - targetKm)).First()
				: distances[0];

			var results = await GetResultsAsync(info, selected.ScreenIndex, selected.ListWindowIndex);

			var payload = new
			{
				Event = selected.EventTitle,
				Distance = selected.Title,
				DistanceKm = selected.Km,
				Source = url,
				DownloadedAt = DateTime.Now,
				Count = results.Count,
				Results = results
			};

			return JsonSerializer.Serialize(payload, new JsonSerializerOptions
			{
				WriteIndented = true,
				Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
			});
		}

		/// <summary>Synchronous wrapper for <see cref="GetNearestDistancePayloadJsonAsync"/>.</summary>
		public string GetNearestDistancePayloadJson(string url, double targetKm)
		{
			return GetNearestDistancePayloadJsonAsync(url, targetKm).GetAwaiter().GetResult();
		}

		/// <summary>
		/// Parses a distance in km from an O'Top screen title such as "21,1 km", "10 km",
		/// "Semi-marathon 21.1km" or "5 KM". Returns 0 if no number can be found.
		/// </summary>
		public static double ParseDistanceKm(string title)
		{
			if (string.IsNullOrWhiteSpace(title))
				return 0;

			// First number in the title, allowing comma or dot as decimal separator.
			var match = System.Text.RegularExpressions.Regex.Match(title, @"(\d+(?:[.,]\d+)?)");
			if (!match.Success)
				return 0;

			var normalized = match.Groups[1].Value.Replace(',', '.');
			return double.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var km) ? km : 0;
		}

		private async Task<JsonDocument> GetConfigAsync(OtopPresenterInfo info)
		{
			var url = $"{info.BaseUrl}/api/presenter/config" +
					  $"?eventid={Uri.EscapeDataString(info.EventId)}" +
					  $"&name={Uri.EscapeDataString(info.Name)}" +
					  $"&key={Uri.EscapeDataString(info.Key)}" +
					  $"&lang=fr";

			var content = await _httpClient.GetStringAsync(url);
			return JsonDocument.Parse(content);
		}

		private async Task<List<OtopRunnerResult>> GetResultsAsync(OtopPresenterInfo info, int screenIndex, int windowIndex)
		{
			var url = $"{info.BaseUrl}/api/presenter/window" +
					  $"?eventid={Uri.EscapeDataString(info.EventId)}" +
					  $"&name={Uri.EscapeDataString(info.Name)}" +
					  $"&key={Uri.EscapeDataString(info.Key)}" +
					  $"&lang=fr&screen={screenIndex}&index={windowIndex}";

			var content = await _httpClient.GetStringAsync(url);
			using var doc = JsonDocument.Parse(content);

			var results = new List<OtopRunnerResult>();

			// The runner rows live in a top-level "Data" array (a sibling of "List"),
			// each row being an array of string cells. Older responses nested it under "List".
			if (!TryGetDataArray(doc.RootElement, out var data))
			{
				return results;
			}

			foreach (var row in data.EnumerateArray())
			{
				if (row.ValueKind != JsonValueKind.Array)
					continue;

				var cells = row.EnumerateArray().Select(c => c.GetString() ?? string.Empty).ToArray();
				var result = MapRow(cells);
				if (result != null)
					results.Add(result);
			}

			return results;
		}

		private static bool TryGetDataArray(JsonElement root, out JsonElement data)
		{
			// Preferred: top-level "Data" array.
			if (root.TryGetProperty("Data", out data) && data.ValueKind == JsonValueKind.Array)
				return true;

			// Fallback: legacy structure with "List": { "Data": [...] }.
			if (root.TryGetProperty("List", out var list) &&
				list.ValueKind == JsonValueKind.Object &&
				list.TryGetProperty("Data", out data) &&
				data.ValueKind == JsonValueKind.Array)
				return true;

			data = default;
			return false;
		}

		private static OtopRunnerResult MapRow(string[] cells)
		{
			string Cell(int index) => index < cells.Length ? cells[index].Trim() : string.Empty;

			var name = Cell(ColName);
			if (string.IsNullOrWhiteSpace(name))
				return null;

			var (lastName, firstName) = SplitName(name);

			return new OtopRunnerResult
			{
				Position = ParseInt(Cell(ColPosition)),
				Bib = Cell(ColBib),
				LastName = lastName,
				FirstName = firstName,
				Club = Cell(ColClub),
				Sex = Cell(ColSex),
				Category = Cell(ColCategory),
				CategoryPosition = ParseInt(Cell(ColCategoryPosition)),
				Time = Cell(ColTime),
				Speed = Cell(ColSpeed),
				Pace = Cell(ColPace)
			};
		}

		/// <summary>
		/// O'Top names are formatted as "LASTNAME Firstname" where the surname is the leading
		/// run of UPPERCASE words. Everything after is the first name.
		/// </summary>
		private static (string LastName, string FirstName) SplitName(string full)
		{
			var tokens = full.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			var lastParts = new List<string>();
			var firstParts = new List<string>();

			bool inLastName = true;
			foreach (var token in tokens)
			{
				if (inLastName && IsUpperCaseToken(token))
				{
					lastParts.Add(token);
				}
				else
				{
					inLastName = false;
					firstParts.Add(token);
				}
			}

			// Fallback: if we could not detect a first name, treat last token as first name.
			if (firstParts.Count == 0 && lastParts.Count > 1)
			{
				firstParts.Add(lastParts[^1]);
				lastParts.RemoveAt(lastParts.Count - 1);
			}

			return (string.Join(" ", lastParts), string.Join(" ", firstParts));
		}

		private static bool IsUpperCaseToken(string token)
		{
			var letters = token.Where(char.IsLetter).ToArray();
			if (letters.Length == 0)
				return false;
			return letters.All(char.IsUpper);
		}

		private static int FindResultListWindowIndex(JsonElement screen)
		{
			if (!screen.TryGetProperty("Windows", out var windows) || windows.ValueKind != JsonValueKind.Array)
				return -1;

			int index = 0;
			int lastListIndex = -1;
			foreach (var window in windows.EnumerateArray())
			{
				var itemType = window.TryGetProperty("ItemType", out var it) ? it.GetString() : null;
				if (string.Equals(itemType, "list", StringComparison.OrdinalIgnoreCase))
				{
					var itemName = window.TryGetProperty("ItemName", out var inEl) ? inEl.GetString() ?? string.Empty : string.Empty;
					// The results list is a full classification list, not the title banner.
					if (!itemName.Contains("Titre", StringComparison.OrdinalIgnoreCase))
						lastListIndex = index;
				}
				index++;
			}

			return lastListIndex;
		}

		private static int ParseInt(string value)
		{
			return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : 0;
		}

		private static string BuildFileName(string eventTitle, string distanceTitle)
		{
			var safeEvent = SanitizeFileName(eventTitle);
			var safeDistance = SanitizeFileName(distanceTitle);
			return $"Otop_{safeEvent}_{safeDistance}.json";
		}

		private static string SanitizeFileName(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return "results";

			var invalid = Path.GetInvalidFileNameChars();
			var cleaned = new string(value.Where(c => !invalid.Contains(c)).ToArray());
			cleaned = cleaned.Replace(' ', '_').Replace(',', '.').Trim('_');
			return cleaned.Length > 50 ? cleaned.Substring(0, 50) : cleaned;
		}

		private static OtopPresenterInfo ParsePresenterUrl(string url)
		{
			var uri = new Uri(url);
			var query = HttpUtility.ParseQueryString(uri.Query);

			var eventId = query["event"] ?? query["eventid"];
			var name = query["n"] ?? query["name"];
			var key = query["k"] ?? query["key"];

			if (string.IsNullOrWhiteSpace(eventId) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentException(
					"Invalid O'Top presenter URL. Expected query parameters: event, n and k " +
					"(e.g. https://web.otop.be/presenters/?event=CAPSS&n=...&k=...).");
			}

			return new OtopPresenterInfo
			{
				BaseUrl = $"{uri.Scheme}://{uri.Host}",
				EventId = eventId,
				Name = name,
				Key = key
			};
		}

		private class OtopPresenterInfo
		{
			public string BaseUrl { get; set; }
			public string EventId { get; set; }
			public string Name { get; set; }
			public string Key { get; set; }
		}
	}

	/// <summary>
	/// Describes one distance (screen) available on an O'Top presenter page.
	/// </summary>
	public class OtopDistanceInfo
	{
		public string EventTitle { get; set; }
		public string Title { get; set; }
		public double Km { get; set; }
		public int ScreenIndex { get; set; }
		public int ListWindowIndex { get; set; }
	}

	/// <summary>
	/// A single normalized runner result parsed from an O'Top web classification.
	/// </summary>
	public class OtopRunnerResult
	{
		public int Position { get; set; }
		public string Bib { get; set; }
		public string LastName { get; set; }
		public string FirstName { get; set; }
		public string Club { get; set; }
		public string Sex { get; set; }
		public string Category { get; set; }
		public int CategoryPosition { get; set; }
		public string Time { get; set; }
		public string Speed { get; set; }
		public string Pace { get; set; }
	}
}
