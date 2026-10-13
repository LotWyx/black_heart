using System.Collections.Generic;
using UnityEngine;

namespace BlackHeart
{
    // Отрисовка интерфейса боя (IMGUI). События, всплывающие надписи и «следы» полос — в HUD.cs.
    public partial class HUD
    {
        // ---------- отрисовка ----------

        void OnGUI()
        {
            if (gm == null) return;
            GUI.depth = 0;
            float s = gm.Pixel.UiScale;

            if (gm.CurrentState == GameManager.State.GameOver)
            {
                DrawGameOver(s);
                return;
            }
            if (gm.CurrentState == GameManager.State.Dying || gm.CurrentState == GameManager.State.Menu) return;

            Vector2 corner = gm.Pixel.RtToGui(0, 0);   // левый верх игрового поля (на широком экране — не край экрана)
            PixelFont.Draw(ScoreLabel(gm.Score.Score), corner.x + 6 * s, corner.y + 6 * s, s, Color.white);
            DrawEnemyInfo(s);

            Rect box = gm.Box.HudRect;
            Vector2 bottom = gm.Pixel.WorldToGui(new Vector2(box.center.x, box.yMin));
            float cx = bottom.x;
            float y = bottom.y + 6 * s;
            DrawPlayerHp(cx, y, s);

            float y2 = y + 12 * s;
            var phase = gm.Battle.Phase;
            bool tutorial = gm.Tutorial != null && gm.Tutorial.IsRunning;   // у обучения свои подсказки
            if (!tutorial && phase == BattlePhase.Attack)
            {
                if (Mathf.Repeat(Time.time, 0.5f) < 0.35f)
                    PixelFont.Draw("АТАКУЙ ВРАГА!", cx, y2, s, Red, 0);
                float w = 60 * s * Mathf.Clamp01(gm.Battle.AttackTimeLeft / gm.Battle.attackWindow);
                Bar(new Rect(cx - w / 2, y2 + 10 * s, w, 2 * s), Color.white);
            }
            else if (!tutorial && !string.IsNullOrEmpty(message))
            {
                PixelFont.Draw(Typed(message, messageTime), cx, y2, s, Color.white, 0);
            }

            foreach (var p in popups)
            {
                Vector2 g = gm.Pixel.WorldToGui(p.world);
                var c = p.color;   // без затухания: надпись просто исчезает в конце жизни
                // Пиксель шрифта всегда равен игровому: крупные цифры не увеличиваются, а становятся жирными.
                // Тёмный контур и тень вниз — цифры читаются на любом фоне.
                bool bold = p.scale > 1f;
                float px = g.x - (bold ? s / 2f : 0f), py = g.y - 3.5f * s;
                for (int k = 0; k <= (bold ? 1 : 0); k++)
                {
                    PixelFont.Draw(p.text, px + k * s, py + 2 * s, s, Ink, 0);
                    foreach (var o in Outline) PixelFont.Draw(p.text, px + (o.x + k) * s, py + o.y * s, s, Ink, 0);
                }
                for (int k = 0; k <= (bold ? 1 : 0); k++) PixelFont.Draw(p.text, px + k * s, py, s, c, 0);
            }
        }

        void DrawPlayerHp(float cx, float y, float s)
        {
            var player = gm.Player;
            string hp = HpLabel(player.Hp, player.MaxHp);
            float barW = player.MaxHp * 6;   // в пикселях игры: по 6 px на единицу здоровья
            float total = PixelFont.Width("HP", s) + 7 * s + (barW + 4) * s + 6 * s + PixelFont.Width(hp, s);
            float x = cx - total / 2;

            PixelFont.Draw("HP", x, y, s, Color.white);
            x += PixelFont.Width("HP", s) + 7 * s;
            DrawHealthBar(x + 2 * s, y, barW, player.Hp, player.MaxHp, playerTrail, GreenBar, s);   // игрок — зелёная
            x += (barW + 4) * s + 6 * s;
            PixelFont.Draw(hp, x, y, s, Color.white);
        }

        void DrawEnemyInfo(float s)
        {
            var e = gm.Battle.CurrentEnemy;
            if (e == null) return;
            Vector2 corner = gm.Pixel.RtToGui(gm.Pixel.RtWidth, 0);   // правый верх игрового поля
            float right = corner.x - (8 + PauseMenu.IconSize + 8) * s;   // правый угол занят иконкой паузы
            float top = corner.y + 6 * s;
            float barW = Mathf.Clamp(e.MaxHp * 10, 30, 64);
            DrawHealthBar(right - (barW + 2) * s, top, barW, e.Hp, e.MaxHp, trailEnemy == e ? enemyTrail : 1f, RedBar, s);   // враг — красная
            PixelFont.Draw(e.Definition.displayName, right - (barW + 4) * s - 6 * s, top, s, Color.white, 1);
        }

