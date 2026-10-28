using System;
using UnityEngine;

namespace FogboundMaze
{
    public enum NoiseKind
    {
        Gunshot,
        Blade,
        Sprint,
        EnemyAlert
    }

    public readonly struct NoiseEvent
    {
        public NoiseEvent(Vector3 position, float radius, NoiseKind kind, UnityEngine.Object source)
        {
            Position = position;
            Radius = radius;
            Kind = kind;
            Source = source;
        }

        public Vector3 Position { get; }
        public float Radius { get; }
        public NoiseKind Kind { get; }
        public UnityEngine.Object Source { get; }
    }

    public static class NoiseSystem
    {
        public static event Action<NoiseEvent> Emitted;

        public static void Emit(Vector3 position, float radius, NoiseKind kind, UnityEngine.Object source = null)
        {
            if (radius <= 0f) return;
            Emitted?.Invoke(new NoiseEvent(position, radius, kind, source));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Emitted = null;
    }
}
