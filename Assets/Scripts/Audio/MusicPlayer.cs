using UnityEngine;

namespace BlackHeart
{
    // Музыка: в главном меню и на экране проигрыша — «Music Box of Souls» (Resources/Audio/Music/MusicBoxOfSouls),
    // в бою — «Cathedral of Blades» (Resources/Audio/Music/CathedralOfBlades). Смена трека — плавным перекрёстным затуханием.
    // Другие треки — назначьте AudioClip в полях. Если боевого файла нет — синтезированная чиптюн-петля.
    public class MusicPlayer : MonoBehaviour
    {
        public AudioClip battleTrack, menuTrack;
        const string BattlePath = "Audio/Music/CathedralOfBlades";
        const string MenuPath = "Audio/Music/MusicBoxOfSouls";
        const float FadeTime = 1.2f;   // секунды; в реальном времени, чтобы работало и на паузе

        readonly AudioSource[] sources = new AudioSource[2];
        int active;                    // источник, который сейчас звучит или нарастает
        float fade = 1f;               // прогресс перехода 0..1
        static AudioClip generated;

        void Awake()
        {
            for (int i = 0; i < 2; i++)
            {
                sources[i] = gameObject.AddComponent<AudioSource>();
                sources[i].loop = true;
                sources[i].playOnAwake = false;
            }
        }

        void OnEnable() => GameSettings.OnChanged += ApplyVolume;
        void OnDisable() => GameSettings.OnChanged -= ApplyVolume;

        void Start()
        {
            if (battleTrack == null) battleTrack = Resources.Load<AudioClip>(BattlePath);
            if (battleTrack == null)
            {
                if (generated == null) generated = MusicSynth.BuildSunsetLoop();
                battleTrack = generated;
            }
            if (menuTrack == null) menuTrack = Resources.Load<AudioClip>(MenuPath);
            if (menuTrack == null) menuTrack = battleTrack;
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            AudioClip want = gm.CurrentState == GameManager.State.Playing ? battleTrack
                           : gm.CurrentState == GameManager.State.Dying ? sources[active].clip   // душа трескается — не перебиваем
                           : menuTrack;
            if (want != null && sources[active].clip != want) SwitchTo(want);
            if (fade < 1f) fade = Mathf.Min(1f, fade + Time.unscaledDeltaTime / FadeTime);
            ApplyVolume();
        }

        void SwitchTo(AudioClip clip)
        {
            bool silent = sources[active].clip == null;
            active = 1 - active;
            sources[active].clip = clip;
            sources[active].Play();
            fade = silent ? 1f : 0f;   // самый первый трек — сразу, без затухания
        }

        void ApplyVolume()
        {
            float v = GameSettings.MusicVolume;
            var current = sources[active];
            var previous = sources[1 - active];
            current.volume = v * fade;
            previous.volume = v * (1f - fade);
            if (fade >= 1f && previous.isPlaying) previous.Stop();
        }
    }

    static class MusicSynth
    {
        const int Rate = 22050;

        public static AudioClip BuildSunsetLoop()
        {
            const float bpm = 92f;
            float step = 60f / bpm / 4f;                 // шестнадцатая
            float[][] chords =
            {
                new[] { 220.00f, 261.63f, 329.63f },     // Am
                new[] { 174.61f, 220.00f, 261.63f },     // F
                new[] { 261.63f, 329.63f, 392.00f },     // C
                new[] { 196.00f, 246.94f, 293.66f },     // G
            };
            float[] bass = { 110.00f, 87.31f, 130.81f, 98.00f };
            int[] arpUp = { 0, 1, 2, 1 }, arpDown = { 2, 1, 0, 1 };

            const int bars = 8;
            int steps = bars * 16;
            var data = new float[Mathf.CeilToInt(steps * step * Rate) + Rate / 4];

            for (int s = 0; s < steps; s++)
            {
                int bar = s / 16, chord = bar % 4, inBar = s % 16;
                float t0 = s * step;
                var notes = chords[chord];

                // Арпеджио: первая половина петли вверх, вторая вниз; во второй половине такта октавой выше
                int[] arp = bar < 4 ? arpUp : arpDown;
                float lead = notes[arp[inBar % 4]] * (inBar >= 8 ? 4f : 2f);
                AddTone(data, t0, step * 0.9f, lead, 0.055f, Wave.Pulse25, 18f);

                if (s % 2 == 0) AddTone(data, t0, step * 1.8f, bass[chord], 0.13f, Wave.Triangle, 4f);
                if (s % 4 == 2) AddNoise(data, t0, 0.03f, 0.025f, s);
                if (inBar == 0)
                    foreach (var n in notes) AddTone(data, t0, step * 16f, n, 0.022f, Wave.Triangle, 0.6f, 0.25f);
            }

            for (int i = 0; i < data.Length; i++) data[i] = Mathf.Clamp(data[i], -1f, 1f);
            int length = Mathf.CeilToInt(steps * step * Rate);
            var clip = AudioClip.Create("SunsetLoop", length, 1, Rate, false);
            var trimmed = new float[length];
            System.Array.Copy(data, trimmed, length);
            clip.SetData(trimmed, 0);
            return clip;
        }

        enum Wave { Pulse25, Triangle }

        static void AddTone(float[] data, float start, float duration, float freq, float volume, Wave wave,
            float decay, float attack = 0.005f)
        {
            int i0 = Mathf.FloorToInt(start * Rate), n = Mathf.CeilToInt(duration * Rate);
            for (int i = 0; i < n && i0 + i < data.Length; i++)
            {
                float t = i / (float)Rate;
                float phase = (t * freq) % 1f;
                float v = wave == Wave.Pulse25 ? (phase < 0.25f ? 1f : -1f) : 1f - 4f * Mathf.Abs(phase - 0.5f);
                float env = Mathf.Min(1f, t / attack) * Mathf.Exp(-decay * t);
                data[i0 + i] += v * volume * env;
            }
        }

        static void AddNoise(float[] data, float start, float duration, float volume, int seed)
        {
            var rnd = new System.Random(seed);
            int i0 = Mathf.FloorToInt(start * Rate), n = Mathf.CeilToInt(duration * Rate);
            for (int i = 0; i < n && i0 + i < data.Length; i++)
                data[i0 + i] += ((float)rnd.NextDouble() * 2f - 1f) * volume * (1f - i / (float)n);
        }
    }
}
