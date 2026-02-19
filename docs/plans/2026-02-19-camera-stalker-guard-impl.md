# CameraStalkerGuard Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Create a BepInEx 5 mod that prevents stalkers from targeting scanner room cameras.

**Architecture:** Single Harmony prefix patch on `CollectShiny.IsTargetValid` that rejects `MapRoomCamera` targets. No config. See `docs/plans/2026-02-19-camera-stalker-guard-design.md` for full design.

**Tech Stack:** C# / .NET 4.7.2 / BepInEx 5 / HarmonyLib

---

### Task 1: Create project scaffolding

**Files:**
- Create: `CameraStalkerGuard/CameraStalkerGuard.csproj`
- Create: `CameraStalkerGuard/CameraStalkerGuard.sln`
- Create: `CameraStalkerGuard/Properties/AssemblyInfo.cs`

**Step 1: Create the .csproj**

Create `CameraStalkerGuard/CameraStalkerGuard.csproj` — same template as `PowerSaver/PowerSaver.csproj` but with CameraStalkerGuard names:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net472</TargetFramework>
    <AssemblyName>CameraStalkerGuard</AssemblyName>
    <RootNamespace>CameraStalkerGuard</RootNamespace>
    <LangVersion>latest</LangVersion>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
    <NoWarn>$(NoWarn);CS0436</NoWarn>
  </PropertyGroup>

  <PropertyGroup>
    <SubnauticaDir>D:\SteamLibrary\steamapps\common\Subnautica</SubnauticaDir>
    <BepInExDir>$(SubnauticaDir)\BepInEx</BepInExDir>
    <ManagedDir>$(SubnauticaDir)\Subnautica_Data\Managed</ManagedDir>
  </PropertyGroup>

  <ItemGroup>
    <Reference Include="BepInEx">
      <HintPath>$(BepInExDir)\core\BepInEx.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="0Harmony">
      <HintPath>$(BepInExDir)\core\0Harmony.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>

  <ItemGroup>
    <Reference Include="Assembly-CSharp">
      <HintPath>$(ManagedDir)\Assembly-CSharp.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="Assembly-CSharp-firstpass">
      <HintPath>$(ManagedDir)\Assembly-CSharp-firstpass.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>

  <ItemGroup>
    <Reference Include="UnityEngine">
      <HintPath>$(ManagedDir)\UnityEngine.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="UnityEngine.CoreModule">
      <HintPath>$(ManagedDir)\UnityEngine.CoreModule.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>

  <Target Name="CopyToPlugins" AfterTargets="Build">
    <Copy
      SourceFiles="$(OutputPath)$(AssemblyName).dll"
      DestinationFolder="$(BepInExDir)\plugins\CameraStalkerGuard"
      SkipUnchangedFiles="true" />
    <Message Text="Copied $(AssemblyName).dll to BepInEx plugins folder" Importance="High" />
  </Target>

