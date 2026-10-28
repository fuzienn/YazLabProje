using DungeonEscape.Dialogue;
using DungeonEscape.Guards;
using DungeonEscape.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DungeonEscape.Editor
{
    /// <summary>Ollama ayarlarini, diyalog panelini ve gardiyan-diyalog baglantisini sahneye kurar.</summary>
    public static class DialogueSetup
    {
        private const string SettingsPath = "Assets/_Project/Settings/OllamaSettings.asset";
        private static readonly Color PanelColor = new(0.08f, 0.06f, 0.11f, 0.92f);
        
        [MenuItem("Dungeon Escape/Setup Dialogue")]
        public static void Setup()
        {
            var settings = LoadOrCreateSettings();
            EnsureEventSystem();

            var old = GameObject.Find("DialogueCanvas");
            if (old != null) Object.DestroyImmediate(old);
            var view = BuildView();

            var controllerGo = GameObject.Find("Dialogue") ?? new GameObject("Dialogue");
            if (!controllerGo.TryGetComponent(out DialogueController controller))
                controller = controllerGo.AddComponent<DialogueController>();
            var exitDoor = GameObject.Find("ExitDoor").GetComponent<LockedDoor>();
            controller.Bind(Object.FindAnyObjectByType<Warden>(), exitDoor, view, settings);

            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            Debug.Log("Diyalog sistemi kuruldu.");
        }

        private static OllamaSettings LoadOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<OllamaSettings>(SettingsPath);
            if (settings != null) return settings;
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Settings"))
                AssetDatabase.CreateFolder("Assets/_Project", "Settings");
            settings = ScriptableObject.CreateInstance<OllamaSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
            return settings;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.transform.SetAsLastSibling();
        }

        private static DialogueView BuildView()
        {
            var canvasGo = new GameObject("DialogueCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);

            var panel = UiFactory.Rect("Panel", canvasGo.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(40, 30), new Vector2(-40, 400));
            panel.gameObject.AddComponent<Image>().color = PanelColor;

            UiFactory.Label("Title", panel, "Gardiyan Bertan", 26, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(0.5f, 1), new Vector2(20, -46), new Vector2(0, -12));
            var fearLabel = UiFactory.Label("FearLabel", panel, "", 18, TextAnchor.UpperRight, new Vector2(0.5f, 1), new Vector2(1, 1), new Vector2(0, -36), new Vector2(-20, -12));
            var fearBack = UiFactory.Rect("FearBar", panel, new Vector2(0.55f, 1), new Vector2(1, 1), new Vector2(0, -58), new Vector2(-20, -42));
            fearBack.gameObject.AddComponent<Image>().color = new Color(0.2f, 0.18f, 0.25f);
            var fearFill = UiFactory.Rect("Fill", fearBack, Vector2.zero, new Vector2(0.15f, 1), Vector2.zero, Vector2.zero);
            var fearImage = fearFill.gameObject.AddComponent<Image>();

            var scrollRect = UiFactory.Rect("Log", panel, new Vector2(0, 0), new Vector2(1, 1), new Vector2(20, 110), new Vector2(-20, -70));
            scrollRect.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.25f);
            scrollRect.gameObject.AddComponent<RectMask2D>();
            var scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var content = UiFactory.Rect("Content", scrollRect, new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, 0), new Vector2(-10, 0));
            content.pivot = new Vector2(0.5f, 1f);
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var log = content.gameObject.AddComponent<Text>();
            UiFactory.Style(log, 21, TextAnchor.UpperLeft);
            log.supportRichText = true;
            scroll.content = content;
            scroll.viewport = scrollRect;

            var thinking = UiFactory.Label("Thinking", panel, "Gardiyan düşünüyor...", 20, TextAnchor.MiddleLeft, new Vector2(0, 0), new Vector2(0.6f, 0), new Vector2(20, 72), new Vector2(0, 104));
            thinking.fontStyle = FontStyle.Italic;
            thinking.color = new Color(1f, 0.8f, 0.45f);
            var error = UiFactory.Label("Error", panel, "", 17, TextAnchor.MiddleLeft, new Vector2(0, 0), new Vector2(1, 0), new Vector2(20, 66), new Vector2(-20, 108));
            error.color = new Color(1f, 0.45f, 0.4f);
            UiFactory.Label("Hint", panel, "Enter: gönder   Esc: konuşmayı bitir", 15, TextAnchor.MiddleRight, new Vector2(0.6f, 0), new Vector2(1, 0), new Vector2(0, 72), new Vector2(-20, 104));

            var input = BuildInput(panel);
            var send = BuildButton(panel, "Gönder");

            var view = canvasGo.AddComponent<DialogueView>();
            view.Bind(panel.gameObject, log, scroll, input, send, thinking, error, fearImage, fearLabel);
            return view;
        }

        private static InputField BuildInput(RectTransform panel)
        {
            var rect = UiFactory.Rect("Input", panel, new Vector2(0, 0), new Vector2(1, 0), new Vector2(20, 18), new Vector2(-180, 62));
            rect.gameObject.AddComponent<Image>().color = new Color(0.16f, 0.14f, 0.2f);
            var input = rect.gameObject.AddComponent<InputField>();

            var placeholder = UiFactory.Label("Placeholder", rect, "Gardiyana ne söyleyeceksin?", 20, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one, new Vector2(12, 0), new Vector2(-12, 0));
            placeholder.fontStyle = FontStyle.Italic;
            placeholder.color = new Color(1f, 1f, 1f, 0.35f);
            var text = UiFactory.Label("Text", rect, "", 20, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one, new Vector2(12, 0), new Vector2(-12, 0));
            text.supportRichText = false;

            input.textComponent = text;
            input.placeholder = placeholder;
            input.characterLimit = 200;
            input.lineType = InputField.LineType.SingleLine;
            return input;
        }

        private static Button BuildButton(RectTransform panel, string caption)
        {
            var rect = UiFactory.Rect("Send", panel, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-170, 18), new Vector2(-20, 62));
            rect.gameObject.AddComponent<Image>().color = new Color(0.55f, 0.32f, 0.18f);
            var button = rect.gameObject.AddComponent<Button>();
            UiFactory.Label("Caption", rect, caption, 21, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }
    }
}
