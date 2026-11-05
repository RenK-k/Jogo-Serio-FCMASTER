using UnityEngine;
using UnityEngine.UI;

namespace CoachMaster.UI
{
    public static class UiTheme
    {
        const string CanvasKey = "cm_theme_canvas";
        const string PanelKey = "cm_theme_panel";
        const string AccentKey = "cm_theme_accent";
        const string TextKey = "cm_theme_text";

        static readonly Color DefaultCanvas = Hex("2A0E62");
        static readonly Color DefaultPanel = Hex("3A167C");
        static readonly Color DefaultAccent = Hex("2DE2FF");
        static readonly Color DefaultText = Hex("F7F4FF");
        static Color _canvas = DefaultCanvas;
        static Color _panel = DefaultPanel;
        static Color _accent = DefaultAccent;
        static Color _text = DefaultText;

        public static Color Canvas { get; private set; }
        public static Color Header { get; private set; }
        public static Color Panel { get; private set; }
        public static Color PanelAlt { get; private set; }
        public static Color KitStage { get; private set; }
        public static Color Accent { get; private set; }
        public static Color AccentDark { get; private set; }
        public static Color AccentSoft { get; private set; }
        public static Color Danger => Hex("E23B4A");
        public static Color Text { get; private set; }
        public static Color Muted { get; private set; }
        public static Color Line { get; private set; }
        public static Color ButtonText { get; private set; }
        public static Color Score { get; private set; }
        public static Color Fitness => Hex("3DDC6A");
        public static Color FitnessLow => Hex("E2C14A");
        public static Color FitnessBad => Hex("E23B4A");
        public static Color Stripe { get; private set; }
        public static Color StripeAlt { get; private set; }
        public static Color Footer { get; private set; }
        public static Color Pitch { get; private set; }
        public static Color PitchLine { get; private set; }

        static UiTheme()
        {
            _canvas = LoadColor(CanvasKey, DefaultCanvas);
            _panel = LoadColor(PanelKey, DefaultPanel);
            _accent = LoadColor(AccentKey, DefaultAccent);
            _text = LoadColor(TextKey, DefaultText);
            RebuildPalette();
        }

        public static Color[] EditablePalette() => new[] { _canvas, _panel, _accent, _text };

        public static Color[] DefaultPalette() => new[] { DefaultCanvas, DefaultPanel, DefaultAccent, DefaultText };

        public static void ApplyPalette(Color[] palette)
        {
            if (palette == null || palette.Length < 4)
                return;

            var previous = CaptureUiColors();
            _canvas = Opaque(palette[0]);
            _panel = Opaque(palette[1]);
            _accent = Opaque(palette[2]);
            _text = Opaque(palette[3]);
            RebuildPalette();
            RepaintLoadedUi(previous);

            PlayerPrefs.SetString(CanvasKey, ColorUtility.ToHtmlStringRGB(_canvas));
            PlayerPrefs.SetString(PanelKey, ColorUtility.ToHtmlStringRGB(_panel));
            PlayerPrefs.SetString(AccentKey, ColorUtility.ToHtmlStringRGB(_accent));
            PlayerPrefs.SetString(TextKey, ColorUtility.ToHtmlStringRGB(_text));
            PlayerPrefs.Save();
        }

        static void RebuildPalette()
        {
            Canvas = _canvas;
            Header = Color.Lerp(_canvas, Color.black, 0.56f);
            Panel = _panel;
            PanelAlt = Color.Lerp(_panel, Color.white, 0.12f);
            KitStage = Color.Lerp(_panel, Color.black, 0.52f);
            Accent = _accent;
            AccentDark = Color.Lerp(_accent, Color.black, 0.58f);
            AccentSoft = Color.Lerp(_accent, Color.white, 0.35f);
            Text = _text;
            Muted = Color.Lerp(_text, _panel, 0.28f);
            Line = _text;
            ButtonText = Luminance(_accent) > 0.58f ? Color.black : Color.white;
            Score = _accent;
            Stripe = Color.Lerp(_canvas, _panel, 0.42f);
            StripeAlt = Color.Lerp(_canvas, Color.black, 0.30f);
            Footer = Color.Lerp(_canvas, Color.black, 0.78f);
            Pitch = _panel;
            PitchLine = Hex("F4F1FF");
        }

