using asec.Digitalization;
using asec.LongRunning;

namespace asec.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddAsecServices(this IServiceCollection services)
    {
        services.AddSingleton<IToolRepository, ToolRepository>();
        services.AddHostedService(provider => provider.GetService<IToolRepository>()!);

        services.AddSingleton<IProcessManager<Process>, ProcessManager<Process>>();
        services.AddHostedService(provider => provider.GetService<IProcessManager<Process>>()!);
        return services;
    }
}