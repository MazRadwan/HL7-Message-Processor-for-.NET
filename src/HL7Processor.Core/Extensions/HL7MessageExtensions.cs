using HL7Processor.Core.Models;
using System.Text.Json;

namespace HL7Processor.Core.Extensions;

public static class HL7MessageExtensions
{
    public static string GetPatientId(this HL7Message message)
    {
        var pidSegment = message.GetSegment("PID");
        return pidSegment?.GetFieldValue(3) ?? string.Empty;
    }

    public static string GetPatientName(this HL7Message message)
    {
        var pidSegment = message.GetSegment("PID");
        var nameField = pidSegment?.GetFieldValue(5);
        
        if (string.IsNullOrEmpty(nameField))
            return string.Empty;

        // Parse name components (Last^First^Middle^Suffix^Prefix)
        var components = nameField.Split('^');
        if (components.Length >= 2)
        {
            return $"{components[1]} {components[0]}".Trim(); // First Last
        }

        return nameField;
    }

    public static DateTime? GetPatientBirthDate(this HL7Message message)
    {
        var pidSegment = message.GetSegment("PID");
        var birthDateField = pidSegment?.GetFieldValue(7);
        
        if (string.IsNullOrEmpty(birthDateField))
            return null;

        return TryParseHL7Date(birthDateField);
    }

    public static string GetPatientGender(this HL7Message message)
    {
        var pidSegment = message.GetSegment("PID");
        return pidSegment?.GetFieldValue(8) ?? string.Empty;
    }

    public static string GetEventType(this HL7Message message)
    {
        var evnSegment = message.GetSegment("EVN");
        return evnSegment?.GetFieldValue(1) ?? string.Empty;
    }

    public static DateTime? GetEventDateTime(this HL7Message message)
    {
        var evnSegment = message.GetSegment("EVN");
        var eventTimeField = evnSegment?.GetFieldValue(2);
        
        if (string.IsNullOrEmpty(eventTimeField))
            return null;

        return TryParseHL7DateTime(eventTimeField);
    }

    public static string GetVisitNumber(this HL7Message message)
    {
        var pv1Segment = message.GetSegment("PV1");
        return pv1Segment?.GetFieldValue(19) ?? string.Empty;
    }

    public static string GetPatientClass(this HL7Message message)
    {
        var pv1Segment = message.GetSegment("PV1");
        return pv1Segment?.GetFieldValue(2) ?? string.Empty;
    }

    public static string GetAssignedPatientLocation(this HL7Message message)
    {
        var pv1Segment = message.GetSegment("PV1");
        return pv1Segment?.GetFieldValue(3) ?? string.Empty;
    }

    public static string GetAttendingDoctor(this HL7Message message)
    {
        var pv1Segment = message.GetSegment("PV1");
        var doctorField = pv1Segment?.GetFieldValue(7);
        
        if (string.IsNullOrEmpty(doctorField))
            return string.Empty;

        // Parse doctor name components
        var components = doctorField.Split('^');
        if (components.Length >= 2)
        {
            return $"{components[1]} {components[0]}".Trim(); // First Last
        }

        return doctorField;
    }

    public static List<string> GetAllergies(this HL7Message message)
    {
        var al1Segments = message.GetSegments("AL1");
        var allergies = new List<string>();

        foreach (var segment in al1Segments)
        {
            var allergen = segment.GetFieldValue(3);
            if (!string.IsNullOrEmpty(allergen))
            {
                allergies.Add(allergen);
            }
        }

        return allergies;
    }

    public static Dictionary<string, string> GetCustomFields(this HL7Message message, string segmentType)
    {
        var segment = message.GetSegment(segmentType);
        if (segment == null)
            return new Dictionary<string, string>();

        var fields = new Dictionary<string, string>();
        
        foreach (var field in segment.Fields)
        {
            if (!string.IsNullOrEmpty(field.Value))
            {
                fields[$"{segmentType}-{field.Position}"] = field.Value;
            }
        }

        return fields;
    }

    public static bool HasSegment(this HL7Message message, string segmentType)
    {
        return message.GetSegment(segmentType) != null;
    }

    public static int GetSegmentCount(this HL7Message message, string segmentType)
    {
        return message.GetSegments(segmentType).Count;
    }

    public static string ToJsonString(this HL7Message message, bool indented = false)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = indented,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Serialize(message, options);
    }

    public static HL7Message FromJsonString(string json)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Deserialize<HL7Message>(json, options) ?? new HL7Message();
    }

    private static DateTime? TryParseHL7Date(string hl7Date)
    {
        if (string.IsNullOrEmpty(hl7Date) || hl7Date.Length < 8)
            return null;

        try
        {
            var year = int.Parse(hl7Date.Substring(0, 4));
            var month = int.Parse(hl7Date.Substring(4, 2));
            var day = int.Parse(hl7Date.Substring(6, 2));

            return new DateTime(year, month, day);
        }
        catch
        {
            return null;
        }
    }

    private static DateTime? TryParseHL7DateTime(string hl7DateTime)
    {
        if (string.IsNullOrEmpty(hl7DateTime))
            return null;

        // Remove timezone information for basic parsing
        var cleanDateTime = hl7DateTime.Split('+')[0].Split('-')[0];
        
        if (cleanDateTime.Length < 8)
            return null;

        try
        {
            var year = int.Parse(cleanDateTime.Substring(0, 4));
            var month = int.Parse(cleanDateTime.Substring(4, 2));
            var day = int.Parse(cleanDateTime.Substring(6, 2));

            var hour = 0;
            var minute = 0;
            var second = 0;

            if (cleanDateTime.Length >= 10)
                hour = int.Parse(cleanDateTime.Substring(8, 2));

            if (cleanDateTime.Length >= 12)
                minute = int.Parse(cleanDateTime.Substring(10, 2));

            if (cleanDateTime.Length >= 14)
                second = int.Parse(cleanDateTime.Substring(12, 2));

            return new DateTime(year, month, day, hour, minute, second);
        }
        catch
        {
            return null;
        }
    }
}