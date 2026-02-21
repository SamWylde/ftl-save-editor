using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FtlSaveEditor.Models;
using FtlSaveEditor.Services;
using FtlSaveEditor.Views;
using Microsoft.Win32;

namespace FtlSaveEditor;

public partial class MainWindow : Window
{
    private readonly SaveEditorState _state = SaveEditorState.Instance;
    private string? _activeSection;
    private readonly Dictionary<string, Func<UserControl>> _editors = new();
    private bool _stateEventsHooked;

    public MainWindow()
    {
        InitializeComponent();
        DetectSaveFiles();
    }

    private void DetectSaveFiles()
    {
        var files = FileService.DetectSaveFiles();
        if (files.Count == 0)
        {
            DetectedFilesPanel.Children.Add(new TextBlock
            {
                Text = "No FTL save files found in the default location.",
                Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush"),
                Margin = new Thickness(0, 0, 0, 8)
            });
            return;
        }

        foreach (var file in files)
        {
            var card = CreateSaveFileCard(file);
            DetectedFilesPanel.Children.Add(card);
        }
    }

    private Border CreateSaveFileCard(DetectedSaveFile file)
    {
        var border = new Border
        {
            Background = (SolidColorBrush)FindResource("BgLightBrush"),
            BorderBrush = (SolidColorBrush)FindResource("BorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(16, 12, 16, 12),
            Margin = new Thickness(0, 0, 0, 8),
            Cursor = System.Windows.Input.Cursors.Hand
        };

        var stack = new StackPanel();

        var header = new StackPanel { Orientation = Orientation.Horizontal };
        header.Children.Add(new TextBlock
        {
            Text = file.DisplayName,
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            Foreground = (SolidColorBrush)FindResource("AccentBlueBrush")
        });
        stack.Children.Add(header);

        var size = file.SizeBytes > 1024 ? $"{file.SizeBytes / 1024} KB" : $"{file.SizeBytes} B";
        stack.Children.Add(new TextBlock
        {
            Text = $"{size} - Modified {file.ModifiedAt:g}",
            Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush"),
            FontSize = 12,
            Margin = new Thickness(0, 4, 0, 0)
        });

        border.Child = stack;
        border.MouseLeftButtonUp += (_, _) => LoadFile(file.Path);
        border.MouseEnter += (_, _) => border.Background = (SolidColorBrush)FindResource("BgLighterBrush");
        border.MouseLeave += (_, _) => border.Background = (SolidColorBrush)FindResource("BgLightBrush");

        return border;
    }

    private void OpenFile_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Open FTL Save File",
            Filter = "FTL Save Files (*.sav)|*.sav|All Files (*.*)|*.*",
            InitialDirectory = FileService.GetFtlSaveDirectory() ?? ""
        };

