using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using FtlSaveEditor.Data;
using FtlSaveEditor.Models;
using FtlSaveEditor.Services;

namespace FtlSaveEditor.Views;

public partial class WeaponsEditor : UserControl
{
    private readonly SaveEditorState _state = SaveEditorState.Instance;

    public WeaponsEditor()
    {
        InitializeComponent();
        AddWeaponBtn.Click += AddWeapon_Click;
        LoadData();
    }

    private void LoadData()
    {
        var ship = _state.GameState?.PlayerShip;
        if (ship == null) return;

        WeaponListPanel.Children.Clear();

        if (ship.Weapons.Count == 0)
        {
            EmptyText.Visibility = Visibility.Visible;
            return;
        }

        EmptyText.Visibility = Visibility.Collapsed;

        for (int i = 0; i < ship.Weapons.Count; i++)
        {
            var weapon = ship.Weapons[i];
            WeaponListPanel.Children.Add(BuildWeaponRow(weapon, i));
        }
    }

    private Border BuildWeaponRow(WeaponState weapon, int index)
    {
        bool isEven = index % 2 == 0;
        var bgBrush = isEven
            ? (SolidColorBrush)FindResource("BgMediumBrush")
            : (SolidColorBrush)FindResource("BgDarkBrush");

        var row = new Border
        {
            Background = bgBrush,
            BorderBrush = (SolidColorBrush)FindResource("BorderBrush"),
            BorderThickness = new Thickness(1, 0, 1, 1),
            Padding = new Thickness(12, 8, 12, 8)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });

        // Index
        var idxTb = new TextBlock
        {
            Text = index.ToString(),
            Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush"),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(idxTb, 0);
        grid.Children.Add(idxTb);

        // Weapon ID (editable ComboBox with suggestions + live filter) + blueprint info
        var idPanel = new StackPanel();
        var cvs = new CollectionViewSource { Source = GetWeaponSuggestions() };
        var idBox = new ComboBox
        {
            IsEditable = true,
            IsTextSearchEnabled = false,
            ItemsSource = cvs.View,
            MaxDropDownHeight = 300,
            VerticalAlignment = VerticalAlignment.Center
        };
        var infoTb = new TextBlock
        {
            FontSize = 11,
            Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush"),
            Margin = new Thickness(2, 2, 0, 0)
        };
        UpdateWeaponInfo(weapon.WeaponId, infoTb);
        bool suppressChange = true;
        idBox.AddHandler(System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent,
            new RoutedEventHandler((_, _) =>
            {
                if (suppressChange) return;
                weapon.WeaponId = idBox.Text;
                _state.MarkDirty();
                UpdateWeaponInfo(idBox.Text, infoTb);
                var text = idBox.Text;
                cvs.View.Filter = item => ((string)item).Contains(text, StringComparison.OrdinalIgnoreCase);
            }));
        idBox.DropDownOpened += (_, _) =>
        {
            suppressChange = true;
            var savedText = idBox.Text;
            cvs.View.Filter = null;
            idBox.Text = savedText;
            suppressChange = false;
        };
        idBox.Text = weapon.WeaponId;
        suppressChange = false;
        idPanel.Children.Add(idBox);
        idPanel.Children.Add(infoTb);
        Grid.SetColumn(idPanel, 1);
        grid.Children.Add(idPanel);

        // Armed
        var armedChk = new CheckBox
        {
            IsChecked = weapon.Armed,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        armedChk.Checked += (_, _) => { weapon.Armed = true; _state.MarkDirty(); };
        armedChk.Unchecked += (_, _) => { weapon.Armed = false; _state.MarkDirty(); };
        Grid.SetColumn(armedChk, 2);
        grid.Children.Add(armedChk);

        // Cooldown
        var cdBox = new TextBox
        {
            Text = weapon.CooldownTicks.ToString(),
            Width = 70,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };
        cdBox.LostFocus += (_, _) =>
        {
            if (int.TryParse(cdBox.Text, out int v)) { weapon.CooldownTicks = v; _state.MarkDirty(); }
        };
        Grid.SetColumn(cdBox, 3);
        grid.Children.Add(cdBox);

        // Remove button
        var removeBtn = new Button
        {
            Content = "X",
            Style = (Style)FindResource("DarkButton"),
            Foreground = (SolidColorBrush)FindResource("AccentRedBrush"),
            Width = 30,
            Height = 26,
            Padding = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        int capturedIndex = index;
        removeBtn.Click += (_, _) =>
        {
            var ship = _state.GameState?.PlayerShip;
            if (ship == null) return;
            if (capturedIndex >= 0 && capturedIndex < ship.Weapons.Count)
            {
                ship.Weapons.RemoveAt(capturedIndex);
                _state.MarkDirty();
                LoadData();
            }
        };
        Grid.SetColumn(removeBtn, 4);
        grid.Children.Add(removeBtn);

        row.Child = grid;
        return row;
    }

    private string[] GetWeaponSuggestions()
    {
        var ship = _state.GameState?.PlayerShip;
        var saveIds = ship?.Weapons.Select(w => w.WeaponId).Where(id => !string.IsNullOrEmpty(id)) ?? [];
        var cargoIds = _state.GameState?.CargoIdList.Where(id => !string.IsNullOrEmpty(id)) ?? [];
        var modIds = _state.Blueprints.Weapons.Keys;
        return ItemIds.Weapons.Concat(saveIds).Concat(cargoIds).Concat(modIds)
            .Distinct().OrderBy(id => id).ToArray();
    }

    private void UpdateWeaponInfo(string id, TextBlock infoTb)
    {
        if (_state.Blueprints.Weapons.TryGetValue(id, out var bp))
        {
            infoTb.Text = $"{bp.Title}  |  {bp.Type}  {bp.Damage}dmg x{bp.Shots}  {bp.Power}pwr  {bp.Cooldown}s  {bp.Cost}scrap";
            infoTb.ToolTip = string.IsNullOrEmpty(bp.Description) ? null : bp.Description;
            infoTb.Visibility = Visibility.Visible;
        }
        else
        {
            infoTb.Text = "";
            infoTb.Visibility = Visibility.Collapsed;
        }
    }

    private void AddWeapon_Click(object sender, RoutedEventArgs e)
    {
        var ship = _state.GameState?.PlayerShip;
        if (ship == null) return;

        ship.Weapons.Add(new WeaponState
        {
            WeaponId = "LASER_BURST_1",
            Armed = false,
            CooldownTicks = 0
        });
        _state.MarkDirty();
        LoadData();
    }
}
