using Bonsai;
using Hexa.NET.ImGui;
using System;
using System.ComponentModel;
using System.Numerics;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Xml.Serialization;

namespace OpenEphys.MiniscopeV4.Gui;

/// <summary>
/// Renders the ImGui status bar controls at the top of the GUI.
/// </summary>
[Combinator]
[Description("Renders the ImGui status bar controls at the top of the GUI.")]
public class StatusBar
{
    /// <summary>
    /// Gets or sets a value indicating whether a recording is currently in progress.
    /// </summary>
    [XmlIgnore]
    [Browsable(false)]
    public bool RecordingStatus { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether an automatic restart was triggered.
    /// </summary>
    /// <remarks>
    /// Automatic restarts are not guaranteed to reset the recording timer; this value
    /// can be set to force a reset of the recording timer.
    /// </remarks>
    [XmlIgnore]
    [Browsable(false)]
    public bool AutomaticRestartTriggered
    {
        get => Volatile.Read(ref automaticRestartTriggered) != 0;
        set => Volatile.Write(ref automaticRestartTriggered, value ? 1 : 0);
    }

    int automaticRestartTriggered;

    /// <summary>
    /// Consumes a pending automatic restart, if one was raised since the last call.
    /// </summary>
    /// <returns><see langword="true"/> if a restart was pending.</returns>
    bool ConsumeAutomaticRestart() => Interlocked.Exchange(ref automaticRestartTriggered, 0) != 0;

    /// <summary>
    /// Gets or sets the commutator settings used to control the commutator serial port.
    /// </summary>
    [XmlIgnore]
    [Browsable(false)]
    public CommutatorSettings CommutatorSettings { get; set; } = new();

    bool wasAcquiring;

    /// <summary>
    /// Gets or sets the camera configuration.
    /// </summary>
    [XmlIgnore]
    [Browsable(false)]
    public CameraStatus Configuration { get; set; } = new();

    /// <summary>
    /// Renders the status bar controls and returns an updated <see cref="CameraStatus"/> alongside each source value.
    /// </summary>
    /// <param name="source">
    /// A sequence pairing the shared <see cref="GuiLayout"/> with whether the Miniscope is actually
    /// acquiring frames, tied to the render tick of DearImGui.
    /// </param>
    /// <returns>A sequence of values paired with the status bar state updated from the rendered controls.</returns>
    public IObservable<Tuple<GuiLayout, CameraStatus>> Process(IObservable<Tuple<GuiLayout, bool>> source)
    {
        double elapsedAcquisitionTime = 0;

        return Observable.Create<Tuple<GuiLayout, CameraStatus>>(observer =>
        {
            DateTime? acquisitionStart = null;
            DateTime? recordingStart = null;

            var sourceObserver = Observer.Create<Tuple<GuiLayout, bool>>(value =>
            {
                var guiLayout = value.Item1;
                var acquiring = value.Item2;

                var cameraIndex = Configuration.CameraIndex;
                var acquisitionRequested = Configuration.AcquisitionRequested;
                var paused = Configuration.Paused;

                if (wasAcquiring && !acquiring)
                    acquisitionRequested = false;
                wasAcquiring = acquiring;

                if (!acquiring)
                    paused = false;

                if (ConsumeAutomaticRestart())
                {
                    recordingStart = null;
                }

                if (ImGui.BeginTable("##statusbar", 3))
                {
                    ImGui.TableNextColumn();

                    ImGui.AlignTextToFramePadding();
                    ImGui.Text("Index: ");
                    ImGui.SameLine();

                    bool indexLocked = acquisitionRequested || acquiring;

                    if (indexLocked)
                        ImGui.BeginDisabled();

                    ImGui.SetNextItemWidth(60f * UiScale.Current);
                    ImGui.InputInt("##statusbar_index", ref cameraIndex, 0, 0);
                    if (Tooltip.Begin(allowWhenDisabled: true))
                    {
                        Tooltip.AddLine(
                            "Index of the Miniscope to acquire from, in the order\n" +
                            "the cameras are detected (0 is the first camera).");
                        if (indexLocked)
                            Tooltip.Note("Unavailable while acquiring.");
                        Tooltip.End();
                    }

                    if (indexLocked)
                        ImGui.EndDisabled();

                    ImGui.SameLine();
                    bool disableAcquisitionButton = !acquisitionRequested
                        && CommutatorSettings.AutoConnect
                        && string.IsNullOrEmpty(CommutatorSettings.PortName);

                    string acquisitionLabel = acquisitionRequested
                        ? (acquiring ? "Stop Acquisition##statusbar_btn" : "Starting...##statusbar_btn")
                        : (acquiring ? "Stopping...##statusbar_btn" : "Start Acquisition##statusbar_btn");

                    var acqButtonSize = new Vector2(140f * UiScale.Current, 0f);
                    using (Palette.PushButtonColors(
                        acquisitionRequested ? Palette.Red : Palette.Green,
                        acquisitionRequested ? Palette.RedHovered : Palette.GreenHovered,
                        acquisitionRequested ? Palette.RedActive : Palette.GreenActive))
                    {
                        if (disableAcquisitionButton) ImGui.BeginDisabled();
                        if (ImGui.Button(acquisitionLabel, acqButtonSize))
                        {
                            acquisitionRequested = !acquisitionRequested;
                        }
                        if (disableAcquisitionButton) ImGui.EndDisabled();
                    }

                    if (Tooltip.Begin(allowWhenDisabled: true))
                    {
                        Tooltip.AddLine(acquisitionRequested
                        ? "Stop acquiring frames from the Miniscope."
                        : "Start acquiring frames from the Miniscope at the selected index.");

                        if (acquisitionRequested != acquiring)
                        {
                            Tooltip.Note(acquisitionRequested
                                ? "Waiting for the Miniscope to start. Click to cancel."
                                : "Waiting for the Miniscope to stop.");
                        }

                        if (disableAcquisitionButton)
                        {
                            Tooltip.Note(
                                "Unavailable while Auto Connect is selected but no commutators are\n" +
                                "found. Connect a commutator or uncheck Auto Connect to start acquisition.");
                        }
                        Tooltip.End();
                    }

                    ImGui.TableNextColumn();

                    if (ImGui.BeginTable("##status_timers", 2))
                    {
                        ImGui.TableNextColumn();

                        if (acquiring)
                        {
                            acquisitionStart ??= DateTime.Now;
                            elapsedAcquisitionTime = (DateTime.Now - acquisitionStart.Value).TotalSeconds;
                            ImGui.Text($"Acquiring: {elapsedAcquisitionTime:F0} s");
                        }
                        else if (acquisitionStart != null)
                        {
                            acquisitionStart = null;
                            ImGui.Text($"Stopped: {elapsedAcquisitionTime:F0} s");
                        } 
                        else
                        {
                            ImGui.Text($"Stopped: {elapsedAcquisitionTime:F0} s");
                        }    

                        ImGui.TableNextColumn();

                        if (RecordingStatus)
                        {
                            recordingStart ??= DateTime.Now;
                            ImGui.Text($"Recording: {(DateTime.Now - recordingStart.Value).TotalSeconds:F0} s");
                        }
                        else if (recordingStart != null)
                        {
                            recordingStart = null;
                        }

                        ImGui.EndTable();
                    }

                    ImGui.TableNextColumn();

                    if (!acquiring)
                        ImGui.BeginDisabled();

                    var pauseButtonSize = new Vector2(200f * UiScale.Current, 0f);
                    float avail = ImGui.GetContentRegionAvail().X;
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + avail - pauseButtonSize.X);
                    bool spacePressed = acquiring && !ImGui.GetIO().WantTextInput && ImGui.IsKeyPressed(ImGuiKey.Space);
                    using (Palette.PushButtonColors(
                        paused ? Palette.Yellow : Palette.Gray,
                        paused ? Palette.YellowHovered : Palette.GrayHovered,
                        paused ? Palette.YellowActive : Palette.GrayActive))
                    {
                        if (ImGui.Button(paused ? "Resume Display (Spacebar)##statusbar_pause" : "Freeze Display (Spacebar)##statusbar_pause", pauseButtonSize) || spacePressed)
                        {
                            paused = !paused;
                        }
                    }
                    if (Tooltip.Begin(allowWhenDisabled: true))
                    {
                        Tooltip.AddLine(paused
                            ? "Resume updating the live display and plots. Acquisition and recording are unaffected."
                            : "Freeze the live display and plots without stopping acquisition or recording.");
                        Tooltip.AddKeyboardShortcut("Spacebar");
                        if (!acquiring)
                            Tooltip.Note("Unavailable while acquisition is stopped.");
                        Tooltip.End();
                    }

                    if (!acquiring)
                        ImGui.EndDisabled();

                    ImGui.EndTable();
                }

                ImGui.Separator();

                var updatedCameraStatus = new CameraStatus
                {
                    CameraIndex = cameraIndex,
                    AcquisitionRequested = acquisitionRequested,
                    Paused = paused
                };

                Configuration = updatedCameraStatus;

                // NB: If the ImageExpanded was requested to be toggled last frame, respect that request here at the top of the current frame.
                if (guiLayout.ImageExpandedRequested != guiLayout.ImageExpanded)
                    guiLayout = guiLayout with { ImageExpanded = guiLayout.ImageExpandedRequested };

                observer.OnNext(Tuple.Create(guiLayout, updatedCameraStatus));
            },
            observer.OnError,
            observer.OnCompleted);

            return source.SubscribeSafe(sourceObserver);
        });
    }
}
