using HL7Processor.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace HL7Processor.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register existing use cases
        // Note: Some implementations moved to Infrastructure layer
        services.AddScoped<ISubmitMessageUseCase, SubmitMessageUseCase>();
        services.AddScoped<IRequeueMessageUseCase, RequeueMessageUseCase>();
        services.AddScoped<IAuthenticateUserUseCase, AuthenticateUserUseCase>();

        return services;
    }
}