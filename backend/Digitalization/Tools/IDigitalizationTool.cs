using asec.Models.Digitalization;

namespace asec.Digitalization.Tools;

public interface IDigitalizationTool
{
    Guid Id { get; set; }
    string Slug { get; }
    string Name { get; }
    string Version { get; }
    string Environment { get; }
    PhysicalMediaType PhysicalMedia { get; }
    bool IsAvailable { get; }

    Task Initialize(CancellationToken cancellationToken);
    Task<DigitalizationResult> Start(Process process, CancellationToken cancellationToken);
    bool EqualsToDB(DigitalizationTool digitalizationTool)
        => Name.Equals(digitalizationTool.Name) && Version.Equals(digitalizationTool.Version) && Environment.Equals(digitalizationTool.Environment);
}