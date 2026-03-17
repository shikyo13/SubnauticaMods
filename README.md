# Subnautica Mods
[![Discord](https://img.shields.io/badge/Discord-Zero's%20Mods-5865F2?logo=discord&logoColor=white)](https://discord.gg/NrdXnbWzGC) [![Steam Workshop](https://img.shields.io/badge/Steam-Workshop-1b2838?logo=steam)](https://steamcommunity.com/id/ahunt/myworkshopfiles/) [![Nexus Mods](https://img.shields.io/badge/Nexus-Mods-da8e35?logo=nexusmods&logoColor=white)](https://www.nexusmods.com/profile/Zer0TheAbs0lute/mods) [![Stats](https://img.shields.io/badge/Live-Stats-6c5ce7)](https://shikyo13.github.io/GameModding/stats/) [![Buy Me a Coffee](https://img.shields.io/badge/Buy%20Me%20a-Coffee-ffdd00?logo=buymeacoffee&logoColor=black)](https://buymeacoffee.com/zerotheabsolute)

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
