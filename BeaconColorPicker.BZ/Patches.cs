using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using SubnauticaMods.Shared;

namespace BeaconColorPicker.BZ
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
        private static readonly FieldInfo _pingsField = AccessTools.Field(typeof(uGUI_Pings), "pings");

        [HarmonyPostfix]
        static void Postfix(uGUI_Pings __instance, PingInstance instance)
        {
            if (instance == null) return;
            if (!CustomColorStore.TryGetColor(instance._id, out Color customColor)) return;

            var pingsDict = (Dictionary<string, uGUI_Ping>)_pingsField.GetValue(__instance);
            if (pingsDict != null && pingsDict.TryGetValue(instance._id, out uGUI_Ping hudPing))
            {
                hudPing.SetColor(customColor);
            }
        }
    }

    /// <summary>
    /// Clears custom color when the user selects a preset color dot.
    /// This MUST be a Prefix because SetColor internally calls PingManager.SetColor
    /// -> PingInstance.SetColor -> PingManager.NotifyColor -> onColor -> our OnColor
    /// prefix. If the custom color still existed at that point, it would override
    /// the preset selection.
    /// </summary>
    [HarmonyPatch(typeof(uGUI_PingEntry), "SetColor")]
    internal static class uGUI_PingEntry_SetColor_Patch
    {
        private static readonly FieldInfo _idField = AccessTools.Field(typeof(uGUI_PingEntry), "id");

        [HarmonyPrefix]
        static void Prefix(uGUI_PingEntry __instance)
        {
            // Skip removal when SetColor fires during Initialize re-init (not a user click)
            if (uGUI_PingEntry_Initialize_Patch.SuppressSetColorRemoval) return;

            string pingId = (string)_idField.GetValue(__instance);
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
    /// the ColorPickerPanel. Also applies custom colors to the entry icon and
    /// selection indicator on initialization.
    /// </summary>
    [HarmonyPatch(typeof(uGUI_PingEntry), "Initialize")]
    internal static class uGUI_PingEntry_Initialize_Patch
    {
        /// <summary>
        /// When true, the SetColor prefix skips removing custom colors.
        /// Set during Initialize to prevent re-init from wiping stored colors.
        /// </summary>
        internal static bool SuppressSetColorRemoval;

        [HarmonyPrefix]
        static void Prefix() => SuppressSetColorRemoval = true;

        [HarmonyPostfix]
        static void Postfix(uGUI_PingEntry __instance, string id, int colorIndex)
        {
            try
            {
                Toggle[] toggles = __instance.colorSelectors;
                if (toggles == null || toggles.Length == 0)
                {
                    BeaconColorPickerBZPlugin.Log.LogWarning($"  colorSelectors is null or empty, skipping.");
                    return;
                }

                // Prevent duplicate buttons on re-initialization
                Transform existing = __instance.transform.Find("CustomColorButton");
                if (existing != null)
                {
                    // Update the button color if a custom color exists
                    UpdateButtonColor(existing.gameObject, id);
                    // Apply custom color to icon and indicator on re-init
                    if (CustomColorStore.TryGetColor(id, out Color c))
                        ApplyCustomColorToEntry(__instance, c, existing.gameObject);

                    // Re-wire button listener with current ping ID and entry (fixes stale closure)
                    var button = existing.GetComponent<Button>();
                    if (button != null)
                        RewireButtonListener(button, id, __instance, existing.gameObject);

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

                // Destroy the cloned Toggle — it carries persistent SetColorN callbacks from the prefab
                // that RemoveAllListeners() can't remove, causing unwanted color resets
                var oldToggle = newToggleGo.GetComponent<Toggle>();
                if (oldToggle != null)
                    Object.DestroyImmediate(oldToggle);

                // Add a clean Button — no persistent callbacks
                var newButton = newToggleGo.AddComponent<Button>();
                newButton.targetGraphic = newToggleGo.GetComponent<Image>();

                // Set appearance — white by default, custom color if one exists
                UpdateButtonColor(newToggleGo, id);

                // Apply custom color to icon and indicator if one exists
                if (CustomColorStore.TryGetColor(id, out Color customColor))
                    ApplyCustomColorToEntry(__instance, customColor, newToggleGo);

                // Wire click to open color picker
                RewireButtonListener(newButton, id, __instance, newToggleGo);
            }
            finally
            {
                SuppressSetColorRemoval = false;
            }
        }

        /// <summary>
        /// Wires (or re-wires) a Button's onClick to open the color picker
        /// for the given ping ID and entry. Used both for new buttons and re-init
        /// of pooled entries to prevent stale closures.
        /// </summary>
        private static void RewireButtonListener(Button button, string pingId, uGUI_PingEntry entry, GameObject buttonGo)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
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

                    // Update PDA entry icon and indicator
                    if (entry != null)
                        ApplyCustomColorToEntry(entry, color, buttonGo);

                    // Update the + button appearance
                    UpdateButtonColor(buttonGo, pid);
                });
            });
        }

        /// <summary>
        /// Updates the PDA entry's icon color and moves the selection indicator
        /// to the custom color button.
        /// </summary>
        private static void ApplyCustomColorToEntry(uGUI_PingEntry entry, Color color, GameObject customButton)
        {
            // Update the ping type icon color
            if (entry.icon != null)
                entry.icon.SetForegroundColors(color, color, color);

            // Move the selection indicator circle to the custom color button
            if (entry.colorSelectionIndicator != null && customButton != null)
            {
                var btnRt = customButton.GetComponent<RectTransform>();
                if (btnRt != null)
                    entry.colorSelectionIndicator.position = btnRt.position;
            }
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
