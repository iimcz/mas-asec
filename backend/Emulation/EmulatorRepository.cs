
using System.Text.Json;
using asec.Configuration;
using asec.DataConversion.Converters;
using asec.Models;
using asec.Models.Digitalization;
using asec.Models.Emulation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace asec.Emulation;

public class EmulatorRepository : IEmulatorRepository
{
    private ILogger<EmulatorRepository> _logger;
    private IServiceProvider _serviceProvider;
    private EmulatorOptions _options;
    private Dictionary<Guid, IConverter> _converters = new();


    public EmulatorRepository(ILogger<EmulatorRepository> logger, IServiceProvider serviceProvider, IOptions<EmulatorOptions> options)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _options = options.Value;

        _logger.LogInformation("Loaded {} emulators from configuration.", _options.Emulators.Count);
        _logger.LogInformation("Loaded {} exploration environments from configuration.", _options.ExplorationEnvironments.Count);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AsecDBContext>();

        foreach (var config in _options.Emulators)
        {
            var platforms = await dbContext.Platforms
                .Where(p => config.Platforms.Contains(p.Name))
                .ToListAsync(cancellationToken);
            if (platforms.Count != config.Platforms.Count)
            {
                _logger.LogWarning("Configured emulator has {} platforms, but only {} of them are present in the database. Check the platform config file!",
                    config.Platforms.Count, platforms.Count);
            }

            var emulator = await dbContext.Emulators
                .Include(e => e.Platforms)
                .FirstOrDefaultAsync(e => e.Name == config.Name, cancellationToken);
            if (emulator == null)
            {
                _logger.LogInformation("Emulator {} not found, adding to database.", config.Name);
                emulator = new()
                {
                    Id = Guid.Empty,
                    Name = config.Name
                };
                await dbContext.Emulators.AddAsync(emulator, cancellationToken);
            }
            emulator.Homepage = config.Homepage;
            emulator.Platforms = platforms;

            await LoadEmulatorVersions(dbContext, config, emulator, cancellationToken);
        }

        foreach (var env in _options.ExplorationEnvironments)
        {
            var environment = await dbContext.Environments
                .Include(e => e.Converters)
                .Where(e => e.Type == EnvironmentType.Exploration)
                .FirstOrDefaultAsync(e => e.Name == env.Name && e.Version == env.Version);

            if (environment == null)
            {
                _logger.LogInformation("Environment record for exploration environment {} not found, adding to database.", env.Name);
                environment = new()
                {
                    Id = Guid.Empty,
                    Name = env.Name,
                    Version = env.Version
                };
                await dbContext.AddAsync(environment, cancellationToken);
            }
            environment.EaasId = env.EaasId;
            environment.InternetConnected = env.InternetConnected;
            environment.Note = env.Note;

            await LoadConverters(dbContext, env, environment, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return;
    }

    private async Task LoadEmulatorVersions(AsecDBContext dbContext, EmulatorConfig config, Emulator emulator, CancellationToken cancellationToken)
    {
        foreach (var env in config.Environments)
        {
            var environment = await dbContext.Environments
                .Include(e => e.Converters)
                .Where(e => e.Type == EnvironmentType.Kiosk)
                .FirstOrDefaultAsync(e => e.Emulator == emulator && e.Version == env.Version, cancellationToken);

            if (environment == null)
            {
                _logger.LogInformation("Environment record for environment {} version {} not found, adding to database.", config.Name, env.Version);
                environment = new()
                {
                    Id = Guid.Empty,
                    Emulator = emulator,
                    Version = env.Version,
                    Type = EnvironmentType.Kiosk
                };
                await dbContext.AddAsync(environment, cancellationToken);
            }
            environment.EaasId = env.EaasId;
            environment.InternetConnected = env.InternetConnected;

            await LoadConverters(dbContext, env, environment, cancellationToken);
        }
    }

    private async Task LoadConverters(AsecDBContext dbContext, EmulatorEnvironmentConfig config, EmulationEnvironment environment, CancellationToken cancellationToken)
    {
        List<Converter> environmentConverters = new();

        foreach (var conv in config.Converters)
        {
            var converter = conv.ConstructConverter();
            var dbConverter = (await dbContext.Converters.ToListAsync(cancellationToken))
                .FirstOrDefault(c => converter.EqualsToDB(c));

            if (dbConverter == null)
            {
                _logger.LogInformation("Converter '{}' not found in database, creating new.", converter.Name);
                dbConverter = new()
                {
                    Id = Guid.NewGuid(),
                    Name = converter.Name,
                    Environment = converter.Environment,
                    Version = converter.Version
                };
                await dbContext.Converters.AddAsync(dbConverter, cancellationToken);
            }
            dbConverter.Configuration = JsonSerializer.Serialize(conv);
            dbConverter.SupportedArtefactTypes = converter.SupportedArtefactTypes;
            converter.Id = dbConverter.Id;

            if (!_converters.ContainsKey(converter.Id))
                _converters.Add(converter.Id, converter);
            environmentConverters.Add(dbConverter);
        }
        environment.Converters = environmentConverters;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // Do nothing.
        return Task.CompletedTask;
    }

    public async Task<Emulator> GetEmulatorAsync(Guid id)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AsecDBContext>();

        return await dbContext.Emulators.FindAsync(id);
    }

    public async Task<IConverter> GetEnvironmentConverterAsync(Guid emulatorId, ArtefactType sourceType)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AsecDBContext>();

        var emulator = await dbContext.Environments
            .Include(e => e.Converters)
            .FirstOrDefaultAsync(e => e.Id == emulatorId);
        if (emulator == null)
            return null;

        var dbConverter = emulator.Converters
            .Where(c => c.SupportedArtefactTypes.Contains(sourceType))
            .FirstOrDefault();
        if (dbConverter == null)
            return null;

        return _converters[dbConverter.Id];
    }

    public async Task<IEnumerable<IConverter>> GetEnvironmentConvertersAsync(Guid emulatorId)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AsecDBContext>();

        var emulator = await dbContext.Environments
            .Include(e => e.Converters)
            .FirstOrDefaultAsync(e => e.Id == emulatorId);
        if (emulator == null)
            return null;

        return emulator.Converters
            .Where(c => _converters.ContainsKey(c.Id))
            .Select(c => _converters[c.Id]);
    }

    public async Task<EmulationEnvironment> GetEnvironmentAsync(Guid id)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AsecDBContext>();

        return await dbContext.Environments.FindAsync(id);
    }
}
