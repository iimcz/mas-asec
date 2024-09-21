using System.Threading.Channels;
using Microsoft.AspNetCore.Components.Web;

namespace asec.LongRunning;

public interface IProcess<TResult>
{
    Guid Id { get; }
    CancellationToken CancellationToken { get; }
    DateTime StartTime { get; }
    string BaseDir { get; }
    string LogPath { get; }
    // TODO: consider locking modification to avoid changing the value during get from different thread
    ProcessStatus Status { get; }
    string StatusDetail { get; }

    Task<TResult> Start(CancellationToken cancellationToken);
}