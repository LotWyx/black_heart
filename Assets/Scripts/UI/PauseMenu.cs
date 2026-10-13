using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BlackHeart
{
    // Пауза в бою: Esc или кнопка «II» вверху экрана (для сенсорных экранов).
    // Пункты: продолжить, настройки (громкость звука и музыки — те же шкалы, что в главном меню), выйти в меню.
    // Панель с рамкой как у поля боя (уголки, метки, акцентная линия), заголовок с кровавой тенью,
    // счёт и рекорд, подсказки клавиш; выбранный пункт — в рамке с завитками.
    public partial class PauseMenu : MonoBehaviour
    {
        static readonly Color White = new Color(0.91f, 0.91f, 0.94f);
        static readonly Color Gray = new Color(0.55f, 0.55f, 0.59f);
        static readonly string[] Items = { "ПРОДОЛЖИТЬ", "НАСТРОЙКИ", "ГЛАВНОЕ МЕНЮ" };
        const int SettingsItem = 1, BackItem = VolumeControls.Count;   // «назад» — после шкал громкости

        public const float IconSize = 6;    // ширина иконки паузы (пиксели низкого разрешения)
        static readonly Color Accent = PixelUI.Accent;
        static readonly Color Red = new Color(0.91f, 0.14f, 0.14f);
        static readonly Color DarkRed = new Color(0.77f, 0.11f, 0.14f);
        static readonly Color Blood = new Color(0.27f, 0.02f, 0.05f);
        const float OpenStep = 0.035f;   // панель раскрывается ступенями (в реальном времени: на паузе timeScale = 0)

        int selected;
        bool settings;                   // открыта страница настроек
        readonly VolumeControls volume = new VolumeControls();
        readonly List<Rect> rects = new List<Rect>();
        Rect pauseButton;
        Vector2 lastPointer;
        int inputFrom;
        float s, openedAt;
        AudioSource sfx;

        GameManager gm => GameManager.I;

        void Awake()
        {
            sfx = GameAudio.AddSource(gameObject, false);
        }

        bool CanPause => gm.CurrentState == GameManager.State.Playing;
        int ItemCount => settings ? VolumeControls.Count + 1 : Items.Length;

        void Open()
        {
            gm.SetPaused(true);
            GoTo(false, 0);
            openedAt = Time.unscaledTime;
            inputFrom = Time.frameCount + 1;
            sfx.PlayOneShot(SfxSynth.MenuSelect);
        }

        void Close()
        {
            gm.SetPaused(false);
            sfx.PlayOneShot(SfxSynth.MenuSelect);
        }

        // Переключить страницу паузы: основная или настройки
        void GoTo(bool toSettings, int select)
        {
            settings = toSettings;
            selected = select;
            rects.Clear();
            volume.Reset();
        }

        void Activate(int index)
        {
            if (settings)
            {
                if (index != BackItem) return;   // шкалы меняются стрелками и мышью, Enter на них ничего не делает
                sfx.PlayOneShot(SfxSynth.MenuSelect);
                GoTo(false, SettingsItem);
                return;
            }

            if (index == 0) Close();
            else if (index == SettingsItem)
            {
                sfx.PlayOneShot(SfxSynth.MenuSelect);
                GoTo(true, 0);
            }
            else
            {
                sfx.PlayOneShot(SfxSynth.MenuSelect);
                gm.QuitToMenu();
            }
        }

        void Update()
        {
            if (gm == null || !CanPause || gm.Fader.Busy) return;
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
                if (kb.escapeKey.wasPressedThisFrame)
                {
                    if (settings) Activate(BackItem);
                    else Close();
                    return;
                }
                if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) Move(-1);
                if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) Move(1);
                if (settings && volume.HandleKeys(kb, selected)) Blip();
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
                if ((moved || pressed) && selected != i) { selected = i; Blip(); }
                if (!pressed) continue;
                if (settings && i < VolumeControls.Count)
                {
                    if (volume.HandleClick(i, gui)) Blip();
                    break;
                }
                Activate(i);
                return;
            }

            if (settings) volume.HandleDrag(pointer.press.isPressed, pressed, gui);
            else volume.StopDrag();
        }

        void Move(int d)
        {
            selected = (selected + d + ItemCount) % ItemCount;
            Blip();
        }

        void Blip() => sfx.PlayOneShot(SfxSynth.MenuMove);
    }
}
