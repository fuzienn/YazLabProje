using System.Collections;
using System.Linq;
using DungeonEscape.Companion;
using DungeonEscape.Core;
using DungeonEscape.Guards;
using DungeonEscape.Interaction;
using DungeonEscape.Navigation;
using DungeonEscape.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DungeonEscape.Tests
{
    /// <summary>Muhafiz yerlesimi, etkisiz hale getirme, yakalanma ve gardiyanin konusma kosulu.</summary>
    public class GuardTests
    {
        private GuardSpawner _spawner;
        private PlayerController _player;
        private Interactor _interactor;
        private Warden _warden;

        [UnitySetUp]
        public IEnumerator LoadLevel()
        {
            GameEvents.Reset();
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;
            _spawner = Object.FindAnyObjectByType<GuardSpawner>();
            _player = Object.FindAnyObjectByType<PlayerController>();
            _player.SetInput(new FakePlayerInput());
            _interactor = _player.GetComponent<Interactor>();
            _warden = Object.FindAnyObjectByType<Warden>();
        }

        [UnityTest]
        public IEnumerator SameSeed_GivesSameLayout()
        {
            _spawner.Spawn(1234);
            yield return null;
            var first = Positions();
            _spawner.Spawn(1234);
            yield return null;
            CollectionAssert.AreEqual(first, Positions());

            _spawner.Spawn(98765);
            yield return null;
            CollectionAssert.AreNotEqual(first, Positions(), "Farkli seed farkli yerlesim vermeli.");
        }

        [UnityTest]
        public IEnumerator SpawnedGuards_AreOutsideCell_AndReachableByRat()
        {
            var rat = Object.FindAnyObjectByType<RatAgent>();
            for (int seed = 1; seed <= 10; seed++)
            {
                _spawner.Spawn(seed);
                yield return null;
                Assert.AreEqual(3, _spawner.Spawned.Count, $"seed {seed}");
                foreach (var guard in _spawner.Spawned)
                {
                    Assert.Greater(guard.transform.position.y, 7f, $"seed {seed}: muhafiz hucrede/tunelde olmamali");
                    Assert.IsTrue(rat.Navigator.TryGetPathLength(guard.transform.position, out _),
                        $"seed {seed}: fare {guard.name}'e ulasabilmeli");
                }
            }
        }

        [UnityTest]
        public IEnumerator Takedown_FromBehind_NeutralizesGuard()
        {
            var guard = FirstGuard();
            int neutralized = 0;
            GameEvents.GuardNeutralized += () => neutralized++;

            PlacePlayer((Vector2)guard.transform.position - guard.LookDirection * 0.8f);
            Assert.IsTrue(guard.IsBehind(_player.transform.position));
            guard.Interact(_interactor);
            yield return null;

            Assert.IsFalse(guard.IsActive);
            Assert.AreEqual(1, neutralized);
            Assert.IsFalse(guard.CanSee(guard.transform.position + Vector3.right));
        }

        [UnityTest]
        public IEnumerator Takedown_FromFront_IsRefused()
        {
            var guard = FirstGuard();
            PlacePlayer((Vector2)guard.transform.position + guard.LookDirection * 0.8f);
            guard.Interact(_interactor);
            yield return null;
            Assert.IsTrue(guard.IsActive);
        }

        [UnityTest]
        public IEnumerator Guard_SeeingPlayerOutsideCell_CatchesPlayer()
        {
            GameObject.Find("CellDoor").GetComponent<LockedDoor>().Open();
            yield return null;
            Assert.AreEqual(GamePhase.Escaping, GameFlow.Instance.Phase);

            var guard = FirstGuard();
            var spot = (Vector2)guard.transform.position + guard.LookDirection * 1.5f;
            if (!guard.Navigator.TrySnap(spot, 1f, out spot)) Assert.Inconclusive("Muhafizin onunde bos yer yok");
            PlacePlayer(spot);

            yield return new WaitForSeconds(1.5f);
            Assert.AreEqual(GamePhase.Lost, GameFlow.Instance.Phase);
            Assert.AreEqual(LoseReason.Caught, GameFlow.Instance.LastLoseReason);
        }

        [UnityTest]
        public IEnumerator Guards_DoNotCatchPlayerInsideCell()
        {
            yield return new WaitForSeconds(2f);
            Assert.AreEqual(GamePhase.InCell, GameFlow.Instance.Phase);
        }

        [UnityTest]
        public IEnumerator Warden_TalksOnlyAfterAllGuardsDown()
        {
            int talks = 0;
            _warden.TalkRequested += () => talks++;

            _warden.Interact(_interactor);
            Assert.AreEqual(0, talks, "Muhafizlar ayaktayken gardiyan konusmamali.");

            foreach (var guard in _spawner.Spawned)
            {
                PlacePlayer((Vector2)guard.transform.position - guard.LookDirection * 0.8f);
                guard.Interact(_interactor);
            }
            yield return null;

            Assert.IsTrue(Warden.AllGuardsDown);
            _warden.Interact(_interactor);
            Assert.AreEqual(1, talks);
        }

        private Guard FirstGuard()
        {
            _spawner.Spawn(42);
            return _spawner.Spawned[0];
        }

        private void PlacePlayer(Vector2 position)
        {
            _player.GetComponent<Rigidbody2D>().position = position;
            _player.transform.position = position;
        }

        private Vector2[] Positions() =>
            _spawner.Spawned.Select(g => (Vector2)g.transform.position).ToArray();
    }
}
