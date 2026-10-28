using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEditorInternal;
using UnityEngine;

namespace DungeonEscape.Editor
{
    /// <summary>
    /// _Project/Art altindaki her PNG'yi pixel art ayarlariyla ice aktarir ve dilimler.
    /// Tiles ve Characters 16px izgara, Props otomatik (bos alana gore) dilimlenir.
    /// </summary>
    public class ArtImportProcessor : AssetPostprocessor
    {
        private const string ArtRoot = "Assets/_Project/Art/";
        private const int PixelsPerUnit = 16;
        private const int GridSize = 16;
        private static readonly string[] CharacterDirections = { "down", "up", "side" };

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtRoot)) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            if (provider.GetSpriteRects().Length > 0) return;

            var texture = LoadReadable(assetPath);
            var baseName = Path.GetFileNameWithoutExtension(assetPath);
            var rects = assetPath.Contains("/Props/")
                ? AutoSlice(texture, baseName)
                : GridSlice(texture, baseName, assetPath.Contains("/Characters/"));
            Object.DestroyImmediate(texture);

            provider.SetSpriteRects(rects.ToArray());
            provider.Apply();
        }

        private static Texture2D LoadReadable(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.LoadImage(File.ReadAllBytes(path));
            return texture;
        }

        private static List<SpriteRect> GridSlice(Texture2D texture, string baseName, bool isCharacter)
        {
            var rects = new List<SpriteRect>();
            int columns = texture.width / GridSize;
            int rows = texture.height / GridSize;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    // Unity'de y asagidan yukari; sayfadaki ust satir row=0 olsun
                    var rect = new Rect(col * GridSize, (rows - 1 - row) * GridSize, GridSize, GridSize);
                    if (IsEmpty(texture, rect)) continue;

                    rects.Add(new SpriteRect
                    {
                        name = isCharacter && row < CharacterDirections.Length
                            ? $"{baseName}_{CharacterDirections[row]}_{col}"
                            : $"{baseName}_{row}_{col}",
                        rect = rect,
                        alignment = SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                        spriteID = GUID.Generate()
                    });
                }
            }
            return rects;
        }

        private static List<SpriteRect> AutoSlice(Texture2D texture, string baseName)
        {
            var rects = new List<SpriteRect>();
            var found = InternalSpriteUtility.GenerateAutomaticSpriteRectangles(texture, 4, 0);
            for (int i = 0; i < found.Length; i++)
            {
                rects.Add(new SpriteRect
                {
                    name = $"{baseName}_{i}",
                    rect = found[i],
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    spriteID = GUID.Generate()
                });
            }
            return rects;
        }

        private static bool IsEmpty(Texture2D texture, Rect rect)
        {
            var pixels = texture.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
            foreach (var pixel in pixels)
                if (pixel.a > 0.01f) return false;
            return true;
        }
    }
}
