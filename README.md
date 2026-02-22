# FTL Save Editor

Desktop save game editor and **live memory trainer** for **FTL: Faster Than Light**, including support for **Hyperspace** and **Multiverse** mod saves.

C# WPF application targeting Windows (`.NET 8`). Two modes: **Save Editor** for editing save files on disk, and **Trainer** for reading/writing game memory in real-time while FTL is running.

## Features

### Three Parse Modes

| Mode | Editable Fields | When Used |
|------|----------------|-----------|
| **Full** | Everything (ship, crew, systems, weapons, drones, augments, cargo, beacons, environment) | Vanilla saves (formats 2, 7, 8, 9) |
| **Partial** | Ship, crew, systems, weapons, drones, augments, state variables, metadata | Hyperspace/Multiverse saves (format 11) |
| **Restricted** | Metadata and state variables only | Fallback when both full and partial parsing fail |

### Multiverse / Hyperspace Support

Hyperspace and Multiverse mods extend FTL's binary save format with custom data at multiple injection points. **Partial mode** handles this by:

1. Parsing the ship header (hull, fuel, scrap, missiles, drone parts)
2. Parsing crew members — vanilla fields are editable, HS inline extensions are preserved as opaque blobs
3. Parsing ship systems (capacity, power, damage, ionization, hack state) — HS custom systems are preserved separately
4. Using a heuristic scanner to locate and parse weapons, drones, and augments
5. Parsing cargo items from the opaque tail after augments
6. Preserving rooms, sector map, and encounters as opaque bytes

This gives you safe editability of ship resources, crew, systems, and loadout while guaranteeing perfect byte-for-byte round-trip fidelity for all Hyperspace extension data.

### Editable in Partial Mode

| Section | Editable Fields |
|---------|----------------|
| **Metadata** | Difficulty, ship name, blueprint, sector number, stats |
| **State Variables** | All quest/progression key-value pairs |
| **Ship** | Hull, fuel, scrap, missiles, drone parts |
| **Crew** | Name, race, health, all 6 skills, masteries, position, stats (add/remove supported) |
| **Systems** | Capacity, power, damaged bars, ionized bars, hack level, hacked state, clonebay/battery/shields/cloaking sub-state |
| **Weapons** | Weapon IDs, armed state (add/remove supported) |
| **Drones** | Drone IDs, armed state, position (add/remove supported) |
| **Augments** | Augment IDs (add/remove supported) |
| **Cargo** | Cargo item IDs (add/remove supported) |

### Multiverse Mod Blueprint Integration

The editor auto-detects your FTL installation and scans Multiverse/Hyperspace mod ZIP files for weapon, drone, and augment blueprints. This populates the item dropdowns with **all mod items** (800+ weapons, 180+ drones, 550+ augments) instead of just vanilla IDs.

Each item shows its **title, type, stats, and description** inline — so you know exactly what you're equipping or replacing.

