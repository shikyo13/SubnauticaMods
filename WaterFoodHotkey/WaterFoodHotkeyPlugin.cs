using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Nautilus.Handlers;

namespace WaterFoodHotkey
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInDependency("com.snmodding.nautilus")]
    public class WaterFoodHotkeyPlugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "com.zerotheabsolute.waterfoodhotkey";
        public const string PLUGIN_NAME = "WaterFoodHotkey";
        public const string PLUGIN_VERSION = "1.0.0";

        internal static ManualLogSource Log;
        internal static Config ConfigInstance;

        internal static GameInput.Button WaterButton;
        internal static GameInput.Button FoodButton;

        private static Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            // Register custom GameInput buttons (replaces legacy Input.GetKeyDown)
            WaterButton = EnumHandler.AddEntry<GameInput.Button>("WFH_Water")
                .CreateInput("Drink Water")
                .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.K)
                .WithCategory("Water/Food Hotkeys");

            FoodButton = EnumHandler.AddEntry<GameInput.Button>("WFH_Food")
                .CreateInput("Eat Food")
                .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.L)
                .WithCategory("Water/Food Hotkeys");

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
