# Tier 2: Project Setup and Configuration

Read when setting up a new mod or migrating from SMLHelper.

## Prerequisites

- .NET SDK (`dotnet --version`)
- IDE: Visual Studio Community, JetBrains Rider, or VS Code with C# extension
- Subnautica (Steam)
- BepInEx Subnautica Pack v5.4.23+ (extract into game folder, `BepInEx/` next to `Subnautica_Data/`)
- Nautilus: `Nautilus.dll` in `BepInEx/plugins/Nautilus/`

Run game once via Steam (never directly via .exe), reach main menu, then exit. See CLAUDE.md for directory layout.

## Project Templates

```bash
dotnet new install Subnautica.Templates
dotnet new snmod_nautilus -n MyModName
```

Templates: `snmod` (basic), `snmod_empty` (minimal), `snmod_nautilus` (with Nautilus example).

## Plugin Class Template

```csharp
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace MyMod
{
    [BepInPlugin(MyGuid, PluginName, VersionString)]
    [BepInProcess("Subnautica.exe")]
    [BepInDependency("com.snmodding.nautilus")]  // Required if using Nautilus
    public class MyModPlugin : BaseUnityPlugin
    {
        private const string MyGuid = "com.adam.mymod";
        private const string PluginName = "My Mod";
        private const string VersionString = "1.0.0";

        internal static new ManualLogSource Logger { get; private set; }
        private static readonly Assembly Assembly = Assembly.GetExecutingAssembly();

        private void Awake()
        {
            Logger = base.Logger;
            Harmony.CreateAndPatchAll(Assembly, MyGuid);
            Logger.LogInfo($"{PluginName} loaded.");
        }
    }
}
```

## Logging

Levels: `LogDebug` (hidden by default), `LogInfo`, `LogWarning`, `LogError`, `LogFatal`. Output: BepInEx console, `BepInEx/LogOutput.log`, Unity `output_log.txt`. Enable console: `BepInEx/config/BepInEx.cfg` → `[Logging.Console]` → `Enabled = true`.

## BepInEx Configuration

```csharp
configDamage = Config.Bind("General", "DamageMultiplier", 2.0f,
    new ConfigDescription("Multiplier", new AcceptableValueRange<float>(0.5f, 10.0f)));
```

Auto-generates at `BepInEx/config/<GUID>.cfg`. Supports: primitives, enums, `Color`, `Vector2/3/4`, `Quaternion`, `KeyboardShortcut`.

## .csproj: NuGet Approach (Preferred)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net472</TargetFramework>
    <LangVersion>latest</LangVersion>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <SubnauticaDir>D:\SteamLibrary\steamapps\common\Subnautica</SubnauticaDir>
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

NuGet `BepInEx.Core` 5.4.21 is compile-time compatible with runtime 5.4.23.

## .csproj: Local DLL Fallback (When nuget.bepinex.dev Is Down)

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
    <Reference Include="Assembly-CSharp">
        <HintPath>$(SubnauticaDir)\Subnautica_Data\Managed\Assembly-CSharp.dll</HintPath>
        <Private>false</Private>
    </Reference>
</ItemGroup>
```

**Use `<Private>false</Private>` on all game references** — they already exist at runtime.

## AssemblyPublicizer

Makes private/protected members accessible at compile time. Generates `IgnoresAccessChecksTo` attributes — original DLLs untouched. Essential for fields that changed visibility across game updates. With publicizer, reference private fields directly — no `AccessTools` needed at compile time.

## SMLHelper 2.0 to Nautilus Migration

### Namespace: `SMLHelper.V2.*` → `Nautilus.*`

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
| `InGameMenuHandler` | `SaveUtils` (in `Nautilus.Utility`) |
| `BioReactorHandler.SetBioreactorCharge()` | `BaseBioReactor.charge[type] = value` |
| `Handler.Main` property pattern | Direct static class access |

### Asset Class Replacements

| SMLHelper 2.0 | Nautilus |
|-|-|
| `ModPrefab` | `CustomPrefab` |
| `Buildable` / `PdaItem` | `ScanningGadget` (via `.SetPdaGroupCategory()`) |
| `Equipable` | `EquipmentGadget` (via `.SetEquipment()`) |
| `Craftable` | `CraftingGadget` (via `.SetRecipe()`) |
| `CustomFabricator` | `FabricatorTemplate` + `.CreateFabricator()` |

### Sound System Changes

- `SoundChannel` replaced by bus paths (`AudioUtils.BusPaths.*`)
- `PlaySound()` renamed to `TryPlaySound()`
- Use `CustomSoundHandler.RegisterCustomSound()` for all custom audio

**SMLHelper 2.15.0.1 and Nautilus are incompatible** — cannot run both.
