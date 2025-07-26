using System.Collections.Generic;

namespace HL7Processor.Application.DTOs;

public class TransformationStatsDto
{
    public int TotalTransformations { get; set; }
    public int SuccessfulTransformations { get; set; }
    public int FailedTransformations { get; set; }
    public double AverageTransformationTimeMs { get; set; }
    public Dictionary<string, int> TransformationsByFormat { get; set; } = new();
    public Dictionary<string, int> TransformationsByRule { get; set; } = new();
    public List<TransformationTrendDto> DailyTrends { get; set; } = new();
}

public class TransformationTrendDto
{
    public DateTime Date { get; set; }
    public int TransformationCount { get; set; }
    public double AverageTimeMs { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
} 