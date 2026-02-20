# Subnautica Mods

BepInEx 5 mods for Subnautica.

## Mods

| Mod | Description |
|-----|-------------|
| [CameraStalkerGuard](CameraStalkerGuard/) | Prevents stalkers from stealing scanner room cameras |
| [PowerSaver](PowerSaver/) | Reduces power/battery drain with configurable multipliers for tools, vehicles, and bases |

## Requirements

- Subnautica (Steam)
- [BepInEx 5.4.x](https://github.com/BepInEx/BepInEx)

## Building

Each mod has its own `.csproj` and `.sln`. See the individual mod READMEs for build instructions.

```bash
cd CameraStalkerGuard
dotnet build --configuration Release

cd ../PowerSaver
dotnet build --configuration Release
```

Built DLLs are automatically copied to `BepInEx\plugins\`.
