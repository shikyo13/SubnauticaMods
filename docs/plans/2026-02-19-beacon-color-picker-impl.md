# BeaconColorPicker Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Create a BepInEx 5 mod that adds an HSV color picker popup to Subnautica's PDA ping manager, allowing any arbitrary color for beacons/signals.

**Architecture:** 4 source files: `CustomColorStore` (JSON persistence), `ColorPickerPanel` (runtime Unity UI), `Patches` (4 Harmony patches for hooking into the color pipeline), and `BeaconColorPickerPlugin` (entry point). Custom colors are stored as a `Dictionary<string, Color>` serialized to JSON, keyed by ping ID. The HUD already accepts raw `Color` objects via `uGUI_Ping.SetColor(Color)`, so we only need to intercept the `colorIndex -> Color` resolution points.

**Tech Stack:** C# / .NET 4.7.2 / BepInEx 5 / HarmonyLib / Newtonsoft.Json / Unity uGUI

---

### Task 1: Create project scaffolding

**Files:**
- Create: `BeaconColorPicker/BeaconColorPicker.csproj`
- Create: `BeaconColorPicker/BeaconColorPicker.sln`
- Create: `BeaconColorPicker/Properties/AssemblyInfo.cs`

**Step 1: Create the .csproj**

Create `BeaconColorPicker/BeaconColorPicker.csproj`. Same template as `CameraStalkerGuard/CameraStalkerGuard.csproj` but with BeaconColorPicker names and **additional references** for Unity UI and Newtonsoft.Json:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net472</TargetFramework>
    <AssemblyName>BeaconColorPicker</AssemblyName>
    <RootNamespace>BeaconColorPicker</RootNamespace>
    <LangVersion>latest</LangVersion>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>

    <!-- Suppress warnings about referencing game DLLs -->
    <NoWarn>$(NoWarn);CS0436</NoWarn>
  </PropertyGroup>

  <!--
    IMPORTANT: Update these paths to match YOUR Subnautica install location.
    Default Steam path shown below. Change if yours differs.
  -->
  <PropertyGroup>
    <SubnauticaDir>D:\SteamLibrary\steamapps\common\Subnautica</SubnauticaDir>
    <BepInExDir>$(SubnauticaDir)\BepInEx</BepInExDir>
    <ManagedDir>$(SubnauticaDir)\Subnautica_Data\Managed</ManagedDir>
  </PropertyGroup>

  <!-- BepInEx references -->
  <ItemGroup>
    <Reference Include="BepInEx">
      <HintPath>$(BepInExDir)\core\BepInEx.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="0Harmony">
      <HintPath>$(BepInExDir)\core\0Harmony.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>

  <!-- Game references -->
  <ItemGroup>
    <Reference Include="Assembly-CSharp">
      <HintPath>$(ManagedDir)\Assembly-CSharp.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="Assembly-CSharp-firstpass">
      <HintPath>$(ManagedDir)\Assembly-CSharp-firstpass.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>

  <!-- Unity references -->
  <ItemGroup>
    <Reference Include="UnityEngine">
      <HintPath>$(ManagedDir)\UnityEngine.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="UnityEngine.CoreModule">
      <HintPath>$(ManagedDir)\UnityEngine.CoreModule.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="UnityEngine.UI">
      <HintPath>$(ManagedDir)\UnityEngine.UI.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="UnityEngine.UIModule">
      <HintPath>$(ManagedDir)\UnityEngine.UIModule.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>

  <!-- JSON serialization -->
  <ItemGroup>
    <Reference Include="Newtonsoft.Json">
      <HintPath>$(ManagedDir)\Newtonsoft.Json.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>

  <!-- Auto-copy to plugins folder after build -->
  <Target Name="CopyToPlugins" AfterTargets="Build">
    <Copy
      SourceFiles="$(OutputPath)$(AssemblyName).dll"
      DestinationFolder="$(BepInExDir)\plugins\BeaconColorPicker"
      SkipUnchangedFiles="true" />
    <Message Text="Copied $(AssemblyName).dll to BepInEx plugins folder" Importance="High" />
  </Target>

