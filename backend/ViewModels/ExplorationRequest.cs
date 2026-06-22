namespace asec.ViewModels;

public record ExplorationRequest(
    string EnvironmentId,
    string VersionId,
    List<string> DigitalObjectIds
);
