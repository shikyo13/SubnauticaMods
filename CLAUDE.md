# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Overview

Monorepo for Subnautica BepInEx 5 mods. Currently contains one mod (**PowerSaver**); new mods should follow the same structure as sibling directories under the root.

## Build Commands

Requires .NET SDK 6.0+ (or Visual Studio 2022). Subnautica with BepInEx 5 must be installed.

```bash
# Build a specific mod (from repo root)
dotnet build PowerSaver/PowerSaver.sln --configuration Release

# Or from within the mod directory
cd PowerSaver && dotnet build --configuration Release
```

Build output goes to `bin/Release/net472/`. A post-build target auto-copies the DLL to the BepInEx plugins folder.

There are no tests, linting, or CI pipelines configured.

## Architecture

Each mod is a standalone BepInEx 5 plugin targeting **.NET Framework 4.7.2** with its own `.sln` and `.csproj`.

### Mod Structure Pattern

```
ModName/
├── ModName.sln
├── ModName.csproj      # SDK-style, references BepInEx + Harmony + game DLLs
├── ModNamePlugin.cs    # Entry point: BaseUnityPlugin subclass with [BepInPlugin]
├── Properties/
│   └── AssemblyInfo.cs
└── README.md
```

### Key Frameworks

- **BepInEx 5.4.x** — Plugin loader. Entry point is a `BaseUnityPlugin` with `[BepInPlugin(GUID, Name, Version)]`. Initialization in `Awake()`, cleanup in `OnDestroy()`.
- **HarmonyLib (0Harmony)** — Runtime method patching. Patches are declared as inner classes with `[HarmonyPatch]` attributes. `_harmony.PatchAll()` auto-discovers them.
- **BepInEx.Configuration** — `ConfigEntry<T>` bound via `Config.Bind()` in `Awake()`. Config files appear in `BepInEx/config/{GUID}.cfg` at first run.

### Patching Conventions

- Use **Prefix** patches with `ref` parameters to modify values before the original method runs.
- When a game method has overloads, use `[HarmonyTargetMethod]` with `AccessTools.Method()` specifying explicit parameter types instead of `[HarmonyPatch]` attributes.
- All patches are `internal static` classes nested or in the same file as the plugin.

### Project File Setup

Each `.csproj` defines path properties that must match the local Subnautica install:

```xml
<SubnauticaDir>D:\SteamLibrary\steamapps\common\Subnautica</SubnauticaDir>
<BepInExDir>$(SubnauticaDir)\BepInEx</BepInExDir>
<ManagedDir>$(SubnauticaDir)\Subnautica_Data\Managed</ManagedDir>
```

All game/BepInEx DLL references use `<Private>false</Private>` (not copied to output).

## Debugging

- Set `EnableLogging = true` in the mod's BepInEx config to get per-call debug output.
- Check `BepInEx/LogOutput.log` for load confirmation and runtime errors.
- Use dnSpy or ILSpy on `Assembly-CSharp.dll` to verify game method signatures if patches fail to bind.

## Gotchas

- **Multiplier stacking**: Some vehicle energy calls go through `EnergyMixin.ConsumeEnergy` before `Vehicle.ConsumeEnergy`, so both the global and vehicle multipliers apply. If drain reduction seems too aggressive, set the category-specific multiplier to 1.0.
- **SMLHelper conflict**: If SMLHelper is installed alongside Nautilus, BepInEx chainloader can hang and prevent all mods from loading. This mod has no dependency on either, but the conflict blocks it.
- **Manual version syncing**: `GenerateAssemblyInfo` is false in the .csproj. Plugin version is the `PLUGIN_VERSION` const in the plugin class; assembly version is in `Properties/AssemblyInfo.cs`. Both must be updated manually when bumping versions.
- **GUID convention**: New mods use `com.adam.<modname>` as the BepInEx plugin GUID.
