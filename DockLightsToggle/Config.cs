using Nautilus.Json;
using Nautilus.Options.Attributes;

namespace DockLightsToggle
{
    [Menu("Dock Lights Toggle")]
    public class Config : ConfigFile
    {
        [Toggle("Restore SeaTruck lights on undock")]
        public bool RestoreSeaTruckLights = true;
    }
}
