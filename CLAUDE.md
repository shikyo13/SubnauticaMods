# SubnauticaMods

Monorepo of BepInEx 5 mods for Subnautica. Each subdirectory is a standalone mod with its own `.sln`/`.csproj`.

## Key Directories

| Path | Purpose |
|-|-|
| `<ModName>/` | Mod source (plugin, patches, properties) |
| `<ModName>/Properties/AssemblyInfo.cs` | Manual version sync (GenerateAssemblyInfo=false) |
| `D:\SteamLibrary\steamapps\common\Subnautica` | Game install |
| `Subnautica_Data\Managed\Assembly-CSharp.dll` | Game logic (decompilable .NET) |
| `BepInEx\core\` | BepInEx.dll, 0Harmony.dll |
| `BepInEx\plugins\` | Deployed mod DLLs |
| `BepInEx\LogOutput.log` | First place to check for errors |

## Build & Deploy

```bash
dotnet build <ModName>/<ModName>.csproj -c Release   # CopyToPlugins target auto-deploys
```

## Coding Conventions

- **Target**: .NET Framework 4.7.2, Unity 2019.4.36, Mono runtime
- **GUID**: `com.adam.<modname>` (reverse domain)
- **Plugin class**: `<ModName>Plugin : BaseUnityPlugin`
- **Patches**: `internal static` classes with `[HarmonyPatch]` attributes
- **Version sync**: update BOTH plugin `const string` AND `Properties/AssemblyInfo.cs`
- **Harmony**: `Harmony.CreateAndPatchAll(Assembly, MyGuid)` in `Awake()`
- **Postfix-first**: prefer Postfix over Prefix; never `return false` in Prefix unless required
- **Private access**: use AssemblyPublicizer or `AccessTools.Field()`; never reference game `mscorlib.dll`/`System.*`
- **Nautilus dependency**: `[BepInDependency("com.snmodding.nautilus")]` when using Nautilus
- **Extension methods**: `using Nautilus.Assets.Gadgets;` required for `SetRecipe()`, `SetEquipment()`, etc.
- **Input**: use `GameInput.GetButtonDown()`, never `Input.GetKeyDown()` (legacy input is dead)
- **Audio**: use FMOD via `CustomSoundHandler`, never Unity `AudioSource`

## Mod Inventory

| Mod | Version | Summary |
|-|-|-|
| PowerSaver | 1.0.0 | Reduces power drain (prefix patches on `ConsumeEnergy`) |
| CameraStalkerGuard | 1.0.0 | Prevents stalkers targeting cameras (`CollectShiny.IsTargetValid`) |
| BeaconColorPicker | 1.0.1 | Custom beacon colors with RGB/hex display |

## Documentation

| When | Read |
|-|-|
| Every session | `docs/tier1-quickref.md` (~80 lines) |
| Writing Harmony patches | `docs/tier2-harmony-patching.md` (~170 lines) |
| Using Nautilus API | `docs/tier2-nautilus-api.md` (~190 lines) |
| Setting up a new mod | `docs/tier2-project-setup.md` (~175 lines) |
| Debugging issues | `docs/tier2-debugging.md` (~95 lines) |
| Specific API lookup | `docs/tier3-full-reference.md` — read section index, then `offset`/`limit` for relevant section only |

## Reverse Engineering

- Use `mcp__re-orchestrator__search_dotnet_assembly` and `enumerate_dotnet_methods` for game types
- Use `mcp__re-orchestrator__disassemble_dotnet_method` for IL inspection
- Much better than Ghidra for .NET assemblies
