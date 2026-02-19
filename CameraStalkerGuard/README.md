# CameraStalkerGuard - Subnautica Scanner Room Camera Protection

A BepInEx 5 mod that prevents stalkers from stealing scanner room cameras.

## What It Does

Patches the creature AI's shiny object targeting system (`CollectShiny.IsTargetValid`) to exclude `MapRoomCamera` objects. Stalkers will no longer pursue, grab, or drag away your scanner room cameras.

- Cameras are completely invisible to stalker pickup AI
- Player can still pick up and recall cameras normally
- Stalkers still interact with metal salvage and other shiny objects
- No configuration needed — install and forget

## Building

### Prerequisites
- .NET SDK 6.0+ or Visual Studio 2022
- Subnautica with BepInEx 5 installed

### Steps

1. Verify `SubnauticaDir` in `CameraStalkerGuard.csproj` points to your Subnautica install.

2. Build:
   ```
   dotnet build --configuration Release
   ```

3. The build auto-copies `CameraStalkerGuard.dll` to `BepInEx\plugins\CameraStalkerGuard\`.

4. Launch Subnautica and check `BepInEx\LogOutput.log` for:
   ```
   [Info : CameraStalkerGuard] CameraStalkerGuard v1.0.0 loaded! Scanner room cameras are now protected from stalkers.
   ```

## Compatibility

- Subnautica (Steam, current build as of Feb 2026)
- BepInEx 5.4.x
- Should not conflict with other mods unless they also patch `CollectShiny.IsTargetValid`
