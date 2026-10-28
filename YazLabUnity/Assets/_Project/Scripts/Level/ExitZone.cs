using DungeonEscape.Core;
using UnityEngine;

namespace DungeonEscape.Level
{
    /// <summary>Cikis kapisi acildiktan sonra kapi araligina giren oyuncu kazanir.</summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class ExitZone : MonoBehaviour
    {
        private BoxCollider2D _trigger;

        private void Awake()
        {
            _trigger = GetComponent<BoxCollider2D>();
            _trigger.isTrigger = true;
            _trigger.enabled = false;
        }

        private void OnEnable() => GameEvents.DoorOpened += OnDoorOpened;
        private void OnDisable() => GameEvents.DoorOpened -= OnDoorOpened;

        private void OnDoorOpened(DoorId door)
        {
            if (door == DoorId.Exit) _trigger.enabled = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player")) GameEvents.RaiseGameWon();
        }
    }
}
