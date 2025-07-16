using HL7Processor.Core.Models;
using HL7Processor.Core.Utilities;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text;

namespace HL7Processor.Core.Transformation.Converters;

public class JsonConverter
{
    private readonly ILogger<JsonConverter> _logger;
    private readonly ObjectMapper _objectMapper;

    public JsonConverter(ILogger<JsonConverter> logger, ObjectMapper objectMapper)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _objectMapper = objectMapper ?? throw new ArgumentNullException(nameof(objectMapper));
    }

    public string ConvertHL7ToJson(HL7Message message, bool prettyPrint = true, bool includeMetadata = true)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        try
        {
            var jsonObject = ConvertHL7ToJsonObject(message, includeMetadata);
            
            var options = new JsonSerializerOptions
            {
                WriteIndented = prettyPrint,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            var json = JsonSerializer.Serialize(jsonObject, options);
            
            _logger.LogDebug("Successfully converted HL7 message {MessageId} to JSON ({Length} characters)", 
                message.Id, json.Length);
            
            return json;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert HL7 message {MessageId} to JSON", message.Id);
            throw;
        }
    }

    public string ConvertHL7ToFlatJson(HL7Message message, bool prettyPrint = true)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        try
        {
            var flatObject = ConvertHL7ToFlatJsonObject(message);
            
            var options = new JsonSerializerOptions
            {
                WriteIndented = prettyPrint,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(flatObject, options);
            
            _logger.LogDebug("Successfully converted HL7 message {MessageId} to flat JSON ({Length} characters)", 
                message.Id, json.Length);
            
            return json;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert HL7 message {MessageId} to flat JSON", message.Id);
            throw;
        }
    }

    public HL7Message ConvertJsonToHL7(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("JSON cannot be null or empty", nameof(json));

        try
        {
            var jsonDocument = JsonDocument.Parse(json);
            var root = jsonDocument.RootElement;

            var message = new HL7Message();

            // Extract message-level properties
            if (root.TryGetProperty("messageId", out var messageIdProp))
            {
                message.Id = messageIdProp.GetString() ?? string.Empty;
            }

            if (root.TryGetProperty("messageType", out var messageTypeProp))
            {
                if (Enum.TryParse<HL7MessageType>(messageTypeProp.GetString(), out var messageType))
                {
                    message.MessageType = messageType;
                }
            }

            if (root.TryGetProperty("timestamp", out var timestampProp))
            {
                if (DateTime.TryParse(timestampProp.GetString(), out var timestamp))
                {
                    message.Timestamp = timestamp;
                }
            }

            if (root.TryGetProperty("version", out var versionProp))
            {
                message.Version = versionProp.GetString() ?? "2.5";
            }

            // Extract segments
            if (root.TryGetProperty("segments", out var segmentsProp) && segmentsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var segmentElement in segmentsProp.EnumerateArray())
                {
                    var segment = ConvertJsonToSegment(segmentElement);
                    if (segment != null)
                    {
                        message.AddSegment(segment);
                    }
                }
            }

            // Rebuild raw message
            message.RawMessage = string.Join("\r", message.Segments.Select(s => s.RawData));

            _logger.LogDebug("Successfully converted JSON to HL7 message {MessageId}", message.Id);
            return message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert JSON to HL7 message");
            throw;
        }
    }

    public HL7Message ConvertFlatJsonToHL7(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("JSON cannot be null or empty", nameof(json));

        try
        {
            var flatData = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            if (flatData == null) throw new JsonException("Failed to deserialize flat JSON");

            var message = new HL7Message();
            var segmentBuilders = new Dictionary<string, SegmentBuilder>();
            var delimiters = new HL7Delimiters();

            // Extract message-level properties
            ExtractMessagePropertiesFromFlat(flatData, message);

            // Group fields by segment and build segments
            foreach (var kvp in flatData)
            {
                var key = kvp.Key.ToLowerInvariant();
                var value = kvp.Value?.ToString() ?? string.Empty;

                // Parse keys like "pid_3", "pid_3_1", "msh_9_1"
                var parts = key.Split('_');
                if (parts.Length >= 2)
                {
                    var segmentType = parts[0].ToUpperInvariant();
                    
                    if (int.TryParse(parts[1], out var fieldNumber))
                    {
                        if (!segmentBuilders.TryGetValue(segmentType, out var builder))
                        {
                            builder = new SegmentBuilder(segmentType, delimiters);
                            segmentBuilders[segmentType] = builder;
                        }

                        // Handle component and sub-component reconstruction
                        if (parts.Length == 3 && int.TryParse(parts[2], out var componentNumber))
                        {
                            builder.SetComponentValue(fieldNumber, componentNumber, value);
                        }
                        else if (parts.Length == 4 && int.TryParse(parts[2], out var compNum) && int.TryParse(parts[3], out var subCompNum))
                        {
                            builder.SetSubComponentValue(fieldNumber, compNum, subCompNum, value);
                        }
                        else
                        {
                            builder.SetFieldValue(fieldNumber, value);
                        }
                    }
                }
            }

            // Build segments in proper order (MSH first, then others)
            var orderedSegmentTypes = new[] { "MSH", "EVN", "PID", "PV1", "OBX", "AL1", "DG1" };
            
            foreach (var segmentType in orderedSegmentTypes)
            {
                if (segmentBuilders.TryGetValue(segmentType, out var builder))
                {
                    var segment = builder.Build();
                    message.AddSegment(segment);
                }
            }

            // Add any remaining segments
            foreach (var kvp in segmentBuilders.Where(sb => !orderedSegmentTypes.Contains(sb.Key)))
            {
                var segment = kvp.Value.Build();
                message.AddSegment(segment);
            }

            message.RawMessage = string.Join("\r", message.Segments.Select(s => s.RawData));

            _logger.LogDebug("Successfully converted flat JSON to HL7 message {MessageId}", message.Id);
            return message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert flat JSON to HL7 message");
            throw;
        }
    }

    public T ConvertJsonToObject<T>(string json) where T : new()
    {
        return _objectMapper.MapJsonToObject<T>(json);
    }

    public string ConvertObjectToJson<T>(T obj) where T : class
    {
        return _objectMapper.MapObjectToJson(obj);
    }

    private Dictionary<string, object> ConvertHL7ToJsonObject(HL7Message message, bool includeMetadata)
    {
        var result = new Dictionary<string, object>
        {
            ["messageId"] = message.Id,
            ["messageType"] = message.MessageType.ToString(),
            ["timestamp"] = message.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            ["version"] = message.Version
        };

        if (includeMetadata)
        {
            result["sendingApplication"] = message.SendingApplication ?? string.Empty;
            result["sendingFacility"] = message.SendingFacility ?? string.Empty;
            result["receivingApplication"] = message.ReceivingApplication ?? string.Empty;
            result["receivingFacility"] = message.ReceivingFacility ?? string.Empty;
            result["messageControlId"] = message.MessageControlId ?? string.Empty;
            result["processingId"] = message.ProcessingId ?? string.Empty;
        }

        // Convert segments
        var segments = new List<Dictionary<string, object>>();
        
        foreach (var segment in message.Segments)
        {
            var segmentObj = ConvertSegmentToJsonObject(segment);
            segments.Add(segmentObj);
        }

        result["segments"] = segments;
        
        return result;
    }

    private Dictionary<string, object> ConvertHL7ToFlatJsonObject(HL7Message message)
    {
        var result = new Dictionary<string, object>
        {
            ["message_id"] = message.Id,
            ["message_type"] = message.MessageType.ToString(),
            ["timestamp"] = message.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            ["version"] = message.Version,
            ["sending_application"] = message.SendingApplication ?? string.Empty,
            ["sending_facility"] = message.SendingFacility ?? string.Empty,
            ["receiving_application"] = message.ReceivingApplication ?? string.Empty,
            ["receiving_facility"] = message.ReceivingFacility ?? string.Empty,
            ["message_control_id"] = message.MessageControlId ?? string.Empty,
            ["processing_id"] = message.ProcessingId ?? string.Empty
        };

        var delimiters = HL7ParsingUtils.ExtractDelimitersFromMessage(message);

        foreach (var segment in message.Segments)
        {
            var segmentType = segment.Type.ToLowerInvariant();
            
            for (int fieldIndex = 1; fieldIndex < segment.Fields.Count; fieldIndex++)
            {
                var fieldValue = segment.GetFieldValue(fieldIndex);
                if (!string.IsNullOrEmpty(fieldValue))
                {
                    var key = $"{segmentType}_{fieldIndex}";
                    
                    // Check if field has components
                    var components = HL7ParsingUtils.SplitComponent(fieldValue, delimiters);
                    if (components.Length > 1)
                    {
                        for (int compIndex = 0; compIndex < components.Length; compIndex++)
                        {
                            var component = components[compIndex];
                            if (!string.IsNullOrEmpty(component))
                            {
                                var compKey = $"{segmentType}_{fieldIndex}_{compIndex + 1}";
                                
                                // Check if component has sub-components
                                var subComponents = HL7ParsingUtils.SplitSubComponent(component, delimiters);
                                if (subComponents.Length > 1)
                                {
                                    for (int subIndex = 0; subIndex < subComponents.Length; subIndex++)
                                    {
                                        var subComponent = subComponents[subIndex];
                                        if (!string.IsNullOrEmpty(subComponent))
                                        {
                                            var subKey = $"{segmentType}_{fieldIndex}_{compIndex + 1}_{subIndex + 1}";
                                            result[subKey] = subComponent;
                                        }
                                    }
                                }
                                else
                                {
                                    result[compKey] = component;
                                }
                            }
                        }
                    }
                    else
                    {
                        result[key] = fieldValue;
                    }
                }
            }
        }

        return result;
    }

    private Dictionary<string, object> ConvertSegmentToJsonObject(HL7Segment segment)
    {
        var result = new Dictionary<string, object>
        {
            ["segmentType"] = segment.Type,
            ["fields"] = new List<object>()
        };

        var fields = (List<object>)result["fields"];
        
        for (int i = 0; i < segment.Fields.Count; i++)
        {
            var fieldValue = segment.Fields[i];
            fields.Add(fieldValue ?? string.Empty);
        }

        return result;
    }

    private HL7Segment? ConvertJsonToSegment(JsonElement segmentElement)
    {
        if (!segmentElement.TryGetProperty("segmentType", out var segmentTypeProp))
            return null;

        var segmentType = segmentTypeProp.GetString();
        if (string.IsNullOrEmpty(segmentType))
            return null;

        var segment = new HL7Segment(segmentType, string.Empty);

        if (segmentElement.TryGetProperty("fields", out var fieldsProp) && fieldsProp.ValueKind == JsonValueKind.Array)
        {
            var fieldIndex = 0;
            foreach (var fieldElement in fieldsProp.EnumerateArray())
            {
                var fieldValue = fieldElement.ValueKind == JsonValueKind.String 
                    ? fieldElement.GetString() ?? string.Empty
                    : fieldElement.ToString();
                
                segment.SetFieldValue(fieldIndex, fieldValue);
                fieldIndex++;
            }
        }

        segment.RebuildRawData();
        return segment;
    }

    private void ExtractMessagePropertiesFromFlat(Dictionary<string, object> flatData, HL7Message message)
    {
        if (flatData.TryGetValue("message_id", out var messageId))
        {
            message.Id = messageId.ToString() ?? string.Empty;
        }

        if (flatData.TryGetValue("message_type", out var messageType))
        {
            if (Enum.TryParse<HL7MessageType>(messageType.ToString(), out var msgType))
            {
                message.MessageType = msgType;
            }
        }

        if (flatData.TryGetValue("timestamp", out var timestamp))
        {
            if (DateTime.TryParse(timestamp.ToString(), out var ts))
            {
                message.Timestamp = ts;
            }
        }

        if (flatData.TryGetValue("version", out var version))
        {
            message.Version = version.ToString() ?? "2.5";
        }

        if (flatData.TryGetValue("sending_application", out var sendApp))
        {
            message.SendingApplication = sendApp.ToString();
        }

        if (flatData.TryGetValue("sending_facility", out var sendFac))
        {
            message.SendingFacility = sendFac.ToString();
        }

        if (flatData.TryGetValue("receiving_application", out var recApp))
        {
            message.ReceivingApplication = recApp.ToString();
        }

        if (flatData.TryGetValue("receiving_facility", out var recFac))
        {
            message.ReceivingFacility = recFac.ToString();
        }

        if (flatData.TryGetValue("message_control_id", out var controlId))
        {
            message.MessageControlId = controlId.ToString();
        }

        if (flatData.TryGetValue("processing_id", out var procId))
        {
            message.ProcessingId = procId.ToString();
        }
    }
}

