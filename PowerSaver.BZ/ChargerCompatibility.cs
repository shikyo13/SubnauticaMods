using System;
using System.Reflection;
using HarmonyLib;

namespace PowerSaver.BZ
{
    internal static class ChargerCompatibility
    {
        private static bool _applied;

        [ThreadStatic]
        private static int _chargerDepth;

        internal static void Apply(Harmony harmony)
        {
            if (_applied)
            {
                return;
            }

            try
            {
                MethodInfo chargerUpdate = AccessTools.Method(typeof(Charger), "Update");
                MethodInfo powerRelayGetPower = AccessTools.Method(typeof(PowerRelay), nameof(PowerRelay.GetPower));
                MethodInfo powerConsumerConsumePower = AccessTools.Method(
                    typeof(PowerConsumer),
                    nameof(PowerConsumer.ConsumePower),
                    new[] { typeof(float), typeof(float).MakeByRefType() });

                if (chargerUpdate == null || powerRelayGetPower == null || powerConsumerConsumePower == null)
                {
                    PowerSaverBZPlugin.Log.LogWarning("[PowerSaver BZ] Charger compatibility patch could not find one or more target methods.");
                    return;
                }

                harmony.Patch(
                    chargerUpdate,
                    prefix: new HarmonyMethod(typeof(ChargerCompatibility), nameof(BeginChargerUpdate)),
                    finalizer: new HarmonyMethod(typeof(ChargerCompatibility), nameof(EndChargerUpdate)));
                harmony.Patch(
                    powerRelayGetPower,
                    postfix: new HarmonyMethod(typeof(ChargerCompatibility), nameof(PowerRelayGetPowerPostfix)));
                harmony.Patch(
                    powerConsumerConsumePower,
                    postfix: new HarmonyMethod(typeof(ChargerCompatibility), nameof(PowerConsumerConsumePowerPostfix)));

                _applied = true;
                PowerSaverBZPlugin.Log.LogInfo("[PowerSaver BZ] Charger power accounting compatibility patch applied.");
            }
            catch (Exception ex)
            {
                PowerSaverBZPlugin.Log.LogError($"[PowerSaver BZ] Failed to apply charger compatibility patch: {ex}");
            }
        }

        private static void BeginChargerUpdate()
        {
            _chargerDepth++;
        }

        private static Exception EndChargerUpdate(Exception __exception)
        {
            if (_chargerDepth > 0)
            {
                _chargerDepth--;
            }

            return __exception;
        }

        private static void PowerRelayGetPowerPostfix(ref float __result)
        {
            if (_chargerDepth <= 0)
            {
                return;
            }

            __result = PowerDrainMath.ToChargerVirtualPower(__result, GetEffectiveBaseMultiplier());
        }

        private static void PowerConsumerConsumePowerPostfix(float powerToConsume, ref float consumed)
        {
            if (_chargerDepth <= 0)
            {
                return;
            }

            consumed = PowerDrainMath.ToChargerVirtualConsumed(consumed, powerToConsume, GetEffectiveBaseMultiplier());
        }

        private static float GetEffectiveBaseMultiplier()
        {
            return PowerDrainMath.GetBaseMultiplier(
                PowerSaverBZPlugin.Options.DrainMultiplier,
                PowerSaverBZPlugin.Options.BaseDrainMultiplier);
        }
    }
}
