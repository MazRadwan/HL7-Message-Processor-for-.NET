using HL7Processor.Web.Models;

namespace HL7Processor.Web.Services;

public interface IValidationService
{
    Task<Models.ValidationResult> ValidateMessageAsync(string hl7Content, string validationLevel = "Strict");
    Task<Models.ValidationResult> ValidateMessageAsync(Guid messageId, string validationLevel = "Strict");
    Task<List<Models.ValidationResult>> GetValidationHistoryAsync(int limit = 50);
    Task<Models.ValidationMetrics> GetValidationMetricsAsync(DateTime? fromDate = null);
}