using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FogboundMaze
{
    [RequireComponent(typeof(CharacterController), typeof(Health))]
    public sealed class EnemyAgent : MonoBehaviour
    {
        private CharacterController controller;
        private Health health;
        private PlayerController player;
        private MazeWorld world;
        private EnemyPerception perception;
        private List<Vector2Int> path = new();
        private int waypoint;
        private float nextPathTime;
        private float nextAttack;
        private float nextAlert;
        private float nextSearchPoint;
        private float speed;
        private float damage;
        private bool elite;
        private bool hadDirectSight;
        private bool hasPatrolTarget;
        private Vector3 patrolTarget;
        private Vector3 searchTarget;
        private Animation animationPlayer;
        private MaterialPropertyBlock flashProperties;
        private float staggerUntil;
        private Coroutine hitFlash;
        private System.Random random;

        public EnemyState State { get; private set; } = EnemyState.Inactive;
        public bool IsActive => gameObject.activeSelf && State != EnemyState.Dead;
        public bool CanSeePlayer => perception != null && perception.CanSeePlayer;
        public bool HeardRecently => perception != null && perception.HeardRecently;
        public bool HasTargetMemory => perception != null && perception.HasTargetMemory;
        public Vector3 LastKnownPosition => perception != null ? perception.LastKnownPosition : transform.position;
        public float Suspicion => perception != null ? perception.Suspicion : 0f;
        public EnemyDecisionResult LastDecision { get; private set; }

        private void Awake()
        {
            flashProperties = new MaterialPropertyBlock();
            controller = GetComponent<CharacterController>();
            health = GetComponent<Health>();
            perception = gameObject.AddComponent<EnemyPerception>();
            health.Died += OnDied;
        }

        public void Spawn(PlayerController target, MazeWorld mazeWorld, Vector3 position, bool isElite)
        {
            StopAllCoroutines();
            player = target;
            world = mazeWorld;
            elite = isElite;
            speed = elite ? 3.35f : 2.65f;
            damage = elite ? 22f : 13f;
            controller.enabled = false;
            transform.SetPositionAndRotation(position + Vector3.up * .05f, Quaternion.identity);
            transform.localScale = elite ? Vector3.one * 1.2f : Vector3.one;
            controller.enabled = true;
            health.ResetHealth(elite ? 125f : 68f);
            perception.Initialize(player, elite);
            gameObject.SetActive(true);
            State = EnemyState.Patrol;
            path.Clear();
            waypoint = 0;
            nextPathTime = nextAttack = nextAlert = nextSearchPoint = 0f;
            staggerUntil = 0f;
            hitFlash = null;
            hadDirectSight = hasPatrolTarget = false;
            random = new System.Random(GetInstanceID() ^ Mathf.RoundToInt(position.x * 31f + position.z * 17f));
            var normal = transform.Find("Normal Visual");
            var heavy = transform.Find("Elite Visual");
            if (normal != null) normal.gameObject.SetActive(!elite);
            if (heavy != null) heavy.gameObject.SetActive(elite);
            animationPlayer = GetComponentInChildren<Animation>();
            SetFlash(false);
            Play("Idle");
        }

        private void Update()
        {
            if (State is EnemyState.Inactive or EnemyState.Dead || player == null || world == null
                || GameDirector.Instance?.Phase != GamePhase.Playing)
                return;
            if (Time.time < staggerUntil) return;

            perception.Tick(GameDirector.Instance.EnemyVisionMultiplierAt(transform.position));
            if (perception.CanSeePlayer && !hadDirectSight && Time.time >= nextAlert)
            {
                nextAlert = Time.time + 4f;
                NoiseSystem.Emit(player.transform.position, elite ? 16f : 12f, NoiseKind.EnemyAlert, this);
            }
            hadDirectSight = perception.CanSeePlayer;

            var distance = Vector3.Distance(transform.position, player.transform.position);
            LastDecision = EnemyDecisionModel.Evaluate(new EnemyDecisionContext(
                perception.CanSeePlayer, perception.HeardRecently, perception.HasTargetMemory, distance));
            State = LastDecision.State;

            switch (State)
            {
                case EnemyState.Attack:
                    AttackPlayer();
                    return;
                case EnemyState.Chase:
                    MoveToward(player.transform.position, true);
                    return;
                case EnemyState.Investigate:
                    MoveToward(perception.LastKnownPosition, false);
                    if (HorizontalDistance(transform.position, perception.LastKnownPosition) < .65f)
                    {
                        perception.ConsumeRecentSound();
                        searchTarget = ChooseNearbySearchPoint();
                        nextSearchPoint = Time.time + 1.2f;
                    }
                    return;
                case EnemyState.Search:
                    if (Time.time >= nextSearchPoint || HorizontalDistance(transform.position, searchTarget) < .65f)
                    {
                        searchTarget = ChooseNearbySearchPoint();
                        nextSearchPoint = Time.time + 1.2f;
                    }
                    MoveToward(searchTarget, false);
                    return;
                default:
                    Patrol();
                    return;
            }
        }

        private void AttackPlayer()
        {
            Play("Idle_Attack", true);
            if (Time.time < nextAttack) return;
            nextAttack = Time.time + (elite ? .78f : 1.05f);
            if (!Physics.Linecast(transform.position + Vector3.up,
                player.transform.position + Vector3.up, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                player.Health.Damage(damage);
        }

        private void Patrol()
        {
            if (!hasPatrolTarget || HorizontalDistance(transform.position, patrolTarget) < .65f)
            {
                patrolTarget = ChooseRandomCell();
                hasPatrolTarget = true;
            }
            MoveToward(patrolTarget, false);
        }

        private Vector3 ChooseNearbySearchPoint()
        {
            var center = world.WorldToCell(perception.LastKnownPosition);
            var options = new List<Vector2Int> { center };
            foreach (var neighbor in world.Layout.GetOpenNeighbors(center)) options.Add(neighbor);
            return world.CellToWorld(options[random.Next(options.Count)]);
        }

        private Vector3 ChooseRandomCell()
        {
            var cell = new Vector2Int(random.Next(world.Layout.Width), random.Next(world.Layout.Height));
            return world.CellToWorld(cell);
        }

        private void MoveToward(Vector3 destination, bool running)
        {
            if (Time.time >= nextPathTime)
            {
                nextPathTime = Time.time + (elite ? .38f : .58f);
                path = world.FindPath(transform.position, destination);
                waypoint = Mathf.Min(1, path.Count - 1);
            }
            if (path.Count == 0 || waypoint < 0 || waypoint >= path.Count)
            {
                Play("Idle");
                return;
            }

            var target = world.CellToWorld(path[waypoint]);
            var delta = target - transform.position;
            delta.y = 0f;
            if (delta.magnitude < .35f && waypoint < path.Count - 1)
            {
                waypoint++;
                return;
            }
            var direction = delta.normalized;
            controller.SimpleMove(direction * speed * (running ? 1f : .78f));
            if (direction.sqrMagnitude > .01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 10f * Time.deltaTime);
            Play(running ? "Run" : "Walk");
        }

        public void TakeDamage(float amount)
        {
            health.Damage(amount);
            if (!health.IsDead)
            {
                staggerUntil = Time.time + (elite ? .045f : .08f);
                if (hitFlash != null) StopCoroutine(hitFlash);
                hitFlash = StartCoroutine(HitFlash());
            }
        }

        private void OnDied(Health value)
        {
            State = EnemyState.Dead;
            controller.enabled = false;
            Play("Death", true);
            GameDirector.Instance?.RegisterKill(elite);
            StartCoroutine(ReturnAfterDeath());
        }

        private IEnumerator ReturnAfterDeath()
        {
            yield return new WaitForSeconds(1.1f);
            State = EnemyState.Inactive;
            perception.ResetMemory();
            gameObject.SetActive(false);
        }

        private IEnumerator HitFlash()
        {
            SetFlash(true);
            yield return new WaitForSeconds(.14f);
            SetFlash(false);
        }

        private void Play(string clip, bool restart = false)
        {
            if (animationPlayer == null || animationPlayer[clip] == null) return;
            if (restart) animationPlayer.Stop();
            if (!animationPlayer.IsPlaying(clip)) animationPlayer.CrossFade(clip, .08f);
        }

        private void SetFlash(bool enabled)
        {
            flashProperties.Clear();
            if (enabled)
            {
                flashProperties.SetColor("_BaseColor", new Color(1.7f, 1.7f, 1.7f));
                flashProperties.SetColor("_Color", new Color(1.7f, 1.7f, 1.7f));
            }
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
                renderer.SetPropertyBlock(flashProperties);
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
