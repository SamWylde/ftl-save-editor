using System.IO;
using System.Text;
using FtlSaveEditor.Data;

namespace FtlSaveEditor.Services;

/// <summary>
/// Reads FTL's data.dat archive and extracts game data files.
/// data.dat format:
///   - 4 bytes: number of file slots (uint32 LE)
///   - N x 4 bytes: file offset table (uint32 LE per slot, 0 = empty)
///   - At each non-zero offset:
///     - 4 bytes: data size (uint32 LE)
///     - 4 bytes: filename length (uint32 LE)
///     - filename bytes
///     - data bytes
/// </summary>
public static class GameDataService
{
    private static Dictionary<string, byte[]>? _cachedFiles;
    private static string? _cachedDatPath;

    public class DatFileEntry
    {
        public string FileName { get; set; } = "";
        public int DataSize { get; set; }
    }

    /// <summary>
    /// Read the data.dat file from the FTL install directory.
    /// Returns a dictionary of filename -> file content bytes.
    /// </summary>
    public static Dictionary<string, byte[]> ReadDataDat()
    {
        var gamePath = ModBlueprintScanner.DetectGamePath();
        if (gamePath == null) return new();

        // Try data.dat first, then ftl.dat
        var datPath = Path.Combine(gamePath, "data.dat");
        if (!File.Exists(datPath))
        {
            datPath = Path.Combine(gamePath, "ftl.dat");
            if (!File.Exists(datPath)) return new();
        }

        if (_cachedFiles != null && _cachedDatPath == datPath) return _cachedFiles;

        try
        {
            var files = ParseDatArchive(datPath);
            _cachedFiles = files;
            _cachedDatPath = datPath;
            return files;
        }
        catch
        {
            return new();
        }
    }

    private static Dictionary<string, byte[]> ParseDatArchive(string datPath)
    {
        var result = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

        using var stream = File.OpenRead(datPath);
        using var reader = new BinaryReader(stream, Encoding.UTF8);

        int fileCount = reader.ReadInt32();
        if (fileCount <= 0 || fileCount > 100000) return result;

        var offsets = new int[fileCount];
        for (int i = 0; i < fileCount; i++)
        {
            offsets[i] = reader.ReadInt32();
        }

        foreach (var offset in offsets)
        {
            if (offset == 0) continue;

            try
            {
                stream.Seek(offset, SeekOrigin.Begin);
                int dataSize = reader.ReadInt32();
                int nameLength = reader.ReadInt32();

                if (nameLength <= 0 || nameLength > 1000 || dataSize < 0 || dataSize > 100_000_000)
                    continue;

                string fileName = Encoding.UTF8.GetString(reader.ReadBytes(nameLength));
                byte[] data = reader.ReadBytes(dataSize);
                result[fileName] = data;
            }
            catch { /* skip corrupt entries */ }
        }

        return result;
    }

    /// <summary>
    /// Extract all blueprints from data.dat into a ModBlueprints structure.
    /// </summary>
    public static ModBlueprints ExtractVanillaBlueprints()
    {
        var files = ReadDataDat();
        var result = new ModBlueprints();

        string[] blueprintFiles =
        [
            "blueprints.xml",
            "autoBlueprints.xml",
            "dlcBlueprints.xml",
            "dlcBlueprintsOverwrite.xml"
        ];

        foreach (var bpFile in blueprintFiles)
        {
            // Find matching key (may have path prefix like "data/")
            var key = files.Keys.FirstOrDefault(k =>
                k.Replace('\\', '/').EndsWith(bpFile, StringComparison.OrdinalIgnoreCase));

            if (key != null)
            {
                var content = Encoding.UTF8.GetString(files[key]);
                ModBlueprintScanner.ParseBlueprints(content, result);
            }
        }

        return result;
    }

    /// <summary>
    /// List all files in the data.dat archive.
    /// </summary>
    public static List<DatFileEntry> ListAllFiles()
    {
        var files = ReadDataDat();
        return files.Select(kvp => new DatFileEntry
        {
            FileName = kvp.Key,
            DataSize = kvp.Value.Length
        }).OrderBy(f => f.FileName).ToList();
    }

    /// <summary>
    /// Get the text content of a specific file from data.dat.
    /// </summary>
    public static string? GetFileContent(string fileName)
    {
        var files = ReadDataDat();
        return files.TryGetValue(fileName, out var data)
            ? Encoding.UTF8.GetString(data)
            : null;
    }
}
