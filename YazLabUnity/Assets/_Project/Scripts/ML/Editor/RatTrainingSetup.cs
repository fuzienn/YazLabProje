using System.Collections.Generic;
using System.Linq;
using DungeonEscape.Characters;
using DungeonEscape.Guards;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DungeonEscape.ML.Editor
{
    /// <summary>
    /// Pekistirmeli ogrenme icin RatTraining sahnesini kurar: 12 paralel arena, her birinde
    /// duvarlar, yer degistiren sutunlar, gorus konili muhafiz ve isin sensorlu fare ajani.
    /// </summary>
    public static class RatTrainingSetup
    {
        public const string ScenePath = "Assets/_Project/Scenes/RatTraining.unity";
        public const string BehaviorName = "RatStealth";

        private const string WallSheet = "Assets/_Project/Art/Tiles/walls_floor.png";
        private const string VisionMaterialPath = "Assets/_Project/Art/VisionCone.mat";
        public const string ModelPath = "Assets/_Project/ML/Models/RatStealth.onnx";
        private static readonly Vector2 ArenaSize = new(10f, 7f);
        private static readonly Vector2 Spacing = new(12f, 9f);
        private const int Columns = 4;
        private const int Rows = 3;
        private const float WallThickness = 0.5f;

        [MenuItem("Dungeon Escape/ML/Build Training Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera();

            var wallSprite = AssetDatabase.LoadAllAssetsAtPath(WallSheet).OfType<Sprite>().First(s => s.name == "walls_floor_1_1");
            for (int row = 0; row < Rows; row++)
                for (int col = 0; col < Columns; col++)
                    CreateArena(new Vector2((col - (Columns - 1) / 2f) * Spacing.x, (row - (Rows - 1) / 2f) * Spacing.y),
                        $"Arena_{row * Columns + col}", wallSprite);

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"Egitim sahnesi kuruldu: {Rows * Columns} arena.");
        }

        private static void CreateCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = Rows * Spacing.y / 2f + 0.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(70, 78, 98, 255);
            go.transform.position = new Vector3(0f, 0f, -10f);
            go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();

            var light = new GameObject("Global Light 2D").AddComponent<UnityEngine.Rendering.Universal.Light2D>();
            light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Global;
        }

        private static void CreateArena(Vector2 origin, string name, Sprite wallSprite)
        {
            var root = new GameObject(name);
            root.transform.position = origin;
            var arena = root.AddComponent<TrainingArena>();

            float halfW = ArenaSize.x / 2f + WallThickness / 2f;
            float halfH = ArenaSize.y / 2f + WallThickness / 2f;
            Block(root.transform, "WallTop", new Vector2(0, halfH), new Vector2(ArenaSize.x + 2 * WallThickness, WallThickness), wallSprite);
            Block(root.transform, "WallBottom", new Vector2(0, -halfH), new Vector2(ArenaSize.x + 2 * WallThickness, WallThickness), wallSprite);
            Block(root.transform, "WallLeft", new Vector2(-halfW, 0), new Vector2(WallThickness, ArenaSize.y), wallSprite);
            Block(root.transform, "WallRight", new Vector2(halfW, 0), new Vector2(WallThickness, ArenaSize.y), wallSprite);

            var pillars = new List<Transform>();
            for (int i = 0; i < 3; i++)
                pillars.Add(Block(root.transform, $"Pillar_{i}", Vector2.zero, Vector2.one, wallSprite));

            var guard = CreateGuard(root.transform);
            arena.Configure(ArenaSize, pillars.ToArray(), guard);
            CreateAgent(root.transform, arena);
        }

        private static Transform Block(Transform parent, string name, Vector2 position, Vector2 size, Sprite sprite)
        {
            var go = new GameObject(name) { tag = "Wall", layer = LayerMask.NameToLayer("Walls") };
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 10;
            go.AddComponent<BoxCollider2D>().size = Vector2.one;
            return go.transform;
        }

        private static TrainingGuard CreateGuard(Transform parent)
        {
            var set = DungeonEscape.Editor.GameplaySetup.LoadOrCreateSpriteSet("guard");
            var go = new GameObject("Guard") { tag = "Guard", layer = LayerMask.NameToLayer("Guards") };
            go.transform.SetParent(parent, false);
            go.AddComponent<SpriteRenderer>().sprite = set.Get(Facing.Down)[0];
            go.AddComponent<CircleCollider2D>().radius = 0.3f;
            go.AddComponent<CharacterAnimator>().SetSprites(set);

            var vision = new GameObject("Vision");
            vision.transform.SetParent(go.transform, false);
            vision.AddComponent<MeshFilter>();
            var renderer = vision.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(VisionMaterialPath);
            renderer.sortingOrder = -5;
            vision.AddComponent<GuardVision>().Configure(4f, 90f, LayerMask.GetMask("Walls"));

            return go.AddComponent<TrainingGuard>();
        }

        private static void CreateAgent(Transform parent, TrainingArena arena)
        {
            var set = DungeonEscape.Editor.GameplaySetup.LoadOrCreateSpriteSet("rat");
            var go = new GameObject("Rat");
            go.transform.SetParent(parent, false);
            go.AddComponent<SpriteRenderer>().sprite = set.Get(Facing.Side)[0];
            go.AddComponent<CharacterAnimator>().SetSprites(set);

            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            go.AddComponent<CircleCollider2D>().radius = 0.15f;

            var behavior = go.AddComponent<BehaviorParameters>();
            behavior.BehaviorName = BehaviorName;
            behavior.BrainParameters.VectorObservationSize = RatStealthAgent.VectorObservationSize;
            behavior.BrainParameters.ActionSpec = ActionSpec.MakeContinuous(2);
            // Egitilmis model varsa sahne Play'de onunla oynar; egitim sirasinda trainer onceliklidir
            behavior.Model = AssetDatabase.LoadAssetAtPath<Unity.InferenceEngine.ModelAsset>(ModelPath);
            // GPU tensorleri ML-Agents tarafinda dogrudan okunamiyor; kucuk ag icin CPU (Burst) yeterli
            behavior.InferenceDevice = InferenceDevice.Burst;

            var rays = go.AddComponent<RayPerceptionSensorComponent2D>();
            rays.SensorName = "RatRays";
            rays.DetectableTags = new List<string> { "Wall", "Guard" };
            rays.RaysPerDirection = 8;
            rays.MaxRayDegrees = 180f;
            rays.RayLength = 4f;
            rays.RayLayerMask = LayerMask.GetMask("Walls", "Guards");

            var agent = go.AddComponent<RatStealthAgent>();
            agent.SetArena(arena);
            agent.MaxStep = 1500;

            var requester = go.AddComponent<DecisionRequester>();
            requester.DecisionPeriod = 5;
            requester.TakeActionsBetweenDecisions = true;
        }
    }
}
