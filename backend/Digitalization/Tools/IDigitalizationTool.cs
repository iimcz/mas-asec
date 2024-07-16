namespace asec.Digitalization.Tools;

public interface IDigitalizationTool
{
    string Id { get; }
    string Name { get; }
    string Version { get; }
    PhysicalMediaType PhysicalMedia { get; }
    bool IsAvailable { get; }

    Task Initialize(CancellationToken cancellationToken);
    Task<string> Start(Process process, CancellationToken cancellationToken);
}