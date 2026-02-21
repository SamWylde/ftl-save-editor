namespace FtlSaveEditor.Tests;

using System;
using System.IO;
using System.Text;
using FtlSaveEditor.SaveFile;
using Xunit;
using Xunit.Abstractions;

public class CrewDebugTest
{
    private readonly ITestOutputHelper _output;
    public CrewDebugTest(ITestOutputHelper output) { _output = output; }

    [Fact]
    public void DumpCrewBoundarySearch()
    {
        var savePath = @"C:\Users\vptom\Documents\My Games\FasterThanLight\hs_mv_continue.sav";
        if (!File.Exists(savePath)) return;

        byte[] data = File.ReadAllBytes(savePath);
        var parser = new SaveFileParser();

        // Capture debug output
        var debugLogPath = Path.Combine(Path.GetTempPath(), "ftl_crew_debug2.log");
        var origOut = Console.Out;
        using (var sw = new StreamWriter(debugLogPath))
        {
            Console.SetOut(sw);
            parser.Parse(data, debug: true);
            Console.SetOut(origOut);
        }

        // Now let's manually examine the opaque interior
        // From the debug log, the interior starts at offset 515 into the save file
        // and is 9324 bytes long (weapon section at 9839)
        int interiorStart = 515;
        int interiorLength = 9324;
        var interior = new byte[interiorLength];
        Array.Copy(data, interiorStart, interior, 0, interiorLength);

        // Crew count at offset 0
        int crewCount = BitConverter.ToInt32(interior, 0);
        _output.WriteLine($"Crew count: {crewCount}");

        // After Crew[6] 'Vincent' (slug_saboteur), stream was at position 4683
        // That means the next crew (Crew[7]) starts at or after offset 4683
        // Let's dump hex around that area to see what's there
        int searchStart = 4683;
        _output.WriteLine($"\n=== Hex around offset {searchStart} (after Crew[6]) ===");

        // Try to find string-like data by scanning for plausible name lengths
        for (int pos = searchStart; pos < interiorLength - 8; pos += 1)
        {
            int nameLen = BitConverter.ToInt32(interior, pos);
            if (nameLen >= 0 && nameLen <= 50 && pos + 4 + nameLen < interiorLength)
            {
                string candidateName = Encoding.UTF8.GetString(interior, pos + 4, nameLen);
                bool printable = true;
                foreach (char c in candidateName)
                {
                    if (c < 0x20 || c > 0x7E) { printable = false; break; }
                }

                if (printable && nameLen > 0)
                {
                    // Check if there's a plausible race after name
                    int racePos = pos + 4 + nameLen;
                    if (racePos + 4 < interiorLength)
                    {
                        int raceLen = BitConverter.ToInt32(interior, racePos);
                        if (raceLen >= 1 && raceLen <= 50 && racePos + 4 + raceLen < interiorLength)
                        {
                            string candidateRace = Encoding.UTF8.GetString(interior, racePos + 4, raceLen);
                            bool racePrintable = true;
                            foreach (char c in candidateRace)
                            {
                                if (!char.IsLetterOrDigit(c) && c != '_') { racePrintable = false; break; }
                            }

                            if (racePrintable && candidateRace.Length > 0)
                            {
                                // Check enemyBoardingDrone (should be 0 or 1)
                                int ebdPos = racePos + 4 + raceLen;
                                if (ebdPos + 4 < interiorLength)
                                {
                                    int ebd = BitConverter.ToInt32(interior, ebdPos);
                                    // Check health
                                    int healthPos = ebdPos + 4;
                                    if (healthPos + 4 < interiorLength)
                                    {
                                        int health = BitConverter.ToInt32(interior, healthPos);
                                        _output.WriteLine($"  offset {pos}: name='{candidateName}' race='{candidateRace}' ebd={ebd} health={health} (nameLen={nameLen})");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // Also dump raw hex for the first 100 bytes after position 4683
        _output.WriteLine($"\n=== Raw hex at offset {searchStart} (60 int32s) ===");
        for (int i = searchStart; i < Math.Min(searchStart + 240, interiorLength - 4); i += 4)
        {
            int val = BitConverter.ToInt32(interior, i);
            _output.WriteLine($"  [{i}] int32={val} (0x{val:X8})");
        }
    }
}
