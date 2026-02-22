using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using FtlSaveEditor.Data;
using FtlSaveEditor.Models;
using FtlSaveEditor.Services;

namespace FtlSaveEditor.Views;

public partial class DronesEditor : UserControl
{
    private readonly SaveEditorState _state = SaveEditorState.Instance;

    public DronesEditor()
    {
        InitializeComponent();
        AddDroneBtn.Click += AddDrone_Click;
        LoadData();
    }

    private void LoadData()
    {
        var ship = _state.GameState?.PlayerShip;
        if (ship == null) return;

        DroneListPanel.Children.Clear();

        if (ship.Drones.Count == 0)
        {
            EmptyText.Visibility = Visibility.Visible;
            return;
        }

        EmptyText.Visibility = Visibility.Collapsed;

        for (int i = 0; i < ship.Drones.Count; i++)
        {
            var drone = ship.Drones[i];
            DroneListPanel.Children.Add(BuildDroneRow(drone, i));
        }
    }

    private Border BuildDroneRow(DroneState drone, int index)
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
            Padding = new Thickness(12, 6, 12, 6)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });

        // Index
        var idxTb = new TextBlock
        {
            Text = index.ToString(),
            Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush"),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(idxTb, 0);
        grid.Children.Add(idxTb);

        // Drone ID (editable ComboBox with suggestions + live filter) + blueprint info
        var idPanel = new StackPanel();
        var cvs = new CollectionViewSource { Source = GetDroneSuggestions() };
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
        UpdateDroneInfo(drone.DroneId, infoTb);
        bool suppressChange = true;
        idBox.AddHandler(System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent,
            new RoutedEventHandler((_, _) =>
            {
                if (suppressChange) return;
                drone.DroneId = idBox.Text;
                _state.MarkDirty();
                UpdateDroneInfo(idBox.Text, infoTb);
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
        var correctDroneId = drone.DroneId;
        idBox.Text = correctDroneId;
        idBox.Loaded += (_, _) =>
        {
            suppressChange = true;
            idBox.Text = correctDroneId;
            suppressChange = false;
        };
        idPanel.Children.Add(idBox);
        idPanel.Children.Add(infoTb);
        Grid.SetColumn(idPanel, 1);
        grid.Children.Add(idPanel);

        // Armed
        var armedChk = new CheckBox
        {
            IsChecked = drone.Armed,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        armedChk.Checked += (_, _) => { drone.Armed = true; _state.MarkDirty(); };
        armedChk.Unchecked += (_, _) => { drone.Armed = false; _state.MarkDirty(); };
        Grid.SetColumn(armedChk, 2);
        grid.Children.Add(armedChk);

        // Player Controlled
        var pcChk = new CheckBox
        {
            IsChecked = drone.PlayerControlled,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        pcChk.Checked += (_, _) => { drone.PlayerControlled = true; _state.MarkDirty(); };
        pcChk.Unchecked += (_, _) => { drone.PlayerControlled = false; _state.MarkDirty(); };
        Grid.SetColumn(pcChk, 3);
        grid.Children.Add(pcChk);

        // Body X
        var bxBox = CreateSmallIntBox(drone.BodyX);
        bxBox.LostFocus += (_, _) =>
        {
            if (int.TryParse(bxBox.Text, out int v)) { drone.BodyX = v; _state.MarkDirty(); }
        };
        Grid.SetColumn(bxBox, 4);
        grid.Children.Add(bxBox);

        // Body Y
        var byBox = CreateSmallIntBox(drone.BodyY);
        byBox.LostFocus += (_, _) =>
        {
            if (int.TryParse(byBox.Text, out int v)) { drone.BodyY = v; _state.MarkDirty(); }
        };
        Grid.SetColumn(byBox, 5);
        grid.Children.Add(byBox);

        // Room ID
        var roomBox = CreateSmallIntBox(drone.BodyRoomId);
        roomBox.LostFocus += (_, _) =>
        {
            if (int.TryParse(roomBox.Text, out int v)) { drone.BodyRoomId = v; _state.MarkDirty(); }
        };
        Grid.SetColumn(roomBox, 6);
        grid.Children.Add(roomBox);

        // Health
        var hpBox = CreateSmallIntBox(drone.Health);
        hpBox.LostFocus += (_, _) =>
        {
            if (int.TryParse(hpBox.Text, out int v)) { drone.Health = v; _state.MarkDirty(); }
        };
        Grid.SetColumn(hpBox, 7);
        grid.Children.Add(hpBox);

        // Room Square
        var rsBox = CreateSmallIntBox(drone.BodyRoomSquare);
        rsBox.LostFocus += (_, _) =>
        {
            if (int.TryParse(rsBox.Text, out int v)) { drone.BodyRoomSquare = v; _state.MarkDirty(); }
        };
        Grid.SetColumn(rsBox, 8);
        grid.Children.Add(rsBox);

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
            if (capturedIndex >= 0 && capturedIndex < ship.Drones.Count)
            {
                ship.Drones.RemoveAt(capturedIndex);
                _state.MarkDirty();
                LoadData();
            }
        };
        Grid.SetColumn(removeBtn, 9);
        grid.Children.Add(removeBtn);

        row.Child = grid;
        return row;
    }

    private TextBox CreateSmallIntBox(int value)
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

    private string[] GetDroneSuggestions()
    {
        var ship = _state.GameState?.PlayerShip;
        var saveIds = ship?.Drones.Select(d => d.DroneId).Where(id => !string.IsNullOrEmpty(id)) ?? [];
        var modIds = _state.Blueprints.Drones.Keys;
        return ItemIds.Drones.Concat(saveIds).Concat(modIds)
            .Distinct().OrderBy(id => id).ToArray();
    }

    private void UpdateDroneInfo(string id, TextBlock infoTb)
    {
        if (_state.Blueprints.Drones.TryGetValue(id, out var bp))
        {
            infoTb.Text = $"{bp.Title}  |  {bp.Type}  {bp.Power}pwr  {bp.Cost}scrap";
            infoTb.ToolTip = CreateDarkTooltip(bp.Description);
            infoTb.Visibility = Visibility.Visible;
        }
        else
        {
            infoTb.Text = "";
            infoTb.Visibility = Visibility.Collapsed;
        }
    }

    private object? CreateDarkTooltip(string? description)
    {
        if (string.IsNullOrEmpty(description)) return null;
        return new ToolTip
        {
            Content = new TextBlock
            {
                Text = description,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 400,
                Foreground = (SolidColorBrush)FindResource("TextPrimaryBrush")
            },
            Background = (SolidColorBrush)FindResource("BgDarkBrush"),
            BorderBrush = (SolidColorBrush)FindResource("BorderBrush"),
            Padding = new Thickness(8, 6, 8, 6)
        };
    }

    private void AddDrone_Click(object sender, RoutedEventArgs e)
    {
        var ship = _state.GameState?.PlayerShip;
        if (ship == null) return;

        ship.Drones.Add(new DroneState
        {
            DroneId = "COMBAT_1",
            Armed = false,
            PlayerControlled = true,
            BodyX = 0,
            BodyY = 0,
            BodyRoomId = 0,
            BodyRoomSquare = -1,
            Health = 0
        });
        _state.MarkDirty();
        LoadData();
    }
}
