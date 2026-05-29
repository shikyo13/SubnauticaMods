using Nautilus.Json;
using Nautilus.Options.Attributes;

namespace PowerSaver.BZ
{
    [Menu("PowerSaver BZ")]
    public class PowerSaverConfig : ConfigFile
    {
        public string ConfigVersion = "";

        [Slider("Global Drain Multiplier", 0.01f, 2.0f, DefaultValue = 0.75f, Step = 0.01f,
            Format = "{0:P0}", Tooltip = "Baseline multiplier for supported power drain. Vehicle and base multipliers stack with this value.")]
        public float DrainMultiplier = 0.75f;

        [Slider("Vehicle Drain Multiplier", 0.01f, 2.0f, DefaultValue = 1.0f, Step = 0.01f,
            Format = "{0:P0}", Tooltip = "Additional vehicle multiplier (Seatruck, Prawn, etc.). Effective vehicle drain is Global x Vehicle.")]
        public float VehicleDrainMultiplier = 1.0f;

        [Slider("Base Drain Multiplier", 0.01f, 2.0f, DefaultValue = 1.0f, Step = 0.01f,
            Format = "{0:P0}", Tooltip = "Additional habitat power relay multiplier. Effective base drain is Global x Base.")]
        public float BaseDrainMultiplier = 1.0f;

        [Toggle("Enable Debug Logging",
            Tooltip = "Log power drain events to console (noisy, for debugging only).")]
        public bool EnableLogging = false;
    }
}
