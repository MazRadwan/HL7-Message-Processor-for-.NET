using HL7Processor.Core.Models;
using HL7Processor.Core.Extensions;
using HL7Processor.Core.Exceptions;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Globalization;

namespace HL7Processor.Core.Transformation;

public class FormatComplianceChecker
{
    private readonly ILogger<FormatComplianceChecker> _logger;
    private readonly Dictionary<string, FormatSpecification> _formatSpecifications;

    public FormatComplianceChecker(ILogger<FormatComplianceChecker> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _formatSpecifications = InitializeFormatSpecifications();
    }

    public ComplianceCheckResult CheckHL7Compliance(HL7Message message, string targetVersion = "2.5")
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        var result = new ComplianceCheckResult
        {
            IsCompliant = true,
            MessageId = message.Id,
            TargetFormat = $"HL7 v{targetVersion}",
            CheckTimestamp = DateTime.UtcNow
        };

        try
        {
            // Check message structure compliance
            CheckMessageStructure(message, targetVersion, result);

            // Check segment compliance
            CheckSegmentCompliance(message, targetVersion, result);

            // Check field compliance
            CheckFieldCompliance(message, targetVersion, result);

            // Check data type compliance
            CheckDataTypeCompliance(message, targetVersion, result);

            // Check encoding compliance
            CheckEncodingCompliance(message, result);

            // Check version-specific rules
            CheckVersionSpecificRules(message, targetVersion, result);

            // Calculate compliance score
            result.ComplianceScore = CalculateComplianceScore(result);

            _logger.LogInformation("HL7 compliance check completed for message {MessageId} with score {Score}", 
                message.Id, result.ComplianceScore);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check HL7 compliance for message {MessageId}: {Error}", 
                message.Id, ex.Message);
            result.AddError($"Compliance check failed: {ex.Message}");
            return result;
        }
    }

    public ComplianceCheckResult CheckJSONCompliance(string json, string targetSchema = "HL7-JSON")
    {
        if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("JSON cannot be null or empty", nameof(json));

        var result = new ComplianceCheckResult
        {
            IsCompliant = true,
            TargetFormat = targetSchema,
            CheckTimestamp = DateTime.UtcNow
        };

        try
        {
            // Parse JSON
            var jsonDocument = JsonDocument.Parse(json);
            var root = jsonDocument.RootElement;

            // Check JSON structure
            CheckJSONStructure(root, result);

            // Check required properties
            CheckJSONRequiredProperties(root, result);

            // Check data types
            CheckJSONDataTypes(root, result);

            // Check format constraints
            CheckJSONFormatConstraints(root, result);

            result.ComplianceScore = CalculateComplianceScore(result);

            _logger.LogInformation("JSON compliance check completed with score {Score}", result.ComplianceScore);

            return result;
        }
        catch (JsonException ex)
        {
            result.AddError($"Invalid JSON format: {ex.Message}");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check JSON compliance: {Error}", ex.Message);
            result.AddError($"JSON compliance check failed: {ex.Message}");
            return result;
        }
    }

    public ComplianceCheckResult CheckFHIRCompliance(string fhirJson, string targetVersion = "R4")
    {
        if (string.IsNullOrWhiteSpace(fhirJson)) throw new ArgumentException("FHIR JSON cannot be null or empty", nameof(fhirJson));

        var result = new ComplianceCheckResult
        {
            IsCompliant = true,
            TargetFormat = $"FHIR {targetVersion}",
            CheckTimestamp = DateTime.UtcNow
        };

        try
        {
            var jsonDocument = JsonDocument.Parse(fhirJson);
            var root = jsonDocument.RootElement;

            // Check FHIR resource structure
            CheckFHIRResourceStructure(root, result);

            // Check FHIR required elements
            CheckFHIRRequiredElements(root, result);

            // Check FHIR data types
            CheckFHIRDataTypes(root, result);

            // Check FHIR cardinality constraints
            CheckFHIRCardinality(root, result);

            // Check FHIR terminology bindings
            CheckFHIRTerminologyBindings(root, result);

            result.ComplianceScore = CalculateComplianceScore(result);

            _logger.LogInformation("FHIR compliance check completed with score {Score}", result.ComplianceScore);

            return result;
        }
        catch (JsonException ex)
        {
            result.AddError($"Invalid FHIR JSON format: {ex.Message}");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check FHIR compliance: {Error}", ex.Message);
            result.AddError($"FHIR compliance check failed: {ex.Message}");
            return result;
        }
    }

    public ComplianceCheckResult CheckXMLCompliance(string xml, string targetSchema = "HL7-XML")
    {
        if (string.IsNullOrWhiteSpace(xml)) throw new ArgumentException("XML cannot be null or empty", nameof(xml));

        var result = new ComplianceCheckResult
        {
            IsCompliant = true,
            TargetFormat = targetSchema,
            CheckTimestamp = DateTime.UtcNow
        };

        try
        {
            var doc = System.Xml.Linq.XDocument.Parse(xml);

            // Check XML structure
            CheckXMLStructure(doc, result);

            // Check XML namespaces
            CheckXMLNamespaces(doc, result);

            // Check XML elements
            CheckXMLElements(doc, result);

            // Check XML attributes
            CheckXMLAttributes(doc, result);

            result.ComplianceScore = CalculateComplianceScore(result);

            _logger.LogInformation("XML compliance check completed with score {Score}", result.ComplianceScore);

            return result;
        }
        catch (System.Xml.XmlException ex)
        {
            result.AddError($"Invalid XML format: {ex.Message}");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check XML compliance: {Error}", ex.Message);
            result.AddError($"XML compliance check failed: {ex.Message}");
            return result;
        }
    }

    public List<ComplianceCheckResult> CheckBatchCompliance(IEnumerable<HL7Message> messages, string targetVersion = "2.5")
    {
        if (messages == null) throw new ArgumentNullException(nameof(messages));

        var results = new List<ComplianceCheckResult>();
        var messageList = messages.ToList();

        foreach (var message in messageList)
        {
            var result = CheckHL7Compliance(message, targetVersion);
            results.Add(result);
        }

        // Check batch-level compliance
        var batchResult = CheckBatchLevelCompliance(messageList, results);
        results.Add(batchResult);

        return results;
    }

    private void CheckMessageStructure(HL7Message message, string targetVersion, ComplianceCheckResult result)
    {
        // Check MSH segment presence
        var mshSegment = message.GetSegment("MSH");
        if (mshSegment == null)
        {
            result.AddError("Missing required MSH (Message Header) segment");
            return;
        }

        // Check message type specific structure
        CheckMessageTypeSpecificStructure(message, result);

        // Check segment order
        CheckSegmentOrder(message, result);

        // Check segment cardinality
        CheckSegmentCardinality(message, result);
    }

    private void CheckMessageTypeSpecificStructure(HL7Message message, ComplianceCheckResult result)
    {
        switch (message.MessageType)
        {
            case HL7MessageType.ADT_A01:
            case HL7MessageType.ADT_A04:
            case HL7MessageType.ADT_A08:
                if (!message.HasSegment("EVN"))
                    result.AddError("ADT messages require EVN segment");
                if (!message.HasSegment("PID"))
                    result.AddError("ADT messages require PID segment");
                break;

            case HL7MessageType.ORM_O01:
                if (!message.HasSegment("ORC"))
                    result.AddError("ORM messages require ORC segment");
                if (!message.HasSegment("OBR"))
                    result.AddError("ORM messages require OBR segment");
                break;

            case HL7MessageType.ORU_R01:
                if (!message.HasSegment("OBR"))
                    result.AddError("ORU messages require OBR segment");
                if (!message.HasSegment("OBX"))
                    result.AddError("ORU messages require OBX segment");
                break;
        }
    }

    private void CheckSegmentOrder(HL7Message message, ComplianceCheckResult result)
    {
        var segmentOrder = message.Segments.Select(s => s.Type).ToList();
        
        // MSH should always be first
        if (segmentOrder.FirstOrDefault() != "MSH")
        {
            result.AddError("MSH segment must be first");
        }

        // Check common ordering rules
        var mshIndex = segmentOrder.IndexOf("MSH");
        var evnIndex = segmentOrder.IndexOf("EVN");
        var pidIndex = segmentOrder.IndexOf("PID");
        var pv1Index = segmentOrder.IndexOf("PV1");

        if (evnIndex >= 0 && evnIndex <= mshIndex)
        {
            result.AddError("EVN segment must come after MSH");
        }

        if (pidIndex >= 0 && evnIndex >= 0 && pidIndex <= evnIndex)
        {
            result.AddError("PID segment must come after EVN");
        }

        if (pv1Index >= 0 && pidIndex >= 0 && pv1Index <= pidIndex)
        {
            result.AddError("PV1 segment must come after PID");
        }
    }

    private void CheckSegmentCardinality(HL7Message message, ComplianceCheckResult result)
    {
        var segmentCounts = message.Segments.GroupBy(s => s.Type).ToDictionary(g => g.Key, g => g.Count());

        // MSH should appear exactly once
        if (segmentCounts.GetValueOrDefault("MSH", 0) != 1)
        {
            result.AddError("MSH segment must appear exactly once");
        }

        // EVN should appear at most once
        if (segmentCounts.GetValueOrDefault("EVN", 0) > 1)
        {
            result.AddError("EVN segment can appear at most once");
        }

        // PID should appear at most once per patient
        if (segmentCounts.GetValueOrDefault("PID", 0) > 1)
        {
            result.AddWarning("Multiple PID segments detected - verify if multiple patients intended");
        }
    }

    private void CheckSegmentCompliance(HL7Message message, string targetVersion, ComplianceCheckResult result)
    {
        foreach (var segment in message.Segments)
        {
            CheckSegmentStructure(segment, result);
            CheckSegmentRequiredFields(segment, result);
            CheckSegmentFieldLengths(segment, result);
        }
    }

    private void CheckSegmentStructure(HL7Segment segment, ComplianceCheckResult result)
    {
        // Check segment type format
        if (segment.Type.Length != 3)
        {
            result.AddError($"Segment type '{segment.Type}' must be exactly 3 characters");
        }

        if (!segment.Type.All(char.IsLetterOrDigit))
        {
            result.AddError($"Segment type '{segment.Type}' must contain only letters and digits");
        }

        // Check field separator for MSH
        if (segment.Type == "MSH")
        {
            var fieldSeparator = segment.GetFieldValue(1);
            if (fieldSeparator != "|")
            {
                result.AddError("MSH-1 (Field Separator) must be '|'");
            }

            var encodingChars = segment.GetFieldValue(2);
            if (string.IsNullOrEmpty(encodingChars) || encodingChars.Length != 4)
            {
                result.AddError("MSH-2 (Encoding Characters) must be exactly 4 characters");
            }
        }
    }

    private void CheckSegmentRequiredFields(HL7Segment segment, ComplianceCheckResult result)
    {
        var requiredFields = GetRequiredFields(segment.Type);
        
        foreach (var fieldNumber in requiredFields)
        {
            var fieldValue = segment.GetFieldValue(fieldNumber);
            if (string.IsNullOrEmpty(fieldValue))
            {
                result.AddError($"{segment.Type}-{fieldNumber} is required but missing");
            }
        }
    }

    private void CheckSegmentFieldLengths(HL7Segment segment, ComplianceCheckResult result)
    {
        var fieldLengthConstraints = GetFieldLengthConstraints(segment.Type);
        
        foreach (var constraint in fieldLengthConstraints)
        {
            var fieldValue = segment.GetFieldValue(constraint.FieldNumber);
            if (!string.IsNullOrEmpty(fieldValue) && fieldValue.Length > constraint.MaxLength)
            {
                result.AddError($"{segment.Type}-{constraint.FieldNumber} exceeds maximum length of {constraint.MaxLength}");
            }
        }
    }

    private void CheckFieldCompliance(HL7Message message, string targetVersion, ComplianceCheckResult result)
    {
        foreach (var segment in message.Segments)
        {
            foreach (var field in segment.Fields)
            {
                CheckFieldDataType(segment.Type, field, result);
                CheckFieldFormat(segment.Type, field, result);
                CheckFieldValues(segment.Type, field, result);
            }
        }
    }

    private void CheckFieldDataType(string segmentType, HL7Field field, ComplianceCheckResult result)
    {
        var expectedDataType = GetExpectedDataType(segmentType, field.Position);
        if (expectedDataType != null && field.DataType != expectedDataType)
        {
            result.AddWarning($"{segmentType}-{field.Position} expected data type {expectedDataType}, found {field.DataType}");
        }
    }

    private void CheckFieldFormat(string segmentType, HL7Field field, ComplianceCheckResult result)
    {
        var formatPattern = GetFieldFormatPattern(segmentType, field.Position);
        if (formatPattern != null && !string.IsNullOrEmpty(field.Value))
        {
            if (!Regex.IsMatch(field.Value, formatPattern))
            {
                result.AddError($"{segmentType}-{field.Position} value '{field.Value}' does not match required format");
            }
        }
    }

    private void CheckFieldValues(string segmentType, HL7Field field, ComplianceCheckResult result)
    {
        var validValues = GetValidFieldValues(segmentType, field.Position);
        if (validValues != null && validValues.Any() && !string.IsNullOrEmpty(field.Value))
        {
            if (!validValues.Contains(field.Value, StringComparer.OrdinalIgnoreCase))
            {
                result.AddError($"{segmentType}-{field.Position} value '{field.Value}' is not in valid value set");
            }
        }
    }

    private void CheckDataTypeCompliance(HL7Message message, string targetVersion, ComplianceCheckResult result)
    {
        foreach (var segment in message.Segments)
        {
            foreach (var field in segment.Fields)
            {
                CheckDataTypeFormat(field, result);
                CheckDataTypeConstraints(field, result);
            }
        }
    }

    private void CheckDataTypeFormat(HL7Field field, ComplianceCheckResult result)
    {
        switch (field.DataType.ToUpperInvariant())
        {
            case "DT": // Date
                if (!IsValidHL7Date(field.Value))
                {
                    result.AddError($"Invalid date format: {field.Value}");
                }
                break;

            case "TM": // Time
                if (!IsValidHL7Time(field.Value))
                {
                    result.AddError($"Invalid time format: {field.Value}");
                }
                break;

            case "TS": // Timestamp
                if (!IsValidHL7Timestamp(field.Value))
                {
                    result.AddError($"Invalid timestamp format: {field.Value}");
                }
                break;

            case "NM": // Numeric
                if (!IsValidHL7Numeric(field.Value))
                {
                    result.AddError($"Invalid numeric format: {field.Value}");
                }
                break;

            case "SI": // Sequence ID
                if (!IsValidHL7SequenceId(field.Value))
                {
                    result.AddError($"Invalid sequence ID format: {field.Value}");
                }
                break;
        }
    }

    private void CheckDataTypeConstraints(HL7Field field, ComplianceCheckResult result)
    {
        if (field.MaxLength > 0 && field.Value.Length > field.MaxLength)
        {
            result.AddError($"Field value exceeds maximum length of {field.MaxLength}");
        }

        if (field.IsRequired && string.IsNullOrEmpty(field.Value))
        {
            result.AddError($"Required field is missing or empty");
        }
    }

    private void CheckEncodingCompliance(HL7Message message, ComplianceCheckResult result)
    {
        var mshSegment = message.GetSegment("MSH");
        if (mshSegment == null) return;

        var encodingChars = mshSegment.GetFieldValue(2);
        if (string.IsNullOrEmpty(encodingChars) || encodingChars.Length != 4)
        {
            result.AddError("Invalid encoding characters specification");
            return;
        }

        var componentSeparator = encodingChars[0];
        var repetitionSeparator = encodingChars[1];
        var escapeCharacter = encodingChars[2];
        var subComponentSeparator = encodingChars[3];

        // Check for proper separator usage
        foreach (var segment in message.Segments)
        {
            CheckSeparatorUsage(segment, componentSeparator, repetitionSeparator, escapeCharacter, subComponentSeparator, result);
        }
    }

    private void CheckSeparatorUsage(HL7Segment segment, char componentSeparator, char repetitionSeparator, 
        char escapeCharacter, char subComponentSeparator, ComplianceCheckResult result)
    {
        foreach (var field in segment.Fields)
        {
            if (field.Value.Contains(componentSeparator) && field.Components.Count <= 1)
            {
                result.AddWarning($"Field contains component separator but no components parsed in {segment.Type}-{field.Position}");
            }

            if (field.Value.Contains(subComponentSeparator))
            {
                var hasSubComponents = field.Components.Any(c => c.SubComponents.Count > 0);
                if (!hasSubComponents)
                {
                    result.AddWarning($"Field contains subcomponent separator but no subcomponents parsed in {segment.Type}-{field.Position}");
                }
            }
        }
    }

    private void CheckVersionSpecificRules(HL7Message message, string targetVersion, ComplianceCheckResult result)
    {
        switch (targetVersion)
        {
            case "2.3":
                CheckVersion23Rules(message, result);
                break;
            case "2.4":
                CheckVersion24Rules(message, result);
                break;
            case "2.5":
                CheckVersion25Rules(message, result);
                break;
            case "2.6":
                CheckVersion26Rules(message, result);
                break;
        }
    }

    private void CheckVersion23Rules(HL7Message message, ComplianceCheckResult result)
    {
        // Version 2.3 specific validation rules
        var mshSegment = message.GetSegment("MSH");
        if (mshSegment != null)
        {
            var versionId = mshSegment.GetFieldValue(12);
            if (versionId != "2.3")
            {
                result.AddWarning($"Version mismatch: expected 2.3, found {versionId}");
            }
        }
    }

    private void CheckVersion24Rules(HL7Message message, ComplianceCheckResult result)
    {
        // Version 2.4 specific validation rules
        var mshSegment = message.GetSegment("MSH");
        if (mshSegment != null)
        {
            var versionId = mshSegment.GetFieldValue(12);
            if (versionId != "2.4")
            {
                result.AddWarning($"Version mismatch: expected 2.4, found {versionId}");
            }
        }
    }

    private void CheckVersion25Rules(HL7Message message, ComplianceCheckResult result)
    {
        // Version 2.5 specific validation rules
        var mshSegment = message.GetSegment("MSH");
        if (mshSegment != null)
        {
            var versionId = mshSegment.GetFieldValue(12);
            if (versionId != "2.5")
            {
                result.AddWarning($"Version mismatch: expected 2.5, found {versionId}");
            }
        }
    }

    private void CheckVersion26Rules(HL7Message message, ComplianceCheckResult result)
    {
        // Version 2.6 specific validation rules
        var mshSegment = message.GetSegment("MSH");
        if (mshSegment != null)
        {
            var versionId = mshSegment.GetFieldValue(12);
            if (versionId != "2.6")
            {
                result.AddWarning($"Version mismatch: expected 2.6, found {versionId}");
            }
        }
    }

    private void CheckJSONStructure(JsonElement root, ComplianceCheckResult result)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            result.AddError("Root element must be a JSON object");
            return;
        }

        // Check for required top-level properties
        var requiredProperties = new[] { "id", "messageType", "version", "timestamp" };
        foreach (var property in requiredProperties)
        {
            if (!root.TryGetProperty(property, out _))
            {
                result.AddError($"Missing required property: {property}");
            }
        }
    }

    private void CheckJSONRequiredProperties(JsonElement root, ComplianceCheckResult result)
    {
        if (root.TryGetProperty("segments", out var segments) && segments.ValueKind == JsonValueKind.Array)
        {
            foreach (var segment in segments.EnumerateArray())
            {
                if (!segment.TryGetProperty("type", out _))
                {
                    result.AddError("Segment missing required 'type' property");
                }
            }
        }
    }

    private void CheckJSONDataTypes(JsonElement root, ComplianceCheckResult result)
    {
        if (root.TryGetProperty("timestamp", out var timestamp) && timestamp.ValueKind != JsonValueKind.String)
        {
            result.AddError("Timestamp must be a string");
        }

        if (root.TryGetProperty("isValid", out var isValid) && isValid.ValueKind != JsonValueKind.True && isValid.ValueKind != JsonValueKind.False)
        {
            result.AddError("isValid must be a boolean");
        }
    }

    private void CheckJSONFormatConstraints(JsonElement root, ComplianceCheckResult result)
    {
        if (root.TryGetProperty("timestamp", out var timestamp) && timestamp.ValueKind == JsonValueKind.String)
        {
            if (!DateTime.TryParse(timestamp.GetString(), out _))
            {
                result.AddError("Invalid timestamp format");
            }
        }
    }

    private void CheckFHIRResourceStructure(JsonElement root, ComplianceCheckResult result)
    {
        if (!root.TryGetProperty("resourceType", out var resourceType))
        {
            result.AddError("Missing required 'resourceType' property");
            return;
        }

        var resourceTypeValue = resourceType.GetString();
        if (string.IsNullOrEmpty(resourceTypeValue))
        {
            result.AddError("ResourceType cannot be empty");
        }

        // Check for required FHIR elements
        if (!root.TryGetProperty("id", out _))
        {
            result.AddError("Missing required 'id' property");
        }
    }

    private void CheckFHIRRequiredElements(JsonElement root, ComplianceCheckResult result)
    {
        var resourceType = root.GetProperty("resourceType").GetString();
        
        switch (resourceType)
        {
            case "Patient":
                // Patient specific required elements
                break;
            case "Observation":
                if (!root.TryGetProperty("status", out _))
                {
                    result.AddError("Observation missing required 'status' element");
                }
                break;
            case "Bundle":
                if (!root.TryGetProperty("type", out _))
                {
                    result.AddError("Bundle missing required 'type' element");
                }
                break;
        }
    }

    private void CheckFHIRDataTypes(JsonElement root, ComplianceCheckResult result)
    {
        // Check FHIR-specific data type formats
        if (root.TryGetProperty("birthDate", out var birthDate) && birthDate.ValueKind == JsonValueKind.String)
        {
            if (!DateTime.TryParseExact(birthDate.GetString(), "yyyy-MM-dd", null, DateTimeStyles.None, out _))
            {
                result.AddError("Invalid FHIR date format for birthDate");
            }
        }
    }

    private void CheckFHIRCardinality(JsonElement root, ComplianceCheckResult result)
    {
        // Check FHIR cardinality constraints
        if (root.TryGetProperty("identifier", out var identifier) && identifier.ValueKind == JsonValueKind.Array)
        {
            if (identifier.GetArrayLength() == 0)
            {
                result.AddWarning("Identifier array is empty but present");
            }
        }
    }

    private void CheckFHIRTerminologyBindings(JsonElement root, ComplianceCheckResult result)
    {
        // Check FHIR terminology bindings
        if (root.TryGetProperty("gender", out var gender) && gender.ValueKind == JsonValueKind.String)
        {
            var genderValue = gender.GetString();
            var validGenders = new[] { "male", "female", "other", "unknown" };
            if (!validGenders.Contains(genderValue))
            {
                result.AddError($"Invalid gender value: {genderValue}");
            }
        }
    }

    private void CheckXMLStructure(System.Xml.Linq.XDocument doc, ComplianceCheckResult result)
    {
        if (doc.Root == null)
        {
            result.AddError("XML document has no root element");
            return;
        }

        // Check for well-formed XML
        if (doc.Root.Name.LocalName != "HL7Message")
        {
            result.AddWarning("Expected root element name 'HL7Message'");
        }
    }

    private void CheckXMLNamespaces(System.Xml.Linq.XDocument doc, ComplianceCheckResult result)
    {
        // Check for proper namespace declarations
        var root = doc.Root;
        if (root != null && root.GetDefaultNamespace() == System.Xml.Linq.XNamespace.None)
        {
            result.AddWarning("No default namespace specified");
        }
    }

    private void CheckXMLElements(System.Xml.Linq.XDocument doc, ComplianceCheckResult result)
    {
        var root = doc.Root;
        if (root == null) return;

        // Check for required elements
        if (root.Element("Segments") == null)
        {
            result.AddError("Missing required 'Segments' element");
        }
    }

    private void CheckXMLAttributes(System.Xml.Linq.XDocument doc, ComplianceCheckResult result)
    {
        var root = doc.Root;
        if (root == null) return;

        // Check for required attributes
        if (root.Attribute("id") == null)
        {
            result.AddError("Missing required 'id' attribute");
        }

        if (root.Attribute("messageType") == null)
        {
            result.AddError("Missing required 'messageType' attribute");
        }
    }

    private ComplianceCheckResult CheckBatchLevelCompliance(List<HL7Message> messages, List<ComplianceCheckResult> results)
    {
        var batchResult = new ComplianceCheckResult
        {
            IsCompliant = true,
            MessageId = "BATCH",
            TargetFormat = "HL7 Batch",
            CheckTimestamp = DateTime.UtcNow
        };

        // Check batch consistency
        var messageTypes = messages.Select(m => m.MessageType).Distinct().ToList();
        if (messageTypes.Count > 1)
        {
            batchResult.AddWarning($"Batch contains mixed message types: {string.Join(", ", messageTypes)}");
        }

        // Check batch size limits
        if (messages.Count > 1000)
        {
            batchResult.AddWarning($"Large batch size: {messages.Count} messages");
        }

        // Calculate overall compliance
        var overallCompliance = results.Where(r => r.MessageId != "BATCH").Average(r => r.ComplianceScore);
        batchResult.ComplianceScore = overallCompliance;

        return batchResult;
    }

    private double CalculateComplianceScore(ComplianceCheckResult result)
    {
        if (result.Errors.Count == 0 && result.Warnings.Count == 0)
            return 100.0;

        var errorPenalty = result.Errors.Count * 15;
        var warningPenalty = result.Warnings.Count * 3;
        var totalPenalty = errorPenalty + warningPenalty;

        return Math.Max(0, 100.0 - totalPenalty);
    }

    private List<int> GetRequiredFields(string segmentType)
    {
        return segmentType switch
        {
            "MSH" => new List<int> { 1, 2, 3, 5, 7, 9, 10, 11, 12 },
            "EVN" => new List<int> { 1, 2 },
            "PID" => new List<int> { 3, 5 },
            "PV1" => new List<int> { 2 },
            "ORC" => new List<int> { 1 },
            "OBR" => new List<int> { 1, 4 },
            "OBX" => new List<int> { 1, 2, 3, 5, 11 },
            _ => new List<int>()
        };
    }

    private List<FieldLengthConstraint> GetFieldLengthConstraints(string segmentType)
    {
        return segmentType switch
        {
            "MSH" => new List<FieldLengthConstraint>
            {
                new() { FieldNumber = 1, MaxLength = 1 },
                new() { FieldNumber = 2, MaxLength = 4 },
                new() { FieldNumber = 3, MaxLength = 227 },
                new() { FieldNumber = 10, MaxLength = 20 },
                new() { FieldNumber = 12, MaxLength = 60 }
            },
            "PID" => new List<FieldLengthConstraint>
            {
                new() { FieldNumber = 3, MaxLength = 250 },
                new() { FieldNumber = 5, MaxLength = 250 }
            },
            _ => new List<FieldLengthConstraint>()
        };
    }

    private string? GetExpectedDataType(string segmentType, int fieldNumber)
    {
        return (segmentType, fieldNumber) switch
        {
            ("MSH", 1) => "ST",
            ("MSH", 2) => "ST",
            ("MSH", 7) => "TS",
            ("MSH", 10) => "ST",
            ("PID", 7) => "TS",
            ("PID", 8) => "IS",
            ("OBX", 2) => "ID",
            ("OBX", 5) => "Varies",
            _ => null
        };
    }

    private string? GetFieldFormatPattern(string segmentType, int fieldNumber)
    {
        return (segmentType, fieldNumber) switch
        {
            ("MSH", 7) => @"^\d{8}(\d{6})?$", // YYYYMMDD[HHMMSS]
            ("PID", 7) => @"^\d{8}$", // YYYYMMDD
            ("MSH", 10) => @"^[\w\-\.]+$", // Alphanumeric with hyphens and dots
            _ => null
        };
    }

    private string[]? GetValidFieldValues(string segmentType, int fieldNumber)
    {
        return (segmentType, fieldNumber) switch
        {
            ("PID", 8) => new[] { "F", "M", "O", "U" }, // Gender
            ("PV1", 2) => new[] { "E", "I", "O", "P", "R", "B", "C", "N" }, // Patient Class
            ("OBX", 11) => new[] { "F", "P", "R", "C", "X", "I", "N", "O", "D" }, // Observation Result Status
            _ => null
        };
    }

    private bool IsValidHL7Date(string value)
    {
        if (string.IsNullOrEmpty(value)) return true;
        return Regex.IsMatch(value, @"^\d{8}$") && DateTime.TryParseExact(value, "yyyyMMdd", null, DateTimeStyles.None, out _);
    }

    private bool IsValidHL7Time(string value)
    {
        if (string.IsNullOrEmpty(value)) return true;
        return Regex.IsMatch(value, @"^\d{6}(\.\d{1,4})?$");
    }

    private bool IsValidHL7Timestamp(string value)
    {
        if (string.IsNullOrEmpty(value)) return true;
        return Regex.IsMatch(value, @"^\d{8}(\d{6}(\.\d{1,4})?)?([+-]\d{4})?$");
    }

    private bool IsValidHL7Numeric(string value)
    {
        if (string.IsNullOrEmpty(value)) return true;
        return decimal.TryParse(value, out _);
    }

    private bool IsValidHL7SequenceId(string value)
    {
        if (string.IsNullOrEmpty(value)) return true;
        return int.TryParse(value, out var result) && result > 0;
    }

    private Dictionary<string, FormatSpecification> InitializeFormatSpecifications()
    {
        return new Dictionary<string, FormatSpecification>
        {
            ["HL7-2.5"] = new FormatSpecification
            {
                Name = "HL7 v2.5",
                Version = "2.5",
                RequiredSegments = new[] { "MSH" },
                OptionalSegments = new[] { "EVN", "PID", "PV1", "ORC", "OBR", "OBX" }
            },
            ["FHIR-R4"] = new FormatSpecification
            {
                Name = "FHIR R4",
                Version = "4.0.1",
                RequiredSegments = new[] { "resourceType", "id" },
                OptionalSegments = new[] { "meta", "implicitRules", "language" }
            }
        };
    }
}

public class ComplianceCheckResult
{
    public bool IsCompliant { get; set; } = true;
    public string MessageId { get; set; } = string.Empty;
    public int? RecordIndex { get; set; }
    public string TargetFormat { get; set; } = string.Empty;
    public DateTime CheckTimestamp { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public double ComplianceScore { get; set; } = 100.0;

    public void AddError(string error)
    {
        Errors.Add(error);
        IsCompliant = false;
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
        return $"ComplianceCheckResult: IsCompliant={IsCompliant}, Score={ComplianceScore:F1}, Errors={Errors.Count}, Warnings={Warnings.Count}";
    }
}

public class FormatSpecification
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string[] RequiredSegments { get; set; } = Array.Empty<string>();
    public string[] OptionalSegments { get; set; } = Array.Empty<string>();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class FieldLengthConstraint
{
    public int FieldNumber { get; set; }
    public int MaxLength { get; set; }
    public int MinLength { get; set; } = 0;
}