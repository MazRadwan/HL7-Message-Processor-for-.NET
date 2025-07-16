using HL7Processor.Core.Models;
using System.Globalization;
using System.Text.RegularExpressions;

namespace HL7Processor.Core.Utilities;

public static class HL7ParsingUtils
{
    private static readonly Regex TimestampPattern = new(@"^(\d{4})(\d{2})?(\d{2})?(\d{2})?(\d{2})?(\d{2})?(\.\d+)?([+-]\d{4})?$");
    
    public static class DefaultDelimiters
    {
        public const char Field = '|';
        public const char Component = '^';
        public const char Repetition = '~';
        public const char Escape = '\\';
        public const char SubComponent = '&';
        public const string Encoding = "^~\\&";
    }

    public static HL7Delimiters ExtractDelimitersFromMSH(string mshSegment)
    {
        if (string.IsNullOrEmpty(mshSegment) || !mshSegment.StartsWith("MSH"))
        {
            return new HL7Delimiters();
        }

        try
        {
            // MSH|^~\&|... - field separator is at position 3, encoding chars at positions 4-7
            if (mshSegment.Length < 8)
            {
                return new HL7Delimiters();
            }

            var fieldSeparator = mshSegment[3];
            var encodingChars = mshSegment.Substring(4, 4);

            return new HL7Delimiters
            {
                Field = fieldSeparator,
                Component = encodingChars[0],
                Repetition = encodingChars[1],
                Escape = encodingChars[2],
                SubComponent = encodingChars[3]
            };
        }
        catch
        {
            return new HL7Delimiters();
        }
    }

    public static HL7Delimiters ExtractDelimitersFromMessage(HL7Message message)
    {
        var mshSegment = message.GetSegment("MSH");
        if (mshSegment != null)
        {
            return ExtractDelimitersFromMSH(mshSegment.RawData);
        }
        return new HL7Delimiters();
    }

    public static DateTime? ParseHL7DateTime(string? dateTimeString, bool allowPartial = true)
    {
        if (string.IsNullOrWhiteSpace(dateTimeString))
            return null;

        var cleaned = dateTimeString.Trim();
        var match = TimestampPattern.Match(cleaned);
        
        if (!match.Success)
            return null;

        try
        {
            var year = int.Parse(match.Groups[1].Value);
            var month = string.IsNullOrEmpty(match.Groups[2].Value) ? 1 : int.Parse(match.Groups[2].Value);
            var day = string.IsNullOrEmpty(match.Groups[3].Value) ? 1 : int.Parse(match.Groups[3].Value);
            var hour = string.IsNullOrEmpty(match.Groups[4].Value) ? 0 : int.Parse(match.Groups[4].Value);
            var minute = string.IsNullOrEmpty(match.Groups[5].Value) ? 0 : int.Parse(match.Groups[5].Value);
            var second = string.IsNullOrEmpty(match.Groups[6].Value) ? 0 : int.Parse(match.Groups[6].Value);

            var millisecond = 0;
            if (!string.IsNullOrEmpty(match.Groups[7].Value))
            {
                var fractionStr = match.Groups[7].Value.Substring(1); // Remove the dot
                if (fractionStr.Length > 3) fractionStr = fractionStr.Substring(0, 3); // Take first 3 digits
                fractionStr = fractionStr.PadRight(3, '0'); // Pad to 3 digits
                millisecond = int.Parse(fractionStr);
            }

            var dateTime = new DateTime(year, month, day, hour, minute, second, millisecond);

            // Handle timezone offset if present
            if (!string.IsNullOrEmpty(match.Groups[8].Value))
            {
                var offsetStr = match.Groups[8].Value;
                if (TimeSpan.TryParseExact(offsetStr, @"\+hhmm|\-hhmm", CultureInfo.InvariantCulture, out var offset))
                {
                    dateTime = dateTime.Subtract(offset); // Convert to UTC
                }
            }

            return dateTime;
        }
        catch
        {
            return null;
        }
    }

    public static string FormatHL7DateTime(DateTime dateTime, bool includeMilliseconds = false, bool includeTimezone = false)
    {
        var format = "yyyyMMddHHmmss";
        var result = dateTime.ToString(format);

        if (includeMilliseconds && dateTime.Millisecond > 0)
        {
            result += $".{dateTime.Millisecond:D3}";
        }

        if (includeTimezone)
        {
            var offset = TimeZoneInfo.Local.GetUtcOffset(dateTime);
            var sign = offset.TotalMinutes >= 0 ? "+" : "-";
            result += $"{sign}{Math.Abs(offset.Hours):D2}{Math.Abs(offset.Minutes):D2}";
        }

        return result;
    }

    public static string FormatHL7Date(DateTime date)
    {
        return date.ToString("yyyyMMdd");
    }

    public static string FormatHL7Time(DateTime time)
    {
        return time.ToString("HHmmss");
    }

    public static DateTime? ParseHL7Date(string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
            return null;

        var formats = new[] { "yyyyMMdd", "yyyy-MM-dd", "MM/dd/yyyy", "dd/MM/yyyy" };
        
        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(dateString.Trim(), format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
            {
                return result;
            }
        }

        return null;
    }

    public static TimeSpan? ParseHL7Time(string? timeString)
    {
        if (string.IsNullOrWhiteSpace(timeString))
            return null;

        var formats = new[] { "HHmmss", "HHmm", "HH:mm:ss", "HH:mm" };
        
        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(timeString.Trim(), format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
            {
                return result.TimeOfDay;
            }
        }

        return null;
    }

