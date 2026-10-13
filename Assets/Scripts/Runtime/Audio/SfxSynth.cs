using System.Collections.Generic;
using UnityEngine;

namespace BlackHeart
{
    // 8-битные звуки, синтезированные в коде (меандр + шум). Можно заменить своими AudioClip в SfxPlayer.
    public static class SfxSynth
    {
        public static AudioClip Slice, Hurt, Split, EnemyHit, Laugh, Defeat, Heal, Whoosh, Crack, Shatter, Start, MenuMove, MenuSelect, Voice, TimeStop, TimeResume;

        public static void Init()
        {
            if (Slice != null) return;
            Slice = Make("slice", 0.07f, 1400f, 500f, 0.12f, 0.3f);
            Hurt = Make("hurt", 0.25f, 300f, 60f, 0.25f, 0.5f);
            Split = Make("split", 0.12f, 700f, 1500f, 0.15f, 0.4f);
            EnemyHit = Make("enemy_hit", 0.2f, 220f, 90f, 0.3f, 0.6f);
            Laugh = Concat("laugh", Tone(0.1f, 520f, 480f, 0.15f), Tone(0.1f, 460f, 420f, 0.15f), Tone(0.15f, 400f, 300f, 0.15f));
            Defeat = Make("defeat", 0.6f, 400f, 50f, 0.3f, 0.7f);
            Heal = Concat("heal", Tone(0.08f, 600f, 600f, 0.12f), Tone(0.08f, 800f, 800f, 0.12f), Tone(0.12f, 1000f, 1000f, 0.12f));
            Whoosh = Make("whoosh", 0.3f, 200f, 900f, 0.12f, 0.85f);
            Crack = Make("crack", 0.15f, 900f, 200f, 0.25f, 0.2f);
            Shatter = Make("shatter", 0.6f, 500f, 80f, 0.25f, 0.8f);
            Start = Make("start", 0.12f, 500f, 1000f, 0.15f, 0f);
            MenuMove = Make("menu_move", 0.04f, 880f, 880f, 0.08f, 0f);
            MenuSelect = Concat("menu_select", Tone(0.05f, 660f, 660f, 0.1f), Tone(0.09f, 990f, 990f, 0.1f));
            Voice = Make("voice", 0.045f, 196f, 175f, 0.12f, 0f);           // «голос» Луноликого
            TimeStop = Make("time_stop", 0.35f, 900f, 120f, 0.14f, 0.25f);
            TimeResume = Make("time_resume", 0.25f, 150f, 900f, 0.12f, 0.25f);
        }

        static AudioClip Make(string name, float duration, float f0, float f1, float volume, float noise)
        {
            return ToClip(name, Tone(duration, f0, f1, volume, noise));
        }

        static float[] Tone(float duration, float f0, float f1, float volume, float noise = 0f)
        {
            const int rate = 44100;
            int n = Mathf.CeilToInt(rate * duration);
            var data = new float[n];
            var rnd = new System.Random(n);
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                phase += Mathf.Lerp(f0, f1, t) / rate;
                float square = phase % 1f < 0.5f ? 1f : -1f;
                float white = (float)rnd.NextDouble() * 2f - 1f;
                data[i] = Mathf.Lerp(square, white, noise) * volume * Mathf.Pow(1f - t, 1.5f);
            }
            return data;
        }

        static AudioClip Concat(string name, params float[][] parts)
        {
            var all = new List<float>();
            foreach (var p in parts) all.AddRange(p);
            return ToClip(name, all.ToArray());
        }

        static AudioClip ToClip(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, 44100, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
