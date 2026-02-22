using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using FtlSaveEditor.Data;
using FtlSaveEditor.Services;

namespace FtlSaveEditor.Views;

public partial class AugmentsEditor : UserControl
{
    private readonly SaveEditorState _state = SaveEditorState.Instance;

    public AugmentsEditor()
    {
        InitializeComponent();
        AddAugmentBtn.Click += AddAugment_Click;
        LoadData();
    }

    private void LoadData()
    {
        var ship = _state.GameState?.PlayerShip;
        if (ship == null) return;

        AugmentListPanel.Children.Clear();

        if (ship.AugmentIds.Count == 0)
        {
            EmptyText.Visibility = Visibility.Visible;
            return;
        }

        EmptyText.Visibility = Visibility.Collapsed;

        for (int i = 0; i < ship.AugmentIds.Count; i++)
        {
            AugmentListPanel.Children.Add(BuildAugmentRow(i));
        }
    }

    private Grid BuildAugmentRow(int index)
    {
        var ship = _state.GameState!.PlayerShip;

        var grid = new Grid { Margin = new Thickness(0, 0, 0, 6) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });

        // Index
        var idxTb = new TextBlock
        {
            Text = index.ToString(),
            Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush"),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(idxTb, 0);
        grid.Children.Add(idxTb);

        // Augment ID (editable ComboBox with suggestions + live filter) + blueprint info
        int capturedIndex = index;
        var idPanel = new StackPanel();
        var cvs = new CollectionViewSource { Source = GetAugmentSuggestions() };
        var idBox = new ComboBox
        {
            IsEditable = true,
            ItemsSource = cvs.View,
            VerticalAlignment = VerticalAlignment.Center
        };
        var infoTb = new TextBlock
        {
            FontSize = 11,
            Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush"),
            Margin = new Thickness(2, 2, 0, 0),
            TextWrapping = TextWrapping.Wrap
        };
        UpdateAugmentInfo(ship.AugmentIds[index], infoTb);
        bool suppressChange = true;
        idBox.AddHandler(System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent,
            new RoutedEventHandler((_, _) =>
            {
                if (suppressChange) return;
                if (capturedIndex < ship.AugmentIds.Count)
                {
                    ship.AugmentIds[capturedIndex] = idBox.Text;
                    _state.MarkDirty();
                    UpdateAugmentInfo(idBox.Text, infoTb);
                }
                var text = idBox.Text;
                cvs.View.Filter = item => ((string)item).Contains(text, StringComparison.OrdinalIgnoreCase);
            }));
        idBox.Text = ship.AugmentIds[index];
        suppressChange = false;
        idPanel.Children.Add(idBox);
        idPanel.Children.Add(infoTb);
        Grid.SetColumn(idPanel, 1);
        grid.Children.Add(idPanel);

        // Remove button
        var removeBtn = new Button
        {
            Content = "X",
            Style = (Style)FindResource("DarkButton"),
            Foreground = (SolidColorBrush)FindResource("AccentRedBrush"),
            Width = 30,
            Height = 26,
            Padding = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        removeBtn.Click += (_, _) =>
        {
            if (capturedIndex >= 0 && capturedIndex < ship.AugmentIds.Count)
            {
                ship.AugmentIds.RemoveAt(capturedIndex);
                _state.MarkDirty();
                LoadData();
            }
        };
        Grid.SetColumn(removeBtn, 2);
        grid.Children.Add(removeBtn);

        return grid;
    }

    private string[] GetAugmentSuggestions()
    {
        var ship = _state.GameState?.PlayerShip;
        var saveIds = ship?.AugmentIds.Where(id => !string.IsNullOrEmpty(id)) ?? [];
        var modIds = _state.Blueprints.Augments.Keys;
        return ItemIds.Augments.Concat(saveIds).Concat(modIds)
            .Distinct().OrderBy(id => id).ToArray();
    }

    private void UpdateAugmentInfo(string id, TextBlock infoTb)
    {
        if (_state.Blueprints.Augments.TryGetValue(id, out var bp))
        {
            var parts = new List<string> { bp.Title };
            if (bp.Cost > 0) parts.Add($"{bp.Cost}scrap");
            if (bp.Stackable) parts.Add("stackable");
            infoTb.Text = string.Join("  |  ", parts);
            infoTb.ToolTip = string.IsNullOrEmpty(bp.Description) ? null : bp.Description;
            infoTb.Visibility = Visibility.Visible;
        }
        else
        {
            infoTb.Text = "";
            infoTb.Visibility = Visibility.Collapsed;
        }
    }

    private void AddAugment_Click(object sender, RoutedEventArgs e)
    {
        var ship = _state.GameState?.PlayerShip;
        if (ship == null) return;

        ship.AugmentIds.Add("AUG_NEW");
        _state.MarkDirty();
        LoadData();
    }
}
