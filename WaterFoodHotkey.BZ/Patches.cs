using System;
using HarmonyLib;

namespace WaterFoodHotkey.BZ
{
    /// <summary>
    /// Food/Drink hotkey: scans inventory for edible items and auto-eats
    /// when food or water is below the configured threshold.
    /// </summary>
    [HarmonyPatch(typeof(Player), "Update")]
    internal static class Player_Update_FoodDrink_Patch
    {
        [HarmonyPostfix]
        static void Postfix(Player __instance)
        {
            try
            {
                if (!WaterFoodHotkeyBZPlugin.Options.ToggleFoodDrink)
                    return;

                if (!GameInput.GetButtonDown(WaterFoodHotkeyBZPlugin.FoodDrinkButton))
                    return;

                if (__instance == null)
                    return;

                Survival survival = __instance.GetComponent<Survival>();
                if (survival == null)
                    return;

                float foodPercent = survival.food;
                float waterPercent = survival.water;
                float threshold = WaterFoodHotkeyBZPlugin.Options.FoodDrinkPercent;

                Inventory pInventory = Inventory.Get();
                if (pInventory == null || pInventory.container == null)
                    return;

                // Find best edible item in inventory
                InventoryItem bestItem = null;
                float bestScore = 0f;

                foreach (InventoryItem item in pInventory.container)
                {
                    if (item == null || item.item == null)
                        continue;

                    Eatable eatable = item.item.GetComponent<Eatable>();
                    if (eatable == null)
                        continue;

                    float foodVal = eatable.GetFoodValue();
                    float waterVal = eatable.GetWaterValue();

                    // Skip items with no food or water value
                    if (foodVal <= 0f && waterVal <= 0f)
                        continue;

                    // Prioritize items that help with the most deficient stat
                    float score = 0f;
                    if (foodPercent <= threshold)
                        score += foodVal;
                    if (waterPercent <= threshold)
                        score += waterVal;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestItem = item;
                    }
                }

                if (bestItem != null && (foodPercent <= threshold || waterPercent <= threshold))
                {
                    pInventory.ExecuteItemAction(ItemAction.Eat, bestItem);
                    ShowMessage($"Auto-consumed: {Language.main.Get(bestItem.item.GetTechName())}");
                }
                else if (foodPercent > threshold && waterPercent > threshold)
                {
                    ShowMessage("Food and water levels are sufficient.");
                }
                else
                {
                    ShowMessage("No edible items found in inventory.");
                }
            }
            catch (Exception e)
            {
                WaterFoodHotkeyBZPlugin.Log.LogError($"[FoodDrink Patch] {e}");
            }
        }

