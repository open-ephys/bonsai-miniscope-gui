using System;
using YamlDotNet.Serialization;

namespace OpenEphys.Miniscope.Gui;

partial class MaxProjectionSettings : IEquatable<MaxProjectionSettings>
{
    /// <summary>
    /// Whether the user asked to reset the accumulation on this frame.
    /// </summary>
    [YamlIgnore]
    public bool ResetRequested { get; set; }

    /// <inheritdoc/>
    public bool Equals(MaxProjectionSettings other) => other is not null && ResetRequested == other.ResetRequested;

    /// <inheritdoc/>
    public override bool Equals(object obj) => Equals(obj as MaxProjectionSettings);

    /// <inheritdoc/>
    public override int GetHashCode() => ResetRequested.GetHashCode();
}
