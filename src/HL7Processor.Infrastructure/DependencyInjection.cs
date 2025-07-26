using HL7Processor.Application.Interfaces;
using HL7Processor.Core.Interfaces;
using HL7Processor.Infrastructure.Auth;
using HL7Processor.Infrastructure.Mapping;
using HL7Processor.Infrastructure.Repositories;
using HL7Processor.Infrastructure.Services;
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
        
        return services;
    }
}