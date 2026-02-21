namespace FtlSaveEditor.SaveFile;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using FtlSaveEditor.Models;

public class SaveFileParser
{
    private const int MaxStringLength = 10000;
    private BinaryReader _reader = null!;

    private int ReadInt() => _reader.ReadInt32();
    private bool ReadBool() => _reader.ReadInt32() != 0;
    private int ReadMinMaxedInt() => _reader.ReadInt32();

    private string ReadString()
    {
        long pos = _reader.BaseStream.Position;
        int len = ReadInt();
        if (len < 0) throw new Exception($"Negative string length {len} at byte offset {pos} (0x{pos:X})");
        if (len > 10000) throw new Exception($"Implausible string length {len} at byte offset {pos} (0x{pos:X})");
        if (len == 0) return "";
        byte[] bytes = _reader.ReadBytes(len);
        return Encoding.UTF8.GetString(bytes);
    }

    private bool _debug;

    private sealed class HeaderSnapshot
    {
        public int FileFormat { get; init; }
        public bool RandomNative { get; init; }
        public bool DlcEnabled { get; init; }
        public int Difficulty { get; init; }
        public int TotalShipsDefeated { get; init; }
        public int TotalBeaconsExplored { get; init; }
        public int TotalScrapCollected { get; init; }
        public int TotalCrewHired { get; init; }
        public string PlayerShipName { get; init; } = "";
        public string PlayerShipBlueprintId { get; init; } = "";
        public int OneBasedSectorNumber { get; init; }
        public int UnknownBeta { get; init; }
        public List<StateVar> StateVars { get; init; } = [];
        public long OffsetAfterStateVars { get; init; }
    }

    public SavedGameState Parse(byte[] data, bool debug = false, string? sourcePath = null)
    {
        _debug = debug;
        HeaderSnapshot header;
        try
        {
            header = ParseHeaderSnapshot(data);
        }
        catch (Exception ex)
        {
            var diagnostic = BuildParseDiagnostic(ex);
            var logPath = ParseDiagnosticsLogger.Write(sourcePath, SaveParseMode.Full, diagnostic, ex);
            diagnostic.LogPath = logPath;
            throw BuildEnrichedParseException(ex, diagnostic);
        }

        using var ms = new MemoryStream(data);
        _reader = new BinaryReader(ms);

        try
        {
            var state = ParseSavedGame();
            state.ParseMode = SaveParseMode.Full;
            state.Capabilities = EditorCapability.Full;
            return state;
        }
        catch (Exception ex)
        {
            var diagnostic = BuildParseDiagnostic(ex);
            var logPath = ParseDiagnosticsLogger.Write(sourcePath, SaveParseMode.Full, diagnostic, ex);
            diagnostic.LogPath = logPath;

            if (header.FileFormat == 11)
            {
                var warning =
                    $"Full parse failed ({diagnostic.Section}, offset {diagnostic.ByteOffset?.ToString() ?? "unknown"}). " +
                    $"Falling back to restricted mode. Log: {logPath}";
                return BuildRestrictedState(header, data, warning, diagnostic);
            }

            throw BuildEnrichedParseException(ex, diagnostic);
        }
    }

    private void DebugLog(string msg)
    {
        if (_debug) Console.WriteLine($"  [parse@{_reader.BaseStream.Position}] {msg}");
    }

    // ========================================================================
    // Section scanner - finds weapon section by scanning for weapon count + string pattern
    // ========================================================================

    private long FindWeaponSection(long searchStart, int fmt)
    {
        var stream = _reader.BaseStream;
        long savedPos = stream.Position;
        var candidatePositions = new List<long>();

        // We know the structure before weapons:
        // ... rooms ... breaches ... doors ... cloakAnimTicks(4, fmt>=7) ... lockdownCrystalCount(4, fmt>=8) + data ... weaponCount
        // Scan from searchStart looking for: int(count 0-10) + int(strlen 3-50) + ASCII bytes
        for (long pos = searchStart; pos + 20 < stream.Length; pos += 4)
        {
            stream.Position = pos;
            int candidateCount = _reader.ReadInt32();
            if (candidateCount < 0 || candidateCount > 10) continue;

            // If count=0, next section is drones: count(int) + for each: string + 7 ints
            // For count>0, verify first weapon: stringLen + ASCII string + armed(0 or 1)
            if (candidateCount == 0)
            {
                // 0 weapons. Next is drone count. Verify it's also small.
                if (pos + 8 > stream.Length) continue;
                int droneCount = _reader.ReadInt32();
                if (droneCount < 0 || droneCount > 10) continue;

                // If drones=0 too, next is augment count + augment strings
                if (droneCount == 0)
                {
                    if (pos + 12 > stream.Length) continue;
                    int augCount = _reader.ReadInt32();
                    if (augCount < 0 || augCount > 20) continue;
                    if (augCount > 0)
                    {
                        int augStrLen = _reader.ReadInt32();
                        if (augStrLen >= 3 && augStrLen <= 50 && pos + 16 + augStrLen <= stream.Length)
                        {
                            byte[] strBytes = _reader.ReadBytes(augStrLen);
                            if (IsAsciiIdentifier(strBytes))
                            {
                                candidatePositions.Add(pos);
                            }
                        }
                    }
                }
                else
                {
                    // Verify first drone string
                    int droneStrLen = _reader.ReadInt32();
                    if (droneStrLen >= 3 && droneStrLen <= 50 && pos + 12 + droneStrLen <= stream.Length)
                    {
                        byte[] strBytes = _reader.ReadBytes(droneStrLen);
                        if (IsAsciiIdentifier(strBytes))
                        {
                            candidatePositions.Add(pos);
                        }
                    }
                }
                continue;
            }

            // candidateCount > 0: verify first weapon string
            if (pos + 8 > stream.Length) continue;
            int strLen = _reader.ReadInt32();
            if (strLen < 3 || strLen > 50) continue;
            if (pos + 8 + strLen + 4 > stream.Length) continue;

            byte[] weaponStr = _reader.ReadBytes(strLen);
            if (!IsAsciiIdentifier(weaponStr)) continue;

            int armed = _reader.ReadInt32();
            if (armed != 0 && armed != 1) continue;

            candidatePositions.Add(pos);
        }

        foreach (var candidatePos in candidatePositions.Distinct().OrderBy(p => p))
        {
            if (ValidateWeaponSectionCandidate(candidatePos, fmt))
            {
                stream.Position = savedPos;
                return candidatePos;
            }
        }

        stream.Position = savedPos;
        throw new Exception($"Could not find weapon section scanning from byte offset {searchStart}");
    }

    private static bool IsAsciiIdentifier(byte[] bytes)
    {
        foreach (byte b in bytes)
        {
            if (b < 0x20 || b > 0x7E) return false; // printable ASCII
        }
        return true;
    }

    private HeaderSnapshot ParseHeaderSnapshot(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        int ReadSnapshotInt()
        {
            if (ms.Position + 4 > ms.Length)
            {
                throw new Exception($"Unexpected end-of-file reading int at byte offset {ms.Position}");
            }

            return reader.ReadInt32();
        }

        bool ReadSnapshotBool() => ReadSnapshotInt() != 0;

        string ReadSnapshotString()
        {
            long pos = ms.Position;
            int len = ReadSnapshotInt();
            if (len < 0)
            {
                throw new Exception($"Negative string length {len} at byte offset {pos} (0x{pos:X})");
            }

            if (len > MaxStringLength)
            {
                throw new Exception($"Implausible string length {len} at byte offset {pos} (0x{pos:X})");
            }

            if (ms.Position + len > ms.Length)
            {
                throw new Exception($"String overruns file at byte offset {pos} (0x{pos:X})");
            }

            if (len == 0)
            {
                return "";
            }

            byte[] bytes = reader.ReadBytes(len);
            return Encoding.UTF8.GetString(bytes);
        }

        int fileFormat = ReadSnapshotInt();
        if (fileFormat != 2 && fileFormat != 7 && fileFormat != 8 && fileFormat != 9 && fileFormat != 11)
        {
            throw new Exception(
                $"Unsupported file format version: {fileFormat}. Expected 2, 7, 8, 9, or 11.");
        }

        bool randomNative = fileFormat >= 11 ? ReadSnapshotBool() : false;
        bool dlcEnabled = fileFormat >= 7 ? ReadSnapshotBool() : false;
        int difficulty = ReadSnapshotInt();
        int totalShipsDefeated = ReadSnapshotInt();
        int totalBeaconsExplored = ReadSnapshotInt();
        int totalScrapCollected = ReadSnapshotInt();
        int totalCrewHired = ReadSnapshotInt();
        string playerShipName = ReadSnapshotString();
        string playerShipBlueprintId = ReadSnapshotString();
        int oneBasedSectorNumber = ReadSnapshotInt();
        int unknownBeta = ReadSnapshotInt();

        int stateVarCount = ReadSnapshotInt();
        if (stateVarCount < 0 || stateVarCount > 100000)
        {
            throw new Exception(
                $"Implausible state var count {stateVarCount} at byte offset {ms.Position - 4} (0x{ms.Position - 4:X})");
        }

        var stateVars = new List<StateVar>(stateVarCount);
        for (int i = 0; i < stateVarCount; i++)
        {
            stateVars.Add(new StateVar
            {
                Key = ReadSnapshotString(),
                Value = ReadSnapshotInt()
            });
        }

        return new HeaderSnapshot
        {
            FileFormat = fileFormat,
            RandomNative = randomNative,
            DlcEnabled = dlcEnabled,
            Difficulty = difficulty,
            TotalShipsDefeated = totalShipsDefeated,
            TotalBeaconsExplored = totalBeaconsExplored,
            TotalScrapCollected = totalScrapCollected,
            TotalCrewHired = totalCrewHired,
            PlayerShipName = playerShipName,
            PlayerShipBlueprintId = playerShipBlueprintId,
            OneBasedSectorNumber = oneBasedSectorNumber,
            UnknownBeta = unknownBeta,
            StateVars = stateVars,
            OffsetAfterStateVars = ms.Position,
        };
    }

