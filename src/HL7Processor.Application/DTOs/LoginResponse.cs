namespace HL7Processor.Application.DTOs;

public class LoginResponse
{
    public bool Success { get; set; }
    public string? AccessToken { get; set; }
    public string? Username { get; set; }
    public string[]? Roles { get; set; }
    public string? ErrorMessage { get; set; }

    public static LoginResponse Successful(string accessToken, string username, string[] roles)
    {
        return new LoginResponse
        {
            Success = true,
            AccessToken = accessToken,
            Username = username,
            Roles = roles
        };
    }

    public static LoginResponse Failed(string errorMessage)
    {
        return new LoginResponse
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}