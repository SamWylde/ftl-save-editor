using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FtlSaveEditor.Data;
using FtlSaveEditor.Services;

namespace FtlSaveEditor.Views;

public partial class GameDataBrowser : UserControl
{
    private ModBlueprints? _vanillaBlueprints;
    private readonly ModBlueprints _modBlueprints;
    private string _activeSource = "vanilla";
    private string _activeCategory = "weapons";

    public GameDataBrowser()
    {
        InitializeComponent();

        _modBlueprints = SaveEditorState.Instance.Blueprints;

        VanillaTabBtn.Click += (_, _) => SwitchSource("vanilla");
        ModsTabBtn.Click += (_, _) => SwitchSource("mods");
        RawFilesTabBtn.Click += (_, _) => SwitchSource("rawfiles");

        WeaponsSubBtn.Click += (_, _) => SwitchCategory("weapons");
        DronesSubBtn.Click += (_, _) => SwitchCategory("drones");
        AugmentsSubBtn.Click += (_, _) => SwitchCategory("augments");
        CrewSubBtn.Click += (_, _) => SwitchCategory("crew");

        SearchBox.TextChanged += (_, _) => RefreshList();

        LoadVanillaBlueprintsAsync();
    }

    private async void LoadVanillaBlueprintsAsync()
    {
        StatusLabel.Text = "Loading vanilla blueprints from data.dat...";
        _vanillaBlueprints = await Task.Run(() => GameDataService.ExtractVanillaBlueprints());

        if (_vanillaBlueprints.HasData)
        {
            StatusLabel.Text = $"Loaded {_vanillaBlueprints.Weapons.Count} weapons, " +
                               $"{_vanillaBlueprints.Drones.Count} drones, " +
                               $"{_vanillaBlueprints.Augments.Count} augments";
        }
        else
        {
            StatusLabel.Text = "Could not find data.dat — set FTL game path in settings.";
        }

        RefreshList();
    }

    private void SwitchSource(string source)
    {
        _activeSource = source;
        CategoryPanel.Visibility = source == "rawfiles" ? Visibility.Collapsed : Visibility.Visible;
        RefreshList();
    }

    private void SwitchCategory(string category)
    {
        _activeCategory = category;
        RefreshList();
    }

    private void RefreshList()
    {
        ContentPanel.Children.Clear();
        var filter = SearchBox.Text.Trim().ToLowerInvariant();

        if (_activeSource == "rawfiles")
        {
            ShowRawFiles(filter);
            return;
        }

        var blueprints = _activeSource == "vanilla" ? _vanillaBlueprints : _modBlueprints;
        if (blueprints == null) return;

        switch (_activeCategory)
        {
            case "weapons":
                ShowWeapons(blueprints, filter);
                break;
            case "drones":
                ShowDrones(blueprints, filter);
                break;
            case "augments":
                ShowAugments(blueprints, filter);
                break;
            case "crew":
                ShowCrew(blueprints, filter);
                break;
        }
    }

    private void ShowWeapons(ModBlueprints bp, string filter)
    {
        var items = bp.Weapons.Values
            .Where(w => string.IsNullOrEmpty(filter) ||
                        w.Id.ToLowerInvariant().Contains(filter) ||
                        w.Title.ToLowerInvariant().Contains(filter))
            .OrderBy(w => w.Title)
            .ToList();

        StatusLabel.Text = $"{items.Count} weapons";

        // Header
        AddHeaderRow("ID", "Title", "Type", "Dmg", "Shots", "Pwr", "Cost", "CD");

        bool alt = false;
        foreach (var w in items)
        {
            AddDataRow(alt, w.Id, w.Title, w.Type,
                w.Damage.ToString(), w.Shots.ToString(), w.Power.ToString(),
                w.Cost.ToString(), w.Cooldown.ToString("0.#"));
            alt = !alt;
        }
    }

    private void ShowDrones(ModBlueprints bp, string filter)
    {
        var items = bp.Drones.Values
            .Where(d => string.IsNullOrEmpty(filter) ||
                        d.Id.ToLowerInvariant().Contains(filter) ||
                        d.Title.ToLowerInvariant().Contains(filter))
            .OrderBy(d => d.Title)
            .ToList();

        StatusLabel.Text = $"{items.Count} drones";
        AddHeaderRow("ID", "Title", "Type", "Power", "Cost", "", "", "");

        bool alt = false;
        foreach (var d in items)
        {
            AddDataRow(alt, d.Id, d.Title, d.Type,
                d.Power.ToString(), d.Cost.ToString(), "", "", "");
            alt = !alt;
        }
    }

    private void ShowAugments(ModBlueprints bp, string filter)
    {
        var items = bp.Augments.Values
            .Where(a => string.IsNullOrEmpty(filter) ||
                        a.Id.ToLowerInvariant().Contains(filter) ||
                        a.Title.ToLowerInvariant().Contains(filter))
            .OrderBy(a => a.Title)
            .ToList();

        StatusLabel.Text = $"{items.Count} augments";
        AddHeaderRow("ID", "Title", "Cost", "Stack", "", "", "", "");

        bool alt = false;
        foreach (var a in items)
        {
            AddDataRow(alt, a.Id, a.Title, a.Cost.ToString(),
                a.Stackable ? "Yes" : "No", "", "", "", "");
            alt = !alt;
        }
    }

