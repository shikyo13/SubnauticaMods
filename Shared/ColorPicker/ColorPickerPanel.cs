using System;
using UnityEngine;
using UnityEngine.UI;
namespace SubnauticaMods.Shared
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
        private Text _rgbLabel;
        private InputField _hexInput;

        private string _contextId;
        private Action<string, Color> _onApply;

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
        }

        private void Update()
        {
            if (IsVisible && Cursor.lockState == CursorLockMode.Locked)
                Hide();
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
            var hexText = hexTextGo.AddComponent<Text>();
            hexText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            hexText.fontSize = 14;
            hexText.color = Color.white;
            hexText.alignment = TextAnchor.MiddleCenter;
            hexText.supportRichText = false;

            _hexInput = hexGo.AddComponent<InputField>();
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

            CreateButton(_panelRoot.transform, "Apply", new Vector2(-60, -170), new Color(0.2f, 0.6f, 0.2f, 1f), OnApplyClicked);
            CreateButton(_panelRoot.transform, "Close", new Vector2(60, -170), new Color(0.5f, 0.2f, 0.2f, 1f), Hide);

            _panelRoot.SetActive(false);
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
