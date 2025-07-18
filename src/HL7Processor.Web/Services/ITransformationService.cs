using HL7Processor.Infrastructure.Entities;

namespace HL7Processor.Web.Services;

public interface ITransformationService
{
    Task<List<TransformationRule>> GetTransformationRulesAsync();
    Task<TransformationRule?> GetTransformationRuleAsync(Guid id);
    Task<TransformationRule> CreateTransformationRuleAsync(TransformationRule rule);
    Task<TransformationRule> UpdateTransformationRuleAsync(TransformationRule rule);
    Task<bool> DeleteTransformationRuleAsync(Guid id);
    
    Task<string> ExecuteTransformationAsync(Guid ruleId, string inputData);
    Task<List<TransformationHistory>> GetTransformationHistoryAsync(int limit = 100);
    Task<TransformationStats> GetTransformationStatsAsync(DateTime? fromDate = null);
    
    Task<string> PreviewTransformationAsync(string ruleDefinition, string inputData);
}

public class TransformationStats
{
    public int TotalTransformations { get; set; }
    public int SuccessfulTransformations { get; set; }
    public int FailedTransformations { get; set; }
    public double AverageTransformationTimeMs { get; set; }
    public Dictionary<string, int> TransformationsByFormat { get; set; } = new();
    public Dictionary<string, int> TransformationsByRule { get; set; } = new();
    public List<TransformationTrend> DailyTrends { get; set; } = new();
}

public class TransformationTrend
{
    public DateTime Date { get; set; }
    public int TransformationCount { get; set; }
    public double AverageTimeMs { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
}