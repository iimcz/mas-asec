using asec.Configuration;
using asec.Digitalization.Tools;
using asec.Models.Digitalization;
using Microsoft.Extensions.Options;

namespace asec.Digitalization;


public class ToolRepository : IToolRepository
{
    private ILogger<ToolRepository> _logger;
    private DigitalizationToolsOptions _options;
    private Dictionary<string, IDigitalizationTool> _tools = new();

    public ToolRepository(ILogger<ToolRepository> logger, IOptions<DigitalizationToolsOptions> options)
    {
        _logger = logger;
        _options = options.Value;

        _logger.LogInformation("Adding {} tools.", _options.Configurations.Count);
        foreach (var config in _options.Configurations)
        {
            var tool = config.ConstructTool();
            _tools.Add(tool.Id, tool);
        }
    }

    public IEnumerable<IDigitalizationTool> GetDigitalizationTools()
    {
        return _tools.Values;
    }

    public IDigitalizationTool GetDigitalizationTool(string toolId)
    {
        return _tools.GetValueOrDefault(toolId);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var tool in _tools.Values)
        {
            _logger.LogInformation("Initializing tool {}", tool.Id);
            await tool.Initialize(cancellationToken);
            _logger.LogInformation("Tool {} available: {}", tool.Id, tool.IsAvailable);
            if (tool.IsAvailable)
            {
                _logger.LogInformation("Name: {}\nVersion: {}\nMedia: {}", tool.Name, tool.Version, tool.PhysicalMedia.ToString());
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // Do nothing.
        return Task.CompletedTask;
    }
}