using HL7Processor.Web.Models;

namespace HL7Processor.Web.Services;

public interface ITransformationService
{
    Task<List<Models.TransformationRule>> GetTransformationRulesAsync();
    Task<Models.TransformationRule?> GetTransformationRuleAsync(Guid id);
    Task<Models.TransformationRule> CreateTransformationRuleAsync(Models.TransformationRule rule);
    Task<Models.TransformationRule> UpdateTransformationRuleAsync(Models.TransformationRule rule);
    Task<bool> DeleteTransformationRuleAsync(Guid id);
    
    Task<string> ExecuteTransformationAsync(Guid ruleId, string inputData);
    Task<List<Models.TransformationHistory>> GetTransformationHistoryAsync(int limit = 100);
    Task<Models.TransformationStats> GetTransformationStatsAsync(DateTime? fromDate = null);
    
    Task<string> PreviewTransformationAsync(string ruleDefinition, string inputData);
}