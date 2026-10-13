using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BlackHeart
{
    // Вступление в духе Undertale: три картинки (Resources/Art/Intro/Scene1..3, по 4 кадра лёгкой анимации),
    // окно медленно едет по каждой; ниже текст печатной машинкой. Картинки появляются и гаснут ступенями,
    // после третьей экран гаснет и проявляется главное меню. Играет при каждом запуске;
    // пропуск — Esc или нажатие в правом нижнем углу экрана, где подсказка.
    public class IntroController : MonoBehaviour
    {
        const float WindowW = 240, WindowH = 120, FrameTime = 0.22f, FadeTime = 0.5f, CharsPerSecond = 24f;

        struct Scene
        {
            public string id;
            public int frameWidth;
            public Vector2 panFrom, panTo;   // левый верхний угол окна на картинке (пиксели)
            public float duration;
            public string text;
        }

        static readonly Scene[] Scenes =
        {
            new Scene { id = "Intro/Scene1", frameWidth = 256, panFrom = new Vector2(8, 0), panTo = new Vector2(8, 40), duration = 6f,
                        text = "Мир погрузился во мрак с приходом зла - Луноликого..." },
            new Scene { id = "Intro/Scene2", frameWidth = 288, panFrom = new Vector2(0, 0), panTo = new Vector2(48, 0), duration = 7f,
                        text = "Чтобы стать сильнее, он стал поглощать души людей и превращать их в нечисть." },
            new Scene { id = "Intro/Scene3", frameWidth = 240, panFrom = new Vector2(0, 40), panTo = new Vector2(0, 4), duration = 7f,
                        text = "Лишь нашему герою хватило воли сохранить душу не чёрной. Теперь он отомстит Луноликому..." },
        };


        public bool IsPlaying { get; private set; }

        Action onDone;
        bool finishing;   // идёт затемнение перед меню: картинка стоит, ввод не принимается
        int scene;
        float time;
        int typed;
        AudioSource voice;
        readonly Sprite[][] frames = new Sprite[Scenes.Length][];

        GameManager gm => GameManager.I;

        void Awake()
        {
            voice = GameAudio.AddSource(gameObject, false);
            voice.playOnAwake = false;
        }

        // Запустить; false — картинок нет, вступление пропущено сразу
        public bool Play(Action done)
        {
            for (int i = 0; i < Scenes.Length; i++)
            {
                if (!ArtLibrary.Has(Scenes[i].id)) return false;
                frames[i] = ArtLibrary.Frames(Scenes[i].id, null, Scenes[i].frameWidth);
            }
            onDone = done;
            finishing = false;
            scene = 0;
            time = 0;
            typed = 0;
            IsPlaying = true;
            return true;
        }

        void Finish()
        {
            if (!IsPlaying || finishing) return;
            finishing = true;
            // последняя картинка остаётся на экране, пока он гаснет; меню появляется уже в темноте
            bool started = gm.Fader.Transition(() =>
            {
                IsPlaying = false;
                finishing = false;
                onDone?.Invoke();
            });
            if (started) return;
            IsPlaying = false;
            finishing = false;
            onDone?.Invoke();
        }

        void Update()
        {
            if (!IsPlaying || finishing) return;
            if (SkipPressed()) { Finish(); return; }

            time += Mathf.Min(Time.unscaledDeltaTime, 0.1f);   // подвисание не должно проглатывать сцену
            var sc = Scenes[scene];
            int visible = Mathf.Clamp(Mathf.FloorToInt((time - 0.4f) * CharsPerSecond), 0, sc.text.Length);
            for (int i = typed; i < visible; i++)
            {
                if (sc.text[i] == ' ' || i % 2 != 0) continue;
                voice.pitch = UnityEngine.Random.Range(1.45f, 1.6f);   // сухой «щелчок» машинки, выше голоса Луноликого
                voice.PlayOneShot(SfxSynth.Voice, 0.8f);
                break;
            }
            typed = visible;

            if (time >= sc.duration)
            {
                scene++;
                time = 0;
                typed = 0;
                if (scene >= Scenes.Length)
                {
                    scene = Scenes.Length - 1;         // последняя картинка стоит, пока экран гаснет
                    time = Scenes[scene].duration;
                    Finish();
                }
            }
        }

        static bool SkipPressed()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) return true;
            var pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame) return false;
            Vector2 p = pointer.position.ReadValue();   // от левого нижнего угла
            return p.x > Screen.width * 0.7f && p.y < Screen.height * 0.25f;
        }

        // ---------- отрисовка ----------

        void OnGUI()
        {
            if (!IsPlaying || gm == null || Event.current.type != EventType.Repaint) return;
            GUI.depth = -20;   // поверх всего
            float W = gm.Pixel.RtWidth, H = gm.Pixel.RtHeight, s = gm.Pixel.PixelScale;

            PixelUI.Box(-2, -2, W + 4, H + 4, Color.black);
            var sc = Scenes[scene];
            float wx = Mathf.Round((W - WindowW) / 2), wy = Mathf.Round((H - 180) / 2) + 14;

            // яркость ступенями: появление и угасание, как в старых интро
            float fade = Mathf.Min(Mathf.Clamp01(time / FadeTime), Mathf.Clamp01((sc.duration - time) / FadeTime));
            if (scene == Scenes.Length - 1 && time > sc.duration - FadeTime) fade = 1f;   // последняя — без угасания, резкий обрыв
            float k = Mathf.Ceil(fade * 4f) / 4f;

            // панорама: окно едет по картинке целыми пикселями
            var anim = frames[scene];
            var sprite = anim[Mathf.FloorToInt(time / FrameTime) % anim.Length];
            float t = Mathf.Clamp01(time / sc.duration);
            t = t * t * (3f - 2f * t);
            Vector2 pan = new Vector2(Mathf.Round(Mathf.Lerp(sc.panFrom.x, sc.panTo.x, t)), Mathf.Round(Mathf.Lerp(sc.panFrom.y, sc.panTo.y, t)));
            var tex = sprite.texture;
            Rect r = sprite.textureRect;
            var uv = new Rect((r.x + pan.x) / tex.width, (r.y + r.height - pan.y - WindowH) / tex.height,
                              WindowW / tex.width, WindowH / tex.height);
            var prev = GUI.color;
            GUI.color = new Color(k, k, k, 1f);
            GUI.DrawTextureWithTexCoords(PixelUI.R(wx, wy, WindowW, WindowH), tex, uv);
            GUI.color = prev;

            // текст под картинкой: белый, печатается по буквам
            int visible = Mathf.Clamp(Mathf.FloorToInt((time - 0.4f) * CharsPerSecond), 0, sc.text.Length);
            float ty = wy + WindowH + 10;
            int left = visible;
            foreach (var line in Wrap(sc.text, WindowW * s, s))
            {
                if (left <= 0) break;
                string part = line.Length <= left ? line : line.Substring(0, left);
                Vector2 g = gm.Pixel.RtToGui(wx, ty);
                PixelFont.Draw(part, g.x, g.y, s, new Color(k, k, k, 1f));
                left -= line.Length + 1;
                ty += 11;
            }

            // подсказка пропуска в правом нижнем углу — как подсказки главного меню: клавиша в рамке и действие.
            // На телефоне клавиши нет — в рамке само слово.
            bool touch = Application.isMobilePlatform;
            string key = touch ? "ПРОПУСТИТЬ" : "ESC", action = touch ? "" : "ПРОПУСТИТЬ";
            PixelUI.KeyHint(W - 5 - PixelUI.KeyHintWidth(key, action), H - 10, key, action);
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
