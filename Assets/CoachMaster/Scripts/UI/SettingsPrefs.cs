using System.Collections.Generic;
using UnityEngine;

namespace CoachMaster.UI
{
    /// <summary>Central access to player-wide settings and their versioned defaults.</summary>
    public static class SettingsPrefs
    {
        public const string UiScaleKey = "cm_ui_scale";
        public const string ReduceMotionKey = "cm_reduce_motion";
        public const string ControllerHintsKey = "cm_controller_hints";
        public const string MatchSpeedKey = "cm_match_speed";
        public const string MatchHighlightsKey = "cm_match_highlights";
        public const string PauseGoalKey = "cm_pause_goal";
        public const string PauseRedKey = "cm_pause_red";
        public const string PauseInjuryKey = "cm_pause_injury";
        public const string LiveRatingsKey = "cm_live_ratings";
        public const string DecisionAlertsKey = "cm_decision_alerts";
        public const string AlertDurationKey = "cm_alert_duration";
        public const string AutoTrainingKey = "cm_auto_training";
        public const string AutoSelectionKey = "cm_auto_selection";
        public const string AutosaveEnabledKey = "cm_autosave_enabled";
        public const string AutosaveBeforeMatchKey = "cm_autosave_before_match";
        public const string AutosaveDaysKey = "cm_autosave_days";
        public const string AutosaveSlotsKey = "cm_autosave_slots";
        public const string BackgroundMuteKey = "cm_background_mute";

        const int CurrentVersion = 1;
        static readonly string[] AllKeys =
        {
            "cm_full", "cm_w", "cm_h", "cm_vsync", "cm_quality", "cm_fps_index", "cm_background_index",
            UiScaleKey, ReduceMotionKey, ControllerHintsKey, MatchSpeedKey, MatchHighlightsKey,
            PauseGoalKey, PauseRedKey, PauseInjuryKey, LiveRatingsKey, DecisionAlertsKey, AlertDurationKey,
            AutoTrainingKey, AutoSelectionKey, AutosaveEnabledKey, AutosaveBeforeMatchKey, AutosaveDaysKey, AutosaveSlotsKey,
            BackgroundMuteKey, "cm_default_injury", "cm_default_referee", "cm_default_realism", "cm_audio_master", "cm_audio_music", "cm_audio_effects", "cm_audio_crowd",
            "cm_audio_menu", "cm_audio_mute", "cm_audio_repeat"
        };

        public sealed class Snapshot
        {
            internal readonly Dictionary<string, int> Integers = new Dictionary<string, int>();
            internal readonly Dictionary<string, float> Floats = new Dictionary<string, float>();
        }

        static SettingsPrefs()
        {
            var version = PlayerPrefs.GetInt("cm_settings_version", 0);
            if (version < CurrentVersion)
            {
                PlayerPrefs.SetInt(UiScaleKey, Mathf.Clamp(PlayerPrefs.GetInt(UiScaleKey, 2), 0, 4));
                PlayerPrefs.SetInt(ControllerHintsKey, PlayerPrefs.GetInt(ControllerHintsKey, 1) == 0 ? 0 : 1);
                PlayerPrefs.SetInt(AutosaveEnabledKey, PlayerPrefs.GetInt(AutosaveEnabledKey, 1) == 0 ? 0 : 1);
                PlayerPrefs.SetInt("cm_settings_version", CurrentVersion);
                PlayerPrefs.Save();
            }
        }

