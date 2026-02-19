using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace BeaconColorPicker
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class BeaconColorPickerPlugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "com.adam.beaconcolorpicker";
        public const string PLUGIN_NAME = "BeaconColorPicker";
        public const string PLUGIN_VERSION = "1.0.0";

        internal static ManualLogSource Log;

        private static Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            CustomColorStore.Load();

            _harmony = new Harmony(PLUGIN_GUID);
            _harmony.PatchAll();

            Log.LogInfo($"{PLUGIN_NAME} v{PLUGIN_VERSION} loaded! Custom beacon colors enabled.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
