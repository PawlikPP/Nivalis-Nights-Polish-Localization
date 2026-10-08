# Nivalis Nights Polish Localization

Polish localization and installer for Nivalis Nights. The installer modifies the game's Unity localization TextAssets in place and can restore the original asset files from its backups.

## What is in this repository

- `src/Installer.cs` - Windows Forms installer source, with the translation package embedded at build time.
- `build.ps1` and `build_icon.ps1` - scripts to assemble the package and compile the installer on Windows.
- `Nivalis_Nights_PL_payload.zip` - PowerShell patcher, localized text catalogs, manifest and the AssetsTools.NET runtime files needed to rebuild the installer.
- `assets/ikona_spolszczenia.png` - installer icon artwork.
- `THIRD_PARTY_NOTICES.md` - dependency attribution.

No game executable or Unity game archives are included.

## Build

On Windows, install the .NET Framework 4.x developer tools so `csc.exe` is available, then run Windows PowerShell 5.1 from this folder:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
```

The result is written to `build/Spolszczenie_Nivalis_Nights.exe`. The script embeds `Nivalis_Nights_PL_payload.zip`, converts the PNG artwork to a multi-size Windows icon, and compiles `src/Installer.cs` with the .NET Framework C# compiler. It does not download software or game files.

The produced installer offers install and uninstall actions. It checks game asset hashes before patching or restoring files and keeps original asset backups in the game's `.nivalis_pl` folder. Close the game before running either action.

## Compatibility

The current catalog baseline targets the Nivalis Nights update inspected on October 2-3, 2026. The patcher validates the game's original localization assets and stops when they do not match the supported baseline. It stores Polish text in the game's French language slot.

## Source and changes

The installer source is in `src/Installer.cs`; the asset transformation logic and reviewed translation catalogs are in `Nivalis_Nights_PL_payload.zip`.

The generated installer embeds the package to keep the distributed ZIP to one visible executable. The payload archive is included here so the build can be reproduced without game files.
