using System.Collections.Generic;
using CoachMaster.Data;
using CoachMaster.Domain;
using UnityEngine;

namespace CoachMaster.UI
{
    public static class CrestStudio
    {
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite For(Club club)
        {
            if (club == null)
                return Draw(club);
            WorldCatalog.PaintClub(club);
            var key = club.Id + "|" + club.JerseyPrimaryHex + "|" + club.KitPattern;
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
                    if (!Inside(club, x, y, size))
                    {
                        pixels[y * size + x] = Color.clear;
                        continue;
                    }

                    var u = (x + 0.5f) / size;
                    var v = (y + 0.5f) / size;
                    var color = BadgeColor(club, u, v, x, y, primary, secondary);
                    if (v > 0.78f)
                        color = Color.Lerp(color, secondary, 0.85f);
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

        static bool Inside(Club club, int x, int y, int size)
        {
            var nx = (x + 0.5f) / size;
            var ny = (y + 0.5f) / size;
            var dx = Mathf.Abs(nx - 0.5f);
            var shape = Shape(club);
            switch (shape)
            {
                case 0: // Shield
                    if (ny > 0.24f)
                        return dx < 0.34f && ny < 0.9f;
                    return ny > 0.24f - (0.34f - dx) * 0.7f && dx < 0.34f;
                case 1: // Roundel
                    return (nx - 0.5f) * (nx - 0.5f) + (ny - 0.5f) * (ny - 0.5f) < 0.19f;
                case 2: // Diamond
                    return dx / 0.38f + Mathf.Abs(ny - 0.5f) / 0.46f < 1f;
                case 3: // Cut-corner crest
                    return nx > 0.16f && nx < 0.84f && ny > 0.12f && ny < 0.9f && !(dx > 0.27f && ny < 0.24f);
                case 4: // Tall hexagon
                    return ny > 0.1f && ny < 0.9f && dx < (ny < 0.26f ? 0.22f + (ny - 0.1f) * 1.35f : ny > 0.74f ? 0.22f + (0.9f - ny) * 1.35f : 0.34f);
                case 5: // Pointed base with arched shoulders
                    if (ny > 0.52f)
                        return dx < 0.32f && ny < 0.9f;
                    return dx < 0.32f && ny > 0.08f + dx * 0.42f;
                case 6: // Broad, flat-bottom badge
                    return nx > 0.14f && nx < 0.86f && ny > 0.1f && ny < 0.88f && !(ny > 0.74f && dx > 0.22f && (ny - 0.74f) > (0.36f - dx) * 1.6f);
                default: // Narrow pentagon
                    if (ny > 0.28f)
                        return dx < 0.3f && ny < 0.91f;
                    return ny > 0.1f + dx * 0.55f && dx < 0.3f;
            }
        }

        static int Shape(Club club)
        {
            if (club == null || string.IsNullOrEmpty(club.Id))
                return 0;
            unchecked
            {
                var hash = 17;
                foreach (var c in club.Id)
                    hash = hash * 31 + c;
                return (hash & 0x7fffffff) % 8;
            }
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
            var rows = Mask(Fold(letter));
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
                case 'H': return new[] { 0x66, 0x66, 0x66, 0x7E, 0x66, 0x66, 0x66 };
                case 'I': return new[] { 0x3C, 0x18, 0x18, 0x18, 0x18, 0x18, 0x3C };
                case 'J': return new[] { 0x06, 0x06, 0x06, 0x06, 0x66, 0x66, 0x3C };
                case 'K': return new[] { 0x66, 0x6C, 0x78, 0x70, 0x78, 0x6C, 0x66 };
                case 'L': return new[] { 0x60, 0x60, 0x60, 0x60, 0x60, 0x60, 0x7E };
                case 'M': return new[] { 0x63, 0x77, 0x7F, 0x6B, 0x63, 0x63, 0x63 };
                case 'N': return new[] { 0x66, 0x76, 0x7E, 0x7E, 0x6E, 0x66, 0x66 };
                case 'O': return new[] { 0x3C, 0x66, 0x66, 0x66, 0x66, 0x66, 0x3C };
                case 'P': return new[] { 0x7C, 0x66, 0x66, 0x7C, 0x60, 0x60, 0x60 };
                case 'Q': return new[] { 0x3C, 0x66, 0x66, 0x66, 0x6E, 0x3C, 0x07 };
                case 'R': return new[] { 0x7C, 0x66, 0x66, 0x7C, 0x6C, 0x66, 0x66 };
                case 'S': return new[] { 0x3C, 0x66, 0x60, 0x3C, 0x06, 0x66, 0x3C };
                case 'T': return new[] { 0x7E, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18 };
                case 'U': return new[] { 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x3C };
                case 'V': return new[] { 0x66, 0x66, 0x66, 0x66, 0x66, 0x3C, 0x18 };
                case 'W': return new[] { 0x63, 0x63, 0x63, 0x6B, 0x7F, 0x77, 0x63 };
                case 'X': return new[] { 0x66, 0x66, 0x3C, 0x18, 0x3C, 0x66, 0x66 };
                case 'Y': return new[] { 0x66, 0x66, 0x3C, 0x18, 0x18, 0x18, 0x18 };
                case 'Z': return new[] { 0x7E, 0x06, 0x0C, 0x18, 0x30, 0x60, 0x7E };
                default: return new[] { 0x3C, 0x66, 0x6E, 0x76, 0x66, 0x66, 0x3C };
            }
        }