    public static string[] SplitField(string fieldValue, HL7Delimiters delimiters)
    {
        if (string.IsNullOrEmpty(fieldValue))
            return Array.Empty<string>();

        return fieldValue.Split(delimiters.Repetition);
    }

    public static string[] SplitComponent(string componentValue, HL7Delimiters delimiters)
    {
        if (string.IsNullOrEmpty(componentValue))
            return Array.Empty<string>();

        return componentValue.Split(delimiters.Component);
    }

    public static string[] SplitSubComponent(string subComponentValue, HL7Delimiters delimiters)
    {
        if (string.IsNullOrEmpty(subComponentValue))
            return Array.Empty<string>();

        return subComponentValue.Split(delimiters.SubComponent);
    }

    public static string JoinComponents(string[] components, HL7Delimiters delimiters)
    {
        return string.Join(delimiters.Component.ToString(), components);
    }

    public static string JoinRepetitions(string[] repetitions, HL7Delimiters delimiters)
    {
        return string.Join(delimiters.Repetition.ToString(), repetitions);
    }

    public static string JoinSubComponents(string[] subComponents, HL7Delimiters delimiters)
    {
        return string.Join(delimiters.SubComponent.ToString(), subComponents);
    }

    public static string EscapeHL7String(string input, HL7Delimiters delimiters)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = input
            .Replace(delimiters.Escape.ToString(), $"{delimiters.Escape}E{delimiters.Escape}")
            .Replace(delimiters.Field.ToString(), $"{delimiters.Escape}F{delimiters.Escape}")
            .Replace(delimiters.Component.ToString(), $"{delimiters.Escape}S{delimiters.Escape}")
            .Replace(delimiters.Repetition.ToString(), $"{delimiters.Escape}T{delimiters.Escape}")
            .Replace(delimiters.SubComponent.ToString(), $"{delimiters.Escape}R{delimiters.Escape}");

        return result;
    }

    public static string UnescapeHL7String(string input, HL7Delimiters delimiters)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = input
            .Replace($"{delimiters.Escape}F{delimiters.Escape}", delimiters.Field.ToString())
            .Replace($"{delimiters.Escape}S{delimiters.Escape}", delimiters.Component.ToString())
            .Replace($"{delimiters.Escape}T{delimiters.Escape}", delimiters.Repetition.ToString())
            .Replace($"{delimiters.Escape}R{delimiters.Escape}", delimiters.SubComponent.ToString())
            .Replace($"{delimiters.Escape}E{delimiters.Escape}", delimiters.Escape.ToString());

        return result;
    }

    public static bool IsValidHL7DateTime(string? dateTimeString)
    {
        return ParseHL7DateTime(dateTimeString) != null;
    }

    public static bool IsValidHL7Date(string? dateString)
    {
        return ParseHL7Date(dateString) != null;
    }

    public static bool IsValidHL7Time(string? timeString)
    {
        return ParseHL7Time(timeString) != null;
    }

    public static string ExtractFieldFromSegment(string segmentData, int fieldNumber, HL7Delimiters delimiters)
    {
        if (string.IsNullOrEmpty(segmentData) || fieldNumber < 1)
            return string.Empty;

        var fields = segmentData.Split(delimiters.Field);
        
        // Adjust for 0-based indexing and MSH special case
        var adjustedIndex = segmentData.StartsWith("MSH") && fieldNumber > 1 ? fieldNumber : fieldNumber;
        
        if (adjustedIndex < fields.Length)
        {
            return fields[adjustedIndex] ?? string.Empty;
        }

        return string.Empty;
    }

    public static string GetComponentFromField(string fieldValue, int componentNumber, HL7Delimiters delimiters)
    {
        if (string.IsNullOrEmpty(fieldValue) || componentNumber < 1)
            return string.Empty;

        var components = SplitComponent(fieldValue, delimiters);
        
        if (componentNumber <= components.Length)
        {
            return components[componentNumber - 1] ?? string.Empty;
        }

        return string.Empty;
    }

    public static string GetSubComponentFromComponent(string componentValue, int subComponentNumber, HL7Delimiters delimiters)
    {
        if (string.IsNullOrEmpty(componentValue) || subComponentNumber < 1)
            return string.Empty;

        var subComponents = SplitSubComponent(componentValue, delimiters);
        
        if (subComponentNumber <= subComponents.Length)
        {
            return subComponents[subComponentNumber - 1] ?? string.Empty;
        }

        return string.Empty;
    }
}

public class HL7Delimiters
{
    public char Field { get; set; } = HL7ParsingUtils.DefaultDelimiters.Field;
    public char Component { get; set; } = HL7ParsingUtils.DefaultDelimiters.Component;
    public char Repetition { get; set; } = HL7ParsingUtils.DefaultDelimiters.Repetition;
    public char Escape { get; set; } = HL7ParsingUtils.DefaultDelimiters.Escape;
    public char SubComponent { get; set; } = HL7ParsingUtils.DefaultDelimiters.SubComponent;

    public string EncodingString => $"{Component}{Repetition}{Escape}{SubComponent}";

    public override string ToString()
    {
        return $"Field={Field}, Component={Component}, Repetition={Repetition}, Escape={Escape}, SubComponent={SubComponent}";
    }
}