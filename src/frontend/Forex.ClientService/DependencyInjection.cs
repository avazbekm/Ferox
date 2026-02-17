namespace Forex.ClientService;

using Forex.ClientService.Configuration;
using Forex.ClientService.Interfaces;
using Forex.ClientService.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;

public static class DependencyInjection
{
    public static IServiceCollection AddClientServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddHttpClient();
        services.AddSingleton<AuthStore>();

        services.AddSingleton<IFileStorageClient, FileStorageClient>();

        services.AddTransient<AuthHeaderHandler>();

        string apiBaseUrl = config.GetValue<string>("ApiBaseUrl")!;
        services.AddAllRefitClients(apiBaseUrl);

        services.AddSingleton<ForexClient>();

        return services;
    }

    private static IServiceCollection AddAllRefitClients(this IServiceCollection services, string baseUrl)
    {
        var assembly = typeof(IApiAuth).Assembly;
        var refitInterfaces = assembly.GetTypes()
            .Where(t => t.IsInterface && t.Name.StartsWith("IApi"))
            .ToList();

        foreach (var apiInterface in refitInterfaces)
        {
            services.AddRefitClient(apiInterface)
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(baseUrl))
                .AddHttpMessageHandler<AuthHeaderHandler>();
        }

        return services;
    }
}
