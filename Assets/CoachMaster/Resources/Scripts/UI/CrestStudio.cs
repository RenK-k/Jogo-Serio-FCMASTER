using System.Collections.Generic;
using CoachMaster.Data;
using CoachMaster.Domain;
using UnityEngine;

namespace CoachMaster.UI
{
    public static class CrestStudio
    {
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        static Sprite Fallback;

        public static Sprite For(Club club)
        {
            if (club == null)
            {
                if (Fallback == null)
                    Fallback = Draw(null);
                return Fallback;
            }
            WorldCatalog.PaintClub(club);
            var key = club.Id + "|" + club.ShortName + "|" + club.JerseyPrimaryHex + "|" + club.JerseySecondaryHex + "|" + club.JerseyShortsHex + "|" + club.KitPattern;
            if (Cache.TryGetValue(key, out var cached) && cached != null)
                return cached;

            var sprite = Draw(club);
            Cache[key] = sprite;
            return sprite;
        }

        static Sprite Draw(Club club)
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            var primary = club == null ? new Color(0.2f, 0.2f, 0.25f) : BroadcastUi.Parse(club.JerseyPrimaryHex, Color.white);
            var secondary = club == null ? Color.white : BroadcastUi.Parse(club.JerseySecondaryHex, Color.white);
            var tertiary = club == null ? new Color(0.12f, 0.14f, 0.18f) : BroadcastUi.Parse(club.JerseyShortsHex, Color.black);
            var style = Style(club);
            var shape = style % 8;
            var layout = style / 8;
            var outline = Color.Lerp(primary, new Color(0.035f, 0.045f, 0.08f), 0.7f);
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var u = (x + 0.5f) / size;
                    var v = (y + 0.5f) / size;
                    if (!InsideShape(shape, u, v, 0f))
                    {
                        pixels[y * size + x] = Color.clear;
                        continue;
                    }

