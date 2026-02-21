using System.Windows.Controls;
using FtlSaveEditor.Services;

namespace FtlSaveEditor.Views;

public partial class MetadataEditor : UserControl
{
    private readonly SaveEditorState _state = SaveEditorState.Instance;
    private bool _loading = true;

    public MetadataEditor()
    {
        InitializeComponent();
        LoadData();
        _loading = false;
        WireEvents();
    }

    private void LoadData()
    {
        var gs = _state.GameState;
        if (gs == null)
        {
            return;
        }

        FileFormatText.Text = gs.FileFormat.ToString();
        PlayerShipNameBox.Text = gs.PlayerShipName;
        PlayerShipBlueprintIdBox.Text = gs.PlayerShipBlueprintId;
        DifficultyBox.Text = gs.Difficulty.ToString();
        TotalShipsDefeatedBox.Text = gs.TotalShipsDefeated.ToString();
        TotalBeaconsExploredBox.Text = gs.TotalBeaconsExplored.ToString();
        TotalScrapCollectedBox.Text = gs.TotalScrapCollected.ToString();
        TotalCrewHiredBox.Text = gs.TotalCrewHired.ToString();
        OneBasedSectorNumberBox.Text = gs.OneBasedSectorNumber.ToString();
        UnknownBetaBox.Text = gs.UnknownBeta.ToString();
        DlcEnabledCheck.IsChecked = gs.DlcEnabled;
        RandomNativeCheck.IsChecked = gs.RandomNative;
    }

    private void WireEvents()
    {
        PlayerShipNameBox.TextChanged += (_, _) =>
        {
            if (_loading || _state.GameState == null) return;
            _state.GameState.PlayerShipName = PlayerShipNameBox.Text;
            _state.MarkDirty();
        };

        PlayerShipBlueprintIdBox.TextChanged += (_, _) =>
        {
            if (_loading || _state.GameState == null) return;
            _state.GameState.PlayerShipBlueprintId = PlayerShipBlueprintIdBox.Text;
            _state.MarkDirty();
        };

        DifficultyBox.LostFocus += (_, _) => SetInt(DifficultyBox, v => _state.GameState!.Difficulty = v);
        TotalShipsDefeatedBox.LostFocus += (_, _) => SetInt(TotalShipsDefeatedBox, v => _state.GameState!.TotalShipsDefeated = v);
        TotalBeaconsExploredBox.LostFocus += (_, _) => SetInt(TotalBeaconsExploredBox, v => _state.GameState!.TotalBeaconsExplored = v);
        TotalScrapCollectedBox.LostFocus += (_, _) => SetInt(TotalScrapCollectedBox, v => _state.GameState!.TotalScrapCollected = v);
        TotalCrewHiredBox.LostFocus += (_, _) => SetInt(TotalCrewHiredBox, v => _state.GameState!.TotalCrewHired = v);
        OneBasedSectorNumberBox.LostFocus += (_, _) => SetInt(OneBasedSectorNumberBox, v => _state.GameState!.OneBasedSectorNumber = v);
        UnknownBetaBox.LostFocus += (_, _) => SetInt(UnknownBetaBox, v => _state.GameState!.UnknownBeta = v);

        DlcEnabledCheck.Checked += (_, _) => SetBool(v => _state.GameState!.DlcEnabled = v, true);
        DlcEnabledCheck.Unchecked += (_, _) => SetBool(v => _state.GameState!.DlcEnabled = v, false);
        RandomNativeCheck.Checked += (_, _) => SetBool(v => _state.GameState!.RandomNative = v, true);
        RandomNativeCheck.Unchecked += (_, _) => SetBool(v => _state.GameState!.RandomNative = v, false);
    }

    private void SetInt(TextBox box, Action<int> setter)
    {
        if (_loading) return;
        if (int.TryParse(box.Text, out int val))
        {
            setter(val);
            _state.MarkDirty();
        }
    }

    private void SetBool(Action<bool> setter, bool value)
    {
        if (_loading) return;
        setter(value);
        _state.MarkDirty();
    }
}