</Project>
```

**Step 2: Create the .sln**

Create `BeaconColorPicker/BeaconColorPicker.sln`. Generate a new GUID with `uuidgen`.

```
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.5.2.0
MinimumVisualStudioVersion = 10.0.40219.1
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "BeaconColorPicker", "BeaconColorPicker.csproj", "{NEW-GUID-HERE}"
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{NEW-GUID-HERE}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{NEW-GUID-HERE}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{NEW-GUID-HERE}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{NEW-GUID-HERE}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
EndGlobal
```

**Step 3: Create AssemblyInfo.cs**

Create `BeaconColorPicker/Properties/AssemblyInfo.cs`:

```csharp
using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("BeaconColorPicker")]
[assembly: AssemblyDescription("HSV color picker for beacon and signal pings")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("BeaconColorPicker")]
[assembly: AssemblyCopyright("Adam 2026")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
```

**Step 4: Commit**

```bash
git add BeaconColorPicker/BeaconColorPicker.csproj BeaconColorPicker/BeaconColorPicker.sln BeaconColorPicker/Properties/AssemblyInfo.cs
git commit -m "feat(BeaconColorPicker): add project scaffolding"
```

---

### Task 2: Implement CustomColorStore

**Files:**
- Create: `BeaconColorPicker/CustomColorStore.cs`

**Context:** This is the persistence layer. It stores custom colors as a `Dictionary<string, SerializableColor>` keyed by ping ID (the `_id` field of `PingInstance`). Uses `Newtonsoft.Json` for serialization to `BepInEx/config/com.adam.beaconcolorpicker.json`.

**Step 1: Write CustomColorStore.cs**

Create `BeaconColorPicker/CustomColorStore.cs`:

```csharp
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace BeaconColorPicker
{
    public static class CustomColorStore
    {
        private static Dictionary<string, SerializableColor> _colors = new Dictionary<string, SerializableColor>();

        private static string FilePath => Path.Combine(
            BepInEx.Paths.ConfigPath, "com.adam.beaconcolorpicker.json");

        public static bool TryGetColor(string pingId, out Color color)
        {
            if (pingId != null && _colors.TryGetValue(pingId, out var sc))
            {
                color = sc.ToColor();
                return true;
            }
            color = default;
            return false;
        }

        public static void SetColor(string pingId, Color color)
        {
            _colors[pingId] = new SerializableColor(color);
        }

        public static void RemoveColor(string pingId)
        {
            _colors.Remove(pingId);
        }

        public static void Save()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_colors, Formatting.Indented);
                File.WriteAllText(FilePath, json);
            }
            catch (System.Exception ex)
            {
                BeaconColorPickerPlugin.Log?.LogWarning($"Failed to save custom colors: {ex.Message}");
            }
        }

        public static void Load()
        {
            if (!File.Exists(FilePath)) return;
            try
            {
                var json = File.ReadAllText(FilePath);
                _colors = JsonConvert.DeserializeObject<Dictionary<string, SerializableColor>>(json)
                    ?? new Dictionary<string, SerializableColor>();
            }
            catch (System.Exception ex)
            {
                BeaconColorPickerPlugin.Log?.LogWarning($"Failed to load custom colors: {ex.Message}");
                _colors = new Dictionary<string, SerializableColor>();
            }
        }

        private struct SerializableColor
        {
            public float r, g, b, a;

            public SerializableColor(Color c)
            {
                r = c.r;
                g = c.g;
                b = c.b;
                a = c.a;
            }

            public Color ToColor()
            {
                return new Color(r, g, b, a);
            }
        }
    }
}
```

**Step 2: Commit**

```bash
git add BeaconColorPicker/CustomColorStore.cs
git commit -m "feat(BeaconColorPicker): implement CustomColorStore with JSON persistence"
```

---

### Task 3: Implement ColorPickerPanel

**Files:**
- Create: `BeaconColorPicker/ColorPickerPanel.cs`

**Context:** This is the runtime Unity UI panel with HSV sliders. It's a singleton `MonoBehaviour` that creates a `ScreenSpaceOverlay` canvas. It has 3 sliders (H/S/V), a color preview swatch, and Apply/Close buttons. Built entirely from Unity UI primitives — no external assets.

**Important references:**
- `UnityEngine.UI.Slider` — requires child hierarchy: Background, Fill Area > Fill, Handle Slide Area > Handle
- `UnityEngine.UI.Image` — colored rectangles
- `UnityEngine.UI.Button` — clickable button
- `UnityEngine.UI.Text` — labels (uses built-in Arial font)
- `Canvas` + `CanvasScaler` + `GraphicRaycaster` — rendering pipeline for overlay UI
- `Color.HSVToRGB(h, s, v)` and `Color.RGBToHSV(color, out h, out s, out v)` — Unity built-in HSV conversion

**Step 1: Write ColorPickerPanel.cs**

Create `BeaconColorPicker/ColorPickerPanel.cs`:

```csharp
using System;
using UnityEngine;
using UnityEngine.UI;

