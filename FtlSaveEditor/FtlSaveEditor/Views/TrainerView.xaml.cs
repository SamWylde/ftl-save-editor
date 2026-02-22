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
        BuildManualAddressRows();

        MaxAllBtn.Click += (_, _) => _trainer.MaxAllResources();
        UnfreezeAllBtn.Click += (_, _) => _trainer.UnfreezeAll();

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
        }
        else
        {
            NotAttachedBanner.Visibility = Visibility.Visible;
            AttachedBanner.Visibility = Visibility.Collapsed;
        }
    }

    private void BuildValueRows()
    {
        foreach (var value in _trainer.AllValues)
        {
            ValuesPanel.Children.Add(BuildValueRow(value));
        }
    }

    private UIElement BuildValueRow(TrainedValue value)
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 6) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });

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

        // Sync freeze checkbox color
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

        return grid;
    }

    private void BuildManualAddressRows()
    {
        foreach (var value in _trainer.AllValues)
        {
            var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };

            sp.Children.Add(new TextBlock
            {
                Text = $"{value.Name}:",
                Width = 100,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush")
            });

            sp.Children.Add(new TextBlock
            {
                Text = "0x",
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush"),
                Margin = new Thickness(0, 0, 2, 0)
            });

            var addrBox = new TextBox { Width = 140 };

            // Show current resolved address as placeholder
            value.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(TrainedValue.CurrentValue) && value.IsResolved && string.IsNullOrEmpty(addrBox.Text))
                {
                    Dispatcher.Invoke(() =>
                    {
                        addrBox.Tag = $"Auto: {value.ResolvedAddress.ToInt64():X}";
                    });
                }
            };

            addrBox.LostFocus += (_, _) =>
            {
                var text = addrBox.Text.Trim();
                if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    text = text[2..];
                if (long.TryParse(text, NumberStyles.HexNumber, null, out long addr))
                {
                    value.ResolvedAddress = (IntPtr)addr;
                }
            };

            sp.Children.Add(addrBox);
            ManualAddressPanel.Children.Add(sp);
        }
    }
}
