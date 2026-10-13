using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BlackHeart
{
    // Отрисовка главного меню (IMGUI). Страницы, выбор пунктов и ввод — в MainMenu.cs.
    public partial class MainMenu
    {
        void OnGUI()
        {
            if (!IsOpen || gm == null) return;
            GUI.depth = -5;
            s = gm.Pixel.PixelScale;
            bool repaint = Event.current.type == EventType.Repaint;
            if (repaint) items.Clear();

            DrawShade();
            DrawLogo();

            if (page == Page.Main) DrawMain(repaint);
            else if (page == Page.Record) DrawRecord(repaint);
            else DrawSettings(repaint);

            if (!Application.isMobilePlatform) DrawHints();
        }

        // ---------- примитивы (координаты — пиксели низкого разрешения) ----------

        const float ColumnX = 80f;   // центр колонки логотипа и пунктов

        Vector2 P(float x, float y) => gm.Pixel.RtToGui(x, y);

        Rect R(float x, float y, float w, float h)
        {
            Vector2 g = P(x, y);
            return new Rect(g.x, g.y, w * s, h * s);
        }

        void Box(Rect r, Color c)
        {
            var prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = prev;
        }


        void Text(string text, float x, float y, float scale, Color color, int align = 0, bool shadow = true)
        {
            Vector2 g = P(x, y);
            if (shadow) PixelFont.Draw(text, g.x + s * scale, g.y + s * scale, s * scale, Color.black, align);
            PixelFont.Draw(text, g.x, g.y, s * scale, color, align);
        }

        float TextWidth(string text, float scale) => PixelFont.Width(text, s * scale) / s;

        // ---------- общие элементы ----------

        // Ступенчатое затемнение слева — под логотипом и пунктами
        void DrawShade()
        {
            float h = gm.Pixel.RtHeight;
            Box(R(0, 0, 140, h), new Color(0, 0, 0, 0.72f));
            for (int i = 0; i < 10; i++)   // плавный спад к сцене мелкими ступенями
                Box(R(140 + i * 7, 0, 7, h), new Color(0, 0, 0, 0.72f * (1f - (i + 1) / 11f)));
        }

        // Логотип: крупное название с кровавой тенью, ниже подзаголовок между двумя чертами
        void DrawLogo()
        {
            const string title = "BLACK HEART";
            float scale = TextWidth(title, 3) <= 140 ? 3 : 2;
            Vector2 g = P(ColumnX, 16);
            PixelFont.Draw(title, g.x + 2 * s, g.y + 2 * s, s * scale, Blood, 0);
            PixelFont.Draw(title, g.x, g.y, s * scale, White, 0);

            const string sub = "HEAVENS FALL";
            float y = 16 + 7 * scale + 7;
            float w = TextWidth(sub, 1);
            Text(sub, ColumnX, y, 1, Red);
            Box(R(ColumnX - w / 2 - 14, y + 3, 10, 1), DarkRed);
            Box(R(ColumnX + w / 2 + 4, y + 3, 10, 1), DarkRed);
        }

        // Подсказки управления внизу слева: клавиша в рамке и действие
        void DrawHints()
        {
            // Внизу у самой правой границы
            float y = gm.Pixel.RtHeight - 10;
            float total = PixelUI.KeyHintWidth("ENTER", "ВЫБРАТЬ");
            if (page != Page.Main) total += 8 + PixelUI.KeyHintWidth("ESC", "НАЗАД");
            float x = gm.Pixel.RtWidth - 5 - total;
            x = PixelUI.KeyHint(x, y, "ENTER", "ВЫБРАТЬ");
            if (page != Page.Main) PixelUI.KeyHint(x + 8, y, "ESC", "НАЗАД");
        }

        // Пункт меню: выбранный — в белой рамке с тёмной подложкой, остальные серые.
        // Ширина рамки у всех пунктов страницы одинаковая (как полоса в референсе).
        void Item(int index, string text, float y, float frameWidth, bool repaint)
        {
            bool sel = index == selected;
            float x = ColumnX - frameWidth / 2;
            if (sel)
            {
                SelectFrame(x, y - 4, frameWidth, 15);
            }
            Text(text, ColumnX, y, 1, sel ? White : Gray);
            if (repaint) items.Add(R(x, y - 4, frameWidth, 15));
        }

        void SelectFrame(float x, float y, float w, float h) => PixelUI.SelectFrame(x, y, w, h);

        float FrameWidth(params string[] texts)
        {
            float w = 0;
            foreach (var t in texts) w = Mathf.Max(w, TextWidth(t, 1));
            return Mathf.Max(96, w + 24);
        }

        // ---------- страницы ----------

        void DrawMain(bool repaint)
        {
            float fw = FrameWidth(MainItems);
            for (int i = 0; i < MainItems.Length; i++) Item(i, MainItems[i], 74 + i * 18, fw, repaint);
        }

        void DrawRecord(bool repaint)
        {
            Text("РЕКОРД", ColumnX, 66, 1, Red);
            int best = gm.Score.Best;
            if (best > 0)
            {
                Vector2 g = P(ColumnX, 82);
                string num = best.ToString();
                PixelFont.Draw(num, g.x + 2 * s, g.y + 2 * s, 3 * s, Blood, 0);
                PixelFont.Draw(num, g.x, g.y, 3 * s, White, 0);
                Text("ЛУЧШИЙ СЧЁТ ЗА ВСЕ ИГРЫ", ColumnX, 110, 1, Gray);
            }
            else
            {
                Text("ЕЩЁ НЕТ РЕКОРДА", ColumnX, 86, 1, White);
                Text("СЫГРАЙ, ЧТОБЫ ЕГО ПОСТАВИТЬ", ColumnX, 100, 1, Gray);
            }
            Item(0, "НАЗАД", 132, FrameWidth("НАЗАД"), repaint);
        }

        void DrawSettings(bool repaint)
        {
            Text("НАСТРОЙКИ", ColumnX, 60, 1, Red);
            float fw = Mathf.Max(FrameWidth(VolumeControls.Labels), 128);
            for (int i = 0; i < VolumeControls.Count; i++)
            {
                Rect row = volume.DrawRow(i, ColumnX, 74 + i * VolumeControls.RowStep, fw, selected == i, repaint);
                if (repaint) items.Add(row);
            }
            Item(2, "НАЗАД", 140, FrameWidth("НАЗАД"), repaint);
        }
    }
}
