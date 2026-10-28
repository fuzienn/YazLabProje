using UnityEngine;

namespace DungeonEscape.Dialogue
{
    [CreateAssetMenu(menuName = "Dungeon Escape/Ollama Settings")]
    public class OllamaSettings : ScriptableObject
    {
        [Tooltip("Ekip arkadasinin makinesindeki Ollama da kullanilabilir: http://<ip>:11434")]
        public string baseUrl = "http://localhost:11434";
        public string model = "gemma3:4b";
        [Range(0f, 1.5f)] public float temperature = 0.7f;
        [Tooltip("Yanit icin azami bekleme; ilk istekte model bellege yuklenirken uzayabilir.")]
        public int timeoutSeconds = 45;
        public int healthTimeoutSeconds = 3;
        [Tooltip("Model bellekte kac dakika tutulsun (ilk yanit gecikmesini azaltir).")]
        public string keepAlive = "10m";
    }
}
