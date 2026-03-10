using HarmonyLib;
using UnityEngine;

namespace DockLightsToggle
{
    /// <summary>
    /// Polls docking state by hijacking VehicleDockingBay.OnUndockingStart via InvokeRepeating.
    /// The prefix returns false to replace the original method — this is intentional, as it
    /// repurposes the invoke to act as a docking-state polling mechanism.
    /// </summary>
    [HarmonyPatch(typeof(VehicleDockingBay), "Start")]
    internal static class VehicleDockingBay_Start_Patch
    {
        [HarmonyPostfix]
        static void Postfix(VehicleDockingBay __instance)
        {
            if (__instance.subRoot is BaseRoot)
            {
                __instance.CancelInvoke("OnUndockingStart");
                __instance.InvokeRepeating("OnUndockingStart", 0f, 1f);
            }
        }
    }

    [HarmonyPatch(typeof(VehicleDockingBay), nameof(VehicleDockingBay.OnUndockingStart))]
    internal static class VehicleDockingBay_OnUndockingStart_Patch
    {
        [HarmonyPrefix]
        static bool Prefix(VehicleDockingBay __instance)
        {
            if (__instance == null)
                return false;

            Dockable docked = __instance.GetDockedObject();
            if (docked != null)
            {
                if (docked.name.Contains("SeaTruck"))
                    DockLightsTogglePlugin.seaTruckIsDocked = true;
                else if (docked.name.Contains("Exosuit"))
                    DockLightsTogglePlugin.exoSuitIsDocked = true;
            }
            else
            {
                DockLightsTogglePlugin.seaTruckIsDocked = false;
                DockLightsTogglePlugin.exoSuitIsDocked = false;
            }

            return false;
        }
    }

    /// <summary>
    /// Disables SeaTruck lights while docked. Restores them on undock if configured.
    /// </summary>
    [HarmonyPatch(typeof(SeaTruckLights), "Update")]
    internal static class SeaTruckLights_Update_Patch
    {
        [HarmonyPrefix]
        static bool Prefix(SeaTruckLights __instance)
        {
            if (DockLightsTogglePlugin.seaTruckIsDocked)
            {
                if (__instance.lightsActive)
                {
                    __instance.lightsActive = false;
                }
            }
            else
            {
                if (DockLightsTogglePlugin.ConfigInstance.RestoreSeaTruckLights && !__instance.lightsActive)
                {
                    __instance.lightsActive = true;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Disables Exosuit lights while docked by toggling the Light components.
    /// </summary>
    [HarmonyPatch(typeof(Exosuit), "Update")]
    internal static class Exosuit_Update_Patch
    {
        [HarmonyPrefix]
        static bool Prefix(Exosuit __instance)
        {
            var lightsParent = __instance.transform.Find("lights_parent");
            if (lightsParent == null)
                return true;

            var exosuitLights = lightsParent.GetComponentsInChildren<Light>();
            for (int i = 0; i < exosuitLights.Length; i++)
            {
                exosuitLights[i].enabled = !DockLightsTogglePlugin.exoSuitIsDocked;
            }

            return true;
        }
    }
}
