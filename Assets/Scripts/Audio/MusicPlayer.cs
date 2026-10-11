using UnityEngine;

namespace BlackHeart
{
    // Саундтрек игры: «Final Form» (Resources/Audio/Music/FinalForm), звучит в меню и в бою.
    // Другой трек — назначьте AudioClip в поле track. Если файла нет — синтезированная чиптюн-петля.
    public class MusicPlayer : MonoBehaviour
    {
        public AudioClip track;
        const string SoundtrackPath = "Audio/Music/FinalForm";

        AudioSource source;
        static AudioClip generated;

        void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
        }

        void OnEnable() => GameSettings.OnChanged += ApplyVolume;
        void OnDisable() => GameSettings.OnChanged -= ApplyVolume;

        void Start()
        {
            if (track == null) track = Resources.Load<AudioClip>(SoundtrackPath);
            if (track == null)
            {
                if (generated == null) generated = MusicSynth.BuildSunsetLoop();
                track = generated;
            }
            source.clip = track;
            ApplyVolume();
            source.Play();
        }

        void ApplyVolume() => source.volume = GameSettings.MusicVolume;
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
