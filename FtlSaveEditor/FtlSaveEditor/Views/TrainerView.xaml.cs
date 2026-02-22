using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FtlSaveEditor.Services;

namespace FtlSaveEditor.Views;

public partial class TrainerView : UserControl
{
    private readonly TrainerService _trainer = TrainerService.Instance;

    public TrainerView()
    {
        InitializeComponent();
        BuildValueRows();
        WireCheatToggles();
        WireQuickActions();
        WireCustomValueControls();
        WireScanner();

        _trainer.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(TrainerService.IsAttached) or nameof(TrainerService.StatusText))
                Dispatcher.Invoke(UpdateConnectionBanner);
        };

        UpdateConnectionBanner();
    }

    private void UpdateConnectionBanner()
    {
        if (_trainer.IsAttached)
        {
            NotAttachedBanner.Visibility = Visibility.Collapsed;
            AttachedBanner.Visibility = Visibility.Visible;
            AttachedStatusText.Text = _trainer.StatusText;
            ResolveMethodLabel.Text = _trainer.DetectedVersion;
        }
        else
        {
            NotAttachedBanner.Visibility = Visibility.Visible;
            AttachedBanner.Visibility = Visibility.Collapsed;
        }
    }

    // ======================== CHEAT TOGGLES ========================

    private void WireCheatToggles()
    {
        GodModeToggle.Checked += (_, _) => _trainer.IsGodMode = true;
        GodModeToggle.Unchecked += (_, _) => _trainer.IsGodMode = false;

        UnlimitedScrapToggle.Checked += (_, _) => _trainer.IsUnlimitedScrap = true;
        UnlimitedScrapToggle.Unchecked += (_, _) => _trainer.IsUnlimitedScrap = false;

        UnlimitedFuelToggle.Checked += (_, _) => _trainer.IsUnlimitedFuel = true;
        UnlimitedFuelToggle.Unchecked += (_, _) => _trainer.IsUnlimitedFuel = false;

        UnlimitedMissilesToggle.Checked += (_, _) => _trainer.IsUnlimitedMissiles = true;
        UnlimitedMissilesToggle.Unchecked += (_, _) => _trainer.IsUnlimitedMissiles = false;

        UnlimitedDronePartsToggle.Checked += (_, _) => _trainer.IsUnlimitedDroneParts = true;
        UnlimitedDronePartsToggle.Unchecked += (_, _) => _trainer.IsUnlimitedDroneParts = false;

        // Sync toggle state from service (e.g., if "Unfreeze All" was clicked)
        _trainer.PropertyChanged += (_, args) =>
        {
            Dispatcher.Invoke(() =>
            {
                if (args.PropertyName == nameof(TrainerService.IsGodMode))
                    GodModeToggle.IsChecked = _trainer.IsGodMode;
                else if (args.PropertyName == nameof(TrainerService.IsUnlimitedScrap))
                    UnlimitedScrapToggle.IsChecked = _trainer.IsUnlimitedScrap;
                else if (args.PropertyName == nameof(TrainerService.IsUnlimitedFuel))
                    UnlimitedFuelToggle.IsChecked = _trainer.IsUnlimitedFuel;
                else if (args.PropertyName == nameof(TrainerService.IsUnlimitedMissiles))
                    UnlimitedMissilesToggle.IsChecked = _trainer.IsUnlimitedMissiles;
                else if (args.PropertyName == nameof(TrainerService.IsUnlimitedDroneParts))
                    UnlimitedDronePartsToggle.IsChecked = _trainer.IsUnlimitedDroneParts;
            });
        };
    }

    // ======================== RESOURCES TABLE ========================

    private void BuildValueRows()
    {
        foreach (var value in _trainer.PresetValues)
        {
            ValuesPanel.Children.Add(BuildValueRow(value, showRemove: false));
        }
    }

    private void WireQuickActions()
    {
        MaxAllBtn.Click += (_, _) => _trainer.MaxAllResources();
        UnfreezeAllBtn.Click += (_, _) => _trainer.UnfreezeAll();
    }

    private UIElement BuildValueRow(TrainedValue value, bool showRemove)
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 6) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
        if (showRemove)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });

        // Name
        var nameLabel = new TextBlock
        {
            Text = value.Name,
            VerticalAlignment = VerticalAlignment.Center,
            ToolTip = value.Description
        };
        Grid.SetColumn(nameLabel, 0);
        grid.Children.Add(nameLabel);

        // Current value (auto-updated)
        var currentLabel = new TextBlock
        {
            Text = "---",
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = (SolidColorBrush)FindResource("AccentGreenBrush"),
            FontWeight = FontWeights.SemiBold,
            FontSize = 14
        };
        value.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(TrainedValue.CurrentValue))
            {
                Dispatcher.Invoke(() =>
                {
                    currentLabel.Text = value.IsResolved ? value.CurrentValue.ToString() : "---";
                    currentLabel.Foreground = (SolidColorBrush)FindResource(
                        value.IsFrozen ? "AccentOrangeBrush" : "AccentGreenBrush");
                });
            }
        };
        Grid.SetColumn(currentLabel, 1);
        grid.Children.Add(currentLabel);

        // Desired value input
        var inputBox = new TextBox
        {
            Width = 90,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };
        inputBox.TextChanged += (_, _) =>
        {
            if (int.TryParse(inputBox.Text, out int v))
                value.DesiredValue = v;
        };
        Grid.SetColumn(inputBox, 2);
        grid.Children.Add(inputBox);

        // Write button
        var writeBtn = new Button
        {
            Content = "Set",
            Style = (Style)FindResource("DarkButton"),
            Padding = new Thickness(8, 3, 8, 3),
            VerticalAlignment = VerticalAlignment.Center
        };
        writeBtn.Click += (_, _) =>
        {
            if (int.TryParse(inputBox.Text, out int v))
            {
                value.DesiredValue = v;
                _trainer.WriteValue(value);
            }
        };
        Grid.SetColumn(writeBtn, 3);
        grid.Children.Add(writeBtn);

        // Freeze checkbox
        var freezeCheck = new CheckBox
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        freezeCheck.Checked += (_, _) =>
        {
            if (int.TryParse(inputBox.Text, out int v))
                value.DesiredValue = v;
            value.IsFrozen = true;
        };
        freezeCheck.Unchecked += (_, _) => value.IsFrozen = false;

        value.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(TrainedValue.IsFrozen))
            {
                Dispatcher.Invoke(() =>
                {
                    currentLabel.Foreground = (SolidColorBrush)FindResource(
                        value.IsFrozen ? "AccentOrangeBrush" : "AccentGreenBrush");
                });
            }
        };

        Grid.SetColumn(freezeCheck, 4);
        grid.Children.Add(freezeCheck);

        // Remove button (for custom values)
        if (showRemove)
        {
            var removeBtn = new Button
            {
                Content = "X",
                Style = (Style)FindResource("DarkButton"),
                Padding = new Thickness(4, 2, 4, 2),
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 10,
                Foreground = (SolidColorBrush)FindResource("AccentRedBrush")
            };
            removeBtn.Click += (_, _) =>
            {
                _trainer.RemoveCustomValue(value);
                RebuildCustomValuesPanel();
            };
            Grid.SetColumn(removeBtn, 5);
            grid.Children.Add(removeBtn);
        }

        return grid;
    }

    // ======================== CUSTOM VALUES ========================

    private void WireCustomValueControls()
    {
        AddCustomValueBtn.Click += (_, _) =>
        {
            var name = CustomNameInput.Text.Trim();
            var addrText = CustomAddressInput.Text.Trim();
            if (addrText.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                addrText = addrText[2..];

            if (string.IsNullOrEmpty(name))
            {
                name = $"Value_{_trainer.CustomValues.Count + 1}";
            }

            if (long.TryParse(addrText, NumberStyles.HexNumber, null, out long addr) && addr > 0)
            {
                _trainer.AddCustomValue(name, (IntPtr)addr);
                RebuildCustomValuesPanel();
                CustomNameInput.Text = "";
                CustomAddressInput.Text = "";
            }
        };
    }

    private void RebuildCustomValuesPanel()
    {
        CustomValuesPanel.Children.Clear();
        foreach (var cv in _trainer.CustomValues)
        {
            CustomValuesPanel.Children.Add(BuildValueRow(cv, showRemove: true));
        }
    }

    // ======================== MEMORY SCANNER ========================

    private void WireScanner()
    {
        FirstScanBtn.Click += async (_, _) =>
        {
            if (!int.TryParse(ScanValueInput.Text, out int targetValue)) return;

            FirstScanBtn.IsEnabled = false;
            NextScanBtn.IsEnabled = false;

            await _trainer.FirstScanAsync(targetValue);

            FirstScanBtn.IsEnabled = true;
            NextScanBtn.IsEnabled = _trainer.ScanCandidateCount > 0;
            ScanStatusLabel.Text = _trainer.ScanStatus;
            RefreshScanResults();
        };

        NextScanBtn.Click += async (_, _) =>
        {
            if (!int.TryParse(ScanValueInput.Text, out int targetValue)) return;

            FirstScanBtn.IsEnabled = false;
            NextScanBtn.IsEnabled = false;

            await _trainer.RefineScanAsync(targetValue);

            FirstScanBtn.IsEnabled = true;
            NextScanBtn.IsEnabled = _trainer.ScanCandidateCount > 0;
            ScanStatusLabel.Text = _trainer.ScanStatus;
            RefreshScanResults();
        };

        ClearScanBtn.Click += (_, _) =>
        {
            _trainer.ClearScan();
            ScanStatusLabel.Text = "";
            ScanResultsBorder.Visibility = Visibility.Collapsed;
            ScanResultsPanel.Children.Clear();
            NextScanBtn.IsEnabled = false;
        };
    }

    private void RefreshScanResults()
    {
        ScanResultsPanel.Children.Clear();

        var results = _trainer.GetScanResults(100);
        if (results.Count == 0)
        {
            ScanResultsBorder.Visibility = Visibility.Collapsed;
            return;
        }

        ScanResultsBorder.Visibility = Visibility.Visible;

        // Header
        var header = new Grid { Margin = new Thickness(0) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        AddScanHeaderText(header, 0, "Address");
        AddScanHeaderText(header, 1, "Current");
        AddScanHeaderText(header, 2, "");
        ScanResultsPanel.Children.Add(header);

        bool alt = false;
        foreach (var (addr, val) in results)
        {
            var row = new Border
            {
                Background = alt ? (SolidColorBrush)FindResource("BgLightBrush") : Brushes.Transparent,
                Padding = new Thickness(0)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var addrText = new TextBlock
            {
                Text = $"0x{addr.ToInt64():X8}",
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Thickness(8, 5, 4, 5)
            };
            Grid.SetColumn(addrText, 0);
            grid.Children.Add(addrText);

            var valText = new TextBlock
            {
                Text = val.ToString(),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = (SolidColorBrush)FindResource("AccentGreenBrush"),
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Thickness(4, 5, 4, 5)
            };
            Grid.SetColumn(valText, 1);
            grid.Children.Add(valText);

            var addBtn = new Button
            {
                Content = "+ Add as Value",
                Style = (Style)FindResource("DarkButton"),
                Padding = new Thickness(8, 2, 8, 2),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(4, 0, 0, 0)
            };
            var capturedAddr = addr;
            addBtn.Click += (_, _) =>
            {
                var name = $"Scan_{capturedAddr.ToInt64():X8}";
                _trainer.AddCustomValue(name, capturedAddr);
                RebuildCustomValuesPanel();
            };
            Grid.SetColumn(addBtn, 2);
            grid.Children.Add(addBtn);

            row.Child = grid;
            ScanResultsPanel.Children.Add(row);
            alt = !alt;
        }

        if (_trainer.ScanCandidateCount > 100)
        {
            ScanResultsPanel.Children.Add(new TextBlock
            {
                Text = $"... and {_trainer.ScanCandidateCount - 100:N0} more (refine scan to narrow results)",
                Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush"),
                FontSize = 11,
                Padding = new Thickness(8, 6, 8, 6)
            });
        }
    }

    private void AddScanHeaderText(Grid grid, int column, string text)
    {
        var tb = new TextBlock
        {
            Text = text,
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush"),
            Padding = new Thickness(8, 4, 4, 4)
        };
        Grid.SetColumn(tb, column);
        grid.Children.Add(tb);
    }
}
