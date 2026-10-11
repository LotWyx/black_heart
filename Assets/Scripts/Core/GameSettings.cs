using System;
using UnityEngine;

namespace BlackHeart
{
    // Настройки игрока (громкость), сохраняются в PlayerPrefs.
    public static class GameSettings
    {
        const string MasterKey = "blackheart.volume.master";
        const string MusicKey = "blackheart.volume.music";

        public static event Action OnChanged;

        static bool loaded;
        static float master = 1f, music = 0.6f;

        static void Load()
        {
            if (loaded) return;
            loaded = true;
            master = PlayerPrefs.GetFloat(MasterKey, 1f);
            music = PlayerPrefs.GetFloat(MusicKey, 0.6f);
        }

        // Общая громкость (все звуки и музыка)
        public static float MasterVolume
        {
            get { Load(); return master; }
            set { Load(); master = Mathf.Clamp01(value); Save(); }
        }

        // Громкость музыки (поверх общей)
        public static float MusicVolume
        {
            get { Load(); return music; }
            set { Load(); music = Mathf.Clamp01(value); Save(); }
        }

        static void Save()
        {
            PlayerPrefs.SetFloat(MasterKey, master);
            PlayerPrefs.SetFloat(MusicKey, music);
            PlayerPrefs.Save();
            Apply();
            OnChanged?.Invoke();
        }

        public static void Apply() => AudioListener.volume = MasterVolume;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            loaded = false;
            OnChanged = null;
        }
    }
}
