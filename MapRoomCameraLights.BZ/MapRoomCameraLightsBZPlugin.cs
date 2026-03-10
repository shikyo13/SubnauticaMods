using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Nautilus.Handlers;

namespace MapRoomCameraLights.BZ
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInDependency("com.snmodding.nautilus")]
    public class MapRoomCameraLightsBZPlugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "com.zerotheabsolute.maproomcameralights.bz";
        public const string PLUGIN_NAME = "MapRoomCameraLights BZ";
        public const string PLUGIN_VERSION = "1.0.0";

        internal static ManualLogSource Log;
        internal static Config ConfigInstance { get; private set; }

        private static Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            ConfigInstance = OptionsPanelHandler.RegisterModOptions<Config>();

            _harmony = new Harmony(PLUGIN_GUID);
            _harmony.PatchAll();

            Log.LogInfo($"{PLUGIN_NAME} v{PLUGIN_VERSION} loaded! Scanner room camera lights are now configurable.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