        if (dlg.ShowDialog() == true)
        {
            LoadFile(dlg.FileName);
        }
    }

    private void LoadFile(string path)
    {
        if (_state.IsDirty)
        {
            var confirmResult = MessageBox.Show(
                "You have unsaved changes. Continue without saving?",
                "Unsaved Changes", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirmResult != MessageBoxResult.Yes) return;
        }

        try
        {
            _state.LoadFile(path);
            OnFileLoaded();
            ShowParseWarningsIfAny();
        }
        catch (System.IO.IOException ioEx)
        {
            MessageBox.Show(
                $"Cannot open — the file may be in use by FTL or another program.\n\n{ioEx.Message}",
                "File In Use", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load save file:\n\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnFileLoaded()
    {
        if (_state.GameState == null)
        {
            return;
        }

        WelcomePanel.Visibility = Visibility.Collapsed;
        EditorPanel.Visibility = Visibility.Visible;
        SaveButton.IsEnabled = true;
        SaveAsButton.IsEnabled = true;
        StatusText.Text = _state.StatusText;

        if (!_stateEventsHooked)
        {
            _state.PropertyChanged += StateOnPropertyChanged;
            _stateEventsHooked = true;
        }

        UpdateParseModeBanner(_state.GameState);
        var firstSection = BuildSidebar(_state.GameState.Capabilities);
        if (firstSection != null)
        {
            NavigateTo(firstSection);
        }
    }

    private void StateOnPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(SaveEditorState.IsDirty))
        {
            DirtyIndicator.Visibility = _state.IsDirty ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void UpdateParseModeBanner(SavedGameState gameState)
    {
        if (gameState.ParseMode == SaveParseMode.RestrictedOpaqueTail)
        {
            ParseModeBanner.Visibility = Visibility.Visible;
            ParseModeBannerText.Text = "Restricted mode: unsupported sections are preserved as opaque bytes for safe round-trip saving.";
            return;
        }

        if (gameState.ParseWarnings.Count > 0)
        {
            ParseModeBanner.Visibility = Visibility.Visible;
            ParseModeBannerText.Text = gameState.ParseWarnings[0];
            return;
        }

        ParseModeBanner.Visibility = Visibility.Collapsed;
        ParseModeBannerText.Text = "";
    }

    private string? BuildSidebar(EditorCapability capabilities)
    {
        SidebarPanel.Children.Clear();
        _editors.Clear();

        var sections = new (string Key, string Label, EditorCapability Capability, Func<UserControl> Factory)[]
        {
            ("metadata", "Metadata", EditorCapability.Metadata, () => new MetadataEditor()),
            ("ship", "Ship", EditorCapability.Ship, () => new ShipEditor()),
            ("crew", "Crew", EditorCapability.Crew, () => new CrewEditor()),
            ("systems", "Systems", EditorCapability.Systems, () => new SystemsEditor()),
            ("weapons", "Weapons", EditorCapability.Weapons, () => new WeaponsEditor()),
            ("drones", "Drones", EditorCapability.Drones, () => new DronesEditor()),
            ("augments", "Augments", EditorCapability.Augments, () => new AugmentsEditor()),
            ("cargo", "Cargo", EditorCapability.Cargo, () => new CargoEditor()),
            ("statevars", "State Variables", EditorCapability.StateVars, () => new StateVarsEditor()),
            ("beacons", "Sector Map", EditorCapability.Beacons, () => new BeaconsEditor()),
            ("misc", "Environment / Misc", EditorCapability.Misc, () => new MiscEditor()),
        };

        string? firstSection = null;
        foreach (var (key, label, capability, factory) in sections)
        {
            if ((capabilities & capability) == 0)
            {
                continue;
            }

            _editors[key] = factory;
            if (firstSection == null)
            {
                firstSection = key;
            }

            var btn = new Button
            {
                Content = label,
                Tag = key,
                Style = (Style)FindResource("SidebarButton")
            };
            btn.Click += (_, _) => NavigateTo(key);
            SidebarPanel.Children.Add(btn);
        }

        // Help tab — always visible regardless of parse mode
        _editors["help"] = () => new HelpView();
        var helpSep = new Separator
        {
            Background = (SolidColorBrush)FindResource("BorderBrush"),
            Margin = new Thickness(12, 8, 12, 8)
        };
        SidebarPanel.Children.Add(helpSep);
        var helpBtn = new Button
        {
            Content = "Help / Info",
            Tag = "help",
            Style = (Style)FindResource("SidebarButton")
        };
        helpBtn.Click += (_, _) => NavigateTo("help");
        SidebarPanel.Children.Add(helpBtn);

        return firstSection;
    }

    private void ShowParseWarningsIfAny()
    {
        var gs = _state.GameState;
        if (gs == null || gs.ParseWarnings.Count == 0)
        {
            return;
        }

        // Partial mode for HS/MV saves is expected — the banner is sufficient.
        // Only show a popup for unexpected modes (restricted fallback).
        if (gs.ParseMode == SaveParseMode.RestrictedOpaqueTail)
        {
            var primaryWarning = gs.ParseWarnings[0];
            var diagnosticPath = gs.ParseDiagnostics.Count > 0 ? gs.ParseDiagnostics[0].LogPath : null;

            var warningText = diagnosticPath == null
                ? primaryWarning
                : $"{primaryWarning}\n\nDiagnostic log:\n{diagnosticPath}";

            MessageBox.Show(warningText, "Loaded With Warnings",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void NavigateTo(string section)
    {
        _activeSection = section;

        foreach (var child in SidebarPanel.Children)
        {
            if (child is not Button btn) continue;
            btn.Style = (Style)FindResource(
                (string)btn.Tag == section ? "SidebarButtonActive" : "SidebarButton");
        }

        EditorPanel.Children.Clear();
        if (_editors.TryGetValue(section, out var factory))
        {
            EditorPanel.Children.Add(factory());
        }
    }

    private void SaveFile_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _state.SaveFile();
            StatusText.Text = $"Saved to {_state.FilePath}";
        }
        catch (System.IO.IOException ioEx)
        {
            MessageBox.Show(
                $"Cannot save — the file may be in use by FTL or another program.\n\n{ioEx.Message}",
                "File In Use", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save:\n\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveFileAs_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog
        {
            Title = "Save FTL Save File As",
            Filter = "FTL Save Files (*.sav)|*.sav|All Files (*.*)|*.*",
            InitialDirectory = FileService.GetFtlSaveDirectory() ?? ""
        };

        if (dlg.ShowDialog() == true)
        {
            try
            {
                _state.SaveFileAs(dlg.FileName);
                StatusText.Text = $"Saved to {dlg.FileName}";
            }
            catch (System.IO.IOException ioEx)
            {
                MessageBox.Show(
                    $"Cannot save — the file may be in use by FTL or another program.\n\n{ioEx.Message}",
                    "File In Use", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save:\n\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (_state.IsDirty)
        {
            var result = MessageBox.Show(
                "You have unsaved changes. Do you want to save before closing?",
                "Unsaved Changes",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _state.SaveFile();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save:\n\n{ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    e.Cancel = true;
                    return;
                }
            }
            else if (result == MessageBoxResult.Cancel)
            {
                e.Cancel = true;
                return;
            }
        }

        base.OnClosing(e);
    }
}
