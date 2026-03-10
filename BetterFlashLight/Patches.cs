using HarmonyLib;
using UnityEngine;

namespace BetterFlashLight.Patches
{
    [HarmonyPatch(typeof(FlashLight), nameof(FlashLight.onLightsToggled))]
    internal static class FlashLight_onLightsToggled_Patch
    {
        [HarmonyPrefix]
        static bool Prefix(FlashLight __instance)
        {
            try
            {
                if (__instance.toggleLights?.lightsParent == null)
                    return true;

                var cfg = BetterFlashLightPlugin.ConfigInstance;
                var lights = __instance.toggleLights.lightsParent.GetComponentsInChildren<Light>();

                foreach (var light in lights)
                {
                    if (cfg.ToggleColor)
                    {
                        light.color = new Color(cfg.Red, cfg.Green, cfg.Blue);
                        light.intensity = cfg.Intensity;
                        light.range = cfg.Range;
                    }
                    else
                    {
                        light.color = new Color(0.996f, 0.942f, 0.819f);
                        light.intensity = 1.0f;
                        light.range = 50f;
                    }
                }
            }
            catch (System.Exception ex)
            {
                BetterFlashLightPlugin.Log.LogError($"Error in FlashLight patch: {ex.Message}");
            }

            return true;
        }
    }
}
