using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BlackHeart
{
    // Отрисовка обучения (IMGUI): реплика Луноликого и строка-инструкция. Сценарий обучения — в TutorialController.cs.
    public partial class TutorialController
    {
        // ---------- интерфейс ----------

        void OnGUI()
        {
            if (!IsRunning || gm == null) return;
            GUI.depth = -1;
            float s = gm.Pixel.PixelScale;
            if (!string.IsNullOrEmpty(speech) && moonface != null) DrawSpeech(s);
            if (instruction != null) DrawInstruction(s);
        }

        // Реплика справа от Луноликого в стиле меню: тёмная панель с белой рамкой и красной линией внутри,
        // табличка с именем, хвостик к говорящему, текст печатается; когда допечатан — мигает «дальше»
        void DrawSpeech(float s)
        {
            Vector2 c = gm.Pixel.WorldToGui(moonface.Position);
            float left = c.x + 32 * s, top = c.y - 24 * s;   // ниже полоски HP врага, с местом под табличку
            // Перенос по реальной ширине текста (шрифт пропорциональный)
            float maxWidth = Mathf.Clamp(gm.Pixel.RtToGui(gm.Pixel.RtWidth, 0).x - left - 24 * s, 60 * s, 150 * s);
            var lines = Wrap(speech, maxWidth, s);
            float longest = 0;
            foreach (var line in lines) longest = Mathf.Max(longest, PixelFont.Width(line, s));
            float w = longest + 14 * s, h = (lines.Count * 10 - 3 + 14) * s;

            // панель: контур, белая рамка 2 px, тёмная заливка, красная линия внутри
            Box(new Rect(left - s, top - s, w + 2 * s, h + 2 * s), Ink);
            Box(new Rect(left, top, w, h), Color.white);
            Box(new Rect(left + 2 * s, top + 2 * s, w - 4 * s, h - 4 * s), Panel);
            Frame(new Rect(left + 3 * s, top + 3 * s, w - 6 * s, h - 6 * s), Accent, s);

            // табличка с именем на верхней кромке
            string name = moonface.Definition.displayName;
            float nw = PixelFont.Width(name, s);
            Box(new Rect(left + 5 * s, top - 5 * s, nw + 8 * s, 10 * s), Ink);
            Box(new Rect(left + 6 * s, top - 4 * s, nw + 6 * s, 8 * s), Accent);
            PixelFont.Draw(name, left + 9 * s, top - 3 * s, s, Color.white);

            int visible = Mathf.FloorToInt(speechTime * charsPerSecond);
            bool done = visible >= speech.Length;
            float y = top + 8 * s;
            foreach (var line in lines)
            {
                if (visible <= 0) break;
                Shadowed(line.Length <= visible ? line : line.Substring(0, visible), left + 7 * s, y, s, Color.white, -1);
                visible -= line.Length + 1;
                y += 10 * s;
            }

            // «дальше»: треугольник в правом нижнем углу, мигает
            if (done && Mathf.Repeat(Time.time, 0.8f) < 0.5f)
            {
                float ax = left + w - 11 * s, ay = top + h - 9 * s;
                Box(new Rect(ax, ay, 5 * s, s), Color.white);
                Box(new Rect(ax + s, ay + s, 3 * s, s), Color.white);
                Box(new Rect(ax + 2 * s, ay + 2 * s, s, s), Color.white);
            }
        }

        // В защите — внутри рамки сверху, в атаке — строкой под HP.
        // Плашка: чёрная, с красными линиями сверху и снизу и ромбиками по бокам.
        void DrawInstruction(float s)
        {
            Rect box = gm.Box.HudRect;
            bool inside = gm.Battle.Phase == BattlePhase.Defense;
            float cx = gm.Pixel.WorldToGui(box.center).x;
            float y = inside
                ? gm.Pixel.WorldToGui(new Vector2(box.center.x, box.yMax)).y + 10 * s
                : gm.Pixel.WorldToGui(new Vector2(box.center.x, box.yMin)).y + 18 * s;
            string main = instruction();

            float w = PixelFont.Width(main.ToUpperInvariant(), s);
            if (instructionSub != null) w = Mathf.Max(w, PixelFont.Width(instructionSub.ToUpperInvariant(), s));
            float h = (instructionSub != null ? 17 : 7) * s;
            var plate = new Rect(cx - w / 2 - 8 * s, y - 4 * s, w + 16 * s, h + 8 * s);
            Box(plate, Color.black);
            Box(new Rect(plate.x, plate.y, plate.width, s), Accent);
            Box(new Rect(plate.x, plate.yMax - s, plate.width, s), Accent);
            Diamond(plate.x - 3 * s, plate.center.y, s);
            Diamond(plate.xMax + 2 * s, plate.center.y, s);

            Shadowed(main, cx, y, s, Color.white);
            if (instructionSub != null && Mathf.Repeat(Time.time, 1f) < 0.65f)
                Shadowed(instructionSub, cx, y + 10 * s, s, Accent);
        }

        // Ромб 5x5 (в пикселях игры) с центром в (x, y)
        static void Diamond(float x, float y, float s)
        {
            Box(new Rect(x - 2 * s, y - 0.5f * s, 5 * s, s), Accent);
            Box(new Rect(x, y - 2.5f * s, s, 5 * s), Accent);
            Box(new Rect(x - s, y - 1.5f * s, 3 * s, 3 * s), Accent);
            Box(new Rect(x, y - 0.5f * s, s, s), Color.white);
        }

        static void Frame(Rect r, Color c, float s)
        {
            Box(new Rect(r.x, r.y, r.width, s), c);
            Box(new Rect(r.x, r.yMax - s, r.width, s), c);
            Box(new Rect(r.x, r.y, s, r.height), c);
            Box(new Rect(r.xMax - s, r.y, s, r.height), c);
        }

        static void Shadowed(string text, float x, float y, float s, Color color, int align = 0)
        {
            PixelFont.Draw(text, x + s, y + s, s, Ink, align);
            PixelFont.Draw(text, x, y, s, color, align);
        }

        static void Box(Rect r, Color c)
        {
            var prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = prev;
        }

        static List<string> Wrap(string text, float maxWidth, float scale)
        {
            var lines = new List<string>();
            string line = "";
            foreach (var word in text.Split(' '))
            {
                if (line.Length == 0) line = word;
                else if (PixelFont.Width(line + " " + word, scale) <= maxWidth) line += " " + word;
                else { lines.Add(line); line = word; }
            }
            if (line.Length > 0) lines.Add(line);
            return lines;
        }
    }
}
