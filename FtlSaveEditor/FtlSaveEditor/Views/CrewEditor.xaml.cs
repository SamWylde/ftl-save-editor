using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FtlSaveEditor.Models;
using FtlSaveEditor.Services;

namespace FtlSaveEditor.Views;

public partial class CrewEditor : UserControl
{
    private readonly SaveEditorState _state = SaveEditorState.Instance;

    public CrewEditor()
    {
        InitializeComponent();
        LoadData();
    }

    private void LoadData()
    {
        var ship = _state.GameState?.PlayerShip;
        if (ship == null) return;

        CrewCountText.Text = $"{ship.Crew.Count} crew member(s)";
        CrewListPanel.Children.Clear();

        for (int i = 0; i < ship.Crew.Count; i++)
        {
            var crew = ship.Crew[i];
            CrewListPanel.Children.Add(BuildCrewCard(crew, i));
        }
    }

    private Border BuildCrewCard(CrewState crew, int index)
    {
        var card = new Border
        {
            Background = (SolidColorBrush)FindResource("BgMediumBrush"),
            BorderBrush = (SolidColorBrush)FindResource("BorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Margin = new Thickness(0, 0, 0, 8)
        };

        var expander = new Expander
        {
            IsExpanded = false,
            Padding = new Thickness(16, 8, 16, 8),
            Foreground = (SolidColorBrush)FindResource("TextPrimaryBrush"),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0)
        };

        // Header
        var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };
        headerPanel.Children.Add(new TextBlock
        {
            Text = $"#{index}",
            Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush"),
            FontSize = 13,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        });
        headerPanel.Children.Add(new TextBlock
        {
            Text = string.IsNullOrEmpty(crew.Name) ? "(unnamed)" : crew.Name,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = (SolidColorBrush)FindResource("TextPrimaryBrush"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 12, 0)
        });
        headerPanel.Children.Add(new TextBlock
        {
            Text = crew.Race,
            FontSize = 12,
            Foreground = (SolidColorBrush)FindResource("AccentBlueBrush"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 12, 0)
        });
        headerPanel.Children.Add(new TextBlock
        {
            Text = $"HP: {crew.Health}",
            FontSize = 12,
            Foreground = (SolidColorBrush)FindResource("AccentGreenBrush"),
            VerticalAlignment = VerticalAlignment.Center
        });

        expander.Header = headerPanel;

