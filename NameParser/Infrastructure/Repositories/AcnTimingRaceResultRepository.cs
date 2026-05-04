using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NameParser.Domain.Entities;
using NameParser.Domain.Repositories;

namespace NameParser.Infrastructure.Repositories
{
    /// <summary>
    /// Repository for extracting race results from ACN Timing website.
    /// Supports both HTML parsing and REST API access.
    /// ACN Timing URLs format: https://www.acn-timing.com/?lng=FR#/events/{eventId}/ctx/{date}_{location}/generic/{raceId}/home/LIVEMARA3
    /// </summary>
    public class AcnTimingRaceResultRepository : IRaceResultRepository
    {
        private const string Separator = ";";
        private const int RaceTimeThresholdMinutes = 15;
        private static readonly HttpClient _httpClient = new HttpClient();

        static AcnTimingRaceResultRepository()
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json, text/html, */*");
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "fr-FR,fr;q=0.9,en;q=0.8");
        }

        public Dictionary<int, string> GetRaceResults(string urlOrFilePath, List<Member> members)
        {
            // Determine if input is a URL or file path
            if (IsUrl(urlOrFilePath))
            {
                return GetRaceResultsFromUrl(urlOrFilePath, members).GetAwaiter().GetResult();
            }
            else
            {
                // Assume it's a cached HTML or JSON file
                return GetRaceResultsFromFile(urlOrFilePath, members);
            }
        }

        private bool IsUrl(string input)
        {
            return input.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                   input.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<Dictionary<int, string>> GetRaceResultsFromUrl(string url, List<Member> members)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Processing ACN Timing URL: {url}");

                // Extract event and race information from URL
                var urlInfo = ParseAcnTimingUrl(url);

                // Try API endpoint first (ACN Timing often has JSON endpoints)
                System.Diagnostics.Debug.WriteLine("Attempting to fetch results from API...");
                var apiResults = await TryGetResultsFromApi(urlInfo);
                if (apiResults != null)
                {
                    System.Diagnostics.Debug.WriteLine("Successfully retrieved results from API, parsing JSON...");
                    var parsedResults = ParseJsonResults(apiResults, members);
                    System.Diagnostics.Debug.WriteLine($"Parsed {parsedResults.Count} rows from JSON");

                    if (parsedResults.Count > 1) // More than just header
                    {
                        return parsedResults;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Warning: JSON parsed but no data rows found");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("API fetch failed or returned null");
                }

                // Fallback to HTML parsing (though this won't work well for SPAs)
                System.Diagnostics.Debug.WriteLine("Attempting to fetch HTML directly (may not work for SPA)...");
                var htmlContent = await _httpClient.GetStringAsync(url);
                System.Diagnostics.Debug.WriteLine($"Retrieved HTML content length: {htmlContent.Length}");

                var htmlResults = ParseHtmlResults(htmlContent, members);
                System.Diagnostics.Debug.WriteLine($"Parsed {htmlResults.Count} rows from HTML");

                if (htmlResults.Count <= 1)
                {
                    throw new Exception(
                        "No race results found. This might be because:\n" +
                        "1. The ACN Timing API structure has changed\n" +
                        "2. The race data is not yet available\n" +
                        "3. The URL format is incorrect\n" +
                        $"Event ID: {urlInfo.EventId}, Race ID: {urlInfo.RaceId}\n" +
                        "Try accessing the URL in a browser to verify the data is available.");
                }

                return htmlResults;
            }
            catch (ArgumentException)
            {
                throw; // Re-throw URL parsing errors
            }
            catch (HttpRequestException httpEx)
            {
                throw new Exception($"Network error while fetching race results from ACN Timing: {httpEx.Message}", httpEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to fetch race results from ACN Timing URL: {url}\nError: {ex.Message}", ex);
            }
        }

        private Dictionary<int, string> GetRaceResultsFromFile(string filePath, List<Member> members)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Race results file not found: {filePath}");
            }

            var content = File.ReadAllText(filePath);

            // Try to parse as JSON first
            if (filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase) || content.TrimStart().StartsWith("{") || content.TrimStart().StartsWith("["))
            {
                return ParseJsonResults(content, members);
            }

            // Otherwise parse as HTML
            return ParseHtmlResults(content, members);
        }

        private AcnTimingUrlInfo ParseAcnTimingUrl(string url)
        {
            // Check if this is a direct chronorace.be API URL
            // Example: https://results.chronorace.be/api/results/table/search/20260328_spa/LIVE1?srch=&pageSize=1000
            var chronoraceMatch = Regex.Match(url, @"results\.chronorace\.be/api/results/table/search/([^/\?]+)/([^/\?]+)");
            if (chronoraceMatch.Success)
            {
                var chronoraceUrlInfo = new AcnTimingUrlInfo
                {
                    EventId = "",
                    Context = chronoraceMatch.Groups[1].Value,
                    RaceId = "",
                    ViewId = chronoraceMatch.Groups[2].Value
                };

                System.Diagnostics.Debug.WriteLine($"Parsed chronorace.be URL Info - Context: {chronoraceUrlInfo.Context}, ViewId: {chronoraceUrlInfo.ViewId}");
                return chronoraceUrlInfo;
            }

            // Example: https://www.acn-timing.com/?lng=FR#/events/2156215316811398/ctx/20260328_spa/generic/199034_1/home/LIVE1
            // Extract the fragment part after the #
            var fragmentMatch = Regex.Match(url, @"#(.+)");
            string urlToParse = fragmentMatch.Success ? fragmentMatch.Groups[1].Value : url;

            var match = Regex.Match(urlToParse, @"events/([^/]+)/ctx/([^/]+)/generic/([^/]+)/home/([^/]+)");
            if (!match.Success)
            {
                throw new ArgumentException($"Invalid ACN Timing URL format. Expected format: " +
                    $"https://www.acn-timing.com/#/events/{{eventId}}/ctx/{{context}}/generic/{{raceId}}/home/{{view}} " +
                    $"or https://results.chronorace.be/api/results/table/search/{{context}}/{{viewId}}. " +
                    $"Received: {url}");
            }

            var urlInfo = new AcnTimingUrlInfo
            {
                EventId = match.Groups[1].Value,
                Context = match.Groups[2].Value,
                RaceId = match.Groups[3].Value,
                ViewId = match.Groups[4].Value
            };

            System.Diagnostics.Debug.WriteLine($"Parsed URL Info - EventId: {urlInfo.EventId}, Context: {urlInfo.Context}, RaceId: {urlInfo.RaceId}, ViewId: {urlInfo.ViewId}");

            return urlInfo;
        }

        private async Task<string> TryGetResultsFromApi(AcnTimingUrlInfo urlInfo)
        {
            try
            {
                // ACN Timing uses chronorace.be API backend
                // API endpoint pattern: https://results.chronorace.be/api/results/table/search/{context}/{viewId}?srch=&pageSize=1000
                // Example: https://results.chronorace.be/api/results/table/search/20260328_spa/LIVE1?srch=&pageSize=1000

                var apiUrls = new[]
                {
                    // Primary chronorace.be API endpoint
                    // pageSize=10000 ensures all participants are fetched (some races exceed 2000 entries)
                    $"https://results.chronorace.be/api/results/table/search/{urlInfo.Context}/{urlInfo.ViewId}?srch=&pageSize=10000",

                    // Fallback patterns if the primary doesn't work
                    $"https://results.chronorace.be/api/results/table/search/{urlInfo.Context}/{urlInfo.RaceId}?srch=&pageSize=10000",
                    $"https://results.chronorace.be/api/results/{urlInfo.Context}/{urlInfo.ViewId}",
                };

                foreach (var apiUrl in apiUrls)
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"Trying API URL: {apiUrl}");
                        var response = await _httpClient.GetAsync(apiUrl);

                        if (response.IsSuccessStatusCode)
                        {
                            var content = await response.Content.ReadAsStringAsync();

                            // Verify we got actual data (not an empty response)
                            if (!string.IsNullOrWhiteSpace(content) && 
                                (content.TrimStart().StartsWith("{") || content.TrimStart().StartsWith("[")))
                            {
                                System.Diagnostics.Debug.WriteLine($"SUCCESS: Got data from {apiUrl}");
                                System.Diagnostics.Debug.WriteLine($"Response length: {content.Length} characters");
                                return content;
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed: HTTP {response.StatusCode} - {apiUrl}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Exception for {apiUrl}: {ex.Message}");
                        // Continue to next URL
                    }
                }

                System.Diagnostics.Debug.WriteLine("All API attempts failed, returning null");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TryGetResultsFromApi exception: {ex.Message}");
                return null;
            }
        }

        private Dictionary<int, string> ParseJsonResults(string jsonContent, List<Member> members)
        {
            var results = new Dictionary<int, string>();
            var rowId = 1;

            try
            {
                using (JsonDocument doc = JsonDocument.Parse(jsonContent))
                {
                    JsonElement root = doc.RootElement;

                    // Handle different JSON structures
                    JsonElement resultsArray;

                    // Check if root is an array first (before trying to access properties)
                    if (root.ValueKind == JsonValueKind.Array)
                    {
                        resultsArray = root;
                    }
                    // chronorace.be structure: { "Groups": [{ "SlaveRows": [[...], [...]] }] }
                    else if (root.TryGetProperty("Groups", out var groupsProperty))
                    {
                        // Check if Groups is null (no results yet)
                        if (groupsProperty.ValueKind == JsonValueKind.Null)
                        {
                            System.Diagnostics.Debug.WriteLine("chronorace.be Groups is null - no results available yet");
                            // Return results with header and a message row
                            results[rowId++] = "Header;Position;Sex;Bib;Name;Country;Status;Time;Speed;Category;Category Position;";
                            results[rowId++] = "No results available yet";
                            return results;
                        }

                        // Check if Groups is an array with data
                        if (groupsProperty.ValueKind == JsonValueKind.Array && groupsProperty.GetArrayLength() > 0)
                        {
                            var firstGroup = groupsProperty[0];
                            if (firstGroup.TryGetProperty("SlaveRows", out var slaveRowsProperty))
                            {
                                System.Diagnostics.Debug.WriteLine($"Found chronorace.be SlaveRows structure with {slaveRowsProperty.GetArrayLength()} rows");
                                return ParseChronoraceResults(slaveRowsProperty, members);
                            }
                        }

                        // Groups exists but is not in expected format
                        throw new Exception("chronorace.be Groups property found but in unexpected format");
                    }
                    else if (root.TryGetProperty("rows", out var rowsProperty))
                    {
                        resultsArray = rowsProperty;
                    }
                    else if (root.TryGetProperty("results", out var resultsProperty))
                    {
                        resultsArray = resultsProperty;
                    }
                    else if (root.TryGetProperty("data", out var dataProperty))
                    {
                        resultsArray = dataProperty;
                    }
                    else if (root.TryGetProperty("participants", out var participantsProperty))
                    {
                        resultsArray = participantsProperty;
                    }
                    else
                    {
                        throw new Exception("Unable to find results array in JSON response");
                    }

                    // Add header
                    results[rowId++] = "Header;Generic JSON results;";

                    foreach (JsonElement item in resultsArray.EnumerateArray())
                    {
                        var parsedResult = ParseJsonResultItem(item, members);
                        if (parsedResult != null)
                        {
                            results[rowId++] = parsedResult;
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                throw new Exception("Failed to parse JSON results", ex);
            }

            return results;
        }

        private string ParseJsonResultItem(JsonElement item, List<Member> members)
        {
            try
            {
                // Extract common fields (field names may vary)
                var position = GetJsonInt(item, "position", "rank", "place", "pos", "classement");
                var firstName = GetJsonString(item, "firstName", "firstname", "prenom", "prénom", "first_name");
                var lastName = GetJsonString(item, "lastName", "lastname", "nom", "name", "last_name");
                var fullName = GetJsonString(item, "fullName", "fullname", "name", "participant");
                var team = GetJsonString(item, "team", "club", "equipe", "équipe");
                var timeStr = GetJsonString(item, "time", "temps", "duration", "chrono", "chipTime", "chip_time");
                var paceStr = GetJsonString(item, "pace", "avgPace", "avg_pace", "timePerKm", "time_per_km");
                var speedStr = GetJsonString(item, "speed", "vitesse", "avgSpeed", "avg_speed");
                var sex = GetJsonString(item, "sex", "sexe", "gender", "genre");
                var sexPosition = GetJsonInt(item, "sexPosition", "sex_position", "positionSexe", "position_sexe", "rankSex");
                var category = GetJsonString(item, "category", "catégorie", "categorie", "ageCategory", "age_category");
                var categoryPosition = GetJsonInt(item, "categoryPosition", "category_position", "positionCategorie", "position_categorie", "rankCategory");

                // Build full name if not provided
                if (string.IsNullOrWhiteSpace(fullName) && !string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName))
                {
                    fullName = $"{firstName} {lastName}";
                }

                // Try to match with member
                var matchedMember = FindMatchingMember(members, fullName, firstName, lastName);
                bool isMember = matchedMember != null;

                if (matchedMember != null && string.IsNullOrWhiteSpace(firstName))
                {
                    firstName = matchedMember.FirstName;
                    lastName = matchedMember.LastName;
                }

                // Parse times
                TimeSpan? raceTime = ParseTime(timeStr);
                TimeSpan? paceTime = ParseTime(paceStr);
                double? speed = ParseSpeed(speedStr);

                // Build result string in keyword format matching ParseChronoraceResults output
                // so that RaceProcessingService.ParseRaceResult can process it correctly.
                var resultBuilder = new System.Text.StringBuilder();

                resultBuilder.Append(isMember ? "TMEM;" : "TWINNER;");

                if (raceTime.HasValue)
                {
                    resultBuilder.Append($"RACETIME;{FormatTimeSpan(raceTime)};");
                }

                if (paceTime.HasValue)
                {
                    resultBuilder.Append($"TIMEPERKM;{FormatTimeSpan(paceTime)};");
                }

                if (position.HasValue)
                {
                    resultBuilder.Append($"POS;{position};");
                }

                if (speed.HasValue)
                {
                    resultBuilder.Append($"SPEED;{FormatSpeed(speed)};");
                }

                if (!string.IsNullOrWhiteSpace(sex))
                {
                    resultBuilder.Append($"SEX;{sex};");
                }

                if (sexPosition.HasValue)
                {
                    resultBuilder.Append($"POSITIONSEX;{sexPosition};");
                }

                if (!string.IsNullOrWhiteSpace(category))
                {
                    resultBuilder.Append($"CATEGORY;{category};");
                }

                if (categoryPosition.HasValue)
                {
                    resultBuilder.Append($"POSITIONCAT;{categoryPosition};");
                }

                if (!string.IsNullOrWhiteSpace(team))
                {
                    resultBuilder.Append($"TEAM;{team};");
                }

                resultBuilder.Append($"ISMEMBER;{(isMember ? "1" : "0")};");
                resultBuilder.Append($"{firstName};{lastName}");

                return resultBuilder.ToString();
            }
            catch
            {
                return null;
            }
        }

        private Dictionary<int, string> ParseChronoraceResults(JsonElement slaveRowsArray, List<Member> members)
        {
            var results = new Dictionary<int, string>();
            var rowId = 1;

            // Add header
            results[rowId++] = "Header;Position;Sex;Bib;Name;Country;Status;Time;Speed;Category;Category Position;";

            // chronorace.be structure: SlaveRows is an array of arrays
            // There are two known formats:
            // 1. HTML format: [position, sex, bib, name_html, country, sex2, ?, ?, status, time_html, category_position, category, detail, style, bib2]
            //    Example: ["46.","M","411","<b>DE VOS Nicolas</b><br/><small></small>","BEL","M","","","Finish","<b>1:36:20</b><br/><small>  13.4km/h</small>","12","V1H","detail:411_3","","411"]
            // 2. Clean format: [position, bib, name, team, country, sex, ?, ?, ?, status, time, ?, speed, category_position, category, link, ?, ?, ?, ?]
            //    Example: ["1.","14263","ZALESKI Jules","TEAMULIEGE","BEL","M","","TEAMULIEGE","TEAMULIEGE","Finish","3:28:45","-","13.250","1","SEH","LINK:...","","14263","diplome.gif","..."]

            // Track sex position by counting participants of each sex (only for finishers)
            var sexPositionCounters = new Dictionary<string, int>();

            foreach (JsonElement row in slaveRowsArray.EnumerateArray())
            {
                if (row.ValueKind != JsonValueKind.Array)
                    continue;

                var cells = new List<string>();
                foreach (JsonElement cell in row.EnumerateArray())
                {
                    if (cell.ValueKind == JsonValueKind.String)
                    {
                        cells.Add(cell.GetString() ?? "");
                    }
                    else if (cell.ValueKind == JsonValueKind.Null)
                    {
                        cells.Add("");
                    }
                    else
                    {
                        cells.Add(cell.ToString());
                    }
                }

                if (cells.Count < 12)
                    continue;

                try
                {
                    // Detect format by structure:
                    // 1. OldHtmlFormat  : cells[3] contains HTML tags — [pos,sex,bib,nameHTML,country,sex2,...,status,timeHTML,catPos,cat,...]
                    // 2. ShortNewHtml  : cells[2] contains HTML, count ≤ 16 — [pos,bib,nameHTML,country,sex,status,timeHTML,catPos,cat,...]
                    // 3. ExtendedNewHtml: cells[2] contains HTML, count > 16  — [pos,bib,nameHTML,country,sex,counters×6,status,timeHTML,catPos,cat,...]
                    // 4. CleanFormat   : no HTML — [pos,bib,name,team,country,sex,...,status,time,?,speed,catPos,cat,...]
                    bool isOldHtmlFormat = cells.Count >= 4 && cells[3].Contains("<");
                    bool isNewHtmlFormat = !isOldHtmlFormat && cells.Count >= 9 && cells[2].Contains("<");
                    bool isShortNewHtmlFormat = isNewHtmlFormat && cells.Count <= 16;
                    bool isExtendedNewHtmlFormat = isNewHtmlFormat && cells.Count > 16;

                    string positionStr, bib, nameRaw, team, country, sex, status, timeStr, speedStr, categoryPositionStr, category;

                    if (isOldHtmlFormat)
                    {
                        // OldHtmlFormat: [pos,sex,bib,nameHTML,country,sex2,?,?,status,timeHTML,catPos,cat,detail,?,bib2]
                        positionStr = cells[0].TrimEnd('.');
                        sex = cells[1];
                        bib = cells[2];
                        nameRaw = cells[3];
                        country = cells[4];
                        status = cells[8];
                        var timeHtml = cells[9];
                        categoryPositionStr = cells[10];
                        category = cells[11];

                        // Parse time from HTML (e.g., "<b>1:36:20</b><br/><small>  13.4km/h</small>")
                        var timeMatch = Regex.Match(timeHtml, @"<b>([^<]+)</b>");
                        timeStr = timeMatch.Success ? timeMatch.Groups[1].Value : "";

                        // Parse speed from HTML
                        var speedMatch = Regex.Match(timeHtml, @"([\d.]+)\s*km/h");
                        speedStr = speedMatch.Success ? speedMatch.Groups[1].Value : "";

                        // Extract team from <small> in nameRaw if present
                        var smallMatchOld = Regex.Match(nameRaw, @"<small>([^<]*)</small>");
                        team = smallMatchOld.Success && !string.IsNullOrWhiteSpace(smallMatchOld.Groups[1].Value)
                            ? smallMatchOld.Groups[1].Value.Trim()
                            : country;
                    }
                    else if (isShortNewHtmlFormat)
                    {
                        // ShortNewHtmlFormat (16 cols): [pos,bib,nameHTML,country,sex,status,timeHTML,catPos,cat,sportogr,sporto,detail,?,diplome,?,bib2]
                        positionStr = cells[0].TrimEnd('.');
                        bib = cells[1];
                        nameRaw = cells[2];
                        country = cells[3];
                        sex = cells[4];
                        status = cells[5];
                        var timeHtml = cells[6];
                        categoryPositionStr = cells[7];
                        category = cells[8];

                        // Parse time from HTML
                        var timeMatch = Regex.Match(timeHtml, @"<b>([^<]+)</b>");
                        timeStr = timeMatch.Success ? timeMatch.Groups[1].Value : "";

                        // Parse speed from HTML
                        var speedMatch = Regex.Match(timeHtml, @"([\d.]+)\s*km/h");
                        speedStr = speedMatch.Success ? speedMatch.Groups[1].Value : "";

                        // Extract team from <small> in nameRaw if present
                        var smallMatch = Regex.Match(nameRaw, @"<small>([^<]*)</small>");
                        team = smallMatch.Success && !string.IsNullOrWhiteSpace(smallMatch.Groups[1].Value)
                            ? smallMatch.Groups[1].Value.Trim()
                            : country;
                    }
                    else if (isExtendedNewHtmlFormat)
                    {
                        // ExtendedNewHtmlFormat (21-22 cols): [pos,bib,nameHTML,country,sex,c1,c2,c3,c4,c5,c6,status,timeHTML,catPos,cat,...]
                        positionStr = cells[0].TrimEnd('.');
                        bib = cells[1];
                        nameRaw = cells[2];
                        country = cells[3];
                        sex = cells[4];
                        // cells[5..10] are numeric ranking counters
                        status = cells.Count > 11 ? cells[11] : "";
                        var timeHtml = cells.Count > 12 ? cells[12] : "";
                        categoryPositionStr = cells.Count > 13 ? cells[13] : "";
                        category = cells.Count > 14 ? cells[14] : "";

                        // Parse time from HTML
                        var timeMatch = Regex.Match(timeHtml, @"<b>([^<]+)</b>");
                        timeStr = timeMatch.Success ? timeMatch.Groups[1].Value : "";

                        // Parse speed from HTML
                        var speedMatch = Regex.Match(timeHtml, @"([\d.]+)\s*km/h");
                        speedStr = speedMatch.Success ? speedMatch.Groups[1].Value : "";

                        // Extract team from <small> in nameRaw if present
                        var smallMatchExt = Regex.Match(nameRaw, @"<small>([^<]*)</small>");
                        team = smallMatchExt.Success && !string.IsNullOrWhiteSpace(smallMatchExt.Groups[1].Value)
                            ? smallMatchExt.Groups[1].Value.Trim()
                            : country;
                    }
                    else
                    {
                        // CleanFormat
                        positionStr = cells[0].TrimEnd('.');
                        bib = cells[1];
                        nameRaw = cells[2];
                        team = cells[3];
                        country = cells[4];
                        sex = cells[5];
                        status = cells.Count > 9 ? cells[9] : "";
                        timeStr = cells.Count > 10 ? cells[10] : "";
                        speedStr = cells.Count > 12 ? cells[12] : "";
                        categoryPositionStr = cells.Count > 13 ? cells[13] : "";
                        category = cells.Count > 14 ? cells[14] : "";
                    }

                    // Parse position (skip DNS/DNF with "-")
                    int? position = null;
                    if (!string.IsNullOrWhiteSpace(positionStr) && positionStr != "-")
                    {
                        if (int.TryParse(positionStr, out int pos))
                        {
                            position = pos;
                        }
                    }

                    // Only count finishers for sex position
                    int? sexPosition = null;
                    if (position.HasValue && !string.IsNullOrWhiteSpace(sex))
                    {
                        if (!sexPositionCounters.ContainsKey(sex))
                        {
                            sexPositionCounters[sex] = 0;
                        }
                        sexPositionCounters[sex]++;
                        sexPosition = sexPositionCounters[sex];
                    }

                    // Strip HTML tags from name: use only <b> content to avoid including team from <small> tag
                    // e.g. "<b>LAMBERT Jean</b><br/><small>WACO</small>" → "LAMBERT Jean", not "LAMBERT Jean WACO"
                    var fullName = nameRaw.Contains("<b>")
                        ? System.Net.WebUtility.HtmlDecode(Regex.Match(nameRaw, @"<b>([^<]+)</b>").Groups[1].Value.Trim())
                        : StripHtmlTags(nameRaw);

                    // Parse category position
                    int? categoryPosition = null;
                    if (!string.IsNullOrWhiteSpace(categoryPositionStr) && categoryPositionStr != "-")
                    {
                        if (int.TryParse(categoryPositionStr, out int catPos))
                        {
                            categoryPosition = catPos;
                        }
                    }

                    // Try to split fullName into firstName and lastName
                    // chronorace.be format: "LastName FirstName" (e.g., "DE VOS Nicolas", "ZALESKI Jules")
                    // The last word is the first name, everything before is the last name
                    string firstName = "";
                    string lastName = "";
                    if (!string.IsNullOrWhiteSpace(fullName))
                    {
                        var trimmedName = fullName.Trim();
                        var lastSpaceIndex = trimmedName.LastIndexOf(' ');

                        if (lastSpaceIndex > 0)
                        {
                            // Split at the last space: everything before = lastName, last word = firstName
                            lastName = trimmedName.Substring(0, lastSpaceIndex).Trim();
                            firstName = trimmedName.Substring(lastSpaceIndex + 1).Trim();
                        }
                        else
                        {
                            // No space found, treat entire name as last name
                            lastName = trimmedName;
                        }
                    }

                    // Try to match with member
                    var matchedMember = FindMatchingMember(members, fullName, firstName, lastName);
                    bool isMember = matchedMember != null;

                    if (matchedMember != null)
                    {
                        firstName = matchedMember.FirstName;
                        lastName = matchedMember.LastName;
                    }

                    // Parse time (skip if "-" or null)
                    TimeSpan? raceTime = null;
                    if (!string.IsNullOrWhiteSpace(timeStr) && timeStr != "-")
                    {
                        raceTime = ParseTime(timeStr);
                    }

                    // Parse speed (skip if "-" or empty)
                    double? speed = null;
                    if (!string.IsNullOrWhiteSpace(speedStr) && speedStr != "-")
                    {
                        speed = ParseSpeed(speedStr);
                    }

                    // Build result string with metadata format matching other repositories
                    // Format: TMEM;RACETIME;hh:mm:ss;POS;46;SPEED;13.4;SEX;M;POSITIONSEX;12;CATEGORY;V1H;POSITIONCAT;12;TEAM;BEL;ISMEMBER;1;Nicolas;De Vos
                    var resultBuilder = new System.Text.StringBuilder();

                    // Add member/winner indicator
                    resultBuilder.Append(isMember ? "TMEM;" : "TWINNER;");

                    // Add race time if available
                    if (raceTime.HasValue)
                    {
                        resultBuilder.Append($"RACETIME;{FormatTimeSpan(raceTime)};");
                    }

                    // Add position
                    if (position.HasValue)
                    {
                        resultBuilder.Append($"POS;{position};");
                    }

                    // Add speed
                    if (speed.HasValue)
                    {
                        resultBuilder.Append($"SPEED;{FormatSpeed(speed)};");
                    }

                    // Add sex
                    if (!string.IsNullOrWhiteSpace(sex))
                    {
                        resultBuilder.Append($"SEX;{sex};");
                    }

                    // Add sex position
                    if (sexPosition.HasValue)
                    {
                        resultBuilder.Append($"POSITIONSEX;{sexPosition};");
                    }

                    // Add category
                    if (!string.IsNullOrWhiteSpace(category))
                    {
                        resultBuilder.Append($"CATEGORY;{category};");
                    }

                    // Add category position
                    if (categoryPosition.HasValue)
                    {
                        resultBuilder.Append($"POSITIONCAT;{categoryPosition};");
                    }

                    // Add team (use team if available, otherwise country)
                    var teamValue = !string.IsNullOrWhiteSpace(team) ? team : country;
                    if (!string.IsNullOrWhiteSpace(teamValue))
                    {
                        resultBuilder.Append($"TEAM;{teamValue};");
                    }

                    // Add member flag
                    resultBuilder.Append($"ISMEMBER;{(isMember ? "1" : "0")};");

                    // Add name parts for member matching
                    resultBuilder.Append($"{firstName};{lastName}");

                    results[rowId++] = resultBuilder.ToString();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error parsing chronorace row: {ex.Message}");
                    // Skip this row
                }
            }

            return results;
        }

        private Dictionary<int, string> ParseHtmlResults(string htmlContent, List<Member> members)
        {
            var results = new Dictionary<int, string>();
            var rowId = 1;

            // Add header
            results[rowId++] = "Position;Nom complet;Prénom;Nom;Équipe;Temps;Temps/km;Vitesse;IsMember;Sexe;Position Sexe;Catégorie;Position Catégorie";

            // Extract table rows from HTML
            // This is a simplified parser - may need enhancement based on actual HTML structure
            var tableRowPattern = @"<tr[^>]*>(.*?)</tr>";
            var cellPattern = @"<t[dh][^>]*>(.*?)</t[dh]>";

            var rowMatches = Regex.Matches(htmlContent, tableRowPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            foreach (Match rowMatch in rowMatches)
            {
                var cellMatches = Regex.Matches(rowMatch.Groups[1].Value, cellPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
                if (cellMatches.Count >= 3) // At least position, name, time
                {
                    var cells = new List<string>();
                    foreach (Match cellMatch in cellMatches)
                    {
                        var cellText = StripHtmlTags(cellMatch.Groups[1].Value).Trim();
                        cells.Add(cellText);
                    }

                    var parsedResult = ParseHtmlResultRow(cells, members);
                    if (parsedResult != null)
                    {
                        results[rowId++] = parsedResult;
                    }
                }
            }

            return results;
        }

        private string ParseHtmlResultRow(List<string> cells, List<Member> members)
        {
            try
            {
                // Common ACN Timing table structure (may vary)
                // Position | Dossard | Nom | Prénom | Sexe | Cat | Temps | Vitesse | Club
                if (cells.Count < 3)
                    return null;

                int position = 0;
                int.TryParse(cells[0], out position);

                // Try to identify columns by content pattern
                string fullName = "";
                string firstName = "";
                string lastName = "";
                string team = "";
                string timeStr = "";
                string sex = "";
                string category = "";

                foreach (var cell in cells)
                {
                    if (Regex.IsMatch(cell, @"^\d{1,2}:\d{2}:\d{2}$|^\d{2}:\d{2}$"))
                    {
                        timeStr = cell;
                    }
                    else if (Regex.IsMatch(cell, @"^[HFMhfm]$"))
                    {
                        sex = cell.ToUpperInvariant();
                    }
                    else if (cell.Contains("(") && cell.Contains(")"))
                    {
                        team = Regex.Match(cell, @"\((.*?)\)").Groups[1].Value;
                        fullName = Regex.Replace(cell, @"\(.*?\)", "").Trim();
                    }
                }

                // Match member
                var matchedMember = FindMatchingMember(members, fullName, firstName, lastName);
                bool isMember = matchedMember != null;

                if (matchedMember != null)
                {
                    firstName = matchedMember.FirstName;
                    lastName = matchedMember.LastName;
                }

                TimeSpan? raceTime = ParseTime(timeStr);

                return $"{position}{Separator}" +
                       $"{fullName}{Separator}" +
                       $"{firstName}{Separator}" +
                       $"{lastName}{Separator}" +
                       $"{team}{Separator}" +
                       $"{FormatTimeSpan(raceTime)}{Separator}" +
                       $"{Separator}" + // TimePerKm
                       $"{Separator}" + // Speed
                       $"{isMember}{Separator}" +
                       $"{sex}{Separator}" +
                       $"{Separator}" + // SexPosition
                       $"{category}{Separator}" +
                       $""; // CategoryPosition
            }
            catch
            {
                return null;
            }
        }

        #region Helper Methods

        private string GetJsonString(JsonElement element, params string[] possibleNames)
        {
            foreach (var name in possibleNames)
            {
                if (element.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String)
                {
                    return property.GetString();
                }
            }
            return string.Empty;
        }

        private int? GetJsonInt(JsonElement element, params string[] possibleNames)
        {
            foreach (var name in possibleNames)
            {
                if (element.TryGetProperty(name, out var property))
                {
                    if (property.ValueKind == JsonValueKind.Number)
                    {
                        return property.GetInt32();
                    }
                    else if (property.ValueKind == JsonValueKind.String && int.TryParse(property.GetString(), out int result))
                    {
                        return result;
                    }
                }
            }
            return null;
        }

        private Member FindMatchingMember(List<Member> members, string fullName, string firstName = null, string lastName = null)
        {
            if (members == null || members.Count == 0)
                return null;

            // Try exact match on full name
            if (!string.IsNullOrWhiteSpace(fullName))
            {
                var normalized = NormalizeName(fullName);
                foreach (var member in members)
                {
                    var memberFullName = NormalizeName($"{member.FirstName} {member.LastName}");
                    if (normalized.Equals(memberFullName, StringComparison.OrdinalIgnoreCase))
                    {
                        return member;
                    }
                }
            }

            // Try match on first and last name
            if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName))
            {
                foreach (var member in members)
                {
                    if (NormalizeName(member.FirstName).Equals(NormalizeName(firstName), StringComparison.OrdinalIgnoreCase) &&
                        NormalizeName(member.LastName).Equals(NormalizeName(lastName), StringComparison.OrdinalIgnoreCase))
                    {
                        return member;
                    }
                }
            }

            return null;
        }

        private string NormalizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            // Remove diacritics and normalize spacing
            var normalized = name.Normalize(System.Text.NormalizationForm.FormD);
            var result = new System.Text.StringBuilder();
            foreach (var ch in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                {
                    result.Append(ch);
                }
            }
            return Regex.Replace(result.ToString(), @"\s+", " ").Trim();
        }

        private TimeSpan? ParseTime(string timeString)
        {
            if (string.IsNullOrWhiteSpace(timeString))
                return null;

            timeString = timeString.Trim();

            // Try parsing formats: HH:MM:SS, MM:SS, H:MM:SS
            if (TimeSpan.TryParse(timeString, CultureInfo.InvariantCulture, out TimeSpan result))
            {
                return result;
            }

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
            if (string.IsNullOrWhiteSpace(speedString))
                return null;

            speedString = Regex.Replace(speedString, @"[^\d.,]", "").Trim();

            if (double.TryParse(speedString, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }

            if (double.TryParse(speedString.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out result))
            {
                return result;
            }

            return null;
        }

        private string FormatTimeSpan(TimeSpan? time)
        {
            if (!time.HasValue)
                return string.Empty;

            if (time.Value.Hours > 0)
                return $"{time.Value.Hours}:{time.Value.Minutes:D2}:{time.Value.Seconds:D2}";
            else
                return $"{time.Value.Minutes}:{time.Value.Seconds:D2}";
        }

        private string FormatSpeed(double? speed)
        {
            if (!speed.HasValue)
                return string.Empty;

            return speed.Value.ToString("F2", CultureInfo.InvariantCulture);
        }

        private string StripHtmlTags(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            // Remove HTML tags
            var stripped = Regex.Replace(html, @"<[^>]+>", " ");
            // Decode HTML entities
            stripped = System.Net.WebUtility.HtmlDecode(stripped);
            // Normalize whitespace
            stripped = Regex.Replace(stripped, @"\s+", " ");
            return stripped.Trim();
        }

        #endregion

        private class AcnTimingUrlInfo
        {
            public string EventId { get; set; }
            public string Context { get; set; }
            public string RaceId { get; set; }
            public string ViewId { get; set; }
        }
    }
}
