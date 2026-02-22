using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FtlSaveEditor.Data;
using FtlSaveEditor.Services;

namespace FtlSaveEditor.Views;

public partial class ItemBrowser : UserControl
{
    private readonly SaveEditorState _state = SaveEditorState.Instance;
    private string _activeCategory = "weapons";
    private string _sortColumn = "Id";
    private bool _sortAscending = true;

    public ItemBrowser()
    {
        InitializeComponent();
        WeaponsTabBtn.Click += (_, _) => SwitchCategory("weapons");
        DronesTabBtn.Click += (_, _) => SwitchCategory("drones");
        AugmentsTabBtn.Click += (_, _) => SwitchCategory("augments");
        SearchBox.TextChanged += (_, _) => RefreshList();
        SwitchCategory("weapons");
    }

    private void SwitchCategory(string category)
    {
        _activeCategory = category;
        _sortColumn = "Id";
        _sortAscending = true;

        // Update tab button styles
        WeaponsTabBtn.Background = category == "weapons"
            ? (SolidColorBrush)FindResource("AccentBlueBrush")
            : (SolidColorBrush)FindResource("BgLightBrush");
        DronesTabBtn.Background = category == "drones"
            ? (SolidColorBrush)FindResource("AccentBlueBrush")
            : (SolidColorBrush)FindResource("BgLightBrush");
        AugmentsTabBtn.Background = category == "augments"
            ? (SolidColorBrush)FindResource("AccentBlueBrush")
            : (SolidColorBrush)FindResource("BgLightBrush");

        BuildHeaders();
        RefreshList();
    }

    private void BuildHeaders()
    {
        HeaderGrid.ColumnDefinitions.Clear();
        HeaderGrid.Children.Clear();

        string[][] columns = _activeCategory switch
        {
            "weapons" => [["Id", "200"], ["Title", "180"], ["Type", "70"], ["Dmg", "50"],
                         ["Shots", "50"], ["Pwr", "50"], ["CD", "55"], ["Cost", "55"]],
            "drones" => [["Id", "220"], ["Title", "220"], ["Type", "90"], ["Pwr", "60"], ["Cost", "60"]],
            "augments" => [["Id", "220"], ["Title", "250"], ["Cost", "60"], ["Stack", "60"]],
            _ => []
        };

        foreach (var col in columns)
        {
            var colName = col[0];
            var width = int.Parse(col[1]);
            HeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(width) });

