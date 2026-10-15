using UnityEngine;

namespace CoachMaster.UI
{
    public static class UiTheme
    {
        public static readonly Color Canvas = Hex("07111C");
        public static readonly Color Header = Hex("040A14");
        public static readonly Color Panel = Hex("102033");
        public static readonly Color PanelAlt = Hex("17304A");
        public static readonly Color KitStage = Hex("0C1A2A");
        public static readonly Color Accent = Hex("2E9BFF");
        public static readonly Color AccentDark = Hex("16324C");
        public static readonly Color AccentSoft = Hex("7EC8FF");
        public static readonly Color Danger = Hex("8E2E3A");
        public static readonly Color Text = Hex("F3F7FB");
        public static readonly Color Muted = Hex("9BB0C4");
        public static readonly Color Line = Hex("2E9BFF");
        public static readonly Color ButtonText = Hex("FFFFFF");

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

        static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString("#" + value, out var color);
            return color;
        }
    }
}
