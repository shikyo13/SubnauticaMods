using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace PowerSaver
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

            PowerSaverPlugin.Log.LogInfo(
                $"[PowerSaver] {LogInterval}s summary - Total: {(float)totalOriginal:F2} -> {(float)totalAdjusted:F2} " +
                $"(saved {(float)saved:F2}, {pct:F0}%) | Tools: {(float)_toolsAdjusted:F2} | Vehicles: {(float)_vehicleAdjusted:F2} | Base: {(float)_baseAdjusted:F2}");

            _toolsOriginal = _toolsAdjusted = 0;
            _vehicleOriginal = _vehicleAdjusted = 0;
            _baseOriginal = _baseAdjusted = 0;
        }
    }

    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class PowerSaverPlugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "com.zerotheabsolute.powersaver";
        public const string PLUGIN_NAME = "PowerSaver";
        public const string PLUGIN_VERSION = "1.0.1";

        internal static ManualLogSource Log;
        internal static ConfigEntry<string> ConfigVersion;
        internal static ConfigEntry<float> DrainMultiplier;
        internal static ConfigEntry<float> VehicleDrainMultiplier;
        internal static ConfigEntry<float> BaseDrainMultiplier;
        internal static ConfigEntry<bool> EnableLogging;

        private static Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            ConfigVersion = Config.Bind(
                "Internal",
                "ConfigVersion",
                string.Empty,
                "Internal config migration marker. Do not edit."
            );

            DrainMultiplier = Config.Bind(
                "General",
                "DrainMultiplier",
                0.75f,
                new ConfigDescription(
                    "Baseline multiplier for supported power drain. 0.75 = 25% less drain, 0.5 = 50% less drain, 1.0 = vanilla.",
                    new AcceptableValueRange<float>(0.01f, 2.0f)
                )
            );

            VehicleDrainMultiplier = Config.Bind(
                "Vehicles",
                "VehicleDrainMultiplier",
                1.0f,
                new ConfigDescription(
                    "Additional vehicle multiplier. Effective vehicle drain is Global x Vehicle. Covers Seamoth, Prawn, Cyclops engines, and Cyclops sonar.",
                    new AcceptableValueRange<float>(0.01f, 2.0f)
                )
            );

            BaseDrainMultiplier = Config.Bind(
                "Base",
                "BaseDrainMultiplier",
                1.0f,
                new ConfigDescription(
                    "Additional base and habitat multiplier. Effective base drain is Global x Base.",
                    new AcceptableValueRange<float>(0.01f, 2.0f)
                )
            );

            EnableLogging = Config.Bind(
                "Debug",
                "EnableLogging",
                false,
                "Log power drain events to console (noisy, for debugging only)."
            );

            MigrateConfigDefaults();

            _harmony = new Harmony(PLUGIN_GUID);
            ApplyPatches();

            Log.LogInfo(
                $"{PLUGIN_NAME} v{PLUGIN_VERSION} loaded! Global drain: {DrainMultiplier.Value}x | " +
                $"Effective vehicles: {PowerDrainMath.GetVehicleMultiplier(DrainMultiplier.Value, VehicleDrainMultiplier.Value)}x | " +
                $"Effective base: {PowerDrainMath.GetBaseMultiplier(DrainMultiplier.Value, BaseDrainMultiplier.Value)}x");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }

        private void MigrateConfigDefaults()
        {
            if (PowerSaverConfigMigration.ShouldMigrateOnePointOneDefaults(
                ConfigVersion.Value,
                DrainMultiplier.Value,
                VehicleDrainMultiplier.Value,
                BaseDrainMultiplier.Value))
            {
                VehicleDrainMultiplier.Value = 1.0f;
                BaseDrainMultiplier.Value = 1.0f;
                ConfigVersion.Value = PowerSaverConfigMigration.CurrentConfigVersion;
                Config.Save();
                Log.LogInfo("[PowerSaver] Migrated 1.0.1 generated category defaults to 1.0 so effective default drain remains 75 percent.");
                return;
            }

            if (ConfigVersion.Value != PowerSaverConfigMigration.CurrentConfigVersion)
            {
                ConfigVersion.Value = PowerSaverConfigMigration.CurrentConfigVersion;
                Config.Save();
            }
        }

        private static void ApplyPatches()
        {
            TryPatch(
                "EnergyMixin.ConsumeEnergy(float)",
                AccessTools.Method(typeof(EnergyMixin), nameof(EnergyMixin.ConsumeEnergy), new[] { typeof(float) }),
                typeof(EnergyMixin_ConsumeEnergy_Patch),
                nameof(EnergyMixin_ConsumeEnergy_Patch.Prefix));

            TryPatch(
                "Vehicle.ConsumeEnergy",
                FindVehicleConsumeEnergyMethod(),
                typeof(Vehicle_ConsumeEnergy_Patch),
                nameof(Vehicle_ConsumeEnergy_Patch.Prefix));

            TryPatch(
                "PowerRelay.ModifyPower(float, out float)",
                AccessTools.Method(typeof(PowerRelay), "ModifyPower", new[] { typeof(float), typeof(float).MakeByRefType() }),
                typeof(PowerRelay_ModifyPower_Patch),
                nameof(PowerRelay_ModifyPower_Patch.Prefix));

            TryPatch(
                "CyclopsSonarButton.SonarPing",
                AccessTools.Method(typeof(CyclopsSonarButton), "SonarPing"),
                typeof(CyclopsSonarButton_SonarPing_Patch),
                nameof(CyclopsSonarButton_SonarPing_Patch.Prefix),
                nameof(CyclopsSonarButton_SonarPing_Patch.Finalizer));
        }

        private static MethodBase FindVehicleConsumeEnergyMethod()
        {
            MethodBase method = AccessTools.Method(typeof(Vehicle), "ConsumeEnergy", new[] { typeof(float) });
            if (method != null)
            {
                return method;
            }

            return AccessTools.Method(typeof(Vehicle), "ConsumeEnergy", new[] { typeof(float), typeof(float).MakeByRefType() });
        }

        private static void TryPatch(
            string targetName,
            MethodBase targetMethod,
            Type patchType,
            string prefixName,
            string finalizerName = null)
        {
            if (targetMethod == null)
            {
                Log.LogWarning($"[PowerSaver] Could not find {targetName}. This patch is disabled.");
                return;
            }

            try
            {
                HarmonyMethod prefix = prefixName == null ? null : new HarmonyMethod(patchType, prefixName);
                HarmonyMethod finalizer = finalizerName == null ? null : new HarmonyMethod(patchType, finalizerName);
                _harmony.Patch(targetMethod, prefix: prefix, finalizer: finalizer);
                Log.LogDebug($"[PowerSaver] Patched {targetName}");
            }
            catch (Exception ex)
            {
                Log.LogError($"[PowerSaver] Failed to patch {targetName}. Patch disabled. {ex}");
            }
        }
    }

    /// <summary>
    /// Core patch: intercepts ALL battery/power cell drain in the game.
    /// EnergyMixin.ConsumeEnergy is called by tools, vehicles, and equipment.
    /// </summary>
    internal static class EnergyMixin_ConsumeEnergy_Patch
    {
        internal static void Prefix(ref float amount)
        {
            float original = amount;
            amount = PowerDrainMath.AdjustToolDrain(amount, PowerSaverPlugin.DrainMultiplier.Value);

            if (PowerSaverPlugin.EnableLogging.Value)
                PowerSaverDiagnostics.RecordEnergyMixin(original, amount);
        }
    }

    /// <summary>
    /// Vehicle-specific patch: catches Seamoth, Prawn Suit, and Cyclops
    /// engine power consumption that goes through the vehicle energy interface.
    /// Uses explicit argument types to resolve overload ambiguity.
    /// </summary>
    internal static class Vehicle_ConsumeEnergy_Patch
    {
        internal static void Prefix(ref float amount)
        {
            float original = amount;
            amount = PowerDrainMath.AdjustVehicleDrain(
                amount,
                PowerSaverPlugin.DrainMultiplier.Value,
                PowerSaverPlugin.VehicleDrainMultiplier.Value);

            if (PowerSaverPlugin.EnableLogging.Value)
                PowerSaverDiagnostics.RecordVehicle(original, amount);
        }
    }

    /// <summary>
    /// Base power relay patch: covers habitat power consumption from things
    /// like water filtration machines, fabricators, scanners, etc.
    /// Uses ModifyPower(float, out float). Amount is negative for consumption.
    /// </summary>
    internal static class PowerRelay_ModifyPower_Patch
    {
        internal static void Prefix(ref float amount)
        {
            if (amount >= 0f) return;

            float original = amount;
            if (CyclopsSonarDrainContext.IsActive)
            {
                amount = PowerDrainMath.AdjustVehicleDrain(
                    amount,
                    PowerSaverPlugin.DrainMultiplier.Value,
                    PowerSaverPlugin.VehicleDrainMultiplier.Value);

                if (PowerSaverPlugin.EnableLogging.Value)
                    PowerSaverDiagnostics.RecordVehicle(original, amount);

                return;
            }

            amount = PowerDrainMath.AdjustBasePowerDelta(
                amount,
                PowerSaverPlugin.DrainMultiplier.Value,
                PowerSaverPlugin.BaseDrainMultiplier.Value);

            if (PowerSaverPlugin.EnableLogging.Value)
                PowerSaverDiagnostics.RecordBase(original, amount);
        }
    }

    internal static class CyclopsSonarButton_SonarPing_Patch
    {
        internal static void Prefix()
        {
            CyclopsSonarDrainContext.Enter();
        }

        internal static void Finalizer()
        {
            CyclopsSonarDrainContext.Exit();
        }
    }
}
