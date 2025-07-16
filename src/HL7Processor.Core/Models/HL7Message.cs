using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HL7Processor.Core.Models;

public class HL7Message
{
    [Required]
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [JsonPropertyName("messageType")]
    public HL7MessageType MessageType { get; set; }

    [Required]
    [StringLength(10, MinimumLength = 3)]
    [JsonPropertyName("version")]
    public string Version { get; set; } = "2.5";

    [Required]
    [JsonPropertyName("rawMessage")]
    public string RawMessage { get; set; } = string.Empty;

    [JsonPropertyName("segments")]
    public List<HL7Segment> Segments { get; set; } = new();

    [JsonPropertyName("sendingApplication")]
    public string? SendingApplication { get; set; }

    [JsonPropertyName("sendingFacility")]
    public string? SendingFacility { get; set; }

    [JsonPropertyName("receivingApplication")]
    public string? ReceivingApplication { get; set; }

    [JsonPropertyName("receivingFacility")]
    public string? ReceivingFacility { get; set; }

    [JsonPropertyName("messageControlId")]
    public string? MessageControlId { get; set; }

    [JsonPropertyName("processingId")]
    public string? ProcessingId { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("isValid")]
    public bool IsValid { get; set; }

    [JsonPropertyName("validationErrors")]
    public List<string> ValidationErrors { get; set; } = new();

    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();

    public void AddValidationError(string error)
    {
        ValidationErrors.Add(error);
        IsValid = false;
    }

    public void ClearValidationErrors()
    {
        ValidationErrors.Clear();
        IsValid = true;
    }

    public HL7Segment? GetSegment(string segmentType)
    {
        return Segments.FirstOrDefault(s => s.Type.Equals(segmentType, StringComparison.OrdinalIgnoreCase));
    }

    public List<HL7Segment> GetSegments(string segmentType)
    {
        return Segments.Where(s => s.Type.Equals(segmentType, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public void AddSegment(HL7Segment segment)
    {
        Segments.Add(segment);
    }

    public void RemoveSegment(string segmentType)
    {
        Segments.RemoveAll(s => s.Type.Equals(segmentType, StringComparison.OrdinalIgnoreCase));
    }

    public override string ToString()
    {
        return $"HL7Message [{MessageType.GetMessageTypeCode()}] - ID: {Id}, Version: {Version}, Valid: {IsValid}";
    }
}