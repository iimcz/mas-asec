using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace asec.Digitalization.Tools;

public class GreaseweazleToolConfig : DigitalizationToolConfig
{
    public string GWPath { get; set; }
    public PhysicalMediaType MediaType { get; set; }

    public override IDigitalizationTool ConstructTool()
    {
        return new GreaseweazleTool(Id, GWPath, MediaType);
    }
}

public class GreaseweazleTool : IDigitalizationTool
{
    public string Id { get; private set; }

    public string Name => "Greaseweazle";

    public string Version { get; private set; }

    public PhysicalMediaType PhysicalMedia { get; private set; }

    public bool IsAvailable { get; private set; } = false;

    private string _GWPath;

    public GreaseweazleTool(string id, string gWPath, PhysicalMediaType mediaType)
    {
        if (mediaType == PhysicalMediaType.Diskette35 || 
            mediaType == PhysicalMediaType.Diskette54)
            PhysicalMedia = mediaType;
        else
            throw new ArgumentException("Invalid physical media type: {}", nameof(mediaType));
        _GWPath = gWPath;
        Id = id;
    }

    public async Task Initialize(CancellationToken cancellationToken)
    {
        if (Path.Exists(_GWPath))
        {
            var gwProcess = System.Diagnostics.Process.Start(_GWPath, "info");
            await gwProcess.WaitForExitAsync(cancellationToken);
            if (!gwProcess.HasExited || gwProcess.ExitCode != 0)
                return;
            
            gwProcess.StandardOutput.ReadLine();
        }
    }

    public async Task<string> Start(Process process, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}