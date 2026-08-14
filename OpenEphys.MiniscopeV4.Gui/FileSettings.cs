using System;
using YamlDotNet.Serialization;

namespace OpenEphys.MiniscopeV4.Gui;

partial class FileSettings : IEquatable<FileSettings>
{
    /// <summary>
    /// Whether the user has asked to record.
    /// </summary>
    [YamlIgnore]
    public bool RecordingRequested { get; set; }

    /// <inheritdoc/>
    public bool Equals(FileSettings other) =>
        other is not null &&
        RecordingRequested == other.RecordingRequested &&
        RecordingMode == other.RecordingMode &&
        CompressVideo == other.CompressVideo &&
        FileName == other.FileName &&
        Suffix == other.Suffix &&
        RecordingDuration == other.RecordingDuration &&
        TotalDuration == other.TotalDuration &&
        SegmentMode == other.SegmentMode &&
        TriggerInput == other.TriggerInput;

    /// <inheritdoc/>
    public override bool Equals(object obj) => Equals(obj as FileSettings);

    /// <inheritdoc/>
    public override int GetHashCode() =>
        (RecordingRequested, RecordingMode, CompressVideo, FileName, Suffix, RecordingDuration, TotalDuration, SegmentMode, TriggerInput).GetHashCode();
}
