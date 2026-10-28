using DungeonEscape.Companion;
using DungeonEscape.Player;
using DungeonEscape.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonEscape.Editor
{
    /// <summary>Gorev metni, fare durumu, baslik/duraklatma/oyun sonu ekranlarini iceren HUD'u kurar.</summary>
    public static class HudSetup
    {
        private static readonly Color Overlay = new(0.05f, 0.04f, 0.08f, 0.95f);
        private static readonly Color Accent = new(1f, 0.8f, 0.45f);

        [MenuItem("Dungeon Escape/Setup HUD")]
        public static void Setup()
        {
            var old = GameObject.Find("GameHud");
            if (old != null) Object.DestroyImmediate(old);

            var canvasGo = new GameObject("GameHud", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            var root = canvasGo.transform;

            var objective = Shadowed(UiFactory.Label("Objective", root, "", 24, TextAnchor.UpperCenter,
                new Vector2(0.26f, 1), new Vector2(0.74f, 1), new Vector2(0, -80), new Vector2(0, -14)));
            var rat = Shadowed(UiFactory.Label("RatStatus", root, "", 19, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(0.24f, 1), new Vector2(18, -90), new Vector2(0, -14)));
            var key = Shadowed(UiFactory.Label("KeyStatus", root, "", 19, TextAnchor.UpperRight,
                new Vector2(0.76f, 1), new Vector2(1, 1), new Vector2(0, -50), new Vector2(-18, -14)));

            var title = Panel(root, "TitlePanel");
            Big(UiFactory.Label("Title", title, "ZİNDANDAN KAÇIŞ", 64, TextAnchor.MiddleCenter,
                new Vector2(0, 0.62f), new Vector2(1, 0.85f), Vector2.zero, Vector2.zero));
            UiFactory.Label("Story", title,
                "Kara Kule'nin hücresinde uyandın. Yanında sadık yoldaşın fare var.\n" +
                "Fare dar delikten geçip en yakın muhafızın anahtarını çalabilir.\n" +
                "Muhafızları sessizce etkisiz hâle getir, son gardiyanı korkutup kapıyı açtır.",
                22, TextAnchor.MiddleCenter, new Vector2(0.1f, 0.38f), new Vector2(0.9f, 0.62f), Vector2.zero, Vector2.zero);
            UiFactory.Label("Controls", title,
                "WASD / Oklar: yürü    E: etkileşim    F: fareye emir    Esc: duraklat",
                20, TextAnchor.MiddleCenter, new Vector2(0, 0.26f), new Vector2(1, 0.34f), Vector2.zero, Vector2.zero).color = Accent;
            UiFactory.Label("Start", title, "Enter: başla", 28, TextAnchor.MiddleCenter,
                new Vector2(0, 0.12f), new Vector2(1, 0.22f), Vector2.zero, Vector2.zero);

            var pause = Panel(root, "PausePanel");
            Big(UiFactory.Label("Paused", pause, "DURAKLATILDI", 56, TextAnchor.MiddleCenter,
                new Vector2(0, 0.5f), new Vector2(1, 0.7f), Vector2.zero, Vector2.zero));
            UiFactory.Label("Options", pause, "Esc: devam    R: yeniden başla    Q: çık", 24, TextAnchor.MiddleCenter,
                new Vector2(0, 0.35f), new Vector2(1, 0.48f), Vector2.zero, Vector2.zero);

            var end = Panel(root, "EndPanel");
            var endTitle = Big(UiFactory.Label("EndTitle", end, "", 64, TextAnchor.MiddleCenter,
                new Vector2(0, 0.5f), new Vector2(1, 0.72f), Vector2.zero, Vector2.zero));
            var endBody = UiFactory.Label("EndBody", end, "", 26, TextAnchor.UpperCenter,
                new Vector2(0, 0.25f), new Vector2(1, 0.48f), Vector2.zero, Vector2.zero);

            canvasGo.AddComponent<GameHud>().Bind(Object.FindAnyObjectByType<PlayerController>(),
                Object.FindAnyObjectByType<RatAgent>(), objective, rat, key,
                title.gameObject, pause.gameObject, end.gameObject, endTitle, endBody);

            EditorSceneManager.MarkSceneDirty(canvasGo.scene);
        }

        private static RectTransform Panel(Transform parent, string name)
        {
            var panel = UiFactory.Rect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            panel.gameObject.AddComponent<Image>().color = Overlay;
            return panel;
        }

        private static Text Shadowed(Text text)
        {
            text.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.9f);
            return text;
        }

        private static Text Big(Text text)
        {
            text.fontStyle = FontStyle.Bold;
            text.color = Accent;
            return Shadowed(text);
        }
    }
}