    private SavedGameState BuildRestrictedState(
        HeaderSnapshot header,
        byte[] fullData,
        string warning,
        ParseDiagnostic diagnostic)
    {
        int tailOffset = (int)Math.Min(Math.Max(0, header.OffsetAfterStateVars), fullData.Length);
        int tailLength = fullData.Length - tailOffset;
        var tailBytes = new byte[tailLength];
        Array.Copy(fullData, tailOffset, tailBytes, 0, tailLength);

        var state = new SavedGameState
        {
            ParseMode = SaveParseMode.RestrictedOpaqueTail,
            Capabilities = EditorCapability.Metadata | EditorCapability.StateVars,
            OpaqueTailBytes = tailBytes,
            FileFormat = header.FileFormat,
            RandomNative = header.RandomNative,
            DlcEnabled = header.DlcEnabled,
            Difficulty = header.Difficulty,
            TotalShipsDefeated = header.TotalShipsDefeated,
            TotalBeaconsExplored = header.TotalBeaconsExplored,
            TotalScrapCollected = header.TotalScrapCollected,
            TotalCrewHired = header.TotalCrewHired,
            PlayerShipName = header.PlayerShipName,
            PlayerShipBlueprintId = header.PlayerShipBlueprintId,
            OneBasedSectorNumber = header.OneBasedSectorNumber,
            UnknownBeta = header.UnknownBeta,
            StateVars = new List<StateVar>(header.StateVars),
            PlayerShip = new ShipState
            {
                ShipName = header.PlayerShipName,
                ShipBlueprintId = header.PlayerShipBlueprintId
            },
        };

        state.ParseWarnings.Add(warning);
        state.ParseDiagnostics.Add(diagnostic);
        return state;
    }

    private ParseDiagnostic BuildParseDiagnostic(Exception ex)
    {
        return new ParseDiagnostic
        {
            Section = InferSection(ex),
            ByteOffset = ExtractByteOffset(ex.Message),
            Message = ex.Message,
        };
    }

    private static string InferSection(Exception ex)
    {
        var trace = new StackTrace(ex, false);
        var frame = trace.GetFrames()?.FirstOrDefault(f =>
            f.GetMethod()?.DeclaringType == typeof(SaveFileParser) &&
            f.GetMethod()?.Name != null &&
            f.GetMethod()!.Name.StartsWith("Parse", StringComparison.Ordinal));

        return frame?.GetMethod()?.Name ?? "UnknownSection";
    }

    private static long? ExtractByteOffset(string message)
    {
        var match = Regex.Match(message, @"byte offset (\d+)", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            match = Regex.Match(message, @"offset (\d+)", RegexOptions.IgnoreCase);
        }

        if (match.Success && long.TryParse(match.Groups[1].Value, out long offset))
        {
            return offset;
        }

        return null;
    }

    private static Exception BuildEnrichedParseException(Exception ex, ParseDiagnostic diagnostic)
    {
        string offsetText = diagnostic.ByteOffset.HasValue ? diagnostic.ByteOffset.Value.ToString() : "unknown";
        string logPath = string.IsNullOrWhiteSpace(diagnostic.LogPath) ? "(not written)" : diagnostic.LogPath!;
        var message =
            $"Failed to parse save file.\n\nSection: {diagnostic.Section}\nByte offset: {offsetText}\nLog: {logPath}\n\n{diagnostic.Message}";
        return new Exception(message, ex);
    }

    private bool ValidateWeaponSectionCandidate(long sectionPos, int fmt)
    {
        var stream = _reader.BaseStream;
        long savedPos = stream.Position;

        try
        {
            stream.Position = sectionPos;
            if (!TryReadBoundedCount(0, 10, out int weaponCount))
            {
                return false;
            }

            for (int i = 0; i < weaponCount; i++)
            {
                if (!TryReadAsciiIdentifierString(1, 80, out _))
                {
                    return false;
                }

                if (!TryReadBoolInt(out _))
                {
                    return false;
                }

                if (fmt == 2 && !TryReadInt(out _))
                {
                    return false;
                }
            }

            if (!TryReadBoundedCount(0, 10, out int droneCount))
            {
                return false;
            }

            for (int i = 0; i < droneCount; i++)
            {
                if (!TryReadAsciiIdentifierString(1, 80, out _))
                {
                    return false;
                }

                if (!TryReadBoolInt(out _) || !TryReadBoolInt(out _))
                {
                    return false;
                }

                for (int j = 0; j < 5; j++)
                {
                    if (!TryReadInt(out _))
                    {
                        return false;
                    }
                }
            }

            if (!TryReadBoundedCount(0, 20, out int augmentCount))
            {
                return false;
            }

            for (int i = 0; i < augmentCount; i++)
            {
                if (!TryReadAsciiIdentifierString(1, 80, out _))
                {
                    return false;
                }
            }

            // Validate next section (cargo) to reduce false positives.
            if (!TryReadBoundedCount(0, 20, out int cargoCount))
            {
                return false;
            }

            for (int i = 0; i < cargoCount; i++)
            {
                if (!TryReadAsciiIdentifierString(1, 80, out _))
                {
                    return false;
                }
            }

            return true;
        }
        finally
        {
            stream.Position = savedPos;
        }
    }

    private bool TryReadInt(out int value)
    {
        var stream = _reader.BaseStream;
        if (stream.Position + 4 > stream.Length)
        {
            value = 0;
            return false;
        }

        value = _reader.ReadInt32();
        return true;
    }

    private bool TryReadBoundedCount(int minInclusive, int maxInclusive, out int count)
    {
        if (!TryReadInt(out count))
        {
            return false;
        }

        return count >= minInclusive && count <= maxInclusive;
    }

    private bool TryReadBoolInt(out bool value)
    {
        value = false;
        if (!TryReadInt(out int raw))
        {
            return false;
        }

        if (raw != 0 && raw != 1)
        {
            return false;
        }

        value = raw == 1;
        return true;
    }

    private bool TryReadAsciiIdentifierString(int minLength, int maxLength, out string value)
    {
        value = "";
        if (!TryReadInt(out int length))
        {
            return false;
        }

        if (length < minLength || length > maxLength)
        {
            return false;
        }

        var stream = _reader.BaseStream;
        if (stream.Position + length > stream.Length)
        {
            return false;
        }

        byte[] bytes = _reader.ReadBytes(length);
        if (!IsAsciiIdentifier(bytes))
        {
            return false;
        }

        value = Encoding.UTF8.GetString(bytes);
        return true;
    }

    private bool LooksLikeAsciiBytes(long position, int length)
    {
        if (length <= 0 || length > 256)
        {
            return false;
        }

        var stream = _reader.BaseStream;
        if (position < 0 || position + length > stream.Length)
        {
            return false;
        }

        long savedPos = stream.Position;
        try
        {
            stream.Position = position;
            byte[] bytes = _reader.ReadBytes(length);
            return bytes.Length == length && IsAsciiIdentifier(bytes);
        }
        finally
        {
            stream.Position = savedPos;
        }
    }

