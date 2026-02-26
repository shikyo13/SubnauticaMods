# Tier 3: Full Subnautica Modding Reference

Use `Read` tool with `offset` and `limit` to load specific sections only. Never read the entire file.

## Section Index

| # | Topic | Lines |
|-|-|-|
| 1 | Subnautica's Architecture | 34-59 |
| 2 | Environment Setup | 61-109 |
| 3 | BepInEx Plugin Fundamentals | 111-183 |
| 4 | Project Setup and Build Configuration | 185-262 |
| 5 | Harmony Patching | 264-422 |
| 6 | Nautilus Core Concepts | 424-470 |
| 7 | Custom Items and Prefabs | 472-519 |
| 8 | EnumHandler | 521-553 |
| 9 | Crafting: Recipes, Trees, and Fabricators | 555-606 |
| 10 | Equipment and Upgrade Systems | 608-647 |
| 11 | Input Handling and Keybinds | 649-676 |
| 12 | Mod Options and Configuration | 678-743 |
| 13 | Spawning, Story Goals, and PDA | 745-785 |
| 14 | Audio (FMOD) | 787-812 |
| 15 | Localization | 814-830 |
| 16 | Performance and Hot-Path Patching | 832-858 |
| 17 | Debugging and Troubleshooting | 860-906 |
| 18 | Migrating from SMLHelper 2.0 to Nautilus | 908-957 |
| 19 | Updating and Fixing Broken Mods | 959-1000 |
| 20 | Case Study: What Broke and Why | 1002-1036 |
| 21 | Common Pitfalls Quick Reference | 1038-1070 |
| 22 | API Quick Reference | 1072-1123 |

---

## 1. Subnautica's Architecture

Subnautica runs on **Unity 2019.4.36** with the **Mono runtime** (not IL2CPP). This means the game's entire logic ships as readable .NET bytecode in `Assembly-CSharp.dll`, located at `Subnautica_Data/Managed/`. Tools like dnSpy and ILSpy can decompile it to near-source-quality C#, and Harmony can patch any method at runtime.

### Core Systems

**TechType** is the master enum identifying every item, creature, buildable, and scannable in the game. **CraftData** is the central static class storing recipes, item sizes, equipment types, and prefab associations. **PrefabDatabase** manages async loading of GameObjects via Unity's Addressable Asset System — the Living Large update (December 2022) replaced synchronous `Resources.Load()` with `CraftData.GetPrefabForTechTypeAsync()`. Every spawnable object needs a **PrefabIdentifier** for persistence, a **TechTag** for system integration, and a **LargeWorldEntity** for save/load.

### Class Hierarchy (Critical for Patching)

The **Seaglide is not a vehicle** — it inherits from `PlayerTool` (like Knife and Scanner), not from `Vehicle`. True vehicles (`SeaMoth`, `Exosuit`) derive from `Vehicle` and have `Equipment` components with upgrade module slots. The `SubRoot` class (Cyclops) is separate from both.

Player movement flows through `PlayerController` → `UnderwaterMotor` (a `PlayerMotor` subclass). **Speed fields like swim velocity live on the motor controller, not on PlayerController itself.** The `seaglideForwardMaxSpeed` field on `PlayerController` is a configuration default; the actual runtime speed is `underWaterController.forwardMaxSpeed`.

### Equipment Slots vs Storage Grids

This distinction trips up many modders:

- **Equipment** uses named string slots (`"Head"`, `"SeamothModule1"`, `"ExosuitArmLeft"`) mapped to `EquipmentType` values via `Dictionary<string, EquipmentType> slotMapping`. Each slot accepts one item matching its type.
- **StorageContainer** wraps an `ItemsContainer` with a width × height grid where items occupy rectangular areas.

**For upgrade modules, always use Equipment, not StorageContainer.** StorageContainer is for cargo and general inventory.

The player has slots for Head, Body, Gloves, Foots, Tank, Chip1, and Chip2. The Seamoth has four module slots (`SeamothModule1`–`4`), the Prawn Suit has four modules plus two arm slots, and the Cyclops has six module slots.

---

## 2. Environment Setup

### Prerequisites
- .NET SDK (verify: `dotnet --version`)
- IDE: Visual Studio Community (free), JetBrains Rider, or VS Code with C# extension
- Subnautica (Steam)

