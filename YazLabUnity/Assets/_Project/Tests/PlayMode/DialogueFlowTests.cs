using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DungeonEscape.Core;
using DungeonEscape.Dialogue;
using DungeonEscape.Guards;
using DungeonEscape.Interaction;
using DungeonEscape.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DungeonEscape.Tests
{
    /// <summary>Gardiyan diyaloğunun oyun durumuna etkisi: kapi, kazanma, kaybetme ve sunucu hatasi.</summary>
    public class DialogueFlowTests
    {
        private class ScriptedClient : ILLMClient
        {
            private readonly Queue<string> _replies;
            private readonly bool _serverDown;

            public ScriptedClient(bool serverDown, params string[] replies)
            {
                _serverDown = serverDown;
                _replies = new Queue<string>(replies);
            }

            public Task<HealthStatus> CheckHealthAsync(CancellationToken c) =>
                Task.FromResult(_serverDown
                    ? new HealthStatus(false, false, "Ollama sunucusuna ulasilamiyor")
                    : new HealthStatus(true, true, "ok"));

            public async Task<LLMResult> ChatAsync(IReadOnlyList<ChatMessage> m, string s, CancellationToken c)
            {
                await Task.Delay(300, c); // gercekci gecikme: oyun bu arada akmali
                return _serverDown
                    ? LLMResult.Fail(LLMErrorKind.ServerUnreachable, "Ollama sunucusuna ulasilamiyor")
                    : LLMResult.Ok(_replies.Dequeue(), 0.3f);
            }
        }

        private static string Json(int fear, string action) =>
            $@"{{""reply"":""..."",""fear"":{fear},""mood"":""scared"",""action"":""{action}""}}";

        private PlayerController _player;
        private Warden _warden;
        private DialogueController _controller;
        private LockedDoor _exitDoor;
        private InputField _input;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            GameEvents.Reset();
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;
            _player = Object.FindAnyObjectByType<PlayerController>();
            _player.SetInput(new FakePlayerInput());
            _warden = Object.FindAnyObjectByType<Warden>();
            _controller = Object.FindAnyObjectByType<DialogueController>();
            _exitDoor = GameObject.Find("ExitDoor").GetComponent<LockedDoor>();
            _input = Object.FindAnyObjectByType<DialogueView>().GetComponentInChildren<InputField>(true);

            GameObject.Find("CellDoor").GetComponent<LockedDoor>().Open();
            Object.FindAnyObjectByType<GuardSpawner>().Clear();
            yield return null;
            Assert.IsTrue(Warden.AllGuardsDown);
        }

        [UnityTest]
        public IEnumerator Intimidation_OpensExitDoor_AndWalkingOutWins()
        {
            _controller.Inject(new ScriptedClient(false, Json(60, "NONE"), Json(95, "OPEN_DOOR")), null);
            yield return StartTalk();
            Assert.AreEqual(GamePhase.Dialogue, GameFlow.Instance.Phase);

            yield return Say("Arkadaşlarının hâlini gördün.");
            Assert.IsFalse(_exitDoor.IsOpen);
            yield return Say("Sıra sende!");
            Assert.IsTrue(_exitDoor.IsOpen);

            yield return new WaitForSeconds(3f);
            Assert.AreEqual(GamePhase.Escaping, GameFlow.Instance.Phase, "Diyalog kapanmali");

            var input = new FakePlayerInput();
            _player.SetInput(input);
            _player.transform.position = _player.GetComponent<Rigidbody2D>().position = _exitDoor.transform.position + new Vector3(-0.5f, -2f);
            input.Move = Vector2.up;
            yield return new WaitForSeconds(1.5f);
            Assert.AreEqual(GamePhase.Won, GameFlow.Instance.Phase);
        }

        [UnityTest]
        public IEnumerator Attack_WhenWardenBrave_LosesGame()
        {
            _controller.Inject(new ScriptedClient(false, Json(5, "ATTACK")), null);
            yield return StartTalk();
            yield return Say("Seni ihtiyar budala!");
            Assert.AreEqual(GamePhase.Lost, GameFlow.Instance.Phase);
            Assert.AreEqual(LoseReason.WardenAttacked, GameFlow.Instance.LastLoseReason);
        }

        [UnityTest]
        public IEnumerator ServerDown_ShowsError_GameKeepsRunning()
        {
            _controller.Inject(new ScriptedClient(true), null);
            yield return StartTalk();
            int frames = Time.frameCount;
            yield return Say("Kapıyı aç!");

            Assert.IsFalse(_exitDoor.IsOpen);
            Assert.AreEqual(GamePhase.Dialogue, GameFlow.Instance.Phase);
            Assert.IsTrue(_input.interactable, "Oyuncu tekrar deneyebilmeli");
            Assert.Greater(Time.frameCount, frames);
            var error = GameObject.Find("DialogueCanvas/Panel/Error").GetComponent<Text>();
            StringAssert.Contains("ulasilamiyor", error.text);
        }

        private IEnumerator StartTalk()
        {
            _warden.Interact(_player.GetComponent<Interactor>());
            yield return WaitUntilInteractable();
        }

        private IEnumerator Say(string text)
        {
            _input.text = text;
            _input.onSubmit.Invoke(text);
            yield return null;
            yield return WaitUntilInteractable();
        }

        private IEnumerator WaitUntilInteractable()
        {
            float waited = 0f;
            while (!_input.interactable && waited < 5f && !GameFlow.Instance.IsOver && !_exitDoor.IsOpen)
            {
                waited += Time.deltaTime;
                yield return null;
            }
        }
    }
}
