namespace asec.ViewModels;

public record DigitalizationRequest(
    string ToolId,
    string VersionId,
    string ParatextId
);