        static Color BadgeColor(Club club, float u, float v, int x, int y, Color primary, Color secondary)
        {
            var pattern = club == null ? KitPattern.Solid : club.KitPattern;
            switch (pattern)
            {
                case KitPattern.VerticalStripes:
                    return (x / 8) % 2 == 0 ? primary : secondary;
                case KitPattern.HorizontalHoops:
                    return (y / 8) % 2 == 0 ? primary : secondary;
                case KitPattern.Halves:
                    return u < 0.5f ? primary : secondary;
                case KitPattern.Sash:
                    return Mathf.Abs((u - 0.15f) - (1f - v)) < 0.14f ? secondary : primary;
                case KitPattern.ChestBand:
                    return v > 0.4f && v < 0.58f ? secondary : primary;
                case KitPattern.Pinstripe:
                    return x % 7 == 0 ? secondary : primary;
                default:
                    return primary;
            }
        }

        static char Fold(char letter)
        {
            switch (char.ToUpperInvariant(letter))
            {
                case 'Á':
                case 'À':
                case 'Ã':
                case 'Â':
                    return 'A';
                case 'É':
                case 'È':
                case 'Ê':
                    return 'E';
                case 'Í':
                    return 'I';
                case 'Ó':
                case 'Õ':
                case 'Ô':
                    return 'O';
                case 'Ú':
                    return 'U';
                case 'Ç':
                    return 'C';
                default:
                    return char.ToUpperInvariant(letter);
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
            var parts = club.ShortName.Split(new[] { ' ', '-' }, System.StringSplitOptions.RemoveEmptyEntries);
            var words = new List<string>();
            foreach (var part in parts)
            {
                var lower = part.ToLowerInvariant();
                if (lower == "de" || lower == "da" || lower == "do" || lower == "du" || lower == "d" || lower == "la" || lower == "le" || lower == "les")
                    continue;
                words.Add(part);
            }

            if (words.Count == 0)
                words.Add(parts.Length == 0 ? club.ShortName : parts[0]);
            if (words.Count == 1)
                return words[0].Substring(0, Mathf.Min(2, words[0].Length)).ToUpperInvariant();
            return (words[0].Substring(0, 1) + words[words.Count - 1].Substring(0, 1)).ToUpperInvariant();
        }

        static Color Contrast(Color color)
        {
            return color.grayscale > 0.62f ? new Color(0.08f, 0.06f, 0.1f) : Color.white;
        }
    }
}
