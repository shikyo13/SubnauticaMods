using HarmonyLib;
using UnityEngine;

namespace MapRoomCameraLights
{
    [HarmonyPatch(typeof(MapRoomCamera), "Update")]
    internal static class MapRoomCamera_Update_Patch
    {
        [HarmonyPostfix]
        static void Postfix(MapRoomCamera __instance)
        {
            var config = MapRoomCameraLightsPlugin.ConfigInstance;
            if (config == null)
                return;

            var mapLights = __instance.lightsParent.GetComponentsInChildren<Light>();
            if (mapLights == null)
                return;

            for (int i = 0; i < mapLights.Length; i++)
            {
                mapLights[i].spotAngle = config.MapspotAngle;
                mapLights[i].intensity = config.MapIntensity;
                mapLights[i].range = config.MapRange;
            }
        }
    }
}
