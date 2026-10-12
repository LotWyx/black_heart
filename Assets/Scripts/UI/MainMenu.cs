using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BlackHeart
{
    // Главное меню в духе S.T.A.L.K.E.R. 2: логотип слева вверху, под ним колонка пунктов,
    // выбранный пункт в рамке, слева затемнение; фон — сцена боя (кровавая луна), отражённая по горизонтали.
    // Пункты: обучение, начать игру, рекорд, настройки громкости, выход.
    // Управление: мышь / палец (Pointer.current) и клавиатура (стрелки/WASD, Enter, Esc).
    public class MainMenu : MonoBehaviour
    {
        enum Page { Main, Record, Settings }

        static readonly Color White = Hex(0xE8E8F0);
        static readonly Color Gray = Hex(0x8C8C96);
        static readonly Color Red = Hex(0xE82424);
        static readonly Color DarkRed = Hex(0xC41C24);
        static readonly Color Blood = Hex(0x46040C);
        static readonly Color Empty = Hex(0x2E2830);

        static readonly string[] MainItems = { "ОБУЧЕНИЕ", "НАЧАТЬ ИГРУ", "РЕКОРД", "НАСТРОЙКИ", "ВЫХОД" };
        static readonly string[] VolumeLabels = { "ГРОМКОСТЬ ЗВУКА", "ГРОМКОСТЬ МУЗЫКИ" };
        const float VolumeStep = 0.1f;

        public bool IsOpen { get; private set; }

        Page page;
        int selected;
        readonly List<Rect> items = new List<Rect>();          // кликабельные строки текущей страницы (GUI)
        readonly Rect[] minus = new Rect[2], plus = new Rect[2], bars = new Rect[2];
        Vector2 lastPointer;
        int inputFrom;   // кадр, с которого меню принимает ввод (нажатие, открывшее меню, не считается)
        int dragBar = -1;   // ползунок, который сейчас тянут; -1 — никакой
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
            Go(Page.Main, 1, false);   // курсор сразу на «Начать игру»
            bg.enabled = true;
            bg.flipX = true;   // луна справа, меню слева на тёмных облаках
            bgAnim.Play(ArtLibrary.Frames(ArtIds.BackgroundMenu, null, 320), 0.25f);
        }

        public void Hide()
        {
            IsOpen = false;
            bg.enabled = false;
            bgAnim.Stop();
        }

        int ItemCount => page == Page.Main ? MainItems.Length : page == Page.Settings ? 3 : 1;

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

            // Ползунок громкости можно тянуть — только если нажатие началось на нём самом.
            // Иначе клик по «Настройкам» (он стоит там же, где ползунок музыки) сразу менял громкость.
            if (!IsOpen || page != Page.Settings || !pointer.press.isPressed) { dragBar = -1; return; }
            if (pressed)
                for (int i = 0; i < 2; i++) if (bars[i].Contains(gui)) dragBar = i;
            if (dragBar >= 0)
            {
                Rect b = bars[dragBar];
                float v = Mathf.Round(Mathf.Clamp01((gui.x - b.x) / b.width) * 10f) / 10f;
                if (!Mathf.Approximately(v, Volume(dragBar))) SetVolume(dragBar, v);
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
                    if (index == 0) { Confirm(); gm.StartGame(true); }        // обучение
                    else if (index == 1) { Confirm(); gm.StartGame(false); }  // сразу в бой
                    else if (index == 2) Go(Page.Record, 0);
                    else if (index == 3) Go(Page.Settings, 0);
                    else Quit();
                    break;
                case Page.Record:
                    Go(Page.Main, 2);
                    break;
                case Page.Settings:
                    if (index == 2) Go(Page.Main, 3);
                    break;
            }
        }

        void Back()
        {
            if (page == Page.Record) Go(Page.Main, 2);
            else if (page == Page.Settings) Go(Page.Main, 3);
        }

        void Go(Page target, int select, bool sound = true)
        {
            page = target;
            selected = select;
            items.Clear();
            dragBar = -1;
            for (int i = 0; i < 2; i++) bars[i] = minus[i] = plus[i] = Rect.zero;   // старые зоны прошлого показа страницы
            if (sound) Confirm();
        }

        float Volume(int i) => i == 0 ? GameSettings.SoundVolume : GameSettings.MusicVolume;

        void SetVolume(int i, float v)
        {
            if (i == 0) GameSettings.SoundVolume = v;
            else GameSettings.MusicVolume = v;
        }

        void Adjust(int i, float delta)
        {
            SetVolume(i, Mathf.Round((Volume(i) + delta) * 10f) / 10f);
            Blip();
        }

        void Blip() => sfx.PlayOneShot(SfxSynth.MenuMove, GameSettings.SoundVolume);
        void Confirm() => sfx.PlayOneShot(SfxSynth.MenuSelect, GameSettings.SoundVolume);

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
            float fw = Mathf.Max(FrameWidth(VolumeLabels), 128);
            const float segW = 6, segGap = 1, barW = 10 * (segW + segGap) - segGap;
            for (int i = 0; i < 2; i++)
            {
                float y = 74 + i * 28;
                bool sel = selected == i;
                float x = ColumnX - fw / 2;
                if (sel)
                {
                    SelectFrame(x, y - 4, fw, 26);
                }
                Text(VolumeLabels[i], ColumnX, y, 1, sel ? White : Gray);

                float by = y + 11, bx = ColumnX - 47;   // группа «< шкала > 100%» по центру колонки
                float v = Volume(i);
                int filled = Mathf.RoundToInt(v * 10f);
                Text("<", bx - 8, by, 1, sel ? White : Gray, -1, false);
                for (int k = 0; k < 10; k++)
                    Box(R(bx + k * (segW + segGap), by, segW, 7), k < filled ? (sel ? Red : DarkRed) : Empty);
                Text(">", bx + barW + 3, by, 1, sel ? White : Gray, -1, false);
                Text(filled * 10 + "%", bx + barW + 11, by, 1, sel ? White : Gray, -1, false);

                if (repaint)
                {
                    items.Add(R(x, y - 4, fw, 26));
                    minus[i] = R(bx - 10, by - 3, 9, 13);
                    plus[i] = R(bx + barW + 1, by - 3, 9, 13);
                    bars[i] = R(bx, by - 2, barW, 11);
                }
            }
            Item(2, "НАЗАД", 140, FrameWidth("НАЗАД"), repaint);
        }
    }
}
