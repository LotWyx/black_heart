using System;
using System.IO;
using UnityEngine;

namespace BlackHeart
{
    // Всё, что игра хранит между запусками. Новое сохраняемое значение — новое поле здесь.
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public int bestScore;
        public bool tutorialDone;
        public float soundVolume = 1f;
        public float musicVolume = 0.6f;
    }

    // Единая система сохранения: один файл save.json в папке данных игры.
    // Остальной код читает и меняет SaveSystem.Data и вызывает Save(); напрямую с диском никто не работает.
    public static class SaveSystem
    {
        const string FileName = "save.json";

        // Ключи PlayerPrefs, в которых данные лежали раньше: читаются один раз, если файла ещё нет
        const string LegacyBest = "blackheart.best";
        const string LegacyTutorial = "blackheart.tutorial.done";
        const string LegacySound = "blackheart.volume.sound";
        const string LegacyMusic = "blackheart.volume.music";

        static SaveData data;

        static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public static SaveData Data
        {
            get
            {
                if (data == null) data = Load();
                return data;
            }
        }

        static SaveData Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var loaded = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
                    if (loaded != null) return loaded;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] Не удалось прочитать сохранение, начинаем с чистого: {e.Message}");
            }
            return FromLegacyPrefs();
        }

        static SaveData FromLegacyPrefs()
        {
            var d = new SaveData();
            d.bestScore = PlayerPrefs.GetInt(LegacyBest, d.bestScore);
            d.tutorialDone = PlayerPrefs.GetInt(LegacyTutorial, 0) == 1;
            d.soundVolume = PlayerPrefs.GetFloat(LegacySound, d.soundVolume);
            d.musicVolume = PlayerPrefs.GetFloat(LegacyMusic, d.musicVolume);
            return d;
        }

        // Пишем во временный файл и подменяем им настоящий: оборванная запись не испортит сохранение
        public static void Save()
        {
            try
            {
                string temp = FilePath + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(Data, true));
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(temp, FilePath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] Не удалось записать сохранение: {e.Message}");
            }
        }

        // При выключенном Domain Reload статика переживает Play Mode — читаем файл заново
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => data = null;
    }
}
