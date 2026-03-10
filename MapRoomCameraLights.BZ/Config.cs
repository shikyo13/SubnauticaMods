using Nautilus.Json;
using Nautilus.Options;
using Nautilus.Options.Attributes;

namespace MapRoomCameraLights.BZ
{
    [Menu("Scanner Room Camera Lights", SaveOn = MenuAttribute.SaveEvents.ChangeValue, LoadOn = MenuAttribute.LoadEvents.MenuRegistered)]
    public class Config : ConfigFile
    {
        [Toggle("Scanner Room Camera Lights", Id = "ToggleScannerRoomLights"), OnChange(nameof(CheckboxToggleEvent))]
        public bool ToggleScannerRoomCameraLights = true;

        [Slider("Scanner Room Camera Light Brightness", 0.000f, 1.999f, DefaultValue = 0.9f, Id = "ScannerCameraLightBrightness", Step = 0.001f, Format = "{0:F3}"), OnChange(nameof(SliderChangeEvent))]
        public float MapIntensity = 0.9f;

        [Slider("Scanner Room Camera Light Range", 40, 100, DefaultValue = 40, Id = "ScannerCameraLightRange"), OnChange(nameof(SliderChangeEvent))]
        public float MapRange = 40;

        [Slider("Scanner Room Camera Light Cone Size", 70, 120, DefaultValue = 70, Id = "ScannerCameraLightConeSize"), OnChange(nameof(SliderChangeEvent))]
        public float MapspotAngle = 70;

        private void CheckboxToggleEvent(ToggleChangedEventArgs e)
        {
            switch (e.Id)
            {
                case "ToggleScannerRoomLights":
                    break;
            }
        }

        private void SliderChangeEvent(SliderChangedEventArgs e)
        {
            switch (e.Id)
            {
                case "ScannerCameraLightBrightness":
                    break;
                case "ScannerCameraLightRange":
                    break;
                case "ScannerCameraLightConeSize":
                    break;
            }
        }
    }
}