    // ========================================================================
    // Top-level parser
    // ========================================================================

    private SavedGameState ParseSavedGame()
    {
        // -- Header --
        var fileFormat = ReadInt();
        if (fileFormat != 2 && fileFormat != 7 && fileFormat != 8 && fileFormat != 9 && fileFormat != 11)
        {
            throw new Exception(
                $"Unsupported file format version: {fileFormat}. Expected 2, 7, 8, 9, or 11.");
        }

        var randomNative = fileFormat >= 11 ? ReadBool() : false;
        var dlcEnabled = fileFormat >= 7 ? ReadBool() : false;
        var difficulty = ReadInt();
        var totalShipsDefeated = ReadInt();
        var totalBeaconsExplored = ReadInt();
        var totalScrapCollected = ReadInt();
        var totalCrewHired = ReadInt();
        var playerShipName = ReadString();
        var playerShipBlueprintId = ReadString();
        var oneBasedSectorNumber = ReadInt();
        var unknownBeta = ReadInt();

        // State vars
        var stateVarCount = ReadInt();
        var stateVars = new List<StateVar>();
        for (int i = 0; i < stateVarCount; i++)
        {
            stateVars.Add(new StateVar { Key = ReadString(), Value = ReadInt() });
        }

        // -- Player Ship --
        DebugLog($"Starting player ship parse");
        var playerShip = ParseShipState(fileFormat);
        DebugLog($"Finished player ship parse");

        // -- Cargo --
        DebugLog("Starting cargo");
        var cargoCount = ReadInt();
        var cargoIdList = new List<string>();
        for (int i = 0; i < cargoCount; i++)
        {
            cargoIdList.Add(ReadString());
        }

        // -- Sector Map --
        DebugLog("Starting sector map");
        var sectorTreeSeed = ReadInt();
        var sectorLayoutSeed = ReadInt();
        var rebelFleetOffset = ReadInt();
        var rebelFleetFudge = ReadInt();
        var rebelPursuitMod = ReadInt();

        int currentBeaconId = 0;
        bool waiting = false;
        int waitEventSeed = 0;
        string unknownEpsilon = "";
        bool sectorHazardsVisible = false;
        bool rebelFlagshipVisible = false;
        int rebelFlagshipHop = 0;
        bool rebelFlagshipMoving = false;
        bool rebelFlagshipRetreating = false;
        int rebelFlagshipBaseTurns = 0;
        bool f2SectorHazardsVisible = false;
        bool f2RebelFlagshipVisible = false;
        int f2RebelFlagshipHop = 0;
        bool f2RebelFlagshipMoving = false;

        if (fileFormat >= 7)
        {
            currentBeaconId = ReadInt();
            waiting = ReadBool();
            waitEventSeed = ReadInt();
            unknownEpsilon = ReadString();
            sectorHazardsVisible = ReadBool();
            rebelFlagshipVisible = ReadBool();
            rebelFlagshipHop = ReadInt();
            rebelFlagshipMoving = ReadBool();
            rebelFlagshipRetreating = ReadBool();
            rebelFlagshipBaseTurns = ReadInt();
        }

        if (fileFormat == 2)
        {
            f2SectorHazardsVisible = ReadBool();
            f2RebelFlagshipVisible = ReadBool();
            f2RebelFlagshipHop = ReadInt();
            f2RebelFlagshipMoving = ReadBool();
        }

        // Sector visitation
        var sectorVisitCount = ReadInt();
        var sectorVisitation = new List<bool>();
        for (int i = 0; i < sectorVisitCount; i++)
        {
            sectorVisitation.Add(ReadBool());
        }
        var sectorNumber = ReadInt();
        var sectorIsHiddenCrystalWorlds = ReadBool();

        // Beacons
        DebugLog($"Starting beacons");
        var beaconCount = ReadInt();
        var beacons = new List<BeaconState>();
        for (int i = 0; i < beaconCount; i++)
        {
            beacons.Add(ParseBeaconState(fileFormat));
        }

        // Quest events
        var questEventCount = ReadInt();
        var questEventMap = new List<QuestEvent>();
        for (int i = 0; i < questEventCount; i++)
        {
            questEventMap.Add(new QuestEvent
            {
                QuestEventId = ReadString(),
                QuestBeaconId = ReadInt(),
            });
        }

        var distantCount = ReadInt();
        var distantQuestEventList = new List<string>();
        for (int i = 0; i < distantCount; i++)
        {
            distantQuestEventList.Add(ReadString());
        }

        // Format 2 tail
        int f2CurrentBeaconId = 0;
        RebelFlagshipState? f2RebelFlagship = null;
        bool nearbyShipPresent = false;
        ShipState? nearbyShip = null;
        NearbyShipAIState? nearbyShipAi = null;

        if (fileFormat == 2)
        {
            f2CurrentBeaconId = ReadInt();
            nearbyShipPresent = ReadBool();
            if (nearbyShipPresent)
            {
                nearbyShip = ParseShipState(fileFormat);
                f2RebelFlagship = ParseRebelFlagshipState();
            }
        }

        // Format 7+
        int unknownMu = 0;
        EncounterState? encounter = null;
        EnvironmentState? environment = null;
        var projectiles = new List<ProjectileState>();
        ExtendedShipInfo? playerExtendedInfo = null;
        ExtendedShipInfo? nearbyExtendedInfo = null;
        int unknownNu = 0;
        int? unknownXi = null;
        bool autofire = false;
        RebelFlagshipState? rebelFlagship = null;

        if (fileFormat >= 7)
        {
            DebugLog("Starting format 7+ tail (encounter, nearby ship, env, projectiles, extended info)");
            unknownMu = ReadInt();
            encounter = ParseEncounterState(fileFormat);
            DebugLog("Finished encounter");

            nearbyShipPresent = ReadBool();
            if (nearbyShipPresent)
            {
                nearbyShip = ParseShipState(fileFormat);
                nearbyShipAi = ParseNearbyShipAI();
            }

            DebugLog($"Nearby ship present: {nearbyShipPresent}");
            environment = ParseEnvironmentState();
            DebugLog("Finished environment");

            var projectileCount = ReadInt();
            for (int i = 0; i < projectileCount; i++)
            {
                projectiles.Add(ParseProjectileState(fileFormat));
            }

            DebugLog($"Finished projectiles ({projectiles.Count})");
            DebugLog("Starting player extended ship info");
            playerExtendedInfo = ParseExtendedShipInfo(playerShip, fileFormat);
            DebugLog("Finished player extended ship info");
            if (nearbyShip != null)
            {
                nearbyExtendedInfo = ParseExtendedShipInfo(nearbyShip, fileFormat);
            }

            unknownNu = ReadInt();
            if (nearbyShip != null)
            {
                unknownXi = ReadInt();
            }
            autofire = ReadBool();

            rebelFlagship = ParseRebelFlagshipState();
        }

        return new SavedGameState
        {
            FileFormat = fileFormat,
            RandomNative = randomNative,
            DlcEnabled = dlcEnabled,
            Difficulty = difficulty,
            TotalShipsDefeated = totalShipsDefeated,
            TotalBeaconsExplored = totalBeaconsExplored,
            TotalScrapCollected = totalScrapCollected,
            TotalCrewHired = totalCrewHired,
            PlayerShipName = playerShipName,
            PlayerShipBlueprintId = playerShipBlueprintId,
            OneBasedSectorNumber = oneBasedSectorNumber,
            UnknownBeta = unknownBeta,
            StateVars = stateVars,
            PlayerShip = playerShip,
            CargoIdList = cargoIdList,
            SectorTreeSeed = sectorTreeSeed,
            SectorLayoutSeed = sectorLayoutSeed,
            RebelFleetOffset = rebelFleetOffset,
            RebelFleetFudge = rebelFleetFudge,
            RebelPursuitMod = rebelPursuitMod,
            CurrentBeaconId = currentBeaconId,
            Waiting = waiting,
            WaitEventSeed = waitEventSeed,
            UnknownEpsilon = unknownEpsilon,
            SectorHazardsVisible = sectorHazardsVisible,
            RebelFlagshipVisible = rebelFlagshipVisible,
            RebelFlagshipHop = rebelFlagshipHop,
            RebelFlagshipMoving = rebelFlagshipMoving,
            RebelFlagshipRetreating = rebelFlagshipRetreating,
            RebelFlagshipBaseTurns = rebelFlagshipBaseTurns,
            F2SectorHazardsVisible = f2SectorHazardsVisible,
            F2RebelFlagshipVisible = f2RebelFlagshipVisible,
            F2RebelFlagshipHop = f2RebelFlagshipHop,
            F2RebelFlagshipMoving = f2RebelFlagshipMoving,
            SectorVisitation = sectorVisitation,
            SectorNumber = sectorNumber,
            SectorIsHiddenCrystalWorlds = sectorIsHiddenCrystalWorlds,
            Beacons = beacons,
            QuestEventMap = questEventMap,
            DistantQuestEventList = distantQuestEventList,
            F2CurrentBeaconId = f2CurrentBeaconId,
            UnknownMu = unknownMu,
            Encounter = encounter,
            NearbyShipPresent = nearbyShipPresent,
            NearbyShip = nearbyShip,
            NearbyShipAi = nearbyShipAi,
            Environment = environment,
            Projectiles = projectiles,
            PlayerExtendedInfo = playerExtendedInfo,
            NearbyExtendedInfo = nearbyExtendedInfo,
            UnknownNu = unknownNu,
            UnknownXi = unknownXi,
            Autofire = autofire,
            RebelFlagship = rebelFlagship,
            F2RebelFlagship = f2RebelFlagship,
        };
    }

