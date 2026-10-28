using UnityEngine;

namespace FogboundMaze
{
    public readonly struct EnemyDecisionContext
    {
        public EnemyDecisionContext(bool visible, bool heard, bool hasMemory, float distance)
        {
            Visible = visible;
            Heard = heard;
            HasMemory = hasMemory;
            Distance = distance;
        }

        public bool Visible { get; }
        public bool Heard { get; }
        public bool HasMemory { get; }
        public float Distance { get; }
    }

    public readonly struct EnemyDecisionResult
    {
        public EnemyDecisionResult(EnemyState state, float patrol, float investigate, float chase, float search, float attack)
        {
            State = state;
            PatrolScore = patrol;
            InvestigateScore = investigate;
            ChaseScore = chase;
            SearchScore = search;
            AttackScore = attack;
        }

        public EnemyState State { get; }
        public float PatrolScore { get; }
        public float InvestigateScore { get; }
        public float ChaseScore { get; }
        public float SearchScore { get; }
        public float AttackScore { get; }
    }

    public static class EnemyDecisionModel
    {
        public static EnemyDecisionResult Evaluate(EnemyDecisionContext context)
        {
            const float patrol = 10f;
            var investigate = context.Heard && !context.Visible ? 58f : 0f;
            var chase = context.Visible && context.Distance > 1.45f
                ? 78f + Mathf.Clamp(18f - context.Distance, 0f, 18f) : 0f;
            var search = !context.Visible && context.HasMemory && !context.Heard ? 46f : 0f;
            var attack = context.Visible && context.Distance <= 1.45f ? 100f : 0f;
            var state = EnemyState.Patrol;
            var best = patrol;
            if (investigate > best) { best = investigate; state = EnemyState.Investigate; }
            if (search > best) { best = search; state = EnemyState.Search; }
            if (chase > best) { best = chase; state = EnemyState.Chase; }
            if (attack > best) state = EnemyState.Attack;
            return new EnemyDecisionResult(state, patrol, investigate, chase, search, attack);
        }
    }
}
