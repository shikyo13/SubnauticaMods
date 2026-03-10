using HarmonyLib;
using UnityEngine;

namespace BetterSeaglide.Patches
{
    /// <summary>
    /// Applies custom light, body, and energy bar colors to the seaglide.
    /// Component lookups are cached per-instance to avoid per-frame allocations.
    /// </summary>
    [HarmonyPatch(typeof(Seaglide), "Update")]
    internal static class SeaglideVisualsPatch
    {
        private static Seaglide _cachedInstance;
        private static Light[] _cachedLights;
        private static SkinnedMeshRenderer[] _cachedSkinned;
        private static MeshRenderer[] _cachedMesh;
        private static VehicleInterface_EnergyBar _cachedEnergyBar;

        [HarmonyPostfix]
        static void Postfix(Seaglide __instance)
        {
            try
            {
                var config = BetterSeaglidePlugin.Config;
                if (!config.ToggleLightColor && !config.ToggleBodyColor && !config.ToggleEnergyBarColor)
                    return;

                CacheComponents(__instance);

                if (config.ToggleLightColor)
                    ApplyLightColor(config.LightColor);

                if (config.ToggleBodyColor)
                    ApplyBodyColor(config.BodyColor);

                if (config.ToggleEnergyBarColor)
                    ApplyEnergyBarColor(config.EnergyBarColor);
            }
            catch { }
        }

        private static void CacheComponents(Seaglide sg)
        {
            if (_cachedInstance == sg) return;
            _cachedInstance = sg;
            _cachedLights = sg.toggleLights?.lightsParent?.GetComponentsInChildren<Light>();
            _cachedSkinned = sg.GetComponentsInChildren<SkinnedMeshRenderer>();
            _cachedMesh = sg.GetComponentsInChildren<MeshRenderer>();
            _cachedEnergyBar = sg.GetComponentInChildren<VehicleInterface_EnergyBar>();
        }

        private static void ApplyLightColor(Color color)
        {
            if (_cachedLights == null) return;
            for (int i = 0; i < _cachedLights.Length; i++)
            {
                if (_cachedLights[i] != null && _cachedLights[i].gameObject.name.Contains("light_"))
                    _cachedLights[i].color = color;
            }
        }

        private static void ApplyBodyColor(Color color)
        {
            if (_cachedSkinned != null)
            {
                for (int i = 0; i < _cachedSkinned.Length; i++)
                {
                    if (_cachedSkinned[i] != null && _cachedSkinned[i].name.Contains("SeaGlide_geo"))
                        _cachedSkinned[i].material.color = color;
                }
            }

            if (_cachedMesh != null)
            {
                for (int i = 0; i < _cachedMesh.Length; i++)
                {
                    if (_cachedMesh[i] != null && _cachedMesh[i].name.Contains("SeaGlide_01_TP"))
                        _cachedMesh[i].material.color = color;
                }
            }
        }

        private static void ApplyEnergyBarColor(Color color)
        {
            if (_cachedEnergyBar != null && _cachedEnergyBar.energyBarMat != null)
                _cachedEnergyBar.energyBarMat.color = color;
        }
    }
}
