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
