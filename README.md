# FTL Save Editor

Desktop save game editor for **FTL: Faster Than Light**, including support for **Hyperspace** and **Multiverse** mod saves.

C# WPF application targeting Windows (`.NET 8`).

## Features

### Three Parse Modes

| Mode | Editable Fields | When Used |
|------|----------------|-----------|
| **Full** | Everything (ship, crew, systems, weapons, drones, augments, cargo, beacons, environment) | Vanilla and Hyperspace saves that parse completely |
| **Partial** | Ship resources (hull, fuel, scrap, missiles, drone parts), weapons, drones, augments, state variables, metadata | Hyperspace/Multiverse saves (format 11) |
| **Restricted** | Metadata and state variables only | Fallback when both full and partial parsing fail |

### Multiverse / Hyperspace Support

Hyperspace and Multiverse mods extend FTL's binary save format with custom data at multiple injection points (per-crew-member extensions, room stat boosts, temporal systems, hidden augments, seed data, etc.). Full semantic parsing of all these extensions is impractical.

**Partial mode** solves this by:
1. Parsing the ship header fields (hull, fuel, scrap, missiles, drone parts) which use the vanilla format
2. Preserving crew, systems, and room data as opaque bytes (contains Hyperspace extensions)
3. Using a heuristic scanner to locate and parse the weapons, drones, and augments sections
4. Preserving all remaining data (cargo, sector map, encounters, etc.) as opaque bytes

This gives you safe editability of ship resources and loadout while guaranteeing perfect round-trip fidelity for all Hyperspace extension data.

### Other Features

- Automatic backups before every save (timestamped, collision-safe)
- Parse diagnostics logged to `%LOCALAPPDATA%\FtlSaveEditor\logs\`
- Debug logging always active via `System.Diagnostics.Debug`
- Auto-detection of save files (`continue.sav`, `hs_continue.sav`, `hs_mv_continue.sav`)
- Dark-themed WPF UI with sidebar navigation

## Automatic Backups

Backups are created automatically before writing save data.

- **Save**: Creates a backup of the target file before overwrite.
- **Save As**: Creates a backup of the source file first (when source and destination differ). If the destination already exists, also backs it up before overwrite.

Backup files are written beside the original with timestamped names:

```
continue_backup_2026-02-21_12-34-56-123.sav
```

## Known Limitations

- **Crew editing** is not available in partial mode. Hyperspace prepends 68+ bytes of custom data (health, powers, stat boosts, teleport state, etc.) before each crew member's vanilla data, making crew parsing infeasible without full Hyperspace format support.
- **Systems editing** is not available in partial mode for the same reason (Hyperspace temporal system extensions, etc.).
- **Cargo, beacons, and environment editing** are not available in partial mode (these sections follow HS extension data that cannot be reliably parsed).
- Room/door semantic editing is not implemented; raw room/door bytes are preserved for round-trip safety.
- Weapon/drone/augment detection uses a heuristic scanner. In rare cases this could find a false positive, but the round-trip test ensures no data corruption.

## Build

Requires .NET 8 SDK.

```bash
cd FtlSaveEditor/FtlSaveEditor
dotnet build
dotnet run
```

To publish a self-contained executable:

```bash
dotnet publish -c Release -r win-x64 --self-contained
```

## Save File Location

Typical location:

```
%USERPROFILE%\Documents\My Games\FasterThanLight\
```

Common files:

| File | Source |
|------|--------|
| `continue.sav` | Vanilla FTL |
| `hs_continue.sav` | FTL: Hyperspace |
| `hs_mv_continue.sav` | FTL: Multiverse |

## Architecture

```
FtlSaveEditor/
  Models/
    SaveData.cs        - All data model classes (SavedGameState, ShipState, CrewState, etc.)
    Enums.cs           - SystemType, Difficulty, FleetPresence, etc.
  SaveFile/
    SaveFileParser.cs  - Binary save reader (BinaryReader), heuristic weapon scanner
    SaveFileWriter.cs  - Binary save writer (BinaryWriter), mode-dispatched
    ShipLayouts.cs     - Hardcoded vanilla ship room square counts
  Services/
    SaveEditorState.cs - Singleton state holder, dirty flag, mode display
    FileService.cs     - Save detection, open/save, backup management
  Views/
    10 editor views    - Ship, Crew, Systems, Weapons, Drones, Augments, Cargo, StateVars, Beacons, Misc
```

## FTL Save Format

- Binary, little-endian, sequential (no checksums or offset tables)
- Booleans are 4-byte int32 (0 or 1)
- Strings are length-prefixed (int32 length + UTF-8 bytes)
- Format versions: 2, 7, 8, 9 (vanilla/AE), 11 (Hyperspace)

## References

- [Vhati's FTL Profile Editor](https://github.com/Vhati/ftl-profile-editor) - Java-based save editor, format documentation
- [FTL official site](https://subsetgames.com/ftl.html)
- [FTL Hyperspace](https://github.com/FTL-Hyperspace/FTL-Hyperspace) - Modding framework that extends the save format
- [FTL Multiverse](https://ftlmultiverse.fandom.com/) - Major total conversion mod built on Hyperspace
- [FTL Multiverse on Subsetgames Forum](https://subsetgames.com/forum/viewtopic.php?t=35332)
