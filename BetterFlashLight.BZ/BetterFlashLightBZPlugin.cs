using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Nautilus.Handlers;
using SubnauticaMods.Shared;

namespace BetterFlashLight.BZ
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInDependency("com.snmodding.nautilus")]
    public class BetterFlashLightBZPlugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "com.zerotheabsolute.betterflashlight.bz";
        public const string PLUGIN_NAME = "BetterFlashLight BZ";
        public const string PLUGIN_VERSION = "1.0.0";

        internal static ManualLogSource Log;
        internal static Config ConfigInstance { get; private set; }

        private static readonly Assembly Assembly = Assembly.GetExecutingAssembly();

        private void Awake()
        {
            Log = Logger;
            ColorPickerPanel.LogWarning = msg => Log.LogWarning(msg);

            ConfigInstance = OptionsPanelHandler.RegisterModOptions<Config>();

            Harmony.CreateAndPatchAll(Assembly, PLUGIN_GUID);

            Log.LogInfo($"{PLUGIN_NAME} v{PLUGIN_VERSION} loaded.");
        }
    }
}
