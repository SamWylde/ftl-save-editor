using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FtlSaveEditor.Models;
using FtlSaveEditor.Services;

namespace FtlSaveEditor.Views;

public partial class MiscEditor : UserControl
{
    private readonly SaveEditorState _state = SaveEditorState.Instance;
    private bool _loading = true;

    public MiscEditor()
    {
        InitializeComponent();
        LoadData();
        _loading = false;
        WireEvents();
    }

    private void LoadData()
    {
        var gs = _state.GameState;
        if (gs == null) return;

        // Difficulty
        DifficultyCombo.Items.Clear();
        DifficultyCombo.Items.Add("Easy");
        DifficultyCombo.Items.Add("Normal");
        DifficultyCombo.Items.Add("Hard");
        DifficultyCombo.SelectedIndex = gs.Difficulty >= 0 && gs.Difficulty <= 2 ? gs.Difficulty : 0;

        // Basic settings
        DlcEnabledCheck.IsChecked = gs.DlcEnabled;
        AutofireCheck.IsChecked = gs.Autofire;
        FileFormatBox.Text = gs.FileFormat.ToString();

        // Sector
        SectorNumberBox.Text = gs.SectorNumber.ToString();
        OneBasedSectorBox.Text = gs.OneBasedSectorNumber.ToString();
        CurrentBeaconBox.Text = gs.CurrentBeaconId.ToString();
        CrystalWorldsCheck.IsChecked = gs.SectorIsHiddenCrystalWorlds;

        // Seeds
        TreeSeedBox.Text = gs.SectorTreeSeed.ToString();
        LayoutSeedBox.Text = gs.SectorLayoutSeed.ToString();

        // Rebel fleet
        FleetOffsetBox.Text = gs.RebelFleetOffset.ToString();
        FleetFudgeBox.Text = gs.RebelFleetFudge.ToString();
        PursuitModBox.Text = gs.RebelPursuitMod.ToString();
        FlagshipVisibleCheck.IsChecked = gs.RebelFlagshipVisible;
        FlagshipMovingCheck.IsChecked = gs.RebelFlagshipMoving;
        FlagshipRetreatingCheck.IsChecked = gs.RebelFlagshipRetreating;
        FlagshipHopBox.Text = gs.RebelFlagshipHop.ToString();
        FlagshipBaseTurnsBox.Text = gs.RebelFlagshipBaseTurns.ToString();

        // Environment
        BuildEnvironmentSection();

        // Encounter
        BuildEncounterSection();

        // Flagship
        BuildFlagshipSection();

        // Stats
        BuildStatsSection();
    }

    private void WireEvents()
    {
        DifficultyCombo.SelectionChanged += (_, _) =>
        {
            if (_loading) return;
            _state.GameState!.Difficulty = DifficultyCombo.SelectedIndex;
            _state.MarkDirty();
        };

        DlcEnabledCheck.Checked += (_, _) => SetBool(v => _state.GameState!.DlcEnabled = v, true);
        DlcEnabledCheck.Unchecked += (_, _) => SetBool(v => _state.GameState!.DlcEnabled = v, false);

        AutofireCheck.Checked += (_, _) => SetBool(v => _state.GameState!.Autofire = v, true);
        AutofireCheck.Unchecked += (_, _) => SetBool(v => _state.GameState!.Autofire = v, false);

        CrystalWorldsCheck.Checked += (_, _) => SetBool(v => _state.GameState!.SectorIsHiddenCrystalWorlds = v, true);
        CrystalWorldsCheck.Unchecked += (_, _) => SetBool(v => _state.GameState!.SectorIsHiddenCrystalWorlds = v, false);

        FlagshipVisibleCheck.Checked += (_, _) => SetBool(v => _state.GameState!.RebelFlagshipVisible = v, true);
        FlagshipVisibleCheck.Unchecked += (_, _) => SetBool(v => _state.GameState!.RebelFlagshipVisible = v, false);

        FlagshipMovingCheck.Checked += (_, _) => SetBool(v => _state.GameState!.RebelFlagshipMoving = v, true);
        FlagshipMovingCheck.Unchecked += (_, _) => SetBool(v => _state.GameState!.RebelFlagshipMoving = v, false);

        FlagshipRetreatingCheck.Checked += (_, _) => SetBool(v => _state.GameState!.RebelFlagshipRetreating = v, true);
        FlagshipRetreatingCheck.Unchecked += (_, _) => SetBool(v => _state.GameState!.RebelFlagshipRetreating = v, false);

        FileFormatBox.LostFocus += (_, _) => SetInt(FileFormatBox, v => _state.GameState!.FileFormat = v);
        SectorNumberBox.LostFocus += (_, _) => SetInt(SectorNumberBox, v => _state.GameState!.SectorNumber = v);
        OneBasedSectorBox.LostFocus += (_, _) => SetInt(OneBasedSectorBox, v => _state.GameState!.OneBasedSectorNumber = v);
        CurrentBeaconBox.LostFocus += (_, _) => SetInt(CurrentBeaconBox, v => _state.GameState!.CurrentBeaconId = v);
        TreeSeedBox.LostFocus += (_, _) => SetInt(TreeSeedBox, v => _state.GameState!.SectorTreeSeed = v);
        LayoutSeedBox.LostFocus += (_, _) => SetInt(LayoutSeedBox, v => _state.GameState!.SectorLayoutSeed = v);
        FleetOffsetBox.LostFocus += (_, _) => SetInt(FleetOffsetBox, v => _state.GameState!.RebelFleetOffset = v);
        FleetFudgeBox.LostFocus += (_, _) => SetInt(FleetFudgeBox, v => _state.GameState!.RebelFleetFudge = v);
        PursuitModBox.LostFocus += (_, _) => SetInt(PursuitModBox, v => _state.GameState!.RebelPursuitMod = v);
        FlagshipHopBox.LostFocus += (_, _) => SetInt(FlagshipHopBox, v => _state.GameState!.RebelFlagshipHop = v);
        FlagshipBaseTurnsBox.LostFocus += (_, _) => SetInt(FlagshipBaseTurnsBox, v => _state.GameState!.RebelFlagshipBaseTurns = v);
    }

