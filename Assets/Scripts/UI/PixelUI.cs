using UnityEngine;

namespace BlackHeart
{
    // Общие примитивы IMGUI в координатах низкого разрешения (пиксели RenderTexture, 320x180):
    // прямоугольники, рамки, спрайты, ромбы и рамка «как у поля боя». Используют пауза и экран проигрыша.
    public static class PixelUI
    {
        public static readonly Color Accent = new Color(0.91f, 0.14f, 0.14f);   // #E82424, как внутренняя линия рамки боя

        static PixelCamera Pixel => GameManager.I.Pixel;
        public static float Scale => Pixel.PixelScale;

        public static Rect R(float x, float y, float w, float h)
        {
            Vector2 g = Pixel.RtToGui(x, y);
            return new Rect(g.x, g.y, w * Scale, h * Scale);
        }

        public static void Box(float x, float y, float w, float h, Color c)
        {
            var prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(R(x, y, w, h), Texture2D.whiteTexture);
            GUI.color = prev;
        }

        public static void Frame(float x, float y, float w, float h, Color c)
        {
            Box(x, y, w, 1, c);
            Box(x, y + h - 1, w, 1, c);
            Box(x, y, 1, h, c);
            Box(x + w - 1, y, 1, h, c);
        }

        // Спрайт из Resources: левый верх в (x, y), масштаб в пикселях RT, с отражением и оттенком
        public static void Sprite(string id, float x, float y, bool flipX, bool flipY, Color color, float scale = 1)
        {
            var sprite = ArtLibrary.Get(id);
            var tex = sprite.texture;
            Rect r = sprite.rect;
            var uv = new Rect(r.x / tex.width, r.y / tex.height, r.width / tex.width, r.height / tex.height);
            if (flipX) uv = new Rect(uv.xMax, uv.y, -uv.width, uv.height);
            if (flipY) uv = new Rect(uv.x, uv.yMax, uv.width, -uv.height);
            var prev = GUI.color;
            GUI.color = color;
            GUI.DrawTextureWithTexCoords(R(x, y, r.width * scale, r.height * scale), tex, uv);
            GUI.color = prev;
        }

        // Ромб 5x5 из пикселей с центром в (x, y)
        public static void Diamond(float x, float y, Color outer, Color inner)
        {
            Box(x - 2, y, 5, 1, outer);
            Box(x, y - 2, 1, 5, outer);
            Box(x - 1, y - 1, 3, 3, outer);
            Box(x, y, 1, 1, inner);
        }

        // Рамка как у поля боя вокруг внутренней области (x, y, w, h): чёрный контур, белая полоса 3 px,
        // акцентная линия внутри, уголки-заклёпки и метки на серединах сторон (спрайты из gen_box.lua)
        public static void BattleFrame(float x, float y, float w, float h, Color accent)
        {
            Frame(x - 4, y - 4, w + 8, h + 8, Color.black);
            for (int i = 1; i <= 3; i++) Frame(x - i, y - i, w + 2 * i, h + 2 * i, Color.white);
            Frame(x + 1, y + 1, w - 2, h - 2, accent);

            float xr = x + w, yb = y + h;
            float nx = Mathf.Floor(x + w / 2) - 3, ny = Mathf.Floor(y + h / 2) - 3;
            void Pair(string id, string accentId, float px, float py, bool fx, bool fy)
            {
                Sprite(id, px, py, fx, fy, Color.white);
                Sprite(accentId, px, py, fx, fy, accent);
            }
            Pair(ArtIds.BoxCorner, ArtIds.BoxCornerAccent, x - 5, y - 5, false, false);
            Pair(ArtIds.BoxCorner, ArtIds.BoxCornerAccent, xr - 5, y - 5, true, false);
            Pair(ArtIds.BoxCorner, ArtIds.BoxCornerAccent, x - 5, yb - 5, false, true);
            Pair(ArtIds.BoxCorner, ArtIds.BoxCornerAccent, xr - 5, yb - 5, true, true);
            Pair(ArtIds.BoxNotchH, ArtIds.BoxNotchHAccent, nx, y - 5, false, false);
            Pair(ArtIds.BoxNotchH, ArtIds.BoxNotchHAccent, nx, yb - 1, false, true);
            Pair(ArtIds.BoxNotchV, ArtIds.BoxNotchVAccent, x - 5, ny, false, false);
            Pair(ArtIds.BoxNotchV, ArtIds.BoxNotchVAccent, xr - 1, ny, true, false);
        }

        // Текст пиксельным шрифтом с тенью на 1 px (масштаб — в пикселях RT). align: -1 влево, 0 центр, 1 вправо
        public static void Text(string text, float x, float y, float scale, Color color, int align = 0)
        {
            Vector2 g = Pixel.RtToGui(x, y);
            float s = Scale * scale;
            PixelFont.Draw(text, g.x + s, g.y + s, s, Color.black, align);
            PixelFont.Draw(text, g.x, g.y, s, color, align);
        }

        public static float Width(string text, float scale = 1) => PixelFont.Width(text, Scale * scale) / Scale;

