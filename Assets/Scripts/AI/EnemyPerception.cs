using UnityEngine;

namespace FogboundMaze
{
    public sealed class EnemyPerception : MonoBehaviour
    {
        private const float MemoryDuration = 6f;
        private const float HearingDuration = 3.5f;
        private PlayerController player;
        private float viewDistance;
        private float fieldOfView;
        private float lastKnownTime = float.NegativeInfinity;
        private float lastHeardTime = float.NegativeInfinity;
        private NoiseKind lastNoiseKind;

        public bool CanSeePlayer { get; private set; }
        public bool HeardRecently => Time.time - lastHeardTime <= HearingDuration;
        public bool HasTargetMemory => Time.time - lastKnownTime <= MemoryDuration;
        public Vector3 LastKnownPosition { get; private set; }
        public NoiseKind LastNoiseKind => lastNoiseKind;
        public float Suspicion { get; private set; }
        public float EffectiveViewDistance { get; private set; }

        public void Initialize(PlayerController target, bool elite)
        {
            player = target;
            viewDistance = elite ? 23f : 18f;
            fieldOfView = elite ? 125f : 105f;
            ResetMemory();
        }

        public void Tick(float visionMultiplier)
        {
            EffectiveViewDistance = viewDistance * Mathf.Clamp(visionMultiplier, .35f, 1f);
            CanSeePlayer = player != null && IsVisible(player.transform.position, EffectiveViewDistance, fieldOfView);
            if (CanSeePlayer)
            {
                LastKnownPosition = player.transform.position;
                lastKnownTime = Time.time;
                Suspicion = 1f;
                return;
            }

            var memory = HasTargetMemory ? 1f - Mathf.Clamp01((Time.time - lastKnownTime) / MemoryDuration) : 0f;
            var hearing = HeardRecently ? 1f - Mathf.Clamp01((Time.time - lastHeardTime) / HearingDuration) : 0f;
            Suspicion = Mathf.Max(memory, hearing * .8f);
        }

        public void ConsumeRecentSound() => lastHeardTime = float.NegativeInfinity;

        public void ResetMemory()
        {
            CanSeePlayer = false;
            LastKnownPosition = transform.position;
            lastKnownTime = lastHeardTime = float.NegativeInfinity;
            Suspicion = 0f;
            lastNoiseKind = NoiseKind.Sprint;
        }

        public bool IsVisible(Vector3 targetPosition, float distance, float fov)
        {
            var origin = transform.position + Vector3.up;
            var target = targetPosition + Vector3.up;
            var delta = target - origin;
            var flat = new Vector3(delta.x, 0f, delta.z);
            if (flat.sqrMagnitude > distance * distance || flat.sqrMagnitude < .001f) return false;
            if (Vector3.Angle(transform.forward, flat) > fov * .5f) return false;
            return !Physics.Linecast(origin, target, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }

        private void OnEnable() => NoiseSystem.Emitted += OnNoise;
        private void OnDisable() => NoiseSystem.Emitted -= OnNoise;

        private void OnNoise(NoiseEvent noise)
        {
            if (player == null) return;
            if (noise.Source is Component component && component.gameObject == gameObject) return;
            if (noise.Source is GameObject sourceObject && sourceObject == gameObject) return;
            if ((noise.Position - transform.position).sqrMagnitude > noise.Radius * noise.Radius) return;
            LastKnownPosition = noise.Position;
            lastKnownTime = lastHeardTime = Time.time;
            lastNoiseKind = noise.Kind;
            Suspicion = noise.Kind == NoiseKind.EnemyAlert ? .9f : .75f;
        }

        private void OnDrawGizmosSelected()
        {
            var distance = EffectiveViewDistance > 0f ? EffectiveViewDistance : viewDistance;
            Gizmos.color = CanSeePlayer ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, distance);
            var left = Quaternion.Euler(0f, -fieldOfView * .5f, 0f) * transform.forward;
            var right = Quaternion.Euler(0f, fieldOfView * .5f, 0f) * transform.forward;
            Gizmos.DrawRay(transform.position + Vector3.up, left * distance);
            Gizmos.DrawRay(transform.position + Vector3.up, right * distance);
            if (HasTargetMemory)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position + Vector3.up, LastKnownPosition + Vector3.up);
            }
        }
    }
}