    // ========================================================================
    // Ship State
    // ========================================================================

    private ShipState ParseShipState(int fmt)
    {
        var shipBlueprintId = ReadString();
        var shipName = ReadString();
        var shipGfxBaseName = ReadString();
        string? extraShipStringBeforeCrew = null;

        var startingCrewCount = ReadInt();
        if (startingCrewCount > 12 && LooksLikeAsciiBytes(_reader.BaseStream.Position, startingCrewCount))
        {
            // Some nearby/enemy ship entries include an extra string before the crew list.
            // Preserve it explicitly and then read the actual crew count.
            var extraShipStringBytes = _reader.ReadBytes(startingCrewCount);
            extraShipStringBeforeCrew = Encoding.UTF8.GetString(extraShipStringBytes);
            startingCrewCount = ReadInt();
            DebugLog($"Detected extra ship string extension '{extraShipStringBeforeCrew}' before starting crew list");
        }

        var startingCrew = new List<StartingCrewMember>();
        for (int i = 0; i < startingCrewCount; i++)
        {
            startingCrew.Add(new StartingCrewMember { Race = ReadString(), Name = ReadString() });
        }

        bool hostile = false;
        int jumpChargeTicks = 0;
        bool jumping = false;
        int jumpAnimTicks = 0;
        if (fmt >= 7)
        {
            hostile = ReadBool();
            jumpChargeTicks = ReadInt();
            jumping = ReadBool();
            jumpAnimTicks = ReadInt();
        }

        var hullAmt = ReadInt();
        var fuelAmt = ReadInt();
        var dronePartsAmt = ReadInt();
        var missilesAmt = ReadInt();
        var scrapAmt = ReadInt();

        var crewCount = ReadInt();
        DebugLog($"Ship '{shipName}' ({shipBlueprintId}): hull={hullAmt}, crew={crewCount}");
        var crew = new List<CrewState>();
        for (int i = 0; i < crewCount; i++)
        {
            crew.Add(ParseCrewState(fmt));
        }
        DebugLog($"Finished crew parse");

        var reservePowerCapacity = ReadInt();
        var systemTypes = SystemTypeHelper.GetOrderedSystemTypes(fmt);
        var systems = new List<SystemState>();
        foreach (var sysType in systemTypes)
        {
            systems.Add(ParseSystemState(sysType, fmt));
        }

        DebugLog($"Finished systems parse ({systems.Count} systems)");

        ClonebayInfo? clonebayInfo = null;
        BatteryInfo? batteryInfo = null;
        ShieldsInfo? shieldsInfo = null;
        CloakingInfo? cloakingInfo = null;

        if (fmt >= 7)
        {
            var clonebaySys = systems.Find(s => s.SystemType == SystemType.Clonebay);
            if (clonebaySys != null && clonebaySys.Capacity > 0)
            {
                clonebayInfo = new ClonebayInfo
                {
                    BuildTicks = ReadInt(),
                    BuildTicksGoal = ReadInt(),
                    DoomTicks = ReadInt(),
                };
            }

            var batterySys = systems.Find(s => s.SystemType == SystemType.Battery);
            if (batterySys != null && batterySys.Capacity > 0)
            {
                batteryInfo = new BatteryInfo
                {
                    Active = ReadBool(),
                    UsedBattery = ReadInt(),
                    DischargeTicks = ReadInt(),
                };
            }

            shieldsInfo = new ShieldsInfo
            {
                ShieldLayers = ReadInt(),
                EnergyShieldLayers = ReadInt(),
                EnergyShieldMax = ReadInt(),
                ShieldRechargeTicks = ReadInt(),
                ShieldDropAnimOn = ReadBool(),
                ShieldDropAnimTicks = ReadInt(),
                ShieldRaiseAnimOn = ReadBool(),
                ShieldRaiseAnimTicks = ReadInt(),
                EnergyShieldAnimOn = ReadBool(),
                EnergyShieldAnimTicks = ReadInt(),
                UnknownLambda = ReadInt(),
                UnknownMu = ReadInt(),
            };

            var cloakingSys = systems.Find(s => s.SystemType == SystemType.Cloaking);
            if (cloakingSys != null && cloakingSys.Capacity > 0)
            {
                cloakingInfo = new CloakingInfo
                {
                    UnknownAlpha = ReadInt(),
                    UnknownBeta = ReadInt(),
                    CloakTicksGoal = ReadInt(),
                    CloakTicks = ReadMinMaxedInt(),
                };
            }
        }

        DebugLog($"Finished clonebay/battery/shields/cloaking");

        // Room, breach, door data requires ship layout files we don't have.
        // Skip this section by scanning forward to find the weapons section.
        // Store raw bytes for round-trip fidelity.
        long roomDataStart = _reader.BaseStream.Position;
        var rooms = new List<RoomState>();
        var breaches = new List<BreachState>();
        var doors = new List<DoorState>();
        int cloakAnimTicks = 0;
        var lockdownCrystals = new List<LockdownCrystal>();

        // Find the weapon section by scanning for weapon count + valid weapon string pattern.
        // Weapon section: count(int) + for each: stringLen(int) + string + armed(int)
        long weaponSectionPos = FindWeaponSection(roomDataStart, fmt);
        DebugLog($"Skipped room/breach/door data ({weaponSectionPos - roomDataStart} bytes, {roomDataStart}->{weaponSectionPos})");
        int rawRoomDoorLength = (int)(weaponSectionPos - roomDataStart);
        _reader.BaseStream.Position = roomDataStart;
        byte[] opaqueRoomDoorBytes = _reader.ReadBytes(rawRoomDoorLength);
        _reader.BaseStream.Position = weaponSectionPos;

        // Weapons
        var weaponCount = ReadInt();
        var weapons = new List<WeaponState>();
        for (int i = 0; i < weaponCount; i++)
        {
            var weaponId = ReadString();
            var armed = ReadBool();
            var cooldownTicks = fmt == 2 ? ReadInt() : 0;
            weapons.Add(new WeaponState
            {
                WeaponId = weaponId,
                Armed = armed,
                CooldownTicks = cooldownTicks,
            });
        }

        DebugLog($"Weapons: {weaponCount}");
        // Drones
        var droneCount = ReadInt();
        var drones = new List<DroneState>();
        for (int i = 0; i < droneCount; i++)
        {
            drones.Add(ParseDroneState());
        }

        DebugLog($"Drones: {droneCount}");
        // Augments
        var augmentCount = ReadInt();
        var augmentIds = new List<string>();
        for (int i = 0; i < augmentCount; i++)
        {
            augmentIds.Add(ReadString());
        }

        return new ShipState
        {
            ShipBlueprintId = shipBlueprintId,
            ShipName = shipName,
            ShipGfxBaseName = shipGfxBaseName,
            ExtraShipStringBeforeCrew = extraShipStringBeforeCrew,
            StartingCrew = startingCrew,
            Hostile = hostile,
            JumpChargeTicks = jumpChargeTicks,
            Jumping = jumping,
            JumpAnimTicks = jumpAnimTicks,
            HullAmt = hullAmt,
            FuelAmt = fuelAmt,
            DronePartsAmt = dronePartsAmt,
            MissilesAmt = missilesAmt,
            ScrapAmt = scrapAmt,
            Crew = crew,
            ReservePowerCapacity = reservePowerCapacity,
            Systems = systems,
            ClonebayInfo = clonebayInfo,
            BatteryInfo = batteryInfo,
            ShieldsInfo = shieldsInfo,
            CloakingInfo = cloakingInfo,
            Rooms = rooms,
            Breaches = breaches,
            Doors = doors,
            CloakAnimTicks = cloakAnimTicks,
            LockdownCrystals = lockdownCrystals,
            OpaqueRoomDoorBytes = opaqueRoomDoorBytes,
            Weapons = weapons,
            Drones = drones,
            AugmentIds = augmentIds,
        };
    }

