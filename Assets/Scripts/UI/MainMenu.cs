using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BlackHeart
{
    // Главное меню в духе S.T.A.L.K.E.R. 2: логотип слева вверху, под ним колонка пунктов,
    // выбранный пункт в рамке, слева затемнение; фон — сцена боя (кровавая луна), отражённая по горизонтали.
    // Пункты: обучение, начать игру, рекорд, настройки громкости, выход.
    // Управление: мышь / палец (Pointer.current) и клавиатура (стрелки/WASD, Enter, Esc).
    public partial class MainMenu : MonoBehaviour
    {
        enum Page { Main, Record, Settings }

        static readonly Color White = Hex(0xE8E8F0);
        static readonly Color Gray = Hex(0x8C8C96);
        static readonly Color Red = Hex(0xE82424);
        static readonly Color DarkRed = Hex(0xC41C24);
        static readonly Color Blood = Hex(0x46040C);
        static readonly Color Empty = Hex(0x2E2830);

        static readonly string[] MainItems = { "ОБУЧЕНИЕ", "НАЧАТЬ ИГРУ", "РЕКОРД", "НАСТРОЙКИ", "ВЫХОД" };

        public bool IsOpen { get; private set; }

        Page page;
        int selected;
        readonly List<Rect> items = new List<Rect>();          // кликабельные строки текущей страницы (GUI)
        readonly VolumeControls volume = new VolumeControls();
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
            sfx = GameAudio.AddSource(gameObject, false);
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
            if (!IsOpen || Time.frameCount < inputFrom || gm.Fader.Busy) return;

            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) Move(-1);
                if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) Move(1);
                if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
                    Activate(selected);
                if (kb.escapeKey.wasPressedThisFrame) Back();
                if (page == Page.Settings && volume.HandleKeys(kb, selected)) Blip();
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

                if (page == Page.Settings && i < VolumeControls.Count)
                {
                    if (volume.HandleClick(i, gui)) Blip();
                }
                else Activate(i);
                break;
            }

            // Ползунок громкости можно тянуть — только если нажатие началось на нём самом.
            // Иначе клик по «Настройкам» (он стоит там же, где ползунок музыки) сразу менял громкость.
            if (!IsOpen || page != Page.Settings) volume.StopDrag();
            else volume.HandleDrag(pointer.press.isPressed, pressed, gui);
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
                    // в игру — через затемнение: экран гаснет, бой появляется уже из темноты
                    if (index == 0) { Confirm(); gm.Fader.Transition(() => gm.StartGame(true)); }        // обучение
                    else if (index == 1) { Confirm(); gm.Fader.Transition(() => gm.StartGame(false)); }  // сразу в бой
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
            volume.Reset();   // старые зоны прошлого показа страницы
            if (sound) Confirm();
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
    }
}
