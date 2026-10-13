using UnityEngine;

namespace CoachMaster.UI
{
    public static class UiTheme
    {
        public static readonly Color Canvas = new Color(0.035f, 0.075f, 0.15f, 1f);
        public static readonly Color Panel = new Color(0.065f, 0.14f, 0.25f, 1f);
        public static readonly Color PanelAlt = new Color(0.09f, 0.19f, 0.32f, 1f);
        public static readonly Color Accent = new Color(1f, 0.73f, 0.08f, 1f);
        public static readonly Color AccentDark = new Color(0.82f, 0.52f, 0.035f, 1f);
        public static readonly Color Danger = new Color(0.42f, 0.19f, 0.25f, 1f);
        public static readonly Color Text = new Color(0.95f, 0.97f, 1f, 1f);
        public static readonly Color Muted = new Color(0.67f, 0.76f, 0.87f, 1f);
        public static readonly Color Line = new Color(0.16f, 0.29f, 0.43f, 1f);
        public static readonly Color ButtonText = new Color(0.06f, 0.13f, 0.23f, 1f);

        public static Font Font
        {
            get
            {
                var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null)
                    font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                return font;
            }
        }
    }
}