### Install BepInEx
Use the [BepInEx Subnautica Pack](https://github.com/toebeann/BepInEx.Subnautica) (v5.4.23+). Extract into your Subnautica folder so `BepInEx/` sits next to `Subnautica_Data/`.

The BepInEx pack for Subnautica v5.4.23+ uses Doorstop v4, which changed the config format from earlier versions. Run the game once via Steam (never directly via the .exe), reach the main menu, then exit.

Resulting directory structure:
```
Subnautica/
├── Subnautica.exe
├── doorstop_config.ini          # Doorstop v4 injection config
├── winhttp.dll                  # Doorstop native hook
├── Subnautica_Data/
│   └── Managed/                 # Game assemblies (Assembly-CSharp.dll)
└── BepInEx/
    ├── core/                    # BepInEx core (BepInEx.dll, 0Harmony.dll)
    ├── config/                  # Plugin .cfg files
    ├── plugins/                 # Your mod DLLs go here
    └── LogOutput.log            # Always check this first
```

### Install Nautilus
Download from Nexus Mods, Submodica, or GitHub Releases. Place `Nautilus.dll` in `BepInEx/plugins/Nautilus/`.

### Project Templates (Recommended)
```bash
dotnet new install Subnautica.Templates
dotnet new snmod_nautilus -n MyModName
```

Templates available: `snmod` (basic), `snmod_empty` (minimal), `snmod_nautilus` (with Nautilus example).

### Recommended Tools
- **BepInEx.AssemblyPublicizer** NuGet package — makes private/protected game fields accessible at compile time without reflection
- **dnSpy** or **ILSpy** — decompile Assembly-CSharp.dll to browse game code
- **Runtime Editor** — explore GameObjects and components in-game
- **BepInEx Configuration Manager** — press F1 for in-game config GUI

### Verify Installation
- Check `BepInEx/LogOutput.log` for load confirmation
- Check in-game Options > Mods tab for Nautilus

---

## 3. BepInEx Plugin Fundamentals

### Plugin Class Template
```csharp
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace MyMod
{
    [BepInPlugin(MyGuid, PluginName, VersionString)]
    [BepInProcess("Subnautica.exe")]
    [BepInDependency("com.snmodding.nautilus")]
    public class Plugin : BaseUnityPlugin
    {
        private const string MyGuid = "com.yourname.mymod";
        private const string PluginName = "My Mod";
        private const string VersionString = "1.0.0";

        internal static new ManualLogSource Logger { get; private set; }
        private static readonly Assembly Assembly = Assembly.GetExecutingAssembly();

        private void Awake()
        {
            Logger = base.Logger;
            // Register Nautilus items, options, etc. here
            Harmony.CreateAndPatchAll(Assembly, MyGuid);
            Logger.LogInfo($"{PluginName} loaded.");
        }
    }
}
```

**Rules:**
- GUID must be globally unique — use reverse domain: `com.yourname.modname`
- Must inherit `BaseUnityPlugin` (which inherits `MonoBehaviour`)
- `[BepInDependency]` is **required** if using any Nautilus features (plugin skipped if Nautilus missing)
- `Harmony.CreateAndPatchAll(Assembly)` auto-discovers all `[HarmonyPatch]` classes
- Since `BaseUnityPlugin` inherits `MonoBehaviour`, plugins have access to Unity lifecycle: `Awake()`, `Start()`, `Update()`, `OnDestroy()`

### Logging
```csharp
Logger.LogDebug("Dev details");     // Hidden by default
Logger.LogInfo("Normal info");      // Normal operations
Logger.LogWarning("Potential issue"); // Non-critical
Logger.LogError("Something failed"); // Actual errors
Logger.LogFatal("Critical failure"); // Unrecoverable
```
Output goes to: BepInEx console (if enabled), `BepInEx/LogOutput.log`, Unity's `output_log.txt`.

Enable console: edit `BepInEx/config/BepInEx.cfg` → `[Logging.Console]` → `Enabled = true`.

### BepInEx Configuration
```csharp
private ConfigEntry<float> configDamage;

private void Awake()
{
    configDamage = Config.Bind("General", "DamageMultiplier", 2.0f,
        new ConfigDescription("How much to multiply knife damage",
            new AcceptableValueRange<float>(0.5f, 10.0f)));

    float value = configDamage.Value;

    // React to runtime changes:
    configDamage.SettingChanged += (s, e) =>
        Logger.LogInfo($"Damage changed to {configDamage.Value}");
}
```
Config files auto-generate at `BepInEx/config/<GUID>.cfg`. Supported types: all primitives, enums, `Color`, `Vector2/3/4`, `Quaternion`, `KeyboardShortcut`.

---

## 4. Project Setup and Build Configuration

### The .csproj File

Subnautica targets **net472**. Two approaches:

**NuGet approach** (cleaner, but depends on external feeds):
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net472</TargetFramework>
    <LangVersion>latest</LangVersion>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <SubnauticaDir>C:\Program Files (x86)\Steam\steamapps\common\Subnautica</SubnauticaDir>
    <RestoreAdditionalProjectSources>
      https://api.nuget.org/v3/index.json;
      https://nuget.bepinex.dev/v3/index.json
    </RestoreAdditionalProjectSources>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="BepInEx.Core" Version="5.4.21" />
    <PackageReference Include="BepInEx.PluginInfoProps" Version="1.1.0" />
    <PackageReference Include="UnityEngine.Modules" Version="2019.4.36" />
    <PackageReference Include="Subnautica.GameLibs" Version="*-*" />
    <PackageReference Include="Subnautica.Nautilus" Version="1.0.0-pre.48" />
    <PackageReference Include="BepInEx.AssemblyPublicizer.MSBuild" Version="0.4.2"
                      PrivateAssets="all" />
    <Publicize Include="Assembly-CSharp" />
  </ItemGroup>

  <Target Name="CopyToPlugins" AfterTargets="Build">
    <Copy SourceFiles="$(TargetPath)"
          DestinationFolder="$(SubnauticaDir)\BepInEx\plugins\$(AssemblyName)"
          SkipUnchangedFiles="true" />
  </Target>
</Project>
```

> **Note:** The BepInEx.Core NuGet package latest stable is 5.4.21, while the Subnautica BepInEx Pack installs runtime 5.4.23. This is fine — the NuGet package provides compile-time references compatible with the newer runtime.

**Local DLL approach** (reliable fallback when `nuget.bepinex.dev` is down):
```xml
<PropertyGroup>
    <SubnauticaDir>D:\SteamLibrary\steamapps\common\Subnautica</SubnauticaDir>
</PropertyGroup>
<ItemGroup>
    <Reference Include="BepInEx">
        <HintPath>$(SubnauticaDir)\BepInEx\core\BepInEx.dll</HintPath>
        <Private>false</Private>
    </Reference>
    <Reference Include="0Harmony">
        <HintPath>$(SubnauticaDir)\BepInEx\core\0Harmony.dll</HintPath>
        <Private>false</Private>
    </Reference>
    <Reference Include="Nautilus">
        <HintPath>$(SubnauticaDir)\BepInEx\plugins\Nautilus\Nautilus.dll</HintPath>
        <Private>false</Private>
    </Reference>
    <Reference Include="Assembly-CSharp">
        <HintPath>$(SubnauticaDir)\Subnautica_Data\Managed\Assembly-CSharp.dll</HintPath>
        <Private>false</Private>
    </Reference>
    <!-- Add Unity modules as needed -->
</ItemGroup>
```

**Use `<Private>false</Private>` on all game references** so they're not copied to output — they already exist at runtime.

> **Critical warning**: never reference `mscorlib.dll`, `netstandard.dll`, or `System.*` from the game's Managed folder — this causes compilation failures.

### AssemblyPublicizer

Makes private/protected members accessible at compile time without runtime reflection overhead. Generates `IgnoresAccessChecksTo` attributes — original game DLLs remain untouched. **Essential** for accessing fields like `GhostCrafter.powerRelay`, `Fabricator.opened`, and `Crafter.state` that became private in recent game versions.

When using the publicizer, you don't need `AccessTools` for field access — just reference them directly. However, always have AccessTools fallbacks for fields that may change visibility across game updates.

---

## 5. Harmony Patching

BepInEx ships with HarmonyX (a fork of Harmony built on MonoMod.RuntimeDetour), fully API-compatible with Harmony 2.

### Patch Type Decision Table

| Goal | Use |
|-|-|
| Run code after original | **Postfix** (safest, most compatible) |
| Modify return value | **Postfix** with `ref __result` |
| Modify arguments before execution | **Prefix** with `ref` params |
| Replace/skip original entirely | **Prefix** returning `false` — **avoid if possible** |
| Surgical IL modification | **Transpiler** |
| Handle exceptions | **Finalizer** |
| Hot-path (Update/FixedUpdate) | **Transpiler** (zero overhead) or minimal **Postfix** |

### Postfix (Default Choice)
```csharp
[HarmonyPatch(typeof(Knife))]
[HarmonyPatch(nameof(Knife.Awake))]
[HarmonyPostfix]
public static void Awake_Postfix(Knife __instance)
{
    __instance.damage *= 5.0f;
}
```

### Prefix (Use Sparingly)
```csharp
[HarmonyPatch(typeof(SomeClass), nameof(SomeClass.Method))]
[HarmonyPrefix]
public static bool Method_Prefix(ref float __result)
{
    __result = 42f;
    return false; // Skips original — breaks other mods' patches!
}
```

### Finalizer (Exception Safety)
```csharp
[HarmonyPatch(typeof(SomeClass), "RiskyMethod")]
class SafetyPatch
{
    static Exception Finalizer(Exception __exception, ref string __result)
    {
        if (__exception != null)
        {
            Plugin.Logger.LogError($"Caught: {__exception.Message}");
            __result = "fallback";
        }
        return null; // Suppress the exception
    }
}
```

### Special Parameter Injection

| Parameter | Meaning |
|-|-|
| `__instance` | The `this` object |
| `__result` | Return value (use `ref` to modify) |
| `__state` | Share data between Prefix→Postfix |
| `___fieldName` | Private field access (3 underscores + name) |
| `__originalMethod` | The MethodBase being patched |
| `__runOriginal` | Whether a prior prefix skipped the original |
| Original param names | Injected automatically by matching name/type |

### Targeting Methods

```csharp
// By name (for public methods)
[HarmonyPatch(typeof(Seaglide), nameof(Seaglide.UpdateEnergy))]

// By string (for private or inherited methods like Update)
[HarmonyPatch(typeof(Seaglide), "Update")]

// Overloaded methods (specify parameter types)
[HarmonyPatch(typeof(SomeClass), "Method", new Type[] { typeof(float), typeof(int) })]

// TargetMethod approach (most reliable for inherited methods)
[HarmonyPatch]
class SeaglidePatch
{
    static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(Seaglide), "Update");
    }
    static void Postfix(Seaglide __instance) { /* ... */ }
}
```

**Critical:** Unity lifecycle methods (`Update`, `Start`, `Awake`, `FixedUpdate`) are often inherited from `MonoBehaviour` and not declared on the target class. If `nameof()` fails to compile, use the string form or `TargetMethod()` with `AccessTools.Method` (which searches the entire type hierarchy).

### AccessTools Reference

```csharp
// Fields (searches base types automatically)
FieldInfo field = AccessTools.Field(typeof(GhostCrafter), "powerRelay");

