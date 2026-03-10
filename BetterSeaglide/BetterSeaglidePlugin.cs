using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Nautilus.Handlers;
using SubnauticaMods.Shared;

namespace BetterSeaglide
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInDependency("com.snmodding.nautilus")]
    public class BetterSeaglidePlugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "com.zerotheabsolute.betterseaglide";
        public const string PLUGIN_NAME = "BetterSeaglide";
        public const string PLUGIN_VERSION = "1.0.0";

        internal static ManualLogSource Log;
        internal new static BetterSeaglideConfig Config;

        private static Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            ColorPickerPanel.LogWarning = msg => Log.LogWarning(msg);

            Config = OptionsPanelHandler.RegisterModOptions<BetterSeaglideConfig>();

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
