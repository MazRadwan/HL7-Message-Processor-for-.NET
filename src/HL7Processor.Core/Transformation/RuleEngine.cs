using HL7Processor.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace HL7Processor.Core.Transformation;

public class RuleEngine
{
    private readonly ILogger<RuleEngine> _logger;
    private readonly Dictionary<string, Func<Dictionary<string, object>, Dictionary<string, object>, Dictionary<string, object>>> _builtInRules;

    public RuleEngine(ILogger<RuleEngine> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _builtInRules = InitializeBuiltInRules();
    }

    public bool EvaluateConditions(List<MappingCondition> conditions, HL7Message message, Dictionary<string, object> context)
    {
        if (conditions == null || conditions.Count == 0)
            return true;

        foreach (var condition in conditions)
        {
            if (!EvaluateCondition(condition, message, context))
            {
                _logger.LogDebug("Condition failed: {Field} {Operator} {Value}", 
                    condition.Field, condition.Operator, condition.Value);
                return false;
            }
        }

        return true;
    }

    public Dictionary<string, object> ApplyCustomRules(Dictionary<string, object> data, List<CustomMappingRule> rules)
    {
        if (rules == null || rules.Count == 0)
            return data;

        var result = new Dictionary<string, object>(data);

        // Sort rules by priority (higher priority first)
        var sortedRules = rules.Where(r => r.IsActive)
                              .OrderByDescending(r => r.Priority)
                              .ToList();

        foreach (var rule in sortedRules)
        {
            try
            {
                result = ApplyCustomRule(result, rule);
                _logger.LogDebug("Applied custom rule: {RuleName}", rule.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply custom rule: {RuleName}", rule.Name);
            }
        }

        return result;
    }

    public ValidationResult ValidateRules(List<CustomMappingRule> rules)
    {
        var result = new ValidationResult { IsValid = true };

        foreach (var rule in rules)
        {
            var ruleValidation = ValidateRule(rule);
            if (!ruleValidation.IsValid)
            {
                result.Errors.AddRange(ruleValidation.Errors);
                result.Warnings.AddRange(ruleValidation.Warnings);
            }
        }

        return result;
    }

    private bool EvaluateCondition(MappingCondition condition, HL7Message message, Dictionary<string, object> context)
    {
        var actualValue = GetConditionValue(condition.Field, message, context);
        var expectedValue = condition.Value;

        return condition.Operator.ToLowerInvariant() switch
        {
            "equals" or "eq" => string.Equals(actualValue, expectedValue, StringComparison.OrdinalIgnoreCase),
            "not_equals" or "ne" => !string.Equals(actualValue, expectedValue, StringComparison.OrdinalIgnoreCase),
            "contains" => actualValue?.Contains(expectedValue, StringComparison.OrdinalIgnoreCase) == true,
            "not_contains" => actualValue?.Contains(expectedValue, StringComparison.OrdinalIgnoreCase) != true,
            "starts_with" => actualValue?.StartsWith(expectedValue, StringComparison.OrdinalIgnoreCase) == true,
            "ends_with" => actualValue?.EndsWith(expectedValue, StringComparison.OrdinalIgnoreCase) == true,
            "regex" => !string.IsNullOrEmpty(actualValue) && Regex.IsMatch(actualValue, expectedValue),
            "greater_than" or "gt" => CompareNumeric(actualValue, expectedValue) > 0,
            "less_than" or "lt" => CompareNumeric(actualValue, expectedValue) < 0,
            "greater_equal" or "ge" => CompareNumeric(actualValue, expectedValue) >= 0,
            "less_equal" or "le" => CompareNumeric(actualValue, expectedValue) <= 0,
            "in" => IsValueInList(actualValue, expectedValue),
            "not_in" => !IsValueInList(actualValue, expectedValue),
            "is_empty" => string.IsNullOrEmpty(actualValue),
            "is_not_empty" => !string.IsNullOrEmpty(actualValue),
            _ => false
        };
    }

    private string? GetConditionValue(string field, HL7Message message, Dictionary<string, object> context)
    {
        // Try context first
        if (context.TryGetValue(field, out var contextValue))
        {
            return contextValue?.ToString();
        }

        // Try message properties
        return field.ToLowerInvariant() switch
        {
            "messagetype" => message.MessageType.ToString(),
            "messageid" => message.Id,
            "sendingapplication" => message.SendingApplication,
            "sendingfacility" => message.SendingFacility,
            "receivingapplication" => message.ReceivingApplication,
            "receivingfacility" => message.ReceivingFacility,
            "processingid" => message.ProcessingId,
            "version" => message.Version,
            _ => ExtractFieldFromMessage(field, message)
        };
    }

    private string ExtractFieldFromMessage(string field, HL7Message message)
    {
        // Handle segment-field notation (e.g., "PID-3")
        var parts = field.Split('-');
        if (parts.Length >= 2)
        {
            var segmentType = parts[0];
            if (int.TryParse(parts[1], out var fieldNumber))
            {
                var segment = message.GetSegment(segmentType);
                return segment?.GetFieldValue(fieldNumber) ?? string.Empty;
            }
        }

        return string.Empty;
    }

    private int CompareNumeric(string? value1, string? value2)
    {
        if (decimal.TryParse(value1, out var num1) && decimal.TryParse(value2, out var num2))
        {
            return num1.CompareTo(num2);
        }
        return string.Compare(value1, value2, StringComparison.OrdinalIgnoreCase);
    }

    private bool IsValueInList(string? value, string? listValues)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(listValues))
            return false;

