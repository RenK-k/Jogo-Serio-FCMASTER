using System.Collections.Generic;
using UnityEngine;

namespace CoachMaster.UI
{
    public static class DisplayPrefs
    {
        static readonly int[] FrameRates = { 30, 60, 120, 144, 165, 240, -1 };
        static readonly Vector2Int[] FallbackResolutions =
        {
            new Vector2Int(1280, 720), new Vector2Int(1600, 900), new Vector2Int(1920, 1080)
        };

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
            var mode = Mathf.Clamp(PlayerPrefs.GetInt("cm_full", 0), 0, 2);
            var windowed = mode == 0;
            var width = PlayerPrefs.GetInt("cm_w", 1600);
            var height = PlayerPrefs.GetInt("cm_h", 900);
            if (mode == 1)
            {
                width = Screen.currentResolution.width;
                height = Screen.currentResolution.height;
            }

            var fullscreenMode = mode == 2 ? FullScreenMode.ExclusiveFullScreen : mode == 1 ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            Screen.SetResolution(width, height, fullscreenMode);
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
            SetWindowMode(Screen.fullScreenMode == FullScreenMode.Windowed ? 1 : 0);
        }

        public static void CycleWindowMode()
        {
            SetWindowMode((Mathf.Clamp(PlayerPrefs.GetInt("cm_full", 0), 0, 2) + 1) % 3);
        }

        public static void SetWindowMode(int mode)
        {
            mode = Mathf.Clamp(mode, 0, 2);
            PlayerPrefs.SetInt("cm_full", mode);
            var width = mode == 1 ? Screen.currentResolution.width : PlayerPrefs.GetInt("cm_w", 1600);
            var height = mode == 1 ? Screen.currentResolution.height : PlayerPrefs.GetInt("cm_h", 900);
            Screen.SetResolution(width, height, mode == 2 ? FullScreenMode.ExclusiveFullScreen : mode == 1 ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        }

        public static void SetSize(int width, int height)
        {
            PlayerPrefs.SetInt("cm_w", width);
            PlayerPrefs.SetInt("cm_h", height);
            var mode = Mathf.Clamp(PlayerPrefs.GetInt("cm_full", 0), 0, 2);
            var applyWidth = mode == 1 ? Screen.currentResolution.width : width;
            var applyHeight = mode == 1 ? Screen.currentResolution.height : height;
            Screen.SetResolution(applyWidth, applyHeight, mode == 2 ? FullScreenMode.ExclusiveFullScreen : mode == 1 ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        }

        public static void CycleResolution()
        {
            var choices = SupportedResolutions();
            var currentWidth = PlayerPrefs.GetInt("cm_w", 1600);
            var currentHeight = PlayerPrefs.GetInt("cm_h", 900);
            var index = -1;
            for (var i = 0; i < choices.Count; i++)
                if (choices[i].x == currentWidth && choices[i].y == currentHeight)
                    index = i;
            var next = choices[(index + 1) % choices.Count];
            SetSize(next.x, next.y);
        }

        static List<Vector2Int> SupportedResolutions()
        {
            var choices = new List<Vector2Int>();
            var available = Screen.resolutions;
            if (available != null)
            {
                for (var i = 0; i < available.Length; i++)
                {
                    var resolution = new Vector2Int(available[i].width, available[i].height);
                    if (resolution.x < 1280 || resolution.y < 720 || choices.Contains(resolution))
                        continue;
                    choices.Add(resolution);
                }
            }
            if (choices.Count == 0)
                choices.AddRange(FallbackResolutions);
            choices.Sort((a, b) => a.x == b.x ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
            return choices;
        }

        public static void ToggleVsync()
        {
            var on = QualitySettings.vSyncCount == 0;
            QualitySettings.vSyncCount = on ? 1 : 0;
            PlayerPrefs.SetInt("cm_vsync", on ? 1 : 0);
        }

        public static void CycleQuality()
        {
            var names = QualitySettings.names;
            if (names == null || names.Length == 0)
                return;
            var next = (QualitySettings.GetQualityLevel() + 1) % names.Length;
            QualitySettings.SetQualityLevel(next, true);
            PlayerPrefs.SetInt("cm_quality", next);
        }

        public static void CycleFrameRate()
        {
            var next = (Mathf.Clamp(PlayerPrefs.GetInt("cm_fps_index", 1), 0, FrameRates.Length - 1) + 1) % FrameRates.Length;
            PlayerPrefs.SetInt("cm_fps_index", next);
            ApplyFrameRate();
        }

        static void ApplyFrameRate()
        {
            var index = Mathf.Clamp(PlayerPrefs.GetInt("cm_fps_index", 1), 0, FrameRates.Length - 1);
            Application.targetFrameRate = FrameRates[index];
        }

        public static string WindowLabel
        {
            get
            {
                var mode = Mathf.Clamp(PlayerPrefs.GetInt("cm_full", 0), 0, 2);
                return mode == 0 ? "MODO  JANELA" : mode == 1 ? "MODO  SEM MOLDURA" : "MODO  ECRÃ INTEIRO";
            }
        }

        public static string ResolutionLabel => "RESOLUÇÃO  " + PlayerPrefs.GetInt("cm_w", 1600) + " × " + PlayerPrefs.GetInt("cm_h", 900);

        public static string VsyncLabel => QualitySettings.vSyncCount > 0 ? "VSYNC LIGADO" : "VSYNC DESLIGADO";

        public static string FrameRateLabel
        {
            get
            {
                var index = Mathf.Clamp(PlayerPrefs.GetInt("cm_fps_index", 1), 0, FrameRates.Length - 1);
                return FrameRates[index] < 0 ? "FPS  SEM LIMITE" : "FPS  " + FrameRates[index];
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

        public static void ResetDefaults()
        {
            PlayerPrefs.SetInt("cm_full", 0);
            PlayerPrefs.SetInt("cm_w", 1600);
            PlayerPrefs.SetInt("cm_h", 900);
            PlayerPrefs.SetInt("cm_vsync", 1);
            PlayerPrefs.SetInt("cm_fps_index", 1);
            var names = QualitySettings.names;
            if (names != null && names.Length > 0)
                PlayerPrefs.SetInt("cm_quality", Mathf.Min(2, names.Length - 1));
            Apply();
        }
    }
}
