using System;
using System.ComponentModel;
using System.Reactive.Linq;
using Bonsai;

namespace OpenEphys.MiniscopeV4.Gui;

/// <summary>
/// Observes the source sequence on the shared <see cref="RecordingScheduler"/>, moving every
/// downstream notification onto the single Miniscope recording thread.
/// </summary>
[Combinator]
[Description("Serializes downstream notifications onto the shared Miniscope recording thread.")]
public class ObserveOnRecording
{
    /// <summary>
    /// Observes the <paramref name="source"/> sequence on the shared <see cref="RecordingScheduler"/>.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
    /// <param name="source">The sequence whose notifications are marshaled onto the recording thread.</param>
    /// <returns>The source sequence whose observer callbacks run on the recording thread.</returns>
    public IObservable<TSource> Process<TSource>(IObservable<TSource> source)
    {
        return source.ObserveOn(RecordingScheduler.Instance);
    }
}
