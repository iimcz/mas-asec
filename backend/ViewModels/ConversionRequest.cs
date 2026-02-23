namespace asec.ViewModels;

public record ConversionRequest(
    string EmulatorId,
    string VersionId,
    List<string> DigitalObjectIds
);
