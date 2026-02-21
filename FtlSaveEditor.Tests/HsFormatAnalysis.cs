namespace FtlSaveEditor.Tests;

using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;
using FtlSaveEditor.SaveFile;

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
}
