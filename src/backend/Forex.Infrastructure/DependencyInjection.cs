namespace Forex.Infrastructure;

using Forex.Application.Common.Interfaces;
using Forex.Infrastructure.Background;
using Forex.Infrastructure.Identity;
using Forex.Infrastructure.Persistence;
using Forex.Infrastructure.Persistence.Interceptors;
using Forex.Infrastructure.Security;
using Forex.Infrastructure.Storage;
using Forex.Infrastructure.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Minio;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IServiceCollection services, IConfiguration conf)
    {
        services.AddHttpContextAccessor();
        services.AddDbContext(conf);
        services.AddFileStorage(conf);
        services.AddIdentity();
        services.AddHostedService<FileCleanupBackgroundService>();
        services.AddScoped<IPagingMetadataWriter, HttpPagingMetadataWriter>();
    }

    private static void AddIdentity(this IServiceCollection services)
    {
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
    }

    private static void AddDbContext(this IServiceCollection services, IConfiguration conf)
    {
        services.AddScoped<AuditInterceptor>();

        var connectionString = conf.GetConnectionString("forex")
                               ?? conf.GetConnectionString("DefaultConnection");

        services.AddDbContext<IAppDbContext, AppDbContext>((sp, options) =>
            options.UseNpgsql(connectionString)
                   .AddInterceptors(sp.GetRequiredService<AuditInterceptor>()));
    }

    public static void AddFileStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MinioStorageOptions>()
            .Bind(configuration.GetSection("Minio"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MinioStorageOptions>>().Value;

            var uri = new Uri(options.Endpoint.StartsWith("http") ? options.Endpoint : $"http://{options.Endpoint}");
            var finalEndpoint = uri.Authority;

            var client = new MinioClient()
                .WithEndpoint(finalEndpoint)
                .WithCredentials(options.AccessKey, options.SecretKey);

            if (options.UseSsl || uri.Scheme == Uri.UriSchemeHttps)
                client.WithSSL();

            return client.Build();
        });

        services.AddScoped<IFileStorageService, MinioFileStorageService>();
    }
}