public class SegmentBuilder
{
    private readonly string _segmentType;
    private readonly HL7Delimiters _delimiters;
    private readonly Dictionary<int, FieldBuilder> _fields;

    public SegmentBuilder(string segmentType, HL7Delimiters delimiters)
    {
        _segmentType = segmentType;
        _delimiters = delimiters;
        _fields = new Dictionary<int, FieldBuilder>();
    }

    public void SetFieldValue(int fieldNumber, string value)
    {
        if (!_fields.TryGetValue(fieldNumber, out var field))
        {
            field = new FieldBuilder(_delimiters);
            _fields[fieldNumber] = field;
        }
        field.SetValue(value);
    }

    public void SetComponentValue(int fieldNumber, int componentNumber, string value)
    {
        if (!_fields.TryGetValue(fieldNumber, out var field))
        {
            field = new FieldBuilder(_delimiters);
            _fields[fieldNumber] = field;
        }
        field.SetComponentValue(componentNumber, value);
    }

    public void SetSubComponentValue(int fieldNumber, int componentNumber, int subComponentNumber, string value)
    {
        if (!_fields.TryGetValue(fieldNumber, out var field))
        {
            field = new FieldBuilder(_delimiters);
            _fields[fieldNumber] = field;
        }
        field.SetSubComponentValue(componentNumber, subComponentNumber, value);
    }

