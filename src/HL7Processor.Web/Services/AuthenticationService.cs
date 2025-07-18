using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace HL7Processor.Web.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthenticationService> _logger;
    private const string AuthTokenKey = "authToken";
    private const string UserInfoKey = "userInfo";

    public AuthenticationService(IHttpClientFactory httpClientFactory, ILogger<AuthenticationService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("HL7ProcessorApi");
        _logger = logger;
    }

    public async Task<AuthenticationResult> LoginAsync(string username, string password)
    {
        try
        {
            var loginRequest = new
            {
                Username = username,
                Password = password
            };

            var json = JsonSerializer.Serialize(loginRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/auth/login", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (loginResponse?.Success == true && !string.IsNullOrEmpty(loginResponse.Token))
                {
                    var userInfo = new UserInfo
                    {
                        Username = loginResponse.Username ?? username,
                        Role = loginResponse.Role ?? "User",
                        Email = loginResponse.Email ?? "",
                        LastLogin = DateTime.UtcNow
                    };

                    return new AuthenticationResult
                    {
                        Success = true,
                        Token = loginResponse.Token,
                        User = userInfo
                    };
                }
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Login failed for user {Username}. Status: {StatusCode}, Response: {Response}", 
                username, response.StatusCode, errorContent);

            return new AuthenticationResult
            {
                Success = false,
                ErrorMessage = "Invalid username or password"
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error during login for user {Username}", username);
            return new AuthenticationResult
            {
                Success = false,
                ErrorMessage = "Unable to connect to authentication service. Please try again."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login for user {Username}", username);
            return new AuthenticationResult
            {
                Success = false,
                ErrorMessage = "An unexpected error occurred. Please try again."
            };
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            // Call logout endpoint if available
            await _httpClient.PostAsync("/api/auth/logout", null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calling logout endpoint");
        }
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            if (string.IsNullOrEmpty(token))
                return false;

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            var response = await _httpClient.GetAsync("/api/auth/validate");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error validating token");
            return false;
        }
    }

    public async Task<UserInfo?> GetCurrentUserAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/auth/user");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var userInfo = JsonSerializer.Deserialize<UserInfo>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                return userInfo;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting current user info");
        }

        return null;
    }

    private class LoginResponse
    {
        public bool Success { get; set; }
        public string? Token { get; set; }
        public string? Username { get; set; }
        public string? Role { get; set; }
        public string? Email { get; set; }
        public string? ErrorMessage { get; set; }
    }
}