    // ========================================================================
    // Crew
    // ========================================================================

    private CrewState ParseCrewState(int fmt)
    {
        DebugLog($"Parsing crew member");
        var name = ReadString();
        var race = ReadString();
        DebugLog($"  Crew: '{name}' race='{race}'");
        var enemyBoardingDrone = ReadBool();
        var health = ReadInt();
        var spriteX = ReadInt();
        var spriteY = ReadInt();
        var roomId = ReadInt();
        var roomSquare = ReadInt();
        var playerControlled = ReadBool();

        int cloneReady = 0;
        int deathOrder = 0;
        var spriteTintIndices = new List<int>();
        bool mindControlled = false;
        int savedRoomSquare = 0;
        int savedRoomId = 0;
        if (fmt >= 7)
        {
            cloneReady = ReadInt();
            deathOrder = ReadInt();
            var tintCount = ReadInt();
            for (int i = 0; i < tintCount; i++) spriteTintIndices.Add(ReadInt());
            mindControlled = ReadBool();
            savedRoomSquare = ReadInt();
            savedRoomId = ReadInt();
        }

        var pilotSkill = ReadInt();
        var engineSkill = ReadInt();
        var shieldSkill = ReadInt();
        var weaponSkill = ReadInt();
        var repairSkill = ReadInt();
        var combatSkill = ReadInt();
        var male = ReadBool();

        var repairs = ReadInt();
        var combatKills = ReadInt();
        var pilotedEvasions = ReadInt();
        var jumpsSurvived = ReadInt();
        var skillMasteriesEarned = ReadInt();

        int stunTicks = 0;
        int healthBoost = 0;
        int clonebayPriority = 0;
        int damageBoost = 0;
        int unknownLambda = 0;
        int universalDeathCount = 0;
        if (fmt >= 7)
        {
            stunTicks = ReadInt();
            healthBoost = ReadInt();
            clonebayPriority = ReadInt();
            damageBoost = ReadInt();
            unknownLambda = ReadInt();
            universalDeathCount = ReadInt();
        }

        bool pilotMasteryOne = false;
        bool pilotMasteryTwo = false;
        bool engineMasteryOne = false;
        bool engineMasteryTwo = false;
        bool shieldMasteryOne = false;
        bool shieldMasteryTwo = false;
        bool weaponMasteryOne = false;
        bool weaponMasteryTwo = false;
        bool repairMasteryOne = false;
        bool repairMasteryTwo = false;
        bool combatMasteryOne = false;
        bool combatMasteryTwo = false;
        if (fmt >= 8)
        {
            pilotMasteryOne = ReadBool();
            pilotMasteryTwo = ReadBool();
            engineMasteryOne = ReadBool();
            engineMasteryTwo = ReadBool();
            shieldMasteryOne = ReadBool();
            shieldMasteryTwo = ReadBool();
            weaponMasteryOne = ReadBool();
            weaponMasteryTwo = ReadBool();
            repairMasteryOne = ReadBool();
            repairMasteryTwo = ReadBool();
            combatMasteryOne = ReadBool();
            combatMasteryTwo = ReadBool();
        }

        bool unknownNu = false;
        AnimState? teleportAnim = null;
        bool unknownPhi = false;
        if (fmt >= 7)
        {
            unknownNu = ReadBool();
            teleportAnim = ParseAnimState();
            unknownPhi = ReadBool();
        }

        int? lockdownRechargeTicks = null;
        int? lockdownRechargeTicksGoal = null;
        int? unknownOmega = null;
        if (fmt >= 7 && race == "crystal")
        {
            lockdownRechargeTicks = ReadInt();
            lockdownRechargeTicksGoal = ReadInt();
            unknownOmega = ReadInt();
        }

        return new CrewState
        {
            Name = name,
            Race = race,
            EnemyBoardingDrone = enemyBoardingDrone,
            Health = health,
            SpriteX = spriteX,
            SpriteY = spriteY,
            RoomId = roomId,
            RoomSquare = roomSquare,
            PlayerControlled = playerControlled,
            CloneReady = cloneReady,
            DeathOrder = deathOrder,
            SpriteTintIndices = spriteTintIndices,
            MindControlled = mindControlled,
            SavedRoomSquare = savedRoomSquare,
            SavedRoomId = savedRoomId,
            PilotSkill = pilotSkill,
            EngineSkill = engineSkill,
            ShieldSkill = shieldSkill,
            WeaponSkill = weaponSkill,
            RepairSkill = repairSkill,
            CombatSkill = combatSkill,
            Male = male,
            Repairs = repairs,
            CombatKills = combatKills,
            PilotedEvasions = pilotedEvasions,
            JumpsSurvived = jumpsSurvived,
            SkillMasteriesEarned = skillMasteriesEarned,
            StunTicks = stunTicks,
            HealthBoost = healthBoost,
            ClonebayPriority = clonebayPriority,
            DamageBoost = damageBoost,
            UnknownLambda = unknownLambda,
            UniversalDeathCount = universalDeathCount,
            PilotMasteryOne = pilotMasteryOne,
            PilotMasteryTwo = pilotMasteryTwo,
            EngineMasteryOne = engineMasteryOne,
            EngineMasteryTwo = engineMasteryTwo,
            ShieldMasteryOne = shieldMasteryOne,
            ShieldMasteryTwo = shieldMasteryTwo,
            WeaponMasteryOne = weaponMasteryOne,
            WeaponMasteryTwo = weaponMasteryTwo,
            RepairMasteryOne = repairMasteryOne,
            RepairMasteryTwo = repairMasteryTwo,
            CombatMasteryOne = combatMasteryOne,
            CombatMasteryTwo = combatMasteryTwo,
            UnknownNu = unknownNu,
            TeleportAnim = teleportAnim,
            UnknownPhi = unknownPhi,
            LockdownRechargeTicks = lockdownRechargeTicks,
            LockdownRechargeTicksGoal = lockdownRechargeTicksGoal,
            UnknownOmega = unknownOmega,
        };
    }

    // ========================================================================
    // System
    // ========================================================================

    private SystemState ParseSystemState(SystemType systemType, int fmt)
    {
        var capacity = ReadInt();
        if (capacity == 0)
        {
            return new SystemState
            {
                SystemType = systemType,
                Capacity = 0,
                Power = 0,
                DamagedBars = 0,
                IonizedBars = 0,
                DeionizationTicks = int.MinValue,
                RepairProgress = 0,
                DamageProgress = 0,
                BatteryPower = 0,
                HackLevel = 0,
                Hacked = false,
                TemporaryCapacityCap = 0,
                TemporaryCapacityLoss = 0,
                TemporaryCapacityDivisor = 0,
            };
        }

        var power = ReadInt();
        var damagedBars = ReadInt();
        var ionizedBars = ReadInt();
        var deionizationTicks = ReadMinMaxedInt();
        var repairProgress = ReadInt();
        var damageProgress = ReadInt();

        int batteryPower = 0;
        int hackLevel = 0;
        bool hacked = false;
        int temporaryCapacityCap = 0;
        int temporaryCapacityLoss = 0;
        int temporaryCapacityDivisor = 0;
        if (fmt >= 7)
        {
            batteryPower = ReadInt();
            hackLevel = ReadInt();
            hacked = ReadBool();
            temporaryCapacityCap = ReadInt();
            temporaryCapacityLoss = ReadInt();
            temporaryCapacityDivisor = ReadInt();
        }

        return new SystemState
        {
            SystemType = systemType,
            Capacity = capacity,
            Power = power,
            DamagedBars = damagedBars,
            IonizedBars = ionizedBars,
            DeionizationTicks = deionizationTicks,
            RepairProgress = repairProgress,
            DamageProgress = damageProgress,
            BatteryPower = batteryPower,
            HackLevel = hackLevel,
            Hacked = hacked,
            TemporaryCapacityCap = temporaryCapacityCap,
            TemporaryCapacityLoss = temporaryCapacityLoss,
            TemporaryCapacityDivisor = temporaryCapacityDivisor,
        };
    }

