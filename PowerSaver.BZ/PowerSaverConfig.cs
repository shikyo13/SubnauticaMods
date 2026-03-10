using Nautilus.Json;
using Nautilus.Options.Attributes;

namespace PowerSaver.BZ
{
    [Menu("PowerSaver BZ")]
    public class PowerSaverConfig : ConfigFile
    {
        [Slider("Global Drain Multiplier", 0.01f, 2.0f, DefaultValue = 0.75f, Step = 0.01f,
            Format = "{0:P0}", Tooltip = "Applies to all power drain. 0.75 = 25% less drain, 0.5 = 50% less, 1.0 = vanilla.")]
        public float DrainMultiplier = 0.75f;

        [Slider("Vehicle Drain Multiplier", 0.01f, 2.0f, DefaultValue = 0.75f, Step = 0.01f,
            Format = "{0:P0}", Tooltip = "Vehicle-specific multiplier (Seatruck, Prawn, etc.). Stacks with global. Set to 1.0 to only use global.")]
        public float VehicleDrainMultiplier = 0.75f;

        [Slider("Base Drain Multiplier", 0.01f, 2.0f, DefaultValue = 0.75f, Step = 0.01f,
            Format = "{0:P0}", Tooltip = "Habitat power relay multiplier. Set to 1.0 to only use global.")]
        public float BaseDrainMultiplier = 0.75f;

        [Toggle("Enable Debug Logging",
            Tooltip = "Log power drain events to console (noisy, for debugging only).")]
        public bool EnableLogging = false;
    }
}
