using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FtlSaveEditor.Models;
using FtlSaveEditor.Services;

namespace FtlSaveEditor.Views;

public partial class StateVarsEditor : UserControl
{
    private readonly SaveEditorState _state = SaveEditorState.Instance;
    private readonly HashSet<int> _selectedIndices = new();

    public StateVarsEditor()
    {
        InitializeComponent();
        FilterBox.TextChanged += (_, _) => LoadData();
        AddVarBtn.Click += AddVar_Click;
        DeleteSelectedBtn.Click += DeleteSelected_Click;
        LoadData();
    }

    private void LoadData()
    {
        var gs = _state.GameState;
        if (gs == null) return;

        VarListPanel.Children.Clear();
        _selectedIndices.Clear();

        string filter = FilterBox.Text.Trim().ToLowerInvariant();
        int totalCount = gs.StateVars.Count;
        int shownCount = 0;

        for (int i = 0; i < gs.StateVars.Count; i++)
        {
            var sv = gs.StateVars[i];

            // Apply filter
            if (!string.IsNullOrEmpty(filter))
            {
                if (!sv.Key.ToLowerInvariant().Contains(filter) &&
                    !sv.Value.ToString().Contains(filter))
                {
                    continue;
                }
            }

            VarListPanel.Children.Add(BuildVarRow(sv, i, shownCount % 2 == 0));
            shownCount++;
        }

        CountText.Text = string.IsNullOrEmpty(filter)
            ? $"{totalCount} variable(s)"
            : $"Showing {shownCount} of {totalCount} variable(s)";

        EmptyText.Visibility = shownCount == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private Border BuildVarRow(StateVar sv, int originalIndex, bool isEven)
    {
        var bgBrush = isEven
            ? (SolidColorBrush)FindResource("BgMediumBrush")
            : (SolidColorBrush)FindResource("BgDarkBrush");

        var row = new Border
        {
            Background = bgBrush,
            BorderBrush = (SolidColorBrush)FindResource("BorderBrush"),
            BorderThickness = new Thickness(1, 0, 1, 1),
            Padding = new Thickness(12, 4, 12, 4)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });

        // Selection checkbox
        var selectChk = new CheckBox
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        int capturedIdx = originalIndex;
        selectChk.Checked += (_, _) => _selectedIndices.Add(capturedIdx);
        selectChk.Unchecked += (_, _) => _selectedIndices.Remove(capturedIdx);
        Grid.SetColumn(selectChk, 0);
        grid.Children.Add(selectChk);

        // Key
        var keyBox = new TextBox
        {
            Text = sv.Key,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };
        keyBox.LostFocus += (_, _) =>
        {
            sv.Key = keyBox.Text;
            _state.MarkDirty();
        };
        Grid.SetColumn(keyBox, 1);
        grid.Children.Add(keyBox);

        // Value
        var valBox = new TextBox
        {
            Text = sv.Value.ToString(),
            VerticalAlignment = VerticalAlignment.Center,
            Width = 100,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        valBox.LostFocus += (_, _) =>
        {
            if (int.TryParse(valBox.Text, out int v))
            {
                sv.Value = v;
                _state.MarkDirty();
            }
        };
        Grid.SetColumn(valBox, 2);
        grid.Children.Add(valBox);

        row.Child = grid;
        return row;
    }

    private void AddVar_Click(object sender, RoutedEventArgs e)
    {
        var gs = _state.GameState;
        if (gs == null) return;

        gs.StateVars.Add(new StateVar { Key = "NEW_VAR", Value = 0 });
        _state.MarkDirty();
        LoadData();
    }

    private void DeleteSelected_Click(object sender, RoutedEventArgs e)
    {
        var gs = _state.GameState;
        if (gs == null || _selectedIndices.Count == 0) return;

        // Remove from highest index to lowest to preserve indices
        var sorted = _selectedIndices.OrderByDescending(i => i).ToList();
        foreach (var idx in sorted)
        {
            if (idx >= 0 && idx < gs.StateVars.Count)
            {
                gs.StateVars.RemoveAt(idx);
            }
        }

        _state.MarkDirty();
        _selectedIndices.Clear();
        LoadData();
    }
}
