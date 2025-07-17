namespace HL7Processor.Web.Services;

public interface IAuthenticationService
{
    Task<AuthenticationResult> LoginAsync(string username, string password);
    Task LogoutAsync();
    Task<bool> ValidateTokenAsync(string token);
    Task<UserInfo?> GetCurrentUserAsync();
}

public class AuthenticationResult
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? ErrorMessage { get; set; }
    public UserInfo? User { get; set; }
}

public class UserInfo
{
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime? LastLogin { get; set; }
}