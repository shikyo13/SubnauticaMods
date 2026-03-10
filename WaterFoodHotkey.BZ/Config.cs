using Nautilus.Json;
using Nautilus.Options;
using Nautilus.Options.Attributes;

namespace WaterFoodHotkey.BZ
{
    [Menu("Water/Food/Health Hotkeys BZ")]
    public class WaterFoodHotkeyConfig : ConfigFile
    {
        [Toggle("Enable Food/Drink Hotkey",
            Tooltip = "Enable automatic eating/drinking when the hotkey is pressed.")]
        public bool ToggleFoodDrink = true;

        [Toggle("Enable Health Pack Hotkey",
            Tooltip = "Enable automatic med kit use when the hotkey is pressed.")]
        public bool ToggleHealth = true;

        [Toggle("Enable Heat Hotkey",
            Tooltip = "Enable automatic thermos/heat item use when the hotkey is pressed.")]
        public bool ToggleHeat = true;

        [Choice("Text Style", "Standard", "Subtitles")]
        [OnChange(nameof(OnTextStyleChanged))]
        public string TextStyle = "Standard";

        [Slider("Food/Drink Threshold %", 1, 100, DefaultValue = 50, Step = 1,
            Tooltip = "Auto-eat when food OR water falls below this percentage.")]
        public float FoodDrinkPercent = 50f;

        [Slider("Health Threshold %", 1, 100, DefaultValue = 50, Step = 1,
            Tooltip = "Auto-use med kit when health falls below this percentage.")]
        public float HealthPercent = 50f;

        [Slider("Heat Threshold %", 1, 100, DefaultValue = 50, Step = 1,
            Tooltip = "Auto-use thermos when body heat falls below this percentage.")]
        public float HeatPercent = 50f;

        private void OnTextStyleChanged(ChoiceChangedEventArgs<string> e)
        {
            // Config auto-persists via Nautilus; nothing extra needed.
        }
    }
}
