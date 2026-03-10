using Nautilus.Json;
using Nautilus.Options;
using Nautilus.Options.Attributes;
using SubnauticaMods.Shared;
using UnityEngine;

namespace BetterFlashLight
{
    [Menu("BetterFlashLight", SaveOn = MenuAttribute.SaveEvents.ChangeValue,
          LoadOn = MenuAttribute.LoadEvents.MenuRegistered)]
    public class Config : ConfigFile
    {
        [Toggle("Enable Custom Flashlight", Id = "ToggleColor")]
        public bool ToggleColor = false;

        [Slider("Brightness", 0.000f, 1.999f, DefaultValue = 1.0f, Step = 0.001f, Format = "{0:F3}")]
        public float Intensity = 1.0f;

        [Slider("Range", 50f, 100f, DefaultValue = 50f)]
        public float Range = 50f;

        // Color values — persisted but not shown in menu (set via HSV picker)
        public float Red = 0.996f;
        public float Green = 0.942f;
        public float Blue = 0.819f;

        public Color LightColor => new Color(Red, Green, Blue);

        [Button("Pick Light Color")]
        public void OnPickLightColor(ButtonClickedEventArgs e)
        {
            ColorPickerPanel.Instance.Show("flashlight", LightColor, (id, color) =>
            {
                Red = color.r;
                Green = color.g;
                Blue = color.b;
                Save();
            });
        }
    }
}
