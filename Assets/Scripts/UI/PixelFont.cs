using System.Collections.Generic;
using UnityEngine;

namespace BlackHeart
{
// Текст игры. Основной шрифт — Hollowetica (пиксельный TTF из Resources/Fonts: 8 px на em,
// заглавные 7 px) рисуется без сглаживания целыми пикселями. Если шрифта нет — запасной битмап 5x7.
public static class PixelFont
{
    const int W = 5, H = 7, Advance = 6;

    static readonly Dictionary<char, string> glyphs = new Dictionary<char, string>();
    static readonly Dictionary<char, Texture2D> cache = new Dictionary<char, Texture2D>();

    static PixelFont()
    {
        Add(' ', ".....|.....|.....|.....|.....|.....|.....");
        Add('0', ".###.|#...#|#..##|#.#.#|##..#|#...#|.###.");
        Add('1', "..#..|.##..|..#..|..#..|..#..|..#..|.###.");
        Add('2', ".###.|#...#|....#|...#.|..#..|.#...|#####");
        Add('3', "#####|...#.|..#..|...#.|....#|#...#|.###.");
        Add('4', "...#.|..##.|.#.#.|#..#.|#####|...#.|...#.");
        Add('5', "#####|#....|####.|....#|....#|#...#|.###.");
        Add('6', "..##.|.#...|#....|####.|#...#|#...#|.###.");
        Add('7', "#####|....#|...#.|..#..|.#...|.#...|.#...");
        Add('8', ".###.|#...#|#...#|.###.|#...#|#...#|.###.");
        Add('9', ".###.|#...#|#...#|.####|....#|...#.|.##..");

        Add('A', ".###.|#...#|#...#|#####|#...#|#...#|#...#");
        Add('B', "####.|#...#|#...#|####.|#...#|#...#|####.");
        Add('C', ".###.|#...#|#....|#....|#....|#...#|.###.");
        Add('D', "####.|#...#|#...#|#...#|#...#|#...#|####.");
        Add('E', "#####|#....|#....|####.|#....|#....|#####");
        Add('F', "#####|#....|#....|####.|#....|#....|#....");
        Add('G', ".###.|#...#|#....|#.###|#...#|#...#|.####");
        Add('H', "#...#|#...#|#...#|#####|#...#|#...#|#...#");
        Add('I', ".###.|..#..|..#..|..#..|..#..|..#..|.###.");
        Add('J', "..###|...#.|...#.|...#.|...#.|#..#.|.##..");
        Add('K', "#...#|#..#.|#.#..|##...|#.#..|#..#.|#...#");
        Add('L', "#....|#....|#....|#....|#....|#....|#####");
        Add('M', "#...#|##.##|#.#.#|#.#.#|#...#|#...#|#...#");
        Add('N', "#...#|#...#|##..#|#.#.#|#..##|#...#|#...#");
        Add('O', ".###.|#...#|#...#|#...#|#...#|#...#|.###.");
        Add('P', "####.|#...#|#...#|####.|#....|#....|#....");
        Add('Q', ".###.|#...#|#...#|#...#|#.#.#|#..#.|.##.#");
        Add('R', "####.|#...#|#...#|####.|#.#..|#..#.|#...#");
        Add('S', ".####|#....|#....|.###.|....#|....#|####.");
        Add('T', "#####|..#..|..#..|..#..|..#..|..#..|..#..");
        Add('U', "#...#|#...#|#...#|#...#|#...#|#...#|.###.");
        Add('V', "#...#|#...#|#...#|#...#|#...#|.#.#.|..#..");
        Add('W', "#...#|#...#|#...#|#.#.#|#.#.#|#.#.#|.#.#.");
        Add('X', "#...#|#...#|.#.#.|..#..|.#.#.|#...#|#...#");
        Add('Y', "#...#|#...#|.#.#.|..#..|..#..|..#..|..#..");
        Add('Z', "#####|....#|...#.|..#..|.#...|#....|#####");

        Add('Б', "#####|#....|#....|####.|#...#|#...#|####.");
        Add('Г', "#####|#....|#....|#....|#....|#....|#....");
        Add('Д', ".###.|.#.#.|.#.#.|.#.#.|.#.#.|#####|#...#");
        Add('Ж', "#.#.#|#.#.#|.###.|..#..|.###.|#.#.#|#.#.#");
        Add('З', ".###.|#...#|....#|..##.|....#|#...#|.###.");
        Add('И', "#...#|#...#|#..##|#.#.#|##..#|#...#|#...#");
        Add('Й', ".#.#.|..#..|#...#|#..##|#.#.#|##..#|#...#");
        Add('Л', "..###|.#..#|.#..#|.#..#|.#..#|.#..#|#...#");
        Add('П', "#####|#...#|#...#|#...#|#...#|#...#|#...#");
        Add('У', "#...#|#...#|#...#|.####|....#|#...#|.###.");
        Add('Ф', "..#..|.###.|#.#.#|#.#.#|#.#.#|.###.|..#..");
        Add('Ц', "#..#.|#..#.|#..#.|#..#.|#..#.|#####|....#");
        Add('Ч', "#...#|#...#|#...#|.####|....#|....#|....#");
        Add('Ш', "#.#.#|#.#.#|#.#.#|#.#.#|#.#.#|#.#.#|#####");
        Add('Щ', "#.#.#|#.#.#|#.#.#|#.#.#|#.#.#|#####|....#");
        Add('Ъ', "##...|.#...|.#...|.###.|.#..#|.#..#|.###.");
        Add('Ы', "#...#|#...#|#...#|##..#|#.#.#|#.#.#|##..#");
        Add('Ь', "#....|#....|#....|####.|#...#|#...#|####.");
        Add('Э', ".###.|#...#|....#|.####|....#|#...#|.###.");
        Add('Ю', "#..#.|#.#.#|#.#.#|###.#|#.#.#|#.#.#|#..#.");
        Add('Я', ".####|#...#|#...#|.####|..#.#|.#..#|#...#");

        // Кириллица, совпадающая с латиницей по начертанию
        foreach (var pair in new[] { "АA", "ВB", "ЕE", "ЁE", "КK", "МM", "НH", "ОO", "РP", "СC", "ТT", "ХX" })
            glyphs[pair[0]] = glyphs[pair[1]];

        Add('.', ".....|.....|.....|.....|.....|.##..|.##..");
        Add(',', ".....|.....|.....|.....|.##..|..#..|.#...");
        Add('!', "..#..|..#..|..#..|..#..|..#..|.....|..#..");
        Add('?', ".###.|#...#|....#|...#.|..#..|.....|..#..");
        Add(':', ".....|.##..|.##..|.....|.##..|.##..|.....");
        Add('*', ".....|..#..|#.#.#|.###.|#.#.#|..#..|.....");
        Add('/', "....#|....#|...#.|..#..|.#...|#....|#....");
        Add('-', ".....|.....|.....|#####|.....|.....|.....");
        Add('+', ".....|..#..|..#..|#####|..#..|..#..|.....");
        Add('<', "...#.|..#..|.#...|#....|.#...|..#..|...#.");
        Add('>', ".#...|..#..|...#.|....#|...#.|..#..|.#...");
        Add('%', "##..#|##..#|...#.|..#..|.#...|#..##|#..##");
        Add('(', "...#.|..#..|.#...|.#...|.#...|..#..|...#.");
        Add(')', ".#...|..#..|...#.|...#.|...#.|..#..|.#...");
    }

