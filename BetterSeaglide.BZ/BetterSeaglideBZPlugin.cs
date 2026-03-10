using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Nautilus.Handlers;
using SubnauticaMods.Shared;

namespace BetterSeaglide.BZ
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInDependency("com.snmodding.nautilus")]
    public class BetterSeaglideBZPlugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "com.zerotheabsolute.betterseaglide.bz";
        public const string PLUGIN_NAME = "BetterSeaglide BZ";
        public const string PLUGIN_VERSION = "1.0.0";

        internal static ManualLogSource Log;
        internal new static BetterSeaglideBZConfig Config;

        private static Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            ColorPickerPanel.LogWarning = msg => Log.LogWarning(msg);

            Config = OptionsPanelHandler.RegisterModOptions<BetterSeaglideBZConfig>();

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
