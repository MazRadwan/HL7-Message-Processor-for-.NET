using HL7Processor.Application.Interfaces;
using HL7Processor.Application.UseCases;
using HL7Processor.Core.Interfaces;
using HL7Processor.Infrastructure.Auth;
using HL7Processor.Infrastructure.Mapping;
using HL7Processor.Infrastructure.Repositories;
using HL7Processor.Infrastructure.Services;
using HL7Processor.Infrastructure.UseCases;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HL7Processor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register configuration
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddSingleton(provider => 
            configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings());
        
        // Register repositories
        services.AddScoped<IArchivedMessageRepository, ArchivedMessageRepository>();
        
        // Register mappers
        services.AddScoped<ArchivedMessageMapper>();
        services.AddScoped<IArchivedMessageMapper, ArchivedMessageMapper>();
        
        // Register infrastructure services
        services.AddScoped<ITokenService, TokenServiceAdapter>();
        
        // Register missing existing use cases (CRITICAL FIX)
        services.AddScoped<IGetArchivedMessagesUseCase, GetArchivedMessagesUseCase>();
        services.AddScoped<IGetArchivedMessageCountUseCase, GetArchivedMessageCountUseCase>();
        
        // Register new query use cases for Web layer
        services.AddScoped<IGetDashboardDataUseCase, GetDashboardDataUseCase>();
        services.AddScoped<IGetParserMetricsUseCase, GetParserMetricsUseCase>();
        services.AddScoped<IGetValidationDataUseCase, GetValidationDataUseCase>();
        services.AddScoped<IGetTransformationDataUseCase, GetTransformationDataUseCase>();
        services.AddScoped<IGetSystemHealthUseCase, GetSystemHealthUseCase>();
        services.AddScoped<ICreateTransformationRuleUseCase, CreateTransformationRuleUseCase>();
        services.AddScoped<IUpdateTransformationRuleUseCase, UpdateTransformationRuleUseCase>();
        services.AddScoped<IDeleteTransformationRuleUseCase, DeleteTransformationRuleUseCase>();
        services.AddScoped<IGetTransformationStatsUseCase, GetTransformationStatsUseCase>();
        
        return services;
    }
}