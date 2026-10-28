using System;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DungeonEscape.Dialogue
{
    /// <summary>uGUI tabanli diyalog paneli: sohbet gecmisi, metin girisi, korku cubugu ve "Dusunuyor..." gostergesi.</summary>
    public class DialogueView : MonoBehaviour, IDialogueView
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text log;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private InputField input;
        [SerializeField] private Button sendButton;
        [SerializeField] private Text thinkingLabel;
        [SerializeField] private Text errorLabel;
        [SerializeField] private Image fearFill;
        [SerializeField] private Text fearLabel;

        private readonly StringBuilder _log = new();
        private bool _thinking;

        public event Action<string> Submitted;
        public event Action CloseRequested;

        public void Bind(GameObject root, Text logText, ScrollRect scrollRect, InputField inputField, Button send,
            Text thinking, Text error, Image fear, Text fearText)
        {
            panel = root;
            log = logText;
            scroll = scrollRect;
            input = inputField;
            sendButton = send;
            thinkingLabel = thinking;
            errorLabel = error;
            fearFill = fear;
            fearLabel = fearText;
        }

        private void Awake()
        {
            sendButton.onClick.AddListener(Submit);
            input.onSubmit.AddListener(_ => Submit());
            panel.SetActive(false);
        }

        private void Update()
        {
            if (!panel.activeSelf) return;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) CloseRequested?.Invoke();
            if (_thinking)
                thinkingLabel.text = "Gardiyan düşünüyor" + new string('.', 1 + (int)(Time.unscaledTime * 3f) % 3);
        }

        public void Open()
        {
            _log.Clear();
            log.text = string.Empty;
            errorLabel.text = string.Empty;
            panel.SetActive(true);
            FocusInput();
        }

        public void Close() => panel.SetActive(false);

        public void AddLine(Speaker speaker, string text)
        {
            string name = speaker switch
            {
                Speaker.Player => "<color=#9fd3ff>Sen</color>",
                Speaker.Warden => "<color=#ffcf7a>Gardiyan</color>",
                _ => "<color=#aaaaaa>*</color>"
            };
            if (_log.Length > 0) _log.Append('\n');
            _log.Append(name).Append(": ").Append(text);
            log.text = _log.ToString();
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 0f;
        }

        public void SetThinking(bool thinking)
        {
            _thinking = thinking;
            thinkingLabel.gameObject.SetActive(thinking);
            if (thinking) errorLabel.text = string.Empty;
        }

        public void SetFear(int level, int threshold)
        {
            // Dolgu, sag capasi korku oraninda olan duz bir Image (sprite gerektirmez)
            fearFill.rectTransform.anchorMax = new Vector2(level / 100f, 1f);
            fearFill.color = level >= threshold ? new Color(0.45f, 0.85f, 0.4f) : new Color(0.9f, 0.55f, 0.2f);
            fearLabel.text = $"Gardiyanın korkusu: {level} / eşik {threshold}";
        }

        public void ShowError(string message) => errorLabel.text = message;

        public void SetInputEnabled(bool enabled)
        {
            input.interactable = enabled;
            sendButton.interactable = enabled;
            if (enabled) FocusInput();
        }

        private void Submit()
        {
            if (!input.interactable || string.IsNullOrWhiteSpace(input.text)) return;
            var text = input.text.Trim();
            input.text = string.Empty;
            Submitted?.Invoke(text);
        }

        private void FocusInput()
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(input.gameObject);
            input.ActivateInputField();
        }
    }
}
