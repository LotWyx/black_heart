using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BlackHeart
{
    // Главное меню (16-бит, закатная гамма, красная луна на фоне).
    // Пункты: начать игру, рекорд, настройки громкости, выход.
    // Управление: мышь / палец (Pointer.current) и клавиатура (стрелки/WASD, Enter, Esc).
    public class MainMenu : MonoBehaviour
    {
        enum Page { Main, Record, Settings }

        static readonly Color Yellow = Hex(0xFFD27A);
        static readonly Color Pink = Hex(0xFF8FB8);
        static readonly Color Magenta = Hex(0xC93468);
        static readonly Color Deep = Hex(0x12061F);
        static readonly Color Dim = Hex(0x5A1C72);

        static readonly string[] MainItems = { "НАЧАТЬ ИГРУ", "РЕКОРД", "НАСТРОЙКИ", "ВЫХОД" };
        static readonly string[] VolumeLabels = { "ОБЩАЯ ГРОМКОСТЬ", "МУЗЫКА" };
        static readonly int[] PulseSequence = { 0, 1, 0, 2 };
        const float VolumeStep = 0.1f;

        public bool IsOpen { get; private set; }

        Page page;
        int selected;
        readonly List<Rect> items = new List<Rect>();          // кликабельные строки текущей страницы (GUI)
        readonly Rect[] minus = new Rect[2], plus = new Rect[2], bars = new Rect[2];
        Vector2 lastPointer;
        int inputFrom;   // кадр, с которого меню принимает ввод (нажатие, открывшее меню, не считается)
        float s;   // экранных пикселей в одном пикселе низкого разрешения

        SpriteRenderer bg;
        SpriteAnimator bgAnim;
        AudioSource sfx;

        GameManager gm => GameManager.I;

        static Color Hex(int h) => new Color(((h >> 16) & 255) / 255f, ((h >> 8) & 255) / 255f, (h & 255) / 255f);

        void Awake()
        {
            var go = new GameObject("MenuBackground");
            bg = go.AddComponent<SpriteRenderer>();
            bg.sortingOrder = -15;
            bg.enabled = false;
            bgAnim = go.AddComponent<SpriteAnimator>();
            sfx = gameObject.AddComponent<AudioSource>();
            SfxSynth.Init();
        }

        public void Show()
        {
            IsOpen = true;
            inputFrom = Time.frameCount + 1;
            Go(Page.Main, 0, false);
            bg.enabled = true;
            bgAnim.Play(ArtLibrary.Frames(ArtIds.BackgroundMenu, ArtIds.BackgroundBattle, 320), 0.25f);
        }

        public void Hide()
        {
            IsOpen = false;
            bg.enabled = false;
            bgAnim.Stop();
        }

        int ItemCount => page == Page.Main ? MainItems.Length : page == Page.Settings ? 4 : 1;

        // ---------- ввод ----------

        void Update()
        {
            if (!IsOpen || Time.frameCount < inputFrom) return;

            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) Move(-1);
                if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) Move(1);
                if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
                    Activate(selected);
                if (kb.escapeKey.wasPressedThisFrame) Back();
                if (page == Page.Settings && selected < 2)
                {
                    if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) Adjust(selected, -VolumeStep);
                    if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) Adjust(selected, VolumeStep);
                }
            }

            var pointer = Pointer.current;
            if (pointer == null || !IsOpen) return;
            Vector2 sp = pointer.position.ReadValue();
            Vector2 gui = new Vector2(sp.x, Screen.height - sp.y);
            bool moved = (gui - lastPointer).sqrMagnitude > 1f;
            bool pressed = pointer.press.wasPressedThisFrame;
            lastPointer = gui;

            for (int i = 0; i < items.Count && IsOpen; i++)
            {
                if (!items[i].Contains(gui)) continue;
                if ((moved || pressed) && selected != i) { selected = i; Blip(); }
                if (!pressed) continue;

                if (page == Page.Settings && i < 2)
                {
                    if (minus[i].Contains(gui)) Adjust(i, -VolumeStep);
                    else if (plus[i].Contains(gui)) Adjust(i, VolumeStep);
                }
                else Activate(i);
                break;
            }

            // Ползунок громкости можно тянуть
            if (IsOpen && page == Page.Settings && pointer.press.isPressed)
            {
                for (int i = 0; i < 2; i++)
                {
                    if (!bars[i].Contains(gui)) continue;
                    float v = Mathf.Round(Mathf.Clamp01((gui.x - bars[i].x) / bars[i].width) * 20f) / 20f;
                    if (!Mathf.Approximately(v, Volume(i))) SetVolume(i, v);
                }
            }
        }

        void Move(int delta)
        {
            selected = (selected + delta + ItemCount) % ItemCount;
            Blip();
        }

        void Activate(int index)
        {
            switch (page)
            {
                case Page.Main:
                    if (index == 0) { Confirm(); gm.StartGame(); }
                    else if (index == 1) Go(Page.Record, 0);
                    else if (index == 2) Go(Page.Settings, 0);
                    else Quit();
                    break;
                case Page.Record:
                    Go(Page.Main, 1);
                    break;
                case Page.Settings:
                    if (index == 2) { Confirm(); gm.StartGame(true); }   // пройти обучение заново
                    else if (index == 3) Go(Page.Main, 2);
                    break;
            }
        }

        void Back()
        {
            if (page == Page.Record) Go(Page.Main, 1);
            else if (page == Page.Settings) Go(Page.Main, 2);
        }

        void Go(Page target, int select, bool sound = true)
        {
            page = target;
            selected = select;
            items.Clear();
            if (sound) Confirm();
        }

        float Volume(int i) => i == 0 ? GameSettings.MasterVolume : GameSettings.MusicVolume;

        void SetVolume(int i, float v)
        {
            if (i == 0) GameSettings.MasterVolume = v;
            else GameSettings.MusicVolume = v;
        }

        void Adjust(int i, float delta)
        {
            SetVolume(i, Mathf.Round((Volume(i) + delta) * 20f) / 20f);
            Blip();
        }

        void Blip() => sfx.PlayOneShot(SfxSynth.MenuMove);
        void Confirm() => sfx.PlayOneShot(SfxSynth.MenuSelect);

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ---------- отрисовка ----------

        void LateUpdate()
        {
            if (!bg.enabled || bg.sprite == null) return;
            Rect view = gm.Pixel.ViewRect;
            Vector2 size = bg.sprite.bounds.size;
            float scale = Mathf.Max(view.width / size.x, view.height / size.y);
            if (scale > 1f && scale - Mathf.Floor(scale) < 0.05f) scale = Mathf.Floor(scale);
            bg.transform.position = new Vector3(view.center.x, view.center.y, 4f);
            bg.transform.localScale = new Vector3(scale, scale, 1f);
        }

        void OnGUI()
        {
            if (!IsOpen || gm == null) return;
            GUI.depth = -5;
            s = gm.Pixel.PixelScale;
            bool repaint = Event.current.type == EventType.Repaint;
            if (repaint) items.Clear();

            if (page == Page.Main) DrawMain(repaint);
            else if (page == Page.Record) DrawRecord(repaint);
            else DrawSettings(repaint);

            Text("ENTER - ВЫБОР   ESC - НАЗАД", 18, gm.Pixel.RtHeight - 12, 1, Magenta, -1, false);
        }

        Vector2 P(float x, float y) => gm.Pixel.RtToGui(x, y);

        Rect R(float x, float y, float w, float h)
        {
            Vector2 g = P(x, y);
            return new Rect(g.x, g.y, w * s, h * s);
        }

        void Text(string text, float x, float y, float scale, Color color, int align = -1, bool shadow = true)
        {
            Vector2 g = P(x, y);
            if (shadow) PixelFont.Draw(text, g.x + s * scale, g.y + s * scale, s * scale, Deep, align);
            PixelFont.Draw(text, g.x, g.y, s * scale, color, align);
        }

        // Заголовок: глубокая тень + пурпурная тень + жёлтый текст
        void Title(string text, float x, float y)
        {
            Vector2 g = P(x, y);
            PixelFont.Draw(text, g.x + 2 * s, g.y + 3 * s, 2 * s, Deep);
            PixelFont.Draw(text, g.x + s, g.y + s, 2 * s, Magenta);
            PixelFont.Draw(text, g.x, g.y, 2 * s, Yellow);
        }

        void Box(Rect r, Color c)
        {
            var prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = prev;
        }

        void Heart(float x, float y)
        {
            var frames = ArtLibrary.Frames(ArtIds.SoulPulse, ArtIds.Soul);
            var sprite = frames[PulseSequence[(int)(Time.unscaledTime / 0.4f) % 4] % frames.Length];
            var tex = sprite.texture;
            Rect r = sprite.rect;
            var uv = new Rect(r.x / tex.width, r.y / tex.height, r.width / tex.width, r.height / tex.height);
            float bob = ((int)(Time.unscaledTime * 3f) % 2);   // покачивание на 1 пиксель
            GUI.DrawTextureWithTexCoords(R(x + bob, y, r.width, r.height), tex, uv);
        }

        // Строка меню: курсор-сердце у выбранной, жёлтый цвет у выбранной, розовый у остальных
        void Item(int index, string text, float y, bool repaint)
        {
            bool sel = index == selected;
            if (sel) Heart(12, y - 1);
            Text(text, 34, y, 2, sel ? Yellow : Pink);
            if (repaint) items.Add(R(8, y - 4, 26 + PixelFont.Width(text.ToUpperInvariant(), 2) + 10, 22));
        }

        void DrawMain(bool repaint)
        {
            Title("BLACK HEART", 18, 16);
            Text("* ЗАЩИТИ СВОЮ ДУШУ *", 18, 38, 1, Pink);
            for (int i = 0; i < MainItems.Length; i++) Item(i, MainItems[i], 66 + i * 22, repaint);
        }

        void DrawRecord(bool repaint)
        {
            Title("РЕКОРД", 18, 16);
            int best = gm.Score.Best;
            if (best > 0)
            {
                Vector2 g = P(18, 62);
                string num = best.ToString();
                PixelFont.Draw(num, g.x + 2 * s, g.y + 3 * s, 4 * s, Deep);
                PixelFont.Draw(num, g.x + s, g.y + s, 4 * s, Magenta);
                PixelFont.Draw(num, g.x, g.y, 4 * s, Yellow);
                Text("ЛУЧШИЙ СЧЁТ ЗА ВСЕ ИГРЫ", 18, 100, 1, Pink);
            }
            else
            {
                Text("ЕЩЁ НЕТ РЕКОРДА", 18, 66, 2, Pink);
                Text("СЫГРАЙ, ЧТОБЫ ЕГО ПОСТАВИТЬ", 18, 88, 1, Pink);
            }
            Item(0, "НАЗАД", 140, repaint);
        }

        void DrawSettings(bool repaint)
        {
            Title("НАСТРОЙКИ", 18, 16);
            for (int i = 0; i < 2; i++)
            {
                float y = 50 + i * 34;
                bool sel = selected == i;
                Color c = sel ? Yellow : Pink;
                if (sel) Heart(12, y - 4);
                Text(VolumeLabels[i], 34, y, 1, c);

                float cy = y + 12;
                Text("<", 34, cy, 2, c);
                const float barX = 50, segW = 9, segGap = 1, segH = 10;
                float v = Volume(i);
                int filled = Mathf.RoundToInt(v * 10f);
                for (int k = 0; k < 10; k++)
                {
                    Box(R(barX + k * (segW + segGap) + 1, cy + 1, segW, segH), Deep);
                    Box(R(barX + k * (segW + segGap), cy, segW, segH), k < filled ? (sel ? Yellow : Pink) : Dim);
                }
                Text(">", barX + 100 + 4, cy, 2, c);
                Text(Mathf.RoundToInt(v * 100f) + "%", barX + 100 + 20, cy + 2, 1, c);

                if (repaint)
                {
                    items.Add(R(8, y - 6, 190, 32));
                    minus[i] = R(30, cy - 2, 16, 18);
                    plus[i] = R(barX + 100, cy - 2, 16, 18);
                    bars[i] = R(barX, cy - 2, 100, 14);
                }
            }
            Item(2, "ОБУЧЕНИЕ", 118, repaint);
            Item(3, "НАЗАД", 142, repaint);
        }
    }
}
