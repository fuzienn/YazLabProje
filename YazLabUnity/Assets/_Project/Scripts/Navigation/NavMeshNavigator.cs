using UnityEngine;
using UnityEngine.AI;

namespace DungeonEscape.Navigation
{
    /// <summary>
    /// NavMeshAgent'i 2D (XY duzlemi) icin ayarlayip INavigator olarak sunar.
    /// Ajan, NavMesh calisma aninda pisirilene kadar kapali tutulur ve Start'ta acilir.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class NavMeshNavigator : MonoBehaviour, INavigator
    {
        [SerializeField] private string agentTypeName = NavAreas.RatAgent;
        [SerializeField] private bool avoidRatTunnel;

        private NavMeshAgent _agent;
        private NavMeshPath _scratchPath;
        private NavMeshQueryFilter _filter;

        public Vector2 Position => transform.position;
        public Vector2 Velocity => IsReady ? (Vector2)_agent.velocity : Vector2.zero;
        public bool IsReady => _agent != null && _agent.enabled && _agent.isOnNavMesh;
        public bool HasPath => IsReady && (_agent.hasPath || _agent.pathPending);
        public float Speed
        {
            get => _agent.speed;
            set => _agent.speed = value;
        }

        public void Configure(string agentType, bool excludeRatTunnel)
        {
            agentTypeName = agentType;
            avoidRatTunnel = excludeRatTunnel;
        }

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.updateRotation = false;
            _agent.updateUpAxis = false;
            // Dondurulmus 2D yuzeyde baseOffset z eksenine kayar; sprite'lar z=0'da kalmali
            _agent.baseOffset = 0f;
            _agent.enabled = false;
            _scratchPath = new NavMeshPath();
        }

        private void Start()
        {
            _agent.agentTypeID = NavAreas.AgentTypeId(agentTypeName);
            if (avoidRatTunnel) _agent.areaMask = NavAreas.GuardAreaMask;
            _filter = new NavMeshQueryFilter { agentTypeID = _agent.agentTypeID, areaMask = _agent.areaMask };

            _agent.enabled = true;
            if (TrySnap(transform.position, 1f, out var start)) _agent.Warp(start);
            else Debug.LogWarning($"{name} NavMesh uzerinde degil ({transform.position}).");
        }

        public bool SetDestination(Vector2 target)
        {
            if (!IsReady) return false;
            _agent.isStopped = false;
            return _agent.SetDestination(target);
        }

        public bool HasArrived(float tolerance)
        {
            if (!IsReady || _agent.pathPending) return false;
            return _agent.remainingDistance <= tolerance;
        }

        public void Stop()
        {
            if (!IsReady) return;
            _agent.isStopped = true;
            _agent.ResetPath();
        }

        public bool TryGetPathLength(Vector2 target, out float length)
        {
            length = float.MaxValue;
            if (!IsReady) return false;
            // Iki uc da NavMesh'e oturtulur; aksi halde z farki yuzunden yol gecersiz sayilir
            if (!TrySnap(transform.position, 1f, out var start)) return false;
            if (!TrySnap(target, 1f, out var snapped)) return false;
            if (!NavMesh.CalculatePath(start, snapped, _filter, _scratchPath)) return false;
            if (_scratchPath.status != NavMeshPathStatus.PathComplete) return false;

            length = 0f;
            var corners = _scratchPath.corners;
            for (int i = 1; i < corners.Length; i++)
                length += Vector3.Distance(corners[i - 1], corners[i]);
            return true;
        }

        public bool TrySnap(Vector2 point, float maxDistance, out Vector2 snapped)
        {
            snapped = point;
            var filter = _agent.enabled ? _filter : new NavMeshQueryFilter
            {
                agentTypeID = NavAreas.AgentTypeId(agentTypeName),
                areaMask = avoidRatTunnel ? NavAreas.GuardAreaMask : NavMesh.AllAreas
            };
            if (!NavMesh.SamplePosition(point, out var hit, maxDistance, filter)) return false;
            snapped = hit.position;
            return true;
        }

        private void OnDrawGizmosSelected()
        {
            if (!IsReady || !_agent.hasPath) return;
            Gizmos.color = Color.cyan;
            var corners = _agent.path.corners;
            for (int i = 1; i < corners.Length; i++) Gizmos.DrawLine(corners[i - 1], corners[i]);
        }
    }
}
