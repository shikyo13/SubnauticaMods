# PowerSaver BZ

BepInEx 5 and Nautilus mod for Subnautica: Below Zero that reduces supported tool, vehicle, and habitat power drain.

## Drain Model

PowerSaver BZ 1.0.3 uses a true global multiplier:

| Drain path | Effective multiplier |
|-|-|
| Tools and battery equipment | `Global` |
| Vehicles and Cyclops sonar fallback path | `Global x Vehicle` |
| Habitat/base power | `Global x Base` |

With the default settings, tools, vehicles, and base power all drain at 75 percent of vanilla. The global multiplier defaults to `0.75`; vehicle and base category multipliers default to `1.0` so they only add extra adjustment when you choose to change them.

## Configuration

Active settings are stored by Nautilus at:

```text
BepInEx\config\PowerSaver.BZ\config.json
```

If `BepInEx\config\com.zerotheabsolute.powersaver.bz.cfg` exists, it is a legacy BepInEx config file and is not used by the current BZ version.

## EasyCraft Compatibility

PowerSaver BZ 1.0.3 includes an always-on scoped compatibility patch for EasyCraft BZ 1.2.5 AutoCraft power accounting. The patch is only active while EasyCraft is running its own energy checks. It does not force EasyCraft's energy checks to succeed. Instead, it reports virtual available and consumed power to EasyCraft while preserving the real reduced base drain.

If standalone `EasyCraftFix.BZ` is installed, remove it when using PowerSaver BZ 1.0.1 or newer. That separate fix bypasses broader EasyCraft energy results and is no longer needed for the PowerSaver interaction.

## Charger Compatibility

Battery chargers and power cell chargers keep vanilla charge speed. PowerSaver applies reduced actual base power drain, then reports vanilla-equivalent available and consumed power back to the charger while `Charger.Update()` is running. This prevents low base drain settings from slowing or stopping battery charging.

## Build

```text
dotnet build PowerSaver.BZ\PowerSaver.BZ.csproj -c Release
```

The project copies `PowerSaver.BZ.dll` to the BZ BepInEx plugin folder after a successful build.
