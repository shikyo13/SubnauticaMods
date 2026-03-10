using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Nautilus.Handlers;

namespace AltMeter.BZ
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInDependency("com.snmodding.nautilus")]
    public class AltMeterBZPlugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "com.zerotheabsolute.altmeter.bz";
        public const string PLUGIN_NAME = "AltMeter BZ";
        public const string PLUGIN_VERSION = "1.0.0";

        internal static ManualLogSource Log;
        internal static Config ModConfig;

        // Config-driven static fields read by patches
        public static bool ToggleSymbol;
        public static bool ToggleDepthTextColor;
        public static bool ToggleAltTextColor;
        public static float DepthTextColorRed, DepthTextColorGreen, DepthTextColorBlue;
        public static float AltColorRed, AltColorGreen, AltColorBlue;

        private static Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            ModConfig = OptionsPanelHandler.RegisterModOptions<Config>();

            // Initialize static fields from config defaults
            ToggleSymbol = ModConfig.ToggleAltSymbol;
            ToggleDepthTextColor = ModConfig.ToggleDepthTextColor;
            ToggleAltTextColor = ModConfig.ToggleAltTextColor;
            DepthTextColorRed = ModConfig.DepthTextRed;
            DepthTextColorGreen = ModConfig.DepthTextGreen;
            DepthTextColorBlue = ModConfig.DepthTextBlue;
            AltColorRed = ModConfig.AltTextRed;
            AltColorGreen = ModConfig.AltTextGreen;
            AltColorBlue = ModConfig.AltTextBlue;

            _harmony = new Harmony(PLUGIN_GUID);
            _harmony.PatchAll();

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
