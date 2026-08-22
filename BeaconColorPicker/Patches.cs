using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using SubnauticaMods.Shared;

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
    ///
    /// The clone keeps its Toggle component (rather than being replaced with a
    /// Button) and is appended onto uGUI_PingEntry.colorSelectors, because that's
    /// the array uGUI_PingEntry.GetSelectables() builds its d-pad/controller
    /// navigation list directly and only from. A control that isn't a member of
    /// it is invisible to controller navigation even though it's visible and
    /// mouse-clickable -- confirmed by adding logging that showed an earlier,
    /// external GetSelectables postfix approach never actually got called in a
    /// live session, while this approach is picked up automatically by the
    /// game's own unmodified method.
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
                    BeaconColorPickerPlugin.Log.LogWarning($"  colorSelectors is null or empty, skipping.");
                    return;
                }

                // colorSelectors may already include our own toggle from an
                // earlier Initialize() call on this same pooled entry (reused
                // for a different ping over the session) -- exclude it by name
                // so "the last preset" below is always a real preset, never
                // our own clone positioning itself relative to itself.
                Toggle[] presets = System.Array.FindAll(toggles, t => t != null && t.gameObject.name != "CustomColorButton");
                if (presets.Length == 0)
                {
                    BeaconColorPickerPlugin.Log.LogWarning($"  no preset color toggles found, skipping.");
                    return;
                }

                // Prevent duplicate buttons on re-initialization
                Transform existing = __instance.transform.Find("CustomColorButton");
                Toggle customToggle;
                if (existing != null)
                {
                    customToggle = existing.GetComponent<Toggle>();
                    if (customToggle == null)
                    {
                        BeaconColorPickerPlugin.Log.LogWarning($"  CustomColorButton exists but has no Toggle component, skipping.");
                        return;
                    }

                    // Update the button color if a custom color exists
                    UpdateButtonColor(existing.gameObject, id);
                    // Apply custom color to icon and indicator on re-init
                    if (CustomColorStore.TryGetColor(id, out Color c))
                        ApplyCustomColorToEntry(__instance, c, existing.gameObject);

                    // Re-wire with the current ping ID/entry every re-init: this
                    // pooled entry gets reused for different pings over a
                    // session, and the closure captured when the toggle was
                    // first created would otherwise keep referencing whichever
                    // ping was here first (a real bug, not hypothetical --
                    // clicks would silently open the picker for the wrong
                    // beacon). A fresh event object also guarantees nothing
                    // stale survives the re-wire.
                    customToggle.onValueChanged = new Toggle.ToggleEvent();
                    WireToggleListener(customToggle, id, __instance, existing.gameObject);
                }
                else
                {
                    // Clone the last color toggle as our "+" button
                    Toggle lastToggle = presets[presets.Length - 1];
                    var newToggleGo = Object.Instantiate(lastToggle.gameObject, lastToggle.transform.parent);
                    newToggleGo.name = "CustomColorButton";
                    customToggle = newToggleGo.GetComponent<Toggle>();

                    // Position after the last toggle
                    var rt = newToggleGo.GetComponent<RectTransform>();
                    var lastRt = lastToggle.GetComponent<RectTransform>();
                    rt.anchoredPosition = lastRt.anchoredPosition + new Vector2(rt.sizeDelta.x + 4f, 0f);

                    // The clone inherited the last preset's own baked-in
                    // onValueChanged listener (e.g. a SetColorN callback) and
                    // possibly its ToggleGroup membership. Assigning a
                    // brand-new ToggleEvent -- rather than RemoveAllListeners(),
                    // which doesn't reliably clear persistent/prefab-serialized
                    // listeners -- guarantees no old callback survives to
                    // silently overwrite the ping's real stored color when this
                    // button is clicked. Kept as a real Toggle (rather than
                    // replaced with a Button, as before) specifically so it's a
                    // genuine colorSelectors[] member; see the class doc above.
                    customToggle.onValueChanged = new Toggle.ToggleEvent();
                    customToggle.group = null;

                    // Set appearance — white by default, custom color if one exists
                    UpdateButtonColor(newToggleGo, id);

                    // Apply custom color to icon and indicator if one exists
                    if (CustomColorStore.TryGetColor(id, out Color customColor))
                        ApplyCustomColorToEntry(__instance, customColor, newToggleGo);

                    // Wire click to open color picker
                    WireToggleListener(customToggle, id, __instance, newToggleGo);
                }

                // Idempotent: only append if colorSelectors doesn't already end
                // with our toggle.
                if (toggles.Length == 0 || toggles[toggles.Length - 1] != customToggle)
                {
                    var extended = new Toggle[presets.Length + 1];
                    presets.CopyTo(extended, 0);
                    extended[presets.Length] = customToggle;
                    __instance.colorSelectors = extended;
                }
            }
            finally
            {
                SuppressSetColorRemoval = false;
            }
        }

        /// <summary>
        /// Wires (or re-wires) a Toggle's onValueChanged to open the color
        /// picker for the given ping ID and entry. Used both for new toggles
        /// and re-init of pooled entries to prevent stale closures.
        /// </summary>
        private static void WireToggleListener(Toggle toggle, string pingId, uGUI_PingEntry entry, GameObject buttonGo)
        {
            toggle.onValueChanged.AddListener(isOn =>
            {
                if (!isOn) return;

                // Momentary-button feel: this toggle doesn't represent a
                // lasting "selected preset" state (the shared
                // colorSelectionIndicator ring already handles showing which
                // color is active), it just needs to register a click.
                toggle.SetIsOnWithoutNotify(false);

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
                var toggle = customButton.GetComponent<Toggle>();
                var btnRt = toggle != null && toggle.targetGraphic != null
                    ? toggle.targetGraphic.rectTransform
                    : customButton.GetComponent<RectTransform>();
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

    /// <summary>
    /// Closes the color picker panel when the PDA itself closes.
    /// uGUI_PDA.OnSelect/OnDeselect unconditionally reset
    /// GamepadInputModule's current navigable grid on every PDA focus change
    /// -- once when it's opened (back to whatever tab is current) and once
    /// when it's closed (to null) -- with no awareness this panel might be
    /// open as an overlay on top of it. Left alone, closing and reopening
    /// the PDA left the panel visibly open but with no controller focus on
    /// any of its controls at all. Closing it along with the PDA is simpler
    /// and more predictable than trying to reconcile grid ownership across
    /// that transition -- the same way any other modal dialog would behave.
    /// </summary>
    [HarmonyPatch(typeof(uGUI_PDA), "OnDeselect")]
    internal static class uGUI_PDA_OnDeselect_Patch
    {
        [HarmonyPostfix]
        static void Postfix()
        {
            if (ColorPickerPanel.Instance.IsVisible)
                ColorPickerPanel.Instance.Hide();
        }
    }
}