// High-performance field reference (for hot paths)
static readonly AccessTools.FieldRef<GhostCrafter, PowerRelay> powerRelayRef =
    AccessTools.FieldRefAccess<GhostCrafter, PowerRelay>("powerRelay");

// Use the cached ref (near-zero overhead):
ref PowerRelay relay = ref powerRelayRef(__instance);

// Methods with overload resolution
MethodInfo method = AccessTools.Method(typeof(KnownTech), "Add",
    new Type[] { typeof(TechType), typeof(bool) });

// Property accessors
MethodInfo getter = AccessTools.PropertyGetter(typeof(Player), "main");

// Inner types
Type inner = AccessTools.Inner(typeof(OuterClass), "InnerClassName");
```

Use `DeclaredField`/`DeclaredMethod` when you specifically need members declared on the target type only (not inherited).

### Transpiler Basics with CodeMatcher
```csharp
[HarmonyPatch(typeof(Seaglide), nameof(Seaglide.UpdateEnergy))]
[HarmonyTranspiler]
public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
{
    return new CodeMatcher(instructions)
        .MatchForward(true, new CodeMatch(OpCodes.Ldc_R4, 0.1f))
        .ThrowIfInvalid("Could not find 0.1f constant!")
        .SetAndAdvance(OpCodes.Ldarg_0, null)
        .Insert(new CodeInstruction(OpCodes.Call,
            AccessTools.Method(typeof(MyClass), nameof(MyClass.GetValue),
            new[] { typeof(Seaglide) })))
        .InstructionEnumeration();
}
```

**Always call `.ThrowIfInvalid()`** after matches. Silent failures corrupt IL and produce mysterious crashes.

**Essential IL opcodes** for Subnautica transpilers: `Ldc_R4` (load float), `Ldc_I4` (load int), `Ldarg_0` (load `this`), `Ldfld`/`Stfld` (read/write fields), `Call`/`Callvirt` (method calls), `Brfalse`/`Brtrue` (conditional branches).

### Mod Compatibility Rules
1. **Prefer Postfix over Prefix** — postfixes always run regardless of other mods
2. **Never `return false` in Prefix** unless you truly need to replace the method
3. **Use unique Harmony IDs** matching your plugin GUID
4. **Use `[HarmonyPriority]` and `[HarmonyBefore/After]`** for ordering when needed
5. **Never bundle your own `0Harmony.dll`** — BepInEx ships HarmonyX

### Debugging Harmony Patches

Enable per-patch IL dumps:
```csharp
[HarmonyDebug]
[HarmonyPatch(typeof(SomeClass), "SomeMethod")]
class DebugPatch { /* ... */ }
```

Or enable global logging: `Harmony.DEBUG = true;` writes `harmony.log.txt` to the Desktop.

---

## 6. Nautilus Core Concepts

Nautilus (v1.0.0-pre.48, released Jan 12, 2026) replaces SMLHelper as the community modding API. Declare the dependency: `[BepInDependency("com.snmodding.nautilus")]`.

### Three-Part Architecture
1. **CustomPrefab** — The registration wrapper. Manages gadgets and the game object.
2. **Gadgets** — Configure game data (recipes, equipment, scanning, spawning). One gadget per type per prefab.
3. **Prefab Templates** — Provide the actual GameObject (`CloneTemplate`, `FabricatorTemplate`, `EnergySourceTemplate`, `AssetBundleTemplate`, `EggTemplate`).

### The Registration Pattern (Used Everywhere)
```csharp
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;         // CRITICAL for extension methods
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Crafting;
using Ingredient = CraftData.Ingredient;

