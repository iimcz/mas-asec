namespace asec.Digitalization;

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