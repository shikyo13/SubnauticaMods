using System;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;

namespace PowerSaver.BZ
{
    internal static class EasyCraftCompatibility
    {
        private const string EasyCraftAssemblyName = "EasyCraft_BZ";
        private const string EasyCraftFixAssemblyName = "EasyCraftFix.BZ";

        private static bool _applied;

        [ThreadStatic]
        private static int _powerAccountingDepth;

        [ThreadStatic]
        private static int _consumeDepth;

        internal static void TryApply(Harmony harmony)
        {
            WarnIfStandaloneFixLoaded();

            if (_applied)
            {
                return;
            }

            try
            {
                TryApplyInternal(harmony);
            }
            catch (Exception ex)
            {
                PowerSaverBZPlugin.Log.LogError($"[PowerSaver BZ] Failed to apply EasyCraft compatibility patch: {ex}");
            }
        }

        private static void TryApplyInternal(Harmony harmony)
        {
            Assembly easyCraftAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(assembly => assembly.GetName().Name == EasyCraftAssemblyName);
            if (easyCraftAssembly == null)
            {
                PowerSaverBZPlugin.Log.LogDebug("[PowerSaver BZ] EasyCraft not found. Compatibility patch not needed.");
                return;
            }

            Type closestFabricators = easyCraftAssembly.GetType("EasyCraft.ClosestFabricators");
            MethodInfo hasEnergy = closestFabricators?.GetMethod(
                "HasEnergy",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            MethodInfo consumeEnergy = closestFabricators?.GetMethod(
                "ConsumeEnergy",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            MethodInfo powerRelayGetPower = AccessTools.Method(typeof(PowerRelay), nameof(PowerRelay.GetPower));
            MethodInfo powerSystemConsumeEnergy = AccessTools.Method(
                typeof(PowerSystem),
                nameof(PowerSystem.ConsumeEnergy),
                new[] { typeof(IPowerInterface), typeof(float), typeof(float).MakeByRefType() });

            if (hasEnergy == null || consumeEnergy == null || powerRelayGetPower == null || powerSystemConsumeEnergy == null)
            {
                PowerSaverBZPlugin.Log.LogWarning("[PowerSaver BZ] EasyCraft compatibility patch could not find one or more target methods.");
                return;
            }

            harmony.Patch(
                hasEnergy,
                prefix: new HarmonyMethod(typeof(EasyCraftCompatibility), nameof(BeginPowerAccounting)),
                finalizer: new HarmonyMethod(typeof(EasyCraftCompatibility), nameof(EndPowerAccounting)));
            harmony.Patch(
                consumeEnergy,
                prefix: new HarmonyMethod(typeof(EasyCraftCompatibility), nameof(BeginConsumeAccounting)),
                finalizer: new HarmonyMethod(typeof(EasyCraftCompatibility), nameof(EndConsumeAccounting)));
            harmony.Patch(
                powerRelayGetPower,
                postfix: new HarmonyMethod(typeof(EasyCraftCompatibility), nameof(PowerRelayGetPowerPostfix)));
            harmony.Patch(
                powerSystemConsumeEnergy,
                postfix: new HarmonyMethod(typeof(EasyCraftCompatibility), nameof(PowerSystemConsumeEnergyPostfix)));

            _applied = true;
            PowerSaverBZPlugin.Log.LogInfo("[PowerSaver BZ] EasyCraft compatibility patch applied.");
        }

        private static void WarnIfStandaloneFixLoaded()
        {
            bool easyCraftFixLoaded = AppDomain.CurrentDomain.GetAssemblies()
                .Any(assembly => assembly.GetName().Name == EasyCraftFixAssemblyName);
            if (easyCraftFixLoaded || IsStandaloneFixInstalled())
            {
                PowerSaverBZPlugin.Log.LogWarning(
                    "[PowerSaver BZ] EasyCraftFix.BZ is installed or loaded. Remove it when using PowerSaver BZ 1.0.1 or newer; PowerSaver now includes a narrower EasyCraft accounting fix.");
            }
        }

        private static bool IsStandaloneFixInstalled()
        {
            try
            {
                return System.IO.Directory.EnumerateFiles(
                        Paths.PluginPath,
                        "EasyCraftFix.BZ.dll",
                        System.IO.SearchOption.AllDirectories)
                    .Any();
            }
            catch (Exception ex)
            {
                PowerSaverBZPlugin.Log.LogDebug($"[PowerSaver BZ] Could not scan plugins for EasyCraftFix.BZ: {ex.Message}");
                return false;
            }
        }

        private static void BeginPowerAccounting()
        {
            _powerAccountingDepth++;
        }

        private static Exception EndPowerAccounting(Exception __exception)
        {
            if (_powerAccountingDepth > 0)
            {
                _powerAccountingDepth--;
            }

            return __exception;
        }

        private static void BeginConsumeAccounting()
        {
            _powerAccountingDepth++;
            _consumeDepth++;
        }

        private static Exception EndConsumeAccounting(Exception __exception)
        {
            if (_consumeDepth > 0)
            {
                _consumeDepth--;
            }

            if (_powerAccountingDepth > 0)
            {
                _powerAccountingDepth--;
            }

            return __exception;
        }

        private static void PowerRelayGetPowerPostfix(ref float __result)
        {
            if (_powerAccountingDepth <= 0)
            {
                return;
            }

            __result = PowerDrainMath.ToEasyCraftVirtualPower(__result, GetEffectiveBaseMultiplier());
        }

        private static void PowerSystemConsumeEnergyPostfix(float amount, ref float amountConsumed)
        {
            if (_consumeDepth <= 0)
            {
                return;
            }

            amountConsumed = PowerDrainMath.ToEasyCraftVirtualConsumed(amountConsumed, amount, GetEffectiveBaseMultiplier());
        }

        private static float GetEffectiveBaseMultiplier()
        {
            return PowerDrainMath.GetBaseMultiplier(
                PowerSaverBZPlugin.Options.DrainMultiplier,
                PowerSaverBZPlugin.Options.BaseDrainMultiplier);
        }
    }
}