        // Полоса здоровья в пиксельном стиле. (x, y) — левый верх заливки в экранных координатах,
        // w — ширина заливки в пикселях игры. Снаружи: белая рамка 1 px и чёрный контур 1 px (выступают на 2 px),
        // уголки срезаны; внутри: заливка 5 px с бликом и тенью, «след» урона, деления по единицам здоровья.
        static void DrawHealthBar(float x, float y, float w, int value, int max, float trail, BarStyle st, float s)
        {
            const float H = 5;
            y += 1 * s;   // по центру строки текста (7 px)
            Rect R(float px, float py, float pw, float ph) => new Rect(x + px * s, y + py * s, pw * s, ph * s);

            // контур и рамка со срезанными углами
            Bar(R(-1, -2, w + 2, 1), Color.black); Bar(R(-1, H + 1, w + 2, 1), Color.black);
            Bar(R(-2, -1, 1, H + 2), Color.black); Bar(R(w + 1, -1, 1, H + 2), Color.black);
            Bar(R(0, -1, w, 1), Color.white); Bar(R(0, H, w, 1), Color.white);
            Bar(R(-1, 0, 1, H), Color.white); Bar(R(w, 0, 1, H), Color.white);
            Bar(R(-1, -1, 1, 1), Color.black); Bar(R(w, -1, 1, 1), Color.black);
            Bar(R(-1, H, 1, 1), Color.black); Bar(R(w, H, 1, 1), Color.black);

            Bar(R(0, 0, w, H), st.empty);
            float f = Mathf.Round(w * Mathf.Clamp01((float)value / max));
            float t = Mathf.Round(w * Mathf.Clamp01(trail));
            if (t > f) Bar(R(f, 0, t - f, H), st.trail);
            if (f > 0)
            {
                Bar(R(0, 0, f, H), st.fill);
                Bar(R(0, 0, f, 1), st.light);
                Bar(R(0, H - 1, f, 1), st.shade);
                Bar(R(0, 1, 1, H - 2), st.light);
            }
            // деления по единицам здоровья, если они не слишком частые
            float step = w / max;
            if (step >= 3)
                for (int i = 1; i < max; i++)
                    Bar(R(Mathf.Round(i * step), 1, 1, H - 1), new Color(0, 0, 0, 0.45f));
        }

        // ---------- экран проигрыша ----------
        // Всё в пикселях RT (320x180) через PixelUI. Фон — тёмная сетка, ползущая по диагонали, кровавое свечение
        // снизу и поднимающиеся искры; сверху разбитое сердце, буквы GAME OVER падают по одной;
        // затем фразы печатной машинкой и счёт в рамке как у поля боя.
        static readonly Color Blood = new Color(0.27f, 0.02f, 0.05f);
        static readonly Color Ink = new Color(0.07f, 0.02f, 0.12f);
        static readonly Vector2[] Outline = { new Vector2(-1, 0), new Vector2(1, 0), new Vector2(0, -1), new Vector2(0, 1),
                                              new Vector2(-1, -1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(1, 1) };
        static readonly Color DarkRed = new Color(0.77f, 0.11f, 0.14f);
        static readonly Color Ember = new Color(1f, 0.5f, 0.2f);
        static readonly Color Gold = new Color(1f, 0.82f, 0.25f);
        static readonly Color Gray = new Color(0.55f, 0.55f, 0.59f);
        Texture2D gridTex;

        Texture2D GridTexture()
        {
            if (gridTex != null) return gridTex;
            const int n = 40;
            gridTex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    Color32 c = new Color32(6, 3, 8, 255);
                    if (x % 10 == 0 && y % 10 == 0) c = new Color32(30, 12, 22, 255);
                    if ((x == 0 && (y <= 1 || y >= n - 1)) || (y == 0 && (x <= 1 || x >= n - 1))) c = new Color32(52, 16, 30, 255);
                    px[y * n + x] = c;
                }
            gridTex.SetPixels32(px);
            gridTex.Apply(false);
            return gridTex;
        }

        static float Hash(int i, int k) => Mathf.Repeat(Mathf.Sin(i * 127.1f + k * 311.7f) * 43758.5453f, 1f);

