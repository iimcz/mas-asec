namespace asec.ViewModels;

// Should copy asec.Exploration.Process.ExplorationMessage, but without Ping, Abort, Save and Finish
public enum ExplorationInputType
{
    GotoExploration,
    GotoKiosk
}

public record ExplorationDone(
    string PackageName,
    string PackageNote
);
