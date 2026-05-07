using Microsoft.Extensions.DependencyInjection.Extensions;
using Minio.AspNetCore;
using IMinioClient = Minio.IMinioClient;

namespace asec.Extensions;

public static class MinioServiceCollectionExtensions
{
    /// <summary>
    /// Adds a named MinIO client to the service collection as a keyed service.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The key name for this client instance.</param>
    /// <param name="configureOptions">An action to configure the MinIO client options.</param>
    public static IServiceCollection AddKeyedMinio(
        this IServiceCollection services, 
        string key,
        Action<MinioOptions> configure)
    {
        services.Configure(key, configure);
        services.TryAddSingleton<IMinioClientFactory, MinioClientFactory>();
        services.TryAddKeyedSingleton<IMinioClient>(key, (sp, key) => sp.GetRequiredService<IMinioClientFactory>().CreateClient((string)key));

        return services;
    }
}
