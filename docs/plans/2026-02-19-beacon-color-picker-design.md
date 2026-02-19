# BeaconColorPicker Design

**Date:** 2026-02-19
**Status:** Approved

## Problem

Subnautica only offers ~5 preset colors for beacons/pings. The color dots in the PDA ping manager UI cycle through a fixed palette (`PingManager.colorOptions`). Players want full color freedom for organizing their beacons.

## Solution

A BepInEx 5 Harmony mod that adds an HSV color picker popup to the PDA ping manager, allowing any arbitrary color for beacons and signals.

## How the Existing System Works

### Color Pipeline

```
PingManager.colorOptions (static Color[])
  -> PingInstance.colorIndex (int)
    -> PingManager.NotifyColor()
      -> uGUI_Ping.SetColor(Color) on HUD
```

### UI Components

- `uGUI_PingEntry` — each row in the PDA ping list
  - `colorSelectors` (`Toggle[]`) — the color dot toggle buttons
  - `SetColor0()` through `SetColor4()` — hardcoded callbacks for 5 toggles
  - `SetColor(int index)` — private, calls `PingManager.SetColor(id, colorIndex)`
  - `colorSelectionIndicator` (`RectTransform`) — shows which color is selected
  - `Initialize(string id, bool visible, PingType type, string name, int colorIndex)` — sets up the entry

- `uGUI_PingTab` — the PDA ping manager tab
  - `entries` (`Dictionary<string, uGUI_PingEntry>`) — all ping entries
  - `prefabEntry` (`GameObject`) — prefab for new entries
  - `UpdateEntries()` — rebuilds the entry list (600 IL bytes, complex)

- `uGUI_Ping` — the HUD ping element
  - `SetColor(Color color)` — takes an actual Color (not an index!)
  - `_iconColor`, `_textColor` — stored colors
  - `UpdateIconColor()`, `UpdateTextColor()` — apply colors to visuals

- `PingManager` — static manager
  - `colorOptions` (`static readonly Color[]`) — the preset palette
  - `NotifyColor(PingInstance)` — fires `onColor` event with the instance
  - `SetColor(string id, int colorIndex)` — sets color on a ping by ID

- `uGUI_SignInput` — in-world beacon/sign editor
  - `colors` (`Color[]`) — color palette for the sign
  - `_colorIndex` (int) — current selected index
  - `ToggleColor()` — cycles to next color
  - `UpdateColor()` — applies selected color to UI elements

### Key Insight

`uGUI_Ping.SetColor` already accepts a `Color` object, not an index. The HUD will render any arbitrary color — no HUD patches needed. We only need to intercept the `colorIndex -> Color` resolution point.

## Mod Design

### Architecture

```
BeaconColorPicker/
├── BeaconColorPickerPlugin.cs   # Plugin entry, config loading, Harmony setup
├── ColorPickerPanel.cs          # Runtime HSV slider UI panel
├── Patches.cs                   # All Harmony patches
├── CustomColorStore.cs          # Per-ping color persistence (JSON)
├── Properties/
│   └── AssemblyInfo.cs
├── BeaconColorPicker.csproj
├── BeaconColorPicker.sln
└── README.md
```

### Component 1: CustomColorStore

A static dictionary `Dictionary<string, Color>` mapping ping IDs to custom colors. Serialized to/from `BepInEx/config/com.adam.beaconcolorpicker.json`.

```csharp
public static class CustomColorStore
{
    private static Dictionary<string, Color> _colors = new();

    public static bool TryGetColor(string pingId, out Color color);
    public static void SetColor(string pingId, Color color);
    public static void RemoveColor(string pingId);
    public static void Save();
    public static void Load();
}
```

JSON format:
```json
{
  "ping_abc123": { "r": 1.0, "g": 0.42, "b": 0.21, "a": 1.0 },
  "ping_def456": { "r": 0.2, "g": 0.8, "b": 0.5, "a": 1.0 }
}
```

### Component 2: ColorPickerPanel

