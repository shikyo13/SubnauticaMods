# BeaconColorPicker - Subnautica Beacon Color Customization

A BepInEx 5 mod that adds an HSV color picker to Subnautica's PDA ping manager, allowing any arbitrary color for beacons and signals.

## What It Does

Replaces the limited 5-color preset palette with a full-spectrum HSV color picker:

- A new "+" button appears after the color dots in each ping entry
- Clicking it opens an HSV slider panel (Hue, Saturation, Value/Brightness)
- The hue slider has a rainbow gradient background for easy navigation
- Preview swatch shows the selected color in real-time
- Click Apply to set the color, Close to cancel
- Custom colors persist across game sessions (saved to JSON)
- Selecting a preset color dot clears any custom color for that ping
- Custom colors display correctly on the HUD

## Building

### Prerequisites
- .NET SDK 6.0+ or Visual Studio 2022
- Subnautica with BepInEx 5 installed

### Steps

1. Verify `SubnauticaDir` in `BeaconColorPicker.csproj` points to your Subnautica install.

2. Build:
   ```
   dotnet build --configuration Release
   ```

3. The build auto-copies `BeaconColorPicker.dll` to `BepInEx\plugins\BeaconColorPicker\`.

4. Launch Subnautica and check `BepInEx\LogOutput.log` for:
   ```
   [Info : BeaconColorPicker] BeaconColorPicker v1.0.0 loaded! Custom beacon colors enabled.
   ```

## Configuration

Custom colors are stored in `BepInEx/config/com.adam.beaconcolorpicker.json`. This file is separate from game saves — colors persist across save files. If you uninstall the mod, beacons revert to their last preset color.

## Compatibility

- Subnautica (Steam, current build as of Feb 2026)
- BepInEx 5.4.x
- Should not conflict with other mods unless they also patch `uGUI_Pings.OnColor`, `uGUI_Pings.OnAdd`, `uGUI_PingEntry.SetColor`, or `uGUI_PingEntry.Initialize`