    // ========================================================================
    // Room
    // ========================================================================

    private RoomState ParseRoomState(int squareCount, int fmt)
    {
        var oxygen = ReadInt();
        var squares = new List<SquareState>();
        for (int i = 0; i < squareCount; i++)
        {
            squares.Add(new SquareState
            {
                FireHealth = ReadInt(),
                IgnitionProgress = ReadInt(),
                ExtinguishmentProgress = ReadInt(),
            });
        }

        int stationSquare = -1;
        int stationDirection = 4;
        if (fmt >= 7)
        {
            stationSquare = ReadInt();
            stationDirection = ReadInt();
        }

        return new RoomState
        {
            Oxygen = oxygen,
            Squares = squares,
            StationSquare = stationSquare,
            StationDirection = stationDirection,
        };
    }

    private RoomState ParseRoomStateHeuristic(int fmt)
    {
        var oxygen = ReadInt();

        if (fmt >= 7)
        {
            // Scan for station fields after N squares (each square = 3 ints = 12 bytes).
            // stationSquare: -1 = no station, >= 0 = square index with station.
            // stationDirection: 0-4 (DOWN, RIGHT, UP, LEFT, NONE).
            // Validate by peeking at the value AFTER station fields (must be 0-100 for next room's oxygen,
            // or we're at the end of room data).
            long squaresStart = _reader.BaseStream.Position;
            int bestSquareCount = -1;

            for (int tryCount = 0; tryCount <= 20; tryCount++)
            {
                long candidatePos = squaresStart + tryCount * 12;
                if (candidatePos + 8 > _reader.BaseStream.Length) break;

                _reader.BaseStream.Position = candidatePos;
                int stationSq = ReadInt();
                int stationDir = ReadInt();

                if (stationDir < 0 || stationDir > 4) continue;
                if (stationSq < -1 || stationSq > 100) continue;

                // Every room has at least 1 square, so tryCount=0 is only valid for stSq=-1
                if (tryCount == 0 && stationSq >= 0) continue;

                // If stationSq >= 0 (has station), it must be a valid square index
                if (stationSq >= 0 && tryCount > 0 && stationSq >= tryCount) continue;

                // Peek at next value: should be 0-100 (next room's oxygen or breach count)
                long afterStation = _reader.BaseStream.Position;
                if (afterStation + 4 <= _reader.BaseStream.Length)
                {
                    int nextVal = ReadInt();
                    if (nextVal < 0 || nextVal > 100)
                    {
                        continue; // Next value doesn't look right, try more squares
                    }
                }

                bestSquareCount = tryCount;
                break;
            }

            if (bestSquareCount < 0)
            {
                throw new Exception(
                    $"Could not determine room square count at byte offset {squaresStart}. This may be an unsupported ship layout.");
            }

            _reader.BaseStream.Position = squaresStart;
            var squares = new List<SquareState>();
            for (int i = 0; i < bestSquareCount; i++)
            {
                squares.Add(new SquareState
                {
                    FireHealth = ReadInt(),
                    IgnitionProgress = ReadInt(),
                    ExtinguishmentProgress = ReadInt(),
                });
            }

            var stationSquare = ReadInt();
            var stationDirection = ReadInt();

            return new RoomState
            {
                Oxygen = oxygen,
                Squares = squares,
                StationSquare = stationSquare,
                StationDirection = stationDirection,
            };
        }
        else
        {
            // Format 2: no station fields, assume 1 square per room
            var squares = new List<SquareState>
            {
                new SquareState
                {
                    FireHealth = ReadInt(),
                    IgnitionProgress = ReadInt(),
                    ExtinguishmentProgress = ReadInt(),
                }
            };
            return new RoomState
            {
                Oxygen = oxygen,
                Squares = squares,
                StationSquare = -2,
                StationDirection = 4,
            };
        }
    }

    // ========================================================================
    // Door, Lockdown Crystal, Drone
    // ========================================================================

    private DoorState ParseDoorState(int fmt)
    {
        int currentMaxHealth = 0;
        int health = 0;
        int nominalHealth = 0;
        if (fmt >= 7)
        {
            currentMaxHealth = ReadInt();
            health = ReadInt();
            nominalHealth = ReadInt();
        }
        var open = ReadBool();
        var walkingThrough = ReadBool();
        int unknownDelta = 0;
        int unknownEpsilon = 0;
        if (fmt >= 7)
        {
            unknownDelta = ReadInt();
            unknownEpsilon = ReadInt();
        }
        return new DoorState
        {
            CurrentMaxHealth = currentMaxHealth,
            Health = health,
            NominalHealth = nominalHealth,
            Open = open,
            WalkingThrough = walkingThrough,
            UnknownDelta = unknownDelta,
            UnknownEpsilon = unknownEpsilon,
        };
    }

    private LockdownCrystal ParseLockdownCrystal()
    {
        return new LockdownCrystal
        {
            CurrentPositionX = ReadInt(),
            CurrentPositionY = ReadInt(),
            Speed = ReadInt(),
            GoalPositionX = ReadInt(),
            GoalPositionY = ReadInt(),
            Arrived = ReadBool(),
            Done = ReadBool(),
            Lifetime = ReadInt(),
            SuperFreeze = ReadBool(),
            LockingRoom = ReadInt(),
            AnimDirection = ReadInt(),
            ShardProgress = ReadInt(),
        };
    }

    private DroneState ParseDroneState()
    {
        return new DroneState
        {
            DroneId = ReadString(),
            Armed = ReadBool(),
            PlayerControlled = ReadBool(),
            BodyX = ReadInt(),
            BodyY = ReadInt(),
            BodyRoomId = ReadInt(),
            BodyRoomSquare = ReadInt(),
            Health = ReadInt(),
        };
    }

    // ========================================================================
    // Beacon & Store
    // ========================================================================

    private BeaconState ParseBeaconState(int fmt)
    {
        var visitCount = ReadInt();
        string? bgStarscapeImage = null;
        string? bgSpriteImage = null;
        int? bgSpritePosX = null;
        int? bgSpritePosY = null;
        int? bgSpriteRotation = null;

        if (visitCount > 0)
        {
            bgStarscapeImage = ReadString();
            bgSpriteImage = ReadString();
            bgSpritePosX = ReadInt();
            bgSpritePosY = ReadInt();
            bgSpriteRotation = ReadInt();
        }

        var seen = ReadBool();
        var enemyPresent = ReadBool();
        string? shipEventId = null;
        string? autoBlueprintId = null;
        int? shipEventSeed = null;
        if (enemyPresent)
        {
            shipEventId = ReadString();
            autoBlueprintId = ReadString();
            shipEventSeed = ReadInt();
        }

        var fleetPresence = ReadInt();
        var underAttack = ReadBool();
        var storePresent = ReadBool();
        var store = storePresent ? ParseStoreState(fmt) : null;
        var unknownEta = (fmt >= 8 && fmt < 11) ? ReadBool() : false;

        return new BeaconState
        {
            VisitCount = visitCount,
            BgStarscapeImage = bgStarscapeImage,
            BgSpriteImage = bgSpriteImage,
            BgSpritePosX = bgSpritePosX,
            BgSpritePosY = bgSpritePosY,
            BgSpriteRotation = bgSpriteRotation,
            Seen = seen,
            EnemyPresent = enemyPresent,
            ShipEventId = shipEventId,
            AutoBlueprintId = autoBlueprintId,
            ShipEventSeed = shipEventSeed,
            FleetPresence = fleetPresence,
            UnderAttack = underAttack,
            StorePresent = storePresent,
            Store = store,
            UnknownEta = unknownEta,
        };
    }

    private StoreState ParseStoreState(int fmt)
    {
        var shelfCount = fmt >= 7 ? ReadInt() : 2;
        var shelves = new List<StoreShelf>();
        for (int i = 0; i < shelfCount; i++)
        {
            shelves.Add(ParseStoreShelf(fmt));
        }
        return new StoreState
        {
            ShelfCount = shelfCount,
            Shelves = shelves,
            Fuel = ReadInt(),
            Missiles = ReadInt(),
            DroneParts = ReadInt(),
        };
    }

    private StoreShelf ParseStoreShelf(int fmt)
    {
        var itemType = ReadInt();
        var items = new List<StoreItem>();
        for (int i = 0; i < 3; i++)
        {
            var available = ReadInt();
            if (available < 0)
            {
                items.Add(new StoreItem { Available = available, ItemId = null, ExtraData = null });
            }
            else
            {
                var itemId = ReadString();
                int? extraData = fmt >= 8 ? ReadInt() : null;
                items.Add(new StoreItem { Available = available, ItemId = itemId, ExtraData = extraData });
            }
        }
        return new StoreShelf { ItemType = itemType, Items = items };
    }

