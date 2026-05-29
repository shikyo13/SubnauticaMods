using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Nautilus.Handlers;
using System.IO;
using UnityEngine;

namespace PowerSaver.BZ
{
    /// <summary>
    /// Accumulates energy drain stats and logs a periodic summary every 30 seconds.
    /// Replaces noisy per-frame logging with one actionable line showing actual savings.
    /// </summary>
    internal static class PowerSaverDiagnostics
    {
        private const float LogInterval = 30f;

        private static double _toolsOriginal, _toolsAdjusted;
        private static double _vehicleOriginal, _vehicleAdjusted;
        private static double _baseOriginal, _baseAdjusted;
        private static float _nextLogTime;

        internal static void RecordEnergyMixin(float original, float adjusted)
        {
            _toolsOriginal += original;
            _toolsAdjusted += adjusted;
            TryLogSummary();
        }

        internal static void RecordVehicle(float original, float adjusted)
        {
            _vehicleOriginal += original;
            _vehicleAdjusted += adjusted;
            TryLogSummary();
        }

        internal static void RecordBase(float original, float adjusted)
        {
            _baseOriginal += original;
            _baseAdjusted += adjusted;
            TryLogSummary();
        }

        private static void TryLogSummary()
        {
            float now = Time.time;
            if (now < _nextLogTime) return;
            _nextLogTime = now + LogInterval;

            double totalOriginal = _toolsOriginal + _vehicleOriginal + _baseOriginal;
            double totalAdjusted = _toolsAdjusted + _vehicleAdjusted + _baseAdjusted;
            double saved = totalOriginal - totalAdjusted;

            if (totalOriginal < 0.001) return;

            double pct = saved / totalOriginal * 100.0;

            PowerSaverBZPlugin.Log.LogInfo(
                $"[PowerSaver] {LogInterval}s summary - Total: {(float)totalOriginal:F2} -> {(float)totalAdjusted:F2} " +
                $"(saved {(float)saved:F2}, {pct:F0}%) | Tools: {(float)_toolsAdjusted:F2} | Vehicles: {(float)_vehicleAdjusted:F2} | Base: {(float)_baseAdjusted:F2}");

            _toolsOriginal = _toolsAdjusted = 0;
            _vehicleOriginal = _vehicleAdjusted = 0;
            _baseOriginal = _baseAdjusted = 0;
        }
    }

    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInDependency("com.snmodding.nautilus")]
    [BepInDependency("snbz.easycraft.mod", BepInDependency.DependencyFlags.SoftDependency)]
    public class PowerSaverBZPlugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "com.zerotheabsolute.powersaver.bz";
        public const string PLUGIN_NAME = "PowerSaver BZ";
        public const string PLUGIN_VERSION = "1.0.3";

        internal static ManualLogSource Log;
        internal static PowerSaverConfig Options;

        private static Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            Options = OptionsPanelHandler.RegisterModOptions<PowerSaverConfig>();
            MigrateConfigDefaults();

            _harmony = new Harmony(PLUGIN_GUID);
            _harmony.PatchAll();

            Log.LogInfo(
                $"{PLUGIN_NAME} v{PLUGIN_VERSION} loaded! Global drain: {Options.DrainMultiplier}x | " +
                $"Effective vehicles: {PowerDrainMath.GetVehicleMultiplier(Options.DrainMultiplier, Options.VehicleDrainMultiplier)}x | " +
                $"Effective base: {PowerDrainMath.GetBaseMultiplier(Options.DrainMultiplier, Options.BaseDrainMultiplier)}x");
        }

        private void Start()
        {
            WarnAboutLegacyConfig();
            ChargerCompatibility.Apply(_harmony);
            EasyCraftCompatibility.TryApply(_harmony);
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }

        private static void WarnAboutLegacyConfig()
        {
            string legacyConfigPath = Path.Combine(Paths.ConfigPath, "com.zerotheabsolute.powersaver.bz.cfg");
            string activeConfigPath = Path.Combine(Paths.ConfigPath, "PowerSaver.BZ", "config.json");
            if (File.Exists(legacyConfigPath))
            {
                Log.LogWarning(
                    $"Legacy PowerSaver BZ config found at {legacyConfigPath}. Active Nautilus settings are stored at {activeConfigPath}; the legacy file is not used.");
            }
        }

        private static void MigrateConfigDefaults()
        {
            if (PowerSaverConfigMigration.ShouldMigrateOnePointOneDefaults(
                Options.ConfigVersion,
                Options.DrainMultiplier,
                Options.VehicleDrainMultiplier,
                Options.BaseDrainMultiplier))
            {
                Options.VehicleDrainMultiplier = 1.0f;
                Options.BaseDrainMultiplier = 1.0f;
                Options.ConfigVersion = PowerSaverConfigMigration.CurrentConfigVersion;
                Options.Save();
                Log.LogInfo("[PowerSaver BZ] Migrated 1.0.1 generated category defaults to 1.0 so effective default drain remains 75 percent.");
                return;
            }

            if (Options.ConfigVersion != PowerSaverConfigMigration.CurrentConfigVersion)
            {
                Options.ConfigVersion = PowerSaverConfigMigration.CurrentConfigVersion;
                Options.Save();
            }
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
            float original = amount;
            amount = PowerDrainMath.AdjustToolDrain(amount, PowerSaverBZPlugin.Options.DrainMultiplier);

            if (PowerSaverBZPlugin.Options.EnableLogging)
                PowerSaverDiagnostics.RecordEnergyMixin(original, amount);
        }
    }

    /// <summary>
    /// Vehicle-specific patch: catches vehicle engine power consumption
    /// that goes through the vehicle energy interface.
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
                PowerSaverBZPlugin.Log.LogDebug("[PowerSaver BZ] Found Vehicle.ConsumeEnergy(float)");
                return method;
            }

            // Fallback: try two-param version if single doesn't exist
            method = AccessTools.Method(typeof(Vehicle), "ConsumeEnergy", new[] { typeof(float), typeof(float).MakeByRefType() });
            if (method != null)
            {
                PowerSaverBZPlugin.Log.LogDebug("[PowerSaver BZ] Found Vehicle.ConsumeEnergy(float, out float)");
                return method;
            }

            PowerSaverBZPlugin.Log.LogWarning("[PowerSaver BZ] Could not find Vehicle.ConsumeEnergy - vehicle patch disabled!");
            return null;
        }

        [HarmonyPrefix]
        static void Prefix(ref float amount)
        {
            float original = amount;
            amount = PowerDrainMath.AdjustVehicleDrain(
                amount,
                PowerSaverBZPlugin.Options.DrainMultiplier,
                PowerSaverBZPlugin.Options.VehicleDrainMultiplier);

            if (PowerSaverBZPlugin.Options.EnableLogging)
                PowerSaverDiagnostics.RecordVehicle(original, amount);
        }
    }

    /// <summary>
    /// Base power relay patch: covers habitat power consumption from things
    /// like water filtration machines, fabricators, scanners, etc.
    /// Uses ModifyPower(float, out float) - amount is negative for consumption.
    /// </summary>
    [HarmonyPatch]
    internal static class PowerRelay_ModifyPower_Patch
    {
        [HarmonyTargetMethod]
        static System.Reflection.MethodBase TargetMethod()
        {
            var method = AccessTools.Method(typeof(PowerRelay), "ModifyPower",
                new[] { typeof(float), typeof(float).MakeByRefType() });
            if (method != null)
            {
                PowerSaverBZPlugin.Log.LogDebug("[PowerSaver BZ] Found PowerRelay.ModifyPower(float, out float)");
                return method;
            }

            PowerSaverBZPlugin.Log.LogWarning("[PowerSaver BZ] Could not find PowerRelay.ModifyPower - base patch disabled!");
            return null;
        }

        [HarmonyPrefix]
        static void Prefix(ref float amount)
        {
            float original = amount;
            if (original >= 0f)
            {
                return;
            }

            if (CyclopsSonarDrainContext.IsActive)
            {
                amount = PowerDrainMath.AdjustVehicleDrain(
                    amount,
                    PowerSaverBZPlugin.Options.DrainMultiplier,
                    PowerSaverBZPlugin.Options.VehicleDrainMultiplier);

                if (PowerSaverBZPlugin.Options.EnableLogging)
                    PowerSaverDiagnostics.RecordVehicle(original, amount);

                return;
            }

            amount = PowerDrainMath.AdjustBasePowerDelta(
                amount,
                PowerSaverBZPlugin.Options.DrainMultiplier,
                PowerSaverBZPlugin.Options.BaseDrainMultiplier);

            if (PowerSaverBZPlugin.Options.EnableLogging)
                PowerSaverDiagnostics.RecordBase(original, amount);
        }
    }

    [HarmonyPatch(typeof(CyclopsSonarButton), "SonarPing")]
    internal static class CyclopsSonarButton_SonarPing_Patch
    {
        [HarmonyPrefix]
        static void Prefix()
        {
            CyclopsSonarDrainContext.Enter();
        }

        [HarmonyFinalizer]
        static void Finalizer()
        {
            CyclopsSonarDrainContext.Exit();
        }
    }
}
