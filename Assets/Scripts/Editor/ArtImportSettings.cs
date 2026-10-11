using UnityEditor;

namespace BlackHeart.EditorTools
{
    // Любая новая картинка в Assets/Resources/Art импортируется как пиксельный спрайт:
    // 16 пикселей на юнит, без сглаживания и сжатия. Применяется только при первом импорте,
    // поэтому настройки можно потом поменять вручную.
    public class ArtImportSettings : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Resources/Art/")) return;
            var importer = (TextureImporter)assetImporter;
            if (!importer.importSettingsMissing) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ArtLibrary.PPU;
            importer.filterMode = UnityEngine.FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = UnityEngine.SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
        }
    }
}