        public static int UiScaleIndex { get => Mathf.Clamp(PlayerPrefs.GetInt(UiScaleKey, 2), 0, 4); set => PlayerPrefs.SetInt(UiScaleKey, Mathf.Clamp(value, 0, 4)); }
        public static float UiScale => new[] { 0.8f, 0.9f, 1f, 1.1f, 1.2f }[UiScaleIndex];
        public static bool ReduceMotion { get => PlayerPrefs.GetInt(ReduceMotionKey, 0) != 0; set => PlayerPrefs.SetInt(ReduceMotionKey, value ? 1 : 0); }
        public static bool ControllerHints { get => PlayerPrefs.GetInt(ControllerHintsKey, 1) != 0; set => PlayerPrefs.SetInt(ControllerHintsKey, value ? 1 : 0); }
        public static int MatchSpeedIndex { get => Mathf.Clamp(PlayerPrefs.GetInt(MatchSpeedKey, 1), 0, 3); set => PlayerPrefs.SetInt(MatchSpeedKey, Mathf.Clamp(value, 0, 3)); }
        public static string MatchSpeedLabel => new[] { "0,5×", "1×", "2×", "4×" }[MatchSpeedIndex];
        public static float MatchSpeed => new[] { 0.34f, 0.68f, 2.4f, 4.2f }[MatchSpeedIndex];
        public static int MatchHighlights { get => Mathf.Clamp(PlayerPrefs.GetInt(MatchHighlightsKey, 1), 0, 3); set => PlayerPrefs.SetInt(MatchHighlightsKey, Mathf.Clamp(value, 0, 3)); }
        public static string MatchHighlightsLabel => new[] { "Todos", "Alargados", "Chave", "Só golos" }[MatchHighlights];
        public static bool PauseOnGoal { get => PlayerPrefs.GetInt(PauseGoalKey, 0) != 0; set => PlayerPrefs.SetInt(PauseGoalKey, value ? 1 : 0); }
        public static bool PauseOnRedCard { get => PlayerPrefs.GetInt(PauseRedKey, 0) != 0; set => PlayerPrefs.SetInt(PauseRedKey, value ? 1 : 0); }
        public static bool PauseOnInjury { get => PlayerPrefs.GetInt(PauseInjuryKey, 0) != 0; set => PlayerPrefs.SetInt(PauseInjuryKey, value ? 1 : 0); }
        public static bool LiveRatings { get => PlayerPrefs.GetInt(LiveRatingsKey, 1) != 0; set => PlayerPrefs.SetInt(LiveRatingsKey, value ? 1 : 0); }
        public static bool DecisionAlerts { get => PlayerPrefs.GetInt(DecisionAlertsKey, 1) != 0; set => PlayerPrefs.SetInt(DecisionAlertsKey, value ? 1 : 0); }
        public static int AlertDurationIndex { get => Mathf.Clamp(PlayerPrefs.GetInt(AlertDurationKey, 1), 0, 2); set => PlayerPrefs.SetInt(AlertDurationKey, Mathf.Clamp(value, 0, 2)); }
        public static float AlertDuration => new[] { 2.5f, 3.5f, 5f }[AlertDurationIndex];
        public static string AlertDurationLabel => new[] { "Curta", "Normal", "Longa" }[AlertDurationIndex];
        public static bool AutomaticTraining { get => PlayerPrefs.GetInt(AutoTrainingKey, 1) != 0; set => PlayerPrefs.SetInt(AutoTrainingKey, value ? 1 : 0); }
        public static bool AutomaticSquadSelection { get => PlayerPrefs.GetInt(AutoSelectionKey, 0) != 0; set => PlayerPrefs.SetInt(AutoSelectionKey, value ? 1 : 0); }
        public static bool AutosaveEnabled { get => PlayerPrefs.GetInt(AutosaveEnabledKey, 1) != 0; set => PlayerPrefs.SetInt(AutosaveEnabledKey, value ? 1 : 0); }
        public static bool AutosaveBeforeMatch { get => PlayerPrefs.GetInt(AutosaveBeforeMatchKey, 1) != 0; set => PlayerPrefs.SetInt(AutosaveBeforeMatchKey, value ? 1 : 0); }
        public static int AutosaveDaysIndex { get => Mathf.Clamp(PlayerPrefs.GetInt(AutosaveDaysKey, 1), 0, 3); set => PlayerPrefs.SetInt(AutosaveDaysKey, Mathf.Clamp(value, 0, 3)); }
        public static int AutosaveDays => new[] { 1, 3, 7, 14 }[AutosaveDaysIndex];
        public static string AutosaveDaysLabel => new[] { "Todos os dias", "A cada 3 dias", "A cada semana", "A cada 2 semanas" }[AutosaveDaysIndex];
        public static int AutosaveSlotsIndex { get => Mathf.Clamp(PlayerPrefs.GetInt(AutosaveSlotsKey, 1), 0, 2); set => PlayerPrefs.SetInt(AutosaveSlotsKey, Mathf.Clamp(value, 0, 2)); }
        public static int AutosaveSlots => new[] { 2, 3, 5 }[AutosaveSlotsIndex];
        public static string AutosaveSlotsLabel => AutosaveSlots + " cópias";
        public static bool MuteWhenBackground { get => PlayerPrefs.GetInt(BackgroundMuteKey, 0) != 0; set => PlayerPrefs.SetInt(BackgroundMuteKey, value ? 1 : 0); }

