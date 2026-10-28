using System.Collections.Generic;
using DungeonEscape.Companion;
using DungeonEscape.Guards;
using DungeonEscape.Interaction;
using DungeonEscape.Navigation;
using NUnit.Framework;
using UnityEngine;

namespace DungeonEscape.Tests
{
    public class TargetSelectorTests
    {
        /// <summary>Yol uzunluklarini tablodan veren sahte navigator; kus ucusu mesafeyle bilerek farkli.</summary>
        private class FakeNavigator : INavigator
        {
            public readonly Dictionary<Vector2, float> PathLengths = new();

            public Vector2 Position => Vector2.zero;
            public Vector2 Velocity => Vector2.zero;
            public bool IsReady => true;
            public bool HasPath => false;
            public bool SetDestination(Vector2 target) => true;
            public bool HasArrived(float tolerance) => true;
            public void Stop() { }
            public bool Teleport(Vector2 point) => true;

            public bool TryGetPathLength(Vector2 target, out float length) =>
                PathLengths.TryGetValue(target, out length);

            public bool TrySnap(Vector2 point, float maxDistance, out Vector2 snapped)
            {
                snapped = point;
                return true;
            }
        }

        private class StubGuard : IGuard
        {
            private readonly GameObject _go;

            public StubGuard(Vector2 position, bool hasKey, bool active = true)
            {
                _go = new GameObject("StubGuard");
                _go.transform.position = position;
                IsActive = active;
                if (hasKey) KeyRing.AddKey(KeyIds.Cell);
            }

            public KeyRing KeyRing { get; } = new();
            public Transform Transform => _go.transform;
            public IKeyHolder Keys => KeyRing;
            public Vector2 LookDirection => Vector2.down;
            public bool IsActive { get; }
            public bool CanSee(Vector2 point) => false;
            public void Destroy() => Object.DestroyImmediate(_go);
        }

        private FakeNavigator _navigator;
        private NearestByPathSelector _selector;
        private readonly List<StubGuard> _guards = new();

        [SetUp]
        public void SetUp()
        {
            _navigator = new FakeNavigator();
            _selector = new NearestByPathSelector(_navigator);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var guard in _guards) guard.Destroy();
            _guards.Clear();
        }

        private StubGuard Guard(Vector2 position, float pathLength, bool hasKey = true, bool active = true)
        {
            var guard = new StubGuard(position, hasKey, active);
            _guards.Add(guard);
            if (pathLength >= 0f) _navigator.PathLengths[position] = pathLength;
            return guard;
        }

        [Test]
        public void PicksShortestPath_NotStraightLineDistance()
        {
            // Duvar arkasindaki muhafiz kus ucusu yakin ama yolu uzun
            Guard(new Vector2(1, 0), pathLength: 12f);
            var expected = Guard(new Vector2(6, 0), pathLength: 7f);

            var choice = _selector.Select(_guards, KeyIds.Cell);

            Assert.AreSame(expected, choice.Guard);
            Assert.AreEqual(7f, choice.PathLength);
        }

        [Test]
        public void IgnoresGuardsWithoutKey()
        {
            Guard(new Vector2(1, 0), pathLength: 2f, hasKey: false);
            var expected = Guard(new Vector2(5, 0), pathLength: 9f);

            Assert.AreSame(expected, _selector.Select(_guards, KeyIds.Cell).Guard);
        }

        [Test]
        public void IgnoresNeutralizedGuards()
        {
            Guard(new Vector2(1, 0), pathLength: 2f, active: false);
            var expected = Guard(new Vector2(5, 0), pathLength: 9f);

            Assert.AreSame(expected, _selector.Select(_guards, KeyIds.Cell).Guard);
        }

        [Test]
        public void IgnoresUnreachableGuards()
        {
            Guard(new Vector2(1, 0), pathLength: -1f);
            var expected = Guard(new Vector2(5, 0), pathLength: 9f);

            Assert.AreSame(expected, _selector.Select(_guards, KeyIds.Cell).Guard);
        }

        [Test]
        public void NoCandidate_ReturnsInvalidChoice()
        {
            Guard(new Vector2(1, 0), pathLength: 2f, hasKey: false);
            Assert.IsFalse(_selector.Select(_guards, KeyIds.Cell).IsValid);
        }
    }
}
