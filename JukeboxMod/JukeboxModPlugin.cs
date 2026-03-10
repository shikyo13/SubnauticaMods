using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace JukeboxMod
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class JukeboxModPlugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "com.zerotheabsolute.jukeboxmod";
        public const string PLUGIN_NAME = "JukeboxMod";
        public const string PLUGIN_VERSION = "1.0.0";

        internal static ManualLogSource Log;

        public static bool isPlaying;
        public static bool isPaused;

        private static Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            _harmony = new Harmony(PLUGIN_GUID);
            _harmony.PatchAll();

            Log.LogInfo($"{PLUGIN_NAME} v{PLUGIN_VERSION} loaded! Jukebox colors and party mode enabled.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