            var arrow = _sortColumn == colName ? (_sortAscending ? " \u25B2" : " \u25BC") : "";
            var tb = new TextBlock
            {
                Text = colName + arrow,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = _sortColumn == colName
                    ? (SolidColorBrush)FindResource("AccentBlueBrush")
                    : (SolidColorBrush)FindResource("TextSecondaryBrush"),
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center
            };
            tb.MouseLeftButtonUp += (_, _) =>
            {
                if (_sortColumn == colName) _sortAscending = !_sortAscending;
                else { _sortColumn = colName; _sortAscending = true; }
                BuildHeaders();
                RefreshList();
            };
            Grid.SetColumn(tb, HeaderGrid.ColumnDefinitions.Count - 1);
            HeaderGrid.Children.Add(tb);
        }
    }

    private void RefreshList()
    {
        ItemListPanel.Children.Clear();
        var filter = SearchBox.Text?.Trim() ?? "";

        switch (_activeCategory)
        {
            case "weapons": RefreshWeapons(filter); break;
            case "drones": RefreshDrones(filter); break;
            case "augments": RefreshAugments(filter); break;
        }
    }

    private void RefreshWeapons(string filter)
    {
        var items = _state.Blueprints.Weapons.Values.AsEnumerable();
        if (!string.IsNullOrEmpty(filter))
            items = items.Where(w =>
                w.Id.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                w.Title.Contains(filter, StringComparison.OrdinalIgnoreCase));

        items = _sortColumn switch
        {
            "Title" => _sortAscending ? items.OrderBy(w => w.Title) : items.OrderByDescending(w => w.Title),
            "Type" => _sortAscending ? items.OrderBy(w => w.Type) : items.OrderByDescending(w => w.Type),
            "Dmg" => _sortAscending ? items.OrderBy(w => w.Damage) : items.OrderByDescending(w => w.Damage),
            "Shots" => _sortAscending ? items.OrderBy(w => w.Shots) : items.OrderByDescending(w => w.Shots),
            "Pwr" => _sortAscending ? items.OrderBy(w => w.Power) : items.OrderByDescending(w => w.Power),
            "CD" => _sortAscending ? items.OrderBy(w => w.Cooldown) : items.OrderByDescending(w => w.Cooldown),
            "Cost" => _sortAscending ? items.OrderBy(w => w.Cost) : items.OrderByDescending(w => w.Cost),
            _ => _sortAscending ? items.OrderBy(w => w.Id) : items.OrderByDescending(w => w.Id),
        };

        var list = items.ToList();
        StatusText.Text = $"Showing {list.Count} weapons";

        for (int i = 0; i < list.Count; i++)
        {
            var w = list[i];
            var row = BuildRow(i, [w.Id, w.Title, w.Type, w.Damage.ToString(), w.Shots.ToString(),
                w.Power.ToString(), w.Cooldown.ToString("0.#"), w.Cost.ToString()],
                w.Description);
            ItemListPanel.Children.Add(row);
        }
    }

    private void RefreshDrones(string filter)
    {
        var items = _state.Blueprints.Drones.Values.AsEnumerable();
        if (!string.IsNullOrEmpty(filter))
            items = items.Where(d =>
                d.Id.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                d.Title.Contains(filter, StringComparison.OrdinalIgnoreCase));

        items = _sortColumn switch
        {
            "Title" => _sortAscending ? items.OrderBy(d => d.Title) : items.OrderByDescending(d => d.Title),
            "Type" => _sortAscending ? items.OrderBy(d => d.Type) : items.OrderByDescending(d => d.Type),
            "Pwr" => _sortAscending ? items.OrderBy(d => d.Power) : items.OrderByDescending(d => d.Power),
            "Cost" => _sortAscending ? items.OrderBy(d => d.Cost) : items.OrderByDescending(d => d.Cost),
            _ => _sortAscending ? items.OrderBy(d => d.Id) : items.OrderByDescending(d => d.Id),
        };

        var list = items.ToList();
        StatusText.Text = $"Showing {list.Count} drones";

        for (int i = 0; i < list.Count; i++)
        {
            var d = list[i];
            var row = BuildRow(i, [d.Id, d.Title, d.Type, d.Power.ToString(), d.Cost.ToString()],
                d.Description);
            ItemListPanel.Children.Add(row);
        }
    }

    private void RefreshAugments(string filter)
    {
        var items = _state.Blueprints.Augments.Values.AsEnumerable();
        if (!string.IsNullOrEmpty(filter))
            items = items.Where(a =>
                a.Id.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                a.Title.Contains(filter, StringComparison.OrdinalIgnoreCase));

        items = _sortColumn switch
        {
            "Title" => _sortAscending ? items.OrderBy(a => a.Title) : items.OrderByDescending(a => a.Title),
            "Cost" => _sortAscending ? items.OrderBy(a => a.Cost) : items.OrderByDescending(a => a.Cost),
            "Stack" => _sortAscending ? items.OrderBy(a => a.Stackable) : items.OrderByDescending(a => a.Stackable),
            _ => _sortAscending ? items.OrderBy(a => a.Id) : items.OrderByDescending(a => a.Id),
        };

        var list = items.ToList();
        StatusText.Text = $"Showing {list.Count} augments";

        for (int i = 0; i < list.Count; i++)
        {
            var a = list[i];
            var row = BuildRow(i, [a.Id, a.Title, a.Cost.ToString(), a.Stackable ? "Yes" : ""],
                a.Description);
            ItemListPanel.Children.Add(row);
        }
    }

    private Border BuildRow(int index, string[] values, string? tooltip)
    {
        var bgBrush = index % 2 == 0
            ? (SolidColorBrush)FindResource("BgMediumBrush")
            : (SolidColorBrush)FindResource("BgDarkBrush");

        var border = new Border
        {
            Background = bgBrush,
            Padding = new Thickness(12, 4, 12, 4)
        };
        if (!string.IsNullOrEmpty(tooltip))
        {
            border.ToolTip = new ToolTip
            {
                Content = new TextBlock
                {
                    Text = tooltip,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 400
                },
                Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom,
                VerticalOffset = 2
            };
        }

        var grid = new Grid();
        for (int c = 0; c < HeaderGrid.ColumnDefinitions.Count && c < values.Length; c++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(HeaderGrid.ColumnDefinitions[c].Width.Value)
            });

            var tb = new TextBlock
            {
                Text = values[c],
                FontSize = 12,
                Foreground = c == 0
                    ? (SolidColorBrush)FindResource("TextPrimaryBrush")
                    : (SolidColorBrush)FindResource("TextSecondaryBrush"),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(tb, c);
            grid.Children.Add(tb);
        }

        // Click to copy ID, right-click context menu
        border.Cursor = Cursors.Hand;
        border.MouseLeftButtonUp += (_, _) =>
        {
            Clipboard.SetText(values[0]);
            StatusText.Text = $"Copied: {values[0]}";
        };

        var menu = new ContextMenu();
        var copyItem = new MenuItem { Header = $"Copy ID: {values[0]}" };
        copyItem.Click += (_, _) =>
        {
            Clipboard.SetText(values[0]);
            StatusText.Text = $"Copied: {values[0]}";
        };
        menu.Items.Add(copyItem);
        border.ContextMenu = menu;

        border.Child = grid;
        return border;
    }
}