namespace BeaconColorPicker
{
    public class ColorPickerPanel : MonoBehaviour
    {
        private static ColorPickerPanel _instance;

        private GameObject _panelRoot;
        private Slider _hueSlider;
        private Slider _satSlider;
        private Slider _valSlider;
        private Image _previewSwatch;
        private Image _hueBackground;

        private string _currentPingId;
        private Action<string, Color> _onApply;

        public static ColorPickerPanel Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("BeaconColorPickerPanel");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<ColorPickerPanel>();
                    _instance.BuildUI();
                }
                return _instance;
            }
        }

        public bool IsVisible => _panelRoot != null && _panelRoot.activeSelf;

        public void Show(string pingId, Color currentColor, Action<string, Color> onApply)
        {
            _currentPingId = pingId;
            _onApply = onApply;

            Color.RGBToHSV(currentColor, out float h, out float s, out float v);
            _hueSlider.SetValueWithoutNotify(h);
            _satSlider.SetValueWithoutNotify(s);
            _valSlider.SetValueWithoutNotify(v);
            UpdatePreview();

            _panelRoot.SetActive(true);
        }

        public void Hide()
        {
            if (_panelRoot != null)
                _panelRoot.SetActive(false);
        }

        private void Update()
        {
            // Auto-close if PDA is no longer open
            if (IsVisible)
            {
                bool pdaOpen = Player.main != null
                    && Player.main.GetPDA() != null
                    && Player.main.GetPDA().isOpen;
                if (!pdaOpen)
                    Hide();
            }
        }

        private Color CurrentColor =>
            Color.HSVToRGB(_hueSlider.value, _satSlider.value, _valSlider.value);

        private void UpdatePreview()
        {
            if (_previewSwatch != null)
                _previewSwatch.color = CurrentColor;
        }

        private void OnApplyClicked()
        {
            _onApply?.Invoke(_currentPingId, CurrentColor);
            Hide();
        }

        private void BuildUI()
        {
            // Root canvas — ScreenSpaceOverlay, on top of PDA
            var canvasGo = new GameObject("ColorPickerCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            // Panel background
            _panelRoot = new GameObject("Panel");
            _panelRoot.transform.SetParent(canvasGo.transform, false);
            var panelRt = _panelRoot.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(320, 300);
            panelRt.anchoredPosition = new Vector2(300, 0);
            var panelImg = _panelRoot.AddComponent<Image>();
            panelImg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

            // Title
            CreateLabel(_panelRoot.transform, "Color Picker", new Vector2(0, 125), 20, TextAnchor.MiddleCenter);

            // Hue slider with rainbow background
            CreateLabel(_panelRoot.transform, "H", new Vector2(-135, 75), 16, TextAnchor.MiddleLeft);
            _hueSlider = CreateSlider(_panelRoot.transform, new Vector2(15, 75), 0f, 1f);
            _hueSlider.onValueChanged.AddListener(_ => UpdatePreview());
            _hueBackground = CreateHueGradient(_hueSlider);

            // Saturation slider
            CreateLabel(_panelRoot.transform, "S", new Vector2(-135, 30), 16, TextAnchor.MiddleLeft);
            _satSlider = CreateSlider(_panelRoot.transform, new Vector2(15, 30), 0f, 1f);
            _satSlider.onValueChanged.AddListener(_ => UpdatePreview());

            // Value/brightness slider
            CreateLabel(_panelRoot.transform, "V", new Vector2(-135, -15), 16, TextAnchor.MiddleLeft);
            _valSlider = CreateSlider(_panelRoot.transform, new Vector2(15, -15), 0f, 1f);
            _valSlider.onValueChanged.AddListener(_ => UpdatePreview());

            // Preview swatch
            CreateLabel(_panelRoot.transform, "Preview", new Vector2(0, -52), 14, TextAnchor.MiddleCenter);
            var swatchGo = new GameObject("PreviewSwatch");
            swatchGo.transform.SetParent(_panelRoot.transform, false);
            var swatchRt = swatchGo.AddComponent<RectTransform>();
            swatchRt.anchoredPosition = new Vector2(0, -80);
            swatchRt.sizeDelta = new Vector2(240, 30);
            _previewSwatch = swatchGo.AddComponent<Image>();
            _previewSwatch.color = Color.white;

            // Buttons
            CreateButton(_panelRoot.transform, "Apply", new Vector2(-60, -125), new Color(0.2f, 0.6f, 0.2f, 1f), OnApplyClicked);
            CreateButton(_panelRoot.transform, "Close", new Vector2(60, -125), new Color(0.5f, 0.2f, 0.2f, 1f), Hide);

            _panelRoot.SetActive(false);
        }

        private Image CreateHueGradient(Slider slider)
        {
            // Create a rainbow texture for the hue slider background
            var bgTransform = slider.transform.Find("Background");
            if (bgTransform == null) return null;

            var bgImage = bgTransform.GetComponent<Image>();
            if (bgImage == null) return null;

            var tex = new Texture2D(256, 1);
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int i = 0; i < 256; i++)
            {
                tex.SetPixel(i, 0, Color.HSVToRGB(i / 255f, 1f, 1f));
            }
            tex.Apply();

            bgImage.sprite = Sprite.Create(tex, new Rect(0, 0, 256, 1), new Vector2(0.5f, 0.5f));
            bgImage.type = Image.Type.Simple;
            bgImage.color = Color.white;

            return bgImage;
        }

        private void CreateLabel(Transform parent, string text, Vector2 position, int fontSize, TextAnchor alignment)
        {
            var go = new GameObject($"Label_{text}");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(280, 25);
            var txt = go.AddComponent<Text>();
            txt.text = text;
            txt.fontSize = fontSize;
            txt.color = Color.white;
            txt.alignment = alignment;
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private Slider CreateSlider(Transform parent, Vector2 position, float min, float max)
        {
            // Root
            var sliderGo = new GameObject("Slider");
            sliderGo.transform.SetParent(parent, false);
            var sliderRt = sliderGo.AddComponent<RectTransform>();
            sliderRt.anchoredPosition = position;
            sliderRt.sizeDelta = new Vector2(230, 20);

            // Background
            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(sliderGo.transform, false);
            var bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.25f, 0.25f, 0.25f, 1f);

            // Fill Area
            var fillAreaGo = new GameObject("Fill Area");
            fillAreaGo.transform.SetParent(sliderGo.transform, false);
            var fillAreaRt = fillAreaGo.AddComponent<RectTransform>();
            fillAreaRt.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRt.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRt.offsetMin = new Vector2(5f, 0f);
            fillAreaRt.offsetMax = new Vector2(-5f, 0f);

            // Fill
            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(fillAreaGo.transform, false);
            var fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.sizeDelta = new Vector2(0f, 0f);
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = new Color(0.4f, 0.7f, 1f, 1f);

            // Handle Slide Area
            var handleAreaGo = new GameObject("Handle Slide Area");
            handleAreaGo.transform.SetParent(sliderGo.transform, false);
            var handleAreaRt = handleAreaGo.AddComponent<RectTransform>();
            handleAreaRt.anchorMin = Vector2.zero;
            handleAreaRt.anchorMax = Vector2.one;
            handleAreaRt.offsetMin = new Vector2(10f, 0f);
            handleAreaRt.offsetMax = new Vector2(-10f, 0f);

            // Handle
            var handleGo = new GameObject("Handle");
            handleGo.transform.SetParent(handleAreaGo.transform, false);
            var handleRt = handleGo.AddComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(16f, 0f);
            var handleImg = handleGo.AddComponent<Image>();
            handleImg.color = Color.white;

            // Wire up the Slider component
            var slider = sliderGo.AddComponent<Slider>();
            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.targetGraphic = handleImg;
            slider.minValue = min;
            slider.maxValue = max;
            slider.direction = Slider.Direction.LeftToRight;
            slider.wholeNumbers = false;

            return slider;
        }

        private void CreateButton(Transform parent, string label, Vector2 position, Color bgColor, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject($"Button_{label}");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(100, 35);

            var img = go.AddComponent<Image>();
            img.color = bgColor;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            // Button label
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;
            var txt = textGo.AddComponent<Text>();
            txt.text = label;
            txt.fontSize = 16;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
```

**Step 2: Commit**

```bash
git add BeaconColorPicker/ColorPickerPanel.cs
git commit -m "feat(BeaconColorPicker): implement ColorPickerPanel with HSV sliders"
```

---

### Task 4: Implement Harmony patches

**Files:**
- Create: `BeaconColorPicker/Patches.cs`

**Context:** There are 4 patches that integrate custom colors into the game's color pipeline:

1. **`uGUI_Pings.OnColor` Prefix** — When the game resolves a color for the HUD, override it with the custom color if one exists. This is the core patch for real-time color changes.

2. **`uGUI_Pings.OnAdd` Postfix** — When a HUD ping element is first created (game load, new beacon placed), apply the custom color. This is needed because `OnAdd` calls `SetColor` directly, bypassing `OnColor`.

3. **`uGUI_PingEntry.SetColor` Prefix** — When the user clicks a preset color dot, remove the custom color BEFORE the vanilla method runs. This must be a Prefix (not Postfix) because `SetColor` internally calls `PingManager.SetColor` → `NotifyColor` → `OnColor`, and our `OnColor` prefix would re-apply the custom color if it still existed.

4. **`uGUI_PingEntry.Initialize` Postfix** — After a PDA ping entry is created, add a "+" toggle button to the color selector row. Clicking it opens the ColorPickerPanel.

**Important game API details (from assembly inspection):**
- `uGUI_PingEntry.id` — private `string` field, stores the ping ID
- `uGUI_PingEntry.colorSelectors` — public `Toggle[]` field, the color dot buttons
- `uGUI_Pings.pings` — private `Dictionary<string, uGUI_Ping>` field
- `uGUI_Pings.OnColor(string id, Color color)` — private, called via `PingManager.onColor` delegate
- `uGUI_Pings.OnAdd(PingInstance instance)` — private, called via `PingManager.onAdd` delegate
- `PingInstance._id` — public `string` field
- `PingInstance.colorIndex` — public `int` field
- `PingManager.colorOptions` — public static readonly `Color[]`
- `PingManager.NotifyColor(PingInstance)` — public static method
- `PingManager.Get(string id)` — public static, returns `PingInstance`

**Step 1: Write Patches.cs**

Create `BeaconColorPicker/Patches.cs`:

```csharp
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace BeaconColorPicker
{
    /// <summary>
    /// Overrides HUD ping color when the game fires a color change event.
    /// uGUI_Pings.OnColor is called (via PingManager.onColor delegate) whenever
    /// a ping's color changes. We modify the color parameter in-place before the
    /// original method passes it to uGUI_Ping.SetColor.
    /// </summary>
    [HarmonyPatch(typeof(uGUI_Pings), "OnColor")]
    internal static class uGUI_Pings_OnColor_Patch
    {
        [HarmonyPrefix]
        static void Prefix(string id, ref Color color)
        {
            if (CustomColorStore.TryGetColor(id, out Color customColor))
            {
                color = customColor;
            }
        }
    }

    /// <summary>
    /// Applies custom color when a HUD ping is first created.
    /// uGUI_Pings.OnAdd directly calls uGUI_Ping.SetColor with the palette color
    /// before adding the ping to its dictionary. Our postfix runs after the method
    /// completes (after the ping is in the dictionary), and overrides the color.
    /// </summary>
    [HarmonyPatch(typeof(uGUI_Pings), "OnAdd")]
    internal static class uGUI_Pings_OnAdd_Patch
    {
        [HarmonyPostfix]
        static void Postfix(uGUI_Pings __instance, PingInstance instance)
        {
            if (instance == null) return;
            if (!CustomColorStore.TryGetColor(instance._id, out Color customColor)) return;

            var pingsDict = Traverse.Create(__instance).Field("pings")
                .GetValue<Dictionary<string, uGUI_Ping>>();
            if (pingsDict != null && pingsDict.TryGetValue(instance._id, out uGUI_Ping hudPing))
            {
                hudPing.SetColor(customColor);
            }
        }
    }

    /// <summary>
    /// Clears custom color when the user selects a preset color dot.
    /// This MUST be a Prefix because SetColor internally calls PingManager.SetColor
    /// → PingInstance.SetColor → PingManager.NotifyColor → onColor → our OnColor
    /// prefix. If the custom color still existed at that point, it would override
    /// the preset selection.
    /// </summary>
    [HarmonyPatch(typeof(uGUI_PingEntry), "SetColor")]
    internal static class uGUI_PingEntry_SetColor_Patch
    {
        [HarmonyPrefix]
        static void Prefix(uGUI_PingEntry __instance)
        {
            string pingId = Traverse.Create(__instance).Field("id").GetValue<string>();
            if (!string.IsNullOrEmpty(pingId))
            {
                CustomColorStore.RemoveColor(pingId);
                CustomColorStore.Save();
            }
        }
    }

    /// <summary>
    /// Adds a custom color "+" button to each ping entry in the PDA ping manager.
    /// Clones an existing color dot toggle, repositions it, and wires it to open
    /// the ColorPickerPanel.
    /// </summary>
    [HarmonyPatch(typeof(uGUI_PingEntry), "Initialize")]
    internal static class uGUI_PingEntry_Initialize_Patch
    {
        [HarmonyPostfix]
        static void Postfix(uGUI_PingEntry __instance, string id, int colorIndex)
        {
            Toggle[] toggles = __instance.colorSelectors;
            if (toggles == null || toggles.Length == 0) return;

            // Prevent duplicate buttons on re-initialization
            Transform existing = __instance.transform.Find("CustomColorButton");
            if (existing != null)
            {
                // Update the button color if a custom color exists
                UpdateButtonColor(existing.gameObject, id);
                return;
            }

            // Clone the last color toggle as our "+" button
            Toggle lastToggle = toggles[toggles.Length - 1];
            var newToggleGo = Object.Instantiate(lastToggle.gameObject, lastToggle.transform.parent);
            newToggleGo.name = "CustomColorButton";

            // Position after the last toggle
            var rt = newToggleGo.GetComponent<RectTransform>();
            var lastRt = lastToggle.GetComponent<RectTransform>();
            rt.anchoredPosition = lastRt.anchoredPosition + new Vector2(rt.sizeDelta.x + 4f, 0f);

            // Remove from toggle group so it doesn't interfere with preset selection
            var toggle = newToggleGo.GetComponent<Toggle>();
            toggle.group = null;
            toggle.isOn = false;

            // Set appearance — white by default, custom color if one exists
            UpdateButtonColor(newToggleGo, id);

            // Wire click to open color picker
            string pingId = id; // Capture for closure
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener(isOn =>
            {
                if (!isOn) return;

                // Get current color for the picker to start from
                Color currentColor;
                if (!CustomColorStore.TryGetColor(pingId, out currentColor))
                {
                    PingInstance ping = PingManager.Get(pingId);
                    int ci = ping != null ? ping.colorIndex : 0;
                    ci = Mathf.Clamp(ci, 0, PingManager.colorOptions.Length - 1);
                    currentColor = PingManager.colorOptions[ci];
                }

                // Show picker
                ColorPickerPanel.Instance.Show(pingId, currentColor, (pid, color) =>
                {
                    CustomColorStore.SetColor(pid, color);
                    CustomColorStore.Save();

                    // Trigger HUD update via the game's normal notification path
                    PingInstance p = PingManager.Get(pid);
                    if (p != null)
                        PingManager.NotifyColor(p);

                    // Update the + button appearance
                    UpdateButtonColor(newToggleGo, pid);
                });

                // Don't stay toggled on
                toggle.SetIsOnWithoutNotify(false);
            });
        }

        private static void UpdateButtonColor(GameObject buttonGo, string pingId)
        {
            Color displayColor = Color.white;
            if (CustomColorStore.TryGetColor(pingId, out Color customColor))
            {
                displayColor = customColor;
            }

            var images = buttonGo.GetComponentsInChildren<Image>();
            foreach (var img in images)
            {
                img.color = displayColor;
            }
        }
    }
}
```

**Step 2: Commit**

```bash
git add BeaconColorPicker/Patches.cs
git commit -m "feat(BeaconColorPicker): implement Harmony patches for color pipeline"
```

---

### Task 5: Implement plugin entry point

**Files:**
- Create: `BeaconColorPicker/BeaconColorPickerPlugin.cs`

**Step 1: Write BeaconColorPickerPlugin.cs**

Create `BeaconColorPicker/BeaconColorPickerPlugin.cs`:

```csharp
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace BeaconColorPicker
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class BeaconColorPickerPlugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "com.adam.beaconcolorpicker";
        public const string PLUGIN_NAME = "BeaconColorPicker";
        public const string PLUGIN_VERSION = "1.0.0";

        internal static ManualLogSource Log;

        private static Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            CustomColorStore.Load();

            _harmony = new Harmony(PLUGIN_GUID);
            _harmony.PatchAll();

            Log.LogInfo($"{PLUGIN_NAME} v{PLUGIN_VERSION} loaded! Custom beacon colors enabled.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
```

**Step 2: Commit**

```bash
git add BeaconColorPicker/BeaconColorPickerPlugin.cs
git commit -m "feat(BeaconColorPicker): implement plugin entry point"
```

---

### Task 6: Build and verify

**Step 1: Build**

```bash
dotnet build BeaconColorPicker/BeaconColorPicker.sln --configuration Release
```

Expected: Build succeeds with 0 errors. Output at `BeaconColorPicker/bin/Release/net472/BeaconColorPicker.dll`. Post-build copies to `BepInEx\plugins\BeaconColorPicker\`.

**Step 2: Fix build errors**

Likely issues and fixes:

- **`uGUI_Pings` not found**: The type is in `Assembly-CSharp.dll`. Ensure the reference is correct.
- **`Traverse` not found**: It's in `HarmonyLib` namespace, ensure `using HarmonyLib;` is present.
- **`Player.main.GetPDA()` not compiling**: `Player` is in the global namespace of `Assembly-CSharp.dll`. If it fails, try accessing `PDA` differently or wrap in a try-catch.
- **`PingManager.Get()` doesn't exist**: The method is `public static PingInstance Get(string id)` in `PingManager`. If the name differs, check with `AccessTools.Method(typeof(PingManager), "Get")`.
- **Toggle group issues**: If `toggle.group = null` doesn't compile (ToggleGroup is a reference type so null should work), use `toggle.group = default`.

**Step 3: Commit build fixes if needed**

```bash
git add -u
git commit -m "fix(BeaconColorPicker): fix build issues"
```

---

### Task 7: Add README

**Files:**
- Create: `BeaconColorPicker/README.md`

**Step 1: Write README**

Create `BeaconColorPicker/README.md`:

```markdown
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
```

**Step 2: Commit**

```bash
git add BeaconColorPicker/README.md
git commit -m "docs(BeaconColorPicker): add README"
```

---

### Task 8: Final push

**Step 1: Push to GitHub**

```bash
git push origin master
```
