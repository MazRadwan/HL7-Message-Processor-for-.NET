using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HL7Processor.Core.Models;

public class HL7Field
{
    [Required]
    [JsonPropertyName("position")]
    public int Position { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("components")]
    public List<HL7Component> Components { get; set; } = new();

    [JsonPropertyName("rawData")]
    public string RawData { get; set; } = string.Empty;

    [JsonPropertyName("isRequired")]
    public bool IsRequired { get; set; }

    [JsonPropertyName("maxLength")]
    public int MaxLength { get; set; } = 0;

    [JsonPropertyName("dataType")]
    public string DataType { get; set; } = "ST"; // Default to String

    public string? GetComponent(int componentNumber)
    {
        var component = Components.FirstOrDefault(c => c.Position == componentNumber);
        return component?.Value;
    }

    public void SetComponent(int componentNumber, string value)
    {
        var component = Components.FirstOrDefault(c => c.Position == componentNumber);
        if (component != null)
        {
            component.Value = value;
        }
        else
        {
            Components.Add(new HL7Component { Position = componentNumber, Value = value });
        }
        
        // Update the field value
        RebuildValue();
    }

    public void RebuildValue()
    {
        if (Components.Count > 0)
        {
            var componentValues = Components.OrderBy(c => c.Position).Select(c => c.Value);
            Value = string.Join("^", componentValues);
        }
    }

    public bool IsEmpty()
    {
        return string.IsNullOrEmpty(Value) && Components.All(c => string.IsNullOrEmpty(c.Value));
    }

    public override string ToString()
    {
        return $"HL7Field [{Position}] - Value: {Value}, Components: {Components.Count}";
    }
}

public class HL7Component
{
    [Required]
    [JsonPropertyName("position")]
    public int Position { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("subComponents")]
    public List<HL7SubComponent> SubComponents { get; set; } = new();

    public string? GetSubComponent(int subComponentNumber)
    {
        var subComponent = SubComponents.FirstOrDefault(sc => sc.Position == subComponentNumber);
        return subComponent?.Value;
    }

    public void SetSubComponent(int subComponentNumber, string value)
    {
        var subComponent = SubComponents.FirstOrDefault(sc => sc.Position == subComponentNumber);
        if (subComponent != null)
        {
            subComponent.Value = value;
        }
        else
        {
            SubComponents.Add(new HL7SubComponent { Position = subComponentNumber, Value = value });
        }
        
        // Update the component value
        RebuildValue();
    }

    public void RebuildValue()
    {
        if (SubComponents.Count > 0)
        {
            var subComponentValues = SubComponents.OrderBy(sc => sc.Position).Select(sc => sc.Value);
            Value = string.Join("&", subComponentValues);
        }
    }

    public override string ToString()
    {
        return $"HL7Component [{Position}] - Value: {Value}";
    }
}

public class HL7SubComponent
{
    [Required]
    [JsonPropertyName("position")]
    public int Position { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"HL7SubComponent [{Position}] - Value: {Value}";
    }
}