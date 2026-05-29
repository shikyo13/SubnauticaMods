# SubnauticaMods

Monorepo of BepInEx 5 mods for Subnautica and Subnautica: Below Zero. Each subdirectory is a standalone mod with its own `.sln`/`.csproj`. BZ ports live in `<ModName>.BZ/` directories.

## Key Directories

| Path | Purpose |
|-|-|
| `<ModName>/` | SN1 mod source (plugin, patches, properties) |
| `<ModName>.BZ/` | BZ mod source (same structure as SN1) |
| `Shared/ColorPicker/` | Shared HSV color picker UI (compiled into each mod via `<Compile Include>` link) |
| `<ModName>/Properties/AssemblyInfo.cs` | Manual version sync (GenerateAssemblyInfo=false) |
| `D:\SteamLibrary\steamapps\common\Subnautica` | SN1 game install |
| `D:\SteamLibrary\steamapps\common\SubnauticaZero` | BZ game install |
| `Subnautica_Data\Managed\Assembly-CSharp.dll` | SN1 game logic |
| `SubnauticaZero_Data\Managed\Assembly-CSharp.dll` | BZ game logic |
| `BepInEx\core\` | BepInEx.dll, 0Harmony.dll (both games) |
| `BepInEx\plugins\` | Deployed mod DLLs (both games) |
| `BepInEx\LogOutput.log` | First place to check for errors |

## Build & Deploy

```bash
dotnet build <ModName>/<ModName>.csproj -c Release   # CopyToPlugins target auto-deploys
```

## Coding Conventions

- **Target**: .NET Framework 4.7.2, Unity 2019.4.36, Mono runtime
- **GUID**: `com.zerotheabsolute.<modname>` (SN1), `com.zerotheabsolute.<modname>.bz` (BZ)
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

| Mod | Version | Game | Summary |
|-|-|-|-|
| PowerSaver | 1.0.1 | SN1 | Reduces power drain with true global stacking and Cyclops sonar vehicle classification |
| PowerSaver.BZ | 1.0.3 | BZ | BZ port of PowerSaver with true global stacking, scoped EasyCraft compatibility, charger accounting compatibility, and Cyclops sonar vehicle classification |
| CameraStalkerGuard | 1.0.0 | SN1 | Prevents stalkers targeting cameras (`CollectShiny.IsTargetValid`) |
| CameraStalkerGuard.BZ | 1.0.0 | BZ | BZ port of CameraStalkerGuard |
| BeaconColorPicker | 1.0.1 | SN1 | Custom beacon colors with RGB/hex display |
| BeaconColorPicker.BZ | 1.0.1 | BZ | BZ port of BeaconColorPicker |
| AltMeter | 1.0.0 | SN1 | Shows altitude on depth compass when above water |
| AltMeter.BZ | 1.0.0 | BZ | BZ port with configurable text colors (Nautilus) |
| MapRoomCameraLights | 1.0.0 | SN1 | Configurable scanner room camera lights (Nautilus) |
| MapRoomCameraLights.BZ | 1.0.0 | BZ | BZ port of MapRoomCameraLights (Nautilus) |
| BetterFlashLight | 1.0.0 | SN1 | Custom flashlight color/brightness/range (Nautilus) |
| BetterFlashLight.BZ | 1.0.0 | BZ | BZ port, patches `ToggleLights.Update` (Nautilus) |
| DockLightsToggle | 1.0.0 | BZ | Turns off vehicle lights when docked (Nautilus) |
| JukeboxMod | 1.0.0 | BZ | Custom jukebox colors + party mode lighting |
| BetterSeaglide | 1.0.0 | SN1 | Speed boost + custom light/body/energy bar colors (Nautilus) |
| BetterSeaglide.BZ | 1.0.0 | BZ | BZ port of BetterSeaglide (Nautilus) |
| WaterFoodHotkey | 1.0.0 | SN1 | Auto-eat/drink hotkeys via GameInput (Nautilus) |
| WaterFoodHotkey.BZ | 1.0.0 | BZ | BZ port with food/drink/health/heat hotkeys (Nautilus) |

## Documentation

| When | Read |
|-|-|
| Every session | `docs/tier1-quickref.md` (~80 lines) |
| Writing Harmony patches | `docs/tier2-harmony-patching.md` (~170 lines) |
| Using Nautilus API | `docs/tier2-nautilus-api.md` (~190 lines) |
| Setting up a new mod | `docs/tier2-project-setup.md` (~175 lines) |
| Debugging issues | `docs/tier2-debugging.md` (~95 lines) |
| Specific API lookup | `docs/tier3-full-reference.md` - read section index, then `offset`/`limit` for relevant section only |

## Reverse Engineering

- Use `mcp__re-orchestrator__search_dotnet_assembly` and `enumerate_dotnet_methods` for game types
- Use `mcp__re-orchestrator__disassemble_dotnet_method` for IL inspection
- Much better than Ghidra for .NET assemblies
