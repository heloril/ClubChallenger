using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NameParser.UI.Services
{
    public class FacebookService
    {
        private readonly HttpClient _httpClient;
        private readonly FacebookSettings _settings;

        public FacebookService(FacebookSettings settings)
        {
            _httpClient = new HttpClient();
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// Post race results to Facebook page
        /// </summary>
        public async Task<FacebookPostResponse> PostRaceResultsAsync(string raceName, string summary, byte[]? imageData = null)
        {
            try
            {
                if (string.IsNullOrEmpty(_settings.PageAccessToken))
                {
                    return new FacebookPostResponse
                    {
                        Success = false,
                        ErrorMessage = "Facebook Page Access Token is not configured. Please update App.config."
                    };
                }

                string postId;

                // If image is provided, post as photo with caption
                if (imageData != null && imageData.Length > 0)
                {
                    postId = await PostPhotoAsync(raceName, summary, imageData);
                }
                else
                {
                    // Post as text
                    postId = await PostTextAsync(raceName, summary);
                }

                return new FacebookPostResponse
                {
                    Success = true,
                    PostId = postId,
                    PostUrl = $"https://www.facebook.com/{_settings.PageId}/posts/{postId}"
                };
            }
            catch (Exception ex)
            {
                return new FacebookPostResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Post challenge results to Facebook page
        /// </summary>
        public async Task<FacebookPostResponse> PostChallengeResultsAsync(string challengeTitle, string summary, byte[]? imageData = null)
        {
            return await PostRaceResultsAsync(challengeTitle, summary, imageData);
        }

        /// <summary>
        /// Post both full race results and challenger results to Facebook
        /// </summary>
        public async Task<List<FacebookPostResponse>> PostRaceWithLatestResultsAsync(
            string raceName,
            string fullResultsSummary,
            string challengerSummary,
            byte[]? imageData = null)
        {
            var results = new List<FacebookPostResponse>();

            try
            {
                if (string.IsNullOrEmpty(_settings.PageAccessToken))
                {
                    var errorResponse = new FacebookPostResponse
                    {
                        Success = false,
                        ErrorMessage = "Facebook Page Access Token is not configured. Please update App.config."
                    };
                    results.Add(errorResponse);
                    results.Add(errorResponse);
                    return results;
                }

                // Post 1: Full Results
                var fullResultsResponse = await PostRaceResultsAsync(
                    raceName + " - Full Results",
                    fullResultsSummary,
                    imageData);
                results.Add(fullResultsResponse);

                // Small delay between posts
                await Task.Delay(2000);

                // Post 2: Challenger Results (all challengers)
                var challengerResponse = await PostRaceResultsAsync(
                    raceName + " - Challenger Results",
                    challengerSummary,
                    null); // No image for second post
                results.Add(challengerResponse);

                return results;
            }
            catch (Exception ex)
            {
                results.Add(new FacebookPostResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
                return results;
            }
        }

        private async Task<string> PostTextAsync(string title, string message)
        {
            var postData = new
            {
                message = $"🏃 {title}\n\n{message}"
            };

            var json = JsonSerializer.Serialize(postData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"https://graph.facebook.com/v18.0/{_settings.PageId}/feed?access_token={_settings.PageAccessToken}",
                content);

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<FacebookPostResult>(responseContent);

            return result?.id ?? string.Empty;
        }

        private async Task<string> PostPhotoAsync(string title, string message, byte[] imageData)
        {
            using var formData = new MultipartFormDataContent();

            // Add image
            var imageContent = new ByteArrayContent(imageData);
            imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
            formData.Add(imageContent, "source", "race-results.png");

            // Add caption
            var caption = $"🏃 {title}\n\n{message}";
            formData.Add(new StringContent(caption), "caption");

            var response = await _httpClient.PostAsync(
                $"https://graph.facebook.com/v18.0/{_settings.PageId}/photos?access_token={_settings.PageAccessToken}",
                formData);

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<FacebookPostResult>(responseContent);

            return result?.id ?? string.Empty;
        }

        /// <summary>
        /// Test connection to Facebook API
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_settings.PageAccessToken))
                {
                    return false;
                }

                var response = await _httpClient.GetAsync(
                    $"https://graph.facebook.com/v18.0/{_settings.PageId}?fields=name&access_token={_settings.PageAccessToken}");

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }

    public class FacebookSettings
    {
        public string AppId { get; set; } = string.Empty;
        public string AppSecret { get; set; } = string.Empty;
        public string PageId { get; set; } = string.Empty;
        public string PageAccessToken { get; set; } = string.Empty;
    }

    public class FacebookPostResponse
    {
        public bool Success { get; set; }
        public string? PostId { get; set; }
        public string? PostUrl { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class FacebookPostResult
    {
        public string id { get; set; } = string.Empty;
    }
}
