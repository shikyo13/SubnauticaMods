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
