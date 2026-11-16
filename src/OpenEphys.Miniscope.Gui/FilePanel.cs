using Bonsai;
using Bonsai.IO;
using Hexa.NET.ImGui;
using OpenEphys.Miniscope;
using System;
using System.ComponentModel;
using System.IO;
using System.Numerics;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace OpenEphys.Miniscope.Gui;

/// <summary>
/// Renders the "Recording" controls (file saving and recording), anchored to the bottom of the settings sidebar.
/// </summary>
/// <remarks>
/// Renders into the shared sidebar child window opened (but not closed) by <see cref="SettingsPanel"/>,
/// and closes it once its own content is done, so the two panels form a single visual region. Content
/// is skipped (but the child is still closed) while <see cref="GuiLayout.SidebarOpen"/> is false, so
/// collapsing the sidebar hides this section along with the rest of the settings. Its own content renders
/// into an auto-sized child so <see cref="GuiLayout.RecordingSectionHeight"/> can be measured;
/// <see cref="SettingsPanel"/> uses that (one frame stale, since the height is otherwise unknown until it
/// renders) to bound its own collapsible content and keep this section anchored to a fixed distance from
/// the bottom. The two panels coordinate through the threaded <see cref="GuiLayout"/>.
/// </remarks>
[Combinator]
[Description("Renders the recording and file saving controls.")]
public class FilePanel
{
    /// <summary>
    /// Gets or sets a value indicating whether the Miniscope is currently acquiring frames.
    /// </summary>
    [XmlIgnore]
    [Browsable(false)]
    public bool AcquisitionStatus { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a segmented run has reached its total duration.
    /// </summary>
    [XmlIgnore]
    [Browsable(false)]
    public bool IsTotalDurationFinished { get; set; }

    /// <summary>
    /// Gets or sets the most recent recording error message.
    /// </summary>
    [XmlIgnore]
    [Browsable(false)]
    public string RecordingError { get; set; }

    /// <summary>
    /// Gets or sets the file settings.
    /// </summary>
    [XmlIgnore]
    [Browsable(false)]
    public FileSettings FileSettings { get; set; } = new();

    static readonly string[] DigitalInNames = Enum.GetNames(typeof(MiniscopeDaqDigitalIn));
    static readonly MiniscopeDaqDigitalIn[] DigitalInValues = (MiniscopeDaqDigitalIn[])Enum.GetValues(typeof(MiniscopeDaqDigitalIn));
    static readonly string[] PathSuffixNames = Enum.GetNames(typeof(PathSuffix));
    static readonly PathSuffix[] PathSuffixValues = (PathSuffix[])Enum.GetValues(typeof(PathSuffix));

    static readonly string RecordButtonLabelText = " (Ctrl+R)##record_button";

    /// <summary>
    /// Renders the file saving and recording controls and returns an updated <see cref="Gui.FileSettings"/> alongside the shared layout.
    /// </summary>
    /// <param name="source">
    /// A sequence pairing the shared <see cref="GuiLayout"/> with whether a file is actually being
    /// written, tied to the render tick of DearImGui.
    /// </param>
    /// <returns>A sequence pairing the updated <see cref="GuiLayout"/> with the file settings updated from the rendered controls.</returns>
    public IObservable<Tuple<GuiLayout, FileSettings>> Process(IObservable<Tuple<GuiLayout, bool>> source)
    {
        return Observable.Create<Tuple<GuiLayout, FileSettings>>(observer =>
        {
            bool wasRecording = false;
            bool wasAcquiring = false;
            string lastRecordingError = string.Empty;

            const nuint bufSize = 1024;
            string fileName = string.Empty;
            Task<string> saveDialogTask = null;
            bool shouldStartRecordingWhenCompleted = false;

            DateTime? recordingStart = null;

            var sourceObserver = Observer.Create<Tuple<GuiLayout, bool>>(value =>
            {
                var layout = value.Item1;
                var recording = value.Item2;

                var recordingMode = FileSettings.RecordingMode;
                bool recordingRequested = FileSettings.RecordingRequested;
                fileName = FileSettings.FileName;
                PathSuffix suffix = FileSettings.Suffix;
                int recordingDurationSeconds = FileSettings.RecordingDuration;
                int totalDurationSeconds = FileSettings.TotalDuration;
                var segmentMode = FileSettings.SegmentMode;
                bool isCompressed = FileSettings.CompressVideo;
                var triggerInput = FileSettings.TriggerInput;
                int triggerIndex = Array.IndexOf(DigitalInValues, triggerInput);

                bool runFinished = segmentMode switch
                {
                    SegmentMode.AutoRestart => false,
                    SegmentMode.MultipleFiles => IsTotalDurationFinished,
                    _ => recordingMode != RecordingMode.Trigger,
                };

                if ((wasRecording && !recording && runFinished) || (wasAcquiring && !AcquisitionStatus))
                    recordingRequested = false;

                if (!ReferenceEquals(RecordingError, lastRecordingError))
                {
                    lastRecordingError = RecordingError;
                    if (!string.IsNullOrEmpty(RecordingError))
                        recordingRequested = false;
                }

                wasRecording = recording;
                wasAcquiring = AcquisitionStatus;

                bool recordButtonEnabled = AcquisitionStatus;

                if (saveDialogTask != null && saveDialogTask.IsCompleted)
                {
                    var result = saveDialogTask.Result;
                    if (!string.IsNullOrEmpty(result))
                    {
                        fileName = Path.ChangeExtension(result, null);
                    }
                    saveDialogTask = null;

                    if (shouldStartRecordingWhenCompleted && !string.IsNullOrEmpty(fileName))
                    {
                        recordingRequested = true;
                        shouldStartRecordingWhenCompleted = false;
                    }
                }

                void RecordButtonPressed()
                {
                    if (string.IsNullOrEmpty(fileName))
                    {
                        if (saveDialogTask == null || saveDialogTask.IsCompleted)
                        {
                            shouldStartRecordingWhenCompleted = true;
                            saveDialogTask = CreateSaveFileDialogTask(fileName);
                        }
                    }
                    else
                    {
                        recordingRequested = !recordingRequested;
                    }
                }

                if (recordButtonEnabled && !ImGui.GetIO().WantTextInput && ImGui.GetIO().KeyCtrl && ImGui.IsKeyPressed(ImGuiKey.R))
                    RecordButtonPressed();

                if (triggerIndex < 1) triggerIndex = 1;

                if (recordingRequested)
                {
                    recordingStart ??= DateTime.Now;
                }
                else if (recordingStart != null)
                {
                    recordingStart = null;
                }

                if (!layout.ImageExpanded && layout.SidebarOpen)
                {
                    ImGui.BeginChild("##file_pane", new Vector2(-1f, 0f), ImGuiChildFlags.AutoResizeY);

                    ImGui.Separator();
                    ImGui.Text("Recording");
                    ImGui.Dummy(new Vector2(0f, ImGui.GetStyle().ItemSpacing.Y));

                    ImGui.Text("Data Path");

                    const string selectLabel = "...";
                    const string browseLabel = "Browse";
                    var (selectWidth, browseWidth, inputWidth) = CalculateFileNameInputWidth(selectLabel, browseLabel);
                    string unavailableWhile = recordingMode == RecordingMode.Trigger ? "armed" : "recording";

                    if (recordingRequested) ImGui.BeginDisabled();

                    ImGui.SetNextItemWidth(inputWidth);
                    ImGui.InputText("##filename", ref fileName, bufSize, ImGuiInputTextFlags.ElideLeft);
                    RecordModeTooltip(
                        "The data path used to save all files: a folder plus a base filename.\n" +
                        "The selected suffix is inserted after the base filename and before the extension.\n" +
                        $"Video files get '{GenerateRecordingFileNames.ImageExtension}', data files get '{GenerateRecordingFileNames.CsvExtension}', logs get '{GenerateRecordingFileNames.LogExtension}', configuration files get '{GenerateRecordingFileNames.ConfigExtension}' appended automatically.",
                        recordingRequested, unavailableWhile);
                    ImGui.SameLine();
                    if (ImGui.Button($"{selectLabel}##choose_filename_button", new Vector2(selectWidth, 0)))
                    {
                        if (saveDialogTask == null || saveDialogTask.IsCompleted)
                        {
                            saveDialogTask = CreateSaveFileDialogTask(fileName);
                        }
                    }
                    RecordModeTooltip(
                        "Specify a save location and base filename for all data\n" +
                        " produced during acquisition (e.g., video, IMU, console log).",
                        recordingRequested, unavailableWhile);

                    ImGui.SameLine();

                    if (recordingRequested) ImGui.EndDisabled();

                    if (ImGui.Button($"{browseLabel}##open_folder_button", new Vector2(browseWidth, 0)))
                    {
                        var dir = FileDialogHelpers.GetDirectory(fileName);
                        if (Directory.Exists(dir))
                            System.Diagnostics.Process.Start("explorer.exe", dir);
                    }
                    Tooltip.Describe("Open the data folder in File Explorer to browse for previously saved data files.");

                    if (recordingRequested) ImGui.BeginDisabled();

                    if (ImGui.BeginTable("##writer_parameters", 2, ImGuiTableFlags.SizingStretchSame))
                    {
                        ImGui.TableNextColumn();
                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("Suffix:");
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(-1f);

                        int suffixIndex = Array.IndexOf(PathSuffixValues, suffix);
                        if (ImGui.BeginCombo("##path_suffix", PathSuffixNames[suffixIndex]))
                        {
                            foreach (var val in PathSuffixValues)
                            {
                                if (val == PathSuffix.None) continue;

                                bool selected = suffix == val;
                                if (ImGui.Selectable(val.ToString(), selected))
                                    suffix = val;

                                if (selected)
                                    ImGui.SetItemDefaultFocus();
                            }
                            ImGui.EndCombo();
                        }

                        RecordModeTooltip(
                            "Text appended to each filename to keep successive recordings unique:\n" +
                            "- FileCount adds an incrementing number.\n" +
                            "- Timestamp adds the recording's date and time.",
                            recordingRequested, unavailableWhile);

                        ImGui.TableNextColumn();
                        ImGui.SetNextItemWidth(-1f);
                        ImGui.Checkbox("Compress Video##compress_video", ref isCompressed);
                        RecordModeTooltip(
                            "Encode the saved video with compression to reduce file size, at the\n" +
                            "cost of higher CPU usage during recording." +
                            "Videos are saved with the 'Y800' codec when compression is disabled,\n" +
                            "or the 'MJPG' codec when compression is enabled.",
                            recordingRequested, unavailableWhile);
                        ImGui.EndTable();
                    }

                    ImGui.Separator();

                    ImGui.AlignTextToFramePadding();
                    ImGui.Text("Mode: ");
                    ImGui.SameLine();
                    if (ImGui.RadioButton("Manual##record_mode_manual", recordingMode == RecordingMode.Manual))
                    {
                        recordingMode = RecordingMode.Manual;
                    }
                    RecordModeTooltip("Start and stop recording manually with the Record button.", recordingRequested, unavailableWhile);
                    ImGui.SameLine();
                    if (ImGui.RadioButton("Segmented##record_mode_segmented", recordingMode == RecordingMode.Segmented))
                    {
                        recordingMode = RecordingMode.Segmented;
                    }
                    RecordModeTooltip("Record data in segments of a fixed duration.", recordingRequested, unavailableWhile);
                    ImGui.SameLine();
                    if (ImGui.RadioButton("Trigger##record_mode_trigger", recordingMode == RecordingMode.Trigger))
                    {
                        recordingMode = RecordingMode.Trigger;
                    }
                    RecordModeTooltip(
                        "Arm recording so a digital input controls recording.\n" +
                        "While the selected digital input is high, data is recorded.", recordingRequested, unavailableWhile);
                    if (recordingRequested) ImGui.EndDisabled();

                    var recordingSettingsHeight = ImGui.GetFrameHeightWithSpacing() * 3 + ImGui.GetStyle().ItemSpacing.Y * 2;

                    if (ImGui.BeginChild("##recording_settings", new Vector2(-1, recordingSettingsHeight), ImGuiChildFlags.None))
                    {
                        if (recordingMode == RecordingMode.Segmented)
                        {
                            if (recordingRequested) ImGui.BeginDisabled();

                            if (ImGui.BeginTable("##record_duration_table", 2))
                            {
                                ImGui.TableNextColumn();
                                ImGui.AlignTextToFramePadding();
                                ImGui.Text("Duration [s]:");
                                ImGui.SameLine();
                                ImGui.SetNextItemWidth(-1f);
                                if (ImGui.InputInt("##recording_duration", ref recordingDurationSeconds, 0, 0, ImGuiInputTextFlags.AutoSelectAll))
                                {
                                    recordingDurationSeconds = Math.Max(1, recordingDurationSeconds);
                                }
                                Tooltip.Describe("Length of each recording file, in seconds.");

                                ImGui.EndTable();
                            }

                            if (ImGui.RadioButton("Single File##segment_mode_single_file", segmentMode == SegmentMode.SingleFile))
                            {
                                segmentMode = SegmentMode.SingleFile;
                            }
                            Tooltip.Describe("Record to a single file until the duration specified above is reached.");
                            ImGui.SameLine();
                            if (ImGui.RadioButton("Multiple Files##segment_mode_total_duration", segmentMode == SegmentMode.MultipleFiles))
                            {
                                segmentMode = SegmentMode.MultipleFiles;
                            }
                            Tooltip.Describe(
                                "Split a long recording into successive files of the duration specified above,\n" +
                                "stopping once the total recording time specified below is reached.");
                            ImGui.SameLine();
                            if (ImGui.RadioButton("Auto Restart##segment_mode_auto_restart", segmentMode == SegmentMode.AutoRestart))
                            {
                                segmentMode = SegmentMode.AutoRestart;
                            }
                            Tooltip.Describe(
                                "Automatically start a new recording each time the duration specified above\n" +
                                "elapses, until you press Stop Recording.");

                            if (segmentMode == SegmentMode.MultipleFiles)
                            {
                                if (ImGui.BeginTable("##total_duration_table", 2))
                                {
                                    ImGui.TableNextColumn();
                                    ImGui.AlignTextToFramePadding();
                                    ImGui.Text("Total [s]:");
                                    ImGui.SameLine();
                                    ImGui.SetNextItemWidth(-1f);
                                    if (ImGui.InputInt("##total_duration", ref totalDurationSeconds, 0, 0, ImGuiInputTextFlags.AutoSelectAll))
                                    {
                                        totalDurationSeconds = Math.Max(1, totalDurationSeconds);
                                    }
                                    Tooltip.Describe("Total recording time across all files, in seconds.");

                                    ImGui.TableNextColumn();
                                    if (recordingDurationSeconds > 0)
                                    {
                                        ImGui.AlignTextToFramePadding();
                                        int filesCount = (int)Math.Ceiling((double)totalDurationSeconds / recordingDurationSeconds);
                                        var endTime = (recordingStart ?? DateTime.Now) + TimeSpan.FromSeconds(totalDurationSeconds);
                                        ImGui.Text($"{filesCount} file{(filesCount == 1 ? "" : "s")} · ends {endTime:HH:mm:ss}");
                                    }

                                    ImGui.EndTable();
                                }
                            }

                            if (recordingRequested) ImGui.EndDisabled();
                        }
                        else if (recordingMode == RecordingMode.Trigger)
                        {
                            ImGui.AlignTextToFramePadding();
                            ImGui.Text("Digital Input: ");
                            ImGui.SameLine();
                            ImGui.SetNextItemWidth(-1f);
                            if (recordingRequested) ImGui.BeginDisabled();
                            if (ImGui.BeginCombo("##trigger_input", DigitalInNames[triggerIndex]))
                            {
                                foreach (var val in DigitalInValues)
                                {
                                    if (val == MiniscopeDaqDigitalIn.None) continue;

                                    bool selected = triggerInput == val;
                                    if (ImGui.Selectable(val.ToString(), selected))
                                        triggerInput = val;

                                    if (selected)
                                        ImGui.SetItemDefaultFocus();
                                }
                                ImGui.EndCombo();
                            }
                            if (Tooltip.Begin(allowWhenDisabled: true))
                            {
                                Tooltip.AddLine("Digital input that triggers recording: recording runs only while it is high.");
                                if (recordingRequested) Tooltip.Note("Unavailable while armed.");
                                Tooltip.End();
                            }
                            if (recordingRequested) ImGui.EndDisabled();
                        }
                    }

                    ImGui.EndChild();

                    using (Palette.PushButtonColors(
                            recordingRequested ? Palette.Red : Palette.Green,
                            recordingRequested ? Palette.RedHovered : Palette.GreenHovered,
                            recordingRequested ? Palette.RedActive : Palette.GreenActive))
                    {
                        Vector2 recordingRequestedSize = new(-1f, ImGui.GetFrameHeight() * 2);
                        if (!recordButtonEnabled) ImGui.BeginDisabled();

                        string recordLabel = "", tooltipLine = "";

                        if (recordingMode == RecordingMode.Manual || recordingMode == RecordingMode.Segmented)
                        {
                            if (recordingRequested)
                            {
                                recordLabel = "Stop Recording" + RecordButtonLabelText;
                                tooltipLine = "Stop the current recording.";
                            }
                            else
                            {
                                recordLabel = "Record" + RecordButtonLabelText;
                                tooltipLine = "Start recording to the data path.";
                            }
                        }
                        else if (recordingMode == RecordingMode.Trigger)
                        {
                            if (recordingRequested)
                            {
                                recordLabel = "Disarm" + RecordButtonLabelText;
                                tooltipLine = "Disarm recording.";
                            }
                            else
                            {
                                recordLabel = "Arm Recording" + RecordButtonLabelText;
                                tooltipLine = "Arm recording so the selected digital input can control it.";
                            }
                        }

                        if (ImGui.Button(recordLabel, recordingRequestedSize))
                        {
                            RecordButtonPressed();
                        }

                        if (Tooltip.Begin(allowWhenDisabled: true))
                        {
                            Tooltip.AddLine(tooltipLine);
                            Tooltip.AddKeyboardShortcut("Ctrl+R");
                            if (!recordButtonEnabled)
                                Tooltip.Note("Unavailable while acquisition is stopped.");
                            Tooltip.End();
                        }

                        if (!recordButtonEnabled) ImGui.EndDisabled();
                    }

                    ImGui.EndChild();
                    layout = layout with { RecordingSectionHeight = ImGui.GetItemRectSize().Y + ImGui.GetStyle().ItemSpacing.Y };
                }

                if (!layout.ImageExpanded)
                    ImGui.EndChild(); // closes the shared sidebar child opened by SettingsPanel

                var updatedFileSettings = new FileSettings
                {
                    RecordingRequested = recordingRequested,
                    RecordingMode = recordingMode,
                    CompressVideo = isCompressed,
                    FileName = fileName,
                    Suffix = suffix,
                    RecordingDuration = recordingDurationSeconds,
                    TotalDuration = totalDurationSeconds,
                    SegmentMode = segmentMode,
                    TriggerInput = triggerInput,
                };

                FileSettings = updatedFileSettings;

                observer.OnNext(Tuple.Create(layout, updatedFileSettings));
            },
            observer.OnError,
            observer.OnCompleted);

            return source.SubscribeSafe(sourceObserver);
        });
    }

    static void RecordModeTooltip(string description, bool disabled, string status)
    {
        if (Tooltip.Begin(allowWhenDisabled: true))
        {
            Tooltip.AddLine(description);
            if (disabled)
                Tooltip.Note($"Unavailable while {status}.");
            Tooltip.End();
        }
    }

    static Task<string> CreateSaveFileDialogTask(string fileName) => FileDialogHelpers.RunDialogTask(() => new SaveFileDialog
    {
        InitialDirectory = FileDialogHelpers.GetDirectory(fileName),
        Filter = "All Files|*.*",
        Title = "Choose a filename template and a folder to save Miniscope data.",
        AddExtension = false,
        CheckFileExists = false,
        OverwritePrompt = false,
        CheckPathExists = false,
        FileName = Path.GetFileName(fileName)
    },
    (dlg) => (dlg as SaveFileDialog).FileName);

    internal static (float selectWidth, float browseWidth, float inputWidth) CalculateFileNameInputWidth(string selectLabel, string browseLabel)
    {
        float selectWidth = ImGui.CalcTextSize(selectLabel).X + ImGui.GetStyle().FramePadding.X * 2f;
        float browseWidth = ImGui.CalcTextSize(browseLabel).X + ImGui.GetStyle().FramePadding.X * 2f;
        return (selectWidth,
            browseWidth,
            ImGui.GetContentRegionAvail().X - selectWidth - browseWidth - ImGui.GetStyle().ItemSpacing.X * 2f);
    }
}
