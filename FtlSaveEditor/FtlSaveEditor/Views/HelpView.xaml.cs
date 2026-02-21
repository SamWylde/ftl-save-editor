using System.Windows.Controls;
using FtlSaveEditor.Models;
using FtlSaveEditor.Services;

namespace FtlSaveEditor.Views;

public partial class HelpView : UserControl
{
    public HelpView()
    {
        InitializeComponent();
        UpdateCurrentMode();
    }

    private void UpdateCurrentMode()
    {
        var state = SaveEditorState.Instance;
        if (state.GameState == null)
        {
            CurrentModeText.Text = "No file loaded.";
            return;
        }

        var gs = state.GameState;
        var modeText = gs.ParseMode switch
        {
            SaveParseMode.Full =>
                "Full mode — all sections are editable. This is a vanilla FTL save.",
            SaveParseMode.PartialPlayerShipOpaqueTail =>
                "Partial mode — Hyperspace/Multiverse save detected. Ship, crew, weapons, drones, augments, state variables, and metadata are editable. " +
                "Systems, cargo, beacons, and environment are preserved as-is.",
            SaveParseMode.RestrictedOpaqueTail =>
                "Restricted mode — only metadata and state variables are editable. The save format could not be fully parsed.",
            _ => $"Unknown mode ({gs.ParseMode})"
        };

        var formatStr = gs.FileFormat switch
        {
            2 => "Format 2",
            7 => "Format 7",
            8 => "Format 8",
            9 => "Format 9 (AE)",
            11 => "Format 11 (HS/MV)",
            _ => $"Format {gs.FileFormat}"
        };

        CurrentModeText.Text = $"{formatStr} — {modeText}";
    }
}
