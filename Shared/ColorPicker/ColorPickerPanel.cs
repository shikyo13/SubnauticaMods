using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
namespace SubnauticaMods.Shared
{
    public class ColorPickerPanel : MonoBehaviour
    {
        private static readonly Color FocusRingColor = new Color(0.3f, 0.85f, 1f, 1f);

        private static ColorPickerPanel _instance;

        private GameObject _panelRoot;
        private Slider _hueSlider;
        private Slider _satSlider;
        private Slider _valSlider;
        private Image _previewSwatch;
        private Image _hueBackground;
        private Text _rgbLabel;
        private TMP_InputField _hexInput;
        private Button _applyButton;
        private Button _closeButton;

        private string _contextId;
        private Action<string, Color> _onApply;

        private NavigableGrid _navigableGrid;
        private uGUI_INavigableIconGrid _previousGrid;
        private GameObject _focusRing;
        private RectTransform _focusRingRect;

        /// <summary>
        /// Optional logger for warnings. Set by consuming mods.
        /// </summary>
        public static Action<string> LogWarning;

        public static ColorPickerPanel Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("ColorPickerPanel");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<ColorPickerPanel>();
                }
                return _instance;
            }
        }

        public bool IsVisible => _panelRoot != null && _panelRoot.activeSelf;

        public void Show(string contextId, Color currentColor, Action<string, Color> onApply)
        {
            if (_panelRoot == null)
                BuildUI();

            // Parent under the active game canvas so we inherit its
            // uGUI_GraphicRaycaster and uGUI_InputGroup. FPSInputModule
            // only routes events to elements inside the active input group.
            var targetCanvas = FindActiveCanvas();
            if (targetCanvas != null)
                _panelRoot.transform.SetParent(targetCanvas.transform, false);

            _contextId = contextId;
            _onApply = onApply;

            Color.RGBToHSV(currentColor, out float h, out float s, out float v);
            _hueSlider.SetValueWithoutNotify(h);
            _satSlider.SetValueWithoutNotify(s);
            _valSlider.SetValueWithoutNotify(v);
            UpdatePreview();

            _panelRoot.SetActive(true);

            // This panel's controls (sliders, hex field, buttons) aren't part
            // of any uGUI_INavigableIconGrid on their own -- this game's
            // controller navigation is driven entirely by whichever grid
            // GamepadInputModule currently holds. Seize it while the panel is
            // open and hand it back on close.
            if (GamepadInputModule.current != null)
            {
                if (_navigableGrid == null)
                    _navigableGrid = new NavigableGrid(this);
                _previousGrid = GamepadInputModule.current.GetCurrentGrid();
                _navigableGrid.SelectFirstItem();
                GamepadInputModule.current.SetCurrentGrid(_navigableGrid);
            }
        }

        private static Canvas FindActiveCanvas()
        {
            if (FPSInputModule.current != null && FPSInputModule.current.lastGroup != null)
            {
                var canvas = FPSInputModule.current.lastGroup.GetComponentInParent<Canvas>();
                if (canvas != null) return canvas;
            }
            return null;
        }

        public void Hide()
        {
            if (_panelRoot != null)
                _panelRoot.SetActive(false);

            HideFocusRing();

            if (GamepadInputModule.current != null && GamepadInputModule.current.GetCurrentGrid() == (object)_navigableGrid)
            {
                GamepadInputModule.current.SetCurrentGrid(_previousGrid);
                _previousGrid = null;
            }
        }

        private void Update()
        {
            // Cursor.lockState == Locked is a mouse-specific proxy for "the
            // player clicked back into the game world, close this menu." It's
            // already true from ordinary gameplay in VR (the cursor is
            // effectively always locked there) and for controller input
            // (there's no mouse-look capture/release cycle for a controller
            // press to trigger), so relying on it alone closed this panel
            // within a frame of it ever opening for either.
            bool mouseDriven = XRSettings.loadedDeviceName != "OpenVR" && GameInput.PrimaryDevice != GameInput.Device.Controller;
            if (mouseDriven && IsVisible && Cursor.lockState == CursorLockMode.Locked)
            {
                Hide();
                return;
            }

            // uGUI_PDA.OnSelect resets GamepadInputModule's current grid back
            // to whatever PDA tab is open on every PDA focus change --
            // including simply reopening the PDA while this panel is still
            // up -- with no awareness this panel might be open on top of it.
            // Re-assert ownership every frame instead of trying to patch
            // every place that might reset it.
            if (IsVisible && _navigableGrid != null && GamepadInputModule.current != null && GamepadInputModule.current.GetCurrentGrid() != (object)_navigableGrid)
            {
                GamepadInputModule.current.SetCurrentGrid(_navigableGrid);
            }
        }

        private Color CurrentColor =>
            Color.HSVToRGB(_hueSlider.value, _satSlider.value, _valSlider.value);

        private void UpdatePreview()
        {
            var color = CurrentColor;
            if (_previewSwatch != null)
                _previewSwatch.color = color;
            if (_rgbLabel != null)
                _rgbLabel.text = $"R: {Mathf.RoundToInt(color.r * 255)}  G: {Mathf.RoundToInt(color.g * 255)}  B: {Mathf.RoundToInt(color.b * 255)}";
            if (_hexInput != null && !_hexInput.isFocused)
                _hexInput.text = $"#{ColorUtility.ToHtmlStringRGB(color)}";
        }

        private void OnApplyClicked()
        {
            _onApply?.Invoke(_contextId, CurrentColor);
            Hide();
        }

        private void BuildUI()
        {
            _panelRoot = new GameObject("ColorPickerRoot");
            _panelRoot.transform.SetParent(transform, false);
            var panelRt = _panelRoot.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(320, 360);
            panelRt.anchoredPosition = new Vector2(300, 0);
            var panelImg = _panelRoot.AddComponent<Image>();
            panelImg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

            var cg = _panelRoot.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = true;
            cg.interactable = true;

            CreateLabel(_panelRoot.transform, "Color Picker", new Vector2(0, 125), 20, TextAnchor.MiddleCenter);

            CreateLabel(_panelRoot.transform, "H", new Vector2(-135, 75), 16, TextAnchor.MiddleLeft);
            _hueSlider = CreateSlider(_panelRoot.transform, new Vector2(15, 75), 0f, 1f);
            _hueSlider.onValueChanged.AddListener(_ => UpdatePreview());
            _hueBackground = CreateHueGradient(_hueSlider);

            CreateLabel(_panelRoot.transform, "S", new Vector2(-135, 30), 16, TextAnchor.MiddleLeft);
            _satSlider = CreateSlider(_panelRoot.transform, new Vector2(15, 30), 0f, 1f);
            _satSlider.onValueChanged.AddListener(_ => UpdatePreview());

            CreateLabel(_panelRoot.transform, "V", new Vector2(-135, -15), 16, TextAnchor.MiddleLeft);
            _valSlider = CreateSlider(_panelRoot.transform, new Vector2(15, -15), 0f, 1f);
            _valSlider.onValueChanged.AddListener(_ => UpdatePreview());

            CreateLabel(_panelRoot.transform, "Preview", new Vector2(0, -52), 14, TextAnchor.MiddleCenter);
            var swatchGo = new GameObject("PreviewSwatch");
            swatchGo.transform.SetParent(_panelRoot.transform, false);
            var swatchRt = swatchGo.AddComponent<RectTransform>();
            swatchRt.anchoredPosition = new Vector2(0, -80);
            swatchRt.sizeDelta = new Vector2(240, 30);
            _previewSwatch = swatchGo.AddComponent<Image>();
            _previewSwatch.color = Color.white;

            var rgbGo = new GameObject("RGBLabel");
            rgbGo.transform.SetParent(_panelRoot.transform, false);
            var rgbRt = rgbGo.AddComponent<RectTransform>();
            rgbRt.anchoredPosition = new Vector2(0, -110);
            rgbRt.sizeDelta = new Vector2(280, 20);
            _rgbLabel = rgbGo.AddComponent<Text>();
            _rgbLabel.fontSize = 14;
            _rgbLabel.color = Color.white;
            _rgbLabel.alignment = TextAnchor.MiddleCenter;
            _rgbLabel.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            var hexGo = new GameObject("HexInput");
            hexGo.transform.SetParent(_panelRoot.transform, false);
            var hexRt = hexGo.AddComponent<RectTransform>();
            hexRt.anchoredPosition = new Vector2(0, -135);
            hexRt.sizeDelta = new Vector2(120, 25);
            var hexBg = hexGo.AddComponent<Image>();
            hexBg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            var hexTextGo = new GameObject("Text");
            hexTextGo.transform.SetParent(hexGo.transform, false);
            var hexTextRt = hexTextGo.AddComponent<RectTransform>();
            hexTextRt.anchorMin = Vector2.zero;
            hexTextRt.anchorMax = Vector2.one;
            hexTextRt.offsetMin = new Vector2(5f, 2f);
            hexTextRt.offsetMax = new Vector2(-5f, -2f);
            var hexText = hexTextGo.AddComponent<TextMeshProUGUI>();
            hexText.fontSize = 14;
            hexText.color = Color.white;
            hexText.alignment = TextAlignmentOptions.Center;

            // TMP_InputField rather than the legacy InputField: SubmersedVR
            // (and likely other VR mods) hook their virtual-keyboard-in-VR
            // support as a Harmony postfix on
            // TMPro.TMP_InputField.ActivateInputField specifically, so a
            // legacy InputField's ActivateInputField never triggers it --
            // the field still accepted clicks and a physical keyboard, so it
            // looked like it worked, it just never opened a VR keyboard.
            _hexInput = hexGo.AddComponent<TMP_InputField>();
            _hexInput.textComponent = hexText;
            _hexInput.characterLimit = 7;
            _hexInput.onEndEdit.AddListener(hexStr =>
            {
                if (string.IsNullOrEmpty(hexStr)) return;
                string toParse = hexStr.StartsWith("#") ? hexStr : "#" + hexStr;
                if (ColorUtility.TryParseHtmlString(toParse, out Color parsed))
                {
                    Color.RGBToHSV(parsed, out float h, out float s, out float v);
                    _hueSlider.value = h;
                    _satSlider.value = s;
                    _valSlider.value = v;
                }
            });

            _applyButton = CreateButton(_panelRoot.transform, "Apply", new Vector2(-60, -170), new Color(0.2f, 0.6f, 0.2f, 1f), OnApplyClicked);
            _closeButton = CreateButton(_panelRoot.transform, "Close", new Vector2(60, -170), new Color(0.5f, 0.2f, 0.2f, 1f), Hide);

            CreateFocusRing();

            _panelRoot.SetActive(false);
        }

        // Unity's own Selectable ColorTint transition turned out unreliable
        // as a focus indicator here: targetGraphic is assigned after
        // AddComponent<Button>()/<Slider>() in CreateButton/CreateSlider, so
        // the very first automatic state transition (which fires immediately
        // on enable) runs against a null targetGraphic and never visibly
        // recovers even once targetGraphic is set. Rather than fight that
        // transition timing, the panel drives its own explicit focus
        // indicator directly from NavigableGrid's selection state -- a
        // 4-bar hollow rectangle, built the same procedural-primitives way
        // as the rest of this panel (no sprite assets needed).
        private void CreateFocusRing()
        {
            var ring = new GameObject("FocusRing");
            ring.transform.SetParent(_panelRoot.transform, false);
            _focusRingRect = ring.AddComponent<RectTransform>();
            const float thickness = 3f;
            CreateFocusRingBar(ring.transform, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, thickness));
            CreateFocusRingBar(ring.transform, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, thickness));
            CreateFocusRingBar(ring.transform, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(thickness, 0f));
            CreateFocusRingBar(ring.transform, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(thickness, 0f));
            _focusRing = ring;
            _focusRing.SetActive(false);
        }

        private void CreateFocusRingBar(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta)
        {
            var bar = new GameObject(name);
            bar.transform.SetParent(parent, false);
            var rect = bar.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = Vector2.zero;
            var image = bar.AddComponent<Image>();
            image.color = FocusRingColor;
            image.raycastTarget = false;
        }

        /// <summary>
        /// Called by NavigableGrid whenever its selection changes. `item` is
        /// whatever GetSelectedItem() returns -- always a Component sharing
        /// this panel's own center-anchored RectTransform convention, so no
        /// space conversion is needed to size the ring around it.
        /// </summary>
        internal void ShowFocusRing(object item)
        {
            var target = (item as Component)?.GetComponent<RectTransform>();
            if (target == null || _focusRing == null)
            {
                HideFocusRing();
                return;
            }
            _focusRingRect.anchoredPosition = target.anchoredPosition;
            _focusRingRect.sizeDelta = target.sizeDelta + new Vector2(10f, 10f);
            _focusRing.transform.SetAsLastSibling();
            _focusRing.SetActive(true);
        }

        internal void HideFocusRing()
        {
            if (_focusRing != null)
                _focusRing.SetActive(false);
        }

        private Image CreateHueGradient(Slider slider)
        {
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
            var sliderGo = new GameObject("Slider");
            sliderGo.transform.SetParent(parent, false);
            var sliderRt = sliderGo.AddComponent<RectTransform>();
            sliderRt.anchoredPosition = position;
            sliderRt.sizeDelta = new Vector2(230, 20);

            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(sliderGo.transform, false);
            var bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.25f, 0.25f, 0.25f, 1f);

            var fillAreaGo = new GameObject("Fill Area");
            fillAreaGo.transform.SetParent(sliderGo.transform, false);
            var fillAreaRt = fillAreaGo.AddComponent<RectTransform>();
            fillAreaRt.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRt.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRt.offsetMin = new Vector2(5f, 0f);
            fillAreaRt.offsetMax = new Vector2(-5f, 0f);

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(fillAreaGo.transform, false);
            var fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.sizeDelta = new Vector2(0f, 0f);
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = new Color(0.4f, 0.7f, 1f, 1f);

            var handleAreaGo = new GameObject("Handle Slide Area");
            handleAreaGo.transform.SetParent(sliderGo.transform, false);
            var handleAreaRt = handleAreaGo.AddComponent<RectTransform>();
            handleAreaRt.anchorMin = Vector2.zero;
            handleAreaRt.anchorMax = Vector2.one;
            handleAreaRt.offsetMin = new Vector2(10f, 0f);
            handleAreaRt.offsetMax = new Vector2(-10f, 0f);

            var handleGo = new GameObject("Handle");
            handleGo.transform.SetParent(handleAreaGo.transform, false);
            var handleRt = handleGo.AddComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(16f, 0f);
            var handleImg = handleGo.AddComponent<Image>();
            handleImg.color = Color.white;

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

        private Button CreateButton(Transform parent, string label, Vector2 position, Color bgColor, UnityEngine.Events.UnityAction onClick)
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

            return btn;
        }

        /// <summary>
        /// Minimal uGUI_INavigableIconGrid over this panel's own controls
        /// (hue/sat/val sliders and the hex field each their own row,
        /// Apply+Close sharing a row navigable left/right).
        /// GamepadInputModule already dispatches value changes based on
        /// GetSelectedItem()'s runtime type -- the right stick onto a
        /// Slider directly, and UISubmit onto anything implementing
        /// IPointerClickHandler (Button and TMP_InputField both do) -- so
        /// this class only needs to move focus between controls, not
        /// manipulate their values.
        /// </summary>
        private class NavigableGrid : uGUI_INavigableIconGrid
        {
            private readonly ColorPickerPanel _panel;
            private int _row;
            private int _col;

            internal NavigableGrid(ColorPickerPanel panel)
            {
                _panel = panel;
            }

            public bool ShowSelector => true;
            public bool EmulateRaycast => true;

            private object[][] Rows => new object[][]
            {
                new object[] { _panel._hueSlider },
                new object[] { _panel._satSlider },
                new object[] { _panel._valSlider },
                new object[] { _panel._hexInput },
                new object[] { _panel._applyButton, _panel._closeButton },
            };

            public object GetSelectedItem()
            {
                var rows = Rows;
                _row = Mathf.Clamp(_row, 0, rows.Length - 1);
                _col = Mathf.Clamp(_col, 0, rows[_row].Length - 1);
                return rows[_row][_col];
            }

            public Graphic GetSelectedIcon() => (GetSelectedItem() as Selectable)?.targetGraphic;

            public void SelectItem(object item)
            {
                var rows = Rows;
                for (int r = 0; r < rows.Length; r++)
                {
                    for (int c = 0; c < rows[r].Length; c++)
                    {
                        if (Equals(rows[r][c], item))
                        {
                            _row = r;
                            _col = c;
                            RefreshFocusRing();
                            return;
                        }
                    }
                }
            }

            public void DeselectItem() => _panel.HideFocusRing();

            public bool SelectFirstItem()
            {
                _row = 0;
                _col = 0;
                RefreshFocusRing();
                return true;
            }

            public bool SelectItemClosestToPosition(Vector3 worldPos) => SelectFirstItem();

            public bool SelectItemInDirection(int dirX, int dirY)
            {
                if (dirX == 0 && dirY == 0)
                    return false;

                var rows = Rows;
                int newRow = Mathf.Clamp(_row + dirY, 0, rows.Length - 1);
                int newCol = dirY != 0 ? 0 : Mathf.Clamp(_col + dirX, 0, rows[_row].Length - 1);
                newCol = Mathf.Clamp(newCol, 0, rows[newRow].Length - 1);
                if (newRow == _row && newCol == _col)
                    return false;

                _row = newRow;
                _col = newCol;
                RefreshFocusRing();
                return true;
            }

            public uGUI_INavigableIconGrid GetNavigableGridInDirection(int dirX, int dirY) => null;

            private void RefreshFocusRing() => _panel.ShowFocusRing(GetSelectedItem());
        }
    }
}
