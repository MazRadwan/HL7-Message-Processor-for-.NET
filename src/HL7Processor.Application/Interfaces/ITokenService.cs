namespace HL7Processor.Application.Interfaces;

public interface ITokenService
{
    string GenerateToken(string username, IEnumerable<string> roles);
}