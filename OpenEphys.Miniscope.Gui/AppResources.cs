using System.Drawing;
using System.Reflection;

namespace OpenEphys.Miniscope.Gui;

/// <summary>
/// Helpers for loading assets embedded in this assembly.
/// </summary>
internal static class AppResources
{
    /// <summary>
    /// Loads the application icon embedded in this assembly.
    /// </summary>
    /// <returns>A new <see cref="Icon"/>; the caller is responsible for disposing it.</returns>
    public static Icon LoadIcon()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("OpenEphys.Miniscope.Gui.Resources.icon.ico");
        return new Icon(stream);
    }
}
