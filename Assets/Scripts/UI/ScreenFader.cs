using System;
using UnityEngine;

namespace BlackHeart
{
    // Переход между экранами через затемнение: экран гаснет, в темноте выполняется смена экрана,
    // затем картинка проявляется. Яркость меняется ступенями, как и всё остальное в игре.
    // Работает в реальном времени, поэтому не зависит от паузы.
    public class ScreenFader : MonoBehaviour
    {
        public const float FadeTime = 0.3f;   // секунды в одну сторону
        const int Steps = 6;

        float darkness;                // 0 — картинка видна, 1 — чёрный экран
        int direction;                 // +1 гаснет, -1 проявляется, 0 — покой
        Action atDark;

        // Идёт переход: меню в это время не принимают ввод
        public bool Busy => direction != 0;
        // Экран сейчас гаснет: музыка затихает вместе с ним
        public bool Darkening => direction > 0;


        // Затемнить, выполнить action в темноте, проявить. Если переход уже идёт — false, action не выполняется
        public bool Transition(Action action)
        {
            if (Busy) return false;
            atDark = action;
            direction = 1;
            return true;
        }

        void Update()
        {
            if (direction == 0) return;
            darkness += direction * Time.unscaledDeltaTime / FadeTime;
            if (direction > 0 && darkness >= 1f)
            {
                darkness = 1f;
                direction = -1;
                var action = atDark;
                atDark = null;
                action?.Invoke();
            }
            else if (direction < 0 && darkness <= 0f)
            {
                darkness = 0f;
                direction = 0;
            }
        }

        void OnGUI()
        {
            if (darkness <= 0f || Event.current.type != EventType.Repaint) return;
            GUI.depth = -100;   // поверх всего, включая вступление и меню
            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, Mathf.Ceil(darkness * Steps) / Steps);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = prev;
        }
    }
}
