using System.Collections.Generic;
using System.Linq;
using DungeonEscape.Core;
using DungeonEscape.Interaction;
using DungeonEscape.Level;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace DungeonEscape.Editor
{
    /// <summary>
    /// level01.txt yerlesimini acik sahnede Tilemap, collider, kapi, mesale ve isaretcilere donusturur.
    /// Her calistirmada "Level" kokunu silip bastan kurar; boylece harita metni tek dogru kaynaktir.
    /// </summary>
    public static class LevelBuilder
    {
        private const string LayoutPath = "Assets/_Project/Tiles/level01.txt";
        private const string ThemePath = "Assets/_Project/Tiles/DungeonTheme.asset";
        private const string GeneratedTilesFolder = "Assets/_Project/Tiles/Generated";
        private const string WallSheet = "Assets/_Project/Art/Tiles/walls_floor.png";
        private const string DoorSheet = "Assets/_Project/Art/Props/doors_lever_chest_animation.png";
        private const string FireSheet = "Assets/_Project/Art/Props/fire_animation.png";
        private const string RatHoleSheet = "Assets/_Project/Art/Props/rat_hole.png";
        private const string RootName = "Level";

        public const int FloorOrder = -10;
        public const int WallOrder = 10;
        public const int DoorOrder = 11;

        [MenuItem("Dungeon Escape/Build Level 01")]
        public static void Build()
        {
            var layoutAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(LayoutPath);
            var layout = LevelLayout.Parse(layoutAsset.text);
            var theme = LoadOrCreateTheme();

            var old = GameObject.Find(RootName);
            if (old != null) Object.DestroyImmediate(old);

            var root = new GameObject(RootName);
            var grid = root.AddComponent<Grid>();

            var floor = CreateTilemap(grid, "Floor", FloorOrder, false);
            var walls = CreateTilemap(grid, "Walls", WallOrder, true);
            var tunnel = CreateTilemap(grid, "RatTunnel", WallOrder, true);

            PaintTiles(layout, theme, floor, walls, tunnel);
            CreateDoor(root.transform, "CellDoor", layout.GetBounds(CellKind.CellDoor), DoorId.Cell, KeyIds.Cell, "door_grate");
            CreateDoor(root.transform, "ExitDoor", layout.GetBounds(CellKind.ExitDoor), DoorId.Exit, null, "door_wood");
            CreateRatHole(root.transform, layout);
            CreateMarkers(root.transform, layout);

            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log($"Level 01 kuruldu: {layout.Width}x{layout.Height}, {layout.Markers.Count} isaretci.");
        }

        private static void PaintTiles(LevelLayout layout, TileTheme theme, Tilemap floor, Tilemap walls, Tilemap tunnel)
        {
            for (int x = 0; x < layout.Width; x++)
            {
                for (int y = 0; y < layout.Height; y++)
                {
                    var cell = new Vector2Int(x, y);
                    var position = new Vector3Int(x, y, 0);
                    var kind = layout.GetCell(cell);

                    // Kapilarin altina da zemin konur; kapi acilinca gorunur
                    if (kind is CellKind.Floor or CellKind.CellDoor or CellKind.ExitDoor)
                        floor.SetTile(position, theme.GetFloor(cell));

                    if (kind == CellKind.Wall)
                        walls.SetTile(position, theme.GetWall(layout.GetWallShape(cell)));
                    else if (kind == CellKind.RatTunnel)
                        tunnel.SetTile(position, theme.GetWall(layout.GetWallShape(cell)));
                }
            }
            floor.CompressBounds();
            walls.CompressBounds();
            tunnel.CompressBounds();
            RebuildColliders(walls);
            RebuildColliders(tunnel);
        }

        /// <summary>Ayni karede boyanan karolar icin collider geometrisini hemen uretir.</summary>
        private static void RebuildColliders(Tilemap tilemap)
        {
            tilemap.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();
            tilemap.GetComponent<CompositeCollider2D>().GenerateGeometry();
        }

        private static Tilemap CreateTilemap(Grid grid, string name, int order, bool solid)
        {
            var go = new GameObject(name);
            go.transform.SetParent(grid.transform, false);
            var tilemap = go.AddComponent<Tilemap>();
            go.AddComponent<TilemapRenderer>().sortingOrder = order;

            if (solid)
            {
                go.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
                go.AddComponent<TilemapCollider2D>().compositeOperation = Collider2D.CompositeOperation.Merge;
                go.AddComponent<CompositeCollider2D>().geometryType = CompositeCollider2D.GeometryType.Polygons;
            }
            return tilemap;
        }

        private static void CreateDoor(Transform parent, string name, RectInt bounds, DoorId id, string keyId, string framePrefix)
        {
            var frames = Enumerable.Range(0, 5).Select(i => FindSprite(DoorSheet, $"{framePrefix}_{i}")).ToArray();
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(bounds.center.x, bounds.center.y, 0f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = frames[0];
            renderer.sortingOrder = DoorOrder;

            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(bounds.width, bounds.height);
            go.AddComponent<LockedDoor>().Configure(id, keyId, frames);
        }

        private static void CreateRatHole(Transform parent, LevelLayout layout)
        {
            var mouth = layout.CellsOf(CellKind.RatTunnel)
                .First(c => layout.GetWallShape(c) == WallShape.FaceLower);
            var go = new GameObject("RatHole");
            go.transform.SetParent(parent, false);
            go.transform.position = CellCenter(mouth);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAllAssetsAtPath(RatHoleSheet).OfType<Sprite>().First();
            renderer.sortingOrder = DoorOrder;
        }

        private static void CreateMarkers(Transform parent, LevelLayout layout)
        {
            var markersRoot = new GameObject("Markers").transform;
            markersRoot.SetParent(parent, false);
            var brazierFrames = LoadBrazierFrames();
            var counts = new Dictionary<MarkerKind, int>();

            foreach (var marker in layout.Markers)
            {
                counts.TryGetValue(marker.Kind, out int index);
                counts[marker.Kind] = index + 1;

                var go = new GameObject($"{marker.Kind}_{index}");
                go.transform.SetParent(markersRoot, false);
                go.transform.position = CellCenter(marker.Cell);
                go.AddComponent<SpawnMarker>().Init(marker.Kind);

                if (marker.Kind == MarkerKind.Torch)
                    DecorateTorch(go, brazierFrames);
            }
        }

        private static void DecorateTorch(GameObject go, Sprite[] frames)
        {
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = frames[0];
            go.AddComponent<SpriteFrameAnimator>().SetFrames(frames, 8f);
            go.AddComponent<BoxCollider2D>().size = new Vector2(0.8f, 0.6f);

            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = new Color(1f, 0.72f, 0.4f);
            light.intensity = 1.1f;
            light.pointLightOuterRadius = 5f;
            light.pointLightInnerRadius = 0.5f;
        }

        private static Sprite[] LoadBrazierFrames()
        {
            return AssetDatabase.LoadAllAssetsAtPath(FireSheet).OfType<Sprite>()
                .Where(s => s.name.StartsWith("brazier_") && !s.name.StartsWith("brazier_big"))
                .OrderBy(s => s.name)
                .ToArray();
        }

        private static TileTheme LoadOrCreateTheme()
        {
            var theme = AssetDatabase.LoadAssetAtPath<TileTheme>(ThemePath);
            if (theme != null) return theme;

            theme = ScriptableObject.CreateInstance<TileTheme>();
            theme.Configure(
                new[] { MakeTile("walls_floor_21_0"), MakeTile("walls_floor_21_1"), MakeTile("walls_floor_22_0"), MakeTile("walls_floor_22_1") },
                MakeTile("walls_floor_1_1"),
                MakeTile("walls_floor_3_1"),
                MakeTile("walls_floor_4_1"));
            AssetDatabase.CreateAsset(theme, ThemePath);
            AssetDatabase.SaveAssets();
            return theme;
        }

        private static Tile MakeTile(string spriteName)
        {
            if (!AssetDatabase.IsValidFolder(GeneratedTilesFolder))
                AssetDatabase.CreateFolder("Assets/_Project/Tiles", "Generated");

            var path = $"{GeneratedTilesFolder}/{spriteName}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile != null) return tile;

            tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = FindSprite(WallSheet, spriteName);
            AssetDatabase.CreateAsset(tile, path);
            return tile;
        }

        private static Sprite FindSprite(string sheetPath, string spriteName)
        {
            var sprite = AssetDatabase.LoadAllAssetsAtPath(sheetPath).OfType<Sprite>()
                .FirstOrDefault(s => s.name == spriteName);
            if (sprite == null) Debug.LogError($"Sprite bulunamadi: {spriteName} ({sheetPath})");
            return sprite;
        }

        private static Vector3 CellCenter(Vector2Int cell) => new(cell.x + 0.5f, cell.y + 0.5f, 0f);
    }
}
