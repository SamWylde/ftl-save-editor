using System.IO;
using FtlSaveEditor.Models;
using FtlSaveEditor.SaveFile;

namespace FtlSaveEditor.Services;

public class DetectedSaveFile
{
    public string Path { get; set; } = "";
    public string FileType { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public long SizeBytes { get; set; }
    public DateTime ModifiedAt { get; set; }
}

public class FileService
{
    private static readonly (string FileName, string FileType, string DisplayName)[] SaveCandidates =
    [
        ("continue.sav", "vanilla", "Vanilla FTL"),
        ("hs_continue.sav", "hyperspace", "Hyperspace"),
        ("hs_mv_continue.sav", "multiverse", "Multiverse"),
    ];

    public static string? GetFtlSaveDirectory()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var docsPath = System.IO.Path.Combine(userProfile, "Documents", "My Games", "FasterThanLight");
        return Directory.Exists(docsPath) ? docsPath : null;
    }

    public static List<DetectedSaveFile> DetectSaveFiles()
    {
        var dir = GetFtlSaveDirectory();
        if (dir == null) return [];

        var files = new List<DetectedSaveFile>();
        foreach (var (fileName, fileType, displayName) in SaveCandidates)
        {
            var filePath = System.IO.Path.Combine(dir, fileName);
            if (File.Exists(filePath))
            {
                var info = new FileInfo(filePath);
                files.Add(new DetectedSaveFile
                {
                    Path = filePath,
                    FileType = fileType,
                    DisplayName = displayName,
                    SizeBytes = info.Length,
                    ModifiedAt = info.LastWriteTime
                });
            }
        }
        return files;
    }

    public static string CreateBackup(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Cannot create backup for a missing file.", filePath);
        }

        var dir = System.IO.Path.GetDirectoryName(filePath)!;
        var ext = System.IO.Path.GetExtension(filePath);
        var baseName = System.IO.Path.GetFileNameWithoutExtension(filePath);
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff");
        var backupPath = System.IO.Path.Combine(dir, $"{baseName}_backup_{timestamp}{ext}");
        int sequence = 1;
        while (File.Exists(backupPath))
        {
            backupPath = System.IO.Path.Combine(dir, $"{baseName}_backup_{timestamp}_{sequence}{ext}");
            sequence++;
        }

        File.Copy(filePath, backupPath);
        return backupPath;
    }

    public static SavedGameState LoadSaveFile(string filePath)
    {
        var data = File.ReadAllBytes(filePath);
        var parser = new SaveFileParser();
        return parser.Parse(data, sourcePath: filePath);
    }

    public static void WriteSaveFile(string filePath, SavedGameState state)
    {
        if (File.Exists(filePath))
        {
            CreateBackup(filePath);
        }
        var writer = new SaveFileWriter();
        var bytes = writer.Write(state);
        File.WriteAllBytes(filePath, bytes);
    }
}
