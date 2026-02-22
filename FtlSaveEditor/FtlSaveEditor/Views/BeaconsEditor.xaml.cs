using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using FtlSaveEditor.Data;
using FtlSaveEditor.Models;
using FtlSaveEditor.Services;

namespace FtlSaveEditor.Views;

public partial class BeaconsEditor : UserControl
{
    private readonly SaveEditorState _state = SaveEditorState.Instance;

    public BeaconsEditor()
    {
        InitializeComponent();
        LoadData();
    }

    private void LoadData()
    {
        var gs = _state.GameState;
        if (gs == null) return;

        BeaconCountText.Text = $"{gs.Beacons.Count} beacon(s) in sector. Current beacon: {gs.CurrentBeaconId}";
        BeaconListPanel.Children.Clear();

        for (int i = 0; i < gs.Beacons.Count; i++)
        {
            var beacon = gs.Beacons[i];
            BeaconListPanel.Children.Add(BuildBeaconCard(beacon, i));
        }
    }

    private Border BuildBeaconCard(BeaconState beacon, int index)
    {
        var card = new Border
        {
            Background = (SolidColorBrush)FindResource("BgMediumBrush"),
            BorderBrush = (SolidColorBrush)FindResource("BorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Margin = new Thickness(0, 0, 0, 6)
        };

        var expander = new Expander
        {
            IsExpanded = false,
            Padding = new Thickness(12, 6, 12, 6),
            Foreground = (SolidColorBrush)FindResource("TextPrimaryBrush"),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0)
        };

        // Header
        var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };
        headerPanel.Children.Add(new TextBlock
        {
            Text = $"Beacon #{index}",
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = (SolidColorBrush)FindResource("TextPrimaryBrush"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 12, 0)
        });

        if (beacon.Seen)
        {
            headerPanel.Children.Add(CreateTag("Seen", "AccentGreenBrush"));
        }
        if (beacon.EnemyPresent)
        {
            headerPanel.Children.Add(CreateTag("Enemy", "AccentRedBrush"));
        }
        if (beacon.StorePresent)
        {
            headerPanel.Children.Add(CreateTag("Store", "AccentOrangeBrush"));
        }
        if (beacon.UnderAttack)
        {
            headerPanel.Children.Add(CreateTag("Under Attack", "AccentRedBrush"));
        }

        string fleetStr = beacon.FleetPresence switch
        {
            1 => "Rebel",
            2 => "Federation",
            3 => "Both",
            _ => ""
        };
        if (!string.IsNullOrEmpty(fleetStr))
        {
            headerPanel.Children.Add(CreateTag($"Fleet: {fleetStr}", "AccentBlueBrush"));
        }

