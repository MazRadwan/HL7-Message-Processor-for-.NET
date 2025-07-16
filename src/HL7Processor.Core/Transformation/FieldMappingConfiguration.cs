using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HL7Processor.Core.Transformation;

public class FieldMappingConfiguration
{
    [Required]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    [JsonPropertyName("mappings")]
    public List<FieldMapping> Mappings { get; set; } = new();

    [JsonPropertyName("customRules")]
    public List<CustomMappingRule> CustomRules { get; set; } = new();

    [JsonPropertyName("validationRules")]
    public List<ValidationRule> ValidationRules { get; set; } = new();

    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();

    public FieldMapping? GetMapping(string sourceField)
    {
        return Mappings.FirstOrDefault(m => m.SourceField.Equals(sourceField, StringComparison.OrdinalIgnoreCase));
    }

    public List<FieldMapping> GetMappingsForSegment(string segmentType)
    {
        return Mappings.Where(m => m.SourceField.StartsWith($"{segmentType}-", StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public List<CustomMappingRule> GetCustomRulesForField(string fieldName)
    {
        return CustomRules.Where(r => r.AppliesTo.Contains(fieldName, StringComparer.OrdinalIgnoreCase)).ToList();
    }

    public void AddMapping(FieldMapping mapping)
    {
        if (mapping == null) throw new ArgumentNullException(nameof(mapping));
        
        // Remove existing mapping for the same source field
        Mappings.RemoveAll(m => m.SourceField.Equals(mapping.SourceField, StringComparison.OrdinalIgnoreCase));
        Mappings.Add(mapping);
    }

    public void RemoveMapping(string sourceField)
    {
        Mappings.RemoveAll(m => m.SourceField.Equals(sourceField, StringComparison.OrdinalIgnoreCase));
    }

    public bool HasMappingFor(string sourceField)
    {
        return Mappings.Any(m => m.SourceField.Equals(sourceField, StringComparison.OrdinalIgnoreCase));
    }

    public void Validate()
    {
        var duplicateFields = Mappings.GroupBy(m => m.SourceField, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateFields.Any())
        {
            throw new InvalidOperationException($"Duplicate mappings found for fields: {string.Join(", ", duplicateFields)}");
        }

        foreach (var mapping in Mappings)
        {
            mapping.Validate();
        }

        foreach (var rule in CustomRules)
        {
            rule.Validate();
        }
    }
}

public class FieldMapping
{
    [Required]
    [JsonPropertyName("sourceField")]
    public string SourceField { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("targetField")]
    public string TargetField { get; set; } = string.Empty;

    [JsonPropertyName("dataType")]
    public string DataType { get; set; } = "string";

    [JsonPropertyName("isRequired")]
    public bool IsRequired { get; set; } = false;

    [JsonPropertyName("defaultValue")]
    public string? DefaultValue { get; set; }

    [JsonPropertyName("transformFunction")]
    public string? TransformFunction { get; set; }

    [JsonPropertyName("validationPattern")]
    public string? ValidationPattern { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("conditions")]
    public List<MappingCondition> Conditions { get; set; } = new();

    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();

    public bool ShouldApplyMapping(Dictionary<string, object> context)
    {
        if (!Conditions.Any()) return true;

        return Conditions.All(condition => condition.IsMetBy(context));
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SourceField))
            throw new ArgumentException("SourceField cannot be null or empty");

        if (string.IsNullOrWhiteSpace(TargetField))
            throw new ArgumentException("TargetField cannot be null or empty");

        if (!string.IsNullOrEmpty(ValidationPattern))
        {
            try
            {
                System.Text.RegularExpressions.Regex.IsMatch("test", ValidationPattern);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Invalid validation pattern: {ex.Message}", ex);
            }
        }
    }

    public override string ToString()
    {
        return $"{SourceField} -> {TargetField} ({DataType})";
    }
}

public class MappingCondition
{
    [Required]
    [JsonPropertyName("field")]
    public string Field { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("operator")]
    public string Operator { get; set; } = "equals";

    [Required]
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("caseSensitive")]
    public bool CaseSensitive { get; set; } = false;

    public bool IsMetBy(Dictionary<string, object> context)
    {
        if (!context.TryGetValue(Field, out var fieldValue))
            return false;

        var stringValue = fieldValue?.ToString() ?? string.Empty;
        var comparison = CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        return Operator.ToLowerInvariant() switch
        {
            "equals" => stringValue.Equals(Value, comparison),
            "not_equals" => !stringValue.Equals(Value, comparison),
            "contains" => stringValue.Contains(Value, comparison),
            "starts_with" => stringValue.StartsWith(Value, comparison),
            "ends_with" => stringValue.EndsWith(Value, comparison),
            "regex" => System.Text.RegularExpressions.Regex.IsMatch(stringValue, Value),
            "is_empty" => string.IsNullOrEmpty(stringValue),
            "is_not_empty" => !string.IsNullOrEmpty(stringValue),
            _ => false
        };
    }
}

public class CustomMappingRule
{
    [Required]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("appliesTo")]
    public List<string> AppliesTo { get; set; } = new();

    [JsonPropertyName("ruleType")]
    public string RuleType { get; set; } = "transform";

    [JsonPropertyName("expression")]
    public string Expression { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public int Priority { get; set; } = 0;

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;

    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new ArgumentException("Rule name cannot be null or empty");

        if (!AppliesTo.Any())
            throw new ArgumentException("Rule must apply to at least one field");

        if (string.IsNullOrWhiteSpace(Expression))
            throw new ArgumentException("Rule expression cannot be null or empty");
    }

    public override string ToString()
    {
        return $"{Name} ({RuleType}) - Priority: {Priority}";
    }
}

public class ValidationRule
{
    [Required]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("field")]
    public string Field { get; set; } = string.Empty;

    [JsonPropertyName("ruleType")]
    public string RuleType { get; set; } = "required";

    [JsonPropertyName("parameters")]
    public Dictionary<string, object> Parameters { get; set; } = new();

    [JsonPropertyName("errorMessage")]
    public string ErrorMessage { get; set; } = string.Empty;

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new ArgumentException("Validation rule name cannot be null or empty");

        if (string.IsNullOrWhiteSpace(Field))
            throw new ArgumentException("Validation rule field cannot be null or empty");
    }

    public override string ToString()
    {
        return $"{Name} ({RuleType}) for {Field}";
    }
}