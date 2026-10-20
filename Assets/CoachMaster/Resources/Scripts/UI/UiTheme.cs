using UnityEngine;

namespace CoachMaster.UI
{
    public static class UiTheme
    {
        public static readonly Color Canvas = Hex("2A0E62");
        public static readonly Color Header = Hex("140828");
        public static readonly Color Panel = Hex("3A167C");
        public static readonly Color PanelAlt = Hex("4C1E96");
        public static readonly Color KitStage = Hex("180A36");
        public static readonly Color Accent = Hex("2DE2FF");
        public static readonly Color AccentDark = Hex("1C0C40");
        public static readonly Color AccentSoft = Hex("8FF4FF");
        public static readonly Color Danger = Hex("E23B4A");
        public static readonly Color Text = Hex("F7F4FF");
        public static readonly Color Muted = Hex("CDBFE6");
        public static readonly Color Line = Hex("FFFFFF");
        public static readonly Color ButtonText = Hex("07111C");
        public static readonly Color Score = Hex("2DE2FF");
        public static readonly Color Fitness = Hex("3DDC6A");
        public static readonly Color FitnessLow = Hex("E2C14A");
        public static readonly Color FitnessBad = Hex("E23B4A");
        public static readonly Color Stripe = Hex("381480");
        public static readonly Color StripeAlt = Hex("240C52");
        public static readonly Color Footer = Hex("07040F");
        public static readonly Color Pitch = Hex("3A1578");
        public static readonly Color PitchLine = Hex("F4F1FF");

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
