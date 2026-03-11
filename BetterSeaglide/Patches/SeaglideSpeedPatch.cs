using HarmonyLib;
using UnityEngine;

namespace BetterSeaglide.Patches
{
    /// <summary>
    /// Replaces UnderwaterMotor.AlterMaxSpeed to add seaglide sprint boost.
    /// Replicates the full vanilla speed calculation so the boost is applied at
    /// the correct point — after equipment penalties but before smoothing.
    /// </summary>
    [HarmonyPatch(typeof(UnderwaterMotor), "AlterMaxSpeed")]
    internal static class UnderwaterMotor_AlterMaxSpeed_Patch
    {
        [HarmonyPrefix]
        static bool Prefix(UnderwaterMotor __instance, float inMaxSpeed, ref float __result,
            ref float ___currentPlayerSpeedMultipler)
        {
            try
            {
                float speed = inMaxSpeed;

                switch (Inventory.main.equipment.GetTechTypeInSlot("Tank"))
                {
                    case TechType.Tank:             speed -= 0.425f;   break;
                    case TechType.DoubleTank:        speed -= 0.5f;     break;
                    case TechType.PlasteelTank:      speed -= 0.10625f; break;
                    case TechType.HighCapacityTank:  speed -= 0.6375f;  break;
                }

                int count = Inventory.main.container.GetCount(TechType.HighCapacityTank);
                speed -= count * 1.275f;
                if (speed < 2.0f)
                    speed = 2.0f;

                if (Inventory.main.equipment.GetTechTypeInSlot("Body") == TechType.ReinforcedDiveSuit)
                    speed = Mathf.Max(2.0f, speed - 1.0f);

                if (Player.main.motorMode != Player.MotorMode.Seaglide)
                {
                    switch (Inventory.main.equipment.GetTechTypeInSlot("Foots"))
                    {
                        case TechType.Fins:           speed += 1.9f; break;
                        case TechType.UltraGlideFins:  speed += 3.2f; break;
                    }

                    if (Inventory.main.GetHeldTool() != null)
                        --speed;
                }

                // Seaglide sprint boost — hold sprint key to boost
                if (BetterSeaglidePlugin.Config.ToggleBoost)
                {
                    var held = Inventory.main.GetHeld();
                    if (held != null && GameInput.GetButtonHeld(GameInput.Button.Sprint) && held.gameObject.GetComponent<Seaglide>() != null)
                    {
                        speed += BetterSeaglidePlugin.Config.BoostAddition;
                        speed *= BetterSeaglidePlugin.Config.BoostMultiplier;
                    }
                }

                if (__instance.gameObject.transform.position.y > Player.main.GetWaterLevel())
                    speed *= 1.3f;

                ___currentPlayerSpeedMultipler = Mathf.MoveTowards(___currentPlayerSpeedMultipler, __instance.playerSpeedModifier, 0.3f * Time.deltaTime);
                __result = speed * ___currentPlayerSpeedMultipler;
            }
            catch
            {
                return true; // fall through to vanilla on error
            }

            return false;
        }
    }
}
