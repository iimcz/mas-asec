using asec.Digitalization;
using asec.DataConversion;
using asec.LongRunning;
using asec.Emulation;
using asec.Compatibility.EaasApi;

namespace asec.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddAsecServices(this IServiceCollection services)
    {
        services.AddSingleton<IToolRepository, ToolRepository>();
        services.AddHostedService(provider => provider.GetService<IToolRepository>()!);

        services.AddSingleton<IEmulatorRepository, EmulatorRepository>();
        services.AddHostedService(provider => provider.GetService<IEmulatorRepository>()!);

        services.AddSingleton<IProcessManager<Digitalization.Process, DigitalizationResult>, ProcessManager<Digitalization.Process, DigitalizationResult>>();
        services.AddHostedService(provider => provider.GetService<IProcessManager<Digitalization.Process, DigitalizationResult>>()!);

        services.AddSingleton<IProcessManager<DataConversion.Process, ConversionResult>, ProcessManager<DataConversion.Process, ConversionResult>>();
        services.AddHostedService(provider => provider.GetService<IProcessManager<DataConversion.Process, ConversionResult>>()!);

        // EaaS clients
        services.AddScoped<EaasUploadClient>();
        services.AddScoped<ObjectRepositoryClient>();
        
        return services;
    }
}