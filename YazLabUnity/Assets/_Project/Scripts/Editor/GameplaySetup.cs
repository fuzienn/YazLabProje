using System.Linq;
using DungeonEscape.Characters;
using DungeonEscape.Core;
using DungeonEscape.Interaction;
using DungeonEscape.Level;
using DungeonEscape.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace DungeonEscape.Editor
{
    /// <summary>
    /// Karakter sprite setlerini, oyuncu prefab'ini ve sahnedeki oyun nesnelerini (kamera takibi,
    /// oyun akisi, istem metni) kurar. LevelBuilder'dan sonra calistirilir.
    /// </summary>
    public static class GameplaySetup
    {
        private const string CharactersFolder = "Assets/_Project/Art/Characters";
        private const string SetsFolder = CharactersFolder + "/Sets";
        private const string PrefabFolder = "Assets/_Project/Prefabs";
        private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";
        private const string Renderer2DPath = "Assets/Settings/Renderer2D.asset";

        public static readonly string[] CharacterNames = { "player", "guard", "warden", "rat" };

        [MenuItem("Dungeon Escape/Setup Gameplay")]
        public static void Setup()
        {
            foreach (var name in CharacterNames) LoadOrCreateSpriteSet(name);
            ConfigureSortingAxis();

            var playerPrefab = BuildPlayerPrefab();
            var player = PlaceInScene(playerPrefab, MarkerKind.PlayerSpawn);
            SetupCamera(player.transform);
            ReplaceSceneObject("Game").AddComponent<GameFlow>();
            BuildHud(player.GetComponent<Interactor>());

            EditorSceneManager.MarkSceneDirty(player.scene);
            Debug.Log("Oyun nesneleri kuruldu.");
        }

        public static CharacterSpriteSet LoadOrCreateSpriteSet(string name)
        {
            EnsureFolder(SetsFolder);
            var path = $"{SetsFolder}/{name}.asset";
            var set = AssetDatabase.LoadAssetAtPath<CharacterSpriteSet>(path);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<CharacterSpriteSet>();
                AssetDatabase.CreateAsset(set, path);
            }

            var sprites = AssetDatabase.LoadAllAssetsAtPath($"{CharactersFolder}/{name}.png").OfType<Sprite>().ToArray();
            Sprite[] Frames(string direction) => Enumerable.Range(0, 3)
                .Select(i => sprites.First(s => s.name == $"{name}_{direction}_{i}"))
                .ToArray();

            set.Configure(Frames("down"), Frames("up"), Frames("side"));
            EditorUtility.SetDirty(set);
            return set;
        }

        /// <summary>Asagidaki karakter one cizilsin: seffaf nesneler Y eksenine gore siralanir.</summary>
        private static void ConfigureSortingAxis()
        {
            var data = AssetDatabase.LoadAssetAtPath<Renderer2DData>(Renderer2DPath);
            var so = new SerializedObject(data);
            so.FindProperty("m_TransparencySortMode").intValue = (int)TransparencySortMode.CustomAxis;
            so.FindProperty("m_TransparencySortAxis").vector3Value = new Vector3(0f, 1f, 0f);
            so.ApplyModifiedProperties();
        }

        private static GameObject BuildPlayerPrefab()
        {
            var go = new GameObject("Player");
            go.tag = "Player";
            var renderer = go.AddComponent<SpriteRenderer>();
            var set = LoadOrCreateSpriteSet("player");
            renderer.sprite = set.Get(Facing.Down)[0];

            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // Ayak hizasinda kucuk collider: 1 karoluk koridorlardan rahat gecer
            var collider = go.AddComponent<CircleCollider2D>();
            collider.radius = 0.3f;
            collider.offset = new Vector2(0f, -0.2f);

            go.AddComponent<CharacterAnimator>().SetSprites(set);
            go.AddComponent<InputSystemPlayerInput>()
                .SetActions(AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath));
            go.AddComponent<Interactor>();
            go.AddComponent<PlayerController>();

            EnsureFolder(PrefabFolder);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabFolder}/Player.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        public static GameObject PlaceInScene(GameObject prefab, MarkerKind spawn)
        {
            var old = GameObject.Find(prefab.name);
            if (old != null) Object.DestroyImmediate(old);

            var marker = Object.FindObjectsByType<SpawnMarker>().First(m => m.Kind == spawn);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = marker.transform.position;
            return instance;
        }

        private static void SetupCamera(Transform target)
        {
            var camera = Camera.main;
            if (!camera.TryGetComponent(out CameraFollow follow)) follow = camera.gameObject.AddComponent<CameraFollow>();
            var layout = LevelLayout.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Tiles/level01.txt").text);
            follow.Configure(target, new Rect(0, 0, layout.Width, layout.Height));
            camera.transform.position = new Vector3(target.position.x, target.position.y, -10f);
        }

        private static void BuildHud(Interactor interactor)
        {
            var hud = ReplaceSceneObject("HUD");
            var canvas = hud.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = hud.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            hud.AddComponent<GraphicRaycaster>();

            var promptGo = new GameObject("Prompt", typeof(RectTransform));
            promptGo.transform.SetParent(hud.transform, false);
            var rect = (RectTransform)promptGo.transform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 60f);
            rect.sizeDelta = new Vector2(800f, 40f);

            var text = promptGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 26;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 0.92f, 0.75f);
            promptGo.AddComponent<Outline>().effectColor = Color.black;

            hud.AddComponent<InteractionPromptView>().Bind(interactor, text);
        }

        private static GameObject ReplaceSceneObject(string name)
        {
            var old = GameObject.Find(name);
            if (old != null) Object.DestroyImmediate(old);
            return new GameObject(name);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
