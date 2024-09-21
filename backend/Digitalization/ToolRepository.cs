using asec.Configuration;
using asec.Digitalization.Tools;
using asec.Models;
using asec.Models.Digitalization;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Writers;

namespace asec.Digitalization;


public class ToolRepository : IToolRepository
{
    private ILogger<ToolRepository> _logger;
    private DigitalizationToolsOptions _options;
    private IServiceProvider _serviceProvider;
    private Dictionary<Guid, IDigitalizationTool> _tools = new();
    private List<IDigitalizationTool> _uninitializedTools = new();

    public ToolRepository(ILogger<ToolRepository> logger, IOptions<DigitalizationToolsOptions> options, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _options = options.Value;
        _serviceProvider = serviceProvider;

        _logger.LogInformation("Adding {} tools.", _options.Configurations.Count);
        foreach (var config in _options.Configurations)
        {
            var tool = config.ConstructTool();
            _uninitializedTools.Add(tool);
        }
    }

    public IEnumerable<IDigitalizationTool> GetDigitalizationTools()
    {
        return _tools.Values;
    }

    public IDigitalizationTool GetDigitalizationTool(string toolId)
    {
        var id = Guid.Parse(toolId);
        return _tools.GetValueOrDefault(id);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AsecDBContext>();

        foreach (var tool in _uninitializedTools)
        {
            _logger.LogInformation("Name: {}\nVersion: {}\nMedia: {}", tool.Name, tool.Version, tool.PhysicalMedia.ToString());
            await tool.Initialize(cancellationToken);
            if (!tool.IsAvailable)
            {
                _logger.LogInformation("Tool unavailable, skipping...");
                continue;
            }

            // TODO: rewrite so that LINQ can translate this to SQL
            var dbTool = dbContext.DigitalizationTools.AsEnumerable().FirstOrDefault(t => tool.EqualsToDB(t));
            if (dbTool == null)
            {
                _logger.LogInformation("Adding new tool to database.");
                dbTool = new DigitalizationTool() {
                    Id = Guid.NewGuid(),
                    Name = tool.Name,
                    Version = tool.Version,
                    Environment = tool.Environment,
                    PhysicalMedia = tool.PhysicalMedia.ToString()
                };
                tool.Id = dbTool.Id;
                dbContext.DigitalizationTools.Add(dbTool);
            }
            else
            {
                _logger.LogInformation("Tool already exists in database, so using that.");
                // TODO: allow updating old tools? Maybe through the UI?
                tool.Id = dbTool.Id;
            }
            _tools.Add(tool.Id, tool);
        }
        _uninitializedTools.Clear();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // Do nothing.
        return Task.CompletedTask;
    }
}