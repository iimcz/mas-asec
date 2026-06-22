namespace asec.ViewModels;

public enum ExplorationInputType
{
    GotoExploration,
    GotoCheck,
    GotoKiosk,
    Done
}

public record ExplorationDone(
    string PackageLabel,
    string PackageNote
);
