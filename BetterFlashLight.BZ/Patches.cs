using HarmonyLib;
using UnityEngine;

namespace BetterFlashLight.BZ
{
    [HarmonyPatch(typeof(ToggleLights), "Update")]
    internal static class ToggleLights_Update_Patch
    {
        public static bool Prefix(ToggleLights __instance)
        {
            if (__instance.lightsParent == null)
                return true;

            var config = BetterFlashLightBZPlugin.ConfigInstance;
            var lights = __instance.lightsParent.GetComponentsInChildren<Light>();

            if (lights.Length == 0)
                return true;

            var light = lights[0];

            if (config.ToggleFlashLightColor)
            {
                light.color = new Color(config.LightRed, config.LightGreen, config.LightBlue);
            }

            if (config.ToggleFlashLightOptions)
            {
                light.intensity = config.LightBrightness;
                light.range = config.LightRange;
            }

            return true;
        }
    }
}
