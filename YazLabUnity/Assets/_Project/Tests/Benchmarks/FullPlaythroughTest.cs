using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using DungeonEscape.Companion;
using DungeonEscape.Core;
using DungeonEscape.Dialogue;
using DungeonEscape.Guards;
using DungeonEscape.Interaction;
using DungeonEscape.Player;
using DungeonEscape.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DungeonEscape.Tests
{
    /// <summary>
    /// Oyunu bastan sona oynar: fareye emir, anahtar, hucre kapisi, muhafizlari etkisizlestirme,
    /// gercek modelle gardiyani korkutma ve cikis. Her adim gercek bilesenlerle (NavMesh, Ollama) calisir;
    /// yalnizca oyuncunun yuruyusu isinlanarak kisaltilir. Gunluk Docs/test_results/full_playthrough.md.
    /// </summary>
    [Explicit("Gercek Ollama gerektirir; adiyla calistirin")]
    [Category("Benchmark")]
    public class FullPlaythroughTest
    {
        private const int GuardSeed = 7;

        private static readonly string[] IntimidationLines =
        {
            "Arkadaşlarının hepsini yere serdim, hepsi baygın yatıyor.",
            "Sıradaki sensin ihtiyar. Elimdeki şu demir çubuğu görüyor musun?",
            "Kapıyı aç ve kenara çekil, canını bağışlayayım. Yoksa onlardan beter olursun.",
            "Son kez söylüyorum: aç şu kapıyı!",
            "Üç deyince saldırıyorum. Bir... iki...",
            "Ben şakacı biri değilim, Bertan. Aç!"
        };

        private readonly StringBuilder _log = new();
        private readonly Stopwatch _clock = new();
        private PlayerController _player;
        private FakePlayerInput _input;
        private Interactor _interactor;

        [UnityTest]
        [Timeout(600000)]
        public IEnumerator EscapeTheDungeon_FromCellToFreedom()
        {
            GameEvents.Reset();
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;
            _clock.Start();

            _player = Object.FindAnyObjectByType<PlayerController>();
            _interactor = _player.GetComponent<Interactor>();
            _input = new FakePlayerInput();
            _player.SetInput(_input);
            Object.FindAnyObjectByType<GameHud>().DismissTitle();
            var spawner = Object.FindAnyObjectByType<GuardSpawner>();
            spawner.Spawn(GuardSeed);
            yield return null;
            Step($"Oyun basladi, {spawner.Spawned.Count} muhafiz (seed {GuardSeed})");

            // 1) Fare anahtari getirir
            var rat = Object.FindAnyObjectByType<RatAgent>();
            _input.CommandPressed = true;
            yield return null;
            _input.CommandPressed = false;
            yield return WaitUntil(() => _player.Keys.HasKey(KeyIds.Cell), 40f, "Fare anahtari getirmedi");
            Step($"Fare anahtari getirdi (hedef yolu {rat.TargetPathLength:F1} m, saklanma {rat.HideCount})");

            // 2) Hucre kapisi
            var cellDoor = GameObject.Find("CellDoor").GetComponent<LockedDoor>();
            Teleport(new Vector2(7f, 3.3f));
            yield return Press();
            Assert.IsTrue(cellDoor.IsOpen, "Hucre kapisi acilmadi");
            Assert.AreEqual(GamePhase.Escaping, GameFlow.Instance.Phase);
            Step("Hucre kapisi anahtarla acildi");

            // 3) Muhafizlar arkadan etkisiz hale getirilir
            foreach (var guard in spawner.Spawned.ToList())
            {
                for (int attempt = 0; attempt < 15 && guard.IsActive; attempt++)
                {
                    Teleport((Vector2)guard.transform.position - guard.LookDirection * 0.8f);
                    yield return Press();
                }
                Assert.IsFalse(guard.IsActive, $"{guard.name} etkisiz hale getirilemedi");
                Assert.AreNotEqual(GamePhase.Lost, GameFlow.Instance.Phase, "Oyuncu yakalandi");
                Step($"{guard.name} arkadan etkisiz hale getirildi");
            }

            // 4) Gardiyan gercek modelle korkutulur
            var warden = Object.FindAnyObjectByType<Warden>();
            Teleport((Vector2)warden.transform.position + Vector2.down * 1.2f);
            yield return Press();
            Assert.AreEqual(GamePhase.Dialogue, GameFlow.Instance.Phase, "Gardiyanla konusma baslamadi");
            var input = Object.FindAnyObjectByType<DialogueView>().GetComponentInChildren<InputField>(true);
            var controller = Object.FindAnyObjectByType<DialogueController>();
            var exitDoor = GameObject.Find("ExitDoor").GetComponent<LockedDoor>();
            yield return WaitUntil(() => input.interactable, 15f, "Sunucu saglik kontrolu bitmedi");

            foreach (var line in IntimidationLines)
            {
                input.text = line;
                input.onSubmit.Invoke(line);
                yield return null;
                yield return WaitUntil(() => input.interactable || exitDoor.IsOpen || GameFlow.Instance.IsOver, 60f, "Model yanit vermedi");
                var turn = controller.Session.Turns.Last();
                Step($"Oyuncu: \"{line}\" → Gardiyan: \"{turn.Decision?.Reply ?? turn.UserFacingError}\" " +
                     $"(korku {turn.FearLevel}, {turn.Decision?.Action}, {turn.LatencySeconds:F1} sn)");
                if (exitDoor.IsOpen || GameFlow.Instance.IsOver) break;
            }
            Assert.IsTrue(exitDoor.IsOpen, "Gardiyan kapiyi acmadi");
            Step("Gardiyan korkup cikis kapisini acti");

            // 5) Cikis
            yield return WaitUntil(() => GameFlow.Instance.Phase == GamePhase.Escaping, 6f, "Diyalog kapanmadi");
            Teleport((Vector2)exitDoor.transform.position + new Vector2(-0.5f, -2f));
            _input.Move = Vector2.up;
            yield return WaitUntil(() => GameFlow.Instance.Phase == GamePhase.Won, 5f, "Cikistan gecilemedi");
            _input.Move = Vector2.zero;
            Step($"OZGURSUN! Oyun kazanildi (oyun ici sure {GameFlow.Instance.ElapsedTime:F1} sn)");

            Write();
        }

        private void Teleport(Vector2 position)
        {
            _player.GetComponent<Rigidbody2D>().position = position;
            _player.transform.position = position;
        }

        private IEnumerator Press()
        {
            yield return new WaitForFixedUpdate();
            yield return null;
            _input.InteractPressed = true;
            yield return null;
            _input.InteractPressed = false;
            yield return null;
        }

        private static IEnumerator WaitUntil(System.Func<bool> condition, float timeout, string failure)
        {
            float waited = 0f;
            while (!condition() && waited < timeout)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsTrue(condition(), failure);
        }

        private void Step(string message)
        {
            _log.AppendLine($"| {_clock.Elapsed.TotalSeconds:F1} | {message.Replace("|", "/")} |");
            UnityEngine.Debug.Log($"[Oynanis] {message}");
        }

        private void Write()
        {
            var md = new StringBuilder("# Uctan Uca Oynanis\n\nGercek fare yapay zekasi ve gercek yerel modelle (gemma3:4b) otomatik oynanis.\n\n");
            md.AppendLine("| Gercek sure (sn) | Adim |");
            md.AppendLine("|---|---|");
            md.Append(_log);
            var folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Docs/test_results"));
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "full_playthrough.md"), md.ToString());
        }
    }
}