    static void Add(char c, string rows) => glyphs[c] = rows;

    static Texture2D Glyph(char c)
    {
        if (cache.TryGetValue(c, out var tex) && tex != null) return tex;
        if (!glyphs.TryGetValue(c, out var def)) def = glyphs['?'];

        var rows = def.Split('|');
        tex = new Texture2D(W, H, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
            tex.SetPixel(x, H - 1 - y, rows[y][x] == '#' ? Color.white : Color.clear);
        tex.Apply();
        cache[c] = tex;
        return tex;
    }

    // ---------- Hollowetica ----------
    const string FontPath = "Fonts/Hollowetica";
    const float EmPixels = 8f;          // пикселей шрифта на em
    const float CapOffset = 2f;         // от верха строки до верха заглавных (ascent 9 − cap 7)
    const float LineHeight = 12f;

    static Font font;
    static bool fontLoaded;
    static GUIStyle style;

    static Font MainFont
    {
        get
        {
            if (!fontLoaded || font == null)
            {
                fontLoaded = true;
                font = Resources.Load<Font>(FontPath);
            }
            return font;
        }
    }

    static GUIStyle StyleFor(float scale)
    {
        if (style == null || style.font != MainFont)
        {
            style = new GUIStyle
            {
                font = MainFont,
                alignment = TextAnchor.UpperLeft,
                wordWrap = false,
                clipping = TextClipping.Overflow,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
            };
        }
        style.fontSize = Mathf.Max(1, Mathf.RoundToInt(EmPixels * scale));
        return style;
    }

    public static float Width(string text, float scale)
    {
        text = text.ToUpperInvariant();
        if (MainFont == null) return (text.Length * Advance - 1) * scale;
        return Mathf.Round(StyleFor(scale).CalcSize(new GUIContent(text)).x);
    }

    // y — верх заглавных букв; align: -1 — по левому краю от x, 0 — по центру, 1 — по правому краю
    public static void Draw(string text, float x, float y, float scale, Color color, int align = -1)
    {
        text = text.ToUpperInvariant();
        float w = Width(text, scale);
        if (align == 0) x -= w / 2f;
        else if (align > 0) x -= w;
        x = Mathf.Round(x);
        y = Mathf.Round(y);

        if (MainFont != null)
        {
            var st = StyleFor(scale);
            var tex = MainFont.material != null ? MainFont.material.mainTexture : null;
            if (tex != null) tex.filterMode = FilterMode.Point;   // без размытия пикселей
            st.normal.textColor = color;
            GUI.Label(new Rect(x, y - CapOffset * scale, w + scale, LineHeight * scale), text, st);
            return;
        }

        var prev = GUI.color;
        GUI.color = color;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] != ' ')
                GUI.DrawTexture(new Rect(x + i * Advance * scale, y, W * scale, H * scale), Glyph(text[i]));
        }
        GUI.color = prev;
    }
}
}