        public static Snapshot Capture()
        {
            var result = new Snapshot();
            for (var i = 0; i < AllKeys.Length; i++)
            {
                var key = AllKeys[i];
                if (key.StartsWith("cm_audio_") && key != "cm_audio_mute" && key != "cm_audio_repeat")
                    result.Floats[key] = PlayerPrefs.GetFloat(key, AudioDefault(key));
                else
                    result.Integers[key] = PlayerPrefs.GetInt(key, IntegerDefault(key));
            }
            return result;
        }

        static float AudioDefault(string key)
        {
            if (key == "cm_audio_master") return GameAudio.MasterVolume;
            if (key == "cm_audio_music") return GameAudio.MusicVolume;
            if (key == "cm_audio_effects") return GameAudio.EffectVolume;
            if (key == "cm_audio_crowd") return GameAudio.CrowdVolume;
            return GameAudio.MenuVolume;
        }

        static int IntegerDefault(string key)
        {
            if (key == "cm_w") return 1600;
            if (key == "cm_h") return 900;
            if (key == "cm_vsync") return QualitySettings.vSyncCount > 0 ? 1 : 0;
            if (key == "cm_quality") return QualitySettings.GetQualityLevel();
            if (key == "cm_fps_index" || key == MatchSpeedKey || key == MatchHighlightsKey || key == AlertDurationKey || key == AutosaveDaysKey || key == AutosaveSlotsKey || key == "cm_default_injury" || key == "cm_default_referee" || key == "cm_default_realism") return 1;
            if (key == UiScaleKey) return 2;
            if (key == ControllerHintsKey || key == LiveRatingsKey || key == DecisionAlertsKey || key == AutoTrainingKey || key == AutosaveEnabledKey || key == AutosaveBeforeMatchKey) return 1;
            if (key == "cm_full" || key == "cm_background_index" || key == "cm_audio_mute" || key == "cm_audio_repeat" || ReduceMotionKey == key || AutoSelectionKey == key || PauseGoalKey == key || PauseRedKey == key || PauseInjuryKey == key || BackgroundMuteKey == key) return 0;
            return 0;
        }

        public static void Restore(Snapshot snapshot)
        {
            if (snapshot == null)
                return;
            for (var i = 0; i < AllKeys.Length; i++)
            {
                var key = AllKeys[i];
                PlayerPrefs.DeleteKey(key);
                if (snapshot.Floats.TryGetValue(key, out var floatValue))
                    PlayerPrefs.SetFloat(key, floatValue);
                else if (snapshot.Integers.TryGetValue(key, out var intValue))
                    PlayerPrefs.SetInt(key, intValue);
            }
            PlayerPrefs.Save();
            DisplayPrefs.Apply();
            GameAudio.ReloadPreferences();
            LeagueBackdrop.RefreshCurrentScene();
        }

        public static void Commit() => PlayerPrefs.Save();