// 1. Identity
var info = PrefabInfo.WithTechType("ClassId", "Display Name", "Description")
    .WithIcon(SpriteManager.Get(TechType.Something));

// 2. Wrapper
var prefab = new CustomPrefab(info);

// 3. Game object
prefab.SetGameObject(new CloneTemplate(info, TechType.ThingToClone));

// 4. Gadgets (chain them)
prefab.SetRecipe(new RecipeData(
        new Ingredient(TechType.Titanium, 2),
        new Ingredient(TechType.Quartz, 1)))
    .WithFabricatorType(CraftTree.Type.Fabricator)
    .WithStepsToFabricatorTab("Personal", "Tools")
    .WithCraftingTime(5f);
prefab.SetUnlock(TechType.Seaglide);
prefab.SetEquipment(EquipmentType.Hand);
prefab.SetPdaGroupCategory(TechGroup.Resources, TechCategory.BasicMaterials);

// 5. Register (ALWAYS last)
prefab.Register();
```

### Key Extension Methods (from `Nautilus.Assets.Gadgets`)
**You must add `using Nautilus.Assets.Gadgets;`** for these to resolve. This is a common source of "does not contain a definition" errors:
- `SetRecipe()`, `SetUnlock()`, `SetPdaGroupCategory()`, `SetEquipment()`, `SetSpawns()`, `CreateFabricator()`, `SetVehicleUpgradeModule()`, `CreateFragment()`

---

## 7. Custom Items and Prefabs

### Cloning an Existing Item
```csharp
var clone = new CloneTemplate(info, TechType.Titanium)
{
    ModifyPrefab = go =>
    {
        // Modify the clone after creation
        go.transform.localScale = Vector3.one * 0.5f;
        var renderer = go.GetComponentInChildren<Renderer>();
        renderer.material.color = Color.red;
    }
};
prefab.SetGameObject(clone);
```

### Custom Model from Asset Bundle
```csharp
AssetBundle bundle = AssetBundleLoadingUtils.LoadFromAssetsFolder(
    Assembly.GetExecutingAssembly(), "myassets");
GameObject model = bundle.LoadAsset<GameObject>("MyPrefab");

// Apply Subnautica shaders (critical for correct rendering)
MaterialUtils.ApplySNShaders(model);
PrefabUtils.AddBasicComponents(model, info.ClassID, info.TechType,
    LargeWorldEntity.CellLevel.Medium);
model.AddComponent<Pickupable>();
```

**Asset bundle rules:** Use Unity **2019.4.36** to build. Do NOT add MonoBehaviour scripts in Unity editor. Always call `MaterialUtils.ApplySNShaders()`.

### Async Prefab Loading
All prefab loading is async since the Living Large update. **Never use `Resources.Load()`.**
```csharp
// Preferred: by TechType
var task = CraftData.GetPrefabForTechTypeAsync(TechType.Peeper);
yield return task;
GameObject prefab = task.GetResult();

// By Class ID
var task = UWE.PrefabDatabase.GetPrefabAsync("classIdString");
yield return task;
task.TryGetPrefab(out var prefab);
```
Use `UWE.CoroutineHost.StartCoroutine()` when you don't have a MonoBehaviour host.

---

## 8. EnumHandler

Nautilus consolidated all individual enum handlers into one generic API:

```csharp
using Nautilus.Handlers;

// Custom TechType
TechType myTech = EnumHandler.AddEntry<TechType>("MyTech")
    .WithPdaInfo("My Tech", "Description");

// Custom EquipmentType (for upgrade slots)
EquipmentType mySlot = EnumHandler.AddEntry<EquipmentType>("MySlot").Value;

// Custom TechCategory + register to a TechGroup
TechCategory myCat = EnumHandler.AddEntry<TechCategory>("MyCat")
    .WithPdaInfo("My Category")
    .RegisterToTechGroup(myTechGroup);

// Custom keybind button
GameInput.Button myButton = EnumHandler.AddEntry<GameInput.Button>("MyAction")
    .CreateInput("My Action")
    .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.V)
    .WithCategory("My Mod");

// Custom craft tree type
CraftTree.Type myTree = EnumHandler.AddEntry<CraftTree.Type>("MyTree")
    .CreateCraftTreeRoot(out ModCraftTreeRoot root);
```

**Replaces:** `TechTypeHandler`, `BackgroundTypeHandler`, `EquipmentHandler`, `PingTypeHandler`, `TechCategoryHandler`, `TechGroupHandler`, `CraftTreeTypeHandler`.

---

## 9. Crafting: Recipes, Trees, and Fabricators

### Recipe Data
```csharp
var recipe = new RecipeData
{
    craftAmount = 1,
    Ingredients = {
        new CraftData.Ingredient(TechType.Titanium, 2),
        new CraftData.Ingredient(TechType.CopperWire, 1),
    },
    LinkedItems = { TechType.Battery } // bonus output items
};
```

### Default Fabricator Craft Tree Paths

**Fabricator:** `Resources/BasicMaterials`, `Resources/AdvancedMaterials`, `Resources/Electronics`, `Survival/Water`, `Survival/CookedFood`, `Survival/CuredFood`, `Personal/Equipment`, `Personal/Tools`, `Machines`

**Vehicle Upgrade Console (SeamothUpgrades):** `CommonModules`, `SeamothModules`, `ExosuitModules`, `Torpedoes`

**Constructor:** `Vehicles`, `Rocket`

**Important:** Path segments are **separate string arguments**: `.WithStepsToFabricatorTab("Personal", "Tools")` NOT `"Personal/Tools"`.

### Adding Tabs to Craft Trees
```csharp
CraftTreeHandler.AddTabNode(
    CraftTree.Type.Fabricator,     // which tree
    "MyTab",                        // tab ID
    "My Tab Name",                  // display name
    SpriteManager.Get(TechType.X), // icon
    "Personal", "Tools");           // parent path
