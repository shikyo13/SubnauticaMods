# Tier 1: Quick Reference

Read this every session. One-liner gotchas and architecture essentials.

## Architecture Essentials

- Game runs on **Unity 2019.4.36 / Mono** (not IL2CPP) — full .NET bytecode in `Assembly-CSharp.dll`
- **TechType** = master enum for all items/creatures; **CraftData** = central static class for recipes/sizes/equipment
- **PrefabDatabase** uses async Addressables (Living Large update) — never `Resources.Load()`
- **Seaglide inherits `PlayerTool`**, not `Vehicle`. True vehicles: `SeaMoth`, `Exosuit` from `Vehicle`; `SubRoot` = Cyclops
- Speed: `PlayerController.seaglideForwardMaxSpeed` is a config default; actual runtime speed = `underWaterController.forwardMaxSpeed`
- **Equipment** (named string slots) = for upgrade modules; **StorageContainer** (grid) = for cargo. Never mix these up
- Player slots: Head, Body, Gloves, Foots, Tank, Chip1, Chip2
- Seamoth: 4 module slots; Prawn: 4 modules + 2 arms; Cyclops: 6 modules

## Dependency / Load Order

| Component | Depends On |
|-|-|
| Your mod plugin | BepInEx 5.4.23, 0Harmony (HarmonyX) |
| Nautilus features | `[BepInDependency("com.snmodding.nautilus")]` |
| Harmony patches | `Harmony.CreateAndPatchAll(Assembly, MyGuid)` in `Awake()` |
| Custom prefabs | `prefab.Register()` must be called LAST after all gadgets |
| Asset bundles | Must be built with Unity 2019.4.36 |

## Critical Gotchas

- `Input.GetKeyDown()` is **dead** since Aug 2025 patch — use `GameInput.GetButtonDown()` via `EnumHandler.AddEntry<GameInput.Button>()`
- `Atlas.Sprite` **removed** in Aug 2025 — use `UnityEngine.Sprite` everywhere
- `Resources.Load()` **dead** — use `CraftData.GetPrefabForTechTypeAsync()` or `PrefabDatabase.GetPrefabAsync()`
- `KnownTech.GetAllKnownTechTypes()` **removed** — use `KnownTech.Contains(TechType)` instead
- GameObjects created during plugin `Awake()` are **immediately destroyed** (2025 timing change) — create in scene-loaded callbacks
- `SetRecipe()`, `SetEquipment()`, `SetUnlock()` etc. won't compile without `using Nautilus.Assets.Gadgets;`
- `[OnChange]` attribute goes on the **field**, not the handler method — compiles but silently fails if misplaced
- `ChoiceChangedEventArgs` is **generic** now — use `ChoiceChangedEventArgs<int>` / `<string>` / `<MyEnum>`
- Craft tree paths use **separate string args**: `.WithStepsToFabricatorTab("Personal", "Tools")` not `"Personal/Tools"`
- `Prefix` returning `false` **breaks other mods' patches** — prefer Postfix always
- Never bundle your own `0Harmony.dll` — BepInEx ships HarmonyX
- Never reference `mscorlib.dll` / `netstandard.dll` / `System.*` from game's `Managed/` folder
- `GhostCrafter.powerRelay`, `Fabricator.opened`, `Crafter.state`, `BatterySource.Start()` all changed visibility — use publicizer or `AccessTools`
- Unity lifecycle methods (`Update`, `Start`, `Awake`) are often inherited — use string form or `TargetMethod()` with `AccessTools.Method`
- Always call `.ThrowIfInvalid()` after CodeMatcher matches — silent failures corrupt IL
- Never use `Traverse` on hot paths — use `AccessTools.FieldRefAccess` (near-zero overhead)
- Never allocate (`new`, LINQ, string concat) or log in `Update`/`FixedUpdate` patches
- `ModOptions` alone does NOT persist — pair with `ConfigEntry<T>`
- SMLHelper 2.15.0.1 and Nautilus are **incompatible** — cannot run both
- `<Private>false</Private>` on all game DLL references to avoid copying to output
- NuGet `BepInEx.Core` 5.4.21 is compile-time compatible with runtime 5.4.23

## Hot-Path Rules

1. Keep patch bodies minimal — early-return for irrelevant cases
2. Cache `AccessTools.FieldRefAccess` as `static readonly`
3. Prefer Transpiler for zero per-frame overhead
4. Wrap in try/catch to prevent game crashes
5. No logging, no allocations, no LINQ

## UI / Input System

- **FPSInputModule** is SN's custom input module — uses `uGUI_InputGroup` system for event routing
- Standard `GraphicRaycaster` + `ScreenSpaceOverlay` canvases **will not receive input** — `FPSInputModule` only routes to the active input group
- To make custom UI interactive: **parent under the active game canvas** via `FPSInputModule.current.lastGroup.GetComponentInParent<Canvas>()`
- `uGUI_GraphicRaycaster` registers in static `allRaycasters` list, but registration alone doesn't fix input — the **input group** is what matters
- **Menu detection**: `Cursor.lockState == CursorLockMode.Locked` = universal "gameplay resumed" check (PDA, pause menu, Nautilus options all unlock cursor)
- **Don't** use `PDA().isOpen` to detect menu state — fails for Nautilus options opened from pause menu
- `GameInput.IsRunning` respects `RunMode` setting (toggle/hold/always-run) — for **pure hold** behavior use `GameInput.GetButtonHeld(GameInput.Button.Sprint)`
- `GameInput.GetButtonDown/GetButtonHeld/GetButtonUp` — the standard input API (down=one frame, held=continuous, up=release frame)

## Anti-Patterns From Past Sessions

- Using `StorageContainer` for upgrade module slots (should be `Equipment`)
- Patching `PlayerController` speed fields instead of `underWaterController` motor fields
- Forgetting `using Nautilus.Assets.Gadgets;` and getting "does not contain a definition" errors
- Assuming field visibility hasn't changed between game versions
- Using `nameof()` for inherited Unity lifecycle methods (won't compile — use string form)
- Creating ScreenSpaceOverlay canvas with standard `GraphicRaycaster` — input won't work (use active game canvas parenting)
- Setting `MeshRenderer.material.color` on seaglide — catches minimap screen display (use `SkinnedMeshRenderer` with `SeaGlide_geo` only)
- Using `GameInput.IsRunning` for hold-to-boost — respects toggle setting, use `GetButtonHeld(Button.Sprint)` instead

## Breaking Changes Log

**August 2025 patch:**
- `Atlas.Sprite` removed — use `UnityEngine.Sprite`
- Legacy Unity input system (`UnityEngine.Input`) killed — use `GameInput`
- CraftData internals changed
- Plugin init timing changed — GameObjects created in `Awake()` destroyed

**October 2025 security hotfix:**
- All legacy game branches permanently disabled — every mod must target latest version

**Nautilus 1.0.0-pre.48 (Jan 2026):**
- `ChoiceChangedEventArgs` became generic (`<T>`)
- Individual enum handlers consolidated into `EnumHandler.AddEntry<T>()`
- SMLHelper namespace `SMLHelper.V2.*` → `Nautilus.*`
- Handlers became `public static` classes (no more `.Main` property)
