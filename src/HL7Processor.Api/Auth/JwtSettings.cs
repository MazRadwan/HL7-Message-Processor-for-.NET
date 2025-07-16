namespace HL7Processor.Api.Auth;

public class JwtSettings
{
    public const string SectionName = "Jwt";
    public string Issuer { get; init; } = "HL7Processor";
    public string Audience { get; init; } = "HL7ProcessorAudience";
    public string SecretKey { get; init; } = "CHANGE_ME_TO_A_SECURE_KEY_OF_MIN_32_CHARS";
    public int ExpiryMinutes { get; init; } = 60;
} 