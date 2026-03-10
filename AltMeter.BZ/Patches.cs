using HarmonyLib;
using System;
using UnityEngine;

namespace AltMeter.BZ
{
    [HarmonyPatch(typeof(uGUI_DepthCompass), "UpdateDepth")]
    internal static class uGUI_DepthCompass_UpdateDepth_Patch
    {
        public static bool Prefix(uGUI_DepthCompass __instance)
        {
            try
            {
                if (Player.main == null)
                    return true;

                int altitude = (int)Player.main.transform.position.y;
                var depth = Mathf.FloorToInt(Player.main.GetDepth());
                var mainAlt = Math.Sign(altitude);

                if (mainAlt >= 1 && depth == 0)
                {
                    // Above water - show altitude
                    Color altColor = AltMeterBZPlugin.ToggleAltTextColor
                        ? new Color(AltMeterBZPlugin.AltColorRed, AltMeterBZPlugin.AltColorGreen, AltMeterBZPlugin.AltColorBlue, 1f)
                        : new Color(1f, 1f, 1f, 1f);

                    __instance.submersibleDepthSuffix.color = altColor;
                    __instance.suffixText.color = altColor;
                    __instance.depthText.color = altColor;

                    string suffix = AltMeterBZPlugin.ToggleSymbol ? "m ^" : "m";
                    string altText = altitude.ToString();

                    __instance.submersibleDepthSuffix.text = suffix;
                    __instance.submersibleDepth.text = altText;
                    __instance.suffixText.text = suffix;
                    __instance.depthText.text = altText;
                }
                else
                {
                    // Underwater - apply depth color if enabled
                    __instance.submersibleDepthSuffix.text = "m";

                    Color depthColor = AltMeterBZPlugin.ToggleDepthTextColor
                        ? new Color(AltMeterBZPlugin.DepthTextColorRed, AltMeterBZPlugin.DepthTextColorGreen, AltMeterBZPlugin.DepthTextColorBlue, 1f)
                        : new Color(1f, 1f, 1f, 1f);

                    __instance.submersibleDepthSuffix.color = depthColor;
                    __instance.suffixText.color = depthColor;
                    __instance.depthText.color = depthColor;
                }
            }
            catch (Exception ex)
            {
                AltMeterBZPlugin.Log.LogError($"Error in UpdateDepth patch: {ex.Message}");
            }

            return true;
        }
    }
}
