using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace AltMeter
{
    [BepInPlugin(MyGuid, PluginName, VersionString)]
    public class AltMeterPlugin : BaseUnityPlugin
    {
        private const string MyGuid = "com.zerotheabsolute.altmeter";
        private const string PluginName = "AltMeter";
        private const string VersionString = "1.0.0";

        private void Awake()
        {
            new Harmony(MyGuid).PatchAll(Assembly.GetExecutingAssembly());
            Logger.LogInfo($"{PluginName} {VersionString} loaded.");
        }
    }

    [HarmonyPatch(typeof(uGUI_DepthCompass))]
    [HarmonyPatch("UpdateDepth")]
    internal static class uGUI_DepthCompass_UpdateDepth_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(uGUI_DepthCompass __instance)
        {
            if (Player.main != null)
            {
                int altitude = (int)Player.main.transform.position.y;
                var depth = Mathf.FloorToInt(Player.main.GetDepth());
                if (altitude != 0 && depth == 0)
                {
                    __instance.depthText.text = altitude.ToString();
                    __instance.suffixText.text = "m\u2191";
                }
            }
            return true;
        }
    }
}
