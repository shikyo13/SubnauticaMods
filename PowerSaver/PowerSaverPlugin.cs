using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace PowerSaver
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class PowerSaverPlugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "com.adam.powersaver";
        public const string PLUGIN_NAME = "PowerSaver";
        public const string PLUGIN_VERSION = "1.0.0";

        internal static ManualLogSource Log;
        internal static ConfigEntry<float> DrainMultiplier;
        internal static ConfigEntry<float> VehicleDrainMultiplier;
        internal static ConfigEntry<float> BaseDrainMultiplier;
        internal static ConfigEntry<bool> EnableLogging;

        private static Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            // Config entries - these show up in BepInEx config file
            DrainMultiplier = Config.Bind(
                "General",
                "DrainMultiplier",
                0.75f,
                new ConfigDescription(
                    "Global power drain multiplier. 0.75 = 25% less drain, 0.5 = 50% less drain, 1.0 = vanilla.",
                    new AcceptableValueRange<float>(0.01f, 2.0f)
                )
            );

            VehicleDrainMultiplier = Config.Bind(
                "Vehicles",
                "VehicleDrainMultiplier",
                0.75f,
                new ConfigDescription(
                    "Vehicle-specific power drain multiplier (Seamoth, Prawn, Cyclops engines). Stacks with global multiplier if both < 1.0. Set to 1.0 to only use global.",
                    new AcceptableValueRange<float>(0.01f, 2.0f)
                )
            );

            BaseDrainMultiplier = Config.Bind(
                "Base",
                "BaseDrainMultiplier",
                0.75f,
                new ConfigDescription(
                    "Base power drain multiplier for habitat power relays. Set to 1.0 to only use global.",
                    new AcceptableValueRange<float>(0.01f, 2.0f)
                )
            );

            EnableLogging = Config.Bind(
                "Debug",
                "EnableLogging",
                false,
                "Log power drain events to console (noisy, for debugging only)."
            );

            _harmony = new Harmony(PLUGIN_GUID);
            _harmony.PatchAll();

            Log.LogInfo($"{PLUGIN_NAME} v{PLUGIN_VERSION} loaded! Global drain: {DrainMultiplier.Value}x | Vehicles: {VehicleDrainMultiplier.Value}x | Base: {BaseDrainMultiplier.Value}x");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }

    /// <summary>
    /// Core patch: intercepts ALL battery/power cell drain in the game.
    /// EnergyMixin.ConsumeEnergy is called by tools, vehicles, and equipment.
    /// </summary>
    [HarmonyPatch(typeof(EnergyMixin), nameof(EnergyMixin.ConsumeEnergy))]
    internal static class EnergyMixin_ConsumeEnergy_Patch
    {
        [HarmonyPrefix]
        static void Prefix(ref float amount)
        {
            float multiplier = PowerSaverPlugin.DrainMultiplier.Value;
            amount *= multiplier;

            if (PowerSaverPlugin.EnableLogging.Value)
                PowerSaverPlugin.Log.LogDebug($"[EnergyMixin] Drain adjusted by {multiplier}x");
        }
    }

    /// <summary>
    /// Vehicle-specific patch: catches Seamoth, Prawn Suit, and Cyclops
    /// engine power consumption that goes through the vehicle energy interface.
    /// Uses explicit argument types to resolve overload ambiguity.
    /// </summary>
    [HarmonyPatch]
    internal static class Vehicle_ConsumeEnergy_Patch
    {
        // Disambiguate overloads by specifying the parameter types
        [HarmonyTargetMethod]
        static System.Reflection.MethodBase TargetMethod()
        {
            // Target the single-float overload: Vehicle.ConsumeEnergy(float amount)
            var method = AccessTools.Method(typeof(Vehicle), "ConsumeEnergy", new[] { typeof(float) });
            if (method != null)
            {
                PowerSaverPlugin.Log.LogDebug("[PowerSaver] Found Vehicle.ConsumeEnergy(float)");
                return method;
            }

            // Fallback: try two-param version if single doesn't exist
            method = AccessTools.Method(typeof(Vehicle), "ConsumeEnergy", new[] { typeof(float), typeof(float).MakeByRefType() });
            if (method != null)
            {
                PowerSaverPlugin.Log.LogDebug("[PowerSaver] Found Vehicle.ConsumeEnergy(float, out float)");
                return method;
            }

            PowerSaverPlugin.Log.LogWarning("[PowerSaver] Could not find Vehicle.ConsumeEnergy - vehicle patch disabled!");
            return null;
        }

        [HarmonyPrefix]
        static void Prefix(ref float amount)
        {
            float multiplier = PowerSaverPlugin.VehicleDrainMultiplier.Value;
            amount *= multiplier;

            if (PowerSaverPlugin.EnableLogging.Value)
                PowerSaverPlugin.Log.LogDebug($"[Vehicle] Drain adjusted by {multiplier}x");
        }
    }

    /// <summary>
    /// Base power relay patch: covers habitat power consumption from things
    /// like water filtration machines, fabricators, scanners, etc.
    /// </summary>
    [HarmonyPatch(typeof(PowerRelay), "ConsumeEnergy")]
    internal static class PowerRelay_ConsumeEnergy_Patch
    {
        [HarmonyPrefix]
        static void Prefix(ref float amount, ref bool __state)
        {
            float multiplier = PowerSaverPlugin.BaseDrainMultiplier.Value;
            amount *= multiplier;

            if (PowerSaverPlugin.EnableLogging.Value)
                PowerSaverPlugin.Log.LogDebug($"[PowerRelay] Drain adjusted by {multiplier}x");
        }
    }
}
