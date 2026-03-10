using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Nautilus.Handlers;

namespace DockLightsToggle
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInDependency("com.snmodding.nautilus")]
    public class DockLightsTogglePlugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "com.zerotheabsolute.docklightstoggle";
        public const string PLUGIN_NAME = "DockLightsToggle";
        public const string PLUGIN_VERSION = "1.0.0";

        internal static ManualLogSource Log;
        internal static Config ConfigInstance;

        public static bool seaTruckIsDocked;
        public static bool exoSuitIsDocked;

        private static Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            ConfigInstance = OptionsPanelHandler.RegisterModOptions<Config>();

            _harmony = new Harmony(PLUGIN_GUID);
            _harmony.PatchAll();

            Log.LogInfo($"{PLUGIN_NAME} v{PLUGIN_VERSION} loaded! Vehicle lights will turn off when docked.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