                    var color = outline;
                    if (InsideShape(shape, u, v, 0.024f))
                    {
                        color = secondary;
                        if (InsideShape(shape, u, v, 0.045f))
                        {
                            color = BadgeColor(club, u, v, x, y, primary, secondary);
                            color = ApplyLayout(layout, u, v, color, secondary, tertiary);
                        }
                    }
                    pixels[y * size + x] = color;
                }
            }

            DrawMotif(pixels, size, layout, primary);
            var ink = Contrast(primary);
            Stamp(pixels, size, Initials(club), 51, Color.Lerp(ink, Color.black, 0.5f));
            Stamp(pixels, size, Initials(club), 53, ink);
            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 128f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        static bool InsideShape(int shape, float nx, float ny, float inset)
        {
            var dx = Mathf.Abs(nx - 0.5f);
            switch (shape)
            {
                case 0: // Shield
                    if (ny > 0.24f + inset)
                        return dx < 0.34f - inset && ny < 0.9f - inset;
                    return ny > 0.24f + inset - (0.34f - inset - dx) * 0.7f && dx < 0.34f - inset;
                case 1: // Roundel
                    return (nx - 0.5f) * (nx - 0.5f) + (ny - 0.5f) * (ny - 0.5f) < (0.44f - inset) * (0.44f - inset);
                case 2: // Diamond
                    return dx / (0.38f - inset) + Mathf.Abs(ny - 0.5f) / (0.46f - inset) < 1f;
                case 3: // Cut-corner crest
                    return nx > 0.16f + inset && nx < 0.84f - inset && ny > 0.12f + inset && ny < 0.9f - inset && !(dx > 0.27f - inset && ny < 0.24f + inset);
                case 4: // Tall hexagon
                    return ny > 0.1f + inset && ny < 0.9f - inset && dx < (ny < 0.26f ? 0.22f + (ny - 0.1f) * 1.35f - inset : ny > 0.74f ? 0.22f + (0.9f - ny) * 1.35f - inset : 0.34f - inset);
                case 5: // Pointed base with arched shoulders
                    if (ny > 0.52f)
                        return dx < 0.32f - inset && ny < 0.9f - inset;
                    return dx < 0.32f - inset && ny > 0.08f + inset + dx * 0.42f;
                case 6: // Broad, flat-bottom badge
                    return nx > 0.14f + inset && nx < 0.86f - inset && ny > 0.1f + inset && ny < 0.88f - inset && !(ny > 0.74f - inset && dx > 0.22f - inset && (ny - 0.74f + inset) > (0.36f - dx - inset) * 1.6f);
                default: // Narrow pentagon
                    if (ny > 0.28f + inset)
                        return dx < 0.3f - inset && ny < 0.91f - inset;
                    return ny > 0.1f + inset + dx * 0.55f && dx < 0.3f - inset;
            }
        }

        static int Style(Club club)
        {
            if (club == null || string.IsNullOrEmpty(club.Id))
                return 0;
            unchecked
            {
                var hash = 17;
                foreach (var c in club.Id)
                    hash = hash * 31 + c;
                return (hash & 0x7fffffff) % 64;
            }
        }

        static Color ApplyLayout(int layout, float u, float v, Color field, Color secondary, Color tertiary)
        {
            switch (layout)
            {
                case 0: return v > 0.59f && v < 0.65f ? secondary : field;
                case 1: return Mathf.Abs(u - 0.5f) < 0.065f ? secondary : field;
                case 2: return Mathf.Abs((u - 0.18f) - (1f - v)) < 0.055f ? secondary : field;
                case 3: return v > 0.57f ? tertiary : field;
                case 4: return v > 0.43f && v < 0.55f ? secondary : field;
                case 5: return v < 0.28f ? tertiary : field;
                case 6: return Mathf.Abs((u - 0.5f) + (v - 0.5f) * 0.68f) < 0.055f ? secondary : field;
                default: return v < 0.34f && Mathf.Abs(u - 0.5f) < (0.42f - v) * 0.55f ? secondary : field;
            }
        }

        static void DrawMotif(Color[] pixels, int size, int layout, Color primary)
        {
            var ink = Contrast(primary);
            for (var y = (int)(size * 0.66f); y < (int)(size * 0.83f); y++)
            {
                for (var x = (int)(size * 0.31f); x < (int)(size * 0.69f); x++)
                {
                    var u = (x + 0.5f) / size;
                    var v = (y + 0.5f) / size;
                    var dx = u - 0.5f;
                    var dy = v - 0.745f;
                    var radius = Mathf.Sqrt(dx * dx + dy * dy);
                    var draw = false;
                    switch (layout)
                    {
                        case 0:
                        case 7:
                            draw = InStar(u, v, 0.5f, 0.745f, 0.085f);
                            break;
                        case 1:
                            draw = radius < 0.078f && (Mathf.Abs(dx) < 0.008f || Mathf.Abs(dy) < 0.008f || Mathf.Abs(Mathf.Abs(dx) - Mathf.Abs(dy)) < 0.009f);
                            break;
                        case 2:
                            draw = InStar(u, v, 0.39f, 0.745f, 0.045f) || InStar(u, v, 0.5f, 0.745f, 0.055f) || InStar(u, v, 0.61f, 0.745f, 0.045f);
                            break;
                        case 3:
                            draw = v > 0.70f && v < 0.78f && (Mathf.Abs(dx) < 0.11f) && (v < 0.735f || Mathf.Abs(dx) > 0.045f);
                            break;
                        case 4:
                            draw = Mathf.Abs(dx) + Mathf.Abs(dy) < 0.085f && radius > 0.018f;
                            break;
                        case 5:
                            draw = Mathf.Abs(dx) < 0.042f && v > 0.70f && v < 0.80f || Mathf.Abs(dx) < (0.80f - v) * 0.55f && v >= 0.78f && v < 0.81f;
                            break;
                        case 6:
                            draw = radius < 0.078f && radius > 0.058f;
                            break;
                    }

                    if (draw)
                        Plot(pixels, size, x, y, ink);
                }
            }

        }

        static bool InStar(float x, float y, float cx, float cy, float outer)
        {
            var dx = x - cx;
            var dy = y - cy;
            var angle = Mathf.Atan2(dy, dx);
            var radius = Mathf.Sqrt(dx * dx + dy * dy);
            var edge = outer * (0.62f + 0.38f * Mathf.Cos(5f * angle));
            return radius <= edge;
        }

        static void Stamp(Color[] pixels, int size, string letters, int y, Color ink)
        {
            var text = string.IsNullOrEmpty(letters) ? "?" : letters;
            var start = size / 2 - text.Length * 7;
            for (var i = 0; i < text.Length; i++)
                Glyph(pixels, size, start + i * 14, y, text[i], ink);
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
