using System.Windows;
using System.Windows.Controls;
using FtlSaveEditor.Services;

namespace FtlSaveEditor.Views;

public partial class ShipEditor : UserControl
{
    private readonly SaveEditorState _state = SaveEditorState.Instance;
    private bool _loading = true;

    public ShipEditor()
    {
        InitializeComponent();
        LoadData();
        _loading = false;
        WireEvents();
    }

    private void LoadData()
    {
        var ship = _state.GameState?.PlayerShip;
        if (ship == null) return;

        ShipNameBox.Text = ship.ShipName;
        BlueprintIdBox.Text = ship.ShipBlueprintId;
        GfxBaseNameBox.Text = ship.ShipGfxBaseName;

        HullBox.Text = ship.HullAmt.ToString();
        FuelBox.Text = ship.FuelAmt.ToString();
        DronePartsBox.Text = ship.DronePartsAmt.ToString();
        MissilesBox.Text = ship.MissilesAmt.ToString();
        ScrapBox.Text = ship.ScrapAmt.ToString();
        ReservePowerBox.Text = ship.ReservePowerCapacity.ToString();

        HostileCheck.IsChecked = ship.Hostile;
        JumpingCheck.IsChecked = ship.Jumping;
        JumpChargeBox.Text = ship.JumpChargeTicks.ToString();
        JumpAnimBox.Text = ship.JumpAnimTicks.ToString();
        CloakAnimBox.Text = ship.CloakAnimTicks.ToString();
    }

    private void WireEvents()
    {
        ShipNameBox.TextChanged += (_, _) =>
        {
            if (_loading) return;
            _state.GameState!.PlayerShip.ShipName = ShipNameBox.Text;
            _state.MarkDirty();
        };

        BlueprintIdBox.TextChanged += (_, _) =>
        {
            if (_loading) return;
            _state.GameState!.PlayerShip.ShipBlueprintId = BlueprintIdBox.Text;
            _state.MarkDirty();
        };

        GfxBaseNameBox.TextChanged += (_, _) =>
        {
            if (_loading) return;
            _state.GameState!.PlayerShip.ShipGfxBaseName = GfxBaseNameBox.Text;
            _state.MarkDirty();
        };

        HullBox.LostFocus += (_, _) => SetInt(HullBox, v => _state.GameState!.PlayerShip.HullAmt = v);
        FuelBox.LostFocus += (_, _) => SetInt(FuelBox, v => _state.GameState!.PlayerShip.FuelAmt = v);
        DronePartsBox.LostFocus += (_, _) => SetInt(DronePartsBox, v => _state.GameState!.PlayerShip.DronePartsAmt = v);
        MissilesBox.LostFocus += (_, _) => SetInt(MissilesBox, v => _state.GameState!.PlayerShip.MissilesAmt = v);
        ScrapBox.LostFocus += (_, _) => SetInt(ScrapBox, v => _state.GameState!.PlayerShip.ScrapAmt = v);
        ReservePowerBox.LostFocus += (_, _) => SetInt(ReservePowerBox, v => _state.GameState!.PlayerShip.ReservePowerCapacity = v);

        HostileCheck.Checked += (_, _) => { if (!_loading) { _state.GameState!.PlayerShip.Hostile = true; _state.MarkDirty(); } };
        HostileCheck.Unchecked += (_, _) => { if (!_loading) { _state.GameState!.PlayerShip.Hostile = false; _state.MarkDirty(); } };

        JumpingCheck.Checked += (_, _) => { if (!_loading) { _state.GameState!.PlayerShip.Jumping = true; _state.MarkDirty(); } };
        JumpingCheck.Unchecked += (_, _) => { if (!_loading) { _state.GameState!.PlayerShip.Jumping = false; _state.MarkDirty(); } };

        JumpChargeBox.LostFocus += (_, _) => SetInt(JumpChargeBox, v => _state.GameState!.PlayerShip.JumpChargeTicks = v);
        JumpAnimBox.LostFocus += (_, _) => SetInt(JumpAnimBox, v => _state.GameState!.PlayerShip.JumpAnimTicks = v);
        CloakAnimBox.LostFocus += (_, _) => SetInt(CloakAnimBox, v => _state.GameState!.PlayerShip.CloakAnimTicks = v);
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
}