    // ========================================================================
    // Encounter, Nearby Ship AI, Environment
    // ========================================================================

    private EncounterState ParseEncounterState(int fmt)
    {
        var shipEventSeed = ReadInt();
        var surrenderEventId = ReadString();
        var escapeEventId = ReadString();
        var destroyedEventId = ReadString();
        var deadCrewEventId = ReadString();
        var gotAwayEventId = ReadString();
        var lastEventId = ReadString();
        int? unknownAlpha = fmt >= 11 ? ReadInt() : null;
        var text = ReadString();
        var affectedCrewSeed = ReadInt();
        var choiceCount = ReadInt();
        var choices = new List<int>();
        for (int i = 0; i < choiceCount; i++) choices.Add(ReadInt());

        return new EncounterState
        {
            ShipEventSeed = shipEventSeed,
            SurrenderEventId = surrenderEventId,
            EscapeEventId = escapeEventId,
            DestroyedEventId = destroyedEventId,
            DeadCrewEventId = deadCrewEventId,
            GotAwayEventId = gotAwayEventId,
            LastEventId = lastEventId,
            UnknownAlpha = unknownAlpha,
            Text = text,
            AffectedCrewSeed = affectedCrewSeed,
            Choices = choices,
        };
    }

    private NearbyShipAIState ParseNearbyShipAI()
    {
        return new NearbyShipAIState
        {
            Surrendered = ReadBool(),
            Escaping = ReadBool(),
            Destroyed = ReadBool(),
            SurrenderThreshold = ReadInt(),
            EscapeThreshold = ReadInt(),
            EscapeTicks = ReadInt(),
            StalemateTriggered = ReadBool(),
            StalemateTicks = ReadInt(),
            BoardingAttempts = ReadInt(),
            BoardersNeeded = ReadInt(),
        };
    }

    private EnvironmentState ParseEnvironmentState()
    {
        var redGiantPresent = ReadBool();
        var pulsarPresent = ReadBool();
        var pdsPresent = ReadBool();
        var vulnerableShips = ReadInt();
        var asteroidsPresent = ReadBool();
        AsteroidFieldState? asteroidField = null;
        if (asteroidsPresent)
        {
            asteroidField = new AsteroidFieldState
            {
                UnknownAlpha = ReadInt(),
                StrayRockTicks = ReadInt(),
                UnknownGamma = ReadInt(),
                BgDriftTicks = ReadInt(),
                CurrentTarget = ReadInt(),
            };
        }

        return new EnvironmentState
        {
            RedGiantPresent = redGiantPresent,
            PulsarPresent = pulsarPresent,
            PdsPresent = pdsPresent,
            VulnerableShips = vulnerableShips,
            AsteroidsPresent = asteroidsPresent,
            AsteroidField = asteroidField,
            SolarFlareFadeTicks = ReadInt(),
            HavocTicks = ReadInt(),
            PdsTicks = ReadInt(),
        };
    }

    // ========================================================================
    // Projectiles
    // ========================================================================

    private ProjectileState ParseProjectileState(int fmt)
    {
        var projectileType = ReadInt();

        if (projectileType == (int)ProjectileType.Invalid)
        {
            return new ProjectileState
            {
                ProjectileType = projectileType,
                CurrentPositionX = 0,
                CurrentPositionY = 0,
                PreviousPositionX = 0,
                PreviousPositionY = 0,
                Speed = 0,
                GoalPositionX = 0,
                GoalPositionY = 0,
                Heading = 0,
                OwnerId = 0,
                SelfId = 0,
                Damage = new DamageState(),
                Lifespan = 0,
                DestinationSpace = 0,
                CurrentSpace = 0,
                TargetId = 0,
                Dead = false,
                DeathAnimId = "",
                FlightAnimId = "",
                DeathAnim = new AnimState(),
                FlightAnim = new AnimState(),
                VelocityX = 0,
                VelocityY = 0,
                Missed = false,
                HitTarget = false,
                HitSolidSound = "",
                HitShieldSound = "",
                MissSound = "",
                EntryAngle = 0,
                StartedDying = false,
                PassedTarget = false,
                BroadcastType = 0,
                BroadcastTarget = false,
                ExtendedInfo = null,
            };
        }

        var currentPositionX = ReadInt();
        var currentPositionY = ReadInt();
        var previousPositionX = ReadInt();
        var previousPositionY = ReadInt();
        var speed = ReadInt();
        var goalPositionX = ReadInt();
        var goalPositionY = ReadInt();
        var heading = ReadInt();
        var ownerId = ReadInt();
        var selfId = ReadInt();
        var damage = ParseDamageState();
        var lifespan = ReadInt();
        var destinationSpace = ReadInt();
        var currentSpace = ReadInt();
        var targetId = ReadInt();
        var dead = ReadBool();
        var deathAnimId = ReadString();
        var flightAnimId = ReadString();
        var deathAnim = ParseAnimState();
        var flightAnim = ParseAnimState();
        var velocityX = ReadInt();
        var velocityY = ReadInt();
        var missed = ReadBool();
        var hitTarget = ReadBool();
        var hitSolidSound = ReadString();
        var hitShieldSound = ReadString();
        var missSound = ReadString();
        var entryAngle = ReadMinMaxedInt();
        var startedDying = ReadBool();
        var passedTarget = ReadBool();
        var broadcastType = ReadInt();
        var broadcastTarget = ReadBool();

        ExtendedProjectileInfo? extendedInfo = null;
        if (projectileType == (int)ProjectileType.LaserOrBurst)
        {
            extendedInfo = new ExtendedProjectileInfo
            {
                Type = "Laser",
                UnknownAlpha = ReadInt(),
                Spin = ReadInt(),
            };
        }
        else if (projectileType == (int)ProjectileType.Bomb)
        {
            extendedInfo = new ExtendedProjectileInfo
            {
                Type = "Bomb",
                UnknownAlpha = ReadInt(),
                FuseTicks = ReadInt(),
                UnknownGamma = ReadInt(),
                UnknownDelta = ReadInt(),
                Arrived = ReadBool(),
            };
        }
        else if (projectileType == (int)ProjectileType.Beam)
        {
            extendedInfo = new ExtendedProjectileInfo
            {
                Type = "Beam",
                EmissionEndX = ReadInt(),
                EmissionEndY = ReadInt(),
                StrafeSourceX = ReadInt(),
                StrafeSourceY = ReadInt(),
                StrafeEndX = ReadInt(),
                StrafeEndY = ReadInt(),
                UnknownBetaX = ReadInt(),
                UnknownBetaY = ReadInt(),
                SwathEndX = ReadInt(),
                SwathEndY = ReadInt(),
                SwathStartX = ReadInt(),
                SwathStartY = ReadInt(),
                UnknownGamma = ReadInt(),
                SwathLength = ReadInt(),
                UnknownDelta = ReadInt(),
                UnknownEpsilonX = ReadInt(),
                UnknownEpsilonY = ReadInt(),
                UnknownZeta = ReadInt(),
                UnknownEta = ReadInt(),
                EmissionAngle = ReadInt(),
                UnknownIota = ReadBool(),
                UnknownKappa = ReadBool(),
                FromDronePod = ReadBool(),
                UnknownMu = ReadBool(),
                UnknownNu = ReadBool(),
            };
        }
        else if (projectileType == (int)ProjectileType.Pds && fmt >= 11)
        {
            extendedInfo = new ExtendedProjectileInfo
            {
                Type = "Pds",
                UnknownAlpha = ReadInt(),
                UnknownBeta = ReadInt(),
                UnknownGamma = ReadInt(),
                UnknownDelta = ReadInt(),
                UnknownEpsilon = ReadInt(),
                UnknownZetaAnim = ParseAnimState(),
            };
        }

        return new ProjectileState
        {
            ProjectileType = projectileType,
            CurrentPositionX = currentPositionX,
            CurrentPositionY = currentPositionY,
            PreviousPositionX = previousPositionX,
            PreviousPositionY = previousPositionY,
            Speed = speed,
            GoalPositionX = goalPositionX,
            GoalPositionY = goalPositionY,
            Heading = heading,
            OwnerId = ownerId,
            SelfId = selfId,
            Damage = damage,
            Lifespan = lifespan,
            DestinationSpace = destinationSpace,
            CurrentSpace = currentSpace,
            TargetId = targetId,
            Dead = dead,
            DeathAnimId = deathAnimId,
            FlightAnimId = flightAnimId,
            DeathAnim = deathAnim,
            FlightAnim = flightAnim,
            VelocityX = velocityX,
            VelocityY = velocityY,
            Missed = missed,
            HitTarget = hitTarget,
            HitSolidSound = hitSolidSound,
            HitShieldSound = hitShieldSound,
            MissSound = missSound,
            EntryAngle = entryAngle,
            StartedDying = startedDying,
            PassedTarget = passedTarget,
            BroadcastType = broadcastType,
            BroadcastTarget = broadcastTarget,
            ExtendedInfo = extendedInfo,
        };
    }

