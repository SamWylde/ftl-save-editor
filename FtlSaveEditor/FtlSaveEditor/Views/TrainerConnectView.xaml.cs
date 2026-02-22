using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FtlSaveEditor.Services;

namespace FtlSaveEditor.Views;

public partial class TrainerConnectView : UserControl
{
    private readonly TrainerService _trainer = TrainerService.Instance;

    public TrainerConnectView()
    {
        InitializeComponent();

        // Populate refresh rate dropdown
        RefreshRateCombo.Items.Add(new ComboBoxItem { Content = "100ms (fast)", Tag = 100 });
        RefreshRateCombo.Items.Add(new ComboBoxItem { Content = "250ms (default)", Tag = 250 });
        RefreshRateCombo.Items.Add(new ComboBoxItem { Content = "500ms (slow)", Tag = 500 });
        RefreshRateCombo.SelectedIndex = 1;

        RefreshRateCombo.SelectionChanged += (_, _) =>
        {
            if (RefreshRateCombo.SelectedItem is ComboBoxItem item && item.Tag is int ms)
                _trainer.RefreshIntervalMs = ms;
        };

        AttachBtn.Click += async (_, _) =>
        {
            AttachBtn.IsEnabled = false;
            await _trainer.TryAttachAsync();
            UpdateUi();
        };

        DetachBtn.Click += (_, _) =>
        {
            _trainer.Detach();
            SmartResolveStatus.Text = "";
            UpdateUi();
        };

        SmartResolveBtn.Click += async (_, _) =>
        {
            SmartResolveBtn.IsEnabled = false;
            SmartResolveStatus.Text = "Scanning...";
            SmartResolveStatus.Foreground = (SolidColorBrush)FindResource("AccentOrangeBrush");

            var result = await _trainer.SmartResolveFromSaveAsync();

            SmartResolveStatus.Text = result.Summary;
            SmartResolveStatus.Foreground = (SolidColorBrush)FindResource(
                result.Success ? "AccentGreenBrush" : "AccentRedBrush");
            SmartResolveBtn.IsEnabled = _trainer.CanSmartResolve;
            UpdateUi();
        };

        _trainer.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(TrainerService.IsAttached)
                or nameof(TrainerService.StatusText)
                or nameof(TrainerService.DetectedVersion)
                or nameof(TrainerService.CanSmartResolve))
                Dispatcher.Invoke(UpdateUi);
        };

        SaveEditorState.Instance.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(SaveEditorState.HasFile))
                Dispatcher.Invoke(UpdateUi);
        };

        UpdateUi();
    }

    private void UpdateUi()
    {
        StatusLabel.Text = _trainer.StatusText;
        VersionLabel.Text = _trainer.DetectedVersion;
        StatusDot.Fill = _trainer.IsAttached
            ? (SolidColorBrush)FindResource("AccentGreenBrush")
            : (SolidColorBrush)FindResource("AccentRedBrush");
        AttachBtn.IsEnabled = !_trainer.IsAttached;
        DetachBtn.IsEnabled = _trainer.IsAttached;
        SmartResolveBtn.IsEnabled = _trainer.CanSmartResolve && !_trainer.IsSmartResolving;
    }
}
