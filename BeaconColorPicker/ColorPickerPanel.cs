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
                }
                return _instance;
            }
        }

        public bool IsVisible => _panelRoot != null && _panelRoot.activeSelf;

        public void Show(string pingId, Color currentColor, Action<string, Color> onApply)
        {
            // Rebuild UI if panel was destroyed (e.g. scene change destroyed PDA canvas)
            if (_panelRoot == null)
                BuildUI();

            _currentPingId = pingId;
            _onApply = onApply;

            Color.RGBToHSV(currentColor, out float h, out float s, out float v);
            _hueSlider.SetValueWithoutNotify(h);
            _satSlider.SetValueWithoutNotify(s);
            _valSlider.SetValueWithoutNotify(v);
            UpdatePreview();

            _panelRoot.SetActive(true);
            _panelRoot.transform.SetAsLastSibling();
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
            // Parent under the PDA's canvas so our panel inherits its
            // uGUI_GraphicRaycaster. Subnautica's custom input module only
            // routes events through that raycaster, not standard GraphicRaycaster.
            Transform parentTransform = transform;
            var pdaCanvas = FindPDACanvas();
            if (pdaCanvas != null)
            {
                parentTransform = pdaCanvas.transform;
            }
            else
            {
                BeaconColorPickerPlugin.Log?.LogWarning(
                    "ColorPickerPanel: Could not find PDA canvas — input may not work.");
            }

            // Panel background
            _panelRoot = new GameObject("BeaconColorPickerRoot");
            _panelRoot.transform.SetParent(parentTransform, false);
            var panelRt = _panelRoot.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(320, 300);
            panelRt.anchoredPosition = new Vector2(300, 0);
            var panelImg = _panelRoot.AddComponent<Image>();
            panelImg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

            // Block raycasts to PDA elements behind this panel
            var cg = _panelRoot.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = true;
            cg.interactable = true;

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

        private Canvas FindPDACanvas()
        {
            var pdaUI = FindObjectOfType<uGUI_PDA>();
            if (pdaUI != null)
                return pdaUI.GetComponentInParent<Canvas>();
            return null;
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
