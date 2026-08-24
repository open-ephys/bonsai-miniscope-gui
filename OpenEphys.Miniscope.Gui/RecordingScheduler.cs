using System.Reactive.Concurrency;
using System.Threading;

namespace OpenEphys.Miniscope.Gui;

/// <summary>
/// Provides the single, process-wide scheduler used to serialize every notification in the
/// data-recording pipeline (frame delivery and the recording stop signal) onto one dedicated
/// thread, so a <c>TakeUntil</c> gating the pipeline is never entered concurrently from two
/// different threads.
/// </summary>
public static class RecordingScheduler
{
    /// <summary>
    /// The shared recording scheduler. Backed by a single <see cref="EventLoopScheduler"/>, so
    /// every action scheduled on it runs sequentially on the same dedicated background thread.
    /// </summary>
    public static readonly EventLoopScheduler Instance = new(start => new Thread(start)
    {
        Name = "MiniscopeRecording",
        IsBackground = true,
    });
}