    // ========================================================================
    // Damage & Animation
    // ========================================================================

    private DamageState ParseDamageState()
    {
        return new DamageState
        {
            HullDamage = ReadInt(),
            ShieldPiercing = ReadInt(),
            FireChance = ReadInt(),
            BreachChance = ReadInt(),
            IonDamage = ReadInt(),
            SystemDamage = ReadInt(),
            PersonnelDamage = ReadInt(),
            HullBuster = ReadBool(),
            OwnerId = ReadInt(),
            SelfId = ReadInt(),
            Lockdown = ReadBool(),
            CrystalShard = ReadBool(),
            StunChance = ReadInt(),
            StunAmount = ReadInt(),
        };
    }

    private AnimState ParseAnimState()
    {
        return new AnimState
        {
            Playing = ReadBool(),
            Looping = ReadBool(),
            CurrentFrame = ReadInt(),
            ProgressTicks = ReadInt(),
            Scale = ReadInt(),
            X = ReadInt(),
            Y = ReadInt(),
        };
    }

    // ========================================================================
    // Extended Ship Info (format 7+)
    // ========================================================================

    private ExtendedShipInfo ParseExtendedShipInfo(ShipState ship, int fmt)
    {
        var hackingSys = ship.Systems.Find(s => s.SystemType == SystemType.Hacking);
        HackingInfo? hackingInfo = null;
        if (hackingSys != null && hackingSys.Capacity > 0)
        {
            hackingInfo = new HackingInfo
            {
                TargetSystemType = ReadInt(),
                UnknownBeta = ReadInt(),
                DronePodVisible = ReadBool(),
                UnknownDelta = ReadInt(),
                DisruptionTicks = ReadInt(),
                DisruptionTicksGoal = ReadInt(),
                Disrupting = ReadBool(),
                DronePod = ParseHackingDronePod(),
            };
        }

        var mindSys = ship.Systems.Find(s => s.SystemType == SystemType.MindControl);
        MindControlInfo? mindControlInfo = null;
        if (mindSys != null && mindSys.Capacity > 0)
        {
            mindControlInfo = new MindControlInfo
            {
                MindControlTicks = ReadInt(),
                MindControlTicksGoal = ReadInt(),
            };
        }

        var weaponModules = new List<WeaponModule>();
        for (int i = 0; i < ship.Weapons.Count; i++)
        {
            weaponModules.Add(ParseWeaponModule(fmt));
        }

        var droneModules = new List<DroneModule>();
        for (int i = 0; i < ship.Drones.Count; i++)
        {
            droneModules.Add(ParseDroneModule());
        }

        return new ExtendedShipInfo
        {
            HackingInfo = hackingInfo,
            MindControlInfo = mindControlInfo,
            WeaponModules = weaponModules,
            DroneModules = droneModules,
        };
    }

    private WeaponModule ParseWeaponModule(int fmt)
    {
        var cooldownTicks = ReadInt();
        var cooldownGoal = ReadInt();
        var subcooldownTicks = ReadInt();
        var subcooldownTicksGoal = ReadInt();
        var boost = ReadInt();
        var charge = ReadInt();
        var currentTargetsCount = ReadInt();
        var weaponAnim = ParseAnimState();
        var protractAnimTicks = ReadInt();
        var firing = ReadBool();
        var fireWhenReady = ReadBool();
        var targetId = ReadInt();
        AnimState? hackAnim = fmt >= 9 ? ParseAnimState() : null;
        var isOnFire = ReadBool();
        var fireId = ReadInt();
        var autofire = ReadBool();

        return new WeaponModule
        {
            CooldownTicks = cooldownTicks,
            CooldownGoal = cooldownGoal,
            SubcooldownTicks = subcooldownTicks,
            SubcooldownTicksGoal = subcooldownTicksGoal,
            Boost = boost,
            Charge = charge,
            CurrentTargetsCount = currentTargetsCount,
            WeaponAnim = weaponAnim,
            ProtractAnimTicks = protractAnimTicks,
            Firing = firing,
            FireWhenReady = fireWhenReady,
            TargetId = targetId,
            HackAnim = hackAnim,
            IsOnFire = isOnFire,
            FireId = fireId,
            Autofire = autofire,
        };
    }

    private DroneModule ParseDroneModule()
    {
        var deployed = ReadBool();
        var armed = ReadBool();
        ExtendedDroneInfo? extendedDroneInfo = null;
        if (deployed)
        {
            extendedDroneInfo = new ExtendedDroneInfo
            {
                BodyX = ReadInt(),
                BodyY = ReadInt(),
                CurrentSpace = ReadInt(),
                DestinationSpace = ReadInt(),
                CurrentPositionX = ReadInt(),
                CurrentPositionY = ReadInt(),
                PreviousPositionX = ReadInt(),
                PreviousPositionY = ReadInt(),
                GoalPositionX = ReadInt(),
                GoalPositionY = ReadInt(),
            };
        }
        return new DroneModule
        {
            Deployed = deployed,
            Armed = armed,
            ExtendedDroneInfo = extendedDroneInfo,
        };
    }

    // ========================================================================
    // Hacking Drone Pod
    // ========================================================================

    private DronePodState ParseHackingDronePod()
    {
        return new DronePodState
        {
            MourningTicks = ReadInt(),
            CurrentSpace = ReadInt(),
            DestinationSpace = ReadInt(),
            CurrentPositionX = ReadMinMaxedInt(),
            CurrentPositionY = ReadMinMaxedInt(),
            PreviousPositionX = ReadMinMaxedInt(),
            PreviousPositionY = ReadMinMaxedInt(),
            GoalPositionX = ReadMinMaxedInt(),
            GoalPositionY = ReadMinMaxedInt(),
            UnknownEpsilon = ReadMinMaxedInt(),
            UnknownZeta = ReadMinMaxedInt(),
            NextTargetX = ReadMinMaxedInt(),
            NextTargetY = ReadMinMaxedInt(),
            UnknownIota = ReadMinMaxedInt(),
            UnknownKappa = ReadMinMaxedInt(),
            BuildupTicks = ReadInt(),
            StationaryTicks = ReadInt(),
            CooldownTicks = ReadInt(),
            OrbitAngle = ReadInt(),
            TurretAngle = ReadInt(),
            UnknownXi = ReadInt(),
            HopsToLive = ReadMinMaxedInt(),
            UnknownPi = ReadInt(),
            UnknownRho = ReadInt(),
            OverloadTicks = ReadInt(),
            UnknownTau = ReadInt(),
            UnknownUpsilon = ReadInt(),
            DeltaPositionX = ReadInt(),
            DeltaPositionY = ReadInt(),
            DeathAnim = ParseAnimState(),
            // Hacking drone-specific extension
            AttachPositionX = ReadInt(),
            AttachPositionY = ReadInt(),
            HackUnknownGamma = ReadInt(),
            HackUnknownDelta = ReadInt(),
            LandingAnim = ParseAnimState(),
            ExtensionAnim = ParseAnimState(),
        };
    }

    // ========================================================================
    // Rebel Flagship
    // ========================================================================

    private RebelFlagshipState ParseRebelFlagshipState()
    {
        var unknownAlpha = ReadInt();
        var pendingStage = ReadInt();
        var unknownGamma = ReadInt();
        var unknownDelta = ReadInt();
        var occupancyCount = ReadInt();
        var previousOccupancy = new List<int>();
        for (int i = 0; i < occupancyCount; i++)
        {
            previousOccupancy.Add(ReadInt());
        }
        return new RebelFlagshipState
        {
            UnknownAlpha = unknownAlpha,
            PendingStage = pendingStage,
            UnknownGamma = unknownGamma,
            UnknownDelta = unknownDelta,
            PreviousOccupancy = previousOccupancy,
        };
    }
}
