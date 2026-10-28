using DungeonEscape.Guards;
using DungeonEscape.Interaction;
using UnityEngine;

namespace DungeonEscape.Tests
{
    /// <summary>Hareket etmeyen, bakis yonu testten ayarlanan muhafiz. Fare testleri icin.</summary>
    public class TestGuard : MonoBehaviour, IGuard
    {
        public Vector2 Look = Vector2.down;
        public float ViewRange = 3.5f;
        public float ViewAngle = 80f;
        public bool Blind;

        public KeyRing KeyRing { get; } = new();
        public Transform Transform => transform;
        public IKeyHolder Keys => KeyRing;
        public Vector2 LookDirection => Look;
        public bool IsActive => true;

        public static TestGuard Create(Vector2 position, bool withKey, bool blind = true)
        {
            var go = new GameObject("TestGuard") { layer = LayerMask.NameToLayer("Guards") };
            go.transform.position = position;
            go.AddComponent<CircleCollider2D>().radius = 0.3f;
            var guard = go.AddComponent<TestGuard>();
            guard.Blind = blind;
            if (withKey) guard.KeyRing.AddKey(KeyIds.Cell);
            return guard;
        }

        public bool CanSee(Vector2 point)
        {
            if (Blind) return false;
            Vector2 toPoint = point - (Vector2)transform.position;
            if (toPoint.magnitude > ViewRange) return false;
            if (Vector2.Angle(Look, toPoint) > ViewAngle * 0.5f) return false;
            return !Physics2D.Linecast(transform.position, point, LayerMask.GetMask("Walls"));
        }

        private void OnEnable() => GuardRegistry.Register(this);
        private void OnDisable() => GuardRegistry.Unregister(this);
    }
}