        var values = listValues.Split(',', StringSplitOptions.RemoveEmptyEntries)
                              .Select(v => v.Trim())
                              .ToArray();

        return values.Contains(value, StringComparer.OrdinalIgnoreCase);
    }

    private Dictionary<string, object> ApplyCustomRule(Dictionary<string, object> data, CustomMappingRule rule)
    {
        var result = new Dictionary<string, object>(data);

        switch (rule.RuleType.ToLowerInvariant())
        {
            case "field_transform":
                return ApplyFieldTransformRule(result, rule);
            case "conditional_mapping":
                return ApplyConditionalMappingRule(result, rule);
            case "calculated_field":
                return ApplyCalculatedFieldRule(result, rule);
            case "data_validation":
                return ApplyDataValidationRule(result, rule);
            case "built_in":
                return ApplyBuiltInRule(result, rule);
            default:
                _logger.LogWarning("Unknown rule type: {RuleType}", rule.RuleType);
                return result;
        }
    }

    private Dictionary<string, object> ApplyFieldTransformRule(Dictionary<string, object> data, CustomMappingRule rule)
    {
        var result = new Dictionary<string, object>(data);

        foreach (var targetField in rule.AppliesTo)
        {
            if (EvaluateRuleExpression(rule.Expression, data, out var calculatedValue))
            {
                result[targetField] = calculatedValue;
            }
        }

        return result;
    }

    private Dictionary<string, object> ApplyConditionalMappingRule(Dictionary<string, object> data, CustomMappingRule rule)
    {
        var result = new Dictionary<string, object>(data);

        // Parse conditional expression (e.g., "if gender == 'M' then genderDisplay = 'Male'")
        if (rule.Expression.Contains("if") && rule.Expression.Contains("then"))
        {
            var parts = rule.Expression.Split(new[] { "if", "then" }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                var condition = parts[0].Trim();
                var action = parts[1].Trim();

                if (EvaluateSimpleCondition(condition, data))
                {
                    ExecuteSimpleAction(action, result);
                }
            }
        }

        return result;
    }

    private Dictionary<string, object> ApplyCalculatedFieldRule(Dictionary<string, object> data, CustomMappingRule rule)
    {
        var result = new Dictionary<string, object>(data);

        foreach (var targetField in rule.AppliesTo)
        {
            if (EvaluateRuleExpression(rule.Expression, data, out var calculatedValue))
            {
                result[targetField] = calculatedValue;
            }
        }

        return result;
    }

    private Dictionary<string, object> ApplyDataValidationRule(Dictionary<string, object> data, CustomMappingRule rule)
    {
        // Validation rules don't modify data, they just log validation results
        foreach (var field in rule.AppliesTo)
        {
            if (data.TryGetValue(field, out var value))
            {
                var isValid = EvaluateRuleExpression(rule.Expression, data, out _);
                if (!isValid)
                {
                    _logger.LogWarning("Validation rule '{RuleName}' failed for field '{Field}' with value '{Value}'", 
                        rule.Name, field, value);
                }
            }
        }

        return data;
    }

    private Dictionary<string, object> ApplyBuiltInRule(Dictionary<string, object> data, CustomMappingRule rule)
    {
        if (_builtInRules.TryGetValue(rule.Expression, out var ruleFunction))
        {
            return ruleFunction(data, rule.Metadata);
        }

        _logger.LogWarning("Unknown built-in rule: {Expression}", rule.Expression);
        return data;
    }

    private bool EvaluateRuleExpression(string expression, Dictionary<string, object> data, out object result)
    {
        result = null!;

        try
        {
            // Simple expression evaluation
            var processedExpression = expression;

            // Replace field references
            foreach (var kvp in data)
            {
                var placeholder = $"{{{kvp.Key}}}";
                if (processedExpression.Contains(placeholder))
                {
                    processedExpression = processedExpression.Replace(placeholder, $"'{kvp.Value}'");
                }

                // Also handle bare field names
                var fieldPattern = $@"\b{Regex.Escape(kvp.Key)}\b";
                if (Regex.IsMatch(processedExpression, fieldPattern))
                {
                    processedExpression = Regex.Replace(processedExpression, fieldPattern, $"'{kvp.Value}'");
                }
            }

            // Handle simple concatenation
            if (processedExpression.Contains(" + "))
            {
                var parts = processedExpression.Split(" + ");
                var concatenated = string.Join("", parts.Select(p => p.Trim('\'')));
                result = concatenated;
                return true;
            }

            // Handle simple arithmetic
            if (Regex.IsMatch(processedExpression, @"^\d+(\.\d+)?\s*[\+\-\*\/]\s*\d+(\.\d+)?$"))
            {
                // This is a basic arithmetic expression - in production use a proper expression evaluator
                result = processedExpression;
                return true;
            }

            result = processedExpression.Trim('\'');
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to evaluate expression: {Expression}", expression);
            return false;
        }
    }

    private bool EvaluateSimpleCondition(string condition, Dictionary<string, object> data)
    {
        // Parse simple conditions like "gender == 'M'"
        var match = Regex.Match(condition, @"(\w+)\s*(==|!=|>|<|>=|<=)\s*'([^']*)'");
        if (match.Success)
        {
            var fieldName = match.Groups[1].Value;
            var operator_ = match.Groups[2].Value;
            var expectedValue = match.Groups[3].Value;

            if (data.TryGetValue(fieldName, out var actualValue))
            {
                var actualString = actualValue?.ToString() ?? string.Empty;
                
                return operator_ switch
                {
                    "==" => string.Equals(actualString, expectedValue, StringComparison.OrdinalIgnoreCase),
                    "!=" => !string.Equals(actualString, expectedValue, StringComparison.OrdinalIgnoreCase),
                    ">" => string.Compare(actualString, expectedValue, StringComparison.OrdinalIgnoreCase) > 0,
                    "<" => string.Compare(actualString, expectedValue, StringComparison.OrdinalIgnoreCase) < 0,
                    ">=" => string.Compare(actualString, expectedValue, StringComparison.OrdinalIgnoreCase) >= 0,
                    "<=" => string.Compare(actualString, expectedValue, StringComparison.OrdinalIgnoreCase) <= 0,
                    _ => false
                };
            }
        }

        return false;
    }

    private void ExecuteSimpleAction(string action, Dictionary<string, object> data)
    {
        // Parse simple actions like "genderDisplay = 'Male'"
        var match = Regex.Match(action, @"(\w+)\s*=\s*'([^']*)'");
        if (match.Success)
        {
            var fieldName = match.Groups[1].Value;
            var value = match.Groups[2].Value;
            data[fieldName] = value;
        }
    }

    private ValidationResult ValidateRule(CustomMappingRule rule)
    {
        var result = new ValidationResult { IsValid = true };

        if (string.IsNullOrEmpty(rule.Name))
        {
            result.AddError("Rule name cannot be empty");
        }

        if (string.IsNullOrEmpty(rule.Expression))
        {
            result.AddError("Rule expression cannot be empty");
        }

        if (rule.AppliesTo == null || rule.AppliesTo.Count == 0)
        {
            result.AddWarning("Rule does not specify any target fields");
        }

        // Validate expression syntax based on rule type
        switch (rule.RuleType.ToLowerInvariant())
        {
            case "conditional_mapping":
                if (!rule.Expression.Contains("if") || !rule.Expression.Contains("then"))
                {
                    result.AddError("Conditional mapping rules must contain 'if' and 'then' keywords");
                }
                break;

            case "built_in":
                if (!_builtInRules.ContainsKey(rule.Expression))
                {
                    result.AddError($"Unknown built-in rule: {rule.Expression}");
                }
                break;
        }

        return result;
    }

    private Dictionary<string, Func<Dictionary<string, object>, Dictionary<string, object>, Dictionary<string, object>>> InitializeBuiltInRules()
    {
        return new Dictionary<string, Func<Dictionary<string, object>, Dictionary<string, object>, Dictionary<string, object>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["concatenate_name"] = (data, metadata) =>
            {
                if (data.TryGetValue("firstName", out var first) && data.TryGetValue("lastName", out var last))
                {
                    data["fullName"] = $"{first} {last}";
                }
                return data;
            },

            ["calculate_age"] = (data, metadata) =>
            {
                if (data.TryGetValue("birthDate", out var birthDateObj) && DateTime.TryParse(birthDateObj.ToString(), out var birthDate))
                {
                    var age = DateTime.Now.Year - birthDate.Year;
                    if (DateTime.Now.DayOfYear < birthDate.DayOfYear) age--;
                    data["age"] = age;
                }
                return data;
            },

            ["format_phone"] = (data, metadata) =>
            {
                foreach (var key in data.Keys.Where(k => k.ToLowerInvariant().Contains("phone")).ToList())
                {
                    if (data[key] is string phone && !string.IsNullOrEmpty(phone))
                    {
                        var digits = new string(phone.Where(char.IsDigit).ToArray());
                        if (digits.Length == 10)
                        {
                            data[key] = $"({digits.Substring(0, 3)}) {digits.Substring(3, 3)}-{digits.Substring(6, 4)}";
                        }
                    }
                }
                return data;
            },

            ["normalize_gender"] = (data, metadata) =>
            {
                if (data.TryGetValue("gender", out var genderObj))
                {
                    var gender = genderObj.ToString()?.ToUpperInvariant();
                    data["genderDisplay"] = gender switch
                    {
                        "M" => "Male",
                        "F" => "Female",
                        "O" => "Other",
                        _ => "Unknown"
                    };
                }
                return data;
            }
        };
    }
}