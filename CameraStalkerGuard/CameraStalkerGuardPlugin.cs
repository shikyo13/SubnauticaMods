using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace CameraStalkerGuard
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class CameraStalkerGuardPlugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "com.adam.camerastalkerguard";
        public const string PLUGIN_NAME = "CameraStalkerGuard";
        public const string PLUGIN_VERSION = "1.0.0";

        internal static ManualLogSource Log;

        private static Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            _harmony = new Harmony(PLUGIN_GUID);
            _harmony.PatchAll();

            Log.LogInfo($"{PLUGIN_NAME} v{PLUGIN_VERSION} loaded! Scanner room cameras are now protected from stalkers.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }

    /// <summary>
    /// Prevents creatures with CollectShiny behavior (stalkers) from targeting
    /// scanner room cameras as shiny objects.
    /// </summary>
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
                return false;
            }
            return true;
        }
    }
}
