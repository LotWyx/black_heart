using System.Collections.Generic;
using UnityEngine;

namespace BlackHeart
{
    // Упрощённый запасной пиксель-арт (16 px на юнит) на случай, если PNG в Resources/Art не найден.
    // Палитра: '#' белый, 'r' красный, '+' серый, 'x' чёрный, '.' прозрачный.
    public static class PixelArt
    {
        public const float FallbackPPU = 16f;

        static readonly Dictionary<string, string[]> maps = new Dictionary<string, string[]>
        {
            [ArtIds.Soul] = new[]
            {
                "..rrr...rrr..",
                ".rrrrr.rrrrr.",
                "rrrrrrrrrrrrr",
                "rrrrrrrrrrrrr",
                "rrrrrrrrrrrrr",
                ".rrrrrrrrrrr.",
                "..rrrrrrrrr..",
                "...rrrrrrr...",
                "....rrrrr....",
                ".....rrr.....",
                "......r......",
            },
            [ArtIds.SoulBroken] = new[]
            {
                "..rrr...rrr..",
                ".rrrrr.rrrrr.",
                "rrrrrr.rrrrrr",
                "rrrrr.rrrrrrr",
                "rrrrrr.rrrrrr",
                ".rrrr.rrrrrr.",
                "..rrrr.rrrr..",
                "...rr.rrrr...",
                "....rr.rr....",
                ".....r.r.....",
                "......r......",
            },
            [ArtIds.SoulShard] = new[]
            {
                "rr.",
                "rrr",
                ".r.",
            },
            [ArtIds.Bullet] = new[]
            {
                "..####..",
                ".######.",
                "########",
                "########",
                "#######+",
                "######++",
                ".####++.",
                "..++++..",
            },
            [ArtIds.BulletCrack] = new[]
            {
                "........",
                "...x....",
                "...xx...",
                "..x..x..",
                ".x....x.",
                "....x...",
                "...x....",
                "........",
            },
            [ArtIds.EnemyGhost] = new[]
            {
                "........########........",
                "......############......",
                ".....##############.....",
                "....################....",
                "...##################...",
                "...##################...",
                "..####xxx######xxx####..",
                "..###xxxxx####xxxxx###..",
                "..###xxxxx####xxxxx###..",
                "..####xxx######xxx####..",
                "..####################..",
                "..########xxxx########..",
                "..#######xxxxxx#######..",
                "..#######xxxxxx#######..",
                "..########xxxx########..",
                "..####################..",
                "..####################..",
                "..####################..",
                "..##.####.####.####.##..",
            },
            [ArtIds.EnemyEye] = new[]
            {
                ".........######.........",
                ".......##########.......",
                ".....##############.....",
                "....################....",
                "...######xxxxxx######...",
                "..######xxxxxxxx######..",
                "..#####xxx++++xxx#####..",
                ".#####xxx++++++xxx#####.",
                ".#####xx++xxxx++xx#####.",
                ".#####xx++xxxx++xx#####.",
                ".#####xx++xxxx++xx#####.",
                ".#####xx++xxxx++xx#####.",
                ".#####xxx++++++xxx#####.",
                "..#####xxx++++xxx#####..",
                "..######xxxxxxxx######..",
                "...######xxxxxx######...",
                "....################....",
                ".....##############.....",
                ".......##########.......",
                ".........######.........",
            },
            [ArtIds.EnemySkull] = new[]
            {
                ".......##########.......",
                ".....##############.....",
                "....################....",
                "...##################...",
                "..####################..",
                "..####################..",
                "..####################..",
                "..###xxxxx####xxxxx###..",
                "..##xxxxxxx##xxxxxxx##..",
                "..##xxxxxxx##xxxxxxx##..",
                "..###xxxxx####xxxxx###..",
                "..#########xx#########..",
                "...#######xxxx#######...",
                "....################....",
                ".....##############.....",
                ".....#x#x#x##x#x#x#.....",
                ".....##############.....",
                "......############......",
            },
        };

        public static IEnumerable<string> AllIds
        {
            get
            {
                foreach (var id in maps.Keys) yield return id;
                yield return ArtIds.BackgroundSunset;
            }
        }

