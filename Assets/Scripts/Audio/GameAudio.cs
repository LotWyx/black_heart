using UnityEngine;
using UnityEngine.Audio;

namespace BlackHeart
{
    // Микшер игры (Resources/Audio/GameMixer): две группы — Music и Sfx. Все источники звука создаются
    // через AddSource и попадают в свою группу, а громкость из настроек задаётся один раз на группу,
    // а не множителем в каждом вызове воспроизведения.
    public static class GameAudio
    {
        const string MixerPath = "Audio/GameMixer";
        const string MusicGroup = "Music", SfxGroup = "Sfx";
        const string MusicParam = "MusicVolume", SfxParam = "SfxVolume";   // открытые параметры микшера, в децибелах

        static AudioMixer mixer;
        static AudioMixerGroup music, sfx;
        static bool loaded;

        static void Load()
        {
            if (loaded) return;
            loaded = true;
            mixer = Resources.Load<AudioMixer>(MixerPath);
            if (mixer == null)
            {
                Debug.LogError($"[GameAudio] Нет микшера Resources/{MixerPath}: громкость из настроек не применяется.");
                return;
            }
            music = FindGroup(MusicGroup);
            sfx = FindGroup(SfxGroup);
        }

        static AudioMixerGroup FindGroup(string name)
        {
            var found = mixer.FindMatchingGroups(name);
            if (found.Length == 0) Debug.LogError($"[GameAudio] В микшере нет группы {name}.");
            return found.Length > 0 ? found[0] : null;
        }

        // Источник звука, подключённый к своей группе микшера
        public static AudioSource AddSource(GameObject host, bool isMusic)
        {
            Load();
            var source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.outputAudioMixerGroup = isMusic ? music : sfx;
            return source;
        }

        // Громкости из настроек (0..1) → в микшер. Вызывать не раньше Start: в Awake микшер значения не принимает.
        public static void ApplyVolumes()
        {
            Load();
            if (mixer == null) return;
            mixer.SetFloat(MusicParam, ToDecibels(GameSettings.MusicVolume));
            mixer.SetFloat(SfxParam, ToDecibels(GameSettings.SoundVolume));
        }

        static float ToDecibels(float volume) => volume <= 0.0001f ? -80f : Mathf.Log10(volume) * 20f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            loaded = false;
            mixer = null;
            music = sfx = null;
        }
    }
}
