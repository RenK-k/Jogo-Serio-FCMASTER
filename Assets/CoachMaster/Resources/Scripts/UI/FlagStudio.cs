using System.Collections.Generic;
using UnityEngine;

namespace CoachMaster.UI
{
    public static class FlagStudio
    {
        const int Width = 120;
        const int Height = 76;
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite For(string nationality)
        {
            var key = Key(nationality);
            if (Cache.TryGetValue(key, out var cached))
                return cached;
            var pixels = new Color32[Width * Height];
            Fill(pixels, new Color32(245, 245, 241, 255));
            DrawFlag(key, pixels);
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            texture.name = "Flag_" + key;
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, Width, Height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            Cache[key] = sprite;
            return sprite;
        }

        static void DrawFlag(string key, Color32[] pixels)
        {
            var green = new Color32(0, 102, 0, 255);
            var red = new Color32(210, 16, 52, 255);
            var blue = new Color32(0, 83, 156, 255);
            var yellow = new Color32(255, 205, 0, 255);
            var white = new Color32(250, 250, 248, 255);
            if (key == "portugal")
            {
                Rect(pixels, 0, 0, 47, Height, green);
                Rect(pixels, 47, 0, Width, Height, red);
                Circle(pixels, 48, 38, 13, yellow);
                Circle(pixels, 48, 38, 8, red);
                Rect(pixels, 43, 29, 53, 47, white);
                Rect(pixels, 46, 31, 50, 44, blue);
                Rect(pixels, 44, 36, 52, 39, white);
                return;
            }

            if (key == "belgium")
            {
                Rect(pixels, 0, 0, Width / 3, Height, new Color32(20, 20, 20, 255));
                Rect(pixels, Width / 3, 0, Width * 2 / 3, Height, white);
                Rect(pixels, Width * 2 / 3, 0, Width, Height, yellow);
                return;
            }

            if (key == "italia" || key == "france" || key == "ireland")
            {
                var left = key == "italia" ? green : key == "france" ? blue : green;
                var right = red;
                Rect(pixels, 0, 0, Width / 3, Height, left);
                Rect(pixels, Width / 3, 0, Width * 2 / 3, Height, white);
                Rect(pixels, Width * 2 / 3, 0, Width, Height, right);
                return;
            }

            if (key == "germany") { Horizontal(pixels, new Color32(20, 20, 20, 255), red, yellow); return; }
            if (key == "spain") { Rect(pixels, 0, 0, Width, 19, red); Rect(pixels, 0, 19, Width, 57, yellow); Rect(pixels, 0, 57, Width, Height, red); return; }
            if (key == "netherlands") { Horizontal(pixels, red, white, blue); return; }
            if (key == "argentina" || key == "uruguay")
            {
                Horizontal(pixels, new Color32(116, 172, 223, 255), white, new Color32(116, 172, 223, 255));
                if (key == "uruguay") for (var y = 0; y < Height; y += 16) Rect(pixels, 0, y, Width, y + 6, white);
                Circle(pixels, Width / 2, Height / 2, 7, yellow);
                return;
            }
            if (key == "japan") { Circle(pixels, Width / 2, Height / 2, 18, red); return; }
            if (key == "southkorea") { Circle(pixels, Width / 2, Height / 2 + 4, 13, red); Circle(pixels, Width / 2, Height / 2 - 4, 13, blue); return; }
            if (key == "turkey") { Circle(pixels, 55, 38, 20, white); Circle(pixels, 64, 38, 16, red); Circle(pixels, 83, 38, 8, white); Circle(pixels, 87, 38, 6, red); return; }
            if (key == "england") { Rect(pixels, 50, 0, 70, Height, red); Rect(pixels, 0, 29, Width, 47, red); return; }
            if (key == "nigeria") { Rect(pixels, 0, 0, 40, Height, green); Rect(pixels, 80, 0, Width, Height, green); return; }
            if (key == "ghana") { Horizontal(pixels, red, yellow, green); Circle(pixels, Width / 2, Height / 2, 9, new Color32(25, 25, 25, 255)); return; }
            if (key == "senegal") { Rect(pixels, 0, 0, 40, Height, green); Rect(pixels, 40, 0, 80, Height, yellow); Rect(pixels, 80, 0, Width, Height, red); Star(pixels, Width / 2, Height / 2, 9, green); return; }
            if (key == "morocco") { Fill(pixels, red); Star(pixels, Width / 2, Height / 2, 17, green); return; }
            if (key == "brazil") { Fill(pixels, green); Diamond(pixels, Width / 2, Height / 2, 41, 28, yellow); Circle(pixels, Width / 2, Height / 2, 14, blue); return; }
            if (key == "colombia") { Rect(pixels, 0, 0, Width, 38, yellow); Rect(pixels, 0, 38, Width, 57, blue); Rect(pixels, 0, 57, Width, Height, red); return; }
            if (key == "drcongo") { Fill(pixels, new Color32(0, 92, 188, 255)); Diagonal(pixels, 20, yellow, 15); Diagonal(pixels, 9, red, 6); return; }
            if (key == "poland") { Horizontal(pixels, white, white, red); return; }
            if (key == "scotland") { Fill(pixels, blue); Diagonal(pixels, 12, white, 0); Diagonal(pixels, 12, white, 10); return; }
            if (key == "denmark") { Fill(pixels, red); Rect(pixels, 34, 0, 45, Height, white); Rect(pixels, 0, 32, Width, 44, white); return; }
            if (key == "sweden") { Fill(pixels, new Color32(0, 106, 167, 255)); Rect(pixels, 34, 0, 45, Height, yellow); Rect(pixels, 0, 32, Width, 44, yellow); return; }
            if (key == "austria") { Horizontal(pixels, red, white, red); return; }
            if (key == "croatia") { Horizontal(pixels, red, white, blue); return; }
            if (key == "serbia") { Horizontal(pixels, red, blue, white); return; }
            if (key == "hungary") { Horizontal(pixels, red, white, green); return; }
            if (key == "czechia") { Rect(pixels, 0, 0, Width, Height / 2, white); Rect(pixels, 0, Height / 2, Width, Height, red); for (var y = 6; y < Height - 6; y++) Rect(pixels, 0, y, Width / 2 - Mathf.Abs(y - Height / 2), y + 1, blue); return; }
            if (key == "switzerland") { Fill(pixels, red); Rect(pixels, 50, 18, 70, 58, white); Rect(pixels, 40, 28, 80, 48, white); return; }
            if (key == "romania") { Rect(pixels, 0, 0, Width / 3, Height, blue); Rect(pixels, Width / 3, 0, Width * 2 / 3, Height, yellow); Rect(pixels, Width * 2 / 3, 0, Width, Height, red); return; }
            if (key == "chile") { Rect(pixels, 0, 0, Width, Height / 2, white); Rect(pixels, 0, Height / 2, Width, Height, red); Rect(pixels, 0, Height / 2, 34, Height, blue); Circle(pixels, 17, 57, 5, white); return; }
            if (key == "paraguay") { Horizontal(pixels, red, white, blue); return; }
            if (key == "ecuador") { Rect(pixels, 0, 0, Width, 38, yellow); Rect(pixels, 0, 38, Width, 57, blue); Rect(pixels, 0, 57, Width, Height, red); return; }
            if (key == "peru") { Rect(pixels, 0, 0, Width / 3, Height, red); Rect(pixels, Width / 3, 0, Width * 2 / 3, Height, white); Rect(pixels, Width * 2 / 3, 0, Width, Height, red); return; }
            if (key == "venezuela") { Horizontal(pixels, yellow, blue, red); return; }
            if (key == "bolivia") { Horizontal(pixels, red, yellow, green); return; }
            Horizontal(pixels, blue, white, red);
        }

