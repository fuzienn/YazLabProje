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
        private const string DoorSheetName = "doors_lever_chest_animation";
        private static readonly string[] DoorRows = { "door_grate", "door_wood", "door_grate_dark", "door_wood_dark", "chest" };
        private const string FireSheetName = "fire_animation";
        private const int FireFrameHeight = 48;
        private static readonly (string name, int x, int width)[] FireColumns =
        {
            ("torch_small", 0, 32), ("torch", 32, 32), ("brazier_big", 64, 64), ("brazier", 128, 48)
        };

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
            List<SpriteRect> rects;
            if (baseName == DoorSheetName) rects = DoorSheetSlice(texture);
            else if (baseName == FireSheetName) rects = FireSheetSlice(texture);
            else if (assetPath.Contains("/Props/")) rects = AutoSlice(texture, baseName);
            else rects = GridSlice(texture, baseName, assetPath.Contains("/Characters/"));
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

                    var name = isCharacter && row < CharacterDirections.Length
                        ? $"{baseName}_{CharacterDirections[row]}_{col}"
                        : $"{baseName}_{row}_{col}";
                    rects.Add(MakeRect(name, rect));
                }
            }
            return rects;
        }

        /// <summary>
        /// Kapi sayfasi: ilk 5 satir 32x32 kapi/sandik kareleri, sonra 32x16 kucuk sandik ve 16x16 salter.
        /// </summary>
        private static List<SpriteRect> DoorSheetSlice(Texture2D texture)
        {
            var rects = new List<SpriteRect>();
            int top = texture.height;
            for (int row = 0; row < DoorRows.Length; row++)
                for (int frame = 0; frame < 5; frame++)
                    rects.Add(MakeRect($"{DoorRows[row]}_{frame}", new Rect(frame * 32, top - (row + 1) * 32, 32, 32)));
            for (int frame = 0; frame < 5; frame++)
                rects.Add(MakeRect($"chest_small_{frame}", new Rect(frame * 32, top - 176, 32, 16)));
            for (int frame = 0; frame < 5; frame++)
                rects.Add(MakeRect($"lever_{frame}", new Rect(frame * 16, top - 192, 16, 16)));
            rects.RemoveAll(r => IsEmpty(texture, r.rect));
            return rects;
        }

        /// <summary>Ates sayfasi: 4 sutun (mesale, buyuk/kucuk mangal), her kare 48px yuksek.</summary>
        private static List<SpriteRect> FireSheetSlice(Texture2D texture)
        {
            var rects = new List<SpriteRect>();
            int frames = texture.height / FireFrameHeight;
            foreach (var (name, x, width) in FireColumns)
                for (int frame = 0; frame < frames; frame++)
                    rects.Add(MakeRect($"{name}_{frame}", new Rect(x, texture.height - (frame + 1) * FireFrameHeight, width, FireFrameHeight)));
            return rects;
        }

        private static SpriteRect MakeRect(string name, Rect rect)
        {
            return new SpriteRect
            {
                name = name,
                rect = rect,
                alignment = SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
                spriteID = GUID.Generate()
            };
        }

        private static List<SpriteRect> AutoSlice(Texture2D texture, string baseName)
        {
            var rects = new List<SpriteRect>();
            var found = InternalSpriteUtility.GenerateAutomaticSpriteRectangles(texture, 4, 0);
            for (int i = 0; i < found.Length; i++)
            {
                rects.Add(MakeRect($"{baseName}_{i}", found[i]));
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
