using asec.Digitalization;

namespace asec.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddAsecServices(this IServiceCollection services)
    {
        services.AddSingleton<IToolRepository, ToolRepository>();
        services.AddHostedService(provider => provider.GetService<IToolRepository>()!);

        services.AddSingleton<IProcessManager, ProcessManager>();
        services.AddHostedService(provider => provider.GetService<IProcessManager>()!);
        return services;
    }
}