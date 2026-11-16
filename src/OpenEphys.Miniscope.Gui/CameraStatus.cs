using System;
using YamlDotNet.Serialization;

namespace OpenEphys.Miniscope.Gui;

partial class CameraStatus : IEquatable<CameraStatus>
{
    /// <summary>
    /// Whether the user has asked the Miniscope to acquire.
    /// </summary>
    [YamlIgnore]
    public bool AcquisitionRequested { get; set; }

    /// <summary>
    /// Whether the data display is frozen. When <see langword="true"/>, the workflow stops sampling new
    /// frames into the image and signal panels while data acquisition continues unaffected.
    /// </summary>
    [YamlIgnore]
    public bool Paused { get; set; }

    /// <inheritdoc/>
    public bool Equals(CameraStatus other) =>
        other is not null &&
        CameraIndex == other.CameraIndex &&
        AcquisitionRequested == other.AcquisitionRequested &&
        Paused == other.Paused;

    /// <inheritdoc/>
    public override bool Equals(object obj) => Equals(obj as CameraStatus);

    /// <inheritdoc/>
    public override int GetHashCode() => (CameraIndex, AcquisitionRequested, Paused).GetHashCode();
}
