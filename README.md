# Open Ephys Miniscope V4 GUI

A self-contained ImGui-based graphical interface for configuring and acquiring data from a UCLA
Miniscope V4 head-borne miniature microscope, including control of an Open Ephys commutator.

It wraps the acquisition node from
[`OpenEphys.Miniscope`](https://www.nuget.org/packages/OpenEphys.Miniscope) in a single-window GUI
(settings panel, live video/data display, file recording controls, and a status/console log).

Full hardware and experiment documentation: <https://open-ephys.github.io/miniscope-docs/>.

## Installing

### As a standalone application

Download the latest `MiniscopeGui-Setup-*.exe` from the [Releases](../../releases) page and run it. The
installer:

- Installs to your user profile (no admin rights required).
- Downloads all dependencies (Bonsai and required packages) so no separate Bonsai install is needed.
- Adds Start Menu / Desktop shortcuts to launch the GUI.

> [!NOTE]
> The GUI is currently in beta, and has not been released on GitHub yet. To download the GUI while
> it is in beta, please refer to the [Software
> Guide](https://open-ephys.github.io/miniscope-docs/Software-Guide/index.html) for instructions on
> how to download and install the GUI.

### As a Bonsai package

Add the [`OpenEphys.MiniscopeV4.Gui`](https://www.nuget.org/packages/OpenEphys.MiniscopeV4.Gui) package
through Bonsai's package manager, then drop the `MiniscopeGui.bonsai` workflow into the editor.

> [!NOTE]
> The GUI is currently in beta, and has not been released on NuGet yet. To download the GUI while
> it is in beta, please refer to the [Software
> Guide](https://open-ephys.github.io/miniscope-docs/Software-Guide/custom-workflows.html) for instructions on
> how to download and install the GUI package.

## Developing

Prerequisites: Visual Studio 2026, and a Windows machine.

1. Clone the repository and open `OpenEphys.MiniscopeV4.Gui.sln`.
2. Double-click `.bonsai\Setup.cmd` once. This downloads a copy of Bonsai into `.bonsai\` and restores
   the packages listed in `.bonsai\Bonsai.config`.
3. Build the solution; this automatically generates a
   `OpenEphys.MiniscopeV4.Gui\Configuration\Settings.cs` file containing generated classes based on
   the `OpenEphys.MiniscopeV4.Gui\Configuration\miniscope-config.schema.json` schema.
4. The main GUI workflow lives at `OpenEphys.MiniscopeV4.Gui\Workflows\MiniscopeGui.bonsai`.

## License

[MIT](LICENSE)
