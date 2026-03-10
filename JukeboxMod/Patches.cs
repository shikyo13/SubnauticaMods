using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace JukeboxMod
{
    /// <summary>
    /// Tracks play/pause state for party mode logic.
    /// </summary>
    [HarmonyPatch(typeof(JukeboxInstance), nameof(JukeboxInstance.OnButtonPlayPause))]
    internal static class JukeboxInstance_OnButtonPlayPause_Patch
    {
        [HarmonyPostfix]
        static void Postfix()
        {
            if (Jukebox.paused)
            {
                JukeboxModPlugin.isPlaying = false;
                JukeboxModPlugin.isPaused = true;
            }
            else
            {
                JukeboxModPlugin.isPlaying = true;
                JukeboxModPlugin.isPaused = false;
            }
        }
    }

    /// <summary>
    /// Tracks stop state for party mode logic.
    /// </summary>
    [HarmonyPatch(typeof(JukeboxInstance), nameof(JukeboxInstance.OnButtonStop))]
    internal static class JukeboxInstance_OnButtonStop_Patch
    {
        [HarmonyPostfix]
        static void Postfix()
        {
            JukeboxModPlugin.isPlaying = false;
            JukeboxModPlugin.isPaused = false;
        }
    }

    /// <summary>
    /// Replaces UpdateEffects to apply custom jukebox colors and party mode lighting.
    /// This prefix returns false because we need full control over the color calculations
    /// and the room lighting pulse effect.
    /// </summary>
    [HarmonyPatch(typeof(JukeboxInstance), nameof(JukeboxInstance.UpdateEffects))]
    internal static class JukeboxInstance_UpdateEffects_Patch
    {
        private static readonly int _ColorID = Shader.PropertyToID("_Color");
        private static readonly int _GlowColorID = Shader.PropertyToID("_GlowColor");

        // Material name substrings to exclude from party mode lighting
        private static readonly string[] ExcludedMaterials = new string[]
        {
            "window", "glass", "WaterPlaneBaseCorridor", "WaterRunOnWall",
            "WaterPlaneBaseRoomObs", "x_BaseWaterFog_BaseRoom",
            "x_BaseWaterFog_RoomCorridorConnector", "Juke", "water"
        };

        public static BaseCellLighting light;

        [HarmonyPrefix]
        static bool Prefix(JukeboxInstance __instance)
        {
            if (__instance._materials == null) return false;

            float num = __instance._flashValue;
            float num2 = __instance._highValue;

            if (__instance.isControlling)
            {
                float num3 = 0f;
                float num4 = 0f;
                List<float> spectrum = Jukebox.spectrum;

                if (spectrum != null && spectrum.Count > 0)
                {
                    int num5 = Mathf.Min(2, spectrum.Count);
                    for (int i = 0; i < num5; i++)
                        num3 = Mathf.Max(num3, spectrum[i]);
                    for (int j = num5; j < spectrum.Count; j++)
                        num4 = Mathf.Max(num4, spectrum[j]);
                }

                num = num < num3
                    ? num3
                    : Mathf.SmoothDamp(num, num3, ref __instance._flashVelocity, 0.2f, float.PositiveInfinity, Time.deltaTime);
                num2 = num2 < num4
                    ? num4
                    : Mathf.SmoothDamp(num2, num4, ref __instance._highVelocity, 0.05f, float.PositiveInfinity, Time.deltaTime);

                num = Mathf.Clamp(num, 0.2f, 1f);
                num2 = Mathf.Clamp01(num2 * 1.75f) * 0.39999998f + 0.6f;
            }
            else if (num != 0f)
            {
                num = 0f;
                __instance._flashVelocity = 0f;
            }
            else if (num2 != 0f)
            {
                num2 = 0f;
                __instance._highVelocity = 0f;
            }

            // Update high-frequency color (material _Color)
            if (__instance._highValue != num2)
            {
                __instance._highValue = num2;
                Color value = JukeboxConfig.JBColor
                    ? Color.Lerp(JukeboxConfig.FlashColor0, JukeboxConfig.FlashColor2, __instance._highValue)
                    : Color.Lerp(__instance.flashColor0, __instance.flashColor2, __instance._highValue);
                __instance._materials[1].SetColor(_ColorID, value);
            }

            // Update flash/beat color (material _GlowColor)
            if (__instance._flashValue != num)
            {
                Color value2 = JukeboxConfig.JBColor
                    ? Color.Lerp(JukeboxConfig.FlashColor0, JukeboxConfig.FlashColor1, __instance._flashValue)
                    : Color.Lerp(__instance.flashColor0, __instance.flashColor1, __instance._flashValue);
                __instance._flashValue = num;
                __instance._materials[1].SetColor(_GlowColorID, value2);
            }

            // Party mode: pulse room lighting with music
            if (JukeboxConfig.PartyMode)
            {
                try
                {
                    Base baseComp = __instance._baseComp;
                    if (baseComp == null) return false;

                    light = baseComp.GetCellLightingFor(__instance.transform.position);
                    if (light == null) return false;

                    HashSet<Renderer> interiorRenderers = light.interior;
                    if (interiorRenderers == null) return false;

                    Color partyColor = JukeboxConfig.JBColor
                        ? Color.Lerp(JukeboxConfig.FlashColor0, JukeboxConfig.FlashColor1, num)
                        : Color.Lerp(Color.black, new Color(1f, 0f, 0.7f, 1f), num);

                    if (JukeboxModPlugin.isPlaying && !JukeboxModPlugin.isPaused)
                    {
                        foreach (Renderer renderer in interiorRenderers)
                        {
                            if (renderer == null) continue;

                            Material[] mats = renderer.materials;
                            for (int m = 0; m < mats.Length; m++)
                            {
                                if (mats[m] == null) continue;

                                string matName = mats[m].name;
                                if (ShouldExcludeMaterial(matName)) continue;

                                mats[m].SetColor(_ColorID, partyColor);
                            }
                        }
                    }
                }
                catch
                {
                    // Silently handle any errors to prevent game crashes
                }
            }

            return false;
        }

        private static bool ShouldExcludeMaterial(string materialName)
        {
            if (string.IsNullOrEmpty(materialName)) return true;

            for (int i = 0; i < ExcludedMaterials.Length; i++)
            {
                if (materialName.IndexOf(ExcludedMaterials[i], System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }
    }
}
