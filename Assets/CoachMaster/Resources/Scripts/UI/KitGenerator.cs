using CoachMaster.Domain;
using UnityEngine;

namespace CoachMaster.UI
{
    public static class KitGenerator
    {
        public static Texture2D Create(Club club, int width = 256, int height = 320)
        {
            Parse(club.JerseyPrimaryHex, out var primary);
            Parse(club.JerseySecondaryHex, out var secondary);
            var pixels = new Color32[width * height];
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(0, 0, 0, 0);

            FillShirt(pixels, width, height, club.KitPattern, primary, secondary);
            DrawCollar(pixels, width, height, secondary);
            DrawNumber(pixels, width, height, Contrast(primary, secondary));

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = club.KitSlug + "_kit";
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        static void FillShirt(Color32[] pixels, int w, int h, KitPattern pattern, Color32 primary, Color32 secondary)
        {
            for (var y = 0; y < h; y++)
            {
                var v = y / (float)(h - 1);
                for (var x = 0; x < w; x++)
                {
                    var u = x / (float)(w - 1);
                    if (!InsideShirt(u, v))
                        continue;
                    var color = PatternColor(pattern, u, v, primary, secondary);
                    if (v > 0.78f && v < 0.90f && u > 0.42f && u < 0.58f)
                        color = Shade(color, 0.82f);
                    pixels[y * w + x] = color;
                }
            }
        }

        static void DrawCollar(Color32[] pixels, int w, int h, Color32 secondary)
        {
            for (var y = 0; y < h; y++)
            {
                var v = y / (float)(h - 1);
                if (v < 0.88f || v > 0.96f)
                    continue;
                for (var x = 0; x < w; x++)
                {
                    var u = x / (float)(w - 1);
                    if (u > 0.38f && u < 0.62f && Mathf.Abs(u - 0.5f) + (v - 0.88f) * 1.6f < 0.18f)
                        pixels[y * w + x] = secondary;
                }
            }
        }

        static void DrawNumber(Color32[] pixels, int w, int h, Color32 ink)
        {
            const string glyph = "111101111";
            var cell = 7;
            var originX = w / 2 - cell * 3 / 2;
            var originY = (int)(h * 0.62f);
            for (var gy = 0; gy < 5; gy++)
            for (var gx = 0; gx < 3; gx++)
            {
                if (gy == 0 || gy == 2 || gy == 4 || gx == 2 && gy == 1 || gx == 0 && gy == 3)
                {
                    for (var py = 0; py < cell; py++)
                    for (var px = 0; px < cell - 1; px++)
                    {
                        var x = originX + gx * cell + px;
                        var y = originY - gy * cell + py;
                        if (x >= 0 && x < w && y >= 0 && y < h)
                            pixels[y * w + x] = ink;
                    }
                }
            }
            _ = glyph;
        }

        static bool InsideShirt(float u, float v)
        {
            if (v < 0.08f || v > 0.96f)
                return false;
            var sleeve = v > 0.62f && v < 0.86f && (u < 0.28f || u > 0.72f) && u > 0.04f && u < 0.96f;
            var body = u > 0.28f && u < 0.72f;
            var neckCut = v > 0.88f && u > 0.4f && u < 0.6f;
            return (body || sleeve) && !neckCut;
        }

        static Color32 PatternColor(KitPattern pattern, float u, float v, Color32 primary, Color32 secondary)
        {
            switch (pattern)
            {
                case KitPattern.VerticalStripes:
                    return Mathf.Repeat((u - 0.24f) * 8.5f, 1f) < 0.5f ? primary : secondary;
                case KitPattern.HorizontalHoops:
                    return Mathf.Repeat((v - 0.46f) * 9f, 1f) < 0.5f ? primary : secondary;
                case KitPattern.Sash:
                    return Mathf.Abs((u - 0.18f) - (1f - v) * 0.55f) < 0.08f ? secondary : primary;
                case KitPattern.Halves:
                    return u < 0.5f ? primary : secondary;
                case KitPattern.ChestBand:
                    return v > 0.62f && v < 0.74f ? secondary : primary;
                case KitPattern.Pinstripe:
                    return Mathf.Repeat(u * 28f, 1f) < 0.18f ? secondary : primary;
                default:
                    return primary;
            }
        }

        static Color32 Contrast(Color32 primary, Color32 secondary)
        {
            var lum = (primary.r * 0.3f + primary.g * 0.59f + primary.b * 0.11f) / 255f;
            return lum > 0.55f ? Shade(secondary, 0.35f) : new Color32(255, 255, 255, 255);
        }

        static Color32 Shade(Color32 color, float mul)
        {
            return new Color32(
                (byte)Mathf.Clamp(color.r * mul, 0, 255),
                (byte)Mathf.Clamp(color.g * mul, 0, 255),
                (byte)Mathf.Clamp(color.b * mul, 0, 255),
                255);
        }

        static void Parse(string hex, out Color32 color)
        {
            if (!ColorUtility.TryParseHtmlString(hex, out var parsed))
                parsed = Color.white;
            color = parsed;
        }
    }
}
