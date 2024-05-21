using asec.Digitalization;

namespace asec.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddAsecServices(this IServiceCollection services)
    {
        services.AddSingleton<IDigitalizationToolRepo, DigitalizationToolRepo>();
        return services;
    }
}