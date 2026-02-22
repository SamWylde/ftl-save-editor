using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FtlSaveEditor.Models;
using FtlSaveEditor.Services;

namespace FtlSaveEditor.Views;

public partial class SystemsEditor : UserControl
{
    private readonly SaveEditorState _state = SaveEditorState.Instance;

    public SystemsEditor()
    {
        InitializeComponent();
        LoadData();
    }

    private void LoadData()
    {
        var ship = _state.GameState?.PlayerShip;
        if (ship == null) return;

        SystemsListPanel.Children.Clear();

        for (int i = 0; i < ship.Systems.Count; i++)
        {
            var sys = ship.Systems[i];
            bool isEven = i % 2 == 0;
            SystemsListPanel.Children.Add(BuildSystemRow(sys, isEven));
        }

        BuildSystemExtras(ship);
    }

    private Border BuildSystemRow(SystemState sys, bool isEven)
    {
        var bgBrush = isEven
            ? (SolidColorBrush)FindResource("BgMediumBrush")
            : (SolidColorBrush)FindResource("BgDarkBrush");

        var row = new Border
        {
            Background = bgBrush,
            BorderBrush = (SolidColorBrush)FindResource("BorderBrush"),
            BorderThickness = new Thickness(1, 0, 1, 1),
            Padding = new Thickness(12, 6, 12, 6)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });

        // System name
        string displayName = SystemTypeHelper.DisplayNames.TryGetValue(sys.SystemType, out var name)
            ? name : sys.SystemType.ToString();

        var nameTb = new TextBlock
        {
            Text = displayName,
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = FontWeights.SemiBold,
            Foreground = (SolidColorBrush)FindResource("TextPrimaryBrush")
        };
        Grid.SetColumn(nameTb, 0);
        grid.Children.Add(nameTb);

        // Capacity
        var capBox = CreateIntBox(sys.Capacity);
        capBox.LostFocus += (_, _) =>
        {
            if (int.TryParse(capBox.Text, out int v)) { sys.Capacity = v; _state.MarkDirty(); }
        };
        Grid.SetColumn(capBox, 1);
        grid.Children.Add(capBox);

        // Power
        var powerBox = CreateIntBox(sys.Power);
        powerBox.LostFocus += (_, _) =>
        {
            if (int.TryParse(powerBox.Text, out int v)) { sys.Power = v; _state.MarkDirty(); }
        };
        Grid.SetColumn(powerBox, 2);
        grid.Children.Add(powerBox);

        // Damaged Bars
        var dmgBox = CreateIntBox(sys.DamagedBars);
        dmgBox.LostFocus += (_, _) =>
        {
            if (int.TryParse(dmgBox.Text, out int v)) { sys.DamagedBars = v; _state.MarkDirty(); }
        };
        Grid.SetColumn(dmgBox, 3);
        grid.Children.Add(dmgBox);

        // Ionized Bars
        var ionBox = CreateIntBox(sys.IonizedBars);
        ionBox.LostFocus += (_, _) =>
        {
            if (int.TryParse(ionBox.Text, out int v)) { sys.IonizedBars = v; _state.MarkDirty(); }
        };
        Grid.SetColumn(ionBox, 4);
        grid.Children.Add(ionBox);

        // Hack Level
        var hackLvlBox = CreateIntBox(sys.HackLevel);
        hackLvlBox.LostFocus += (_, _) =>
        {
            if (int.TryParse(hackLvlBox.Text, out int v)) { sys.HackLevel = v; _state.MarkDirty(); }
        };
        Grid.SetColumn(hackLvlBox, 5);
        grid.Children.Add(hackLvlBox);

        // Hacked
        var hackedChk = new CheckBox
        {
            IsChecked = sys.Hacked,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        hackedChk.Checked += (_, _) => { sys.Hacked = true; _state.MarkDirty(); };
        hackedChk.Unchecked += (_, _) => { sys.Hacked = false; _state.MarkDirty(); };
        Grid.SetColumn(hackedChk, 6);
        grid.Children.Add(hackedChk);

        row.Child = grid;
        return row;
    }

    private void BuildSystemExtras(ShipState ship)
    {
        SystemExtrasPanel.Children.Clear();

        if (ship.ClonebayInfo != null)
            SystemExtrasPanel.Children.Add(BuildClonebayCard(ship.ClonebayInfo));
        if (ship.BatteryInfo != null)
            SystemExtrasPanel.Children.Add(BuildBatteryCard(ship.BatteryInfo));
        if (ship.ShieldsInfo != null)
            SystemExtrasPanel.Children.Add(BuildShieldsCard(ship.ShieldsInfo));
        if (ship.CloakingInfo != null)
            SystemExtrasPanel.Children.Add(BuildCloakingCard(ship.CloakingInfo));
    }