        public static void RestoreDefaults()
        {
            for (var i = 0; i < AllKeys.Length; i++)
                PlayerPrefs.DeleteKey(AllKeys[i]);
            PlayerPrefs.SetInt("cm_full", 0);
            PlayerPrefs.SetInt("cm_w", 1600);
            PlayerPrefs.SetInt("cm_h", 900);
            PlayerPrefs.SetInt("cm_vsync", 1);
            PlayerPrefs.SetInt("cm_fps_index", 1);
            PlayerPrefs.SetInt(MatchSpeedKey, 1);
            PlayerPrefs.SetInt(MatchHighlightsKey, 1);
            PlayerPrefs.SetInt(LiveRatingsKey, 1);
            PlayerPrefs.SetInt(DecisionAlertsKey, 1);
            PlayerPrefs.SetInt(AlertDurationKey, 1);
            PlayerPrefs.SetInt(AutoTrainingKey, 1);
            PlayerPrefs.SetInt(AutosaveEnabledKey, 1);
            PlayerPrefs.SetInt(AutosaveBeforeMatchKey, 1);
            PlayerPrefs.SetInt(AutosaveDaysKey, 1);
            PlayerPrefs.SetInt(AutosaveSlotsKey, 1);
            PlayerPrefs.SetInt("cm_default_injury", 1);
            PlayerPrefs.SetInt("cm_default_referee", 1);
            PlayerPrefs.SetInt("cm_default_realism", 1);
            PlayerPrefs.SetInt(UiScaleKey, 2);
            PlayerPrefs.SetInt(ControllerHintsKey, 1);
            PlayerPrefs.SetInt("cm_background_index", 0);
            PlayerPrefs.SetInt("cm_settings_version", CurrentVersion);
            GameAudio.ResetDefaults();
            DisplayPrefs.ResetDefaults();
            LeagueBackdrop.RefreshCurrentScene();
        }

        public static int DefaultInjuryFrequency { get => Mathf.Clamp(PlayerPrefs.GetInt("cm_default_injury", 1), 0, 2); set => PlayerPrefs.SetInt("cm_default_injury", Mathf.Clamp(value, 0, 2)); }
        public static int DefaultRefereeStrictness { get => Mathf.Clamp(PlayerPrefs.GetInt("cm_default_referee", 1), 0, 2); set => PlayerPrefs.SetInt("cm_default_referee", Mathf.Clamp(value, 0, 2)); }
        public static int DefaultRealismPreset { get => Mathf.Clamp(PlayerPrefs.GetInt("cm_default_realism", 1), 0, 4); set => PlayerPrefs.SetInt("cm_default_realism", Mathf.Clamp(value, 0, 4)); }

        public static string RealismPresetLabel(int preset)
        {
            var labels = new[] { "Casual", "Equilibrado", "Realista", "Hardcore", "Personalizado" };
            return labels[Mathf.Clamp(preset, 0, labels.Length - 1)];
        }

        public static string FrequencyLabel(int value) => new[] { "Baixa", "Normal", "Alta" }[Mathf.Clamp(value, 0, 2)];
        public static float InjuryMultiplier(int value) => new[] { 0.62f, 1f, 1.42f }[Mathf.Clamp(value, 0, 2)];
        public static float RefereeMultiplier(int value) => new[] { 0.72f, 1f, 1.3f }[Mathf.Clamp(value, 0, 2)];

        public static void ApplyRealismPreset(CoachMaster.Career.CareerState career, int preset)
        {
            preset = Mathf.Clamp(preset, 0, 3);
            var injury = preset == 0 ? 0 : preset == 3 ? 2 : 1;
            var referee = preset == 0 ? 0 : preset >= 2 ? 2 : 1;
            if (career == null)
            {
                DefaultRealismPreset = preset;
                DefaultInjuryFrequency = injury;
                DefaultRefereeStrictness = referee;
                return;
            }

            career.RealismPreset = preset;
            career.InjuryFrequency = injury;
            career.RefereeStrictness = referee;
        }
    }
}
