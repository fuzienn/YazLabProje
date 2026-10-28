using DungeonEscape.Characters;
using DungeonEscape.Companion;
using DungeonEscape.Level;
using DungeonEscape.Navigation;
using DungeonEscape.Player;
using NavMeshPlus.Components;
using NavMeshPlus.Extensions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace DungeonEscape.Editor
{
    /// <summary>
    /// NavMesh ajan tiplerini (Rat, Guard) ve fare tuneli alanini proje ayarlarina ekler,
    /// sahneye 2D NavMesh yuzeylerini ve fare yoldasini kurar.
    /// </summary>
    public static class NavigationSetup
    {
        public const string WallsLayer = "Walls";
        public const string GuardsLayer = "Guards";

        private const string NavSettingsPath = "ProjectSettings/NavMeshAreas.asset";
        private const int RatTunnelAreaIndex = 3;
        private const float RatRadius = 0.15f;
        private const float GuardRadius = 0.3f;
        private const string PrefabFolder = "Assets/_Project/Prefabs";

        [MenuItem("Dungeon Escape/Setup Navigation And Rat")]
        public static void Setup()
        {
            EnsureProjectSettings();

            var root = GameObject.Find("Navigation");
            if (root != null) Object.DestroyImmediate(root);
            root = new GameObject("Navigation");
            // NavMeshPlus yuzeyleri XY duzlemine bakacak sekilde dondurulur
            root.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            root.AddComponent<NavMeshRuntimeBaker>();
            CreateSurface(root.transform, NavAreas.RatAgent);
            CreateSurface(root.transform, NavAreas.GuardAgent);

            var player = Object.FindAnyObjectByType<PlayerController>();
            MarkIgnored(player.gameObject);
            var rat = GameplaySetup.PlaceInScene(BuildRatPrefab(), MarkerKind.RatSpawn);
            rat.GetComponent<RatAgent>().SetOwner(player);

            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log("Navigasyon yuzeyleri ve fare kuruldu.");
        }

        /// <summary>Nesneyi NavMesh pisirmesinde verilen alan olarak isaretler.</summary>
        public static void MarkArea(GameObject go, string areaName)
        {
            EnsureProjectSettings();
            if (!go.TryGetComponent(out NavMeshModifier modifier)) modifier = go.AddComponent<NavMeshModifier>();
            modifier.overrideArea = true;
            modifier.area = NavMesh.GetAreaFromName(areaName);
        }

        /// <summary>Karakterlerin sprite'lari NavMesh kaynagi olmasin.</summary>
        public static void MarkIgnored(GameObject go)
        {
            if (!go.TryGetComponent(out NavMeshModifier modifier)) modifier = go.AddComponent<NavMeshModifier>();
            modifier.ignoreFromBuild = true;
        }

        private static void CreateSurface(Transform parent, string agentName)
        {
            var go = new GameObject($"Surface_{agentName}");
            go.transform.SetParent(parent, false);
            var surface = go.AddComponent<NavMeshSurface>();
            surface.agentTypeID = NavAreas.AgentTypeId(agentName);
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
            surface.defaultArea = NavMesh.GetAreaFromName(NavAreas.NotWalkableArea);
            surface.hideEditorLogs = true;
            go.AddComponent<CollectSources2d>();
        }

        private static GameObject BuildRatPrefab()
        {
            var go = new GameObject("Rat");
            var set = GameplaySetup.LoadOrCreateSpriteSet("rat");
            go.AddComponent<SpriteRenderer>().sprite = set.Get(Facing.Side)[0];

            var agent = go.AddComponent<NavMeshAgent>();
            agent.speed = 2.6f;
            agent.acceleration = 20f;
            agent.angularSpeed = 0f;
            agent.stoppingDistance = 0.05f;
            agent.radius = RatRadius;
            agent.height = 0.3f;
            agent.baseOffset = 0f;
            agent.enabled = false;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            agent.autoBraking = true;

            go.AddComponent<NavMeshNavigator>().Configure(NavAreas.RatAgent, false);
            go.AddComponent<RaySensor2D>().Configure(16, 3f, LayerMask.GetMask(WallsLayer, GuardsLayer));
            go.AddComponent<CharacterAnimator>().SetSprites(set);
            go.AddComponent<RatAgent>();
            MarkIgnored(go);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabFolder}/Rat.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        /// <summary>Rat ve Guard ajan tiplerini, RatTunnel alanini proje NavMesh ayarlarina ekler.</summary>
        private static void EnsureProjectSettings()
        {
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath(NavSettingsPath)[0]);
            bool changed = false;

            var area = settings.FindProperty("areas").GetArrayElementAtIndex(RatTunnelAreaIndex);
            if (area.FindPropertyRelative("name").stringValue != NavAreas.RatTunnelArea)
            {
                area.FindPropertyRelative("name").stringValue = NavAreas.RatTunnelArea;
                area.FindPropertyRelative("cost").floatValue = 1f;
                changed = true;
            }

            changed |= EnsureAgent(settings, NavAreas.RatAgent, RatRadius);
            changed |= EnsureAgent(settings, NavAreas.GuardAgent, GuardRadius);

            if (!changed) return;
            settings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        private static bool EnsureAgent(SerializedObject settings, string agentName, float radius)
        {
            var names = settings.FindProperty("m_SettingNames");
            for (int i = 0; i < names.arraySize; i++)
                if (names.GetArrayElementAtIndex(i).stringValue == agentName) return false;

            var list = settings.FindProperty("m_Settings");
            int index = list.arraySize;
            list.InsertArrayElementAtIndex(index);
            var agent = list.GetArrayElementAtIndex(index);

            var lastId = settings.FindProperty("m_LastAgentTypeID");
            int id = lastId.intValue + 1;
            lastId.intValue = id;

            agent.FindPropertyRelative("agentTypeID").intValue = id;
            agent.FindPropertyRelative("agentRadius").floatValue = radius;
            agent.FindPropertyRelative("agentHeight").floatValue = radius * 2f;
            agent.FindPropertyRelative("agentSlope").floatValue = 45f;
            agent.FindPropertyRelative("agentClimb").floatValue = 0.1f;
            agent.FindPropertyRelative("minRegionArea").floatValue = 0.1f;
            agent.FindPropertyRelative("manualCellSize").boolValue = true;
            agent.FindPropertyRelative("cellSize").floatValue = 0.05f;

            names.InsertArrayElementAtIndex(names.arraySize);
            names.GetArrayElementAtIndex(names.arraySize - 1).stringValue = agentName;
            return true;
        }
    }
}
