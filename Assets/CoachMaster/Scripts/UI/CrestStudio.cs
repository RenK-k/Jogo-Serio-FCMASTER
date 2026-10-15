using System.Collections.Generic;
using CoachMaster.Domain;
using UnityEngine;

namespace CoachMaster.UI
{
    public static class CrestStudio
    {
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite For(Club club)
        {
            var key = club == null ? "vazio" : club.Id;
            if (Cache.TryGetValue(key, out var cached) && cached != null)
                return cached;

            var sprite = Draw(club);
            Cache[key] = sprite;
            return sprite;
        }

        static Sprite Draw(Club club)
        {
            const int size = 96;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            var primary = club == null ? new Color(0.2f, 0.2f, 0.25f) : BroadcastUi.Parse(club.JerseyPrimaryHex, Color.white);
            var secondary = club == null ? Color.white : BroadcastUi.Parse(club.JerseySecondaryHex, Color.white);
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    if (!Inside(x, y, size))
                    {
                        pixels[y * size + x] = Color.clear;
                        continue;
                    }

                    var stripe = ((x + y) / 10) % 2 == 0;
                    var color = stripe ? primary : Color.Lerp(primary, secondary, 0.55f);
                    if (y > size * 0.72f)
                        color = secondary;
                    pixels[y * size + x] = color;
                }
            }

            Stamp(pixels, size, Initials(club), Contrast(primary));
            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 96f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        static bool Inside(int x, int y, int size)
        {
            var nx = (x + 0.5f) / size;
            var ny = (y + 0.5f) / size;
            if (ny > 0.18f)
                return nx > 0.16f && nx < 0.84f && ny < 0.9f;
            var belly = 0.5f - Mathf.Abs(nx - 0.5f);
            return ny > 0.18f - belly * 0.28f && nx > 0.16f && nx < 0.84f;
        }

        static void Stamp(Color[] pixels, int size, string letters, Color ink)
        {
            var text = string.IsNullOrEmpty(letters) ? "?" : letters;
            var start = size / 2 - text.Length * 7;
            for (var i = 0; i < text.Length; i++)
                Glyph(pixels, size, start + i * 14, 40, text[i], ink);
        }

        static void Glyph(Color[] pixels, int size, int ox, int oy, char letter, Color ink)
        {
            var rows = Mask(letter);
            for (var y = 0; y < rows.Length; y++)
            {
                for (var x = 0; x < 8; x++)
                {
                    if ((rows[y] & (1 << (7 - x))) == 0)
                        continue;
                    Plot(pixels, size, ox + x, oy + (rows.Length - 1 - y) * 2, ink);
                    Plot(pixels, size, ox + x, oy + (rows.Length - 1 - y) * 2 + 1, ink);
                }
            }
        }

        static int[] Mask(char letter)
        {
            switch (char.ToUpperInvariant(letter))
            {
                case 'A': return new[] { 0x3C, 0x66, 0x66, 0x7E, 0x66, 0x66, 0x66 };
                case 'B': return new[] { 0x7C, 0x66, 0x66, 0x7C, 0x66, 0x66, 0x7C };
                case 'C': return new[] { 0x3C, 0x66, 0x60, 0x60, 0x60, 0x66, 0x3C };
                case 'D': return new[] { 0x78, 0x6C, 0x66, 0x66, 0x66, 0x6C, 0x78 };
                case 'E': return new[] { 0x7E, 0x60, 0x60, 0x7C, 0x60, 0x60, 0x7E };
                case 'F': return new[] { 0x7E, 0x60, 0x60, 0x7C, 0x60, 0x60, 0x60 };
                case 'G': return new[] { 0x3C, 0x66, 0x60, 0x6E, 0x66, 0x66, 0x3C };
                case 'L': return new[] { 0x60, 0x60, 0x60, 0x60, 0x60, 0x60, 0x7E };
                case 'M': return new[] { 0x63, 0x77, 0x7F, 0x6B, 0x63, 0x63, 0x63 };
                case 'N': return new[] { 0x66, 0x76, 0x7E, 0x7E, 0x6E, 0x66, 0x66 };
                case 'O': return new[] { 0x3C, 0x66, 0x66, 0x66, 0x66, 0x66, 0x3C };
                case 'P': return new[] { 0x7C, 0x66, 0x66, 0x7C, 0x60, 0x60, 0x60 };
                case 'R': return new[] { 0x7C, 0x66, 0x66, 0x7C, 0x6C, 0x66, 0x66 };
                case 'S': return new[] { 0x3C, 0x66, 0x60, 0x3C, 0x06, 0x66, 0x3C };
                case 'T': return new[] { 0x7E, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18 };
                case 'U': return new[] { 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x3C };
                case 'V': return new[] { 0x66, 0x66, 0x66, 0x66, 0x66, 0x3C, 0x18 };
                default: return new[] { 0x3C, 0x66, 0x6E, 0x76, 0x66, 0x66, 0x3C };
            }
        }

        static void Plot(Color[] pixels, int size, int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= size || y >= size)
                return;
            pixels[y * size + x] = color;
        }

        static string Initials(Club club)
        {
            if (club == null || string.IsNullOrEmpty(club.ShortName))
                return "?";
            var parts = club.ShortName.Split(' ');
            if (parts.Length == 1)
                return parts[0].Substring(0, Mathf.Min(2, parts[0].Length)).ToUpperInvariant();
            return (parts[0].Substring(0, 1) + parts[parts.Length - 1].Substring(0, 1)).ToUpperInvariant();
        }

        static Color Contrast(Color color)
        {
            return color.grayscale > 0.62f ? new Color(0.08f, 0.06f, 0.1f) : Color.white;
        }
    }
}
