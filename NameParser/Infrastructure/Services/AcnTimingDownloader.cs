using System;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NameParser.Infrastructure.Services
{
    /// <summary>
    /// Utility service for downloading and caching race results from ACN Timing website.
    /// Downloads HTML or JSON data and saves it locally for offline processing.
    /// </summary>
    public class AcnTimingDownloader
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        static AcnTimingDownloader()
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json, text/html, */*");
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "fr-FR,fr;q=0.9,en;q=0.8");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// Downloads race results from an ACN Timing URL and saves to a local file.
        /// </summary>
        /// <param name="url">ACN Timing race results URL</param>
        /// <param name="outputPath">Optional output file path. If not specified, generates filename from URL.</param>
        /// <returns>Path to the saved file</returns>
        public async Task<string> DownloadRaceResultsAsync(string url, string outputPath = null)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL cannot be empty", nameof(url));

            try
            {
                // Extract race info from URL for filename generation
                var urlInfo = ParseAcnTimingUrl(url);

                // Try API endpoints first
                var apiContent = await TryDownloadFromApi(urlInfo);
                if (apiContent != null)
                {
                    var jsonPath = outputPath ?? GenerateFileName(urlInfo, ".json");
                    await File.WriteAllTextAsync(jsonPath, apiContent);
                    Console.WriteLine($"Downloaded JSON results to: {jsonPath}");
                    return jsonPath;
                }

                // Fallback to HTML download
                var htmlContent = await _httpClient.GetStringAsync(url);
                var htmlPath = outputPath ?? GenerateFileName(urlInfo, ".html");
                await File.WriteAllTextAsync(htmlPath, htmlContent);
                Console.WriteLine($"Downloaded HTML results to: {htmlPath}");
                return htmlPath;
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Failed to download race results from: {url}", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new Exception($"Request timed out while downloading from: {url}", ex);
            }
        }

        /// <summary>
        /// Downloads race results synchronously.
        /// </summary>
        public string DownloadRaceResults(string url, string outputPath = null)
        {
            return DownloadRaceResultsAsync(url, outputPath).GetAwaiter().GetResult();
        }

        private async Task<string> TryDownloadFromApi(AcnTimingUrlInfo urlInfo)
        {
            try
            {
                // Common ACN Timing API endpoint patterns
                var apiUrls = new[]
                {
                    $"https://www.acn-timing.com/api/events/{urlInfo.EventId}/races/{urlInfo.RaceId}/results",
                    $"https://www.acn-timing.com/api/results/{urlInfo.RaceId}",
                    $"https://www.acn-timing.com/api/v1/events/{urlInfo.EventId}/results",
                    $"https://api.acn-timing.com/events/{urlInfo.EventId}/races/{urlInfo.RaceId}/results",
                    $"https://www.acn-timing.com/api/events/{urlInfo.EventId}/ctx/{urlInfo.Context}/generic/{urlInfo.RaceId}/results"
                };

                foreach (var apiUrl in apiUrls)
                {
                    try
                    {
                        Console.WriteLine($"Trying API endpoint: {apiUrl}");
                        var response = await _httpClient.GetAsync(apiUrl);
                        if (response.IsSuccessStatusCode)
                        {
                            var content = await response.Content.ReadAsStringAsync();
                            // Verify it's valid JSON
                            if (content.TrimStart().StartsWith("{") || content.TrimStart().StartsWith("["))
                            {
                                Console.WriteLine("✓ API endpoint successful!");
                                return content;
                            }
                        }
                    }
                    catch
                    {
                        // Continue to next URL
                    }
                }

                Console.WriteLine("No API endpoint available, falling back to HTML...");
                return null;
            }
            catch
            {
                return null;
            }
        }

        private AcnTimingUrlInfo ParseAcnTimingUrl(string url)
        {
            // Example: https://www.acn-timing.com/?lng=FR#/events/2156215316811398/ctx/20260328_spa/generic/198022_3/home/LIVEMARA3
            var match = Regex.Match(url, @"events/([^/]+)/ctx/([^/]+)/generic/([^/]+)");
            if (!match.Success)
            {
                // Try alternative patterns
                match = Regex.Match(url, @"events/([^/]+).*?/(\d+)");
                if (!match.Success)
                {
                    throw new ArgumentException("Invalid ACN Timing URL format. Expected format: .../events/{eventId}/ctx/{context}/generic/{raceId}/...");
                }
            }

            return new AcnTimingUrlInfo
            {
                EventId = match.Groups[1].Value,
                Context = match.Groups.Count > 2 ? match.Groups[2].Value : "unknown",
                RaceId = match.Groups.Count > 3 ? match.Groups[3].Value : match.Groups[2].Value
            };
        }

        private string GenerateFileName(AcnTimingUrlInfo urlInfo, string extension)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var safeName = SanitizeFileName(urlInfo.Context);
            return $"ACN_{safeName}_{urlInfo.RaceId}_{timestamp}{extension}";
        }

        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "results";

            var invalid = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", fileName.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
            return sanitized.Length > 50 ? sanitized.Substring(0, 50) : sanitized;
        }

        private class AcnTimingUrlInfo
        {
            public string EventId { get; set; }
            public string Context { get; set; }
            public string RaceId { get; set; }
        }
    }
}
