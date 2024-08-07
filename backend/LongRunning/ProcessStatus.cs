namespace asec.LongRunning;

public enum ProcessStatus
{
    Initialization,
    Running,
    WaitingForInput,
    WaitingForProcess,
    Failed,
    Success,
    Restarting
}