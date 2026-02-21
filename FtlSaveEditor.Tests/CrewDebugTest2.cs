namespace FtlSaveEditor.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FtlSaveEditor.SaveFile;
using Xunit;
using Xunit.Abstractions;

public class CrewDebugTest2
{
    private readonly ITestOutputHelper _output;
    public CrewDebugTest2(ITestOutputHelper output) { _output = output; }

    [Fact]
    public void DumpAllPossibleStringPairs()
    {
        var savePath = @"C:\Users\vptom\Documents\My Games\FasterThanLight\hs_mv_continue.sav";
        if (!File.Exists(savePath)) return;

        byte[] data = File.ReadAllBytes(savePath);
        
        // Interior starts at offset 515, length 9324
        int interiorStart = 515;
        int interiorLength = 9324;
        var interior = new byte[interiorLength];
        Array.Copy(data, interiorStart, interior, 0, interiorLength);

        _output.WriteLine($"Interior length: {interiorLength}");
        _output.WriteLine($"Crew count (first int): {BitConverter.ToInt32(interior, 0)}");
        
        // After crew[6] ends at 4683, look for ANY two consecutive valid strings 
        // (name then race) without ASCII restriction
        int searchStart = 4683;
        int found = 0;
        
        for (int pos = searchStart; pos < interiorLength - 12; pos++)
        {
            if (pos + 4 > interiorLength) break;
            int nameLen = BitConverter.ToInt32(interior, pos);
            if (nameLen < 0 || nameLen > 50) continue;
            if (pos + 4 + nameLen + 4 > interiorLength) continue;
            
            // Try to read a string
            string name = "";
            if (nameLen > 0)
            {
                name = Encoding.UTF8.GetString(interior, pos + 4, nameLen);
            }
            
            int racePos = pos + 4 + nameLen;
            int raceLen = BitConverter.ToInt32(interior, racePos);
            if (raceLen < 1 || raceLen > 50) continue;
            if (racePos + 4 + raceLen > interiorLength) continue;
            
            string race = Encoding.UTF8.GetString(interior, racePos + 4, raceLen);
            
            // Check if race looks like an identifier (letters, digits, underscores only)
            bool raceValid = true;
            foreach (char c in race)
            {
                if (!char.IsLetterOrDigit(c) && c != '_') { raceValid = false; break; }
            }
            if (!raceValid) continue;
            
            // Check what follows (should be enemyBoardingDrone: 0 or 1)
            int ebdPos = racePos + 4 + raceLen;
            if (ebdPos + 8 > interiorLength) continue;
            int ebd = BitConverter.ToInt32(interior, ebdPos);
            int health = BitConverter.ToInt32(interior, ebdPos + 4);
            
            // Only show entries where ebd is 0 or 1 and health is 0-500
            if ((ebd == 0 || ebd == 1) && health >= 0 && health <= 500)
            {
                _output.WriteLine($"  *** STRONG MATCH at offset {pos}: name='{name}' (len={nameLen}) race='{race}' ebd={ebd} health={health}");
                found++;
            }
            else if (race.Length >= 3 && race.Length <= 30)
            {
                // Also show race-like strings regardless of ebd/health
                _output.WriteLine($"  WEAK match at offset {pos}: name='{name}' (len={nameLen}) race='{race}' ebd={ebd} health={health}");
                found++;
            }
            
            if (found > 50) break;
        }
        
        _output.WriteLine($"\n=== Total matches found: {found} ===");
    }
}