        void DrawGameOver(float s)
        {
            float t = gameOverTime;
            float W = gm.Pixel.RtWidth, H = gm.Pixel.RtHeight, cx = W / 2;

            // фон: сетка, свечение снизу и сверху ступенями
            PixelUI.Box(0, 0, W, H, Color.black);
            float off = Mathf.Floor(t * 3f) / 40f;
            GUI.DrawTextureWithTexCoords(PixelUI.R(0, 0, W, H), GridTexture(), new Rect(off, off, W / 40f, H / 40f));
            for (int i = 0; i < 8; i++)
                PixelUI.Box(0, H - (i + 1) * 6, W, 6, new Color(Blood.r, Blood.g, Blood.b, 0.55f * (1 - i / 8f)));
            for (int i = 0; i < 4; i++)
                PixelUI.Box(0, i * 5, W, 5, new Color(0, 0, 0, 0.5f * (1 - i / 4f)));

            // искры поднимаются снизу и мерцают
            for (int i = 0; i < 28; i++)
            {
                float speed = 6 + Hash(i, 1) * 12;
                float y = H + 4 - Mathf.Repeat(t * speed + Hash(i, 2) * (H + 20), H + 20);
                float x = Mathf.Floor(Hash(i, 3) * W + Mathf.Sin(t * 1.3f + i) * 3);
                if (((int)(t * 8) + i) % 7 == 0) continue;
                Color c = i % 3 == 0 ? Ember : i % 3 == 1 ? RedBar.fill : DarkRed;
                float size = i % 5 == 0 ? 2 : 1;
                PixelUI.Box(x, Mathf.Floor(y), size, size, c);
            }

            // разбитое сердце, слегка покачивается
            float bob = Mathf.Repeat(t, 1.6f) < 0.8f ? 0 : 1;
            PixelUI.Sprite(ArtIds.SoulBroken, cx - 16, 6 + bob, false, false, Color.white, 2);

            // GAME OVER: буквы падают сверху ступенями, по очереди
            const string title = "GAME OVER";
            const float ts = 4, ty = 42;
            float x0 = Mathf.Round(cx - PixelUI.Width(title, ts) / 2);
            for (int i = 0; i < title.Length; i++)
            {
                string ch = title[i].ToString();
                float cw = PixelUI.Width(ch, ts);
                float p = Mathf.Clamp01((t - 0.2f - i * 0.07f) / 0.25f);
                if (p > 0 && ch != " ")
                {
                    float k = Mathf.Ceil(p * 4) / 4;
                    Vector2 g = gm.Pixel.RtToGui(x0, ty - (1 - k) * 24);
                    float ps = gm.Pixel.PixelScale;
                    PixelFont.Draw(ch, g.x + 2 * ps, g.y + 2 * ps, ts * ps, Blood);
                    PixelFont.Draw(ch, g.x, g.y, ts * gm.Pixel.PixelScale, Color.white);
                }
                x0 += cw;
            }

            // разделитель с ромбом
            if (t > 1.1f)
            {
                float yd = 86;
                PixelUI.Box(60, yd, W - 120, 1, Blood);
                PixelUI.Box(cx - 40, yd, 80, 1, DarkRed);
                PixelUI.Diamond(cx, yd, RedBar.fill, Color.white);
            }

            // фразы — печатной машинкой, строки выровнены по общему левому краю
            const string m1 = "* НЕ СДАВАЙСЯ...", m2 = "* СОХРАНЯЙ РЕШИМОСТЬ!";
            float mx = Mathf.Round(cx - Mathf.Max(PixelUI.Width(m1), PixelUI.Width(m2)) / 2);
            PixelUI.Text(Typed(m1, t - 1.3f), mx, 94, 1, Color.white, -1);
            PixelUI.Text(Typed(m2, t - 2f), mx, 106, 1, Gray, -1);

            // счёт в рамке: раскрывается ступенями
            if (t > 3f)
            {
                int step = Mathf.Min(4, Mathf.FloorToInt((t - 3f) / 0.04f));
                float pw = Mathf.Round(170 * (step + 1) / 5f / 2) * 2, ph = 18;
                float px = cx - pw / 2, py = 126;
                PixelUI.Box(px, py, pw, ph, Color.black);
                PixelUI.Box(px + 2, py + 2, pw - 4, ph - 4, new Color(Blood.r, Blood.g, Blood.b, 0.6f));
                PixelUI.BattleFrame(px, py, pw, ph, PixelUI.Accent);
                if (step >= 4)
                {
                    if (newRecord)
                    {
                        Color c = Mathf.Repeat(Time.unscaledTime, 0.5f) < 0.25f ? Gold : Color.white;
                        PixelUI.Text($"НОВЫЙ РЕКОРД: {gm.Score.Score}", cx, py + 6, 1, c);
                    }
                    else
                    {
                        PixelUI.Text($"ОЧКИ {gm.Score.Score}", cx - 7, py + 6, 1, Color.white, 1);
                        PixelUI.Box(cx, py + 5, 1, 9, PixelUI.Accent);
                        PixelUI.Text($"РЕКОРД {gm.Score.Best}", cx + 7, py + 6, 1, Gray, -1);
                    }
                }
            }

            // приглашение продолжить, мигает
            if (t > 3.3f && Mathf.Repeat(Time.unscaledTime, 1f) < 0.65f)
            {
                string prompt = Application.isMobilePlatform ? "КОСНИСЬ, ЧТОБЫ ПРОДОЛЖИТЬ" : "НАЖМИ, ЧТОБЫ ПРОДОЛЖИТЬ";
                float pw = PixelUI.Width(prompt);
                PixelUI.Text(prompt, cx, 158, 1, Color.white);
                PixelUI.Diamond(cx - pw / 2 - 8, 161, DarkRed, RedBar.fill);
                PixelUI.Diamond(cx + pw / 2 + 8, 161, DarkRed, RedBar.fill);
            }
        }

        static void Bar(Rect r, Color c)
        {
            var prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = prev;
        }

        // Эффект печатной машинки
    }
}
