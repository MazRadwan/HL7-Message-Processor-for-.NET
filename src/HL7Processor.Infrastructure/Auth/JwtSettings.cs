namespace HL7Processor.Infrastructure.Auth;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";
    public string Issuer { get; init; } = "HL7Processor";
    public string Audience { get; init; } = "HL7ProcessorAudience";
    public string SecretKey { get; init; } = string.Empty;
    public int ExpirationMinutes { get; init; } = 60;
}