        static void Horizontal(Color32[] pixels, Color32 top, Color32 middle, Color32 bottom)
        {
            Rect(pixels, 0, 0, Width, Height / 3, bottom);
            Rect(pixels, 0, Height / 3, Width, Height * 2 / 3, middle);
            Rect(pixels, 0, Height * 2 / 3, Width, Height, top);
        }

        static void Fill(Color32[] pixels, Color32 color) { Rect(pixels, 0, 0, Width, Height, color); }

        static void Rect(Color32[] pixels, int x0, int y0, int x1, int y1, Color32 color)
        {
            for (var y = Mathf.Clamp(y0, 0, Height); y < Mathf.Clamp(y1, 0, Height); y++)
                for (var x = Mathf.Clamp(x0, 0, Width); x < Mathf.Clamp(x1, 0, Width); x++)
                    pixels[y * Width + x] = color;
        }

        static void Circle(Color32[] pixels, int cx, int cy, int radius, Color32 color)
        {
            var squared = radius * radius;
            for (var y = -radius; y <= radius; y++)
                for (var x = -radius; x <= radius; x++)
                    if (x * x + y * y <= squared && cx + x >= 0 && cx + x < Width && cy + y >= 0 && cy + y < Height)
                        pixels[(cy + y) * Width + cx + x] = color;
        }

