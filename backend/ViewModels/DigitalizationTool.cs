namespace asec.ViewModels;

public record DigitalizationTool(
    string Id,
    string Slug,
    string Name,
    string Version,
    string PhysicalMediaType,
    bool IsAvailable
);