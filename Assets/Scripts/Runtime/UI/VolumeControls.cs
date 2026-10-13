using UnityEngine;
using UnityEngine.InputSystem;

namespace BlackHeart
{
    // Две шкалы громкости (звук и музыка) — общая часть настроек в главном меню и на паузе:
    // отрисовка строки, клавиши влево/вправо, кнопки «<» «>» и перетаскивание шкалы мышью.
    public class VolumeControls
    {
        public static readonly string[] Labels = { "ГРОМКОСТЬ ЗВУКА", "ГРОМКОСТЬ МУЗЫКИ" };
        public const int Count = 2;
        public const float RowStep = 28, RowHeight = 26;
        const float Step = 0.1f;
        const float SegW = 6, SegGap = 1, BarW = 10 * (SegW + SegGap) - SegGap;

        static readonly Color White = new Color(0.91f, 0.91f, 0.94f);    // #E8E8F0
        static readonly Color Gray = new Color(0.55f, 0.55f, 0.59f);     // #8C8C96
        static readonly Color Red = new Color(0.91f, 0.14f, 0.14f);      // #E82424
        static readonly Color DarkRed = new Color(0.77f, 0.11f, 0.14f);  // #C41C24
        static readonly Color Empty = new Color(0.18f, 0.16f, 0.19f);    // #2E2830
        static readonly string[] Percent = { "0%", "10%", "20%", "30%", "40%", "50%", "60%", "70%", "80%", "90%", "100%" };

        readonly Rect[] minus = new Rect[Count], plus = new Rect[Count], bars = new Rect[Count];
        int dragBar = -1;   // шкала, которую сейчас тянут; -1 — никакая

        public static float Get(int i) => i == 0 ? GameSettings.SoundVolume : GameSettings.MusicVolume;

        public static void Set(int i, float v)
        {
            if (i == 0) GameSettings.SoundVolume = v;
            else GameSettings.MusicVolume = v;
        }

        // Забыть зоны прошлого показа страницы (иначе клик по другому пункту на том же месте менял бы громкость)
        public void Reset()
        {
            dragBar = -1;
            for (int i = 0; i < Count; i++) bars[i] = minus[i] = plus[i] = Rect.zero;
        }

        // Строка i: подпись и под ней «< шкала > 100%», по центру колонки cx (координаты низкого разрешения).
        // Возвращает зону строки для выбора мышью.
        public Rect DrawRow(int i, float cx, float y, float frameWidth, bool selected, bool repaint)
        {
            float x = cx - frameWidth / 2;
            if (selected) PixelUI.SelectFrame(x, y - 4, frameWidth, RowHeight);
            Color color = selected ? White : Gray;
            PixelUI.Text(Labels[i], cx, y, 1, color);

            float by = y + 11, bx = cx - 47;
            int filled = Mathf.RoundToInt(Get(i) * 10f);
            PixelUI.TextPlain("<", bx - 8, by, color);
            for (int k = 0; k < 10; k++)
                PixelUI.Box(bx + k * (SegW + SegGap), by, SegW, 7, k < filled ? (selected ? Red : DarkRed) : Empty);
            PixelUI.TextPlain(">", bx + BarW + 3, by, color);
            PixelUI.TextPlain(Percent[Mathf.Clamp(filled, 0, 10)], bx + BarW + 11, by, color);

            Rect row = PixelUI.R(x, y - 4, frameWidth, RowHeight);
            if (repaint)
            {
                minus[i] = PixelUI.R(bx - 10, by - 3, 9, 13);
                plus[i] = PixelUI.R(bx + BarW + 1, by - 3, 9, 13);
                bars[i] = PixelUI.R(bx, by - 2, BarW, 11);
            }
            return row;
        }

        public static void Adjust(int i, float delta) => Set(i, Mathf.Round((Get(i) + delta) * 10f) / 10f);

        // Клавиши влево/вправо для выбранной строки. true — громкость изменилась
        public bool HandleKeys(Keyboard kb, int selected)
        {
            if (kb == null || selected < 0 || selected >= Count) return false;
            if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) { Adjust(selected, -Step); return true; }
            if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) { Adjust(selected, Step); return true; }
            return false;
        }

        // Нажатие мышью в строке i: кнопки «<» и «>». true — громкость изменилась
        public bool HandleClick(int i, Vector2 gui)
        {
            if (i < 0 || i >= Count) return false;
            if (minus[i].Contains(gui)) { Adjust(i, -Step); return true; }
            if (plus[i].Contains(gui)) { Adjust(i, Step); return true; }
            return false;
        }

        // Перетаскивание шкалы — только если нажатие началось на ней самой
        public void HandleDrag(bool held, bool pressedThisFrame, Vector2 gui)
        {
            if (!held) { dragBar = -1; return; }
            if (pressedThisFrame)
                for (int i = 0; i < Count; i++) if (bars[i].Contains(gui)) dragBar = i;
            if (dragBar < 0) return;
            Rect b = bars[dragBar];
            float v = Mathf.Round(Mathf.Clamp01((gui.x - b.x) / b.width) * 10f) / 10f;
            if (!Mathf.Approximately(v, Get(dragBar))) Set(dragBar, v);
        }

        public void StopDrag() => dragBar = -1;
    }
}
