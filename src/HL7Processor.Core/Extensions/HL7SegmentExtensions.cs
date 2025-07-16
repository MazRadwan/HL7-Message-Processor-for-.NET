using HL7Processor.Core.Models;

namespace HL7Processor.Core.Extensions;

public static class HL7SegmentExtensions
{
    public static string GetComponentValue(this HL7Segment segment, int fieldPosition, int componentPosition)
    {
        var field = segment.Fields.FirstOrDefault(f => f.Position == fieldPosition);
        return field?.GetComponent(componentPosition) ?? string.Empty;
    }

    public static void SetComponentValue(this HL7Segment segment, int fieldPosition, int componentPosition, string value)
    {
        var field = segment.Fields.FirstOrDefault(f => f.Position == fieldPosition);
        if (field != null)
        {
            field.SetComponent(componentPosition, value);
        }
        else
        {
            var newField = new HL7Field { Position = fieldPosition };
            newField.SetComponent(componentPosition, value);
            segment.Fields.Add(newField);
        }
    }

    public static List<string> GetAllFieldValues(this HL7Segment segment)
    {
        return segment.Fields.OrderBy(f => f.Position).Select(f => f.Value).ToList();
    }

    public static bool HasField(this HL7Segment segment, int fieldPosition)
    {
        return segment.Fields.Any(f => f.Position == fieldPosition && !string.IsNullOrEmpty(f.Value));
    }

    public static void RemoveField(this HL7Segment segment, int fieldPosition)
    {
        segment.Fields.RemoveAll(f => f.Position == fieldPosition);
    }

    public static int GetFieldCount(this HL7Segment segment)
    {
        return segment.Fields.Count;
    }

    public static bool IsEmpty(this HL7Segment segment)
    {
        return segment.Fields.All(f => f.IsEmpty());
    }

    public static HL7Segment Clone(this HL7Segment segment)
    {
        var cloned = new HL7Segment
        {
            Type = segment.Type,
            SequenceNumber = segment.SequenceNumber,
            RawData = segment.RawData,
            IsRequired = segment.IsRequired,
            MaxOccurrences = segment.MaxOccurrences
        };

        foreach (var field in segment.Fields)
        {
            var clonedField = new HL7Field
            {
                Position = field.Position,
                Value = field.Value,
                RawData = field.RawData,
                IsRequired = field.IsRequired,
                MaxLength = field.MaxLength,
                DataType = field.DataType
            };

            foreach (var component in field.Components)
            {
                var clonedComponent = new HL7Component
                {
                    Position = component.Position,
                    Value = component.Value
                };

                foreach (var subComponent in component.SubComponents)
                {
                    clonedComponent.SubComponents.Add(new HL7SubComponent
                    {
                        Position = subComponent.Position,
                        Value = subComponent.Value
                    });
                }

                clonedField.Components.Add(clonedComponent);
            }

            cloned.Fields.Add(clonedField);
        }

        return cloned;
    }

    public static void CopyFieldFrom(this HL7Segment targetSegment, HL7Segment sourceSegment, int fieldPosition)
    {
        var sourceField = sourceSegment.Fields.FirstOrDefault(f => f.Position == fieldPosition);
        if (sourceField != null)
        {
            targetSegment.RemoveField(fieldPosition);
            
            var clonedField = new HL7Field
            {
                Position = sourceField.Position,
                Value = sourceField.Value,
                RawData = sourceField.RawData,
                IsRequired = sourceField.IsRequired,
                MaxLength = sourceField.MaxLength,
                DataType = sourceField.DataType
            };

            foreach (var component in sourceField.Components)
            {
                var clonedComponent = new HL7Component
                {
                    Position = component.Position,
                    Value = component.Value
                };

                foreach (var subComponent in component.SubComponents)
                {
                    clonedComponent.SubComponents.Add(new HL7SubComponent
                    {
                        Position = subComponent.Position,
                        Value = subComponent.Value
                    });
                }

                clonedField.Components.Add(clonedComponent);
            }

            targetSegment.Fields.Add(clonedField);
        }
    }

    public static Dictionary<string, string> ToFieldDictionary(this HL7Segment segment)
    {
        var dictionary = new Dictionary<string, string>
        {
            ["SegmentType"] = segment.Type,
            ["SequenceNumber"] = segment.SequenceNumber.ToString(),
            ["RawData"] = segment.RawData
        };

        foreach (var field in segment.Fields.OrderBy(f => f.Position))
        {
            dictionary[$"Field{field.Position}"] = field.Value;
        }

        return dictionary;
    }

    public static string ToDisplayString(this HL7Segment segment, bool includeFieldPositions = false)
    {
        if (includeFieldPositions)
        {
            var fields = segment.Fields.OrderBy(f => f.Position)
                .Select(f => $"{f.Position}:{f.Value}")
                .ToArray();
            return $"{segment.Type}|{string.Join("|", fields)}";
        }
        else
        {
            return segment.RawData;
        }
    }

    public static bool ValidateRequiredFields(this HL7Segment segment, params int[] requiredFieldPositions)
    {
        return requiredFieldPositions.All(pos => segment.HasField(pos));
    }

    public static List<int> GetMissingRequiredFields(this HL7Segment segment, params int[] requiredFieldPositions)
    {
        return requiredFieldPositions.Where(pos => !segment.HasField(pos)).ToList();
    }

    public static void EnsureFieldExists(this HL7Segment segment, int fieldPosition, string defaultValue = "")
    {
        if (!segment.HasField(fieldPosition))
        {
            segment.SetFieldValue(fieldPosition, defaultValue);
        }
    }

    public static void TrimAllFields(this HL7Segment segment)
    {
        foreach (var field in segment.Fields)
        {
            field.Value = field.Value.Trim();
            foreach (var component in field.Components)
            {
                component.Value = component.Value.Trim();
                foreach (var subComponent in component.SubComponents)
                {
                    subComponent.Value = subComponent.Value.Trim();
                }
            }
        }
    }
}