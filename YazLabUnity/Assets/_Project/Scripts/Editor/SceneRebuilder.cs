using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DungeonEscape.Editor
{
    /// <summary>
    /// Main sahnesini bastan kurar. Adimlarin sirasi onemlidir: her adim bir oncekinin
    /// olusturdugu nesnelere (harita, oyuncu, kapi, gardiyan) baglanir.
    /// </summary>
    public static class SceneRebuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main.unity";

        [MenuItem("Dungeon Escape/Rebuild Scene", priority = 0)]
        public static void RebuildAll()
        {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath);

            LevelBuilder.Build();
            GameplaySetup.Setup();
            NavigationSetup.Setup();
            GuardSetup.Setup();
            DialogueSetup.Setup();
            HudSetup.Setup();

            EditorSceneManager.SaveOpenScenes();
            Debug.Log("Main sahnesi yeniden kuruldu.");
        }
    }
}
