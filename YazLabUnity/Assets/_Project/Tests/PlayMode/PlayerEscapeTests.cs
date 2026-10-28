using System.Collections;
using DungeonEscape.Core;
using DungeonEscape.Interaction;
using DungeonEscape.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DungeonEscape.Tests
{
    /// <summary>
    /// Hucreden kacis fizigi: oyuncu kapali kapidan ve fare tunelinden gecemez,
    /// anahtarla kapiyi acip hucreden cikabilir.
    /// </summary>
    public class PlayerEscapeTests
    {
        // Level 01 dunya koordinatlari: hucre zemini y 1-4, kapi x 6-8 / y 4-6, fare tuneli x 9-10, salon y >= 7
        private const float CellTop = 4f;
        private static readonly Vector2 BelowDoor = new(7f, 3.3f);
        private static readonly Vector2 BelowTunnel = new(9.5f, 3.3f);

        private PlayerController _player;
        private FakePlayerInput _input;
        private LockedDoor _cellDoor;

        [UnitySetUp]
        public IEnumerator LoadLevel()
        {
            GameEvents.Reset();
            yield return SceneManager.LoadSceneAsync("Main");
            _player = Object.FindAnyObjectByType<PlayerController>();
            _cellDoor = GameObject.Find("CellDoor").GetComponent<LockedDoor>();
            _input = new FakePlayerInput();
            _player.SetInput(_input);
        }

        [UnityTest]
        public IEnumerator ClosedCellDoor_BlocksPlayer()
        {
            yield return WalkFrom(BelowDoor, Vector2.up, 1.5f);
            Assert.Less(_player.transform.position.y, CellTop, "Oyuncu kapali kapidan gecmemeli.");
        }

        [UnityTest]
        public IEnumerator RatTunnel_BlocksPlayer()
        {
            yield return WalkFrom(BelowTunnel, Vector2.up, 1.5f);
            Assert.Less(_player.transform.position.y, CellTop, "Fare tuneli oyuncuya kapali olmali.");
        }

        [UnityTest]
        public IEnumerator CellSideWall_BlocksPlayer()
        {
            // Hucrenin dogu duvari x = 11
            yield return WalkFrom(new Vector2(10.3f, 2.5f), Vector2.right, 1.5f);
            Assert.Less(_player.transform.position.x, 11f, "Oyuncu duvardan gecmemeli.");
        }

        [UnityTest]
        public IEnumerator DoorWithoutKey_StaysLocked()
        {
            yield return WalkFrom(BelowDoor, Vector2.zero, 0.1f);
            yield return PressInteract();
            Assert.IsFalse(_cellDoor.IsOpen);
            Assert.AreEqual(GamePhase.InCell, GameFlow.Instance.Phase);
        }

        [UnityTest]
        public IEnumerator Key_OpensDoor_ConsumesKey_AndStartsEscape()
        {
            _player.Keys.AddKey(KeyIds.Cell);
            yield return WalkFrom(BelowDoor, Vector2.zero, 0.1f);
            yield return PressInteract();

            Assert.IsTrue(_cellDoor.IsOpen);
            Assert.IsFalse(_player.Keys.HasKey(KeyIds.Cell), "Anahtar kapida kullanilmali.");
            Assert.AreEqual(GamePhase.Escaping, GameFlow.Instance.Phase);
        }

        [UnityTest]
        public IEnumerator OpenDoor_LetsPlayerLeaveCell()
        {
            _cellDoor.Open();
            yield return WalkFrom(BelowDoor, Vector2.up, 2f);
            Assert.Greater(_player.transform.position.y, 7f, "Oyuncu salona cikabilmeli.");
        }

        private IEnumerator WalkFrom(Vector2 start, Vector2 direction, float seconds)
        {
            var body = _player.GetComponent<Rigidbody2D>();
            body.position = start;
            _player.transform.position = start;
            _input.Move = direction;
            yield return new WaitForSeconds(seconds);
            _input.Move = Vector2.zero;
            yield return new WaitForFixedUpdate();
        }

        private IEnumerator PressInteract()
        {
            _input.InteractPressed = true;
            yield return null;
            _input.InteractPressed = false;
            yield return null;
        }
    }
}
