using Nautilus.Json;
using Nautilus.Options;
using Nautilus.Options.Attributes;
using SubnauticaMods.Shared;
using UnityEngine;

namespace BetterFlashLight.BZ
{
    [Menu("BetterFlashLight BZ Options", SaveOn = MenuAttribute.SaveEvents.ChangeValue)]
    public class Config : ConfigFile
    {
        [Toggle("Enable Flashlight Options", Id = "FlashLightOptions")]
        public bool ToggleFlashLightOptions = false;

        [Toggle("Enable Flashlight Color", Id = "FlashLightColor")]
        public bool ToggleFlashLightColor = false;

        [Slider("FlashLight Brightness", 0.000f, 1.999f, DefaultValue = 0.9f,
            Id = "FlashLightBrightness", Step = 0.001f, Format = "{0:F3}")]
        public float LightBrightness = 1.0f;

        [Slider("FlashLight Range", 40f, 100f, DefaultValue = 50f,
            Id = "FlashLightRange", Step = 1f)]
        public float LightRange = 50f;

        // Color values — persisted but not shown in menu (set via HSV picker)
        public float LightRed = 1.000f;
        public float LightGreen = 1.000f;
        public float LightBlue = 1.000f;

        public Color LightColor => new Color(LightRed, LightGreen, LightBlue);

        [Button("Pick Light Color")]
        public void OnPickLightColor(ButtonClickedEventArgs e)
        {
            ColorPickerPanel.Instance.Show("flashlight", LightColor, (id, color) =>
            {
                LightRed = color.r;
                LightGreen = color.g;
                LightBlue = color.b;
                Save();
            });
        }
    }
}
