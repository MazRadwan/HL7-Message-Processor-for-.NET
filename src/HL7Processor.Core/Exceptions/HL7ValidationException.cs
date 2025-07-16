namespace HL7Processor.Core.Exceptions;

public class HL7ValidationException : HL7ProcessingException
{
    public List<string> ValidationErrors { get; }

    public HL7ValidationException(string message, List<string> validationErrors) : base(message)
    {
        ValidationErrors = validationErrors ?? throw new ArgumentNullException(nameof(validationErrors));
    }

    public HL7ValidationException(string message, List<string> validationErrors, string? messageType, string? segmentName = null, int? fieldNumber = null, string? originalMessage = null) 
        : base(message, messageType, segmentName, fieldNumber, originalMessage)
    {
        ValidationErrors = validationErrors ?? throw new ArgumentNullException(nameof(validationErrors));
    }

    public HL7ValidationException(string message, List<string> validationErrors, Exception innerException, string? messageType, string? segmentName = null, int? fieldNumber = null, string? originalMessage = null) 
        : base(message, innerException, messageType, segmentName, fieldNumber, originalMessage)
    {
        ValidationErrors = validationErrors ?? throw new ArgumentNullException(nameof(validationErrors));
    }

    public override string ToString()
    {
        var result = base.ToString();
        
        if (ValidationErrors.Count > 0)
        {
            result += "\nValidation Errors:";
            foreach (var error in ValidationErrors)
            {
                result += $"\n  - {error}";
            }
        }
        
        return result;
    }
}