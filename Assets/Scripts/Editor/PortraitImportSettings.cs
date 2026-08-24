using UnityEditor;
using UnityEngine;

namespace WalkingIntoNight.TRPG.Editor
{
    public static class PortraitImportSettings
    {
        const string CharacterFolder = "Assets/Resources/Art/Characters";
        const string BackgroundFolder = "Assets/Resources/Art/Backgrounds";

        [MenuItem("WalkingIntoNight/Art/Apply Import Settings")]
        public static void Apply()
        {
            var count = 0;
            count += ApplyFolder(CharacterFolder, 1024, true);
            count += ApplyFolder(BackgroundFolder, 2048, false);

            Debug.Log($"Art import settings applied to {count} textures.");
        }

        static int ApplyFolder(string folder, int maxTextureSize, bool alphaIsTransparency)
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            var count = 0;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith("manifest.json")) continue;

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.maxTextureSize = maxTextureSize;
                importer.alphaIsTransparency = alphaIsTransparency;
                importer.SaveAndReimport();
                count++;
            }
            return count;
        }
    }
}