        public static Texture2D CreateTexture(string id)
        {
            if (id == ArtIds.BackgroundSunset) return Sunset(320, 180);
            if (!maps.TryGetValue(id, out var rows)) return null;

            int h = rows.Length, w = rows[0].Length;
            var tex = NewTexture(w, h);
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                string row = rows[h - 1 - y];
                for (int x = 0; x < w; x++)
                    px[y * w + x] = ColorOf(x < row.Length ? row[x] : '.');
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        // Минималистичный закат: приглушённое розово-жёлтое солнце и его блики на воде,
        // всё остальное — тусклые фиолетовые контуры на чёрном. 320x180 = 20x11.25 юнитов.
        static Texture2D Sunset(int w, int h)
        {
            var tex = NewTexture(w, h);
            var px = new Color[w * h];
            Color outline = new Color(0.22f, 0.10f, 0.30f);
            Color pink = new Color(0.62f, 0.22f, 0.40f);
            Color yellow = new Color(0.80f, 0.62f, 0.30f);

            int cx = w / 2, horizon = h * 4 / 9, r = h * 3 / 10;

            void Set(int x, int y, Color c)
            {
                if (x >= 0 && x < w && y >= 0 && y < h) px[y * w + x] = c;
            }

            void Line(int x0, int y0, int x1, int y1, Color c)
            {
                int dx = Mathf.Abs(x1 - x0), dy = -Mathf.Abs(y1 - y0);
                int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1, err = dx + dy;
                while (true)
                {
                    Set(x0, y0, c);
                    if (x0 == x1 && y0 == y1) break;
                    int e2 = 2 * err;
                    if (e2 >= dy) { err += dy; x0 += sx; }
                    if (e2 <= dx) { err += dx; y0 += sy; }
                }
            }

            // Горизонт и далёкие волны
            Line(0, horizon, w - 1, horizon, outline);
            for (int x = 0; x < w; x++)
            {
                if ((x / 6 + x / 17) % 3 != 0) Set(x, horizon - 58, outline);
                if ((x / 9 + x / 23) % 3 == 1) Set(x, horizon - 70, outline);
            }

            // Контуры гор по краям
            int[] mountain = { 0, 18, 22, 30, 40, 14, 62, 26, 86, 8, 104, 0 };
            for (int i = 0; i + 3 < mountain.Length; i += 2)
            {
                Line(mountain[i], horizon + mountain[i + 1], mountain[i + 2], horizon + mountain[i + 3], outline);
                Line(w - 1 - mountain[i], horizon + mountain[i + 1], w - 1 - mountain[i + 2], horizon + mountain[i + 3], outline);
            }

            // Тусклые звёзды
            int[] stars = { 14, 168, 52, 150, 97, 172, 141, 160, 186, 175, 231, 152, 268, 170, 305, 158, 30, 132, 290, 136 };
            for (int i = 0; i + 1 < stars.Length; i += 2) Set(stars[i], stars[i + 1], outline);

            // Солнце над горизонтом, с ретро-полосами у нижнего края
            for (int y = horizon + 1; y <= horizon + r; y++)
            {
                int k = y - horizon;
                float stripeZone = r * 0.45f;
                bool gap = k < stripeZone && (k % 6) < Mathf.RoundToInt(Mathf.Lerp(3f, 1f, k / stripeZone));
                if (gap) continue;
                Color c = Color.Lerp(pink, yellow, k / (float)r);
                for (int x = cx - r; x <= cx + r; x++)
                {
                    float dx = x - cx + 0.5f, dy = k - 0.5f;
                    if (dx * dx + dy * dy <= r * r) Set(x, y, c);
                }
            }

            // Блики солнца на воде: прерывистые полосы, короче и темнее книзу
            int[] offsets = { 3, 7, 12, 18, 25, 33 };
            for (int i = 0; i < offsets.Length; i++)
            {
                int y = horizon - offsets[i];
                int half = Mathf.RoundToInt(r * (0.95f - i * 0.13f));
                float fade = 1f - i * 0.14f;
                Color c = Color.Lerp(pink, yellow, 0.35f - i * 0.05f) * fade;
                c.a = 1f;
                for (int x = cx - half; x <= cx + half; x++)
                    if (((x + i * 13) * 7 % 11) < 8) Set(x, y, c);
            }

            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        static Color ColorOf(char c)
        {
            switch (c)
            {
                case '#': return Color.white;
                case 'r': return Color.red;
                case '+': return new Color(0.6f, 0.6f, 0.6f);
                case 'x': return Color.black;
                default: return Color.clear;
            }
        }

        static Texture2D NewTexture(int w, int h)
        {
            return new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
        }
    }
}
