using System;
using UnityEngine;

namespace BlackHeart
{
    // Настройки игрока (громкость звуков и музыки — независимо) и техническая настройка частоты кадров.
    // Значения громкости хранит SaveSystem, а применяет их микшер (GameAudio): звуки и музыка — отдельные группы.
    public static class GameSettings
    {
        // Игра рисует 320x180, без ограничения она выдаёт тысячи кадров в секунду и зря грузит видеокарту.
        // Потолок высокий, чтобы на быстрых мониторах разрез оставался плавным; на телефонах — 60 ради батареи.
        public const int DesktopFrameRate = 240, MobileFrameRate = 60;

        public static event Action OnChanged;

        // Громкость звуков: удары, разрезы, интерфейс, голос (музыка — отдельно)
        public static float SoundVolume
        {
            get => Step(SaveSystem.Data.soundVolume);
            set { SaveSystem.Data.soundVolume = Step(value); Save(); }
        }

        // Громкость музыки
        public static float MusicVolume
        {
            get => Step(SaveSystem.Data.musicVolume);
            set { SaveSystem.Data.musicVolume = Step(value); Save(); }
        }

        // Громкость меняется шагами по 10% — как деления шкалы в настройках
        static float Step(float v) => Mathf.Round(Mathf.Clamp01(v) * 10f) / 10f;

        static void Save()
        {
            SaveSystem.Save();
            Apply();
            OnChanged?.Invoke();
        }

        // Общий уровень не трогаем: звуки и музыка регулируются каждый своей громкостью
        public static void Apply()
        {
            AudioListener.volume = 1f;
            GameAudio.ApplyVolumes();
            QualitySettings.vSyncCount = 0;   // иначе targetFrameRate игнорируется
            Application.targetFrameRate = Application.isMobilePlatform ? MobileFrameRate : DesktopFrameRate;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => OnChanged = null;
    }
}
