using System.Threading.Channels;

namespace asec.LongRunning;

public interface IProcess
{
    Guid Id { get; }
    CancellationToken CancellationToken { get; }
    DateTime StartTime { get; }
    string BaseDir { get; }
    string LogPath { get; }
    // TODO: consider locking modification to avoid changing the value during get from different thread
    ProcessStatus Status { get; }
    string StatusDetail { get; }

    Task<string> Start(CancellationToken cancellationToken);
}