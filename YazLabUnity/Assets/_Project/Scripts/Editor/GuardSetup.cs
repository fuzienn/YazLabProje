using DungeonEscape.Characters;
using DungeonEscape.Guards;
using DungeonEscape.Level;
using DungeonEscape.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace DungeonEscape.Editor
{
    /// <summary>Muhafiz ve gardiyan prefab'larini kurar; sahneye spawner ve gardiyani yerlestirir.</summary>
    public static class GuardSetup
    {
        private const string PrefabFolder = "Assets/_Project/Prefabs";
        private const string VisionMaterialPath = "Assets/_Project/Art/VisionCone.mat";
        private const int GuardCount = 3;

        [MenuItem("Dungeon Escape/Setup Guards")]
        public static void Setup()
        {
            var guardPrefab = BuildGuardPrefab();

            var old = GameObject.Find("Guards");
            if (old != null) Object.DestroyImmediate(old);
            var spawner = new GameObject("Guards").AddComponent<GuardSpawner>();
            spawner.Configure(guardPrefab.GetComponent<Guard>(), GuardCount);

            var warden = GameplaySetup.PlaceInScene(BuildWardenPrefab(), MarkerKind.Warden);
            EditorSceneManager.MarkSceneDirty(warden.scene);
            Debug.Log("Muhafiz spawner'i ve gardiyan kuruldu.");
        }

        private static GameObject BuildGuardPrefab()
        {
            var go = CreateCharacter("Guard", "guard");
            go.layer = LayerMask.NameToLayer(NavigationSetup.GuardsLayer);

            var agent = go.AddComponent<NavMeshAgent>();
            agent.speed = 1.6f;
            agent.acceleration = 12f;
            agent.angularSpeed = 0f;
            agent.radius = 0.3f;
            agent.height = 0.6f;
            agent.baseOffset = 0f;
            agent.stoppingDistance = 0.05f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            agent.enabled = false;
            go.AddComponent<NavMeshNavigator>().Configure(NavAreas.GuardAgent, true);
            go.AddComponent<Guard>();

            var vision = new GameObject("Vision");
            vision.transform.SetParent(go.transform, false);
            vision.AddComponent<MeshFilter>();
            var renderer = vision.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = LoadOrCreateVisionMaterial();
            renderer.sortingOrder = -5;
            vision.AddComponent<GuardVision>().Configure(4f, 90f, LayerMask.GetMask(NavigationSetup.WallsLayer));

            return SavePrefab(go);
        }

        private static GameObject BuildWardenPrefab()
        {
            var go = CreateCharacter("Warden", "warden");
            go.AddComponent<Warden>();
            return SavePrefab(go);
        }

        /// <summary>Ortak govde: sprite, kinematik fizik govdesi, ayak hizasinda collider.</summary>
        private static GameObject CreateCharacter(string name, string spriteSet)
        {
            var go = new GameObject(name);
            var set = GameplaySetup.LoadOrCreateSpriteSet(spriteSet);
            go.AddComponent<SpriteRenderer>().sprite = set.Get(Facing.Down)[0];

            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            var collider = go.AddComponent<CircleCollider2D>();
            collider.radius = 0.3f;
            collider.offset = new Vector2(0f, -0.2f);

            go.AddComponent<CharacterAnimator>().SetSprites(set);
            NavigationSetup.MarkIgnored(go);
            return go;
        }

        private static GameObject SavePrefab(GameObject go)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabFolder}/{go.name}.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static Material LoadOrCreateVisionMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(VisionMaterialPath);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"))
            {
                color = new Color(1f, 0.85f, 0.4f, 0.12f)
            };
            AssetDatabase.CreateAsset(material, VisionMaterialPath);
            return material;
        }
    }
}
