using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BlackHeart
{
    // Пауза в бою: Esc или кнопка «II» вверху экрана (для сенсорных экранов).
    // Пункты: продолжить, выйти в главное меню. Панель с рамкой как у поля боя (уголки, метки, акцентная линия),
    // заголовок с кровавой тенью, счёт и рекорд, подсказки клавиш; выбранный пункт — в рамке с завитками.
    public class PauseMenu : MonoBehaviour
    {
        static readonly Color White = new Color(0.91f, 0.91f, 0.94f);
        static readonly Color Gray = new Color(0.55f, 0.55f, 0.59f);
        static readonly string[] Items = { "ПРОДОЛЖИТЬ", "ГЛАВНОЕ МЕНЮ" };

        public const float IconSize = 6;    // ширина иконки паузы (пиксели низкого разрешения)
        static readonly Color Accent = PixelUI.Accent;
        static readonly Color Red = new Color(0.91f, 0.14f, 0.14f);
        static readonly Color DarkRed = new Color(0.77f, 0.11f, 0.14f);
        static readonly Color Blood = new Color(0.27f, 0.02f, 0.05f);
        const float OpenStep = 0.035f;   // панель раскрывается ступенями (в реальном времени: на паузе timeScale = 0)

        int selected;
        readonly List<Rect> rects = new List<Rect>();
        Rect pauseButton;
        Vector2 lastPointer;
        int inputFrom;
        float s, openedAt;
        AudioSource sfx;

        GameManager gm => GameManager.I;

        void Awake() => sfx = gameObject.AddComponent<AudioSource>();

        bool CanPause => gm.CurrentState == GameManager.State.Playing;

        void Open()
        {
            gm.SetPaused(true);
            selected = 0;
            openedAt = Time.unscaledTime;
            inputFrom = Time.frameCount + 1;
            sfx.PlayOneShot(SfxSynth.MenuSelect);
        }

        void Close()
        {
            gm.SetPaused(false);
            sfx.PlayOneShot(SfxSynth.MenuSelect);
        }

        void Activate(int index)
        {
            if (index == 0) Close();
            else
            {
                sfx.PlayOneShot(SfxSynth.MenuSelect);
                gm.QuitToMenu();
            }
        }

        void Update()
        {
            if (gm == null || !CanPause) return;
            var kb = Keyboard.current;
            var pointer = Pointer.current;
            Vector2 gui = Vector2.zero;
            bool pressed = false;
            if (pointer != null)
            {
                Vector2 sp = pointer.position.ReadValue();
                gui = new Vector2(sp.x, Screen.height - sp.y);
                pressed = pointer.press.wasPressedThisFrame;
            }

            if (!gm.Paused)
            {
                bool esc = kb != null && (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame);
                if (esc || (pressed && pauseButton.Contains(gui))) Open();
                return;
            }

            if (Time.frameCount < inputFrom) return;
            if (kb != null)
            {
                if (kb.escapeKey.wasPressedThisFrame) { Close(); return; }
                if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) Move(-1);
                if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) Move(1);
                if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
                {
                    Activate(selected);
                    return;
                }
            }
            if (pointer == null) return;
            bool moved = (gui - lastPointer).sqrMagnitude > 1f;
            lastPointer = gui;
            for (int i = 0; i < rects.Count; i++)
            {
                if (!rects[i].Contains(gui)) continue;
                if ((moved || pressed) && selected != i) { selected = i; sfx.PlayOneShot(SfxSynth.MenuMove); }
                if (pressed) { Activate(i); return; }
            }
        }

        void Move(int d)
        {
            selected = (selected + d + Items.Length) % Items.Length;
            sfx.PlayOneShot(SfxSynth.MenuMove);
        }

        // ---------- отрисовка ----------

        Rect R(float x, float y, float w, float h)
        {
            Vector2 g = gm.Pixel.RtToGui(x, y);
            return new Rect(g.x, g.y, w * s, h * s);
        }

        void Box(Rect r, Color c)
        {
            var prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = prev;
        }

        void Frame(float x, float y, float w, float h, Color c)
        {
            Box(R(x, y, w, 1), c);
            Box(R(x, y + h - 1, w, 1), c);
            Box(R(x, y, 1, h), c);
            Box(R(x + w - 1, y, 1, h), c);
        }

        // Красные пиксельные завитки по бокам рамки выбранного пункта (справа как есть, слева зеркально)
        void Ornaments(float x, float y, float w, float h)
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

        void Text(string text, float x, float y, float scale, Color color, int align = 0)
        {
            Vector2 g = gm.Pixel.RtToGui(x, y);
            PixelFont.Draw(text, g.x + s * scale, g.y + s * scale, s * scale, Color.black, align);
            PixelFont.Draw(text, g.x, g.y, s * scale, color, align);
        }

        float Hint(float x, float y, string key, string action)
        {
            float kw = PixelFont.Width(key, s) / s;
            Frame(x, y - 3, kw + 8, 13, Gray);
            Text(key, x + 4, y, 1, White, -1);
            x += kw + 12;
            Text(action, x, y, 1, Gray, -1);
            return x + PixelFont.Width(action, s) / s;
        }

        void OnGUI()
        {
            if (gm == null || !CanPause) return;
            GUI.depth = -6;
            s = gm.Pixel.PixelScale;
            float cx = gm.Pixel.RtWidth / 2f, h = gm.Pixel.RtHeight;
            bool repaint = Event.current.type == EventType.Repaint;

            if (!gm.Paused)
            {
                // Иконка паузы в стиле HUD: две белые полоски высотой с букву, на строке «ОЧКИ»
                float px = gm.Pixel.RtWidth - 6 - IconSize, py = 6;
                if (repaint) pauseButton = R(px - 5, py - 5, IconSize + 10, 17);   // с запасом для пальца
                Box(R(px, py, 2, 7), Color.white);
                Box(R(px + 4, py, 2, 7), Color.white);
                return;
            }

            // затемнение сцены, по краям экрана — гуще (ступенями)
            float W = gm.Pixel.RtWidth;
            Box(R(0, 0, W, h), new Color(0, 0, 0, 0.62f));
            for (int i = 0; i < 6; i++)
            {
                var c = new Color(0, 0, 0, 0.1f);
                float d = i * 6;
                Box(R(0, d, W, 6), c); Box(R(0, h - d - 6, W, 6), c);
                Box(R(d, 0, 6, h), c); Box(R(W - d - 6, 0, 6, h), c);
            }

            // панель раскрывается за 4 ступени: от полосы к полному размеру
            float pw = 188, ph = 124;
            int step = Mathf.Min(4, Mathf.FloorToInt((Time.unscaledTime - openedAt) / OpenStep));
            float k = (step + 1) / 5f;
            float cw = Mathf.Max(24, Mathf.Round(pw * k / 2) * 2), ch = step < 4 ? Mathf.Max(6, Mathf.Round(ph * k / 2) * 2) : ph;
            float panelX = Mathf.Round(cx - cw / 2), top = Mathf.Round(h / 2 - ch / 2);
            Box(R(panelX, top, cw, ch), Color.black);
            for (int i = 0; i < 6; i++)   // кровавое свечение под заголовком, спадает ступенями
                Box(R(panelX + 2, top + 2 + i * 3, cw - 4, 3), new Color(Blood.r, Blood.g, Blood.b, 0.9f * (1 - i / 6f)));
            PixelUI.BattleFrame(panelX, top, cw, ch, Accent);
            if (step < 4) return;

            // заголовок с кровавой тенью и черты по бокам, как у подзаголовка главного меню
            float y0 = top + 9;
            Vector2 g = gm.Pixel.RtToGui(cx, y0);
            PixelFont.Draw("ПАУЗА", g.x + 2 * s, g.y + 2 * s, 3 * s, Blood, 0);
            PixelFont.Draw("ПАУЗА", g.x, g.y, 3 * s, White, 0);
            float tw = PixelFont.Width("ПАУЗА", 3 * s) / s;
            Box(R(cx - tw / 2 - 26, y0 + 10, 18, 1), DarkRed);
            Box(R(cx + tw / 2 + 8, y0 + 10, 18, 1), DarkRed);
            PixelUI.Diamond(cx - tw / 2 - 28, y0 + 10, DarkRed, Red);
            PixelUI.Diamond(cx + tw / 2 + 28, y0 + 10, DarkRed, Red);

            // разделитель с ромбом и счёт
            float yd = y0 + 28;
            Box(R(panelX + 14, yd, cw - 28, 1), Blood);
            Box(R(cx - 30, yd, 60, 1), DarkRed);
            PixelUI.Diamond(cx, yd, Red, White);
            Text($"ОЧКИ {gm.Score.Score}", cx - 6, yd + 7, 1, Gray, 1);
            Text($"РЕКОРД {gm.Score.Best}", cx + 6, yd + 7, 1, Gray, -1);
            Box(R(cx, yd + 7, 1, 7), Blood);

            float fw = 0;
            foreach (var it in Items) fw = Mathf.Max(fw, PixelFont.Width(it, s) / s);
            fw = Mathf.Max(104, fw + 24);
            if (repaint) rects.Clear();
            for (int i = 0; i < Items.Length; i++)
            {
                float y = yd + 28 + i * 18, x = cx - fw / 2;
                bool sel = i == selected;
                if (sel)
                {
                    Box(R(x, y - 4, fw, 15), new Color(Blood.r, Blood.g, Blood.b, 0.8f));
                    Frame(x, y - 4, fw, 15, White);
                    Ornaments(x, y - 4, fw, 15);
                }
                Text(Items[i], cx, y, 1, sel ? White : Gray);
                if (repaint) rects.Add(R(x, y - 4, fw, 15));
            }

            // подсказки клавиш внизу панели
            if (!Application.isMobilePlatform)
            {
                float hy = top + ph - 17;
                float total = PixelFont.Width("ENTER", s) / s + 12 + PixelFont.Width("ВЫБРАТЬ", s) / s + 10
                            + PixelFont.Width("ESC", s) / s + 12 + PixelFont.Width("НАЗАД", s) / s;
                float hx = Mathf.Round(cx - total / 2);
                hx = Hint(hx, hy, "ENTER", "ВЫБРАТЬ");
                Hint(hx + 10, hy, "ESC", "НАЗАД");
            }
        }
    }
}
