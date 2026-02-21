using System.ComponentModel;
using System.Runtime.CompilerServices;
using FtlSaveEditor.Models;

namespace FtlSaveEditor.Services;

public class SaveEditorState : INotifyPropertyChanged
{
    public static SaveEditorState Instance { get; } = new();

    private SavedGameState? _gameState;
    private string? _filePath;
    private bool _isDirty;
    private string _statusText = "No file loaded";

    public SavedGameState? GameState
    {
        get => _gameState;
        set { _gameState = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasFile)); }
    }

    public string? FilePath
    {
        get => _filePath;
        set { _filePath = value; OnPropertyChanged(); }
    }

    public bool IsDirty
    {
        get => _isDirty;
        set { _isDirty = value; OnPropertyChanged(); }
    }

    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(); }
    }

    public bool HasFile => _gameState != null;

    public void MarkDirty()
    {
        IsDirty = true;
    }

    public void LoadFile(string path)
    {
        var state = FileService.LoadSaveFile(path);
        GameState = state;
        FilePath = path;
        IsDirty = false;

        var fileName = System.IO.Path.GetFileName(path);
        var formatStr = state.FileFormat switch
        {
            2 => "Format 2",
            7 => "Format 7",
            8 => "Format 8",
            9 => "Format 9 (AE)",
            11 => "Format 11 (HS)",
            _ => $"Format {state.FileFormat}"
        };

        var shipName = !string.IsNullOrWhiteSpace(state.PlayerShip.ShipName)
            ? state.PlayerShip.ShipName
            : state.PlayerShipName;
        var modeSuffix = state.ParseMode == SaveParseMode.RestrictedOpaqueTail
            ? " - Restricted mode"
            : "";

        StatusText = $"{fileName} - {formatStr} - {shipName}{modeSuffix}";
    }

    public void SaveFile()
    {
        if (GameState == null || FilePath == null) return;
        FileService.WriteSaveFile(FilePath, GameState);
        IsDirty = false;
    }

    public void SaveFileAs(string path)
    {
        if (GameState == null) return;
        if (!string.IsNullOrWhiteSpace(FilePath) &&
            !string.Equals(FilePath, path, StringComparison.OrdinalIgnoreCase) &&
            System.IO.File.Exists(FilePath))
        {
            // Preserve the currently loaded save before writing to a new destination.
            FileService.CreateBackup(FilePath);
        }

        FileService.WriteSaveFile(path, GameState);
        FilePath = path;
        IsDirty = false;

        var modeSuffix = GameState.ParseMode == SaveParseMode.RestrictedOpaqueTail
            ? " - Restricted mode"
            : "";
        StatusText = $"{System.IO.Path.GetFileName(path)} - Format {GameState.FileFormat}{modeSuffix}";
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
