using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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

        // Augment ID
        int capturedIndex = index;
        var idBox = new TextBox
        {
            Text = ship.AugmentIds[index],
            VerticalAlignment = VerticalAlignment.Center
        };
        idBox.TextChanged += (_, _) =>
        {
            if (capturedIndex < ship.AugmentIds.Count)
            {
                ship.AugmentIds[capturedIndex] = idBox.Text;
                _state.MarkDirty();
            }
        };
        Grid.SetColumn(idBox, 1);
        grid.Children.Add(idBox);

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

    private void AddAugment_Click(object sender, RoutedEventArgs e)
    {
        var ship = _state.GameState?.PlayerShip;
        if (ship == null) return;

        ship.AugmentIds.Add("AUG_NEW");
        _state.MarkDirty();
        LoadData();
    }
}
