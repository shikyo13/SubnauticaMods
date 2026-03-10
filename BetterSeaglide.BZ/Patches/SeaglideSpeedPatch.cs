using HarmonyLib;
using UnityEngine;

namespace BetterSeaglide.BZ.Patches
{
    /// <summary>
    /// Activates power glide posture when sprint is held while using the seaglide.
    /// </summary>
    [HarmonyPatch(typeof(Seaglide), "Update")]
    internal static class SeaglideBoostActivatePatch
    {
        [HarmonyPostfix]
        static void Postfix(Seaglide __instance)
        {
            try
            {
                if (!BetterSeaglideBZPlugin.Config.ToggleBoost) return;
                if (Player.main == null || Player.main.motorMode != Player.MotorMode.Seaglide) return;

                if (GameInput.GetButtonHeld(GameInput.Button.Sprint) && __instance.HasEnergy())
                {
                    __instance.powerGlideActive = true;
                }
            }
            catch { }
        }
    }

    /// <summary>
    /// Boosts seaglide max speed when sprint is held.
    /// Patches the speed CAP via UnderwaterMotor.AlterMaxSpeed, preventing
    /// the unbounded force accumulation of the old FixedUpdate approach.
    /// </summary>
    [HarmonyPatch(typeof(UnderwaterMotor), "AlterMaxSpeed")]
    internal static class UnderwaterMotor_AlterMaxSpeed_Patch
    {
        private static bool _wasBoosting;

        [HarmonyPostfix]
        static void Postfix(ref float __result)
        {
            try
            {
                if (!BetterSeaglideBZPlugin.Config.ToggleBoost) return;
                if (Player.main == null || Player.main.motorMode != Player.MotorMode.Seaglide) return;

                var held = Inventory.main?.GetHeld()?.GetComponent<Seaglide>();
                if (held == null || !held.HasEnergy()) return;

                bool boosting = GameInput.GetButtonHeld(GameInput.Button.Sprint);
                if (boosting)
                {
                    __result += BetterSeaglideBZPlugin.Config.BoostAddition;
                    __result *= BetterSeaglideBZPlugin.Config.BoostMultiplier;
                }

                if (boosting && !_wasBoosting)
                    ErrorMessage.AddMessage("Seaglide Boost: ON");
                else if (!boosting && _wasBoosting)
                    ErrorMessage.AddMessage("Seaglide Boost: OFF");
                _wasBoosting = boosting;
            }
            catch { }
        }
    }
}
