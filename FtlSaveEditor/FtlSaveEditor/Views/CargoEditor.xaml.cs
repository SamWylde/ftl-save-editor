using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using FtlSaveEditor.Data;
using FtlSaveEditor.Services;

namespace FtlSaveEditor.Views;

public partial class CargoEditor : UserControl
{
    private readonly SaveEditorState _state = SaveEditorState.Instance;

    public CargoEditor()
    {
        InitializeComponent();
        AddCargoBtn.Click += AddCargo_Click;
        LoadData();
    }

    private void LoadData()
    {
        var gs = _state.GameState;
        if (gs == null) return;

        CargoListPanel.Children.Clear();

        if (gs.CargoIdList.Count == 0)
        {
            EmptyText.Visibility = Visibility.Visible;
            return;
        }

        EmptyText.Visibility = Visibility.Collapsed;

        for (int i = 0; i < gs.CargoIdList.Count; i++)
        {
            CargoListPanel.Children.Add(BuildCargoRow(i));
        }
    }

    private Grid BuildCargoRow(int index)
    {
        var gs = _state.GameState!;

        var grid = new Grid { Margin = new Thickness(0, 0, 0, 6) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
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

        // Cargo ID (editable ComboBox with suggestions + live filter) + blueprint info
        int capturedIndex = index;
        var idPanel = new StackPanel();
        var cvs = new CollectionViewSource { Source = GetCargoSuggestions() };
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
            Margin = new Thickness(2, 2, 0, 0),
            TextWrapping = TextWrapping.Wrap
        };
        UpdateCargoInfo(gs.CargoIdList[index], infoTb);
        bool suppressChange = true;
        idBox.AddHandler(System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent,
            new RoutedEventHandler((_, _) =>
            {
                if (suppressChange) return;
                if (capturedIndex < gs.CargoIdList.Count)
                {
                    gs.CargoIdList[capturedIndex] = idBox.Text;
                    _state.MarkDirty();
                    UpdateCargoInfo(idBox.Text, infoTb);
                }
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
        var correctCargoId = gs.CargoIdList[index];
        idBox.Text = correctCargoId;
        idBox.Loaded += (_, _) =>
        {
            suppressChange = true;
            idBox.Text = correctCargoId;
            suppressChange = false;
        };
        idPanel.Children.Add(idBox);
        idPanel.Children.Add(infoTb);
        Grid.SetColumn(idPanel, 1);
        grid.Children.Add(idPanel);

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
        removeBtn.Click += (_, _) =>
        {
            if (capturedIndex >= 0 && capturedIndex < gs.CargoIdList.Count)
            {
                gs.CargoIdList.RemoveAt(capturedIndex);
                _state.MarkDirty();
                LoadData();
            }
        };
        Grid.SetColumn(removeBtn, 2);
        grid.Children.Add(removeBtn);

        return grid;
    }

    private string[] GetCargoSuggestions()
    {
        var gs = _state.GameState;
        var saveIds = gs?.CargoIdList.Where(id => !string.IsNullOrEmpty(id)) ?? [];
        var modWeapons = _state.Blueprints.Weapons.Keys;
        var modDrones = _state.Blueprints.Drones.Keys;
        var modAugments = _state.Blueprints.Augments.Keys;
        return ItemIds.Weapons.Concat(ItemIds.Drones).Concat(ItemIds.Augments)
            .Concat(saveIds).Concat(modWeapons).Concat(modDrones).Concat(modAugments)
            .Distinct().OrderBy(id => id).ToArray();
    }

    private void UpdateCargoInfo(string id, TextBlock infoTb)
    {
        if (_state.Blueprints.Weapons.TryGetValue(id, out var wp))
        {
            infoTb.Text = $"{wp.Title}  |  {wp.Type}  {wp.Damage}dmg x{wp.Shots}  {wp.Power}pwr  {wp.Cooldown}s  {wp.Cost}scrap";
            infoTb.ToolTip = string.IsNullOrEmpty(wp.Description) ? null : wp.Description;
            infoTb.Visibility = Visibility.Visible;
        }
        else if (_state.Blueprints.Drones.TryGetValue(id, out var dp))
        {
            infoTb.Text = $"{dp.Title}  |  {dp.Type}  {dp.Power}pwr  {dp.Cost}scrap";
            infoTb.ToolTip = string.IsNullOrEmpty(dp.Description) ? null : dp.Description;
            infoTb.Visibility = Visibility.Visible;
        }
        else if (_state.Blueprints.Augments.TryGetValue(id, out var ap))
        {
            var parts = new System.Collections.Generic.List<string> { ap.Title };
            if (ap.Cost > 0) parts.Add($"{ap.Cost}scrap");
            if (ap.Stackable) parts.Add("stackable");
            infoTb.Text = string.Join("  |  ", parts);
            infoTb.ToolTip = string.IsNullOrEmpty(ap.Description) ? null : ap.Description;
            infoTb.Visibility = Visibility.Visible;
        }
        else
        {
            infoTb.Text = "";
            infoTb.Visibility = Visibility.Collapsed;
        }
    }

    private void AddCargo_Click(object sender, RoutedEventArgs e)
    {
        var gs = _state.GameState;
        if (gs == null) return;

        gs.CargoIdList.Add("LASER_BURST_1");
        _state.MarkDirty();
        LoadData();
    }
}