        static Color[] CaptureUiColors()
        {
            return new[]
            {
                Canvas, Header, Panel, PanelAlt, KitStage, Accent, AccentDark, AccentSoft,
                Text, Muted, Line, ButtonText, Score, Stripe, StripeAlt, Footer, Pitch, PitchLine
            };
        }

        static void RepaintLoadedUi(Color[] previous)
        {
            var graphics = Resources.FindObjectsOfTypeAll<Graphic>();
            for (var i = 0; i < graphics.Length; i++)
            {
                var graphic = graphics[i];
                if (graphic == null || !graphic.gameObject.scene.IsValid())
                    continue;

                if (graphic is Text text)
                {
                    var target = RemapText(text.color, previous);
                    if (target.HasValue)
                        text.color = WithAlpha(target.Value, text.color.a);
                    continue;
                }

                if (!(graphic is Image image))
                    continue;
                var imageTarget = RemapImage(image, previous);
                if (imageTarget.HasValue)
                    image.color = WithAlpha(imageTarget.Value, image.color.a);
            }
        }

        static Color? RemapText(Color current, Color[] previous)
        {
            for (var i = 0; i < previous.Length; i++)
                if (SameRgb(current, previous[i]))
                    return CurrentUiColor(i);
            if (SameRgb(current, Color.white))
                return Text;

            Color.RGBToHSV(current, out var hue, out var saturation, out var value);
            if (saturation > 0.25f && value > 0.48f && hue > 0.46f && hue < 0.62f)
                return Accent;
            if (saturation > 0.2f && hue > 0.68f && hue < 0.86f)
                return Muted;
            return null;
        }

        static Color? RemapImage(Image image, Color[] previous)
        {
            var current = image.color;
            for (var i = 0; i < previous.Length; i++)
                if (SameRgb(current, previous[i]))
                    return CurrentUiColor(i);

            // Re-tint UI surfaces only. Keep crests, portraits, kits and other artwork intact.
            if (image.sprite != null && image.sprite != BroadcastUi.Round
                && image.sprite != BroadcastUi.Circle && image.sprite != BroadcastUi.Ring)
                return null;
            if (SameRgb(current, Color.white))
                return Text;

            Color.RGBToHSV(current, out var hue, out var saturation, out var value);
            if (saturation > 0.18f && value < 0.78f && hue > 0.68f && hue < 0.86f)
                return value < 0.22f ? Header : value < 0.43f ? Panel : PanelAlt;
            if (saturation > 0.4f && value > 0.62f && hue > 0.46f && hue < 0.62f)
                return Accent;
            return null;
        }

        static Color CurrentUiColor(int index)
        {
            switch (index)
            {
                case 0: return Canvas;
                case 1: return Header;
                case 2: return Panel;
                case 3: return PanelAlt;
                case 4: return KitStage;
                case 5: return Accent;
                case 6: return AccentDark;
                case 7: return AccentSoft;
                case 8: return Text;
                case 9: return Muted;
                case 10: return Line;
                case 11: return ButtonText;
                case 12: return Score;
                case 13: return Stripe;
                case 14: return StripeAlt;
                case 15: return Footer;
                case 16: return Pitch;
                default: return PitchLine;
            }
        }

        static bool SameRgb(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.012f && Mathf.Abs(a.g - b.g) < 0.012f
                && Mathf.Abs(a.b - b.b) < 0.012f;
        }

        static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        static Color Opaque(Color color)
        {
            color.a = 1f;
            return color;
        }

        static float Luminance(Color color) => color.r * 0.2126f + color.g * 0.7152f + color.b * 0.0722f;

        static Color LoadColor(string key, Color fallback)
        {
            var raw = PlayerPrefs.GetString(key, "");
            if (string.IsNullOrEmpty(raw) || !ColorUtility.TryParseHtmlString("#" + raw, out var parsed))
                return fallback;
            return Opaque(parsed);
        }

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