    private void ShowCrew(ModBlueprints bp, string filter)
    {
        var items = bp.CrewRaces
            .Where(r => string.IsNullOrEmpty(filter) || r.ToLowerInvariant().Contains(filter))
            .OrderBy(r => r)
            .ToList();

        StatusLabel.Text = $"{items.Count} crew races";

        bool alt = false;
        foreach (var race in items)
        {
            var row = new Border
            {
                Background = alt ? (SolidColorBrush)FindResource("BgLightBrush") : Brushes.Transparent,
                Padding = new Thickness(12, 6, 12, 6),
                Cursor = Cursors.Hand
            };

            var tb = new TextBlock { Text = race, FontSize = 13 };
            row.Child = tb;

            var raceCopy = race;
            row.MouseLeftButtonUp += (_, _) =>
            {
                Clipboard.SetText(raceCopy);
                StatusLabel.Text = $"Copied: {raceCopy}";
            };
            row.MouseEnter += (_, _) => row.Background = (SolidColorBrush)FindResource("BgLighterBrush");
            row.MouseLeave += (_, _) => row.Background = alt ? (SolidColorBrush)FindResource("BgLightBrush") : Brushes.Transparent;

            ContentPanel.Children.Add(row);
            alt = !alt;
        }
    }

    private void ShowRawFiles(string filter)
    {
        var files = GameDataService.ListAllFiles();
        var filtered = files
            .Where(f => string.IsNullOrEmpty(filter) || f.FileName.ToLowerInvariant().Contains(filter))
            .ToList();

        StatusLabel.Text = $"{filtered.Count} files in data.dat";

        bool alt = false;
        foreach (var f in filtered)
        {
            var row = new Border
            {
                Background = alt ? (SolidColorBrush)FindResource("BgLightBrush") : Brushes.Transparent,
                Padding = new Thickness(12, 6, 12, 6),
                Cursor = Cursors.Hand
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

            var nameText = new TextBlock { Text = f.FileName, FontSize = 12 };
            Grid.SetColumn(nameText, 0);
            grid.Children.Add(nameText);

            var sizeText = new TextBlock
            {
                Text = f.DataSize > 1024 ? $"{f.DataSize / 1024} KB" : $"{f.DataSize} B",
                FontSize = 11,
                Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush"),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetColumn(sizeText, 1);
            grid.Children.Add(sizeText);

            row.Child = grid;

            var fileName = f.FileName;
            row.MouseLeftButtonUp += (_, _) => ShowFileContent(fileName);
            row.MouseEnter += (_, _) => row.Background = (SolidColorBrush)FindResource("BgLighterBrush");
            var altCopy = alt;
            row.MouseLeave += (_, _) => row.Background = altCopy ? (SolidColorBrush)FindResource("BgLightBrush") : Brushes.Transparent;

            ContentPanel.Children.Add(row);
            alt = !alt;
        }
    }

    private void ShowFileContent(string fileName)
    {
        var content = GameDataService.GetFileContent(fileName);
        if (content == null)
        {
            StatusLabel.Text = $"Could not read {fileName}";
            return;
        }

        ContentPanel.Children.Clear();
        StatusLabel.Text = $"Viewing: {fileName}";

        // Back button
        var backBtn = new Button
        {
            Content = "Back to file list",
            Style = (Style)FindResource("DarkButton"),
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 0, 8)
        };
        backBtn.Click += (_, _) => RefreshList();
        ContentPanel.Children.Add(backBtn);

        // Content display
        var textBox = new TextBox
        {
            Text = content,
            IsReadOnly = true,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 11,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.NoWrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            MaxHeight = 500,
            Background = (SolidColorBrush)FindResource("BgDarkBrush"),
            Foreground = (SolidColorBrush)FindResource("TextPrimaryBrush"),
            BorderBrush = (SolidColorBrush)FindResource("BorderBrush")
        };
        ContentPanel.Children.Add(textBox);
    }

    private void AddHeaderRow(params string[] cols)
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 4) };
        var colWidths = new[] { 200, 160, 80, 50, 50, 50, 50, 50 };
        for (int i = 0; i < colWidths.Length; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(colWidths[i]) });
        }

        for (int i = 0; i < cols.Length && i < colWidths.Length; i++)
        {
            if (string.IsNullOrEmpty(cols[i])) continue;
            var tb = new TextBlock
            {
                Text = cols[i],
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush"),
                Padding = new Thickness(12, 4, 4, 4)
            };
            Grid.SetColumn(tb, i);
            grid.Children.Add(tb);
        }

        ContentPanel.Children.Add(grid);
    }

    private void AddDataRow(bool alt, params string[] cols)
    {
        var row = new Border
        {
            Background = alt ? (SolidColorBrush)FindResource("BgLightBrush") : Brushes.Transparent,
            Cursor = Cursors.Hand
        };

        var grid = new Grid();
        var colWidths = new[] { 200, 160, 80, 50, 50, 50, 50, 50 };
        for (int i = 0; i < colWidths.Length; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(colWidths[i]) });
        }

        for (int i = 0; i < cols.Length && i < colWidths.Length; i++)
        {
            if (string.IsNullOrEmpty(cols[i])) continue;
            var tb = new TextBlock
            {
                Text = cols[i],
                FontSize = 12,
                Padding = new Thickness(12, 5, 4, 5)
            };
            Grid.SetColumn(tb, i);
            grid.Children.Add(tb);
        }

        row.Child = grid;

        // Click to copy ID (first column)
        if (cols.Length > 0)
        {
            var id = cols[0];
            row.MouseLeftButtonUp += (_, _) =>
            {
                Clipboard.SetText(id);
                StatusLabel.Text = $"Copied: {id}";
            };
        }

        row.MouseEnter += (_, _) => row.Background = (SolidColorBrush)FindResource("BgLighterBrush");
        row.MouseLeave += (_, _) => row.Background = alt ? (SolidColorBrush)FindResource("BgLightBrush") : Brushes.Transparent;

        ContentPanel.Children.Add(row);
    }
}
