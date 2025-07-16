using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;

namespace HL7Processor.Core.Transformation;

public class ObjectMapper
{
    private readonly ILogger<ObjectMapper> _logger;
    private readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertyCache;
    private readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> _propertyLookupCache;

    public ObjectMapper(ILogger<ObjectMapper> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _propertyCache = new ConcurrentDictionary<Type, PropertyInfo[]>();
        _propertyLookupCache = new ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>>();
    }

    public T MapDictionaryToObject<T>(Dictionary<string, object> dictionary) where T : new()
    {
        if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));

        var type = typeof(T);
        var obj = new T();

        try
        {
            var propertyLookup = GetPropertyLookup(type);

            foreach (var kvp in dictionary)
            {
                if (propertyLookup.TryGetValue(kvp.Key, out var property))
                {
                    try
                    {
                        var convertedValue = ConvertValueToPropertyType(kvp.Value, property.PropertyType);
                        property.SetValue(obj, convertedValue);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to set property {PropertyName} with value {Value} on type {TypeName}", 
                            property.Name, kvp.Value, type.Name);
                    }
                }
                else
                {
                    _logger.LogDebug("Property {PropertyName} not found on type {TypeName}", kvp.Key, type.Name);
                }
            }

            _logger.LogDebug("Successfully mapped dictionary to object of type {TypeName}", type.Name);
            return obj;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to map dictionary to object of type {TypeName}", type.Name);
            throw;
        }
    }

    public Dictionary<string, object> MapObjectToDictionary<T>(T obj) where T : class
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));

        var type = typeof(T);
        var result = new Dictionary<string, object>();

        try
        {
            var properties = GetProperties(type);

            foreach (var property in properties)
            {
                if (property.CanRead)
                {
                    try
                    {
                        var value = property.GetValue(obj);
                        if (value != null)
                        {
                            result[property.Name] = value;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to read property {PropertyName} from object of type {TypeName}", 
                            property.Name, type.Name);
                    }
                }
            }

            _logger.LogDebug("Successfully mapped object of type {TypeName} to dictionary", type.Name);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to map object of type {TypeName} to dictionary", type.Name);
            throw;
        }
    }

    public T MapJsonToObject<T>(string json) where T : new()
    {
        if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("JSON cannot be null or empty", nameof(json));

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var obj = JsonSerializer.Deserialize<T>(json, options);
            return obj ?? new T();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize JSON to object of type {TypeName}", typeof(T).Name);
            
            // Fallback to dictionary-based mapping
            try
            {
                var dictionary = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                if (dictionary != null)
                {
                    return MapDictionaryToObject<T>(dictionary);
                }
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(fallbackEx, "Fallback dictionary mapping also failed for type {TypeName}", typeof(T).Name);
            }

            throw;
        }
    }

    public string MapObjectToJson<T>(T obj) where T : class
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));

        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            return JsonSerializer.Serialize(obj, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to serialize object of type {TypeName} to JSON", typeof(T).Name);
            throw;
        }
    }

    public List<T> MapDictionaryListToObjectList<T>(IEnumerable<Dictionary<string, object>> dictionaries) where T : new()
    {
        if (dictionaries == null) throw new ArgumentNullException(nameof(dictionaries));

        var result = new List<T>();
        var dictionaryList = dictionaries.ToList();

        _logger.LogDebug("Starting batch mapping of {Count} dictionaries to objects of type {TypeName}", 
            dictionaryList.Count, typeof(T).Name);

        foreach (var dictionary in dictionaryList)
        {
            try
            {
                var obj = MapDictionaryToObject<T>(dictionary);
                result.Add(obj);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to map dictionary to object of type {TypeName}, skipping entry", typeof(T).Name);
            }
        }

        _logger.LogDebug("Completed batch mapping: {SuccessCount}/{TotalCount} objects mapped successfully", 
            result.Count, dictionaryList.Count);

        return result;
    }

    public List<Dictionary<string, object>> MapObjectListToDictionaryList<T>(IEnumerable<T> objects) where T : class
    {
        if (objects == null) throw new ArgumentNullException(nameof(objects));

        var result = new List<Dictionary<string, object>>();
        var objectList = objects.ToList();

        _logger.LogDebug("Starting batch mapping of {Count} objects of type {TypeName} to dictionaries", 
            objectList.Count, typeof(T).Name);

        foreach (var obj in objectList)
        {
            try
            {
                var dictionary = MapObjectToDictionary(obj);
                result.Add(dictionary);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to map object of type {TypeName} to dictionary, skipping entry", typeof(T).Name);
            }
        }

        _logger.LogDebug("Completed batch mapping: {SuccessCount}/{TotalCount} dictionaries mapped successfully", 
            result.Count, objectList.Count);

        return result;
    }

    public bool TryMapDictionaryToObject<T>(Dictionary<string, object> dictionary, out T result) where T : new()
    {
        result = default!;

        try
        {
            result = MapDictionaryToObject<T>(dictionary);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to map dictionary to object of type {TypeName}", typeof(T).Name);
            return false;
        }
    }

    public void ClearCache()
    {
        _propertyCache.Clear();
        _propertyLookupCache.Clear();
        _logger.LogDebug("Object mapper cache cleared");
    }

    public void WarmUpCache<T>()
    {
        var type = typeof(T);
        GetProperties(type);
        GetPropertyLookup(type);
        _logger.LogDebug("Cache warmed up for type {TypeName}", type.Name);
    }

    public void WarmUpCache(params Type[] types)
    {
        foreach (var type in types)
        {
            GetProperties(type);
            GetPropertyLookup(type);
        }
        _logger.LogDebug("Cache warmed up for {TypeCount} types", types.Length);
    }

    private PropertyInfo[] GetProperties(Type type)
    {
        return _propertyCache.GetOrAdd(type, t => 
            t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
             .Where(p => p.CanWrite)
             .ToArray());
    }

    private Dictionary<string, PropertyInfo> GetPropertyLookup(Type type)
    {
        return _propertyLookupCache.GetOrAdd(type, t =>
        {
            var properties = GetProperties(t);
            var lookup = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (var property in properties)
            {
                // Add exact name
                lookup[property.Name] = property;

                // Add camelCase version
                var camelCase = char.ToLowerInvariant(property.Name[0]) + property.Name.Substring(1);
                if (!lookup.ContainsKey(camelCase))
                {
                    lookup[camelCase] = property;
                }

                // Add snake_case version
                var snakeCase = string.Concat(property.Name.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x : x.ToString())).ToLowerInvariant();
                if (!lookup.ContainsKey(snakeCase))
                {
                    lookup[snakeCase] = property;
                }
            }

            return lookup;
        });
    }

    private object? ConvertValueToPropertyType(object? value, Type targetType)
    {
        if (value == null)
            return null;

        var sourceType = value.GetType();
        
        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        // If types match, return as-is
        if (sourceType == underlyingType || sourceType == targetType)
            return value;

        // Handle string to other type conversions
        if (sourceType == typeof(string))
        {
            var stringValue = (string)value;
            
            if (string.IsNullOrEmpty(stringValue))
                return GetDefaultValue(targetType);

            return underlyingType.Name switch
            {
                nameof(Int32) => int.Parse(stringValue),
                nameof(Int64) => long.Parse(stringValue),
                nameof(Decimal) => decimal.Parse(stringValue),
                nameof(Double) => double.Parse(stringValue),
                nameof(Single) => float.Parse(stringValue),
                nameof(Boolean) => bool.Parse(stringValue),
                nameof(DateTime) => DateTime.Parse(stringValue),
                nameof(TimeSpan) => TimeSpan.Parse(stringValue),
                nameof(Guid) => Guid.Parse(stringValue),
                _ => Convert.ChangeType(value, underlyingType)
            };
        }

        // Handle other conversions
        try
        {
            return Convert.ChangeType(value, underlyingType);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to convert value {Value} from type {SourceType} to {TargetType}", 
                value, sourceType.Name, targetType.Name);
            return GetDefaultValue(targetType);
        }
    }

    private object? GetDefaultValue(Type type)
    {
        if (type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }
        return null;
    }
}