        static void Diamond(Color32[] pixels, int cx, int cy, int rx, int ry, Color32 color)
        {
            for (var y = -ry; y <= ry; y++)
            {
                var span = rx * (ry - Mathf.Abs(y)) / ry;
                Rect(pixels, cx - span, cy + y, cx + span + 1, cy + y + 1, color);
            }
        }

        static void Star(Color32[] pixels, int cx, int cy, int radius, Color32 color)
        {
            Circle(pixels, cx, cy, radius, color);
            Circle(pixels, cx, cy, radius / 2, new Color32(255, 210, 0, 255));
            for (var i = 0; i < 5; i++)
            {
                var angle = -Mathf.PI * 0.5f + i * Mathf.PI * 2f / 5f;
                Circle(pixels, cx + Mathf.RoundToInt(Mathf.Cos(angle) * radius * 0.65f), cy + Mathf.RoundToInt(Mathf.Sin(angle) * radius * 0.65f), Mathf.Max(2, radius / 4), color);
            }
        }

        static void Diagonal(Color32[] pixels, int thickness, Color32 color, int offset)
        {
            for (var y = 0; y < Height; y++)
                for (var x = 0; x < Width; x++)
                    if (Mathf.Abs(y - (Height - x * Height / Width) - offset) <= thickness / 2)
                        pixels[y * Width + x] = color;
        }

        static string Key(string value)
        {
            if (string.IsNullOrEmpty(value)) return "portugal";
            var key = value.ToLowerInvariant().Replace(" ", "").Replace("-", "").Replace("á", "a").Replace("ã", "a").Replace("â", "a").Replace("é", "e").Replace("ê", "e").Replace("í", "i").Replace("ó", "o").Replace("õ", "o").Replace("ô", "o").Replace("ú", "u").Replace("ç", "c");
            switch (key)
            {
                case "brasil": return "brazil";
                case "japao": return "japan";
                case "coreiadosul": return "southkorea";
                case "turquia": return "turkey";
                case "alemanha": return "germany";
                case "espanha": return "spain";
                case "belgica": return "belgium";
                case "belgian":
                case "belgie":
                case "belgique": return "belgium";
                case "franca": return "france";
                case "inglaterra": return "england";
                case "paisesbaixos": return "netherlands";
                case "uruguai": return "uruguay";
                case "gana": return "ghana";
                case "irlanda": return "ireland";
                case "marrocos": return "morocco";
                case "rdcongo":
                case "congokinshasa": return "drcongo";
                case "polonia": return "poland";
                case "escocia": return "scotland";
                case "dinamarca": return "denmark";
                case "suecia": return "sweden";
                case "croacia": return "croatia";
                case "servia": return "serbia";
                case "hungria": return "hungary";
                case "republicacheca": return "czechia";
                case "suica": return "switzerland";
                case "romenia": return "romania";
                case "paraguai": return "paraguay";
                case "equador": return "ecuador";
            }
            return key;
        }
    }
}
