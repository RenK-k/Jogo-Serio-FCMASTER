using System.Collections.Generic;
using UnityEngine;

namespace CoachMaster.UI
{
    public static class PortraitStudio
    {
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        static readonly Color[] Skin =
        {
            Hex("F3D2B5"), Hex("E0B08A"), Hex("C68642"), Hex("8D5524"),
            Hex("5C3317"), Hex("F6D7C3"), Hex("A86B45"), Hex("D9A066")
        };

        static readonly Color[] Hair =
        {
            Hex("1A120C"), Hex("3B2414"), Hex("6B3E26"), Hex("C4A574"),
            Hex("8C3A2E"), Hex("2C2C2C"), Hex("E6E0D4"), Hex("111111")
        };

        public static Sprite For(string id)
        {
            if (string.IsNullOrEmpty(id))
                id = "anon";
            if (Cache.TryGetValue(id, out var cached) && cached != null)
                return cached;

            var sprite = Draw(id);
            Cache[id] = sprite;
            return sprite;
        }

        static Sprite Draw(string id)
        {
            const int size = 96;
            var seed = Hash(id);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            var pixels = new Color[size * size];
            var skin = Skin[seed % Skin.Length];
            var hair = Hair[(seed / 7) % Hair.Length];
            var shirt = Color.Lerp(Hex("2A0E62"), Hex("F4F1FF"), ((seed / 13) % 10) / 12f);
            var bald = seed % 11 == 0;
            var beard = seed % 5 == 0;
            var glasses = seed % 9 == 0;
            var smile = (seed / 3) % 3;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x - 48f;
                    var dy = y - 46f;
                    var head = (dx * dx) / (26f * 26f) + (dy * dy) / (30f * 30f);
                    var color = Color.clear;
                    if (y < 24 && Mathf.Abs(dx) < 18f)
                        color = shirt;
                    if (y >= 18 && y < 32 && Mathf.Abs(dx) < 12f)
                        color = Darken(skin, 0.08f);
                    if (head <= 1f)
                        color = skin;
                    if (!bald && y > 58 && head <= 1.05f)
                        color = hair;
                    if (!bald && (seed / 5) % 3 != 0 && y > 52 && y < 70 && Mathf.Abs(dx) > 18f && Mathf.Abs(dx) < 30f && Mathf.Abs(y - 58) < 10f)
                        color = hair;
                    pixels[y * size + x] = color;
                }
            }

            Eye(pixels, size, 38, 50, seed);
            Eye(pixels, size, 58, 50, seed);
            Brow(pixels, size, 34, 58, hair);
            Brow(pixels, size, 52, 58, hair);
            Mouth(pixels, size, 48, 34, smile, Darken(skin, 0.35f));
            if (beard)
                Beard(pixels, size, hair);
            if (glasses)
                Glasses(pixels, size);

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 96f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        static void Eye(Color[] pixels, int size, int cx, int cy, int seed)
        {
            var iris = (seed / 17) % 3 == 0 ? Hex("6B8F71") : (seed / 17) % 3 == 1 ? Hex("6A5138") : Hex("3E4C68");
            Disc(pixels, size, cx, cy, 4.2f, Color.white);
            Disc(pixels, size, cx, cy, 2.3f, iris);
            Disc(pixels, size, cx, cy, 1.1f, Hex("1A120C"));
        }

        static void Brow(Color[] pixels, int size, int x0, int y, Color hair)
        {
            for (var i = 0; i < 12; i++)
                Plot(pixels, size, x0 + i, y + (i < 3 ? 0 : 1), hair);
        }

        static void Mouth(Color[] pixels, int size, int cx, int cy, int smile, Color ink)
        {
            for (var i = -6; i <= 6; i++)
            {
                var lift = smile == 2 ? (6 - Mathf.Abs(i)) / 4 : smile == 0 ? 0 : -(6 - Mathf.Abs(i)) / 6;
                Plot(pixels, size, cx + i, cy + lift, ink);
            }
        }

        static void Beard(Color[] pixels, int size, Color hair)
        {
            for (var y = 24; y < 40; y++)
            {
                for (var x = 36; x < 60; x++)
                {
                    var dx = (x - 48f) / 14f;
                    var dy = (y - 36f) / 10f;
                    if (dx * dx + dy * dy <= 1f && pixels[y * size + x].a > 0.5f)
                        pixels[y * size + x] = Color.Lerp(pixels[y * size + x], hair, 0.85f);
                }
            }
        }

        static void Glasses(Color[] pixels, int size)
        {
            var ink = new Color(0.12f, 0.1f, 0.16f, 1f);
            Ring(pixels, size, 38, 50, 6.5f, ink);
            Ring(pixels, size, 58, 50, 6.5f, ink);
            for (var x = 44; x < 52; x++)
                Plot(pixels, size, x, 51, ink);
        }

        static void Disc(Color[] pixels, int size, int cx, int cy, float radius, Color color)
        {
            var r = Mathf.CeilToInt(radius);
            for (var y = -r; y <= r; y++)
            {
                for (var x = -r; x <= r; x++)
                {
                    if (x * x + y * y <= radius * radius)
                        Plot(pixels, size, cx + x, cy + y, color);
                }
            }
        }

        static void Ring(Color[] pixels, int size, int cx, int cy, float radius, Color color)
        {
            var r = Mathf.CeilToInt(radius + 1f);
            for (var y = -r; y <= r; y++)
            {
                for (var x = -r; x <= r; x++)
                {
                    var dist = Mathf.Sqrt(x * x + y * y);
                    if (dist <= radius && dist >= radius - 1.3f)
                        Plot(pixels, size, cx + x, cy + y, color);
                }
            }
        }

        static void Plot(Color[] pixels, int size, int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= size || y >= size)
                return;
            pixels[y * size + x] = color;
        }

        static Color Darken(Color color, float amount)
        {
            return Color.Lerp(color, Color.black, amount);
        }

        static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString("#" + value, out var color);
            return color;
        }

        static int Hash(string value)
        {
            unchecked
            {
                var hash = 23;
                foreach (var c in value)
                    hash = hash * 31 + c;
                return hash & 0x7fffffff;
            }
        }
    }
}
