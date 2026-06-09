namespace asec.ViewModels;

public record ExplorationDetail(
    string StreamUrl,
    RunnablePackage LatestParsedRunnable
);

public record ExplorationProcess : Process<ExplorationDetail>
{
    public static ExplorationProcess FromProcess(asec.Exploration.Process process)
    {
        return new ExplorationProcess {
            Id = process.Id.ToString(),
            StartTime = process.StartTime.ToString(),
            Status = process.Status.ToString(),
            StatusDetail = new(
                process.CurrentStreamUrl,
                RunnablePackage.FromDBEntity(process.LatestRunnablePackage)
            )
        };
    }
}