```

### Custom Fabricators
```csharp
var fab = new CustomPrefab(fabInfo);
var gadget = fab.CreateFabricator(out CraftTree.Type treeType)
    .AddTabNode("Tools", "Tools", SpriteManager.Get(TechType.Fabricator));

fab.SetGameObject(new FabricatorTemplate(fabInfo, treeType)
{
    FabricatorModel = FabricatorTemplate.Model.Fabricator
});
fab.SetRecipe(recipe);
fab.Register();

// Other mods reference treeType to add their items to this fabricator
```

---

## 10. Equipment and Upgrade Systems

### Equipment vs StorageContainer (Correct Choice)
The game has two distinct inventory systems:
- **`ItemsContainer`** (via `StorageContainer`) — grid-based inventory for holding items
- **`Equipment`** — typed slots for equippable items (what vehicles and the player use)

**For upgrade modules, use `Equipment`, not `StorageContainer`.** This was a key lesson from our case study.

### Custom Equipment Slots
```csharp
// Register a custom equipment type
var myEquipType = EnumHandler.AddEntry<EquipmentType>("SeaglideUpgrade").Value;

// Register slot-type mapping so the game knows what fits where
Equipment.slotMapping.Add("SeaglideUpgrade1", myEquipType);
Equipment.slotMapping.Add("SeaglideUpgrade2", myEquipType);

// Make your module equippable in this slot type
prefab.SetEquipment(myEquipType);
```

### Vehicle Upgrade Modules (Built-in Nautilus Support)
```csharp
// Passive module (always active when equipped)
prefab.SetVehicleUpgradeModule(EquipmentType.SeamothModule, QuickSlotType.Passive)
    .WithDepthUpgrade(1700f, true)
    .WithOnModuleAdded((Vehicle v, int slot) => { /* equipped */ })
    .WithOnModuleRemoved((Vehicle v, int slot) => { /* removed */ });

// Selectable module (player-activated)
prefab.SetVehicleUpgradeModule(EquipmentType.VehicleModule, QuickSlotType.Selectable)
    .WithEnergyCost(5f)
    .WithCooldown(10f)
    .WithOnModuleUsed((v, slot, charge, scalar) => { /* activated */ });
```

QuickSlotType options: `Passive`, `Selectable`, `SelectableChargeable`, `Instant`.

---

## 11. Input Handling and Keybinds

### The Right Way: Nautilus GameInput.Button
```csharp
// In Plugin.cs — register once
public static GameInput.Button MyButton =
    EnumHandler.AddEntry<GameInput.Button>("MyAction")
        .CreateInput("My Action Name")
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.V)
        .WithCategory("My Mod");

// In a Harmony patch or Update — check input
if (GameInput.GetButtonDown(Plugin.MyButton))
{
    // Do something
}
```

This integrates with the game's input system: respects menus/pause state, appears in the game's keybind settings, and players can rebind it.

### The Wrong Way: Raw Unity Input
```csharp
// DON'T DO THIS — dead since the August 2025 patch
if (Input.GetKeyDown(KeyCode.V)) { ... }
```
The legacy Unity input system (`UnityEngine.Input`) no longer functions in Subnautica after the 2025 patch, which migrated to the new `com.unity.inputsystem` package. Even before that, raw input fires during menus, pauses, and text input, and players cannot rebind it.

---

## 12. Mod Options and Configuration

### Simple: Nautilus ConfigFile with Attributes
```csharp
using Nautilus.Json;
using Nautilus.Options.Attributes;

[Menu("My Mod Options")]
public class MyConfig : ConfigFile
{
    [Slider("Damage Multiplier", 0, 100, DefaultValue = 50)]
    public int DamageMultiplier = 50;

    [Toggle("Enable Feature")]
    public bool FeatureEnabled = true;

    [Choice("Difficulty", "Easy", "Normal", "Hard")]
    [OnChange(nameof(OnDifficultyChanged))]   // NOTE: [OnChange] goes on the FIELD
    public int Difficulty = 1;

    private void OnDifficultyChanged(ChoiceChangedEventArgs<int> e)
    {
        // React to changes — note the generic type parameter
    }
}

// Register in Awake():
var config = new MyConfig();
OptionsPanelHandler.RegisterModOptions(config);
```

**Critical details:**
- `[OnChange]` goes on the **field**, not the handler method. Placing it on the method compiles but does nothing.
- `ChoiceChangedEventArgs` is **generic** in current Nautilus — use `ChoiceChangedEventArgs<int>`, `ChoiceChangedEventArgs<string>`, or `ChoiceChangedEventArgs<MyEnum>` matching your choice type.

### Advanced: ModOptions with Per-Item Events
```csharp
using Nautilus.Options;

public class MyModOptions : ModOptions
{
    public MyModOptions() : base("My Mod")
    {
        AddItem(ModSliderOption.Create("dmg", "Damage", 0, 100, 50));
        AddItem(ModToggleOption.Create("enable", "Enabled", true));
        AddItem(ModChoiceOption<string>.Create("mode", "Mode",
            new[] { "Normal", "Fast", "Turbo" }, 0));
        OnChanged += HandleChanged;
    }

    private void HandleChanged(object sender, OptionEventArgs e)
    {
        switch (e)
        {
            case SliderChangedEventArgs slider:
                Plugin.Logger.LogInfo($"{slider.Id} = {slider.Value}");
                break;
            case ChoiceChangedEventArgs<string> choice:  // Generic!
                break;
        }
    }
}
```
ModOptions alone does NOT persist. Pair with `ConfigEntry<T>` for persistence.

---

## 13. Spawning, Story Goals, and PDA

### Coordinated Spawns (Fixed Positions)
```csharp
CoordinatedSpawnsHandler.RegisterCoordinatedSpawn(
    new SpawnInfo(TechType.ReaperLeviathan, new Vector3(280, -1400, 47)));
