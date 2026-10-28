using System.Collections;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DungeonEscape.Dialogue;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DungeonEscape.Tests
{
    /// <summary>
    /// Gercek Ollama sunucusuyla calisan testler. Sunucu yoksa saglikli-yol testleri Inconclusive olur;
    /// hata yolu testleri (yanlis port, olmayan model, zaman asimi) her zaman calisir.
    /// </summary>
    public class OllamaIntegrationTests
    {
        private static OllamaSettings Settings(string url = "http://localhost:11434", string model = "gemma3:4b", int timeout = 60)
        {
            var settings = ScriptableObject.CreateInstance<OllamaSettings>();
            settings.baseUrl = url;
            settings.model = model;
            settings.timeoutSeconds = timeout;
            settings.healthTimeoutSeconds = 2;
            return settings;
        }

        private static IEnumerator Await<T>(Task<T> task)
        {
            while (!task.IsCompleted) yield return null;
        }

        [UnityTest]
        public IEnumerator WrongPort_ReportsServerUnreachable_Quickly()
        {
            var client = new OllamaClient(Settings("http://localhost:1", timeout: 5));
            var watch = Stopwatch.StartNew();

            var health = client.CheckHealthAsync(CancellationToken.None);
            yield return Await(health);
            Assert.IsFalse(health.Result.ServerUp);
            StringAssert.Contains("ulaşılamıyor", health.Result.Message);

            var chat = client.ChatAsync(new[] { ChatMessage.User("merhaba") }, GuardDecision.JsonSchema, CancellationToken.None);
            yield return Await(chat);
            Assert.AreEqual(LLMErrorKind.ServerUnreachable, chat.Result.ErrorKind);
            Assert.Less(watch.Elapsed.TotalSeconds, 6);
        }

        [UnityTest]
        public IEnumerator UnroutableHost_TimesOut_WithoutBlocking()
        {
            var client = new OllamaClient(Settings("http://10.255.255.1:11434", timeout: 1));
            int framesBefore = Time.frameCount;

            var chat = client.ChatAsync(new[] { ChatMessage.User("merhaba") }, GuardDecision.JsonSchema, CancellationToken.None);
            yield return Await(chat);

            Assert.IsTrue(chat.Result.ErrorKind is LLMErrorKind.Timeout or LLMErrorKind.ServerUnreachable, chat.Result.ErrorKind.ToString());
            Assert.Greater(Time.frameCount - framesBefore, 5, "Beklerken oyun kareleri ilerlemeli (bloklama yok)");
        }

        [UnityTest]
        public IEnumerator Cancellation_StopsRequest()
        {
            var client = new OllamaClient(Settings("http://10.255.255.1:11434", timeout: 30));
            var cts = new CancellationTokenSource();
            var chat = client.ChatAsync(new[] { ChatMessage.User("merhaba") }, GuardDecision.JsonSchema, cts.Token);
            yield return null;
            cts.Cancel();
            yield return Await(chat);
            Assert.AreEqual(LLMErrorKind.Cancelled, chat.Result.ErrorKind);
        }

        [UnityTest]
        public IEnumerator MissingModel_IsDetected()
        {
            var client = new OllamaClient(Settings(model: "olmayan-model:1b"));
            var health = client.CheckHealthAsync(CancellationToken.None);
            yield return Await(health);
            if (!health.Result.ServerUp) Assert.Inconclusive("Ollama calismiyor");

            Assert.IsFalse(health.Result.ModelAvailable);
            StringAssert.Contains("ollama pull", health.Result.Message);

            var chat = client.ChatAsync(new[] { ChatMessage.User("merhaba") }, GuardDecision.JsonSchema, CancellationToken.None);
            yield return Await(chat);
            Assert.AreEqual(LLMErrorKind.ModelMissing, chat.Result.ErrorKind);
        }

        [UnityTest]
        public IEnumerator RealModel_ReturnsParsableDecision()
        {
            var client = new OllamaClient(Settings());
            var health = client.CheckHealthAsync(CancellationToken.None);
            yield return Await(health);
            if (!health.Result.Healthy) Assert.Inconclusive(health.Result.Message);

            var session = new DialogueSession(client, new DecisionParser());
            var turn = session.SendAsync("Arkadaşlarının hepsi yerde. Kapıyı aç yoksa sıra sende!", CancellationToken.None);
            yield return Await(turn);

            Assert.IsTrue(turn.Result.Success, turn.Result.UserFacingError);
            Assert.IsNotEmpty(turn.Result.Decision.Reply);
            UnityEngine.Debug.Log($"[Ollama] {turn.Result.LatencySeconds:F1} sn: {turn.Result.RawOutput}");
        }
    }
}
