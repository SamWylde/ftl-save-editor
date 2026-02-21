namespace FtlSaveEditor.Tests;

using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;
using FtlSaveEditor.SaveFile;
using FtlSaveEditor.Services;

public class HsFormatAnalysis
{
    private readonly ITestOutputHelper _output;
    public HsFormatAnalysis(ITestOutputHelper output) { _output = output; }

    [Fact]
    public void DumpByteDifferences()
    {
        var savePath = @"C:\Users\vptom\Documents\My Games\FasterThanLight\hs_mv_continue.sav";
        if (!File.Exists(savePath)) return;

        byte[] originalData = File.ReadAllBytes(savePath);
        var parser = new SaveFileParser();
        var parsed = parser.Parse(originalData, debug: false);
        
        var writer = new SaveFileWriter();
        byte[] rewritten = writer.Write(parsed);
        
        if (originalData.Length != rewritten.Length)
        {
            _output.WriteLine($"Size mismatch: Original {originalData.Length}, Rewritten {rewritten.Length}");
        }
        
        int diffCount = 0;
        int minLen = Math.Min(originalData.Length, rewritten.Length);

        for (int i = 0; i < minLen; i++)
        {
            if (originalData[i] != rewritten[i])
            {
                // Show the 4-byte aligned word containing this diff
                int wordStart = (i / 4) * 4;
                int origWord = wordStart + 3 < minLen ? BitConverter.ToInt32(originalData, wordStart) : -1;
                int rewWord = wordStart + 3 < minLen ? BitConverter.ToInt32(rewritten, wordStart) : -1;
                _output.WriteLine($"  Diff #{diffCount}: offset {i} (0x{i:X4}), word@{wordStart}: orig=0x{origWord:X8} ({origWord}), rew=0x{rewWord:X8} ({rewWord})");
                diffCount++;
                // Skip rest of this 4-byte word
                i = wordStart + 3;
                if (diffCount > 200)
                {
                    _output.WriteLine("Too many differences, stopping.");
                    break;
                }
            }
        }

        _output.WriteLine($"\nTotal 4-byte-word differences: {diffCount}");
    }

    [Fact]
    public void ModBlueprintScanner_FindsBlueprints()
    {
        var gamePath = ModBlueprintScanner.DetectGamePath();
        if (gamePath == null)
        {
            _output.WriteLine("Game path not found, skipping");
            return;
        }
        _output.WriteLine($"Game path: {gamePath}");

        var bp = ModBlueprintScanner.Scan();
        _output.WriteLine($"Weapons: {bp.Weapons.Count}");
        _output.WriteLine($"Drones: {bp.Drones.Count}");
        _output.WriteLine($"Augments: {bp.Augments.Count}");

        foreach (var kv in bp.Weapons.Take(5))
            _output.WriteLine($"  W: {kv.Key} = {kv.Value.Title} ({kv.Value.Type}, {kv.Value.Damage}dmg x{kv.Value.Shots})");
        foreach (var kv in bp.Drones.Take(3))
            _output.WriteLine($"  D: {kv.Key} = {kv.Value.Title} ({kv.Value.Type}, {kv.Value.Power}pwr)");
        foreach (var kv in bp.Augments.Take(3))
            _output.WriteLine($"  A: {kv.Key} = {kv.Value.Title} ({kv.Value.Cost}scrap)");

        Assert.True(bp.HasData, "Expected blueprints to be found in mod files");
    }
}
