using System;
using UnityEngine;

namespace BlackHeart
{
    // Настройки игрока (громкость звуков и музыки — независимо), сохраняются в PlayerPrefs.
    public static class GameSettings
    {
        const string SoundKey = "blackheart.volume.sound";
        const string MusicKey = "blackheart.volume.music";

        public static event Action OnChanged;

        static bool loaded;
        static float sound = 1f, music = 0.6f;

        static void Load()
        {
            if (loaded) return;
            loaded = true;
            sound = Step(PlayerPrefs.GetFloat(SoundKey, 1f));
            music = Step(PlayerPrefs.GetFloat(MusicKey, 0.6f));
        }

        // Громкость звуков: удары, разрезы, интерфейс, голос (музыка — отдельно)
        public static float SoundVolume
        {
            get { Load(); return sound; }
            set { Load(); sound = Step(value); Save(); }
        }

        // Громкость музыки
        public static float MusicVolume
        {
            get { Load(); return music; }
            set { Load(); music = Step(value); Save(); }
        }

        // Громкость меняется шагами по 10% — как деления шкалы в настройках
        static float Step(float v) => Mathf.Round(Mathf.Clamp01(v) * 10f) / 10f;

        static void Save()
        {
            PlayerPrefs.SetFloat(SoundKey, sound);
            PlayerPrefs.SetFloat(MusicKey, music);
            PlayerPrefs.Save();
            Apply();
            OnChanged?.Invoke();
        }

        // Общий уровень не трогаем: звуки и музыка регулируются каждый своей громкостью
        public static void Apply() => AudioListener.volume = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            loaded = false;
            OnChanged = null;
        }
    }
}
