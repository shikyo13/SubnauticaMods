using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace WaterFoodHotkey
{
    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    internal static class Player_Update_WaterPatch
    {
        private static readonly TechType[] WaterTypes = new[]
        {
            TechType.FilteredWater,
            TechType.DisinfectedWater,
            TechType.BigFilteredWater,
            TechType.Coffee
        };

        [HarmonyPostfix]
        static void Postfix(Player __instance)
        {
            try
            {
                var config = WaterFoodHotkeyPlugin.ConfigInstance;
                if (config == null || !config.ToggleWaterHotKey) return;
                if (!GameInput.GetButtonDown(WaterFoodHotkeyPlugin.WaterButton)) return;

                var survival = __instance.GetComponent<Survival>();
                if (survival == null) return;
                if (survival.water > config.WaterPercentage) return;

                var pInventory = Inventory.main;
                if (pInventory == null) return;
                var container = pInventory.container;
                if (container == null) return;

                foreach (var type in WaterTypes)
                {
                    IList<InventoryItem> items = container.GetItems(type);
                    if (items != null && items.Count > 0)
                    {
                        var item = items.First();
                        ShowMessage($"Drinking {Language.main.Get(type.AsString(false))}", config);
                        pInventory.ExecuteItemAction(ItemAction.Eat, item);
                        return;
                    }
                }

                ShowMessage("No water items in inventory", config);
            }
            catch
            {
                // Swallow exceptions to prevent game crashes from hot-path patch
            }
        }

        private static void ShowMessage(string message, Config config)
        {
            if (config.TextStyle == "Subtitles")
            {
                Subtitles.Add(message);
            }
            else
            {
                ErrorMessage.AddMessage(message);
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    internal static class Player_Update_FoodPatch
    {
        private static readonly TechType[] FoodTypes = new[]
        {
            // Cooked fish
            TechType.CookedBladderfish,
            TechType.CookedBoomerang,
            TechType.CookedEyeye,
            TechType.CookedGarryFish,
            TechType.CookedHoleFish,
            TechType.CookedHoopfish,
            TechType.CookedHoverfish,
            TechType.CookedLavaBoomerang,
            TechType.CookedLavaEyeye,
            TechType.CookedOculus,
            TechType.CookedPeeper,
            TechType.CookedReginald,
            TechType.CookedSpadefish,
            TechType.CookedSpinefish,
            // Cured fish
            TechType.CuredBladderfish,
            TechType.CuredBoomerang,
            TechType.CuredEyeye,
            TechType.CuredGarryFish,
            TechType.CuredHoleFish,
            TechType.CuredHoopfish,
            TechType.CuredHoverfish,
            TechType.CuredLavaBoomerang,
            TechType.CuredLavaEyeye,
            TechType.CuredOculus,
            TechType.CuredPeeper,
            TechType.CuredReginald,
            TechType.CuredSpadefish,
            TechType.CuredSpinefish,
            // Plants and snacks
            TechType.BulboTreePiece,
            TechType.PurpleVegetable,
            TechType.HangingFruit,
            TechType.Melon,
            TechType.NutrientBlock,
            TechType.Snack1,
            TechType.Snack2,
            TechType.Snack3
        };

        [HarmonyPostfix]
        static void Postfix(Player __instance)
        {
            try
            {
                var config = WaterFoodHotkeyPlugin.ConfigInstance;
                if (config == null || !config.ToggleFoodHotKey) return;
                if (!GameInput.GetButtonDown(WaterFoodHotkeyPlugin.FoodButton)) return;

                var survival = __instance.GetComponent<Survival>();
                if (survival == null) return;
                if (survival.food > config.FoodPercentage) return;

                var pInventory = Inventory.main;
                if (pInventory == null) return;
                var container = pInventory.container;
                if (container == null) return;

                foreach (var type in FoodTypes)
                {
                    IList<InventoryItem> items = container.GetItems(type);
                    if (items != null && items.Count > 0)
                    {
                        var item = items.First();
                        ShowMessage($"Eating {Language.main.Get(type.AsString(false))}", config);
                        pInventory.ExecuteItemAction(ItemAction.Eat, item);
                        return;
                    }
                }

                ShowMessage("No food items in inventory", config);
            }
            catch
            {
                // Swallow exceptions to prevent game crashes from hot-path patch
            }
        }

        private static void ShowMessage(string message, Config config)
        {
            if (config.TextStyle == "Subtitles")
            {
                Subtitles.Add(message);
            }
            else
            {
                ErrorMessage.AddMessage(message);
            }
        }
    }
}