A singleton Unity panel created at runtime with:
- H slider (0-360 hue, rainbow gradient background)
- S slider (0-1 saturation)
- V slider (0-1 value/brightness)
- Color preview swatch
- Apply/Close buttons

Built from Unity UI primitives (`GameObject`, `Slider`, `Image`, `Button`) — no external assets needed. Created once, repositioned and shown/hidden per entry.

### Component 3: Patches

**Patch 1: `uGUI_PingEntry.Initialize` (Postfix)**
- After the entry initializes, add a "+" toggle button to the `colorSelectors` row
- Wire its click to show the ColorPickerPanel positioned near that entry
- If the ping has a custom color in the store, visually indicate it (e.g., tint the "+" button with the custom color)

**Patch 2: `PingManager.NotifyColor` (Postfix)**
- After the vanilla color notification fires, check if the ping has a custom color
- If yes, find the corresponding `uGUI_Ping` HUD element and call `SetColor()` with the custom color, overriding the palette color
- This is the core patch that makes custom colors appear on the HUD

**Patch 3: `uGUI_PingEntry.SetColor` (Postfix)**
- When user selects a preset color dot, remove any custom color for that ping
- This ensures preset dots still work and clear custom colors

**Patch 4: `uGUI_SignInput.UpdateColor` (Postfix) [stretch goal]**
- Apply custom color in the in-world beacon editor
- Lower priority — the PDA is the primary interface

### Data Flow

```
User clicks "+" on ping entry
  -> ColorPickerPanel opens with current color (custom or preset)
  -> User adjusts H/S/V sliders
  -> Preview swatch updates in real-time
  -> User clicks Apply
    -> CustomColorStore.SetColor(pingId, color)
    -> CustomColorStore.Save() (writes JSON)
    -> PingManager.NotifyColor() called to update HUD
    -> ColorPickerPanel hides
```

```
Game loads / PDA opens
  -> Plugin.Awake() calls CustomColorStore.Load()
  -> uGUI_PingEntry.Initialize patches check store for custom colors
  -> PingManager.NotifyColor patches override HUD colors
```

### Persistence

- Custom colors stored in `BepInEx/config/com.adam.beaconcolorpicker.json`
- Separate from game saves — colors persist across save files
- Loading: Plugin `Awake()` reads JSON at startup
- Saving: After every color change via the picker
- Cleanup: When a preset color is selected, the custom entry is removed

### Compatibility

- `PingInstance.colorIndex` is untouched — the game still stores its int index normally
- Custom colors are a separate overlay — if the mod is removed, beacons revert to whatever their last `colorIndex` was
- Save files are not modified

## Research Notes

Key types from Assembly-CSharp.dll:

- `PingManager.colorOptions` — `static readonly Color[]`, the preset palette
- `PingInstance.colorIndex` — `int`, public, stores selected color index
- `PingInstance.SetColor(int index)` — sets index, calls NotifyColor
- `uGUI_PingEntry.colorSelectors` — `Toggle[]`, the color dot toggles
- `uGUI_PingEntry.SetColor(int index)` — private, resolves color and updates
- `uGUI_PingEntry.SetColor0..4()` — hardcoded per-toggle callbacks
- `uGUI_PingEntry.Initialize(string, bool, PingType, string, int)` — entry setup
- `uGUI_Ping.SetColor(Color)` — HUD accepts raw Color (not index!)
- `uGUI_SignInput.colors` — `Color[]`, sign editor palette
- `uGUI_SignInput._colorIndex` — int, current sign color
- `uGUI_SignInput.ToggleColor()` — cycles sign colors
- `uGUI_SignInput.UpdateColor()` — applies color to sign UI
- `ColoredLabel.colorIndex` — int, used by beacon labels

## Complexity

This is significantly larger than PowerSaver or CameraStalkerGuard:
- ~300-500 lines of C# across 4 source files
- 3-4 Harmony patches
- Runtime Unity UI construction
- Custom JSON persistence
- UI state management (panel show/hide, slider callbacks)