// Or via CustomPrefab:
myPrefab.SetSpawns(new SpawnLocation(280, -1400, 47));
```

### Loot Distribution (Biome-Based)
```csharp
LootDistributionHandler.AddLootDistributionData(classId,
    new LootDistributionData.BiomeData[]
    {
        new() { biome = BiomeType.SafeShallows_Grass, count = 1, probability = 0.07f }
    });
```

### Story Goals
```csharp
StoryGoalHandler.RegisterBiomeGoal("MyGoal", GoalType.PDA, "kelpForest", 30f, 3f);
StoryGoalHandler.RegisterItemGoal("MyGoal", GoalType.Encyclopedia, TechType.Spadefish);
StoryGoalHandler.RegisterCustomEvent("MyGoal", () => { /* fires on completion */ });
```

### Databank (Encyclopedia) Entries
```csharp
PDAHandler.AddEncyclopediaEntry("EntryKey", "Lifeforms/Fauna", "Title", "Body text...",
    image: myTexture, unlockSound: PDAHandler.UnlockBasic);
```

### KnownTech Runtime Control
```csharp
KnownTech.Add(techType, verbose: true);   // Unlock a blueprint
KnownTech.Remove(techType);               // Lock a blueprint
KnownTech.Contains(techType);             // Check if known
// Note: GetAllKnownTechTypes() was REMOVED — use Contains() instead
```

---

## 14. Audio (FMOD)

**Never use Unity's built-in audio system** (`AudioSource`, `AudioClip.Play`). It ignores volume sliders, has no underwater effects, and plays during pause.

### Custom Sounds with Nautilus
```csharp
using Nautilus.Handlers;
using Nautilus.Utility;

// Register a custom sound from an AudioClip in an asset bundle
AssetBundle bundle = AssetBundleLoadingUtils.LoadFromAssetsFolder(
    Assembly.GetExecutingAssembly(), "sounds");
AudioClip clip = bundle.LoadAsset<AudioClip>("ExplosionSound");

CustomSoundHandler.RegisterCustomSound("MySFX", clip, AudioUtils.BusPaths.UnderwaterAmbient);

// Create an FMODAsset reference (required for game integration)
FMODAsset asset = AudioUtils.GetFmodAsset("MySFX");

