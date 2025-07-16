using System.Text.Json;
using System.Text.Json.Serialization;
using HL7Processor.Core.Models;
using HL7Processor.Core.Messages;

namespace HL7Processor.Core.Serialization;

public class HL7MessageSerializer
{
    private readonly JsonSerializerOptions _jsonOptions;

    public HL7MessageSerializer()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new JsonStringEnumConverter(),
                new HL7MessageTypeConverter()
            }
        };
    }

    public string SerializeToJson(HL7Message message)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        return JsonSerializer.Serialize(message, _jsonOptions);
    }

    public HL7Message? DeserializeFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("JSON string cannot be null or empty", nameof(json));

        return JsonSerializer.Deserialize<HL7Message>(json, _jsonOptions);
    }

    public T? DeserializeFromJson<T>(string json) where T : HL7Message
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("JSON string cannot be null or empty", nameof(json));

        return JsonSerializer.Deserialize<T>(json, _jsonOptions);
    }

    public byte[] SerializeToBytes(HL7Message message)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        return JsonSerializer.SerializeToUtf8Bytes(message, _jsonOptions);
    }

    public HL7Message? DeserializeFromBytes(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
            throw new ArgumentException("Bytes cannot be null or empty", nameof(bytes));

        return JsonSerializer.Deserialize<HL7Message>(bytes, _jsonOptions);
    }

    public async Task<string> SerializeToJsonAsync(HL7Message message)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        using var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, message, _jsonOptions);
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    public async Task<HL7Message?> DeserializeFromJsonAsync(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("JSON string cannot be null or empty", nameof(json));

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        return await JsonSerializer.DeserializeAsync<HL7Message>(stream, _jsonOptions);
    }

    public async Task SerializeToStreamAsync(HL7Message message, Stream stream)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        await JsonSerializer.SerializeAsync(stream, message, _jsonOptions);
    }

    public async Task<HL7Message?> DeserializeFromStreamAsync(Stream stream)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        return await JsonSerializer.DeserializeAsync<HL7Message>(stream, _jsonOptions);
    }

    public string SerializeToCompactJson(HL7Message message)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var compactOptions = new JsonSerializerOptions(_jsonOptions)
        {
            WriteIndented = false
        };

        return JsonSerializer.Serialize(message, compactOptions);
    }

    public Dictionary<string, object> SerializeToDictionary(HL7Message message)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var json = SerializeToJson(message);
        return JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
    }

    public HL7Message? DeserializeFromDictionary(Dictionary<string, object> dictionary)
    {
        if (dictionary == null)
            throw new ArgumentNullException(nameof(dictionary));

        var json = JsonSerializer.Serialize(dictionary);
        return DeserializeFromJson(json);
    }

    public ADTMessage? DeserializeToADTMessage(string json)
    {
        var baseMessage = DeserializeFromJson(json);
        if (baseMessage == null)
            return null;

        return new ADTMessage(baseMessage);
    }

    public ORMMessage? DeserializeToORMMessage(string json)
    {
        var baseMessage = DeserializeFromJson(json);
        if (baseMessage == null)
            return null;

        return new ORMMessage(baseMessage);
    }

    public T? DeserializeToSpecificMessage<T>(string json) where T : HL7Message
    {
        var baseMessage = DeserializeFromJson(json);
        if (baseMessage == null)
            return null;

        if (typeof(T) == typeof(ADTMessage))
            return new ADTMessage(baseMessage) as T;

        if (typeof(T) == typeof(ORMMessage))
            return new ORMMessage(baseMessage) as T;

        return baseMessage as T;
    }
}

public class HL7MessageTypeConverter : JsonConverter<HL7MessageType>
{
    public override HL7MessageType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (Enum.TryParse<HL7MessageType>(value, true, out var result))
                return result;
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            var value = reader.GetInt32();
            if (Enum.IsDefined(typeof(HL7MessageType), value))
                return (HL7MessageType)value;
        }

        return HL7MessageType.Unknown;
    }

    public override void Write(Utf8JsonWriter writer, HL7MessageType value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}