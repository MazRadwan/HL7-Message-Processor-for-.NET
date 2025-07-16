using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HL7Processor.Core.Models;

public class HL7Segment
{
    [Required]
    [StringLength(3, MinimumLength = 3)]
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("fields")]
    public List<HL7Field> Fields { get; set; } = new();

    [JsonPropertyName("sequenceNumber")]
    public int SequenceNumber { get; set; }

    [JsonPropertyName("rawData")]
    public string RawData { get; set; } = string.Empty;

    [JsonPropertyName("isRequired")]
    public bool IsRequired { get; set; }

    [JsonPropertyName("maxOccurrences")]
    public int MaxOccurrences { get; set; } = 1;

    public HL7Segment()
    {
    }

    public HL7Segment(string type, string rawData)
    {
        Type = type;
        RawData = rawData;
        ParseFields();
    }

    public string? GetFieldValue(int fieldNumber)
    {
        var field = Fields.FirstOrDefault(f => f.Position == fieldNumber);
        return field?.Value;
    }

    public void SetFieldValue(int fieldNumber, string value)
    {
        var field = Fields.FirstOrDefault(f => f.Position == fieldNumber);
        if (field != null)
        {
            field.Value = value;
        }
        else
        {
            Fields.Add(new HL7Field { Position = fieldNumber, Value = value });
        }
    }

    public List<string> GetRepeatingFieldValues(int fieldNumber)
    {
        var field = Fields.FirstOrDefault(f => f.Position == fieldNumber);
        return field?.Components.Select(c => c.Value).ToList() ?? new List<string>();
    }

    private void ParseFields()
    {
        if (string.IsNullOrEmpty(RawData))
            return;

        var fieldSeparator = '|';
        var fieldData = RawData.Split(fieldSeparator);
        
        // Skip the segment type (first element)
        for (int i = 1; i < fieldData.Length; i++)
        {
            var field = new HL7Field
            {
                Position = i,
                Value = fieldData[i],
                RawData = fieldData[i]
            };
            
            // Parse components (separated by ^)
            if (!string.IsNullOrEmpty(fieldData[i]))
            {
                var components = fieldData[i].Split('^');
                for (int j = 0; j < components.Length; j++)
                {
                    field.Components.Add(new HL7Component
                    {
                        Position = j + 1,
                        Value = components[j]
                    });
                }
            }
            
            Fields.Add(field);
        }
    }

    public void RebuildRawData()
    {
        var fieldSeparator = '|';
        var componentSeparator = '^';
        
        var segments = new List<string> { Type };
        
        for (int i = 1; i <= Fields.Count; i++)
        {
            var field = Fields.FirstOrDefault(f => f.Position == i);
            if (field != null && field.Components.Count > 0)
            {
                var componentValues = field.Components.OrderBy(c => c.Position).Select(c => c.Value);
                segments.Add(string.Join(componentSeparator, componentValues));
            }
            else
            {
                segments.Add(field?.Value ?? string.Empty);
            }
        }
        
        RawData = string.Join(fieldSeparator, segments);
    }

    public override string ToString()
    {
        return $"HL7Segment [{Type}] - Fields: {Fields.Count}, Sequence: {SequenceNumber}";
    }
}