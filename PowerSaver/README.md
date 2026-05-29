# PowerSaver - Subnautica Power Drain Reduction Mod

A BepInEx 5 mod that reduces power/battery drain across the game with configurable multipliers for tools, vehicles, and bases.

## What It Does

Patches the core energy consumption paths via Harmony:

- **`EnergyMixin.ConsumeEnergy`** - Covers battery-powered tools and equipment, including flashlight, seaglide, scanner, and repair tool.
- **`Vehicle.ConsumeEnergy`** - Covers Seamoth, Prawn Suit, and vehicle upgrade module drain.
- **`PowerRelay.ModifyPower`** - Covers habitat power consumption.
- **`CyclopsSonarButton.SonarPing` context** - Treats Cyclops sonar power relay drain as vehicle drain instead of habitat drain.

PowerSaver 1.0.1 uses a true global multiplier:

| Drain path | Effective multiplier |
|-|-|
| Tools and battery equipment | `Global` |
| Vehicles and Cyclops sonar | `Global x Vehicle` |
| Habitat/base power | `Global x Base` |

With the default settings, tools, vehicles, Cyclops sonar, and base power all drain at 75 percent of vanilla. The global multiplier defaults to `0.75`; vehicle and base category multipliers default to `1.0` so they only add extra adjustment when you choose to change them.

## Configuration

After first launch, a config file is generated at:
```
BepInEx\config\com.zerotheabsolute.powersaver.cfg
```

### Settings

| Section | Key | Default | Description |
|-|-|-|-|
| General | DrainMultiplier | 0.75 | Baseline multiplier for supported power drain |
| Vehicles | VehicleDrainMultiplier | 1.0 | Additional vehicle multiplier. Effective vehicle drain is Global x Vehicle |
| Base | BaseDrainMultiplier | 1.0 | Additional base multiplier. Effective base drain is Global x Base |
| Debug | EnableLogging | false | Log drain events to BepInEx console (very noisy) |

### Examples
- `DrainMultiplier = 0.5` means tools use half power.
- `DrainMultiplier = 0.75` and `VehicleDrainMultiplier = 0.5` means vehicles use 37.5 percent power.
- `DrainMultiplier = 0.75` and `BaseDrainMultiplier = 1.0` means base drain uses 75 percent power.

Existing generated 1.0 defaults of `0.75 / 0.75 / 0.75` migrate to `0.75 / 1.0 / 1.0` so effective default drain stays at 75 percent. Custom category values are left alone.

## Building

### Prerequisites
- .NET SDK 6.0+ (for building .NET 4.7.2 targets) or Visual Studio 2022
- Subnautica with BepInEx 5 installed

### Steps

1. **Verify paths in `PowerSaver.csproj`:**
   Open the .csproj and confirm `SubnauticaDir` points to your install:
   ```xml
   <SubnauticaDir>D:\SteamLibrary\steamapps\common\Subnautica</SubnauticaDir>
   ```

2. **Build:**
   ```
   dotnet build --configuration Release
   ```

3. **Deploy:**
   The build auto-copies `PowerSaver.dll` to `BepInEx\plugins\PowerSaver\`.
   If auto-copy fails, manually copy:
   ```
   bin\Release\net472\PowerSaver.dll -> BepInEx\plugins\PowerSaver\PowerSaver.dll
   ```

4. **Launch Subnautica** and check `BepInEx\LogOutput.log` for:
   ```
   [Info : PowerSaver] PowerSaver v1.0.1 loaded! Global drain: 0.75x | Effective vehicles: 0.75x | Effective base: 0.75x
   ```

## Troubleshooting

- **Mod not loading:** Make sure SMLHelper is removed and you're running Nautilus. This mod doesn't depend on either, but SMLHelper conflicts can prevent the chainloader from finishing.
- **No config file generated:** The config is only created on first successful load. Check LogOutput.log for errors.
- **"Could not resolve type" errors:** Your game version may have changed method names or signatures. Open `Assembly-CSharp.dll` in dnSpy/ILSpy and verify `EnergyMixin.ConsumeEnergy`, `Vehicle.ConsumeEnergy`, `PowerRelay.ModifyPower`, and `CyclopsSonarButton.SonarPing` still exist with the same signatures.

## Compatibility

- Subnautica (Steam, current build as of Feb 2026)
- BepInEx 5.4.x
- Should not conflict with other mods unless they also patch the same ConsumeEnergy methods

## How to Extend

Want to add per-device control? In dnSpy, look at what calls `EnergyMixin.ConsumeEnergy`. You can check the parent GameObject's TechType in the prefix to apply different multipliers per tool. For example:

```csharp
[HarmonyPrefix]
static void Prefix(EnergyMixin __instance, ref float amount)
{
    var techType = CraftData.GetTechType(__instance.gameObject);
    if (techType == TechType.Seaglide)
        amount *= 0.5f; // Seaglide gets extra savings
}
```
