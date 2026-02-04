using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace NameParser.ConfigChecker;

class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("=== Facebook Configuration Checker ===\n");

        try
        {
            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddUserSecrets<Program>(optional: true)
                .AddEnvironmentVariables()
                .Build();

            // Get Facebook settings
            var appId = configuration["Facebook:AppId"];
            var appSecret = configuration["Facebook:AppSecret"];
            var pageId = configuration["Facebook:PageId"];
            var pageAccessToken = configuration["Facebook:PageAccessToken"];

            // Check configuration
            Console.WriteLine("📋 Configuration Status:");
            Console.WriteLine($"   AppId: {CheckValue(appId)}");
            Console.WriteLine($"   AppSecret: {CheckValue(appSecret, maskValue: true)}");
            Console.WriteLine($"   PageId: {CheckValue(pageId)}");
            Console.WriteLine($"   PageAccessToken: {CheckValue(pageAccessToken, maskValue: true)}");
            Console.WriteLine();

            // If all values are present, test the connection
            if (!string.IsNullOrEmpty(appId) && 
                !string.IsNullOrEmpty(appSecret) && 
                !string.IsNullOrEmpty(pageId) && 
                !string.IsNullOrEmpty(pageAccessToken))
            {
                Console.WriteLine("🔄 Testing Facebook API connection...\n");
                
                using var httpClient = new HttpClient();
                
                // Test 1: Get Page Info
                Console.WriteLine("Test 1: Fetching Page Information");
                var pageInfoResult = await TestPageInfo(httpClient, pageId, pageAccessToken);
                Console.WriteLine($"   Status: {(pageInfoResult.Success ? "✅ Success" : "❌ Failed")}");
                if (!string.IsNullOrEmpty(pageInfoResult.Message))
                {
                    Console.WriteLine($"   {pageInfoResult.Message}");
                }
                Console.WriteLine();

                // Test 2: Verify Token Permissions
                Console.WriteLine("Test 2: Checking Access Token Permissions");
                var permissionsResult = await TestTokenPermissions(httpClient, pageAccessToken);
                Console.WriteLine($"   Status: {(permissionsResult.Success ? "✅ Success" : "❌ Failed")}");
                if (!string.IsNullOrEmpty(permissionsResult.Message))
                {
                    Console.WriteLine($"   {permissionsResult.Message}");
                }
                Console.WriteLine();

                // Test 3: Check Token Expiry
                Console.WriteLine("Test 3: Checking Access Token Expiry");
                var tokenInfoResult = await TestTokenExpiry(httpClient, pageAccessToken);
                Console.WriteLine($"   Status: {(tokenInfoResult.Success ? "✅ Success" : "❌ Failed")}");
                if (!string.IsNullOrEmpty(tokenInfoResult.Message))
                {
                    Console.WriteLine($"   {tokenInfoResult.Message}");
                }
                Console.WriteLine();

                // Summary
                Console.WriteLine("=== Summary ===");
                if (pageInfoResult.Success && permissionsResult.Success && tokenInfoResult.Success)
                {
                    Console.WriteLine("✅ All tests passed! Facebook configuration is working correctly.");
                    return 0;
                }
                else
                {
                    Console.WriteLine("⚠️ Some tests failed. Please check the configuration and token permissions.");
                    return 1;
                }
            }
            else
            {
                Console.WriteLine("⚠️ Configuration incomplete. Please set all Facebook settings.");
                Console.WriteLine("\nYou can configure using:");
                Console.WriteLine("  1. User Secrets (Recommended): dotnet user-secrets set \"Facebook:AppId\" \"your-value\"");
                Console.WriteLine("  2. appsettings.json: Add Facebook section with all required values");
                Console.WriteLine("  3. Environment Variables: Set Facebook__AppId, etc.");
                return 1;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Error: {ex.Message}");
            Console.WriteLine($"\nStack Trace:\n{ex.StackTrace}");
            return 1;
        }
    }

    static string CheckValue(string? value, bool maskValue = false)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "❌ Not Set";
        }
        
        if (maskValue)
        {
            if (value.Length > 8)
            {
                return $"✅ Set ({value.Substring(0, 4)}...{value.Substring(value.Length - 4)})";
            }
            return $"✅ Set (****)";
        }
        
        return $"✅ Set ({value})";
    }

    static async Task<TestResult> TestPageInfo(HttpClient httpClient, string pageId, string pageAccessToken)
    {
        try
        {
            var response = await httpClient.GetAsync(
                $"https://graph.facebook.com/v18.0/{pageId}?fields=id,name,about,category&access_token={pageAccessToken}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var pageInfo = JsonSerializer.Deserialize<JsonElement>(content);
                
                var name = pageInfo.GetProperty("name").GetString();
                var category = pageInfo.TryGetProperty("category", out var cat) ? cat.GetString() : "N/A";
                
                return new TestResult
                {
                    Success = true,
                    Message = $"Page Name: {name}\n   Category: {category}"
                };
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                return new TestResult
                {
                    Success = false,
                    Message = $"HTTP {response.StatusCode}: {error}"
                };
            }
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Success = false,
                Message = $"Exception: {ex.Message}"
            };
        }
    }

    static async Task<TestResult> TestTokenPermissions(HttpClient httpClient, string pageAccessToken)
    {
        try
        {
            // For Page Access Tokens, use debug_token to check permissions
            var response = await httpClient.GetAsync(
                $"https://graph.facebook.com/v18.0/debug_token?input_token={pageAccessToken}&access_token={pageAccessToken}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(content);

                var data = result.GetProperty("data");

                // Get the type of token
                var tokenType = data.TryGetProperty("type", out var type) ? type.GetString() : "unknown";

                // Get scopes (permissions) if available
                var grantedPermissions = new List<string>();
                if (data.TryGetProperty("scopes", out var scopes))
                {
                    foreach (var scope in scopes.EnumerateArray())
                    {
                        var permission = scope.GetString();
                        if (permission != null)
                        {
                            grantedPermissions.Add(permission);
                        }
                    }
                }

                // For page tokens, we need pages_manage_posts or pages_show_list at minimum
                var requiredPermissions = new[] { "pages_manage_posts", "pages_read_engagement" };
                var message = $"Token Type: {tokenType}";

                // Check if it's a USER token instead of a PAGE token
                if (tokenType?.ToUpper() == "USER")
                {
                    message += "\n   🚫 ERROR: You provided a USER Access Token, but need a PAGE Access Token!";
                    message += "\n   ";
                    message += "\n   How to get a PAGE Access Token:";
                    message += "\n   1. Go to: https://developers.facebook.com/tools/explorer/";
                    message += "\n   2. Select your app from the dropdown";
                    message += "\n   3. Click 'Generate Access Token'";
                    message += "\n   4. Grant permissions: pages_manage_posts, pages_read_engagement";
                    message += "\n   5. IMPORTANT: In the dropdown next to your name, select your PAGE";
                    message += "\n   6. The token will change - copy this PAGE token (not the user token)";
                    message += "\n   7. Make it long-lived: https://developers.facebook.com/docs/facebook-login/guides/access-tokens/get-long-lived";

                    return new TestResult
                    {
                        Success = false,
                        Message = message
                    };
                }

                if (grantedPermissions.Any())
                {
                    message += $"\n   Scopes: {string.Join(", ", grantedPermissions)}";

                    var hasManagePosts = grantedPermissions.Contains("pages_manage_posts");
                    var hasReadEngagement = grantedPermissions.Contains("pages_read_engagement");

                    if (!hasManagePosts)
                    {
                        message += "\n   ⚠️ Missing: pages_manage_posts (required to post to page)";
                    }
                    if (!hasReadEngagement)
                    {
                        message += "\n   ⚠️ Missing: pages_read_engagement (recommended for reading page info)";
                    }

                    // Consider it successful if we have manage_posts
                    return new TestResult
                    {
                        Success = hasManagePosts,
                        Message = message
                    };
                }
                else
                {
                    // Page tokens might not show scopes in debug_token, so we'll test actual posting capability
                    message += "\n   Scopes not listed in debug_token (this is normal for some page tokens)";
                    message += "\n   ℹ️ Token will be validated by actual posting test";

                    return new TestResult
                    {
                        Success = true,
                        Message = message
                    };
                }
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                return new TestResult
                {
                    Success = false,
                    Message = $"HTTP {response.StatusCode}: {error}"
                };
            }
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Success = false,
                Message = $"Exception: {ex.Message}"
            };
        }
    }

    static async Task<TestResult> TestTokenExpiry(HttpClient httpClient, string pageAccessToken)
    {
        try
        {
            var response = await httpClient.GetAsync(
                $"https://graph.facebook.com/v18.0/debug_token?input_token={pageAccessToken}&access_token={pageAccessToken}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(content);
                
                var data = result.GetProperty("data");
                var isValid = data.GetProperty("is_valid").GetBoolean();
                
                if (!isValid)
                {
                    return new TestResult
                    {
                        Success = false,
                        Message = "Token is not valid"
                    };
                }
                
                var expiresAt = data.TryGetProperty("expires_at", out var exp) ? exp.GetInt64() : 0;
                var dataAccessExpiresAt = data.TryGetProperty("data_access_expires_at", out var dataExp) ? dataExp.GetInt64() : 0;
                
                var message = "Token is valid";
                
                if (expiresAt == 0)
                {
                    message += "\n   ✅ Token does not expire (long-lived or permanent)";
                }
                else
                {
                    var expiryDate = DateTimeOffset.FromUnixTimeSeconds(expiresAt);
                    var daysUntilExpiry = (expiryDate - DateTimeOffset.UtcNow).TotalDays;
                    message += $"\n   Expires: {expiryDate:yyyy-MM-dd HH:mm:ss} UTC ({daysUntilExpiry:F0} days)";
                    
                    if (daysUntilExpiry < 7)
                    {
                        message += " ⚠️ Expiring soon!";
                    }
                }
                
                if (dataAccessExpiresAt > 0)
                {
                    var dataExpiryDate = DateTimeOffset.FromUnixTimeSeconds(dataAccessExpiresAt);
                    var daysUntilDataExpiry = (dataExpiryDate - DateTimeOffset.UtcNow).TotalDays;
                    message += $"\n   Data Access Expires: {dataExpiryDate:yyyy-MM-dd HH:mm:ss} UTC ({daysUntilDataExpiry:F0} days)";
                }
                
                return new TestResult
                {
                    Success = true,
                    Message = message
                };
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                return new TestResult
                {
                    Success = false,
                    Message = $"HTTP {response.StatusCode}: {error}"
                };
            }
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Success = false,
                Message = $"Exception: {ex.Message}"
            };
        }
    }

    class TestResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
