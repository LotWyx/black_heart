using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BlackHeart
{
    // Пауза в бою: Esc или кнопка «II» вверху экрана (для сенсорных экранов).
    // Пункты: продолжить, выйти в главное меню. Оформление как у главного меню — рамка у выбранного.
    public class PauseMenu : MonoBehaviour
    {
        static readonly Color White = new Color(0.91f, 0.91f, 0.94f);
        static readonly Color Gray = new Color(0.55f, 0.55f, 0.59f);
        static readonly string[] Items = { "ПРОДОЛЖИТЬ", "ГЛАВНОЕ МЕНЮ" };

        int selected;
        readonly List<Rect> rects = new List<Rect>();
        Rect pauseButton;
        Vector2 lastPointer;
        int inputFrom;
        float s;
        AudioSource sfx;

        GameManager gm => GameManager.I;

        void Awake() => sfx = gameObject.AddComponent<AudioSource>();

        bool CanPause => gm.CurrentState == GameManager.State.Playing;

        void Open()
        {
            gm.SetPaused(true);
            selected = 0;
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

        void Text(string text, float x, float y, float scale, Color color)
        {
            Vector2 g = gm.Pixel.RtToGui(x, y);
            PixelFont.Draw(text, g.x + s * scale, g.y + s * scale, s * scale, Color.black, 0);
            PixelFont.Draw(text, g.x, g.y, s * scale, color, 0);
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
                // Кнопка паузы «II» вверху по центру
                if (repaint) pauseButton = R(cx - 7, 4, 14, 13);
                Frame(cx - 7, 4, 14, 13, Gray);
                Box(R(cx - 3, 7, 2, 7), Gray);
                Box(R(cx + 1, 7, 2, 7), Gray);
                return;
            }

            Box(R(0, 0, gm.Pixel.RtWidth, h), new Color(0, 0, 0, 0.7f));
            Vector2 g = gm.Pixel.RtToGui(cx, h / 2 - 34);
            PixelFont.Draw("ПАУЗА", g.x + 2 * s, g.y + 2 * s, 3 * s, new Color(0.27f, 0.02f, 0.05f), 0);
            PixelFont.Draw("ПАУЗА", g.x, g.y, 3 * s, White, 0);

            float fw = 0;
            foreach (var it in Items) fw = Mathf.Max(fw, PixelFont.Width(it, s) / s);
            fw = Mathf.Max(96, fw + 24);
            if (repaint) rects.Clear();
            for (int i = 0; i < Items.Length; i++)
            {
                float y = h / 2 + 2 + i * 18, x = cx - fw / 2;
                bool sel = i == selected;
                if (sel)
                {
                    Box(R(x, y - 4, fw, 15), new Color(0, 0, 0, 0.55f));
                    Frame(x, y - 4, fw, 15, White);
                }
                Text(Items[i], cx, y, 1, sel ? White : Gray);
                if (repaint) rects.Add(R(x, y - 4, fw, 15));
            }
            Text(Application.isMobilePlatform ? "" : "ESC - ПРОДОЛЖИТЬ", cx, h - 14, 1, Gray);
        }
    }
}
