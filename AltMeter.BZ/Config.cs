using Nautilus.Json;
using Nautilus.Options;
using Nautilus.Options.Attributes;

namespace AltMeter.BZ
{
    [Menu("Altitude Meter", SaveOn = MenuAttribute.SaveEvents.ChangeValue, LoadOn = MenuAttribute.LoadEvents.MenuRegistered)]
    public class Config : ConfigFile
    {
        [Toggle("Toggle Altitude Symbol", Id = "ToggleAltSymbol"), OnChange(nameof(CheckboxToggleEvent))]
        public bool ToggleAltSymbol = true;

        [Toggle("Toggle Depth Text Color", Id = "ToggleDepthTextColor"), OnChange(nameof(CheckboxToggleEvent))]
        public bool ToggleDepthTextColor = true;

        [Toggle("Toggle Altitude Text Color", Id = "ToggleAltTextColor"), OnChange(nameof(CheckboxToggleEvent))]
        public bool ToggleAltTextColor = true;

        [Slider("Depth Text Red", 0.001f, 1.000f, DefaultValue = 0.016f, Id = "DepthTextRed", Step = 0.001f, Format = "{0:F3}"), OnChange(nameof(SliderChangeEvent))]
        public float DepthTextRed = 1.000f;

        [Slider("Depth Text Green", 0.001f, 1.000f, DefaultValue = 0.016f, Id = "DepthTextGreen", Step = 0.001f, Format = "{0:F3}"), OnChange(nameof(SliderChangeEvent))]
        public float DepthTextGreen = 1.000f;

        [Slider("Depth Text Blue", 0.001f, 1.000f, DefaultValue = 0.016f, Id = "DepthTextBlue", Step = 0.001f, Format = "{0:F3}"), OnChange(nameof(SliderChangeEvent))]
        public float DepthTextBlue = 1.000f;

        [Slider("Alt Text Red", 0.001f, 1.000f, DefaultValue = 0.016f, Id = "AltTextRed", Step = 0.001f, Format = "{0:F3}"), OnChange(nameof(SliderChangeEvent))]
        public float AltTextRed = 1.000f;

        [Slider("Alt Text Green", 0.001f, 1.000f, DefaultValue = 0.016f, Id = "AltTextGreen", Step = 0.001f, Format = "{0:F3}"), OnChange(nameof(SliderChangeEvent))]
        public float AltTextGreen = 1.000f;

        [Slider("Alt Text Blue", 0.001f, 1.000f, DefaultValue = 0.016f, Id = "AltTextBlue", Step = 0.001f, Format = "{0:F3}"), OnChange(nameof(SliderChangeEvent))]
        public float AltTextBlue = 1.000f;

        private void CheckboxToggleEvent(ToggleChangedEventArgs e)
        {
            switch (e.Id)
            {
                case "ToggleAltSymbol":
                    AltMeterBZPlugin.ToggleSymbol = e.Value;
                    break;
                case "ToggleDepthTextColor":
                    AltMeterBZPlugin.ToggleDepthTextColor = e.Value;
                    break;
                case "ToggleAltTextColor":
                    AltMeterBZPlugin.ToggleAltTextColor = e.Value;
                    break;
            }
        }

        private void SliderChangeEvent(SliderChangedEventArgs e)
        {
            switch (e.Id)
            {
                case "DepthTextRed":
                    AltMeterBZPlugin.DepthTextColorRed = e.Value;
                    break;
                case "DepthTextGreen":
                    AltMeterBZPlugin.DepthTextColorGreen = e.Value;
                    break;
                case "DepthTextBlue":
                    AltMeterBZPlugin.DepthTextColorBlue = e.Value;
                    break;
                case "AltTextRed":
                    AltMeterBZPlugin.AltColorRed = e.Value;
                    break;
                case "AltTextGreen":
                    AltMeterBZPlugin.AltColorGreen = e.Value;
                    break;
                case "AltTextBlue":
                    AltMeterBZPlugin.AltColorBlue = e.Value;
                    break;
            }
        }
    }
}
