using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace NameParser.Diagnostics
{
    /// <summary>
    /// Diagnostic tool to test ACN Timing API endpoints
    /// </summary>
    class AcnTimingApiTester
    {
        static async Task Main(string[] args)
        {
            var url = "https://www.acn-timing.com/?lng=FR#/events/2156215316811398/ctx/20260328_spa/generic/199034_1/home/LIVE1";

            Console.WriteLine("=== ACN Timing API Diagnostic Tool ===");
            Console.WriteLine($"Testing URL: {url}");
            Console.WriteLine();

            // Parse URL components
            var eventId = "2156215316811398";
            var context = "20260328_spa";
            var raceId = "199034_1";

            Console.WriteLine($"Extracted components:");
            Console.WriteLine($"  Event ID: {eventId}");
            Console.WriteLine($"  Context: {context}");
            Console.WriteLine($"  Race ID: {raceId}");
            Console.WriteLine();

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json, text/html, */*");
            httpClient.DefaultRequestHeaders.Add("Accept-Language", "fr-FR,fr;q=0.9,en;q=0.8");

            // Test various API endpoints
            var apiEndpoints = new[]
            {
                $"https://www.acn-timing.com/api/participants?eventId={eventId}&raceId={raceId}",
                $"https://www.acn-timing.com/api/participants?event={eventId}&race={raceId}",
                $"https://www.acn-timing.com/api/events/{eventId}/races/{raceId}/participants",
                $"https://www.acn-timing.com/api/events/{eventId}/participants/{raceId}",
                $"https://www.acn-timing.com/api/results?eventId={eventId}&raceId={raceId}",
                $"https://www.acn-timing.com/api/results/{eventId}/{raceId}",
                $"https://www.acn-timing.com/api/live/{eventId}/{raceId}",
                $"https://www.acn-timing.com/api/v1/events/{eventId}/results",
                $"https://api.acn-timing.com/events/{eventId}/races/{raceId}/results",
            };

            int successCount = 0;
            foreach (var endpoint in apiEndpoints)
            {
                Console.WriteLine($"Testing: {endpoint}");
                try
                {
                    var response = await httpClient.GetAsync(endpoint);
                    var statusCode = response.StatusCode;
                    Console.Write($"  Status: {(int)statusCode} {statusCode}");

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($" | Length: {content.Length} chars");

                        if (content.Length > 0 && (content.TrimStart().StartsWith("{") || content.TrimStart().StartsWith("[")))
                        {
                            Console.WriteLine("  ✓ SUCCESS! Got JSON response");
                            Console.WriteLine($"  Preview (first 500 chars): {content.Substring(0, Math.Min(500, content.Length))}");
                            Console.WriteLine();
                            successCount++;

                            // Save full response for analysis
                            var filename = $"acn_response_{successCount}.json";
                            System.IO.File.WriteAllText(filename, content);
                            Console.WriteLine($"  Full response saved to: {filename}");
                        }
                        else
                        {
                            Console.WriteLine("  ⚠ Response is empty or not JSON");
                        }
                    }
                    else
                    {
                        Console.WriteLine(" | Failed");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ✗ Exception: {ex.Message}");
                }
                Console.WriteLine();
            }

            Console.WriteLine($"\n=== Summary ===");
            Console.WriteLine($"Successful API calls: {successCount}/{apiEndpoints.Length}");

            if (successCount == 0)
            {
                Console.WriteLine("\n⚠ WARNING: No API endpoints responded successfully.");
                Console.WriteLine("Possible reasons:");
                Console.WriteLine("  1. ACN Timing may require authentication or special headers");
                Console.WriteLine("  2. The API structure may have changed");
                Console.WriteLine("  3. The race data may not be available via API");
                Console.WriteLine("  4. CORS or other security restrictions may be in place");
                Console.WriteLine("\nRecommendation:");
                Console.WriteLine("  - Open the URL in a browser with Developer Tools (F12)");
                Console.WriteLine("  - Go to Network tab");
                Console.WriteLine("  - Look for XHR/Fetch requests to find the actual API endpoint");
                Console.WriteLine("  - Check what parameters and headers are being sent");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