// Play it:
Utils.PlayFMODAsset(asset, Player.main.transform.position);
```

Common bus paths: `AudioUtils.BusPaths.Music`, `.UnderwaterAmbient`, `.PDAVoice`, `.VoiceOvers`.

---

## 15. Localization

### JSON Files (Recommended)
Create `{modFolder}/Localization/English.json`:
```json
{ "MyItem": "My Item", "Tooltip_MyItem": "Description." }
```
```csharp
LanguageHandler.RegisterLocalizationFolder();
```

### Programmatic
```csharp
LanguageHandler.SetLanguageLine("MyItem", "My Item", "English");
```

---

## 16. Performance and Hot-Path Patching

When patching `Update()`, `FixedUpdate()`, or other per-frame methods:

1. **Keep patch bodies minimal.** Early-return for irrelevant cases immediately.
2. **Cache everything.** No allocations (`new`), no LINQ, no string concatenation.
3. **Never use `Traverse`** on hot paths — use `AccessTools.FieldRefAccess` instead:
   ```csharp
   // Cache once (static)
   static readonly AccessTools.FieldRef<Player, float> oxygenRef =
       AccessTools.FieldRefAccess<Player, float>("_oxygen");

   // Use in patch (near-zero overhead)
   ref float oxygen = ref oxygenRef(__instance);
   ```
4. **Prefer Transpiler** for hot-path changes — zero per-frame overhead since IL is modified at patch time.
5. **Never log in hot-path patches.**
6. **Wrap patch bodies in try/catch** to prevent game crashes:
   ```csharp
   static void Postfix(SomeClass __instance)
   {
       try { /* mod logic */ }
       catch (Exception ex) { Plugin.Logger.LogError(ex); }
   }
   ```

---

## 17. Debugging and Troubleshooting

### Essential Debug Steps
1. **Check `BepInEx/LogOutput.log`** — first place to look for errors
2. **Enable Harmony debug log:** `Harmony.DEBUG = true;` creates `harmony.log.txt` on Desktop
3. **Use `[HarmonyDebug]`** on individual patch classes for IL dumps
4. **Use Runtime Editor** to inspect GameObjects at runtime
5. **Inspect other mods' patches:**
   ```csharp
   var patches = Harmony.GetPatchInfo(AccessTools.Method(typeof(X), "Y"));
   foreach (var p in patches.Prefixes) Logger.LogInfo($"Prefix: {p.owner}");
   ```

### Inspecting Game Assemblies

Write a small console app to enumerate types, fields, and methods via reflection. This is the authoritative way to discover what the current game API actually looks like:

```csharp
var asm = Assembly.LoadFrom("path/to/Assembly-CSharp.dll");
var type = asm.GetType("PlayerController");
foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic
    | BindingFlags.Instance | BindingFlags.Static))
{
    Console.WriteLine($"{field.FieldType.Name} {field.Name} [{field.Attributes}]");
}
```

### Common Errors and Fixes

| Error | Cause | Fix |
|-|-|-|
| No `LogOutput.log` generated | BepInEx not loading | Verify `winhttp.dll` and `doorstop_config.ini` in game root |
| Plugin not detected | Missing attribute or wrong base class | Add `[BepInPlugin]` and inherit `BaseUnityPlugin` |
| `CS0117: 'X' does not contain 'Y'` | Field/method renamed or removed in update | Use reflection to inspect actual API |
| `Undefined target method for patch` | Method renamed/removed | Decompile new Assembly-CSharp; update target |
| `CS0507: cannot change access modifiers` | Method visibility changed | Match the current access level |
| `CS0122: inaccessible due to protection level` | Field became private | Use `AccessTools.Field()` or publicizer |
| `CS0305: requires type arguments` | API became generic (e.g., `ChoiceChangedEventArgs<T>`) | Add the type parameter |
| Missing extension methods (`SetRecipe`, etc.) | Missing `using Nautilus.Assets.Gadgets;` | Add the using directive |
| `AssetReferenceGameObject` not found | Missing Unity.Addressables reference | Add `Unity.Addressables.dll` reference |
| NuGet restore fails | `nuget.bepinex.dev` is down | Switch to local DLL references |
| Transpiler silent failure | CodeMatcher match failed | Always use `.ThrowIfInvalid()` |
| Patch runs but nothing happens | Wrong field being modified | Use reflection to inspect actual class members |
| GameObject immediately destroyed | Created during plugin init (2025 patch) | Create objects in scene-loaded callbacks |
| Config file not generated | `Config.Bind` never called | Ensure at least one `Bind` call executes |

---

## 18. Migrating from SMLHelper 2.0 to Nautilus

The root namespace changed from `SMLHelper.V2` to `Nautilus`. All handlers became `public static` classes (no more `.Main` property, no more interface implementations).

### Essential Namespace Imports
```csharp
using Nautilus.Assets;              // CustomPrefab, PrefabInfo
using Nautilus.Assets.Gadgets;      // SetRecipe, SetEquipment, SetUnlock, etc.
using Nautilus.Assets.PrefabTemplates; // CloneTemplate, FabricatorTemplate
using Nautilus.Crafting;            // RecipeData
using Nautilus.Handlers;            // EnumHandler, CraftDataHandler, CraftTreeHandler
using Nautilus.Options;             // ModOptions, ModSliderOption, etc.
using Nautilus.Options.Attributes;  // [Menu], [Slider], [Toggle], [Choice], [OnChange]
using Nautilus.Json;                // ConfigFile
using Nautilus.Utility;             // AudioUtils, MaterialUtils
```

### Handler Replacements

| SMLHelper 2.0 | Nautilus |
|-|-|
| `TechTypeHandler.AddTechType()` | `EnumHandler.AddEntry<TechType>()` |
| `EquipmentHandler` | `EnumHandler.AddEntry<EquipmentType>()` |
| `BackgroundTypeHandler` | `EnumHandler.AddEntry<CraftData.BackgroundType>()` |
| `PingTypeHandler` | `EnumHandler.AddEntry<PingType>()` |
| `PDAEncyclopediaHandler` | `PDAHandler` |
| `PDALogHandler` | `PDAHandler` |
| `InGameMenuHandler` | `SaveUtils` (in `Nautilus.Utility`) |
| `BioReactorHandler.SetBioreactorCharge()` | `BaseBioReactor.charge[type] = value` (direct) |
| `Handler.Main` property pattern | Direct static class access |

### Asset Class Replacements

| SMLHelper 2.0 | Nautilus |
|-|-|
| `ModPrefab` | `CustomPrefab` |
| `Buildable` | `ScanningGadget` (via `.SetPdaGroupCategory()`) |
| `PdaItem` | `ScanningGadget` (via `.SetPdaGroupCategory()`) |
| `Equipable` | `EquipmentGadget` (via `.SetEquipment()`) |
| `Craftable` | `CraftingGadget` (via `.SetRecipe()`) |
| `CustomFabricator` | `FabricatorTemplate` + `.CreateFabricator()` |

### Sound System Changes
- `SoundChannel` replaced by bus paths (`AudioUtils.BusPaths.*`)
- `PlaySound()` renamed to `TryPlaySound()`
- Use `CustomSoundHandler.RegisterCustomSound()` for all custom audio

**SMLHelper 2.15.0.1 and Nautilus are incompatible** — you cannot run both simultaneously.

---

## 19. Updating and Fixing Broken Mods

Every major Subnautica update rewrites `Assembly-CSharp.dll`, breaking Harmony patches that target renamed, removed, or re-signatured methods. The August 2025 patch removed `Atlas.Sprite`, killed the legacy Unity input system, changed CraftData internals, and altered plugin initialization timing. The October 2025 security hotfix permanently disabled all legacy game branches, forcing every mod onto the latest version.

### Systematic Repair Process

**Step 1: Read the log.** Open `BepInEx/LogOutput.log`. It tells you exactly which plugins loaded, failed, and why. `ArgumentException: Undefined target method for patch method` means a Harmony target was renamed or removed.

**Step 2: Decompile the new Assembly-CSharp.dll.** Open it in dnSpy or ILSpy. Search for the class and method your patch targets. Compare against the old version to identify renames, signature changes, and visibility changes.

**Step 3: Write a reflection inspector.** For quick API discovery without decompiling:
```csharp
var asm = Assembly.LoadFrom("path/to/Assembly-CSharp.dll");
var type = asm.GetType("PlayerController");
foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic
    | BindingFlags.Instance | BindingFlags.Static))
{
    Console.WriteLine($"{field.FieldType.Name} {field.Name} [{field.Attributes}]");
}
```

**Step 4: Fix each breakage** using the patterns from this guide.

**Step 5: Update all references.** Rebuild against new game assemblies and latest Nautilus/BepInEx.

### Common Breakage Patterns

**Field/method visibility changes** (`public` → `private`/`protected`): `GhostCrafter.powerRelay`, `Fabricator.opened`, `Crafter.state`, `BatterySource.Start()` all changed visibility. Fix: use AssemblyPublicizer or `AccessTools`.

**Field renames / wrong target class**: `seaglideForwardMaxSpeed` on `PlayerController` is a config default, not the live motor. Actual runtime speed lives on `underWaterController` (a `PlayerMotor`). Fix: decompile to find the correct class and field.

**API removal**: `KnownTech.GetAllKnownTechTypes()` removed. Fix: use `KnownTech.Contains(TechType)`.

**Method signature changes**: `KnownTech.Add` now takes `(TechType, bool verbose)`. Fix: update call sites.

**Atlas.Sprite removal** (2025 patch): All code using `Atlas.Sprite` must switch to `UnityEngine.Sprite`.

**Legacy input system death** (2025 patch): `Input.GetKeyDown(KeyCode.X)` stopped working. Use `GameInput` system.

**Plugin initialization timing** (2025 patch): GameObjects created during init (before scene loads) are immediately destroyed. Move creation to lifecycle hooks or use `UWE.CoroutineHost.StartCoroutine()`.

---

## 20. Case Study: What Broke and Why (LiteralSeaglideUpgrades)

### Problem: Mod broken after Subnautica + Nautilus updates

### What the Author Changed to Fix It

**1. StorageContainer → Equipment system**
- Old: `PrefabUtils.AddStorageContainer()` — grid-based inventory on the Seaglide
- New: `Equipment` class with custom `EquipmentType` and named slots
- Why: The Equipment system is what the game natively uses for upgrade slots. StorageContainer is for general item storage. Using Equipment means modules appear as proper equippable items with dedicated slots.

**2. Raw `Input.GetKeyDown()` → `GameInput.GetButtonDown()`**
- Old: `Input.GetKeyDown(KeyCode.V)` — fires during menus/pause
- New: `EnumHandler.AddEntry<GameInput.Button>()` + `GameInput.GetButtonDown()` — respects game state, rebindable
- Why: Legacy input system is dead post-2025 patch. Proper integration with Subnautica's input system.

**3. Speed fields: direct PlayerController → underWaterController sub-object**
- Old: `__instance.seaglideForwardMaxSpeed` — these are config defaults, not the active motor
- New: `__instance.underWaterController.forwardMaxSpeed` — the actual runtime `PlayerMotor`
- Why: `PlayerController` has both *configured defaults* (like `seaglideForwardMaxSpeed`) and *active motor objects* (like `underWaterController`). Patching `SetMotorMode` postfix needs to modify the motor that's actually driving movement.

**4. `KnownTech.GetAllKnownTechTypes()` → `KnownTech.Contains()`**
- Removed in current game version. Use `Contains()` to check individual TechTypes.

**5. Private field access**
- Multiple game classes made fields private/protected between versions
- `GhostCrafter.powerRelay`, `Fabricator.opened`, `Crafter.state`, `BatterySource.Start()` visibility all changed
- Fix: Use `AccessTools.Field()` for reflection, or better yet, use `BepInEx.AssemblyPublicizer`

**6. Nautilus API changes**
- `ChoiceChangedEventArgs` → `ChoiceChangedEventArgs<T>` (became generic)
- `[OnChange]` attribute goes on the **field**, not the handler method
- `SetRecipe()`, `SetUnlock()`, `SetPdaGroupCategory()` are extension methods in `Nautilus.Assets.Gadgets` — need explicit `using`

---

## 21. Common Pitfalls Quick Reference

### Do
- Use `GameInput.GetButtonDown()` for keybinds
- Use `Equipment` for upgrade/module slots
- Use Postfix patches by default
- Use `AccessTools` or publicizer for private members
- Use `.ThrowIfInvalid()` in CodeMatcher chains
- Use `UWE.CoroutineHost.StartCoroutine()` for coroutines without a MonoBehaviour
- Call `prefab.Register()` last
- Use separate string args for craft tree paths
- Add `using Nautilus.Assets.Gadgets;` for extension methods
- Use Unity 2019.4.36 for asset bundles
- Use FMOD / `CustomSoundHandler` for all audio
- Keep local DLL fallbacks for when `nuget.bepinex.dev` goes down
- Use `UnityEngine.Sprite` (not `Atlas.Sprite`, which was removed)

### Don't
- Use `Input.GetKeyDown()` — legacy input system is dead
- Use `StorageContainer` for equipment slots
- Use `return false` in Prefix patches — breaks other mods
- Use `Resources.Load()` — game uses Addressables
- Use Unity's built-in audio (`AudioSource`) — breaks volume/effects
- Bundle your own `0Harmony.dll`
- Use `Traverse` on hot paths — use `FieldRefAccess` instead
- Allocate in Update patches — cache everything
- Log in per-frame patches
- Assume field names haven't changed between game versions
- Create GameObjects during plugin `Awake()` (destroyed by 2025 patch timing)
- Reference `mscorlib.dll` or `System.*` from the game's Managed folder
- Use `Atlas.Sprite` — removed in 2025 update

---

## 22. API Quick Reference

### Nautilus Handlers (Static Classes)
| Handler | Purpose |
|-|-|
| `EnumHandler` | Register custom enum values (TechType, EquipmentType, etc.) |
| `CraftTreeHandler` | Add/remove nodes in craft trees |
| `CraftDataHandler` | Modify recipes, equipment types, backgrounds |
| `KnownTechHandler` | Configure blueprint unlock conditions |
| `PDAHandler` | Encyclopedia entries, log entries, scanner data |
| `CoordinatedSpawnsHandler` | Fixed-position spawns |
| `LootDistributionHandler` | Biome-based random spawns |
| `LanguageHandler` | Localization/translations |
| `OptionsPanelHandler` | Register mod options UI |
| `ConsoleCommandsHandler` | Custom console commands |
| `StoryGoalHandler` | Story goals, triggers, events |
| `CustomSoundHandler` | Register custom audio |

### Nautilus Prefab Templates
| Template | Purpose |
|-|-|
| `CloneTemplate` | Clone an existing game item |
| `FabricatorTemplate` | Custom fabricator |
| `EnergySourceTemplate` | Batteries/power cells |
| `AssetBundleTemplate` | Load from Unity asset bundle |
| `EggTemplate` | Creature eggs |

### Nautilus Gadget Extension Methods (require `using Nautilus.Assets.Gadgets;`)
| Method | Purpose |
|-|-|
| `SetRecipe()` | Crafting recipe |
| `SetUnlock()` | Unlock condition |
| `SetEquipment()` | Make equippable |
| `SetPdaGroupCategory()` | PDA placement |
| `SetSpawns()` | World spawning |
| `SetVehicleUpgradeModule()` | Vehicle upgrade behavior |
| `CreateFabricator()` | Custom fabricator tree |
| `CreateFragment()` | Scannable fragments |

### Game APIs (from Assembly-CSharp)
| API | Purpose |
|-|-|
| `GameInput.GetButtonDown(button)` | Check custom input |
| `KnownTech.Add(techType, verbose)` | Unlock blueprint at runtime |
| `KnownTech.Remove(techType)` | Lock blueprint |
| `KnownTech.Contains(techType)` | Check if known |
| `Equipment.slotMapping.Add(name, type)` | Register custom equipment slots |
| `CraftData.GetPrefabForTechTypeAsync(type)` | Async prefab loading |
| `SpriteManager.Get(TechType)` | Get item icon sprite |
| `Player.main` | Current player instance |
| `Inventory.main` | Current inventory |
| `UWE.CoroutineHost.StartCoroutine()` | Run coroutines without MonoBehaviour |
