# Tier 2: Nautilus API Reference

Read when using Nautilus features (custom items, recipes, equipment, options, etc.).

## Three-Part Architecture

1. **CustomPrefab** — registration wrapper, manages gadgets and the game object
2. **Gadgets** — configure game data (recipes, equipment, scanning, spawning). One gadget per type per prefab
3. **Prefab Templates** — provide the actual GameObject: `CloneTemplate`, `FabricatorTemplate`, `EnergySourceTemplate`, `AssetBundleTemplate`, `EggTemplate`

## Registration Pattern (Core Workflow)

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
    .WithStepsToFabricatorTab("Personal", "Tools")   // Separate string args!
    .WithCraftingTime(5f);
prefab.SetUnlock(TechType.Seaglide);
prefab.SetEquipment(EquipmentType.Hand);
prefab.SetPdaGroupCategory(TechGroup.Resources, TechCategory.BasicMaterials);

// 5. Register (ALWAYS last)
prefab.Register();
```

## EnumHandler (Consolidated API)

```csharp
using Nautilus.Handlers;

// Custom TechType
TechType myTech = EnumHandler.AddEntry<TechType>("MyTech")
    .WithPdaInfo("My Tech", "Description");

// Custom EquipmentType
EquipmentType mySlot = EnumHandler.AddEntry<EquipmentType>("MySlot").Value;

// Custom TechCategory + register to TechGroup
TechCategory myCat = EnumHandler.AddEntry<TechCategory>("MyCat")
    .WithPdaInfo("My Category")
    .RegisterToTechGroup(myTechGroup);

// Custom keybind
GameInput.Button myButton = EnumHandler.AddEntry<GameInput.Button>("MyAction")
    .CreateInput("My Action")
    .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.V)
    .WithCategory("My Mod");

// Custom craft tree type
CraftTree.Type myTree = EnumHandler.AddEntry<CraftTree.Type>("MyTree")
    .CreateCraftTreeRoot(out ModCraftTreeRoot root);
```

Replaces: `TechTypeHandler`, `EquipmentHandler`, `BackgroundTypeHandler`, `PingTypeHandler`, `TechCategoryHandler`, `TechGroupHandler`, `CraftTreeTypeHandler`.

## Recipes and Craft Trees

```csharp
var recipe = new RecipeData
{
    craftAmount = 1,
    Ingredients = {
        new CraftData.Ingredient(TechType.Titanium, 2),
        new CraftData.Ingredient(TechType.CopperWire, 1),
    },
    LinkedItems = { TechType.Battery }  // bonus output
};
```

### Default Craft Tree Paths

- **Fabricator:** `Resources/BasicMaterials`, `Resources/AdvancedMaterials`, `Resources/Electronics`, `Survival/Water`, `Survival/CookedFood`, `Survival/CuredFood`, `Personal/Equipment`, `Personal/Tools`, `Machines`
- **Vehicle Upgrade Console (SeamothUpgrades):** `CommonModules`, `SeamothModules`, `ExosuitModules`, `Torpedoes`
- **Constructor:** `Vehicles`, `Rocket`

Path segments are **separate string arguments**: `.WithStepsToFabricatorTab("Personal", "Tools")` NOT `"Personal/Tools"`.

### Adding Tabs and Custom Fabricators

```csharp
// Add tab to existing tree
CraftTreeHandler.AddTabNode(CraftTree.Type.Fabricator, "MyTab", "My Tab", sprite, "Personal", "Tools");

// Custom fabricator
var fab = new CustomPrefab(fabInfo);
var gadget = fab.CreateFabricator(out CraftTree.Type treeType)
    .AddTabNode("Tools", "Tools", SpriteManager.Get(TechType.Fabricator));
fab.SetGameObject(new FabricatorTemplate(fabInfo, treeType) { FabricatorModel = FabricatorTemplate.Model.Fabricator });
fab.SetRecipe(recipe);
fab.Register();
```

## Equipment and Upgrades

```csharp
// Custom equipment slots
var myEquipType = EnumHandler.AddEntry<EquipmentType>("SeaglideUpgrade").Value;
Equipment.slotMapping.Add("SeaglideUpgrade1", myEquipType);

// Vehicle upgrade — passive (always active)
prefab.SetVehicleUpgradeModule(EquipmentType.SeamothModule, QuickSlotType.Passive)
    .WithDepthUpgrade(1700f, true)
    .WithOnModuleAdded((Vehicle v, int slot) => { /* equipped */ })
    .WithOnModuleRemoved((Vehicle v, int slot) => { /* removed */ });

// Vehicle upgrade — selectable (player-activated)
prefab.SetVehicleUpgradeModule(EquipmentType.VehicleModule, QuickSlotType.Selectable)
    .WithEnergyCost(5f)
    .WithCooldown(10f)
    .WithOnModuleUsed((v, slot, charge, scalar) => { /* activated */ });
```

QuickSlotType options: `Passive`, `Selectable`, `SelectableChargeable`, `Instant`.

## Input Handling

See EnumHandler section above for `GameInput.Button` registration. Check with `GameInput.GetButtonDown(Plugin.MyButton)`. Integrates with game's input system: respects menus/pause, rebindable.

## Mod Options

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
    [OnChange(nameof(OnDifficultyChanged))]   // Goes on the FIELD!
    public int Difficulty = 1;

    private void OnDifficultyChanged(ChoiceChangedEventArgs<int> e) { }  // Generic!
}
// Register: OptionsPanelHandler.RegisterModOptions(new MyConfig());
```

For per-item events, use `ModOptions` base class with `AddItem()` + `OnChanged` handler.
ModOptions alone does NOT persist — pair with `ConfigEntry<T>`.

## Spawning and Story Goals

```csharp
myPrefab.SetSpawns(new SpawnLocation(280, -1400, 47));  // Fixed position

LootDistributionHandler.AddLootDistributionData(classId,  // Biome-based
    new LootDistributionData.BiomeData[] {
        new() { biome = BiomeType.SafeShallows_Grass, count = 1, probability = 0.07f }
    });

StoryGoalHandler.RegisterBiomeGoal("MyGoal", GoalType.PDA, "kelpForest", 30f, 3f);
PDAHandler.AddEncyclopediaEntry("EntryKey", "Lifeforms/Fauna", "Title", "Body...");
```

## Audio (FMOD)

```csharp
CustomSoundHandler.RegisterCustomSound("MySFX", clip, AudioUtils.BusPaths.UnderwaterAmbient);
Utils.PlayFMODAsset(AudioUtils.GetFmodAsset("MySFX"), Player.main.transform.position);
```

Bus paths: `.Music`, `.UnderwaterAmbient`, `.PDAVoice`, `.VoiceOvers`.

## Localization

Create `{modFolder}/Localization/English.json`: `{ "MyItem": "My Item", "Tooltip_MyItem": "Desc." }`
Then call `LanguageHandler.RegisterLocalizationFolder();` — or use `LanguageHandler.SetLanguageLine()` programmatically.
