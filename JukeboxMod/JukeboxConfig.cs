using HarmonyLib;
using UnityEngine;

namespace JukeboxMod
{
    public static class JukeboxConfig
    {
        public static bool JBColor;
        public static bool PartyMode;
        public static Color FlashColor0 = new Color(0f, 0f, 0f, 1f);
        public static Color FlashColor1 = new Color(1f, 0f, 0.7f, 1f);
        public static Color FlashColor2 = new Color(1f, 0.4f, 0f, 1f);
    }

    internal static class JukeboxMenu
    {
        public static GameObject MainStoppedColor, MainColor, BeatColor;

        /// <summary>
        /// Replaces the game's AddTabs to inject a custom Jukebox tab.
        /// Returns false to skip the original method -- this is necessary
        /// because there is no post-tab-creation hook to append to.
        /// </summary>
        [HarmonyPatch(typeof(uGUI_OptionsPanel), nameof(uGUI_OptionsPanel.AddTabs))]
        internal static class uGUI_OptionsPanel_AddTabs_Patch
        {
            [HarmonyPrefix]
            static bool Prefix(uGUI_OptionsPanel __instance)
            {
                // Recreate the BZ tab structure exactly
                __instance.AddGeneralTab();

                if (__instance.showGameModeOptionsTab)
                    __instance.AddGameModeOptionsTab();

                __instance.AddGraphicsTab();

                if (GameInput.IsKeyboardAvailable())
                    __instance.AddKeyboardTab();

                if (GameInput.IsControllerAvailable())
                    __instance.AddControllerTab();

                __instance.AddAccessibilityTab();

                if (!PlatformUtils.isConsolePlatform)
                    __instance.AddTroubleshootingTab();

                // Add our custom Jukebox tab
                if (__instance != null)
                    AddJukeBoxTab(__instance);

                return false;
            }
        }

        /// <summary>
        /// Persists jukebox settings via the game's native serialization system.
        /// </summary>
        [HarmonyPatch(typeof(GameSettings), nameof(GameSettings.SerializeSettings))]
        internal static class GameSettings_SerializeSettings_Patch
        {
            [HarmonyPostfix]
            static void Postfix(GameSettings.ISerializer serializer)
            {
                JukeboxConfig.JBColor = serializer.Serialize("Jukebox/ColorToggle", JukeboxConfig.JBColor);
                JukeboxConfig.FlashColor0 = serializer.Serialize("Jukebox/MainStoppedColor", JukeboxConfig.FlashColor0);
                JukeboxConfig.FlashColor2 = serializer.Serialize("Jukebox/MainColor", JukeboxConfig.FlashColor2);
                JukeboxConfig.FlashColor1 = serializer.Serialize("Jukebox/BeatColor", JukeboxConfig.FlashColor1);
                JukeboxConfig.PartyMode = serializer.Serialize("Jukebox/PartyMode", JukeboxConfig.PartyMode);
            }
        }

        public static void AddJukeBoxTab(uGUI_OptionsPanel oPanel)
        {
            int tabIndex = oPanel.AddTab("Jukebox");

            oPanel.AddHeading(tabIndex, "Jukebox Colors");
            oPanel.AddToggleOption(tabIndex, "Toggle Jukebox Colors", JukeboxConfig.JBColor,
                (bool v) => JukeboxConfig.JBColor = ToggleColorChange(v));

            MainStoppedColor = oPanel.AddColorOption(tabIndex, "Color 1", JukeboxConfig.FlashColor0,
                (Color c) => JukeboxConfig.FlashColor0 = c);
            MainColor = oPanel.AddColorOption(tabIndex, "Color 2", JukeboxConfig.FlashColor2,
                (Color c) => JukeboxConfig.FlashColor2 = c);
            BeatColor = oPanel.AddColorOption(tabIndex, "Color 3", JukeboxConfig.FlashColor1,
                (Color c) => JukeboxConfig.FlashColor1 = c);

            oPanel.AddHeading(tabIndex, "Jukebox Options");
            oPanel.AddToggleOption(tabIndex, "Toggle Party Mode", JukeboxConfig.PartyMode,
                (bool p) => JukeboxConfig.PartyMode = p);

            ToggleColorChange(JukeboxConfig.JBColor);
        }

        public static bool ToggleColorChange(bool value)
        {
            if (MainStoppedColor != null) MainStoppedColor.SetActive(value);
            if (MainColor != null) MainColor.SetActive(value);
            if (BeatColor != null) BeatColor.SetActive(value);

            if (!value)
            {
                JukeboxConfig.FlashColor0 = new Color(0f, 0f, 0f, 1f);
                JukeboxConfig.FlashColor1 = new Color(1f, 0f, 0.7f, 1f);
                JukeboxConfig.FlashColor2 = new Color(1f, 0.4f, 0f, 1f);
            }

            return value;
        }
    }
}
