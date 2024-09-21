namespace asec.ViewModels;

public record ConversionRequest(
    string EmulatorId,
    List<string> ArtefactIds
);