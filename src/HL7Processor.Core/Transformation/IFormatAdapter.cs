using HL7Processor.Core.Models;

namespace HL7Processor.Core.Transformation;

public interface IFormatAdapter<T>
{
    string AdapterName { get; }
    string Description { get; }
    string SupportedVersion { get; }
    bool CanConvertFrom(string data);
    bool CanConvertTo(HL7Message message);
    T ConvertFrom(HL7Message message);
    HL7Message ConvertTo(T data);
    ValidationResult ValidateData(T data);
}

public interface IFormatAdapter
{
    string AdapterName { get; }
    string Description { get; }
    string SupportedVersion { get; }
    Type SupportedType { get; }
    bool CanConvertFrom(string data);
    bool CanConvertTo(HL7Message message);
    object ConvertFrom(HL7Message message);
    HL7Message ConvertTo(object data);
    ValidationResult ValidateData(object data);
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();

    public void AddError(string error)
    {
        Errors.Add(error);
        IsValid = false;
    }

    public void AddWarning(string warning)
    {
        Warnings.Add(warning);
    }

    public void AddMetadata(string key, object value)
    {
        Metadata[key] = value;
    }

    public override string ToString()
    {
        return $"ValidationResult: IsValid={IsValid}, Errors={Errors.Count}, Warnings={Warnings.Count}";
    }
}