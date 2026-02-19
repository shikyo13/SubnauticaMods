# CameraStalkerGuard Design

**Date:** 2026-02-19
**Status:** Approved

## Problem

Stalkers grab scanner room cameras (MapRoomCamera), dragging them away from the base. This is a well-known annoyance with no vanilla fix.

## Solution

A BepInEx 5 Harmony mod that patches `CollectShiny.IsTargetValid` to reject `MapRoomCamera` objects, preventing stalkers from ever targeting deployed cameras.

## How It Works

### Call Chain (vanilla)

1. `CollectShiny.UpdateShinyTarget()` — finds nearby `EcoTargetType.Shiny` objects
2. `CollectShiny.IsTargetValid(IEcoTarget)` — validates target (distance < 64)
3. `CollectShiny.TryPickupShinyTarget()` — grabs the object
4. `Stalker.OnShinyPickUp(object)` — callback, triggers tooth loss check

`MapRoomCamera` has a `shinyTarget` field of type `EcoTarget`, so it registers as a shiny object and enters the stalker's target pool.

### Patch Point

`CollectShiny.IsTargetValid(IEcoTarget target)` — a Harmony Prefix checks if the target's GameObject has a `MapRoomCamera` component. If yes, returns `false` (invalid target). The stalker never pursues the camera.

This is the earliest filter point. The stalker doesn't even start swimming toward the camera.

### Scope

- Only affects `CollectShiny` behavior (stalker shiny collection AI)
- Does not affect player pickup, camera recall, or any other camera functionality
- Does not affect stalker interaction with other shiny objects (metal salvage, etc.)

## Mod Structure

```
CameraStalkerGuard/
├── CameraStalkerGuard.sln
├── CameraStalkerGuard.csproj
├── CameraStalkerGuardPlugin.cs
├── Properties/
│   └── AssemblyInfo.cs
└── README.md
```

- **GUID:** `com.adam.camerastalkerguard`
- **Version:** 1.0.0
- **Config:** None — install and it works
- **Target:** .NET Framework 4.7.2
- **Dependencies:** BepInEx 5.4.x, HarmonyLib

## Patch Implementation

```csharp
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
            return false; // skip original
        }
        return true; // run original for everything else
    }
}
```

## Research Notes

Key types discovered via .NET assembly inspection of `Assembly-CSharp.dll`:

- `CollectShiny` — creature AI behavior with 20 methods including `IsTargetValid`, `UpdateShinyTarget`, `TryPickupShinyTarget`
- `CollectShiny.shinyTarget` (private `GameObject`) — current target being pursued
- `CollectShiny.isTargetValidFilter` (private `TargetFilter`) — existing filter delegate
- `MapRoomCamera.shinyTarget` (public `EcoTarget`) — registers camera as shiny
- `MapRoomCamera.pickupAble` (public `Pickupable`) — player pickup component
- `Stalker.OnShinyPickUp(object)` — 111 IL bytes, handles tooth loss logic
- `Stalker.OnShinyPickedUp(GameObject)` — cleanup callback