    private void BuildEnvironmentSection()
    {
        EnvironmentPanel.Children.Clear();
        var env = _state.GameState?.Environment;
        if (env == null)
        {
            EnvironmentPanel.Children.Add(new TextBlock
            {
                Text = "No environment data present",
                Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush"),
                FontStyle = FontStyles.Italic
            });
            return;
        }

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });

        int row = 0;
        AddCheckFieldToGrid(grid, ref row, 0, "Red Giant", env.RedGiantPresent, v => env.RedGiantPresent = v);
        AddCheckFieldToGrid(grid, ref row, 0, "Pulsar", env.PulsarPresent, v => env.PulsarPresent = v);
        AddCheckFieldToGrid(grid, ref row, 0, "PDS", env.PdsPresent, v => env.PdsPresent = v);
        AddCheckFieldToGrid(grid, ref row, 0, "Asteroids", env.AsteroidsPresent, v => env.AsteroidsPresent = v);

        row = 0;
        AddIntFieldToGrid(grid, ref row, 3, "Vulnerable Ships", env.VulnerableShips, v => env.VulnerableShips = v);
        AddIntFieldToGrid(grid, ref row, 3, "Solar Flare Ticks", env.SolarFlareFadeTicks, v => env.SolarFlareFadeTicks = v);
        AddIntFieldToGrid(grid, ref row, 3, "Havoc Ticks", env.HavocTicks, v => env.HavocTicks = v);
        AddIntFieldToGrid(grid, ref row, 3, "PDS Ticks", env.PdsTicks, v => env.PdsTicks = v);

        EnvironmentPanel.Children.Add(grid);

        // Asteroid field details
        if (env.AsteroidField != null)
        {
            var af = env.AsteroidField;
            EnvironmentPanel.Children.Add(new TextBlock
            {
                Text = "Asteroid Field",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = (SolidColorBrush)FindResource("AccentOrangeBrush"),
                Margin = new Thickness(0, 12, 0, 8)
            });

            var afGrid = new Grid();
            afGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
            afGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            afGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
            afGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
            afGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });

            int ar = 0;
            AddIntFieldToGrid(afGrid, ref ar, 0, "Stray Rock Ticks", af.StrayRockTicks, v => af.StrayRockTicks = v);
            AddIntFieldToGrid(afGrid, ref ar, 0, "BG Drift Ticks", af.BgDriftTicks, v => af.BgDriftTicks = v);

            ar = 0;
            AddIntFieldToGrid(afGrid, ref ar, 3, "Current Target", af.CurrentTarget, v => af.CurrentTarget = v);

            EnvironmentPanel.Children.Add(afGrid);
        }
    }

    private void BuildEncounterSection()
    {
        EncounterPanel.Children.Clear();
        var enc = _state.GameState?.Encounter;
        if (enc == null)
        {
            EncounterPanel.Children.Add(new TextBlock
            {
                Text = "No active encounter",
                Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush"),
                FontStyle = FontStyles.Italic
            });
            return;
        }

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        int row = 0;
        AddIntFieldToGrid(grid, ref row, 0, "Ship Event Seed", enc.ShipEventSeed, v => enc.ShipEventSeed = v);
        AddStringFieldToGrid(grid, ref row, 0, "Surrender Event", enc.SurrenderEventId, v => enc.SurrenderEventId = v);
        AddStringFieldToGrid(grid, ref row, 0, "Escape Event", enc.EscapeEventId, v => enc.EscapeEventId = v);
        AddStringFieldToGrid(grid, ref row, 0, "Destroyed Event", enc.DestroyedEventId, v => enc.DestroyedEventId = v);
        AddStringFieldToGrid(grid, ref row, 0, "Dead Crew Event", enc.DeadCrewEventId, v => enc.DeadCrewEventId = v);
        AddStringFieldToGrid(grid, ref row, 0, "Got Away Event", enc.GotAwayEventId, v => enc.GotAwayEventId = v);
        AddStringFieldToGrid(grid, ref row, 0, "Last Event", enc.LastEventId, v => enc.LastEventId = v);
        AddIntFieldToGrid(grid, ref row, 0, "Affected Crew Seed", enc.AffectedCrewSeed, v => enc.AffectedCrewSeed = v);

        EncounterPanel.Children.Add(grid);

        // Encounter text
        if (!string.IsNullOrEmpty(enc.Text))
        {
            EncounterPanel.Children.Add(new TextBlock
            {
                Text = "Encounter Text:",
                Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush"),
                Margin = new Thickness(0, 12, 0, 4),
                FontSize = 12
            });

            var textBox = new TextBox
            {
                Text = enc.Text,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                Height = 80,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            textBox.LostFocus += (_, _) =>
            {
                enc.Text = textBox.Text;
                _state.MarkDirty();
            };
            EncounterPanel.Children.Add(textBox);
        }
    }

    private void BuildFlagshipSection()
    {
        FlagshipPanel.Children.Clear();
        var fs = _state.GameState?.RebelFlagship;
        if (fs == null)
        {
            FlagshipPanel.Children.Add(new TextBlock
            {
                Text = "No flagship state present (only available in the last sector)",
                Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush"),
                FontStyle = FontStyles.Italic
            });
            return;
        }

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });

        int row = 0;
        AddIntFieldToGrid(grid, ref row, 0, "Pending Stage", fs.PendingStage, v => fs.PendingStage = v);

        FlagshipPanel.Children.Add(grid);

        // Previous Occupancy
        if (fs.PreviousOccupancy.Count > 0)
        {
            FlagshipPanel.Children.Add(new TextBlock
            {
                Text = "Previous Occupancy (room crew counts):",
                Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush"),
                Margin = new Thickness(0, 12, 0, 4),
                FontSize = 12
            });

            var occPanel = new WrapPanel { Margin = new Thickness(0, 0, 0, 0) };
            for (int i = 0; i < fs.PreviousOccupancy.Count; i++)
            {
                var occStack = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 0, 16, 4)
                };

                occStack.Children.Add(new TextBlock
                {
                    Text = $"Room {i}:",
                    Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush"),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 4, 0),
                    FontSize = 12
                });

                int capturedI = i;
                var occBox = new TextBox
                {
                    Text = fs.PreviousOccupancy[i].ToString(),
                    Width = 50
                };
                occBox.LostFocus += (_, _) =>
                {
                    if (int.TryParse(occBox.Text, out int v) && capturedI < fs.PreviousOccupancy.Count)
                    {
                        fs.PreviousOccupancy[capturedI] = v;
                        _state.MarkDirty();
                    }
                };
                occStack.Children.Add(occBox);
                occPanel.Children.Add(occStack);
            }
            FlagshipPanel.Children.Add(occPanel);
        }
    }

    private void BuildStatsSection()
    {
        StatsPanel.Children.Clear();
        var gs = _state.GameState;
        if (gs == null) return;

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });

        int row = 0;
        AddIntFieldToGrid(grid, ref row, 0, "Total Ships Defeated", gs.TotalShipsDefeated, v => gs.TotalShipsDefeated = v);
        AddIntFieldToGrid(grid, ref row, 0, "Total Beacons Explored", gs.TotalBeaconsExplored, v => gs.TotalBeaconsExplored = v);
        AddIntFieldToGrid(grid, ref row, 0, "Total Scrap Collected", gs.TotalScrapCollected, v => gs.TotalScrapCollected = v);

        row = 0;
        AddIntFieldToGrid(grid, ref row, 3, "Total Crew Hired", gs.TotalCrewHired, v => gs.TotalCrewHired = v);

        StatsPanel.Children.Add(grid);
    }

    private void SetBool(Action<bool> setter, bool value)
    {
        if (_loading) return;
        setter(value);
        _state.MarkDirty();
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
            Margin = new Thickness(0, 0, 0, 6),
            Width = 100,
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

    private void AddStringFieldToGrid(Grid grid, ref int row, int colOffset, string label, string value, Action<string> setter)
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
            Margin = new Thickness(0, 0, 0, 6)
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
}
