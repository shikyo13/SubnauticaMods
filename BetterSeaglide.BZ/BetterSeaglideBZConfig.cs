using Nautilus.Json;
using Nautilus.Options;
using Nautilus.Options.Attributes;
using SubnauticaMods.Shared;
using UnityEngine;

namespace BetterSeaglide.BZ
{
    [Menu("BetterSeaglide", SaveOn = MenuAttribute.SaveEvents.ChangeValue,
          LoadOn = MenuAttribute.LoadEvents.MenuRegistered)]
    public class BetterSeaglideBZConfig : ConfigFile
    {
        [Toggle("Enable Boost")]
        public bool ToggleBoost = true;

        [Slider("Boost Speed Addition", 0f, 20f, DefaultValue = 0f, Step = 0.5f)]
        public float BoostAddition = 0f;

        [Slider("Boost Speed Multiplier", 1.0f, 10.0f, DefaultValue = 2.0f, Step = 0.05f)]
        public float BoostMultiplier = 2.0f;

        [Toggle("Enable Light Color")]
        public bool ToggleLightColor = false;

        [Toggle("Enable Body Color")]
        public bool ToggleBodyColor = false;

        [Toggle("Enable Energy Bar Color")]
        public bool ToggleEnergyBarColor = false;

        // Color values — persisted but not shown in menu (set via HSV picker)
        public float LightColorR = 0.016f;
        public float LightColorG = 1.0f;
        public float LightColorB = 1.0f;

        public float BodyColorR = 1.0f;
        public float BodyColorG = 1.0f;
        public float BodyColorB = 1.0f;

        public float EnergyBarColorR = 0.0f;
        public float EnergyBarColorG = 1.0f;
        public float EnergyBarColorB = 0.0f;

        public Color LightColor => new Color(LightColorR, LightColorG, LightColorB);
        public Color BodyColor => new Color(BodyColorR, BodyColorG, BodyColorB);
        public Color EnergyBarColor => new Color(EnergyBarColorR, EnergyBarColorG, EnergyBarColorB);

        [Button("Pick Light Color")]
        public void OnPickLightColor(ButtonClickedEventArgs e)
        {
            ColorPickerPanel.Instance.Show("light", LightColor, (id, color) =>
            {
                LightColorR = color.r;
                LightColorG = color.g;
                LightColorB = color.b;
                Save();
            });
        }

        [Button("Pick Body Color")]
        public void OnPickBodyColor(ButtonClickedEventArgs e)
        {
            ColorPickerPanel.Instance.Show("body", BodyColor, (id, color) =>
            {
                BodyColorR = color.r;
                BodyColorG = color.g;
                BodyColorB = color.b;
                Save();
            });
        }

        [Button("Pick Energy Bar Color")]
        public void OnPickEnergyBarColor(ButtonClickedEventArgs e)
        {
            ColorPickerPanel.Instance.Show("energy", EnergyBarColor, (id, color) =>
            {
                EnergyBarColorR = color.r;
                EnergyBarColorG = color.g;
                EnergyBarColorB = color.b;
                Save();
            });
        }
    }
}