    private Border BuildSubStateCard(string title, string colorKey, UIElement content)
    {
        var border = new Border
        {
            Background = (SolidColorBrush)FindResource("BgMediumBrush"),
            BorderBrush = (SolidColorBrush)FindResource("BorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 12)
        };

        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = (SolidColorBrush)FindResource(colorKey),
            Margin = new Thickness(0, 0, 0, 12)
        });
        stack.Children.Add(content);
        border.Child = stack;
        return border;
    }

    private Border BuildClonebayCard(ClonebayInfo cb)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });

        int row = 0;
        AddIntFieldToGrid(grid, ref row, 0, "Build Ticks", cb.BuildTicks, v => cb.BuildTicks = v);
        AddIntFieldToGrid(grid, ref row, 0, "Build Ticks Goal", cb.BuildTicksGoal, v => cb.BuildTicksGoal = v);

        row = 0;
        AddIntFieldToGrid(grid, ref row, 3, "Doom Ticks", cb.DoomTicks, v => cb.DoomTicks = v);

        return BuildSubStateCard("CLONEBAY STATE", "AccentGreenBrush", grid);
    }

    private Border BuildBatteryCard(BatteryInfo bat)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });

        int row = 0;
        AddCheckFieldToGrid(grid, ref row, 0, "Active", bat.Active, v => bat.Active = v);
        AddIntFieldToGrid(grid, ref row, 0, "Used Battery", bat.UsedBattery, v => bat.UsedBattery = v);

        row = 0;
        AddIntFieldToGrid(grid, ref row, 3, "Discharge Ticks", bat.DischargeTicks, v => bat.DischargeTicks = v);

        return BuildSubStateCard("BATTERY STATE", "AccentOrangeBrush", grid);
    }

    private Border BuildShieldsCard(ShieldsInfo sh)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });

        int row = 0;
        AddIntFieldToGrid(grid, ref row, 0, "Shield Layers", sh.ShieldLayers, v => sh.ShieldLayers = v);
        AddIntFieldToGrid(grid, ref row, 0, "Energy Shield Layers", sh.EnergyShieldLayers, v => sh.EnergyShieldLayers = v);
        AddIntFieldToGrid(grid, ref row, 0, "Energy Shield Max", sh.EnergyShieldMax, v => sh.EnergyShieldMax = v);
        AddIntFieldToGrid(grid, ref row, 0, "Recharge Ticks", sh.ShieldRechargeTicks, v => sh.ShieldRechargeTicks = v);

        row = 0;
        AddCheckFieldToGrid(grid, ref row, 3, "Drop Anim On", sh.ShieldDropAnimOn, v => sh.ShieldDropAnimOn = v);
        AddIntFieldToGrid(grid, ref row, 3, "Drop Anim Ticks", sh.ShieldDropAnimTicks, v => sh.ShieldDropAnimTicks = v);
        AddCheckFieldToGrid(grid, ref row, 3, "Raise Anim On", sh.ShieldRaiseAnimOn, v => sh.ShieldRaiseAnimOn = v);
        AddIntFieldToGrid(grid, ref row, 3, "Raise Anim Ticks", sh.ShieldRaiseAnimTicks, v => sh.ShieldRaiseAnimTicks = v);

        return BuildSubStateCard("SHIELDS STATE", "AccentBlueBrush", grid);
    }

    private Border BuildCloakingCard(CloakingInfo clk)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });

        int row = 0;
        AddIntFieldToGrid(grid, ref row, 0, "Cloak Ticks Goal", clk.CloakTicksGoal, v => clk.CloakTicksGoal = v);

        row = 0;
        AddIntFieldToGrid(grid, ref row, 3, "Cloak Ticks", clk.CloakTicks, v => clk.CloakTicks = v);

        return BuildSubStateCard("CLOAKING STATE", "AccentBlueBrush", grid);
    }

    private void AddIntFieldToGrid(Grid grid, ref int row, int colOffset, string label, int value, Action<int> setter)
    {
        while (grid.RowDefinitions.Count <= row)
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var lbl = new Label
        {
            Content = label,
            Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush"),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetRow(lbl, row);
        Grid.SetColumn(lbl, colOffset);
        grid.Children.Add(lbl);

        var box = new TextBox
        {
            Text = value.ToString(),
            Width = 80,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 0, 6)
        };
        box.LostFocus += (_, _) =>
        {
            if (int.TryParse(box.Text, out int v))
            {
                setter(v);
                _state.MarkDirty();
            }
        };
        Grid.SetRow(box, row);
        Grid.SetColumn(box, colOffset + 1);
        grid.Children.Add(box);

        row++;
    }

    private void AddCheckFieldToGrid(Grid grid, ref int row, int colOffset, string label, bool value, Action<bool> setter)
    {
        while (grid.RowDefinitions.Count <= row)
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var lbl = new Label
        {
            Content = label,
            Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush"),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetRow(lbl, row);
        Grid.SetColumn(lbl, colOffset);
        grid.Children.Add(lbl);

        var chk = new CheckBox
        {
            IsChecked = value,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 6)
        };
        chk.Checked += (_, _) => { setter(true); _state.MarkDirty(); };
        chk.Unchecked += (_, _) => { setter(false); _state.MarkDirty(); };
        Grid.SetRow(chk, row);
        Grid.SetColumn(chk, colOffset + 1);
        grid.Children.Add(chk);

        row++;
    }

    private TextBox CreateIntBox(int value)
    {
        return new TextBox
        {
            Text = value.ToString(),
            Width = 50,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Padding = new Thickness(4, 2, 4, 2),
            FontSize = 12
        };
    }
}
