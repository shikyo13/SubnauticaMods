using HarmonyLib;
using UnityEngine;

namespace MapRoomCameraLights.BZ
{
    [HarmonyPatch(typeof(MapRoomCamera), "Update")]
    internal static class MapRoomCamera_Update_Patch
    {
        [HarmonyPrefix]
        static bool Prefix(MapRoomCamera __instance)
        {
            var config = MapRoomCameraLightsBZPlugin.ConfigInstance;
            if (config == null)
                return true;

            if (!config.ToggleScannerRoomCameraLights)
                return true;

            var mapLights = __instance.lightsParent.GetComponentsInChildren<Light>();
            if (mapLights != null)
            {
                foreach (var light in mapLights)
                {
                    light.spotAngle = config.MapspotAngle;
                    light.intensity = config.MapIntensity;
                    light.range = config.MapRange;
                }
            }

            return true;
        }
    }
}
