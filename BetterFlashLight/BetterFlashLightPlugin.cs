using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Nautilus.Handlers;
using SubnauticaMods.Shared;

namespace BetterFlashLight
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInDependency("com.snmodding.nautilus")]
    public class BetterFlashLightPlugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "com.zerotheabsolute.betterflashlight";
        public const string PLUGIN_NAME = "BetterFlashLight";
        public const string PLUGIN_VERSION = "1.0.0";

        internal static ManualLogSource Log;
        internal static Config ConfigInstance;

        private static Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            ColorPickerPanel.LogWarning = msg => Log.LogWarning(msg);

            ConfigInstance = OptionsPanelHandler.RegisterModOptions<Config>();

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PLUGIN_GUID);

            foreach (var method in _harmony.GetPatchedMethods())
            {
                Log.LogInfo($"  Patched: {method.DeclaringType?.Name}.{method.Name}");
            }

            Log.LogInfo($"{PLUGIN_NAME} v{PLUGIN_VERSION} loaded!");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
