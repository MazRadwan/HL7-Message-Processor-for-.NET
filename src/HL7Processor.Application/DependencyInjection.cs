using HL7Processor.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace HL7Processor.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register use cases
        services.AddScoped<IGetArchivedMessagesUseCase, GetArchivedMessagesUseCase>();
        services.AddScoped<IGetArchivedMessageCountUseCase, GetArchivedMessageCountUseCase>();
        services.AddScoped<ISubmitMessageUseCase, SubmitMessageUseCase>();
        services.AddScoped<IRequeueMessageUseCase, RequeueMessageUseCase>();
        services.AddScoped<IAuthenticateUserUseCase, AuthenticateUserUseCase>();

        return services;
    }
}