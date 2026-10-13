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

    // ---------- мелкий шрифт для подсказок клавиш (5 px высотой, ширина буквы своя) ----------
    static readonly Dictionary<char, string> small = new Dictionary<char, string>
    {
        ['A'] = ".#.|#.#|###|#.#|#.#", ['Б'] = "###|#..|##.|#.#|##.", ['В'] = "##.|#.#|##.|#.#|##.",
        ['Г'] = "###|#..|#..|#..|#..", ['Д'] = ".##.|.#.#|.#.#|####|#..#", ['E'] = "###|#..|##.|#..|###",
        ['З'] = "##.|..#|.#.|..#|##.", ['И'] = "#..#|#.##|##.#|#..#|#..#", ['Й'] = ".##.|#..#|#.##|##.#|#..#",
        ['К'] = "#.#|#.#|##.|#.#|#.#", ['Л'] = ".##|#.#|#.#|#.#|#.#", ['M'] = "#...#|##.##|#.#.#|#...#|#...#",
        ['Н'] = "#.#|#.#|###|#.#|#.#", ['N'] = "#..#|##.#|#.##|#..#|#..#", ['O'] = ".#.|#.#|#.#|#.#|.#.",
        ['П'] = "###|#.#|#.#|#.#|#.#", ['P'] = "##.|#.#|##.|#..|#..", ['R'] = "##.|#.#|##.|#.#|#.#",
        ['C'] = ".##|#..|#..|#..|.##", ['S'] = ".##|#..|.#.|..#|##.", ['T'] = "###|.#.|.#.|.#.|.#.",
        ['У'] = "#.#|#.#|.##|..#|##.", ['Ч'] = "#.#|#.#|.##|..#|..#", ['Ш'] = "#.#.#|#.#.#|#.#.#|#.#.#|#####",
        ['Ы'] = "#...#|#...#|##..#|#.#.#|##..#", ['Ь'] = "#..|#..|##.|#.#|##.", ['Я'] = ".##|#.#|.##|#.#|#.#",
        ['Ж'] = "#.#.#|#.#.#|.###.|#.#.#|#.#.#", ['Ф'] = ".#.|###|#.#|###|.#.", ['Х'] = "#.#|#.#|.#.|#.#|#.#",
        ['L'] = "#..|#..|#..|#..|###", ['B'] = "##.|#.#|##.|#.#|##.", ['U'] = "#.#|#.#|#.#|#.#|###",
        [' '] = "..|..|..|..|..", ['>'] = "#..|.#.|..#|.#.|#..", ['<'] = "..#|.#.|#..|.#.|..#",
    };
    static readonly Dictionary<char, Texture2D> smallCache = new Dictionary<char, Texture2D>();
    public const float SmallHeight = 5;

    static string SmallDef(char c)
    {
        // кириллица, совпадающая с латиницей по начертанию
        switch (c) { case 'А': c = 'A'; break; case 'Е': case 'Ё': c = 'E'; break; case 'М': c = 'M'; break; case 'О': c = 'O'; break;
                     case 'Р': c = 'P'; break; case 'С': c = 'C'; break; case 'Т': c = 'T'; break; }
        return small.TryGetValue(c, out var d) ? d : null;
    }

    static Texture2D SmallGlyph(char c, string def)
    {
        if (smallCache.TryGetValue(c, out var tex) && tex != null) return tex;
        var rows = def.Split('|');
        int w = rows[0].Length;
        tex = new Texture2D(w, 5, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < 5; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, 4 - y, rows[y][x] == '#' ? Color.white : Color.clear);
        tex.Apply();
        smallCache[c] = tex;
        return tex;
    }

    // Ширина мелкой надписи в экранных пикселях (scale — экранных пикселей в пикселе игры)
    public static float SmallWidth(string text, float scale)
    {
        float w = 0;
        foreach (char ch in text.ToUpperInvariant())
        {
            var def = SmallDef(ch) ?? small['O'];
            w += (def.IndexOf('|') + 1) * scale;
        }
        return Mathf.Max(0, w - scale);
    }

    // Мелкая надпись: (x, y) — левый верх, без выравнивания
    public static void DrawSmall(string text, float x, float y, float scale, Color color)
    {
        x = Mathf.Round(x); y = Mathf.Round(y);
        var prev = GUI.color;
        GUI.color = color;
        foreach (char ch in text.ToUpperInvariant())
        {
            var def = SmallDef(ch) ?? small['O'];
            int w = def.IndexOf('|');
            if (ch != ' ') GUI.DrawTexture(new Rect(x, y, w * scale, 5 * scale), SmallGlyph(ch, def));
            x += (w + 1) * scale;
        }
        GUI.color = prev;
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

    // Текст рисуется десятки раз за кадр (контуры, тени, две фазы IMGUI), поэтому перевод в верхний регистр
    // и измеренная ширина запоминаются: повторная отрисовка той же строки не создаёт мусора.
    const int CacheLimit = 1024;   // печатающиеся и меняющиеся строки не должны копиться бесконечно
    static readonly Dictionary<string, string> upperCache = new Dictionary<string, string>();
    static readonly Dictionary<(string, int), float> widthCache = new Dictionary<(string, int), float>();
    static readonly GUIContent measureContent = new GUIContent();

    static string Upper(string text)
    {
        if (upperCache.TryGetValue(text, out var upper)) return upper;
        upper = text.ToUpperInvariant();
        if (upperCache.Count >= CacheLimit) upperCache.Clear();
        upperCache[text] = upper;
        if (!ReferenceEquals(upper, text)) upperCache[upper] = upper;
        return upper;
    }

    public static float Width(string text, float scale)
    {
        text = Upper(text);
        if (MainFont == null) return (text.Length * Advance - 1) * scale;
        var style = StyleFor(scale);
        var key = (text, style.fontSize);
        if (widthCache.TryGetValue(key, out float width)) return width;
        measureContent.text = text;
        width = Mathf.Round(style.CalcSize(measureContent).x);
        if (widthCache.Count >= CacheLimit) widthCache.Clear();
        widthCache[key] = width;
        return width;
    }

    // y — верх заглавных букв; align: -1 — по левому краю от x, 0 — по центру, 1 — по правому краю
    public static void Draw(string text, float x, float y, float scale, Color color, int align = -1)
    {
        text = Upper(text);
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
