# Tier 2: Debugging and Troubleshooting

Read when debugging mod issues, fixing broken mods, or investigating game updates.

## Essential Debug Steps

1. **Check `BepInEx/LogOutput.log`** — first place to look for errors
2. **Enable Harmony debug:** `Harmony.DEBUG = true;` creates `harmony.log.txt` on Desktop
3. **Use `[HarmonyDebug]`** on individual patch classes for IL dumps
4. **Use Runtime Editor** to inspect GameObjects at runtime
5. **Inspect other mods' patches:**
   ```csharp
   var patches = Harmony.GetPatchInfo(AccessTools.Method(typeof(X), "Y"));
   foreach (var p in patches.Prefixes) Logger.LogInfo($"Prefix: {p.owner}");
   ```

## Inspecting Game Assemblies

Write a console app to enumerate types/fields/methods via reflection:

```csharp
var asm = Assembly.LoadFrom("path/to/Assembly-CSharp.dll");
var type = asm.GetType("PlayerController");
foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic
    | BindingFlags.Instance | BindingFlags.Static))
{
    Console.WriteLine($"{field.FieldType.Name} {field.Name} [{field.Attributes}]");
}
```

Or use RE orchestrator MCP tools:
- `mcp__re-orchestrator__search_dotnet_assembly` — search for types/methods
- `mcp__re-orchestrator__enumerate_dotnet_methods` — list methods on a type
- `mcp__re-orchestrator__disassemble_dotnet_method` — IL inspection

## Common Errors and Fixes

| Error | Cause | Fix |
|-|-|-|
| No `LogOutput.log` generated | BepInEx not loading | Verify `winhttp.dll` and `doorstop_config.ini` in game root |
| Plugin not detected | Missing attribute or wrong base class | Add `[BepInPlugin]` and inherit `BaseUnityPlugin` |
| `CS0117: 'X' does not contain 'Y'` | Field/method renamed in update | Inspect actual API via reflection |
| `Undefined target method for patch` | Method renamed/removed | Decompile new Assembly-CSharp; update target |
| `CS0507: cannot change access modifiers` | Method visibility changed | Match the current access level |
| `CS0122: inaccessible due to protection level` | Field became private | Use `AccessTools.Field()` or publicizer |
| `CS0305: requires type arguments` | API became generic (e.g., `ChoiceChangedEventArgs<T>`) | Add type parameter |
| Missing extension methods | Missing `using Nautilus.Assets.Gadgets;` | Add the using directive |
| `AssetReferenceGameObject` not found | Missing Unity.Addressables ref | Add `Unity.Addressables.dll` reference |
| NuGet restore fails | `nuget.bepinex.dev` is down | Switch to local DLL references |
| Transpiler silent failure | CodeMatcher match failed | Always use `.ThrowIfInvalid()` |
| Patch runs but nothing happens | Wrong field being modified | Inspect actual class members via reflection |
| GameObject immediately destroyed | Created during plugin init | Create in scene-loaded callbacks |
| Config file not generated | `Config.Bind` never called | Ensure at least one `Bind` call executes |

## Systematic Repair Process (After Game Updates)

Every major Subnautica update rewrites `Assembly-CSharp.dll`, breaking Harmony patches targeting renamed/removed/re-signatured methods.

**Step 1: Read the log.** `BepInEx/LogOutput.log` tells you which plugins loaded, failed, and why. `ArgumentException: Undefined target method for patch method` = Harmony target renamed/removed.

**Step 2: Decompile new Assembly-CSharp.dll.** Use dnSpy or ILSpy. Compare against old version to identify renames, signature changes, visibility changes.

**Step 3: Reflection inspector.** Quick API discovery without full decompilation — enumerate fields/methods on suspected types.

**Step 4: Fix each breakage** using patterns from the common errors table above.

**Step 5: Update all references.** Rebuild against new game assemblies and latest Nautilus/BepInEx.

## Common Breakage Patterns

- **Visibility changes** (`public` → `private`): `GhostCrafter.powerRelay`, `Fabricator.opened`, `Crafter.state`, `BatterySource.Start()` — use publicizer or `AccessTools`
- **Field renames / wrong target**: `seaglideForwardMaxSpeed` on `PlayerController` is config default; actual speed = `underWaterController.forwardMaxSpeed` — decompile to find correct class
- **API removal**: `KnownTech.GetAllKnownTechTypes()` removed → use `Contains()`
- **Signature changes**: `KnownTech.Add` now takes `(TechType, bool verbose)` — update call sites
- **Atlas.Sprite removal** (2025): switch to `UnityEngine.Sprite`
- **Legacy input death** (2025): `Input.GetKeyDown()` → `GameInput.GetButtonDown()`
- **Init timing** (2025): GameObjects in `Awake()` destroyed → use lifecycle hooks or `UWE.CoroutineHost`

## Case Study Lessons (LiteralSeaglideUpgrades)

Key fixes that illustrate common patterns:

1. **StorageContainer → Equipment**: Grid inventory is wrong for upgrade slots; use `Equipment` with custom `EquipmentType` and named slots
2. **Raw `Input.GetKeyDown()` → `GameInput.GetButtonDown()`**: Legacy input dead; proper system respects game state and is rebindable
3. **`PlayerController` speed → `underWaterController` motor**: Config defaults vs. active motor — patch the motor that drives movement
4. **Private field access across versions**: Multiple game classes changed visibility — use publicizer as standard practice
5. **Nautilus API generics**: `ChoiceChangedEventArgs<T>`, `[OnChange]` on field not method, extension methods need `using Nautilus.Assets.Gadgets;`

## Recommended Tools

- **dnSpy** / **ILSpy** — decompile Assembly-CSharp.dll
- **Runtime Editor** — explore GameObjects and components in-game
- **BepInEx Configuration Manager** — press F1 for in-game config GUI
- **BepInEx.AssemblyPublicizer** — compile-time access to private members
