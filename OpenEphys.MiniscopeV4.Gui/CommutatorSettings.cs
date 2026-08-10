using System;
using YamlDotNet.Serialization;

namespace OpenEphys.MiniscopeV4.Gui;

partial class CommutatorSettings : IEquatable<CommutatorSettings>
{
    /// <summary>
    /// Whether the user has asked for the commutator serial port to be opened.
    /// </summary>
    [YamlIgnore]
    public bool ConnectionRequested { get; set; }

    /// <inheritdoc/>
    public bool Equals(CommutatorSettings other) =>
        other is not null &&
        PortName == other.PortName &&
        ConnectionRequested == other.ConnectionRequested &&
        Enable == other.Enable &&
        EnableLed == other.EnableLed &&
        AutoConnect == other.AutoConnect;

    /// <inheritdoc/>
    public override bool Equals(object obj) => Equals(obj as CommutatorSettings);

    /// <inheritdoc/>
    public override int GetHashCode() => (PortName, ConnectionRequested, Enable, EnableLed, AutoConnect).GetHashCode();
}