</Project>
```

**Step 2: Create the .sln**

Create `CameraStalkerGuard/CameraStalkerGuard.sln`. Generate a new project GUID (use `uuidgen` or similar). Format matches `PowerSaver/PowerSaver.sln`.

```
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.5.2.0
MinimumVisualStudioVersion = 10.0.40219.1
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "CameraStalkerGuard", "CameraStalkerGuard.csproj", "{NEW-GUID-HERE}"
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{NEW-GUID-HERE}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{NEW-GUID-HERE}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{NEW-GUID-HERE}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{NEW-GUID-HERE}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
EndGlobal
```

**Step 3: Create AssemblyInfo.cs**

Create `CameraStalkerGuard/Properties/AssemblyInfo.cs`:

```csharp
using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("CameraStalkerGuard")]
[assembly: AssemblyDescription("Prevents stalkers from stealing scanner room cameras")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("CameraStalkerGuard")]
[assembly: AssemblyCopyright("Adam 2026")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
```

**Step 4: Commit**

```bash
git add CameraStalkerGuard/CameraStalkerGuard.csproj CameraStalkerGuard/CameraStalkerGuard.sln CameraStalkerGuard/Properties/AssemblyInfo.cs
git commit -m "feat(CameraStalkerGuard): add project scaffolding"
```

---

### Task 2: Implement the plugin and patch

**Files:**
- Create: `CameraStalkerGuard/CameraStalkerGuardPlugin.cs`

**Step 1: Write the plugin source**

Create `CameraStalkerGuard/CameraStalkerGuardPlugin.cs` with the plugin class and the single Harmony patch:

```csharp
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace CameraStalkerGuard
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class CameraStalkerGuardPlugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "com.adam.camerastalkerguard";
        public const string PLUGIN_NAME = "CameraStalkerGuard";
        public const string PLUGIN_VERSION = "1.0.0";

        internal static ManualLogSource Log;

        private static Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            _harmony = new Harmony(PLUGIN_GUID);
            _harmony.PatchAll();

            Log.LogInfo($"{PLUGIN_NAME} v{PLUGIN_VERSION} loaded! Scanner room cameras are now protected from stalkers.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }

    /// <summary>
    /// Prevents creatures with CollectShiny behavior (stalkers) from targeting
    /// scanner room cameras as shiny objects.
    /// </summary>
    [HarmonyPatch(typeof(CollectShiny), "IsTargetValid")]
    internal static class CollectShiny_IsTargetValid_Patch
    {
        [HarmonyPrefix]
        static bool Prefix(IEcoTarget target, ref bool __result)
        {
            GameObject go = target.GetGameObject();
            if (go != null && go.GetComponent<MapRoomCamera>() != null)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}
```

**Step 2: Commit**

```bash
git add CameraStalkerGuard/CameraStalkerGuardPlugin.cs
git commit -m "feat(CameraStalkerGuard): implement IsTargetValid patch"
```

---

### Task 3: Build and verify

**Step 1: Build**

```bash
dotnet build CameraStalkerGuard/CameraStalkerGuard.sln --configuration Release
```

Expected: Build succeeds with 0 errors. Output at `CameraStalkerGuard/bin/Release/net472/CameraStalkerGuard.dll`. Post-build copies to `BepInEx\plugins\CameraStalkerGuard\`.

**Step 2: Fix any build errors**

If `CollectShiny` or `IEcoTarget` are not found, check:
- `Assembly-CSharp.dll` reference is correct in .csproj
- The types may be in `Assembly-CSharp-firstpass.dll` instead

If `IEcoTarget.GetGameObject()` doesn't exist with that signature, use dnSpy/ILSpy to find the correct interface method name and update the patch.

**Step 3: Commit build fix if needed**

```bash
git add -u
git commit -m "fix(CameraStalkerGuard): fix build issues"
```

---

### Task 4: Add README

**Files:**
- Create: `CameraStalkerGuard/README.md`

**Step 1: Write README**

Create `CameraStalkerGuard/README.md`:

```markdown
# CameraStalkerGuard - Subnautica Scanner Room Camera Protection

A BepInEx 5 mod that prevents stalkers from stealing scanner room cameras.

## What It Does

Patches the creature AI's shiny object targeting system (`CollectShiny.IsTargetValid`) to exclude `MapRoomCamera` objects. Stalkers will no longer pursue, grab, or drag away your scanner room cameras.

- Cameras are completely invisible to stalker pickup AI
- Player can still pick up and recall cameras normally
- Stalkers still interact with metal salvage and other shiny objects
- No configuration needed — install and forget

## Building

### Prerequisites
- .NET SDK 6.0+ or Visual Studio 2022
- Subnautica with BepInEx 5 installed

### Steps

1. Verify `SubnauticaDir` in `CameraStalkerGuard.csproj` points to your Subnautica install.

2. Build:
   ```
   dotnet build --configuration Release
   ```

3. The build auto-copies `CameraStalkerGuard.dll` to `BepInEx\plugins\CameraStalkerGuard\`.

4. Launch Subnautica and check `BepInEx\LogOutput.log` for:
   ```
   [Info : CameraStalkerGuard] CameraStalkerGuard v1.0.0 loaded! Scanner room cameras are now protected from stalkers.
   ```

## Compatibility

- Subnautica (Steam, current build as of Feb 2026)
- BepInEx 5.4.x
- Should not conflict with other mods unless they also patch `CollectShiny.IsTargetValid`
```

**Step 2: Commit**

```bash
git add CameraStalkerGuard/README.md
git commit -m "docs(CameraStalkerGuard): add README"
```

---

### Task 5: Final push

**Step 1: Push to GitHub**

```bash
git push origin master
```
