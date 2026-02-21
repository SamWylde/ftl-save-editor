# FTL Save Editor

Desktop save game editor for FTL: Faster Than Light. C# WPF (.NET 8) application targeting Windows.

## Status

**Work in progress — the parser does not fully work yet.**

### What works
- App builds and launches (dark-themed WPF window)
- Binary parser reads: file header, ship header, crew members, system states, extended system info (clonebay, battery, shields, cloaking)
- All crew names, races, skills, and stats parse correctly
- All 16 system types parse correctly (capacity, power, damage, etc.)
- Weapon, drone, and augment ID strings are located in the binary data

### What doesn't work
- **Room parsing** — FTL save files do not store room counts or square counts. The Java reference parser (Vhati's) gets these from external ship layout `.txt` files bundled with the game data. Our parser currently skips room/breach/door data by scanning forward to find the weapon section. This means room, breach, and door data is not loaded.
- **Door parsing** — Same issue as rooms; door count comes from layout data, not the save file.
- **Multiverse/modded ship support** — The `hs_mv_continue.sav` file fails earlier (during crew parsing), likely due to Hyperspace-specific extensions to the crew data format.
- **Save file writing** — Writer exists but hasn't been tested since the parser can't fully load files yet.
- **Editing UI** — Views exist for ship overview, crew, systems, weapons, drones, augments, and sector map, but they depend on fully parsed data.

### Key technical findings
- FTL save format is little-endian binary with int32 values, length-prefixed UTF-8 strings, and bools stored as int32 (0/1).
- Room square counts per room are NOT in the save file — they come from the ship's layout data file (e.g., `kestrel.txt`). Without this external data, room parsing is ambiguous.
- The "no station" value for `stationSquare` is `-1` (not `-2` as some docs suggest). `stationDirection` uses 0-4 (DOWN, RIGHT, UP, LEFT, NONE).
- `extinguishmentProgress` defaults to `-1` when no fire is present (not `0`).
- The HackingInfo section includes a full DronePodState (~30 fields + AnimState + hacking extension).
- Hyperspace/Multiverse mods extend the save format with additional fields in crew, systems, and other sections.

## Sources / References

- **Vhati's FTL Profile Editor** (Java, primary reference): https://github.com/Vhati/ftl-profile-editor
  - `SavedGameParser.java` — the authoritative reference for the binary save format
  - Uses external ship layout data from game files for room/door counts
- **FTL: Faster Than Light** by Subset Games: https://subsetgames.com/ftl.html
- **FTL Hyperspace** mod: https://github.com/FTL-Hyperspace/FTL-Hyperspace
- **FTL Multiverse** mod: https://ftlmultiverse.fandom.com/

## Project Structure

```
FtlSaveEditor/
  FtlSaveEditor/
    App.xaml, App.xaml.cs          — WPF application entry
    MainWindow.xaml, .cs           — Main editor window (dark theme)
    Models/
      SaveData.cs                  — All data model classes
      Enums.cs                     — System types, helpers
    SaveFile/
      SaveFileParser.cs            — Binary save file parser
      SaveFileWriter.cs            — Binary save file writer
      ShipLayouts.cs               — Hardcoded room layouts (incomplete)
    Services/
      FileService.cs               — File open/save dialogs
      SaveEditorState.cs           — Singleton editor state
    Views/
      ShipOverviewView.xaml        — Ship summary (hull, resources)
      CrewView.xaml                — Crew list and editing
      SystemsView.xaml             — Systems management
      WeaponsView.xaml             — Weapons and drones
      SectorMapView.xaml           — Sector/beacon info
```

## Building

Requires .NET 8 SDK.

```bash
cd FtlSaveEditor/FtlSaveEditor
dotnet build
dotnet run
```

## Save file location

Save files are typically at:
```
%USERPROFILE%\Documents\My Games\FasterThanLight\
```
- `continue.sav` — vanilla/Hyperspace save
- `hs_mv_continue.sav` — Multiverse save
