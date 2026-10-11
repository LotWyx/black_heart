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
    }
}
