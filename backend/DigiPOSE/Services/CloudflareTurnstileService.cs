using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using DigiPOSE.Models;

namespace DigiPOSE.Services
{
    public interface ICloudflareTurnstileService
    {
        Task<(bool Success, string ErrorMessage)> VerifyTokenAsync(string token, string? remoteIp = null);
    }

    public class CloudflareTurnstileResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("challenge_ts")]
        public string? ChallengeTs { get; set; }

        [JsonPropertyName("hostname")]
        public string? Hostname { get; set; }

        [JsonPropertyName("error-codes")]
        public List<string>? ErrorCodes { get; set; }
    }

    public class CloudflareTurnstileService : ICloudflareTurnstileService
    {
        private readonly HttpClient _httpClient;
        private readonly TurnstileSettings _settings;
        private readonly ILogger<CloudflareTurnstileService> _logger;
        private const int MaxRetries = 3;

        public CloudflareTurnstileService(
            HttpClient httpClient, 
            IOptions<TurnstileSettings> options, 
            ILogger<CloudflareTurnstileService> logger)
        {
            _httpClient = httpClient;
            _settings = options.Value;
            _logger = logger;

            _httpClient.Timeout = TimeSpan.FromSeconds(5);
        }

        public async Task<(bool Success, string ErrorMessage)> VerifyTokenAsync(string token, string? remoteIp = null)
        {
            // If Turnstile is disabled in configuration, pass immediately (Fail-Safe Dev Mode)
            if (!_settings.IsEnabled)
            {
                _logger.LogInformation("[TURNSTILE_BYPASS]: Turnstile verification is currently disabled in system settings.");
                return (true, string.Empty);
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("[TURNSTILE_DENIAL]: Missing Turnstile verification token from client request.");
                return (false, "Bot verification challenge failed or missing token. Please reload and try again.");
            }

            var parameters = new Dictionary<string, string>
            {
                { "secret", _settings.SecretKey },
                { "response", token }
            };

            if (!string.IsNullOrEmpty(remoteIp))
            {
                parameters.Add("remoteip", remoteIp);
            }

            int retryCount = 0;
            TimeSpan delay = TimeSpan.FromMilliseconds(200);

            while (retryCount <= MaxRetries)
            {
                try
                {
                    using var content = new FormUrlEncodedContent(parameters);
                    using var response = await _httpClient.PostAsync(_settings.VerificationUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResponse = await response.Content.ReadAsStringAsync();
                        var verificationResult = JsonSerializer.Deserialize<CloudflareTurnstileResponse>(jsonResponse);

                        if (verificationResult != null && verificationResult.Success)
                        {
                            _logger.LogInformation("[TURNSTILE_SUCCESS]: Verified security token for IP {RemoteIp}.", remoteIp ?? "Unknown");
                            return (true, string.Empty);
                        }

                        string errors = verificationResult?.ErrorCodes != null && verificationResult.ErrorCodes.Any()
                            ? string.Join(", ", verificationResult.ErrorCodes)
                            : "invalid-token";

                        _logger.LogWarning("[TURNSTILE_REJECTED]: Token validation failed with code(s): {Errors}.", errors);
                        return (false, $"Security challenge failed: {errors}. Please refresh and re-verify.");
                    }

                    _logger.LogWarning("[TURNSTILE_HTTP_FAULT]: Non-success status {StatusCode} on try {Attempt}/{MaxRetries}.", 
                        response.StatusCode, retryCount + 1, MaxRetries + 1);
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogWarning(ex, "[TURNSTILE_NETWORK_ANOMALY]: Network glitch during Cloudflare verification on try {Attempt}/{MaxRetries}.", 
                        retryCount + 1, MaxRetries + 1);
                }
                catch (TaskCanceledException ex)
                {
                    _logger.LogWarning(ex, "[TURNSTILE_TIMEOUT]: HTTP request timed out on try {Attempt}/{MaxRetries}.", 
                        retryCount + 1, MaxRetries + 1);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[TURNSTILE_CRITICAL]: Unexpected fault in Turnstile verification loop.");
                    break;
                }

                retryCount++;
                if (retryCount <= MaxRetries)
                {
                    // Exponential Backoff with slight random jitter
                    await Task.Delay(delay);
                    delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2 + new Random().Next(50, 150));
                }
            }

            _logger.LogError("[TURNSTILE_EXHAUSTED]: All {MaxRetries} verification retry attempts exhausted via Exponential Backoff.", MaxRetries);
            return (false, "Temporary connection disruption to defensive Cloudflare grid. Please try logging in again.");
        }
    }
}
