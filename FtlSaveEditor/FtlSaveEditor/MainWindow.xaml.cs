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
            Text = $"{size} — Modified {file.ModifiedAt:g}",
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
        try
        {
            _state.LoadFile(path);
            OnFileLoaded();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load save file:\n\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnFileLoaded()
    {
        WelcomePanel.Visibility = Visibility.Collapsed;
        EditorPanel.Visibility = Visibility.Visible;
        SaveButton.IsEnabled = true;
        SaveAsButton.IsEnabled = true;
        StatusText.Text = _state.StatusText;

        _state.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(SaveEditorState.IsDirty))
            {
                DirtyIndicator.Visibility = _state.IsDirty ? Visibility.Visible : Visibility.Collapsed;
            }
        };

        BuildSidebar();
        NavigateTo("ship");
    }

    private void BuildSidebar()
    {
        SidebarPanel.Children.Clear();
        _editors.Clear();

        var sections = new (string Key, string Label)[]
        {
            ("ship", "Ship"),
            ("crew", "Crew"),
            ("systems", "Systems"),
            ("weapons", "Weapons"),
            ("drones", "Drones"),
            ("augments", "Augments"),
            ("cargo", "Cargo"),
            ("statevars", "State Variables"),
            ("beacons", "Sector Map"),
            ("misc", "Environment / Misc"),
        };

        _editors["ship"] = () => new ShipEditor();
        _editors["crew"] = () => new CrewEditor();
        _editors["systems"] = () => new SystemsEditor();
        _editors["weapons"] = () => new WeaponsEditor();
        _editors["drones"] = () => new DronesEditor();
        _editors["augments"] = () => new AugmentsEditor();
        _editors["cargo"] = () => new CargoEditor();
        _editors["statevars"] = () => new StateVarsEditor();
        _editors["beacons"] = () => new BeaconsEditor();
        _editors["misc"] = () => new MiscEditor();

        foreach (var (key, label) in sections)
        {
            var btn = new Button
            {
                Content = label,
                Tag = key,
                Style = (Style)FindResource("SidebarButton")
            };
            btn.Click += (_, _) => NavigateTo(key);
            SidebarPanel.Children.Add(btn);
        }
    }

    private void NavigateTo(string section)
    {
        _activeSection = section;

        // Update sidebar highlight
        foreach (Button btn in SidebarPanel.Children)
        {
            btn.Style = (Style)FindResource(
                (string)btn.Tag == section ? "SidebarButtonActive" : "SidebarButton");
        }

        // Show editor
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
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save:\n\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
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
                _state.SaveFile();
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
