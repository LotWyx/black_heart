using UnityEditor;
using UnityEngine;

namespace BlackHeart.EditorTools
{
    // Музыка в Assets/Resources/Audio/Music импортируется для потокового воспроизведения
    // (не держит весь трек в памяти). Применяется только при первом импорте.
    public class AudioImportSettings : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith("Assets/Resources/Audio/Music/")) return;
            var importer = (AudioImporter)assetImporter;
            if (!importer.importSettingsMissing) return;

            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.Streaming;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.7f;
            importer.defaultSampleSettings = settings;
        }
    }
}