        headerPanel.Children.Add(new TextBlock
        {
            Text = $"Visits: {beacon.VisitCount}",
            FontSize = 11,
            Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0)
        });

        expander.Header = headerPanel;

        // Content
        var content = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };

        // Basic beacon fields
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });

        int row = 0;
        AddIntField(grid, ref row, 0, "Visit Count", beacon.VisitCount, v => beacon.VisitCount = v);
        AddIntField(grid, ref row, 0, "Fleet Presence", beacon.FleetPresence, v => beacon.FleetPresence = v);
        if (!string.IsNullOrEmpty(beacon.ShipEventId))
            AddStringField(grid, ref row, 0, "Ship Event ID", beacon.ShipEventId, v => beacon.ShipEventId = v);
        if (!string.IsNullOrEmpty(beacon.AutoBlueprintId))
            AddStringField(grid, ref row, 0, "Auto Blueprint", beacon.AutoBlueprintId, v => beacon.AutoBlueprintId = v);

        row = 0;
        AddCheckField(grid, ref row, 3, "Seen", beacon.Seen, v => beacon.Seen = v);
        AddCheckField(grid, ref row, 3, "Enemy Present", beacon.EnemyPresent, v => beacon.EnemyPresent = v);
        AddCheckField(grid, ref row, 3, "Store Present", beacon.StorePresent, v => beacon.StorePresent = v);
        AddCheckField(grid, ref row, 3, "Under Attack", beacon.UnderAttack, v => beacon.UnderAttack = v);

        content.Children.Add(grid);

        // Store details (if present)
        if (beacon.Store != null)
        {
            content.Children.Add(new Separator
            {
                Background = (SolidColorBrush)FindResource("BorderBrush"),
                Margin = new Thickness(0, 12, 0, 12)
            });

            content.Children.Add(new TextBlock
            {
                Text = "STORE DETAILS",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = (SolidColorBrush)FindResource("AccentOrangeBrush"),
                Margin = new Thickness(0, 0, 0, 8)
            });

            var store = beacon.Store;
            var storeGrid = new Grid();
            storeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            storeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            storeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
            storeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            storeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });

            int sr = 0;
            AddIntField(storeGrid, ref sr, 0, "Fuel", store.Fuel, v => store.Fuel = v);
            AddIntField(storeGrid, ref sr, 0, "Missiles", store.Missiles, v => store.Missiles = v);
            AddIntField(storeGrid, ref sr, 0, "Drone Parts", store.DroneParts, v => store.DroneParts = v);

            sr = 0;
            AddIntField(storeGrid, ref sr, 3, "Shelf Count", store.ShelfCount, v => store.ShelfCount = v);

            content.Children.Add(storeGrid);

            // Shelves
            for (int si = 0; si < store.Shelves.Count; si++)
            {
                var shelf = store.Shelves[si];
                content.Children.Add(new TextBlock
                {
                    Text = $"Shelf {si} (Type: {shelf.ItemType})",
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = (SolidColorBrush)FindResource("TextPrimaryBrush"),
                    Margin = new Thickness(0, 8, 0, 4)
                });

                for (int ii = 0; ii < shelf.Items.Count; ii++)
                {
                    var item = shelf.Items[ii];
                    var itemPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Margin = new Thickness(16, 2, 0, 2)
                    };

                    itemPanel.Children.Add(new TextBlock
                    {
                        Text = $"Item {ii}:",
                        Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush"),
                        Width = 60,
                        VerticalAlignment = VerticalAlignment.Center
                    });

                    var availBox = new TextBox
                    {
                        Text = item.Available.ToString(),
                        Width = 40,
                        Margin = new Thickness(0, 0, 8, 0)
                    };
                    var capturedItem = item;
                    availBox.LostFocus += (_, _) =>
                    {
                        if (int.TryParse(availBox.Text, out int v))
                        {
                            capturedItem.Available = v;
                            _state.MarkDirty();
                        }
                    };
                    itemPanel.Children.Add(new TextBlock
                    {
                        Text = "Avail:",
                        Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush"),
                        Margin = new Thickness(0, 0, 4, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    itemPanel.Children.Add(availBox);

                    if (item.ItemId != null)
                    {
                        itemPanel.Children.Add(new TextBlock
                        {
                            Text = "ID:",
                            Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush"),
                            Margin = new Thickness(0, 0, 4, 0),
                            VerticalAlignment = VerticalAlignment.Center
                        });

                        var idPanel = new StackPanel();
                        var cvs = new CollectionViewSource { Source = GetStoreItemSuggestions() };
                        var idBox = new ComboBox
                        {
                            IsEditable = true,
                            IsTextSearchEnabled = false,
                            ItemsSource = cvs.View,
                            MaxDropDownHeight = 300,
                            Width = 240,
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        var infoTb = new TextBlock
                        {
                            FontSize = 11,
                            Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush"),
                            Margin = new Thickness(2, 2, 0, 0),
                            TextWrapping = TextWrapping.Wrap
                        };
                        UpdateStoreItemInfo(item.ItemId, infoTb);
                        bool suppressChange = true;
                        idBox.AddHandler(System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent,
                            new RoutedEventHandler((_, _) =>
                            {
                                if (suppressChange) return;
                                capturedItem.ItemId = idBox.Text;
                                _state.MarkDirty();
                                UpdateStoreItemInfo(idBox.Text, infoTb);
                                var text = idBox.Text;
                                cvs.View.Filter = f => ((string)f).Contains(text, StringComparison.OrdinalIgnoreCase);
                            }));
                        idBox.DropDownOpened += (_, _) =>
                        {
                            suppressChange = true;
                            var savedText = idBox.Text;
                            cvs.View.Filter = null;
                            idBox.Text = savedText;
                            suppressChange = false;
                        };
                        idBox.Text = item.ItemId;
                        suppressChange = false;
                        idPanel.Children.Add(idBox);
                        idPanel.Children.Add(infoTb);
                        itemPanel.Children.Add(idPanel);
                    }

                    content.Children.Add(itemPanel);
                }
            }
        }

        expander.Content = content;
        card.Child = expander;
        return card;
    }

    private Border CreateTag(string text, string colorKey)
    {
        var brush = (SolidColorBrush)FindResource(colorKey);
        return new Border
        {
            Background = Brushes.Transparent,
            BorderBrush = brush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(3),
            Padding = new Thickness(6, 1, 6, 1),
            Margin = new Thickness(0, 0, 6, 0),
            Child = new TextBlock
            {
                Text = text,
                FontSize = 10,
                Foreground = brush
            }
        };
    }

    private void AddIntField(Grid grid, ref int row, int colOffset, string label, int value, Action<int> setter)
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
            Margin = new Thickness(0, 0, 0, 4),
            Width = 80,
            HorizontalAlignment = HorizontalAlignment.Left
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

    private void AddStringField(Grid grid, ref int row, int colOffset, string label, string value, Action<string> setter)
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
            Text = value,
            Margin = new Thickness(0, 0, 0, 4)
        };
        box.TextChanged += (_, _) =>
        {
            setter(box.Text);
            _state.MarkDirty();
        };
        Grid.SetRow(box, row);
        Grid.SetColumn(box, colOffset + 1);
        grid.Children.Add(box);

        row++;
    }

    private string[] GetStoreItemSuggestions()
    {
        var modWeapons = _state.Blueprints.Weapons.Keys;
        var modDrones = _state.Blueprints.Drones.Keys;
        var modAugments = _state.Blueprints.Augments.Keys;
        return ItemIds.Weapons.Concat(ItemIds.Drones).Concat(ItemIds.Augments)
            .Concat(modWeapons).Concat(modDrones).Concat(modAugments)
            .Distinct().OrderBy(id => id).ToArray();
    }

    private void UpdateStoreItemInfo(string id, TextBlock infoTb)
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

    private void AddCheckField(Grid grid, ref int row, int colOffset, string label, bool value, Action<bool> setter)
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
            Margin = new Thickness(0, 0, 0, 4)
        };
        chk.Checked += (_, _) => { setter(true); _state.MarkDirty(); };
        chk.Unchecked += (_, _) => { setter(false); _state.MarkDirty(); };
        Grid.SetRow(chk, row);
        Grid.SetColumn(chk, colOffset + 1);
        grid.Children.Add(chk);

        row++;
    }
}
