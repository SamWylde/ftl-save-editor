using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FtlSaveEditor.Services;

namespace FtlSaveEditor.Views;

public partial class CargoEditor : UserControl
{
    private readonly SaveEditorState _state = SaveEditorState.Instance;

    public CargoEditor()
    {
        InitializeComponent();
        AddCargoBtn.Click += AddCargo_Click;
        LoadData();
    }

    private void LoadData()
    {
        var gs = _state.GameState;
        if (gs == null) return;

        CargoListPanel.Children.Clear();

        if (gs.CargoIdList.Count == 0)
        {
            EmptyText.Visibility = Visibility.Visible;
            return;
        }

        EmptyText.Visibility = Visibility.Collapsed;

        for (int i = 0; i < gs.CargoIdList.Count; i++)
        {
            CargoListPanel.Children.Add(BuildCargoRow(i));
        }
    }

    private Grid BuildCargoRow(int index)
    {
        var gs = _state.GameState!;

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

        // Cargo ID
        int capturedIndex = index;
        var idBox = new TextBox
        {
            Text = gs.CargoIdList[index],
            VerticalAlignment = VerticalAlignment.Center
        };
        idBox.TextChanged += (_, _) =>
        {
            if (capturedIndex < gs.CargoIdList.Count)
            {
                gs.CargoIdList[capturedIndex] = idBox.Text;
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
            if (capturedIndex >= 0 && capturedIndex < gs.CargoIdList.Count)
            {
                gs.CargoIdList.RemoveAt(capturedIndex);
                _state.MarkDirty();
                LoadData();
            }
        };
        Grid.SetColumn(removeBtn, 2);
        grid.Children.Add(removeBtn);

        return grid;
    }

    private void AddCargo_Click(object sender, RoutedEventArgs e)
    {
        var gs = _state.GameState;
        if (gs == null) return;

        gs.CargoIdList.Add("CARGO_NEW");
        _state.MarkDirty();
        LoadData();
    }
}