    public HL7Segment Build()
    {
        var segment = new HL7Segment(_segmentType, string.Empty);
        
        var maxFieldNumber = _fields.Keys.DefaultIfEmpty(0).Max();
        for (int i = 1; i <= maxFieldNumber; i++)
        {
            if (_fields.TryGetValue(i, out var field))
            {
                segment.SetFieldValue(i, field.Build());
            }
            else
            {
                segment.SetFieldValue(i, string.Empty);
            }
        }

        segment.RebuildRawData();
        return segment;
    }
}

public class FieldBuilder
{
    private readonly HL7Delimiters _delimiters;
    private readonly Dictionary<int, ComponentBuilder> _components;
    private string? _simpleValue;

    public FieldBuilder(HL7Delimiters delimiters)
    {
        _delimiters = delimiters;
        _components = new Dictionary<int, ComponentBuilder>();
    }

    public void SetValue(string value)
    {
        _simpleValue = value;
        _components.Clear();
    }

    public void SetComponentValue(int componentNumber, string value)
    {
        _simpleValue = null;
        if (!_components.TryGetValue(componentNumber, out var component))
        {
            component = new ComponentBuilder(_delimiters);
            _components[componentNumber] = component;
        }
        component.SetValue(value);
    }

    public void SetSubComponentValue(int componentNumber, int subComponentNumber, string value)
    {
        _simpleValue = null;
        if (!_components.TryGetValue(componentNumber, out var component))
        {
            component = new ComponentBuilder(_delimiters);
            _components[componentNumber] = component;
        }
        component.SetSubComponentValue(subComponentNumber, value);
    }

