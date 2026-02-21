# FTL Save Editor

Desktop save game editor for FTL: Faster Than Light.
C# WPF application targeting Windows (`.NET 8`).

## Current Status

This project now supports two safe parse/write modes:

- `Full` mode (vanilla-compatible saves):
  - Parses and edits normal sections (ship, crew, systems, weapons, cargo, etc.)
  - Preserves room/door raw bytes for round-trip safety
  - Fixes scanner false-positives when locating weapon sections
- `RestrictedOpaqueTail` mode (currently used for unsupported Hyperspace/Multiverse layouts):
  - Parses header + state vars
  - Preserves the remaining file tail as opaque bytes
  - Exposes only safe editors (`Metadata`, `State Variables`)

## Automatic Backups

Backups are automatic before writing save data.

- `Save`:
  - Always creates a backup of the current target file before overwrite.
- `Save As`:
  - Creates a backup of the currently loaded source file first (when source and destination differ).
  - If destination already exists, also creates a backup of destination before overwrite.

Backup files are written beside the original with timestamped names, for example:

- `continue_backup_2026-02-21_12-34-56-123.sav`

The backup naming includes milliseconds and collision-safe suffixing.

## Diagnostics

When full parsing fails, diagnostics are written to:

- `%LOCALAPPDATA%\FtlSaveEditor\logs\`

The UI shows warning context and log path.

## Known Limitations

- Full semantic parsing for some Hyperspace/Multiverse sections is still incomplete.
- Room/door semantic editing is not implemented yet; raw room/door bytes are preserved for safety.

## Build

Requires .NET 8 SDK.

```bash
cd FtlSaveEditor/FtlSaveEditor
dotnet build
dotnet run
```

## Tests

```bash
dotnet test ftl-save-editor.sln
```

## Save File Location

Typical location:

```text
%USERPROFILE%\Documents\My Games\FasterThanLight\
```

Common files:

- `continue.sav`
- `hs_continue.sav`
- `hs_mv_continue.sav`

## References

- Vhati's FTL Profile Editor: https://github.com/Vhati/ftl-profile-editor
- FTL official site: https://subsetgames.com/ftl.html
- FTL Hyperspace: https://github.com/FTL-Hyperspace/FTL-Hyperspace
- FTL Multiverse: https://ftlmultiverse.fandom.com/