        static void ShowMessage(string msg)
        {
            if (WaterFoodHotkeyBZPlugin.Options.TextStyle == "Subtitles")
                Subtitles.Add(msg);
            else
                ErrorMessage.AddMessage(msg);
        }
    }

    /// <summary>
    /// Health hotkey: finds FirstAidKit in inventory and auto-uses
    /// when health is below the configured threshold.
    /// </summary>
    [HarmonyPatch(typeof(Player), "Update")]
    internal static class Player_Update_Health_Patch
    {
        [HarmonyPostfix]
        static void Postfix(Player __instance)
        {
            try
            {
                if (!WaterFoodHotkeyBZPlugin.Options.ToggleHealth)
                    return;

                if (!GameInput.GetButtonDown(WaterFoodHotkeyBZPlugin.MedButton))
                    return;

                if (__instance == null)
                    return;

                LiveMixin liveMixin = __instance.liveMixin;
                if (liveMixin == null)
                    return;

                float healthPercent = (liveMixin.health / liveMixin.maxHealth) * 100f;
                float threshold = WaterFoodHotkeyBZPlugin.Options.HealthPercent;

                Inventory pInventory = Inventory.Get();
                if (pInventory == null || pInventory.container == null)
                    return;

                if (healthPercent <= threshold)
                {
                    // Find a FirstAidKit in inventory
                    InventoryItem medItem = null;
                    foreach (InventoryItem item in pInventory.container)
                    {
                        if (item == null || item.item == null)
                            continue;

                        if (item.item.GetTechType() == TechType.FirstAidKit)
                        {
                            medItem = item;
                            break;
                        }
                    }

                    if (medItem != null)
                    {
                        pInventory.ExecuteItemAction(ItemAction.Use, medItem);
                        ShowMessage("Auto-used: First Aid Kit");
                    }
                    else
                    {
                        ShowMessage("No First Aid Kit found in inventory.");
                    }
                }
                else
                {
                    ShowMessage("Health level is sufficient.");
                }
            }
            catch (Exception e)
            {
                WaterFoodHotkeyBZPlugin.Log.LogError($"[Health Patch] {e}");
            }
        }

        static void ShowMessage(string msg)
        {
            if (WaterFoodHotkeyBZPlugin.Options.TextStyle == "Subtitles")
                Subtitles.Add(msg);
            else
                ErrorMessage.AddMessage(msg);
        }
    }

    /// <summary>
    /// Heat hotkey: finds items with cold meter value (thermos/heat items)
    /// in inventory and auto-eats when body temperature is below threshold.
    /// In BZ, Thermos is a tool held in hand, but Eatable items with
    /// coldMeterValue > 0 (like Coffee) can be consumed from inventory.
    /// </summary>
    [HarmonyPatch(typeof(Player), "Update")]
    internal static class Player_Update_Heat_Patch
    {
        // Cache field accessor for hot-path (Player.Update runs every frame)
        private static readonly AccessTools.FieldRef<Survival, BodyTemperature> BodyTempRef =
            AccessTools.FieldRefAccess<Survival, BodyTemperature>("bodyTemperature");

        [HarmonyPostfix]
        static void Postfix(Player __instance)
        {
            try
            {
                if (!WaterFoodHotkeyBZPlugin.Options.ToggleHeat)
                    return;

                if (!GameInput.GetButtonDown(WaterFoodHotkeyBZPlugin.HeatButton))
                    return;

                if (__instance == null)
                    return;

                Survival survival = __instance.GetComponent<Survival>();
                if (survival == null)
                    return;

                // Access body temperature (private field on Survival)
                BodyTemperature bodyTemp = BodyTempRef(survival);
                if (bodyTemp == null)
                    return;

                float currentHeat = bodyTemp.currentBodyHeatValue;
                float maxHeat = bodyTemp.maxBodyHeatValue;

                // Avoid division by zero
                if (maxHeat <= 0f)
                    return;

                float heatPercent = (currentHeat / maxHeat) * 100f;
                float threshold = WaterFoodHotkeyBZPlugin.Options.HeatPercent;

                Inventory pInventory = Inventory.Get();
                if (pInventory == null || pInventory.container == null)
                    return;

                if (heatPercent <= threshold)
                {
                    // Find an item with cold meter value in inventory
                    InventoryItem bestItem = null;
                    float bestColdValue = 0f;

                    foreach (InventoryItem item in pInventory.container)
                    {
                        if (item == null || item.item == null)
                            continue;

                        Eatable eatable = item.item.GetComponent<Eatable>();
                        if (eatable == null)
                            continue;

                        float coldVal = eatable.GetColdMeterValue();
                        if (coldVal > bestColdValue)
                        {
                            bestColdValue = coldVal;
                            bestItem = item;
                        }
                    }

                    if (bestItem != null)
                    {
                        pInventory.ExecuteItemAction(ItemAction.Eat, bestItem);
                        ShowMessage($"Auto-consumed for warmth: {Language.main.Get(bestItem.item.GetTechName())}");
                    }
                    else
                    {
                        ShowMessage("No heat items found in inventory.");
                    }
                }
                else
                {
                    ShowMessage("Body temperature is sufficient.");
                }
            }
            catch (Exception e)
            {
                WaterFoodHotkeyBZPlugin.Log.LogError($"[Heat Patch] {e}");
            }
        }

        static void ShowMessage(string msg)
        {
            if (WaterFoodHotkeyBZPlugin.Options.TextStyle == "Subtitles")
                Subtitles.Add(msg);
            else
                ErrorMessage.AddMessage(msg);
        }
    }
}
