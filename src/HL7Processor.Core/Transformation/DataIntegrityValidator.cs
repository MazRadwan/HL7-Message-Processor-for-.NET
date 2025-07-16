using HL7Processor.Core.Models;
using HL7Processor.Core.Extensions;
using HL7Processor.Core.Exceptions;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using System.Globalization;

namespace HL7Processor.Core.Transformation;

public class DataIntegrityValidator
{
    private readonly ILogger<DataIntegrityValidator> _logger;
    private readonly Dictionary<string, Func<string, bool>> _validationFunctions;

    public DataIntegrityValidator(ILogger<DataIntegrityValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _validationFunctions = InitializeValidationFunctions();
    }

    public IntegrityValidationResult ValidateTransformedData(Dictionary<string, object> transformedData, 
        HL7Message originalMessage, FieldMappingConfiguration mappingConfig)
    {
        if (transformedData == null) throw new ArgumentNullException(nameof(transformedData));
        if (originalMessage == null) throw new ArgumentNullException(nameof(originalMessage));
        if (mappingConfig == null) throw new ArgumentNullException(nameof(mappingConfig));

        var result = new IntegrityValidationResult
        {
            IsValid = true,
            MessageId = originalMessage.Id,
            ValidationTimestamp = DateTime.UtcNow
        };

        try
        {
            // Validate required fields
            ValidateRequiredFields(transformedData, mappingConfig, result);

            // Validate field formats
            ValidateFieldFormats(transformedData, mappingConfig, result);

            // Validate cross-field consistency
            ValidateCrossFieldConsistency(transformedData, result);

            // Validate data completeness
            ValidateDataCompleteness(transformedData, originalMessage, result);

            // Validate business rules
            ValidateBusinessRules(transformedData, mappingConfig, result);

            // Validate data ranges
            ValidateDataRanges(transformedData, mappingConfig, result);

            // Calculate integrity score
            result.IntegrityScore = CalculateIntegrityScore(result);

            _logger.LogInformation("Data integrity validation completed for message {MessageId} with score {Score}", 
                originalMessage.Id, result.IntegrityScore);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate data integrity for message {MessageId}: {Error}", 
                originalMessage.Id, ex.Message);
            result.AddError($"Validation failed: {ex.Message}");
            return result;
        }
    }

    public IntegrityValidationResult ValidateDataConsistency(Dictionary<string, object> data1, 
        Dictionary<string, object> data2, string[] keyFields)
    {
        if (data1 == null) throw new ArgumentNullException(nameof(data1));
        if (data2 == null) throw new ArgumentNullException(nameof(data2));
        if (keyFields == null) throw new ArgumentNullException(nameof(keyFields));

        var result = new IntegrityValidationResult
        {
            IsValid = true,
            ValidationTimestamp = DateTime.UtcNow
        };

        foreach (var keyField in keyFields)
        {
            var value1 = data1.GetValueOrDefault(keyField)?.ToString();
            var value2 = data2.GetValueOrDefault(keyField)?.ToString();

            if (!string.Equals(value1, value2, StringComparison.OrdinalIgnoreCase))
            {
                result.AddError($"Inconsistent values for field '{keyField}': '{value1}' vs '{value2}'");
            }
        }

        // Check for missing fields
        var missingInData1 = data2.Keys.Except(data1.Keys).ToList();
        var missingInData2 = data1.Keys.Except(data2.Keys).ToList();

        foreach (var missing in missingInData1)
        {
            result.AddWarning($"Field '{missing}' is missing in first dataset");
        }

        foreach (var missing in missingInData2)
        {
            result.AddWarning($"Field '{missing}' is missing in second dataset");
        }

        result.IntegrityScore = CalculateIntegrityScore(result);
        return result;
    }

    public IntegrityValidationResult ValidateFieldIntegrity(string fieldName, object value, 
        FieldMapping mapping, Dictionary<string, object> context)
    {
        if (mapping == null) throw new ArgumentNullException(nameof(mapping));

        var result = new IntegrityValidationResult
        {
            IsValid = true,
            ValidationTimestamp = DateTime.UtcNow
        };

        var stringValue = value?.ToString() ?? string.Empty;

        // Check if required field is present
        if (mapping.IsRequired && string.IsNullOrEmpty(stringValue))
        {
            result.AddError($"Required field '{fieldName}' is missing or empty");
            return result;
        }

        // Skip validation for empty optional fields
        if (string.IsNullOrEmpty(stringValue) && !mapping.IsRequired)
        {
            return result;
        }

        // Validate against pattern if specified
        if (!string.IsNullOrEmpty(mapping.ValidationPattern))
        {
            try
            {
                if (!Regex.IsMatch(stringValue, mapping.ValidationPattern))
                {
                    result.AddError($"Field '{fieldName}' value '{stringValue}' does not match pattern '{mapping.ValidationPattern}'");
                }
            }
            catch (Exception ex)
            {
                result.AddError($"Invalid validation pattern for field '{fieldName}': {ex.Message}");
            }
        }

        // Validate data type
        if (!ValidateDataType(stringValue, mapping.DataType))
        {
            result.AddError($"Field '{fieldName}' value '{stringValue}' is not a valid {mapping.DataType}");
        }

        // Validate length constraints
        if (mapping.Metadata.TryGetValue("MaxLength", out var maxLengthObj) && 
            int.TryParse(maxLengthObj.ToString(), out var maxLength))
        {
            if (stringValue.Length > maxLength)
            {
                result.AddError($"Field '{fieldName}' value exceeds maximum length of {maxLength}");
            }
        }

        if (mapping.Metadata.TryGetValue("MinLength", out var minLengthObj) && 
            int.TryParse(minLengthObj.ToString(), out var minLength))
        {
            if (stringValue.Length < minLength)
            {
                result.AddError($"Field '{fieldName}' value is below minimum length of {minLength}");
            }
        }

        result.IntegrityScore = CalculateIntegrityScore(result);
        return result;
    }

    public List<IntegrityValidationResult> ValidateBatchData(IEnumerable<Dictionary<string, object>> dataSet, 
        FieldMappingConfiguration mappingConfig)
    {
        if (dataSet == null) throw new ArgumentNullException(nameof(dataSet));
        if (mappingConfig == null) throw new ArgumentNullException(nameof(mappingConfig));

        var results = new List<IntegrityValidationResult>();
        var dataList = dataSet.ToList();

        for (int i = 0; i < dataList.Count; i++)
        {
            var data = dataList[i];
            var result = new IntegrityValidationResult
            {
                IsValid = true,
                ValidationTimestamp = DateTime.UtcNow,
                RecordIndex = i
            };

            // Validate individual record
            ValidateRequiredFields(data, mappingConfig, result);
            ValidateFieldFormats(data, mappingConfig, result);
            ValidateCrossFieldConsistency(data, result);

            // Validate uniqueness constraints
            ValidateUniquenessConstraints(data, dataList, i, mappingConfig, result);

            result.IntegrityScore = CalculateIntegrityScore(result);
            results.Add(result);
        }

        return results;
    }

    private void ValidateRequiredFields(Dictionary<string, object> data, FieldMappingConfiguration mappingConfig, 
        IntegrityValidationResult result)
    {
        var requiredMappings = mappingConfig.Mappings.Where(m => m.IsRequired).ToList();

        foreach (var mapping in requiredMappings)
        {
            if (!data.ContainsKey(mapping.TargetField) || 
                string.IsNullOrEmpty(data[mapping.TargetField]?.ToString()))
            {
                result.AddError($"Required field '{mapping.TargetField}' is missing or empty");
            }
        }
    }

    private void ValidateFieldFormats(Dictionary<string, object> data, FieldMappingConfiguration mappingConfig, 
        IntegrityValidationResult result)
    {
        foreach (var mapping in mappingConfig.Mappings)
        {
            if (data.TryGetValue(mapping.TargetField, out var value))
            {
                var fieldResult = ValidateFieldIntegrity(mapping.TargetField, value, mapping, data);
                result.Errors.AddRange(fieldResult.Errors);
                result.Warnings.AddRange(fieldResult.Warnings);
            }
        }
    }

    private void ValidateCrossFieldConsistency(Dictionary<string, object> data, IntegrityValidationResult result)
    {
        // Validate name consistency
        if (data.TryGetValue("firstName", out var firstName) && 
            data.TryGetValue("lastName", out var lastName) &&
            data.TryGetValue("fullName", out var fullName))
        {
            var expectedFullName = $"{firstName} {lastName}";
            if (!string.Equals(fullName?.ToString(), expectedFullName, StringComparison.OrdinalIgnoreCase))
            {
                result.AddWarning($"Full name '{fullName}' does not match first name '{firstName}' and last name '{lastName}'");
            }
        }

        // Validate age consistency with birth date
        if (data.TryGetValue("birthDate", out var birthDateObj) && 
            data.TryGetValue("age", out var ageObj))
        {
            if (DateTime.TryParse(birthDateObj?.ToString(), out var birthDate) &&
                int.TryParse(ageObj?.ToString(), out var age))
            {
                var calculatedAge = DateTime.Now.Year - birthDate.Year;
                if (DateTime.Now.DayOfYear < birthDate.DayOfYear) calculatedAge--;

                if (Math.Abs(calculatedAge - age) > 1)
                {
                    result.AddWarning($"Age '{age}' is inconsistent with birth date '{birthDate:yyyy-MM-dd}' (calculated: {calculatedAge})");
                }
            }
        }

        // Validate gender consistency
        if (data.TryGetValue("gender", out var genderObj))
        {
            var gender = genderObj?.ToString()?.ToUpperInvariant();
            if (gender != null && !new[] { "M", "F", "O", "U", "MALE", "FEMALE", "OTHER", "UNKNOWN" }.Contains(gender))
            {
                result.AddError($"Invalid gender value: '{gender}'");
            }
        }

        // Validate date consistency
        if (data.TryGetValue("admissionDate", out var admissionObj) && 
            data.TryGetValue("dischargeDate", out var dischargeObj))
        {
            if (DateTime.TryParse(admissionObj?.ToString(), out var admissionDate) &&
                DateTime.TryParse(dischargeObj?.ToString(), out var dischargeDate))
            {
                if (dischargeDate < admissionDate)
                {
                    result.AddError($"Discharge date '{dischargeDate:yyyy-MM-dd}' cannot be before admission date '{admissionDate:yyyy-MM-dd}'");
                }
            }
        }
    }

    private void ValidateDataCompleteness(Dictionary<string, object> data, HL7Message originalMessage, 
        IntegrityValidationResult result)
    {
        var expectedFields = new[] { "patientId", "messageType", "timestamp" };
        var missingFields = expectedFields.Where(field => !data.ContainsKey(field) || 
            string.IsNullOrEmpty(data[field]?.ToString())).ToList();

        foreach (var missingField in missingFields)
        {
            result.AddWarning($"Expected field '{missingField}' is missing or empty");
        }

        // Check data loss during transformation
        var originalFieldCount = originalMessage.Segments.Sum(s => s.Fields.Count);
        var transformedFieldCount = data.Count;
        var dataLossPercentage = (double)(originalFieldCount - transformedFieldCount) / originalFieldCount * 100;

        if (dataLossPercentage > 20)
        {
            result.AddWarning($"Significant data loss detected: {dataLossPercentage:F1}% of original fields missing");
        }

        result.AddMetadata("OriginalFieldCount", originalFieldCount);
        result.AddMetadata("TransformedFieldCount", transformedFieldCount);
        result.AddMetadata("DataLossPercentage", dataLossPercentage);
    }

    private void ValidateBusinessRules(Dictionary<string, object> data, FieldMappingConfiguration mappingConfig, 
        IntegrityValidationResult result)
    {
        // Apply custom validation rules
        foreach (var rule in mappingConfig.CustomRules.Where(r => r.IsActive && r.RuleType == "validation"))
        {
            try
            {
                var isValid = EvaluateValidationRule(rule, data);
                if (!isValid)
                {
                    result.AddError($"Business rule violation: {rule.Description}");
                }
            }
            catch (Exception ex)
            {
                result.AddError($"Failed to evaluate business rule '{rule.Name}': {ex.Message}");
            }
        }

        // Common business rules
        if (data.TryGetValue("patientClass", out var patientClassObj))
        {
            var patientClass = patientClassObj?.ToString()?.ToUpperInvariant();
            if (patientClass == "I" && !data.ContainsKey("roomNumber"))
            {
                result.AddWarning("Inpatient should have room number");
            }
        }

        if (data.TryGetValue("age", out var ageObj) && int.TryParse(ageObj?.ToString(), out var age))
        {
            if (age < 0 || age > 150)
            {
                result.AddError($"Invalid age: {age}");
            }
        }
    }

    private void ValidateDataRanges(Dictionary<string, object> data, FieldMappingConfiguration mappingConfig, 
        IntegrityValidationResult result)
    {
        foreach (var mapping in mappingConfig.Mappings)
        {
            if (data.TryGetValue(mapping.TargetField, out var value))
            {
                var stringValue = value?.ToString();
                if (string.IsNullOrEmpty(stringValue)) continue;

                // Check numeric ranges
                if (mapping.DataType.Contains("int") || mapping.DataType.Contains("decimal"))
                {
                    ValidateNumericRange(mapping.TargetField, stringValue, mapping, result);
                }

                // Check date ranges
                if (mapping.DataType.Contains("date"))
                {
                    ValidateDateRange(mapping.TargetField, stringValue, mapping, result);
                }

                // Check string length ranges
                if (mapping.DataType.Contains("string"))
                {
                    ValidateStringLength(mapping.TargetField, stringValue, mapping, result);
                }
            }
        }
    }

    private void ValidateNumericRange(string fieldName, string value, FieldMapping mapping, IntegrityValidationResult result)
    {
        if (mapping.Metadata.TryGetValue("MinValue", out var minValueObj) && 
            decimal.TryParse(minValueObj.ToString(), out var minValue) &&
            decimal.TryParse(value, out var numericValue))
        {
            if (numericValue < minValue)
            {
                result.AddError($"Field '{fieldName}' value {numericValue} is below minimum {minValue}");
            }
        }

        if (mapping.Metadata.TryGetValue("MaxValue", out var maxValueObj) && 
            decimal.TryParse(maxValueObj.ToString(), out var maxValue) &&
            decimal.TryParse(value, out numericValue))
        {
            if (numericValue > maxValue)
            {
                result.AddError($"Field '{fieldName}' value {numericValue} exceeds maximum {maxValue}");
            }
        }
    }

    private void ValidateDateRange(string fieldName, string value, FieldMapping mapping, IntegrityValidationResult result)
    {
        if (DateTime.TryParse(value, out var dateValue))
        {
            if (mapping.Metadata.TryGetValue("MinDate", out var minDateObj) && 
                DateTime.TryParse(minDateObj.ToString(), out var minDate))
            {
                if (dateValue < minDate)
                {
                    result.AddError($"Field '{fieldName}' date {dateValue:yyyy-MM-dd} is before minimum {minDate:yyyy-MM-dd}");
                }
            }

            if (mapping.Metadata.TryGetValue("MaxDate", out var maxDateObj) && 
                DateTime.TryParse(maxDateObj.ToString(), out var maxDate))
            {
                if (dateValue > maxDate)
                {
                    result.AddError($"Field '{fieldName}' date {dateValue:yyyy-MM-dd} is after maximum {maxDate:yyyy-MM-dd}");
                }
            }

            // Check for future dates where inappropriate
            if (fieldName.ToLowerInvariant().Contains("birth") && dateValue > DateTime.Now)
            {
                result.AddError($"Birth date cannot be in the future: {dateValue:yyyy-MM-dd}");
            }
        }
    }

    private void ValidateStringLength(string fieldName, string value, FieldMapping mapping, IntegrityValidationResult result)
    {
        if (mapping.Metadata.TryGetValue("MaxLength", out var maxLengthObj) && 
            int.TryParse(maxLengthObj.ToString(), out var maxLength))
        {
            if (value.Length > maxLength)
            {
                result.AddError($"Field '{fieldName}' exceeds maximum length of {maxLength}");
            }
        }

        if (mapping.Metadata.TryGetValue("MinLength", out var minLengthObj) && 
            int.TryParse(minLengthObj.ToString(), out var minLength))
        {
            if (value.Length < minLength)
            {
                result.AddError($"Field '{fieldName}' is below minimum length of {minLength}");
            }
        }
    }

    private void ValidateUniquenessConstraints(Dictionary<string, object> currentData, 
        List<Dictionary<string, object>> allData, int currentIndex, FieldMappingConfiguration mappingConfig, 
        IntegrityValidationResult result)
    {
        var uniqueFields = mappingConfig.Mappings
            .Where(m => m.Metadata.ContainsKey("IsUnique") && 
                       bool.TryParse(m.Metadata["IsUnique"].ToString(), out var isUnique) && isUnique)
            .Select(m => m.TargetField)
            .ToList();

        foreach (var uniqueField in uniqueFields)
        {
            if (currentData.TryGetValue(uniqueField, out var currentValue))
            {
                var currentValueStr = currentValue?.ToString();
                if (string.IsNullOrEmpty(currentValueStr)) continue;

                var duplicates = allData
                    .Where((data, index) => index != currentIndex && 
                           data.TryGetValue(uniqueField, out var otherValue) &&
                           string.Equals(currentValueStr, otherValue?.ToString(), StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (duplicates.Any())
                {
                    result.AddError($"Duplicate value '{currentValueStr}' found for unique field '{uniqueField}'");
                }
            }
        }
    }

    private bool ValidateDataType(string value, string dataType)
    {
        return dataType.ToLowerInvariant() switch
        {
            "int" or "integer" => int.TryParse(value, out _),
            "long" => long.TryParse(value, out _),
            "decimal" or "double" => decimal.TryParse(value, out _),
            "bool" or "boolean" => bool.TryParse(value, out _),
            "datetime" => DateTime.TryParse(value, out _),
            "date" => DateTime.TryParseExact(value, new[] { "yyyy-MM-dd", "yyyyMMdd", "MM/dd/yyyy" }, 
                CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
            "time" => DateTime.TryParseExact(value, new[] { "HH:mm:ss", "HHmmss", "HH:mm" }, 
                CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
            "email" => IsValidEmail(value),
            "phone" => IsValidPhone(value),
            "ssn" => IsValidSSN(value),
            "zipcode" => IsValidZipCode(value),
            _ => true // Default to valid for string types
        };
    }

    private bool EvaluateValidationRule(CustomMappingRule rule, Dictionary<string, object> data)
    {
        // Simple rule evaluation - in production, use a proper expression evaluator
        var expression = rule.Expression;
        
        // Replace data placeholders
        foreach (var kvp in data)
        {
            expression = expression.Replace($"{{{kvp.Key}}}", kvp.Value?.ToString() ?? string.Empty);
        }

        // Basic expression evaluation
        if (expression.Contains("required") && expression.Contains("not_empty"))
        {
            var fieldName = ExtractFieldNameFromExpression(expression);
            return data.ContainsKey(fieldName) && !string.IsNullOrEmpty(data[fieldName]?.ToString());
        }

        if (expression.Contains("in_range"))
        {
            return EvaluateRangeExpression(expression, data);
        }

        return true; // Default to valid
    }

    private string ExtractFieldNameFromExpression(string expression)
    {
        var match = Regex.Match(expression, @"(\w+)\.required");
        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    private bool EvaluateRangeExpression(string expression, Dictionary<string, object> data)
    {
        // Parse range expressions like "age in_range(0, 150)"
        var match = Regex.Match(expression, @"(\w+)\s+in_range\((\d+),\s*(\d+)\)");
        if (match.Success)
        {
            var fieldName = match.Groups[1].Value;
            var minValue = int.Parse(match.Groups[2].Value);
            var maxValue = int.Parse(match.Groups[3].Value);

            if (data.TryGetValue(fieldName, out var value) && int.TryParse(value?.ToString(), out var intValue))
            {
                return intValue >= minValue && intValue <= maxValue;
            }
        }

        return true;
    }

    private double CalculateIntegrityScore(IntegrityValidationResult result)
    {
        if (result.Errors.Count == 0 && result.Warnings.Count == 0)
            return 100.0;

        var errorPenalty = result.Errors.Count * 10;
        var warningPenalty = result.Warnings.Count * 2;
        var totalPenalty = errorPenalty + warningPenalty;

        return Math.Max(0, 100.0 - totalPenalty);
    }

    private Dictionary<string, Func<string, bool>> InitializeValidationFunctions()
    {
        return new Dictionary<string, Func<string, bool>>(StringComparer.OrdinalIgnoreCase)
        {
            ["email"] = IsValidEmail,
            ["phone"] = IsValidPhone,
            ["ssn"] = IsValidSSN,
            ["zipcode"] = IsValidZipCode,
            ["url"] = IsValidUrl,
            ["guid"] = IsValidGuid,
            ["ip"] = IsValidIpAddress
        };
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private bool IsValidPhone(string phone)
    {
        var phonePattern = @"^[\+]?[1-9][\d]{0,15}$";
        var cleaned = Regex.Replace(phone, @"[^\d\+]", "");
        return Regex.IsMatch(cleaned, phonePattern);
    }

    private bool IsValidSSN(string ssn)
    {
        var ssnPattern = @"^\d{3}-?\d{2}-?\d{4}$";
        return Regex.IsMatch(ssn, ssnPattern);
    }

    private bool IsValidZipCode(string zipCode)
    {
        var zipPattern = @"^\d{5}(-\d{4})?$";
        return Regex.IsMatch(zipCode, zipPattern);
    }

    private bool IsValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out _);
    }

    private bool IsValidGuid(string guid)
    {
        return Guid.TryParse(guid, out _);
    }

    private bool IsValidIpAddress(string ip)
    {
        return System.Net.IPAddress.TryParse(ip, out _);
    }
}

public class IntegrityValidationResult
{
    public bool IsValid { get; set; } = true;
    public string MessageId { get; set; } = string.Empty;
    public int? RecordIndex { get; set; }
    public DateTime ValidationTimestamp { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public double IntegrityScore { get; set; } = 100.0;

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
        return $"IntegrityValidationResult: IsValid={IsValid}, Score={IntegrityScore:F1}, Errors={Errors.Count}, Warnings={Warnings.Count}";
    }
}