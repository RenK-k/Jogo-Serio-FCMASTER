using UnityEngine;

namespace CoachMaster.UI
{
    public static class DisplayPrefs
    {
        public static void Boot()
        {
#if UNITY_EDITOR
            return;
#else
            Apply();
#endif
        }

        public static void Apply()
        {
            var windowed = PlayerPrefs.GetInt("cm_full", 0) == 0;
            var width = PlayerPrefs.GetInt("cm_w", 1600);
            var height = PlayerPrefs.GetInt("cm_h", 900);
            if (!windowed)
            {
                width = Screen.currentResolution.width;
                height = Screen.currentResolution.height;
            }

            Screen.SetResolution(width, height, windowed ? FullScreenMode.Windowed : FullScreenMode.FullScreenWindow);
            QualitySettings.vSyncCount = PlayerPrefs.GetInt("cm_vsync", 1) > 0 ? 1 : 0;
            ApplyFrameRate();
            var names = QualitySettings.names;
            if (names != null && names.Length > 0)
            {
                var level = PlayerPrefs.GetInt("cm_quality", QualitySettings.GetQualityLevel());
                if (level < 0)
                    level = 0;
                if (level >= names.Length)
                    level = names.Length - 1;
                QualitySettings.SetQualityLevel(level, true);
            }
        }

        public static void ToggleFullscreen()
        {
            if (Screen.fullScreenMode == FullScreenMode.Windowed)
            {
                PlayerPrefs.SetInt("cm_full", 1);
                Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, FullScreenMode.FullScreenWindow);
            }
            else
            {
                PlayerPrefs.SetInt("cm_full", 0);
                Screen.SetResolution(PlayerPrefs.GetInt("cm_w", 1600), PlayerPrefs.GetInt("cm_h", 900), FullScreenMode.Windowed);
            }

            PlayerPrefs.Save();
        }

        public static void SetSize(int width, int height)
        {
            PlayerPrefs.SetInt("cm_w", width);
            PlayerPrefs.SetInt("cm_h", height);
            PlayerPrefs.SetInt("cm_full", 0);
            PlayerPrefs.Save();
            Screen.SetResolution(width, height, FullScreenMode.Windowed);
        }

        public static void ToggleVsync()
        {
            var on = QualitySettings.vSyncCount == 0;
            QualitySettings.vSyncCount = on ? 1 : 0;
            PlayerPrefs.SetInt("cm_vsync", on ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void CycleQuality()
        {
            var names = QualitySettings.names;
            if (names == null || names.Length == 0)
                return;
            var next = (QualitySettings.GetQualityLevel() + 1) % names.Length;
            QualitySettings.SetQualityLevel(next, true);
            PlayerPrefs.SetInt("cm_quality", next);
            PlayerPrefs.Save();
        }

        public static void CycleFrameRate()
        {
            var values = new[] { 30, 60, 120, -1 };
            var next = (PlayerPrefs.GetInt("cm_fps_index", 1) + 1) % values.Length;
            PlayerPrefs.SetInt("cm_fps_index", next);
            ApplyFrameRate();
            PlayerPrefs.Save();
        }

        static void ApplyFrameRate()
        {
            var index = Mathf.Clamp(PlayerPrefs.GetInt("cm_fps_index", 1), 0, 3);
            var values = new[] { 30, 60, 120, -1 };
            Application.targetFrameRate = values[index];
        }

        public static string WindowLabel => Screen.fullScreenMode == FullScreenMode.Windowed ? "JANELA" : "ECRÃ INTEIRO";

        public static string VsyncLabel => QualitySettings.vSyncCount > 0 ? "VSYNC LIGADO" : "VSYNC DESLIGADO";

        public static string FrameRateLabel
        {
            get
            {
                var values = new[] { 30, 60, 120, -1 };
                var index = Mathf.Clamp(PlayerPrefs.GetInt("cm_fps_index", 1), 0, values.Length - 1);
                return values[index] < 0 ? "FPS  SEM LIMITE" : "FPS  " + values[index];
            }
        }

        public static string QualityLabel
        {
            get
            {
                var names = QualitySettings.names;
                var level = QualitySettings.GetQualityLevel();
                if (names == null || level < 0 || level >= names.Length)
                    return "QUALIDADE";
                return "QUALIDADE  " + names[level].ToUpperInvariant();
            }
        }
    }
}
