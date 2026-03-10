using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Nautilus.Handlers;

namespace WaterFoodHotkey.BZ
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInDependency("com.snmodding.nautilus")]
    public class WaterFoodHotkeyBZPlugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "com.zerotheabsolute.waterfoodhotkey.bz";
        public const string PLUGIN_NAME = "WaterFoodHotkey BZ";
        public const string PLUGIN_VERSION = "1.0.0";

        internal static ManualLogSource Log;
        internal static WaterFoodHotkeyConfig Options;

        /// <summary>Custom GameInput button for food/drink hotkey.</summary>
        public static GameInput.Button FoodDrinkButton;

        /// <summary>Custom GameInput button for med kit hotkey.</summary>
        public static GameInput.Button MedButton;

        /// <summary>Custom GameInput button for heat/thermos hotkey.</summary>
        public static GameInput.Button HeatButton;

        private static Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            // Register custom GameInput buttons BEFORE Harmony patches.
            // Players rebind these in Options > Keyboard > Mod Keybindings.
            FoodDrinkButton = EnumHandler.AddEntry<GameInput.Button>("WFH_FoodDrink");
            MedButton = EnumHandler.AddEntry<GameInput.Button>("WFH_Med");
            HeatButton = EnumHandler.AddEntry<GameInput.Button>("WFH_Heat");

            Options = OptionsPanelHandler.RegisterModOptions<WaterFoodHotkeyConfig>();

            _harmony = new Harmony(PLUGIN_GUID);
            _harmony.PatchAll(Assembly.GetExecutingAssembly());

            Log.LogInfo($"{PLUGIN_NAME} v{PLUGIN_VERSION} loaded!");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
