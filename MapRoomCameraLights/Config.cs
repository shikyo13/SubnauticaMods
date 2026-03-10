using Nautilus.Json;
using Nautilus.Options.Attributes;

namespace MapRoomCameraLights
{
    [Menu("Map Room Camera Lights")]
    public class Config : ConfigFile
    {
        [Slider("Light Brightness", 0.000f, 1.999f, DefaultValue = 0.9f, Step = 0.001f,
            Format = "{0:F3}", Tooltip = "Controls the intensity of the scanner room camera lights.")]
        public float MapIntensity = 0.9f;

        [Slider("Light Range", 40f, 100f, DefaultValue = 40f, Step = 1f,
            Format = "{0:F0}", Tooltip = "How far the camera lights reach.")]
        public float MapRange = 40f;

        [Slider("Light Cone Size", 70f, 130f, DefaultValue = 70f, Step = 1f,
            Format = "{0:F0}", Tooltip = "The angle of the camera light cone.")]
        public float MapspotAngle = 70f;
    }
}
