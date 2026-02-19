# PowerSaver - Subnautica Power Drain Reduction Mod

A BepInEx 5 mod that reduces power/battery drain across the game with configurable multipliers for tools, vehicles, and bases.

## What It Does

Patches three core energy consumption methods via Harmony:

- **`EnergyMixin.ConsumeEnergy`** — Covers all battery-powered tools and equipment (flashlight, seaglide, scanner, repair tool, etc.)
- **`Vehicle.ConsumeEnergy`** — Covers Seamoth, Prawn Suit, and Cyclops engine drain
- **`PowerRelay.ConsumeEnergy`** — Covers base/habitat power consumption (water filtration, fabricators, charge fins, etc.)

Each has an independent multiplier so you can fine-tune exactly what gets reduced.

## Configuration

After first launch, a config file is generated at:
```
BepInEx\config\com.adam.powersaver.cfg
```

### Settings

| Section | Key | Default | Description |
|---------|-----|---------|-------------|
| General | DrainMultiplier | 0.75 | Global multiplier for all battery drain. 0.75 = 25% reduction |
| Vehicles | VehicleDrainMultiplier | 0.75 | Vehicle-specific multiplier. Stacks with global on vehicle batteries |
| Base | BaseDrainMultiplier | 0.75 | Base power relay multiplier |
| Debug | EnableLogging | false | Log drain events to BepInEx console (very noisy) |

### Examples
- `DrainMultiplier = 0.5` → Tools use half power
- `VehicleDrainMultiplier = 0.25` → Vehicles use 75% less power
- `BaseDrainMultiplier = 1.0` → Base drain unchanged (vanilla)

**Note:** Vehicle and base multipliers are applied on top of the global multiplier if the drain goes through EnergyMixin first. Some vehicle systems call Vehicle.ConsumeEnergy directly, some go through EnergyMixin. If something feels too aggressive, set the category-specific one to 1.0 and just use Global.

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
   bin\Release\net472\PowerSaver.dll → BepInEx\plugins\PowerSaver\PowerSaver.dll
   ```

4. **Launch Subnautica** and check `BepInEx\LogOutput.log` for:
   ```
   [Info : PowerSaver] PowerSaver v1.0.0 loaded! Global drain: 0.75x | Vehicles: 0.75x | Base: 0.75x
   ```

## Troubleshooting

- **Mod not loading:** Make sure SMLHelper is removed and you're running Nautilus. This mod doesn't depend on either, but SMLHelper conflicts can prevent the chainloader from finishing.
- **No config file generated:** The config is only created on first successful load. Check LogOutput.log for errors.
- **"Could not resolve type" errors:** Your game version may have changed the method signatures. Open `Assembly-CSharp.dll` in dnSpy/ILSpy and verify `EnergyMixin.ConsumeEnergy`, `Vehicle.ConsumeEnergy`, and `PowerRelay.ConsumeEnergy` still exist with the same signatures.

## Compatibility

- Subnautica (Steam, current build as of Feb 2026)
- BepInEx 5.4.x
- Should not conflict with other mods unless they also patch the same ConsumeEnergy methods

## How to Extend

Want to add per-device control? In dnSpy, look at what calls `EnergyMixin.ConsumeEnergy` — you can check the parent GameObject's TechType in the prefix to apply different multipliers per tool. For example:

```csharp
[HarmonyPrefix]
static void Prefix(EnergyMixin __instance, ref float amount)
{
    var techType = CraftData.GetTechType(__instance.gameObject);
    if (techType == TechType.Seaglide)
        amount *= 0.5f; // Seaglide gets extra savings
}
```
