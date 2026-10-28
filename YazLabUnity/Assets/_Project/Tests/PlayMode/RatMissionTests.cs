using System.Collections;
using DungeonEscape.Companion;
using DungeonEscape.Core;
using DungeonEscape.Guards;
using DungeonEscape.Interaction;
using DungeonEscape.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DungeonEscape.Tests
{
    /// <summary>Farenin uctan uca gorevi: hedef secimi, tunelden gecis, saklanma, calma ve teslim.</summary>
    public class RatMissionTests
    {
        private const float Timeout = 40f;

        private RatAgent _rat;
        private PlayerController _player;
        private RatMissionReport? _report;
        private string _deliveredKey;

        [UnitySetUp]
        public IEnumerator LoadLevel()
        {
            GameEvents.Reset();
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;
            // Spawner'in rastgele muhafizlari kaldirilir; her test kendi muhafizlarini kurar
            Object.FindAnyObjectByType<GuardSpawner>()?.Clear();
            yield return null;
            Assert.AreEqual(0, GuardRegistry.All.Count);

            _rat = Object.FindAnyObjectByType<RatAgent>();
            _player = Object.FindAnyObjectByType<PlayerController>();
            _player.SetInput(new FakePlayerInput());
            _report = null;
            _deliveredKey = null;
            _rat.MissionFinished += r => _report = r;
            GameEvents.KeyDelivered += k => _deliveredKey = k;
            yield return null;
        }

        [UnityTest]
        public IEnumerator StealsFromNearestGuard_AndDeliversKey()
        {
            var near = TestGuard.Create(new Vector2(10.5f, 9.5f), withKey: true);
            var far = TestGuard.Create(new Vector2(18.5f, 12.5f), withKey: true);

            _rat.Command();
            yield return WaitForReport();

            Assert.IsTrue(_report.Value.Success, _report.Value.FailReason);
            Assert.IsTrue(_player.Keys.HasKey(KeyIds.Cell), "Anahtar oyuncuya teslim edilmeli.");
            Assert.AreEqual(KeyIds.Cell, _deliveredKey);
            Assert.IsFalse(near.Keys.HasKey(KeyIds.Cell), "En yakin muhafizdan calinmali.");
            Assert.IsTrue(far.Keys.HasKey(KeyIds.Cell), "Uzak muhafiza dokunulmamali.");
            Assert.AreEqual("Idle", _rat.StateName);
        }

        [UnityTest]
        public IEnumerator HidesWhileWatched_ThenSucceeds()
        {
            // Tunelin cikisina bakan muhafiz
            var watcher = TestGuard.Create(new Vector2(9.5f, 10.5f), withKey: true, blind: false);
            watcher.Look = Vector2.down;

            _rat.Command();
            float waited = 0f;
            while (_rat.StateName != "Hide" && waited < 15f)
            {
                waited += Time.deltaTime;
                yield return null;
            }
            Assert.AreEqual("Hide", _rat.StateName, "Muhafiz bakarken fare saklanmali.");

            yield return new WaitForSeconds(1f);
            Assert.AreEqual("Hide", _rat.StateName, "Muhafiz bakmaya devam ederken saklanmaya devam etmeli.");

            watcher.Look = Vector2.up;
            yield return WaitForReport();

            Assert.IsTrue(_report.Value.Success, _report.Value.FailReason);
            Assert.GreaterOrEqual(_report.Value.HideCount, 1);
            Assert.IsTrue(_player.Keys.HasKey(KeyIds.Cell));
        }

        [UnityTest]
        public IEnumerator NoGuards_FailsWithoutCrashing()
        {
            _rat.Command();
            yield return WaitForReport();

            Assert.IsFalse(_report.Value.Success);
            Assert.IsNotNull(_report.Value.FailReason);
            Assert.AreEqual("Idle", _rat.StateName);
            Assert.IsFalse(_player.Keys.HasKey(KeyIds.Cell));
        }

        [UnityTest]
        public IEnumerator GuardsWithoutKeys_AreIgnored()
        {
            TestGuard.Create(new Vector2(10.5f, 9.5f), withKey: false);
            var keyHolder = TestGuard.Create(new Vector2(4.5f, 12.5f), withKey: true);

            _rat.Command();
            yield return WaitForReport();

            Assert.IsTrue(_report.Value.Success, _report.Value.FailReason);
            Assert.IsFalse(keyHolder.Keys.HasKey(KeyIds.Cell));
        }

        [UnityTest]
        public IEnumerator SecondCommand_WhileOnMission_IsRejected()
        {
            TestGuard.Create(new Vector2(10.5f, 9.5f), withKey: true);
            _rat.Command();
            yield return null;
            Assert.IsFalse(_rat.CanAcceptCommand);
            yield return WaitForReport();
            Assert.IsFalse(_rat.CanAcceptCommand, "Anahtar oyuncudayken yeni gorev alinmamali.");
        }

        private IEnumerator WaitForReport()
        {
            float waited = 0f;
            while (_report == null && waited < Timeout)
            {
                waited += Time.deltaTime;
                yield return null;
            }
            Assert.IsNotNull(_report, $"Fare {Timeout} sn icinde gorevi bitirmedi (durum: {_rat.StateName}).");
        }
    }
}
