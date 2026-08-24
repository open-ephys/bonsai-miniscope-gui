using System;
using YamlDotNet.Serialization;

namespace OpenEphys.Miniscope.Gui;

partial class OverlaySettings : IEquatable<OverlaySettings>
{
    /// <summary>
    /// Whether the user asked to capture the current image on this frame.
    /// </summary>
    [YamlIgnore]
    public bool CaptureRequested { get; set; }

    /// <inheritdoc/>
    public bool Equals(OverlaySettings other) =>
        other is not null &&
        CaptureRequested == other.CaptureRequested &&
        ApplyOverlay == other.ApplyOverlay &&
        ReferencePath == other.ReferencePath &&
        ReferenceColor == other.ReferenceColor &&
        LiveColor == other.LiveColor;

    /// <inheritdoc/>
    public override bool Equals(object obj) => Equals(obj as OverlaySettings);

    /// <inheritdoc/>
    public override int GetHashCode() => (CaptureRequested, ApplyOverlay, ReferencePath, ReferenceColor, LiveColor).GetHashCode();
}
