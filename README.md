# Open Ephys Miniscope GUI

A self-contained ImGui-based graphical interface for configuring and acquiring data from a UCLA
Miniscope V4 head-borne miniature microscope, including control of an Open Ephys commutator.

It wraps the acquisition node from
[`OpenEphys.Miniscope`](https://www.nuget.org/packages/OpenEphys.Miniscope) in a single-window GUI
(settings panel, live video/data display, file recording controls, and a status/console log).

- Documentation: the [Miniscope Software
  Guide](https://open-ephys.github.io/miniscope-docs/Software-Guide/index.html).
- API reference: <https://open-ephys.github.io/bonsai-miniscope-gui/>.

## Installing

### As a standalone application

Download the latest `MiniscopeV4Gui-Setup-*.exe` from the
[Releases](https://github.com/open-ephys/bonsai-miniscope-gui/releases) page and run it. The
installer:

- Installs to your user profile (no admin rights required).
- Downloads all dependencies (Bonsai and required packages) so no separate Bonsai install is needed.
- Adds Start Menu / Desktop shortcuts to launch the GUI.

### As a Bonsai package

Add the [`OpenEphys.Miniscope.Gui`](https://www.nuget.org/packages/OpenEphys.Miniscope.Gui) package
through Bonsai's package manager, then drop the `MiniscopeV4Gui.bonsai` workflow into the editor. See
[Custom Workflows](https://open-ephys.github.io/miniscope-docs/Software-Guide/custom-workflows.html)
in the Software Guide.

## License

[MIT](https://github.com/open-ephys/bonsai-miniscope-gui/blob/main/LICENSE)
