using asec.Digitalization;
using asec.DataConversion;
using asec.LongRunning;
using asec.Emulation;
using asec.Compatibility.EaasApi;
using asec.Compatibility.CollectiveAccess;
using asec.Upload;

namespace asec.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddAsecServices(this IServiceCollection services)
    {
        services.AddSingleton<IToolRepository, ToolRepository>();
        services.AddHostedService(provider => provider.GetService<IToolRepository>()!);

        services.AddSingleton<IEmulatorRepository, EmulatorRepository>();
        services.AddHostedService(provider => provider.GetService<IEmulatorRepository>()!);

        services.AddSingleton<IProcessManager<Digitalization.Process, DigitalizationResult, DigitalizationProcessDetail>, ProcessManager<Digitalization.Process, DigitalizationResult, DigitalizationProcessDetail>>();
        services.AddHostedService(provider => provider.GetService<IProcessManager<Digitalization.Process, DigitalizationResult, DigitalizationProcessDetail>>()!);

        services.AddSingleton<IProcessManager<DataConversion.Process, ConversionResult, ConversionProcessDetail>, ProcessManager<DataConversion.Process, ConversionResult, ConversionProcessDetail>>();
        services.AddHostedService(provider => provider.GetService<IProcessManager<DataConversion.Process, ConversionResult, ConversionProcessDetail>>()!);

        services.AddSingleton<IProcessManager<Emulation.BaseProcess, EmulationResult, EmulationProcessDetail>, ProcessManager<Emulation.BaseProcess, EmulationResult, EmulationProcessDetail>>();
        services.AddHostedService(provider => provider.GetService<IProcessManager<Emulation.BaseProcess, EmulationResult, EmulationProcessDetail>>()!);

        services.AddSingleton<IProcessManager<Upload.Process, UploadResult, EmptyProcessDetail>, ProcessManager<Upload.Process, UploadResult, EmptyProcessDetail>>();
        services.AddHostedService(provider => provider.GetService<IProcessManager<Upload.Process, UploadResult, EmptyProcessDetail>>()!);

        // EaaS clients
        services.AddScoped<EaasUploadClient>();
        services.AddScoped<ObjectRepositoryClient>();
        services.AddScoped<EnvironmentRepositoryClient>();
        services.AddScoped<ComponentsClient>();
        services.AddScoped<NetworksClient>();
        services.AddScoped<SessionsClient>();

        // CollectiveAccess clients
        services.AddSingleton<CollectiveAccessAuth>();
        services.AddScoped<SearchClient>();
        services.AddScoped<ItemClient>();
        services.AddScoped<EditClient>();

        return services;
    }
}