    public string Build()
    {
        if (_simpleValue != null)
        {
            return _simpleValue;
        }

        if (_components.Count == 0)
        {
            return string.Empty;
        }

        var maxComponentNumber = _components.Keys.Max();
        var componentValues = new string[maxComponentNumber];
        
        for (int i = 1; i <= maxComponentNumber; i++)
        {
            if (_components.TryGetValue(i, out var component))
            {
                componentValues[i - 1] = component.Build();
            }
            else
            {
                componentValues[i - 1] = string.Empty;
            }
        }

        return HL7ParsingUtils.JoinComponents(componentValues, _delimiters);
    }
}

public class ComponentBuilder
{
    private readonly HL7Delimiters _delimiters;
    private readonly Dictionary<int, string> _subComponents;
    private string? _simpleValue;

    public ComponentBuilder(HL7Delimiters delimiters)
    {
        _delimiters = delimiters;
        _subComponents = new Dictionary<int, string>();
    }

    public void SetValue(string value)
    {
        _simpleValue = value;
        _subComponents.Clear();
    }

    public void SetSubComponentValue(int subComponentNumber, string value)
    {
        _simpleValue = null;
        _subComponents[subComponentNumber] = value;
    }

    public string Build()
    {
        if (_simpleValue != null)
        {
            return _simpleValue;
        }

        if (_subComponents.Count == 0)
        {
            return string.Empty;
        }

        var maxSubComponentNumber = _subComponents.Keys.Max();
        var subComponentValues = new string[maxSubComponentNumber];
        
        for (int i = 1; i <= maxSubComponentNumber; i++)
        {
            subComponentValues[i - 1] = _subComponents.TryGetValue(i, out var value) ? value : string.Empty;
        }

        return HL7ParsingUtils.JoinSubComponents(subComponentValues, _delimiters);
    }
}