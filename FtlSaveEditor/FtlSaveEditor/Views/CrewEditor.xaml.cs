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

        bool isPartial = _state.GameState?.ParseMode == SaveParseMode.PartialPlayerShipOpaqueTail;

        // Show info banner in partial mode
        if (isPartial)
        {
            PartialModeBanner.Visibility = Visibility.Visible;
            PartialModeBannerText.Text =
                "Partial mode: Hyperspace extension data is preserved. " +
                "Vanilla crew fields are editable. HS-specific data shown as read-only below.";
        }

        CrewCountText.Text = $"{ship.Crew.Count} crew member(s)";
        CrewListPanel.Children.Clear();

        // Add Crew button
        var addBtn = new Button
        {
            Content = "Add Crew Member",
            Style = (Style)FindResource("DarkButton"),
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 0, 12)
        };
        addBtn.Click += (_, _) => AddNewCrewMember();
        CrewListPanel.Children.Add(addBtn);

        for (int i = 0; i < ship.Crew.Count; i++)
        {
            var crew = ship.Crew[i];
            CrewListPanel.Children.Add(BuildCrewCard(crew, i));
        }
    }

    private void AddNewCrewMember()
    {
        var ship = _state.GameState?.PlayerShip;
        if (ship == null) return;

        bool isPartial = _state.GameState?.ParseMode == SaveParseMode.PartialPlayerShipOpaqueTail;

        var crew = new CrewState
        {
            Name = "New Crew",
            Race = "human",
            Health = 100,
            PlayerControlled = true,
            RoomId = 0,
            RoomSquare = 0,
            SavedRoomId = 0,
            SavedRoomSquare = 0,
            Male = true,
        };

        if (isPartial)
        {
            // Generate HS inline extension bytes for the new crew member.
            // Use existing crew as template if available, otherwise generate minimal defaults.
            var template = ship.Crew.Count > 0 ? ship.Crew[0] : null;
            BuildHsExtensionForNewCrew(crew, template);
        }

        ship.Crew.Add(crew);
        _state.MarkDirty();
        LoadData();
    }

    private static void BuildHsExtensionForNewCrew(CrewState crew, CrewState? template)
    {
        crew.HsOriginalColorRace = crew.Race;
        crew.HsOriginalRace = crew.Race;
        crew.HealthBoost = crew.Health * 1000;

        if (template != null && template.HsInlinePostStringBytes.Length > 0)
        {
            // Clone the template's post-string byte length, zeroed out for a fresh crew member.
            crew.HsInlinePostStringBytes = new byte[template.HsInlinePostStringBytes.Length];
        }
        else
        {
            // Default: 44 bytes (11 ints) of zeros — matches observed pattern.
            crew.HsInlinePostStringBytes = new byte[44];
        }

        if (template != null && template.HsInlinePreStringBytes.Length > 0)
        {
            // Use the existing crew's pre-string bytes as a template.
            // This preserves the correct sentinel/header values for this HS version.
            // Clone and zero out just the variable-length power/resource data
            // (which for 0 powers / 0 resources means the powerCount and resourceCount
            // ints at the end are already 0 in a zeroed copy, and the header/sentinel
            // bytes at the start match the HS version exactly).
            crew.HsInlinePreStringBytes = (byte[])template.HsInlinePreStringBytes.Clone();
        }
        else
        {
            // No template — generate minimal pre-string bytes (8 bytes: 2 sentinel ints of 0).
            // Then powerCount=0, resourceCount=0. Total = 16 bytes.
            // This is a best-effort fallback; using a template is strongly preferred.
            crew.HsInlinePreStringBytes = new byte[16];
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
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 12, 0)
        });

        // Remove button
        int capturedIndex = index;
        var removeBtn = new Button
        {
            Content = "Remove",
            Style = (Style)FindResource("DarkButton"),
            Foreground = (SolidColorBrush)FindResource("AccentRedBrush"),
            FontSize = 11,
            Padding = new Thickness(8, 2, 8, 2),
            VerticalAlignment = VerticalAlignment.Center
        };
        removeBtn.Click += (_, _) =>
        {
            var ship = _state.GameState?.PlayerShip;
            if (ship == null) return;
            var result = MessageBox.Show(
                $"Remove crew member \"{crew.Name}\" ({crew.Race})?\n\nThis cannot be undone.",
                "Confirm Removal", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;
            if (capturedIndex >= 0 && capturedIndex < ship.Crew.Count)
            {
                ship.Crew.RemoveAt(capturedIndex);
                _state.MarkDirty();
                LoadData();
            }
        };
        headerPanel.Children.Add(removeBtn);

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

        bool isHs = crew.HsInlinePreStringBytes.Length > 0;

        row = 0;
        AddIntFieldRow(basicGrid, ref row, 3, "Health", crew.Health, v =>
        {
            crew.Health = v;
            // Hyperspace uses HealthBoost as actual HP * 1000
            if (isHs) crew.HealthBoost = v * 1000;
        });
        if (!isHs)
            AddIntFieldRow(basicGrid, ref row, 3, "Health Boost", crew.HealthBoost, v => crew.HealthBoost = v);

        content.Children.Add(basicGrid);
        content.Children.Add(CreateSeparator());

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
        AddSkillRow(skillGrid, sRow++, "Pilot", crew.PilotSkill, v => crew.PilotSkill = v);
        AddSkillRow(skillGrid, sRow++, "Engine", crew.EngineSkill, v => crew.EngineSkill = v);
        AddSkillRow(skillGrid, sRow++, "Shield", crew.ShieldSkill, v => crew.ShieldSkill = v);
        AddSkillRow(skillGrid, sRow++, "Weapon", crew.WeaponSkill, v => crew.WeaponSkill = v);
        AddSkillRow(skillGrid, sRow++, "Repair", crew.RepairSkill, v => crew.RepairSkill = v);
        AddSkillRow(skillGrid, sRow++, "Combat", crew.CombatSkill, v => crew.CombatSkill = v);

        content.Children.Add(skillGrid);
        content.Children.Add(CreateSeparator());

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
        content.Children.Add(CreateSeparator());

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
        if (isHs)
        {
            // In HS mode, show safe fields; healthBoost and damageBoost are repurposed by HS
            AddIntFieldRow(statsGrid, ref stRow, 3, "Stun Ticks (HS)", crew.StunTicks, v => crew.StunTicks = v);
            AddIntFieldRow(statsGrid, ref stRow, 3, "Clonebay Prio (HS)", crew.ClonebayPriority, v => crew.ClonebayPriority = v);
            AddIntFieldRow(statsGrid, ref stRow, 3, "Death Count (HS)", crew.UniversalDeathCount, v => crew.UniversalDeathCount = v);
        }
        else
        {
            AddIntFieldRow(statsGrid, ref stRow, 3, "Stun Ticks", crew.StunTicks, v => crew.StunTicks = v);
            AddIntFieldRow(statsGrid, ref stRow, 3, "Damage Boost", crew.DamageBoost, v => crew.DamageBoost = v);
            AddIntFieldRow(statsGrid, ref stRow, 3, "Clonebay Priority", crew.ClonebayPriority, v => crew.ClonebayPriority = v);
            AddIntFieldRow(statsGrid, ref stRow, 3, "Death Count", crew.UniversalDeathCount, v => crew.UniversalDeathCount = v);
        }

        content.Children.Add(statsGrid);

        // HS Extension Data
        if (isHs)
        {
            content.Children.Add(CreateSeparator());
            content.Children.Add(CreateSectionHeader("HYPERSPACE EXTENSION DATA", "AccentOrangeBrush"));
            content.Children.Add(BuildHsExtensionSection(crew));
        }

        expander.Content = content;
        card.Child = expander;
        return card;
    }

    private UIElement BuildHsExtensionSection(CrewState crew)
    {
        var panel = new StackPanel();

        // Editable string fields
        var strGrid = new Grid();
        strGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
        strGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });

        int sRow = 0;
        AddFieldRow(strGrid, ref sRow, 0, "Orig Color Race", crew.HsOriginalColorRace, v => crew.HsOriginalColorRace = v);
        AddFieldRow(strGrid, ref sRow, 0, "Orig Race", crew.HsOriginalRace, v => crew.HsOriginalRace = v);
        panel.Children.Add(strGrid);

        // Pre-string bytes as editable int32 fields
        if (crew.HsInlinePreStringBytes.Length >= 4)
        {
            var pre = crew.HsInlinePreStringBytes;
            int numInts = pre.Length / 4;

            panel.Children.Add(new TextBlock
            {
                Text = $"PRE-STRING DATA ({pre.Length} bytes, {numInts} ints)",
                FontSize = 10, FontWeight = FontWeights.SemiBold,
                Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush"),
                Margin = new Thickness(0, 8, 0, 4)
            });

            var preGrid = new Grid();
            preGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            preGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            preGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
            preGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            preGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });

            int pRow = 0;
            for (int i = 0; i < numInts; i++)
            {
                int offset = i * 4;
                int colOff = (i % 2 == 0) ? 0 : 3;
                AddByteArrayIntField(preGrid, ref pRow, colOff, $"Int[{i}]",
                    pre, offset);
            }
            panel.Children.Add(preGrid);
        }

        // Post-string bytes as editable int32 fields
        if (crew.HsInlinePostStringBytes.Length >= 4)
        {
            var post = crew.HsInlinePostStringBytes;
            int numInts = post.Length / 4;

            panel.Children.Add(new TextBlock
            {
                Text = $"POST-STRING DATA ({post.Length} bytes, {numInts} ints)",
                FontSize = 10, FontWeight = FontWeights.SemiBold,
                Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush"),
                Margin = new Thickness(0, 8, 0, 4)
            });

            var postGrid = new Grid();
            postGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            postGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            postGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
            postGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            postGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });

            int ptRow = 0;
            for (int i = 0; i < numInts; i++)
            {
                int offset = i * 4;
                int colOff = (i % 2 == 0) ? 0 : 3;
                AddByteArrayIntField(postGrid, ref ptRow, colOff, $"Int[{i}]",
                    post, offset);
            }
            panel.Children.Add(postGrid);
        }

        return panel;
    }

    private void AddByteArrayIntField(Grid grid, ref int row, int colOffset, string label,
        byte[] data, int byteOffset)
    {
        while (grid.RowDefinitions.Count <= row)
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var lbl = new Label
        {
            Content = label,
            Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush"),
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 11
        };
        Grid.SetRow(lbl, row);
        Grid.SetColumn(lbl, colOffset);
        grid.Children.Add(lbl);

        int currentValue = BitConverter.ToInt32(data, byteOffset);
        var box = new TextBox
        {
            Text = currentValue.ToString(),
            Margin = new Thickness(0, 0, 0, 4),
            FontSize = 11
        };
        box.LostFocus += (_, _) =>
        {
            if (int.TryParse(box.Text, out int v))
            {
                BitConverter.GetBytes(v).CopyTo(data, byteOffset);
                _state.MarkDirty();
            }
        };
        Grid.SetRow(box, row);
        Grid.SetColumn(box, colOffset + 1);
        grid.Children.Add(box);

        // Only advance row when we've placed in the right column
        if (colOffset > 0) row++;
    }

    private Separator CreateSeparator()
    {
        return new Separator
        {
            Background = (SolidColorBrush)FindResource("BorderBrush"),
            Margin = new Thickness(0, 12, 0, 12)
        };
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

        var headers = new[] { ("Skill", 0), ("Level", 1) };
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
        int skillValue, Action<int> skillSetter)
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
    }
}
