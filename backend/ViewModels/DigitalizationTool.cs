namespace asec.ViewModels;

public record DigitalizationTool(
    string Id,
    string Name,
    string Version,
    string PhysicalMediaType,
    bool IsAvailable
);