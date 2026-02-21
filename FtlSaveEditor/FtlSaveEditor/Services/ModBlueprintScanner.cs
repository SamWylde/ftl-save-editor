using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using FtlSaveEditor.Data;

namespace FtlSaveEditor.Services;

public static class ModBlueprintScanner
{
    private static ModBlueprints? _cached;
    private static string? _cachedPath;

    public static ModBlueprints Scan()
    {
        var gamePath = DetectGamePath();
        if (gamePath == null) return new ModBlueprints();

        if (_cached != null && _cachedPath == gamePath) return _cached;

        var result = new ModBlueprints();
        var modsDir = Path.Combine(gamePath, "mods");
        if (!Directory.Exists(modsDir)) return result;

        foreach (var zipPath in Directory.GetFiles(modsDir, "*.zip"))
        {
            try { ScanZip(zipPath, result); }
            catch { /* skip corrupt zips */ }
        }

        _cached = result;
        _cachedPath = gamePath;
        return result;
    }

    public static string? DetectGamePath()
    {
        // Check settings file first
        var settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FtlSaveEditor", "settings.txt");
        if (File.Exists(settingsPath))
        {
            var customPath = File.ReadAllText(settingsPath).Trim();
            if (Directory.Exists(customPath)) return customPath;
        }

        // Common installation paths
        string[] candidates =
        [
            @"D:\GOG\Games\FTL Advanced Edition",
            @"C:\GOG Games\FTL Advanced Edition",
            @"C:\Program Files (x86)\GOG Galaxy\Games\FTL Advanced Edition",
            @"C:\Program Files (x86)\Steam\steamapps\common\FTL Faster Than Light",
            @"D:\Steam\steamapps\common\FTL Faster Than Light",
            @"D:\SteamLibrary\steamapps\common\FTL Faster Than Light",
            @"E:\SteamLibrary\steamapps\common\FTL Faster Than Light",
        ];

        foreach (var path in candidates)
        {
            if (Directory.Exists(path) && Directory.Exists(Path.Combine(path, "mods")))
                return path;
        }

        // Try to find via Steam libraryfolders.vdf
        try
        {
            var steamPath = @"C:\Program Files (x86)\Steam\steamapps\libraryfolders.vdf";
            if (File.Exists(steamPath))
            {
                var text = File.ReadAllText(steamPath);
                var pathMatches = Regex.Matches(text, @"""path""\s+""([^""]+)""");
                foreach (Match m in pathMatches)
                {
                    var lib = m.Groups[1].Value.Replace(@"\\", @"\");
                    var ftlPath = Path.Combine(lib, "steamapps", "common", "FTL Faster Than Light");
                    if (Directory.Exists(ftlPath) && Directory.Exists(Path.Combine(ftlPath, "mods")))
                        return ftlPath;
                }
            }
        }
        catch { /* ignore */ }

        return null;
    }

    private static void ScanZip(string zipPath, ModBlueprints result)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        foreach (var entry in archive.Entries)
        {
            var name = entry.FullName;
            if (!name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) &&
                !name.EndsWith(".xml.append", StringComparison.OrdinalIgnoreCase))
                continue;

            // Only scan blueprint-related files
            var fileName = Path.GetFileName(name).ToLowerInvariant();
            if (!fileName.Contains("blueprint") && !fileName.Contains("auto"))
                continue;

            try
            {
                using var stream = entry.Open();
                using var reader = new StreamReader(stream);
                var content = reader.ReadToEnd();
                ParseBlueprints(content, result);
            }
            catch { /* skip unreadable entries */ }
        }
    }

    private static readonly Regex WeaponRx = new(
        @"<weaponBlueprint\s+name=""([^""]+)"">(.*?)</weaponBlueprint>",
        RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex DroneRx = new(
        @"<droneBlueprint\s+name=""([^""]+)"">(.*?)</droneBlueprint>",
        RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex AugRx = new(
        @"<augBlueprint\s+name=""([^""]+)"">(.*?)</augBlueprint>",
        RegexOptions.Singleline | RegexOptions.Compiled);

    private static void ParseBlueprints(string content, ModBlueprints result)
    {
        foreach (Match m in WeaponRx.Matches(content))
        {
            var id = m.Groups[1].Value;
            var body = m.Groups[2].Value;
            result.Weapons[id] = new WeaponBlueprint(
                id,
                ExtractTag(body, "title") ?? ExtractTag(body, "short") ?? id,
                ExtractTag(body, "tooltip") ?? ExtractTag(body, "desc") ?? "",
                ExtractTag(body, "type") ?? "",
                ParseInt(ExtractTag(body, "damage")),
                ParseInt(ExtractTag(body, "shots"), 1),
                ParseInt(ExtractTag(body, "power")),
                ParseInt(ExtractTag(body, "cost")),
                ParseDouble(ExtractTag(body, "cooldown")));
        }

        foreach (Match m in DroneRx.Matches(content))
        {
            var id = m.Groups[1].Value;
            var body = m.Groups[2].Value;
            result.Drones[id] = new DroneBlueprint(
                id,
                ExtractTag(body, "title") ?? ExtractTag(body, "short") ?? id,
                ExtractTag(body, "tooltip") ?? ExtractTag(body, "desc") ?? "",
                ExtractTag(body, "type") ?? "",
                ParseInt(ExtractTag(body, "power")),
                ParseInt(ExtractTag(body, "cost")));
        }

        foreach (Match m in AugRx.Matches(content))
        {
            var id = m.Groups[1].Value;
            var body = m.Groups[2].Value;
            result.Augments[id] = new AugBlueprint(
                id,
                ExtractTag(body, "title") ?? id,
                ExtractTag(body, "desc") ?? "",
                ParseInt(ExtractTag(body, "cost")),
                ExtractTag(body, "stackable")?.ToLowerInvariant() == "true");
        }
    }

    private static string? ExtractTag(string body, string tag)
    {
        var match = Regex.Match(body, $@"<{tag}>(.*?)</{tag}>", RegexOptions.Singleline);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    private static int ParseInt(string? value, int defaultVal = 0)
        => int.TryParse(value, out int v) ? v : defaultVal;

    private static double ParseDouble(string? value, double defaultVal = 0)
        => double.TryParse(value, out double v) ? v : defaultVal;
}
