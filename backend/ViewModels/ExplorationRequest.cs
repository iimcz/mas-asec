namespace asec.ViewModels;

public record ExplorationRequest(
    string EnvironmentId,
    List<string> DigitalObjectIds
);
