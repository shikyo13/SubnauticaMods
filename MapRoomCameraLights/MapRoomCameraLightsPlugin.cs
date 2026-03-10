using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Nautilus.Handlers;

namespace MapRoomCameraLights
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInDependency("com.snmodding.nautilus")]
    public class MapRoomCameraLightsPlugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "com.zerotheabsolute.maproomcameralights";
        public const string PLUGIN_NAME = "MapRoomCameraLights";
        public const string PLUGIN_VERSION = "1.0.0";

        internal static ManualLogSource Log;
        internal static Config ConfigInstance;

        private static Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            ConfigInstance = OptionsPanelHandler.RegisterModOptions<Config>();

            _harmony = new Harmony(PLUGIN_GUID);
            _harmony.PatchAll();

            foreach (var method in _harmony.GetPatchedMethods())
            {
                Log.LogInfo($"  Patched: {method.DeclaringType?.Name}.{method.Name}");
            }

            Log.LogInfo($"{PLUGIN_NAME} v{PLUGIN_VERSION} loaded! Brightness: {ConfigInstance.MapIntensity} | Range: {ConfigInstance.MapRange} | Cone: {ConfigInstance.MapspotAngle}");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