Supported mod file locations:
- GOG: `D:\GOG\Games\FTL Advanced Edition\mods\`
- Steam: `...\steamapps\common\FTL Faster Than Light\mods\`
- Custom: Create `%LOCALAPPDATA%\FtlSaveEditor\settings.txt` with your FTL install path

### Live Memory Trainer

Switch to **Trainer mode** via the toolbar toggle to edit FTL's memory in real-time while the game is running.

| Feature | Description |
|---------|-------------|
| **Attach/Detach** | Connect to a running FTL process with one click |
| **Live Resources** | Read and write Hull, Scrap, Fuel, Missiles, Drone Parts in real-time |
| **Value Freezing** | Lock any resource value — the trainer continuously writes it back |
| **Auto-Detection** | Attempts to resolve memory addresses for known FTL versions |
| **Manual Override** | Enter hex addresses from Cheat Engine when auto-detection fails |
| **Quick Actions** | Max All Resources, Unfreeze All |

The trainer uses P/Invoke (`ReadProcessMemory`/`WriteProcessMemory`) with no external dependencies. FTL has no anti-cheat.

**Note:** Memory addresses vary by FTL version. The auto-detection may not work for all versions — use the manual address override with Cheat Engine as a fallback. May require running as Administrator.

### Game Data Browser

Available in Trainer mode. Browse FTL's game data files and mod blueprints.

- **Vanilla tab** — Weapons, drones, augments, crew races extracted from FTL's `data.dat` archive
- **Mod Blueprints tab** — All items from Multiverse/Hyperspace mod ZIP files
- **Raw Files tab** — Browse and view any file inside `data.dat` (XML blueprints, events, sector data)
- Search/filter across all tabs, click any row to copy the item ID

### Other Features

- **Item Browser** tab for searching, sorting, and filtering all mod items (weapons, drones, augments) with stats — click any row to copy the item ID to clipboard
- **Live search filter** on all item dropdowns (weapons, drones, augments, cargo, beacon store items) — type to filter the list in real time
- **Blueprint info** shown inline for all item fields — weapon stats, drone type/power, augment cost/stackability
- **Crew race dropdown** with all Multiverse races (300+) instead of plain text entry
- **Crew mastery checkboxes** (M1/M2) for all 6 skills directly in the skill grid
- **Sector state editing** — waiting flag, hazards visibility, sector visitation map
- **Quest event editing** — quest event map and distant quest events with add/remove
- **Format 2+ fields** — secondary flagship/hazard/beacon fields for format 7+ saves
- In-app Help / Info panel with parse mode details and save format documentation
- Automatic backups before every save (timestamped, collision-safe)
- Parse diagnostics logged to `%LOCALAPPDATA%\FtlSaveEditor\logs\`
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

- **Beacons and environment editing** are not available in partial mode (these are in the opaque tail after cargo).
- Room/door semantic editing is not implemented in any mode; raw bytes are preserved for round-trip safety.
- Weapon/drone/augment detection uses a heuristic scanner. In rare cases this could find a false positive, but the round-trip test ensures no data corruption.

## Hyperspace Crew Extension Format

Empirically reverse-engineered through binary analysis of Multiverse saves:

Hyperspace injects extension data **inline** within each crew member, between `universalDeathCount` and the mastery bools:

```
[vanilla fields: name, race, health, skills, ... universalDeathCount]
[HS extension: sentinels(-1000,-1000) | powers | resources | origColorRace | origRace | customTele | boosts | 6 extras]
[vanilla fields: 12 mastery bools | unknownNu | teleportAnim | unknownPhi]
[HS trailing: 2 ints (always 0, replaces crystal lockdown fields for all races)]
```

Key findings:
- **Crystal lockdown** replaced by 2 trailing ints (always 0) written for every crew member regardless of race
- **Power data is variable-length** — some powers contain embedded animation strings (92-155 bytes)
- The **doubled race string** (origColorRace + origRace matching the crew's own race) is the reliable anchor for locating extension boundaries
- Post-string gap is always 44 bytes (11 ints) across all crew

The parser preserves all HS extension bytes as opaque blobs (`HsInlinePreStringBytes`, `HsInlinePostStringBytes`) while exposing vanilla crew fields for editing.

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

## Tests

```bash
dotnet test ftl-save-editor.sln
```

Tests include round-trip fidelity checks for both vanilla and HS/MV saves, crew edit persistence verification, and heuristic scanner regression tests.

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
    SaveFileParser.cs  - Binary save reader, heuristic weapon scanner, HS crew/system parser
    SaveFileWriter.cs  - Binary save writer, mode-dispatched (full/partial/restricted)
    ShipLayouts.cs     - Hardcoded vanilla ship room square counts
  Data/
    ItemIds.cs         - Vanilla weapon/drone/augment ID lists (fallback)
    BlueprintData.cs   - Data models for parsed mod blueprints
  Services/
    SaveEditorState.cs - Singleton state holder, dirty flag, mode display
    FileService.cs     - Save detection, open/save, backup management
    ModBlueprintScanner.cs - Auto-detects FTL install, parses mod ZIP blueprints
    ProcessMemoryService.cs - P/Invoke wrapper for ReadProcessMemory/WriteProcessMemory
    TrainerService.cs  - Live memory trainer (attach, read, write, freeze, version detection)
    GameDataService.cs - Reads FTL's data.dat archive, extracts vanilla blueprints
  Views/
    12 editor views    - Ship, Crew, Systems, Weapons, Drones, Augments, Cargo, StateVars, Beacons, Misc, Help, ItemBrowser
    TrainerConnectView - Trainer connection page (attach/detach, status, settings)
    TrainerView        - Live resource editing (read/write/freeze game values)
    GameDataBrowser    - Browse vanilla + mod blueprints and raw data.dat files
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
