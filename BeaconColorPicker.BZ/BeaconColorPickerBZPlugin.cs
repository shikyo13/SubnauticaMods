using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SubnauticaMods.Shared;

namespace BeaconColorPicker.BZ
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class BeaconColorPickerBZPlugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "com.zerotheabsolute.beaconcolorpicker.bz";
        public const string PLUGIN_NAME = "BeaconColorPicker BZ";
        public const string PLUGIN_VERSION = "1.0.1";

        internal static ManualLogSource Log;

        private static Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            ColorPickerPanel.LogWarning = msg => Log.LogWarning(msg);

            CustomColorStore.Load();

            _harmony = new Harmony(PLUGIN_GUID);
            _harmony.PatchAll();

            // Log which patches were applied for diagnostics
            foreach (var method in _harmony.GetPatchedMethods())
            {
                Log.LogInfo($"  Patched: {method.DeclaringType?.Name}.{method.Name}");
            }

            Log.LogInfo($"{PLUGIN_NAME} v{PLUGIN_VERSION} loaded! Custom beacon colors enabled.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