        // Плашка выбранного пункта меню в стиле рамки боя: тень, тёмная подложка с кровавым отсветом,
        // белая рамка со срезанными углами, красная линия внутри (мягко пульсирует), заклёпки в углах
        // и красные завитки по бокам. Общая для главного меню и паузы.
        static readonly Color SelWhite = new Color(0xE8 / 255f, 0xE8 / 255f, 0xF0 / 255f);
        static readonly Color SelRed = new Color(0xE8 / 255f, 0x24 / 255f, 0x24 / 255f);
        static readonly Color SelDarkRed = new Color(0xC4 / 255f, 0x1C / 255f, 0x24 / 255f);
        static readonly Color SelBlood = new Color(0x46 / 255f, 0x04 / 255f, 0x0C / 255f);

        public static void SelectFrame(float x, float y, float w, float h)
        {
            Box(x + 1, y + 1, w, h, new Color(0, 0, 0, 0.5f));                        // тень
            Box(x, y, w, h, new Color(0.02f, 0.01f, 0.02f, 0.82f));                     // подложка
            for (int i = 0; i < 6; i++)                                                 // отсвет слева ступенями
                Box(x + 1 + i * 6, y + 1, 6, h - 2, new Color(SelBlood.r, SelBlood.g, SelBlood.b, 0.55f * (1f - i / 6f)));
            for (int i = 0; i < 6; i++)                                                 // и справа, слабее
                Box(x + w - 7 - i * 6, y + 1, 6, h - 2, new Color(SelBlood.r, SelBlood.g, SelBlood.b, 0.3f * (1f - i / 6f)));

            // белая рамка со срезанными углами
            Box(x + 1, y, w - 2, 1, SelWhite); Box(x + 1, y + h - 1, w - 2, 1, SelWhite);
            Box(x, y + 1, 1, h - 2, SelWhite); Box(x + w - 1, y + 1, 1, h - 2, SelWhite);

            // красная линия внутри — пульсирует ступенями
            float pulse = Mathf.Floor(Mathf.PingPong(Time.unscaledTime * 2f, 1f) * 3f) / 3f;
            var accent = Color.Lerp(SelDarkRed, SelRed, pulse);
            Box(x + 2, y + 2, w - 4, 1, accent); Box(x + 2, y + h - 3, w - 4, 1, accent);
            Box(x + 2, y + 3, 1, h - 6, accent); Box(x + w - 3, y + 3, 1, h - 6, accent);

            // заклёпки-ромбики в углах, как у рамки боя
            foreach (var c in new[] { new Vector2(x, y), new Vector2(x + w - 1, y), new Vector2(x, y + h - 1), new Vector2(x + w - 1, y + h - 1) })
            {
                Box(c.x, c.y, 1, 1, SelRed);
                Box(c.x - 1, c.y, 1, 1, SelDarkRed); Box(c.x + 1, c.y, 1, 1, SelDarkRed);
                Box(c.x, c.y - 1, 1, 1, SelDarkRed); Box(c.x, c.y + 1, 1, 1, SelDarkRed);
            }
            Ornaments(x, y, w, h);
        }

        // Красные пиксельные завитки по бокам рамки (справа как есть, слева зеркально)
        public static void Ornaments(float x, float y, float w, float h)
        {
            var sprite = ArtLibrary.Get(ArtIds.MenuOrnament);
            var tex = sprite.texture;
            Rect r = sprite.rect;
            var uv = new Rect(r.x / tex.width, r.y / tex.height, r.width / tex.width, r.height / tex.height);
            var flipped = new Rect(uv.xMax, uv.y, -uv.width, uv.height);
            float oy = y + Mathf.Floor((h - r.height) / 2f);
            GUI.DrawTextureWithTexCoords(R(x + w + 2, oy, r.width, r.height), tex, uv);
            GUI.DrawTextureWithTexCoords(R(x - 2 - r.width, oy, r.width, r.height), tex, flipped);
        }

        // Подсказка клавиши мелким шрифтом: клавиша в рамке и действие рядом (главное меню, вступление).
        // (x, y) — левый верх букв; возвращает правый край. action может быть пустым.
        public static readonly Color HintWhite = new Color(0xE8 / 255f, 0xE8 / 255f, 0xF0 / 255f);
        public static readonly Color HintGray = new Color(0x8C / 255f, 0x8C / 255f, 0x96 / 255f);

        static float SmallW(string text) => PixelFont.SmallWidth(text, Scale) / Scale;

        public static float KeyHintWidth(string key, string action) =>
            SmallW(key) + 6 + (string.IsNullOrEmpty(action) ? 0 : 4 + SmallW(action));

        public static float KeyHint(float x, float y, string key, string action)
        {
            float kw = SmallW(key);
            Frame(x, y - 2, kw + 6, 9, HintGray);
            Vector2 g = Pixel.RtToGui(x + 3, y);
            PixelFont.DrawSmall(key, g.x, g.y, Scale, HintWhite);
            x += kw + 6;
            if (string.IsNullOrEmpty(action)) return x;
            g = Pixel.RtToGui(x + 4, y);
            PixelFont.DrawSmall(action, g.x, g.y, Scale, HintGray);
            return x + 4 + SmallW(action);
        }
    }
}
