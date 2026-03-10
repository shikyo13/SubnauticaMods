using Nautilus.Json;
using Nautilus.Options.Attributes;

namespace WaterFoodHotkey
{
    [Menu("Water/Food Hotkeys", SaveOn = MenuAttribute.SaveEvents.ChangeValue,
          LoadOn = MenuAttribute.LoadEvents.MenuRegistered)]
    public class Config : ConfigFile
    {
        [Toggle("Enable Water Hotkey")]
        public bool ToggleWaterHotKey = true;

        [Toggle("Enable Food Hotkey")]
        public bool ToggleFoodHotKey = true;

        [Choice("Text Style", "Standard", "Subtitles")]
        public string TextStyle = "Standard";

        [Slider("Water %", 1, 99, DefaultValue = 99)]
        public float WaterPercentage = 99f;

        [Slider("Food %", 1, 99, DefaultValue = 99)]
        public float FoodPercentage = 99f;
    }
}
