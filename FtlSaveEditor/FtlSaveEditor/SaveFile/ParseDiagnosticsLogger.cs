namespace FtlSaveEditor.SaveFile;

using System;
using System.IO;
using FtlSaveEditor.Models;

internal static class ParseDiagnosticsLogger
{
    public static string Write(
        string? sourcePath,
        SaveParseMode parseMode,
        ParseDiagnostic diagnostic,
        Exception ex)
    {
        var baseDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FtlSaveEditor",
            "logs");

        Directory.CreateDirectory(baseDir);

        var sourceName = string.IsNullOrWhiteSpace(sourcePath)
            ? "unknown.sav"
            : Path.GetFileName(sourcePath);
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
        var guid = Guid.NewGuid().ToString("N")[..8];
        var logPath = Path.Combine(baseDir, $"{timestamp}_{guid}_{sourceName}.log");

        using var writer = new StreamWriter(logPath);
        writer.WriteLine($"Timestamp: {DateTime.Now:O}");
        writer.WriteLine($"SourceFile: {sourcePath ?? "(unknown)"}");
        writer.WriteLine($"ParseMode: {parseMode}");
        writer.WriteLine($"Section: {diagnostic.Section}");
        writer.WriteLine($"ByteOffset: {(diagnostic.ByteOffset.HasValue ? diagnostic.ByteOffset.Value : -1)}");
        writer.WriteLine($"Message: {diagnostic.Message}");
        writer.WriteLine();
        writer.WriteLine("Exception:");
        writer.WriteLine(ex.ToString());

        return logPath;
    }
}