        // Content
        var content = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };

        // Basic Info Section
        content.Children.Add(CreateSectionHeader("BASIC INFO", "AccentBlueBrush"));

        var basicGrid = new Grid();
        basicGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
        basicGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
        basicGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
        basicGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
        basicGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });

        int row = 0;
        AddFieldRow(basicGrid, ref row, 0, "Name", crew.Name, v => crew.Name = v);
        AddFieldRow(basicGrid, ref row, 0, "Race", crew.Race, v => crew.Race = v);

        row = 0;
        AddIntFieldRow(basicGrid, ref row, 3, "Health", crew.Health, v => crew.Health = v);
        AddIntFieldRow(basicGrid, ref row, 3, "Health Boost", crew.HealthBoost, v => crew.HealthBoost = v);

        content.Children.Add(basicGrid);
        content.Children.Add(new Separator
        {
            Background = (SolidColorBrush)FindResource("BorderBrush"),
            Margin = new Thickness(0, 12, 0, 12)
        });

        // Skills Section
        content.Children.Add(CreateSectionHeader("SKILLS", "AccentGreenBrush"));

        var skillGrid = new Grid { Margin = new Thickness(0, 0, 0, 4) };
        skillGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
        skillGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        skillGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
        skillGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
        skillGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });

        // Header row
        AddSkillHeader(skillGrid);

        // Skill rows
        int sRow = 1;
        AddSkillRow(skillGrid, sRow++, "Pilot", crew.PilotSkill, v => crew.PilotSkill = v,
            crew.PilotMasteryOne, v => crew.PilotMasteryOne = v,
            crew.PilotMasteryTwo, v => crew.PilotMasteryTwo = v);
        AddSkillRow(skillGrid, sRow++, "Engine", crew.EngineSkill, v => crew.EngineSkill = v,
            crew.EngineMasteryOne, v => crew.EngineMasteryOne = v,
            crew.EngineMasteryTwo, v => crew.EngineMasteryTwo = v);
        AddSkillRow(skillGrid, sRow++, "Shield", crew.ShieldSkill, v => crew.ShieldSkill = v,
            crew.ShieldMasteryOne, v => crew.ShieldMasteryOne = v,
            crew.ShieldMasteryTwo, v => crew.ShieldMasteryTwo = v);
        AddSkillRow(skillGrid, sRow++, "Weapon", crew.WeaponSkill, v => crew.WeaponSkill = v,
            crew.WeaponMasteryOne, v => crew.WeaponMasteryOne = v,
            crew.WeaponMasteryTwo, v => crew.WeaponMasteryTwo = v);
        AddSkillRow(skillGrid, sRow++, "Repair", crew.RepairSkill, v => crew.RepairSkill = v,
            crew.RepairMasteryOne, v => crew.RepairMasteryOne = v,
            crew.RepairMasteryTwo, v => crew.RepairMasteryTwo = v);
        AddSkillRow(skillGrid, sRow++, "Combat", crew.CombatSkill, v => crew.CombatSkill = v,
            crew.CombatMasteryOne, v => crew.CombatMasteryOne = v,
            crew.CombatMasteryTwo, v => crew.CombatMasteryTwo = v);

        content.Children.Add(skillGrid);
        content.Children.Add(new Separator
        {
            Background = (SolidColorBrush)FindResource("BorderBrush"),
            Margin = new Thickness(0, 12, 0, 12)
        });

        // Position Section
        content.Children.Add(CreateSectionHeader("POSITION & STATE", "AccentOrangeBrush"));

        var posGrid = new Grid();
        posGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
        posGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
        posGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
        posGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
        posGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });

        int pRow = 0;
        AddIntFieldRow(posGrid, ref pRow, 0, "Room ID", crew.RoomId, v => crew.RoomId = v);
        AddIntFieldRow(posGrid, ref pRow, 0, "Room Square", crew.RoomSquare, v => crew.RoomSquare = v);
        AddIntFieldRow(posGrid, ref pRow, 0, "Saved Room ID", crew.SavedRoomId, v => crew.SavedRoomId = v);
        AddIntFieldRow(posGrid, ref pRow, 0, "Saved Room Sq", crew.SavedRoomSquare, v => crew.SavedRoomSquare = v);
        AddIntFieldRow(posGrid, ref pRow, 0, "Sprite X", crew.SpriteX, v => crew.SpriteX = v);
        AddIntFieldRow(posGrid, ref pRow, 0, "Sprite Y", crew.SpriteY, v => crew.SpriteY = v);

        pRow = 0;
        AddCheckFieldRow(posGrid, ref pRow, 3, "Player Controlled", crew.PlayerControlled, v => crew.PlayerControlled = v);
        AddCheckFieldRow(posGrid, ref pRow, 3, "Mind Controlled", crew.MindControlled, v => crew.MindControlled = v);
        AddCheckFieldRow(posGrid, ref pRow, 3, "Enemy Drone", crew.EnemyBoardingDrone, v => crew.EnemyBoardingDrone = v);
        AddCheckFieldRow(posGrid, ref pRow, 3, "Male", crew.Male, v => crew.Male = v);
        AddIntFieldRow(posGrid, ref pRow, 3, "Clone Ready", crew.CloneReady, v => crew.CloneReady = v);
        AddIntFieldRow(posGrid, ref pRow, 3, "Death Order", crew.DeathOrder, v => crew.DeathOrder = v);

        content.Children.Add(posGrid);
        content.Children.Add(new Separator
        {
            Background = (SolidColorBrush)FindResource("BorderBrush"),
            Margin = new Thickness(0, 12, 0, 12)
        });

        // Stats Section
        content.Children.Add(CreateSectionHeader("STATS & MISCELLANEOUS", "AccentBlueBrush"));

        var statsGrid = new Grid();
        statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
        statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
        statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
        statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
        statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });

        int stRow = 0;
        AddIntFieldRow(statsGrid, ref stRow, 0, "Repairs", crew.Repairs, v => crew.Repairs = v);
        AddIntFieldRow(statsGrid, ref stRow, 0, "Combat Kills", crew.CombatKills, v => crew.CombatKills = v);
        AddIntFieldRow(statsGrid, ref stRow, 0, "Piloted Evasions", crew.PilotedEvasions, v => crew.PilotedEvasions = v);
        AddIntFieldRow(statsGrid, ref stRow, 0, "Jumps Survived", crew.JumpsSurvived, v => crew.JumpsSurvived = v);
        AddIntFieldRow(statsGrid, ref stRow, 0, "Masteries Earned", crew.SkillMasteriesEarned, v => crew.SkillMasteriesEarned = v);

        stRow = 0;
        AddIntFieldRow(statsGrid, ref stRow, 3, "Stun Ticks", crew.StunTicks, v => crew.StunTicks = v);
        AddIntFieldRow(statsGrid, ref stRow, 3, "Damage Boost", crew.DamageBoost, v => crew.DamageBoost = v);
        AddIntFieldRow(statsGrid, ref stRow, 3, "Clonebay Priority", crew.ClonebayPriority, v => crew.ClonebayPriority = v);
        AddIntFieldRow(statsGrid, ref stRow, 3, "Death Count", crew.UniversalDeathCount, v => crew.UniversalDeathCount = v);

        content.Children.Add(statsGrid);

        expander.Content = content;
        card.Child = expander;
        return card;
    }

    private TextBlock CreateSectionHeader(string text, string colorKey)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = (SolidColorBrush)FindResource(colorKey),
            Margin = new Thickness(0, 0, 0, 8)
        };
    }

    private void AddFieldRow(Grid grid, ref int row, int colOffset, string label, string value, Action<string> setter)
    {
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

    private void AddIntFieldRow(Grid grid, ref int row, int colOffset, string label, int value, Action<int> setter)
    {
        // Only add row definition if we are on column 0 or the row doesn't exist yet
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

    private void AddCheckFieldRow(Grid grid, ref int row, int colOffset, string label, bool value, Action<bool> setter)
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

    private void AddSkillHeader(Grid grid)
    {
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var headers = new[] { ("Skill", 0), ("Level", 1), ("", 2), ("Mastery 1", 3), ("Mastery 2", 4) };
        foreach (var (text, col) in headers)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = 11,
                Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush"),
                Margin = new Thickness(0, 0, 0, 4),
                FontWeight = FontWeights.SemiBold
            };
            Grid.SetRow(tb, 0);
            Grid.SetColumn(tb, col);
            grid.Children.Add(tb);
        }
    }

    private void AddSkillRow(Grid grid, int row, string name,
        int skillValue, Action<int> skillSetter,
        bool mastery1, Action<bool> mastery1Setter,
        bool mastery2, Action<bool> mastery2Setter)
    {
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var lbl = new TextBlock
        {
            Text = name,
            Foreground = (SolidColorBrush)FindResource("TextPrimaryBrush"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 4)
        };
        Grid.SetRow(lbl, row);
        Grid.SetColumn(lbl, 0);
        grid.Children.Add(lbl);

        var box = new TextBox
        {
            Text = skillValue.ToString(),
            Width = 60,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 0, 4)
        };
        box.LostFocus += (_, _) =>
        {
            if (int.TryParse(box.Text, out int v))
            {
                skillSetter(v);
                _state.MarkDirty();
            }
        };
        Grid.SetRow(box, row);
        Grid.SetColumn(box, 1);
        grid.Children.Add(box);

        var chk1 = new CheckBox
        {
            IsChecked = mastery1,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 4)
        };
        chk1.Checked += (_, _) => { mastery1Setter(true); _state.MarkDirty(); };
        chk1.Unchecked += (_, _) => { mastery1Setter(false); _state.MarkDirty(); };
        Grid.SetRow(chk1, row);
        Grid.SetColumn(chk1, 3);
        grid.Children.Add(chk1);

        var chk2 = new CheckBox
        {
            IsChecked = mastery2,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 4)
        };
        chk2.Checked += (_, _) => { mastery2Setter(true); _state.MarkDirty(); };
        chk2.Unchecked += (_, _) => { mastery2Setter(false); _state.MarkDirty(); };
        Grid.SetRow(chk2, row);
        Grid.SetColumn(chk2, 4);
        grid.Children.Add(chk2);
    }
}
