using System.Collections.Generic;
using UnityEngine;

namespace FogboundMaze
{
    public sealed class GameDirector : MonoBehaviour
    {
        public static GameDirector Instance { get; private set; }

        private IReadOnlyList<LevelDefinition> levels;
        private LevelDefinition level;
        private MazeWorld world;
        private PlayerController player;
        private GameInput input;
        private CameraRig cameraRig;
        private EnemyPool pool;
        private EnvironmentController environment;
        private float nextSpawn;
        private float elapsed;
        private int kills;
        private int pendingSpawns;
        private int currentLevel;
        private GamePhase phaseBeforePause;
        private System.Random random;
        private int runGeneration;
        private bool aimSlowMotion;

        public GamePhase Phase { get; private set; }
        public GameHud Hud { get; private set; }
        public CampaignProgress Progress { get; private set; }
        public LevelDefinition CurrentLevel => level;
        public PlayerController Player => player;
        public MazeWorld World => world;
        public GameInput Input => input;
        public CameraRig CameraRig => cameraRig;
        public int Kills => kills;
        public float Elapsed => elapsed;
        public bool IsPlayerSafe => !level.miasma || world.IsWithinSafeLight(player.transform.position, level.safeLightRadius);
        public int ActiveEnemyCount => pool.ActiveCount;
        public int EnemyPoolCapacity => pool.Capacity;
        public bool LoadoutConfirmed { get; private set; }
        public float EnemyVisionMultiplier => environment != null ? environment.EnemyVisionMultiplier : 1f;
        public float EnemyVisionMultiplierAt(Vector3 position) => EnemyVisionMultiplier * world.LocalVisibilityMultiplier(position);
        public bool IsPlayerInFog => world != null && player != null && world.IsInsideFogZone(player.transform.position);
        public bool IsAimSlowMotion => aimSlowMotion && Phase == GamePhase.Playing;

        private void Awake()
        {
            Instance = this;
            levels = LevelCatalog.CreateDefault();
            var smoke = System.Array.Exists(System.Environment.GetCommandLineArgs(), value => value == "-fogboundSmoke");
            if (smoke) Application.runInBackground = true;
            Progress = new CampaignProgress(smoke);
            BuildPersistentSystems();
            CombatEffects.Ensure();
            LoadLevel(1);
            ShowTitle();
            if (smoke)
            {
                gameObject.AddComponent<RuntimeSmokeCapture>();
            }
        }

        private void Update()
        {
            if (PortfolioSettings.IsOpen) return;
            if (input.PausePressed && (Phase == GamePhase.LevelSelect || Phase == GamePhase.Guide)) ShowTitle();
            if (Phase == GamePhase.Staging)
            {
                if (LoadoutConfirmed
                    && player.transform.position.z >= world.EntryPosition.z - 1.25f
                    && world.IsInside(player.transform.position))
                    BeginRun();
                Hud.Refresh(this, player);
            }

            if (input.PausePressed && (Phase is GamePhase.Playing or GamePhase.Paused
                || (Phase == GamePhase.Staging && LoadoutConfirmed)))
            {
                SetPaused(Phase != GamePhase.Paused);
            }

            if (Phase != GamePhase.Playing)
            {
                return;
            }

            elapsed += Time.deltaTime;
            if (Time.time >= nextSpawn && pool.ActiveCount + pendingSpawns < level.maxEnemies)
            {
                nextSpawn = Time.time + level.spawnInterval;
                BeginEnemySpawn();
            }

            if (level.miasma && world.IsInside(player.transform.position)
                              && !world.IsWithinSafeLight(player.transform.position, level.safeLightRadius))
            {
                player.Health.Damage(level.miasmaDamagePerSecond * Time.deltaTime);
            }

            Hud.Refresh(this, player);
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
            {
                SetCursorLocked(false);
                return;
            }
            if (focused && player != null && player.Weapon != null
                && LoadoutConfirmed
                && Phase is GamePhase.Staging or GamePhase.Playing)
                SetCursorLocked(true);
        }

        public void SelectWeapon(WeaponType type)
        {
            if (Phase != GamePhase.Staging) return;
            player.Weapon.Equip(type);
            ConfirmLoadout();
        }

        public void SetAimSlowMotion(bool active)
        {
            aimSlowMotion = active && Phase == GamePhase.Playing;
            if (Phase != GamePhase.Paused && !PortfolioSettings.IsOpen)
                Time.timeScale = aimSlowMotion ? 0.5f : 1f;
        }

        public void ConfirmLoadout()
        {
            if (Phase != GamePhase.Staging || !player.Weapon.HasLoadout || LoadoutConfirmed) return;
            LoadoutConfirmed = true;
            world.OpenStartGate();
            Hud.HideWeaponSelection();
            Hud.ShowEntryPrompt();
            Hud.Refresh(this, player);
            SetCursorLocked(true);
        }

        public void BeginRun()
        {
            if (Phase != GamePhase.Staging || !LoadoutConfirmed) return;
            Phase = GamePhase.Playing;
            nextSpawn = Time.time + 1.2f;
            Hud.SetPhase(Phase, currentLevel);
            Hud.HideEntryPrompt();
        }

        public void CompleteLevel()
        {
            if (Phase != GamePhase.Playing) return;
            Phase = GamePhase.Won;
            player.Weapon.ExitAim();
            SetAimSlowMotion(false);
            Progress.Complete(currentLevel);
            Hud.Refresh(this, player);
            Hud.ShowResult(true, currentLevel, elapsed, kills);
            SetCursorLocked(false);
        }

        public void RegisterKill(bool elite)
        {
            kills += elite ? 2 : 1;
        }

        public void Retry()
        {
            if (Phase is GamePhase.Title or GamePhase.LevelSelect or GamePhase.Guide) return;
            LoadLevel(currentLevel);
        }

        public void NextLevel()
        {
            if (Phase == GamePhase.Won && currentLevel < 10 && Progress.IsUnlocked(currentLevel + 1))
                LoadLevel(currentLevel + 1);
        }

        public void ReturnToLoadout() => Retry();

        public bool SelectLevel(int number)
        {
            if (Phase != GamePhase.LevelSelect || !Progress.IsUnlocked(number)) return false;
            LoadLevel(number);
            return true;
        }

        public void ShowTitle() => ShowMenu(GamePhase.Title);
        public void ShowGuide() => ShowMenu(GamePhase.Guide);
        public void ShowLevelSelection() => ShowMenu(GamePhase.LevelSelect);

        private void ShowMenu(GamePhase phase)
        {
            CombatEffects.Clear();
            Time.timeScale = 1f;
            aimSlowMotion = false;
            runGeneration++;
            pool.DespawnAll();
            pendingSpawns = 0;
            input.ResetMobile();
            LoadoutConfirmed = false;
            player.Weapon.ClearLoadout();
            player.Teleport(world.StartPosition + Vector3.up * 0.08f);
            player.Health.ResetHealth(100f);
            player.transform.rotation = Quaternion.identity;
            world.OpenStartGate();
            Phase = phase;
            cameraRig.ShowMenuView(world.StartPosition);
            Hud.ShowCampaignMenu(phase, Progress);
            SetCursorLocked(false);
        }

        public void QuitGame()
        {
            Time.timeScale = 1f;
            SetCursorLocked(false);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void SetPaused(bool paused)
        {
            if (paused && Phase != GamePhase.Playing
                && !(Phase == GamePhase.Staging && LoadoutConfirmed)) return;
            if (!paused && Phase != GamePhase.Paused) return;
            input.ResetMobile();
            if (paused) phaseBeforePause = Phase;
            Phase = paused ? GamePhase.Paused : phaseBeforePause;
            Time.timeScale = paused ? 0f : aimSlowMotion && phaseBeforePause == GamePhase.Playing ? 0.5f : 1f;
            Hud.SetPause(paused);
            SetCursorLocked(!paused);
        }

        private void LoadLevel(int number)
        {
            CombatEffects.Clear();
            Time.timeScale = 1f;
            aimSlowMotion = false;
            runGeneration++;
            input.ResetMobile();
            currentLevel = number;
            level = levels[number - 1];
            random = new System.Random(level.seed + 97);
            pool.DespawnAll();
            pool.Prewarm(level.maxEnemies);
            pendingSpawns = 0;
            world.Build(level);
            player.Teleport(world.StartPosition + Vector3.up * 0.08f);
            player.transform.rotation = Quaternion.identity;
            cameraRig.ResetView();
            player.Health.ResetHealth(100f);
            LoadoutConfirmed = false;
            player.Weapon.PrepareLoadout();
            environment.Apply(level);
            elapsed = 0f;
            kills = 0;
            Phase = GamePhase.Staging;
            Hud.ShowWeaponSelection(number);
            Hud.ResetMap(world, player, cameraRig);
            ConfirmLoadout();
        }

        private void BuildPersistentSystems()
        {
            input = new GameObject("Game Input").AddComponent<GameInput>();
            world = new GameObject("Maze World").AddComponent<MazeWorld>();
            pool = new GameObject("Enemy Pool").AddComponent<EnemyPool>();
            environment = new GameObject("Environment").AddComponent<EnvironmentController>();
            player = CreatePlayer();
            var cameraObject = new GameObject("Player Camera");
            cameraRig = cameraObject.AddComponent<CameraRig>();
            cameraRig.Initialize(player.transform);
            player.Initialize(input, cameraRig);
            player.Health.Died += _ => Lose();
            Hud = GameHud.Create(input);
            Hud.BindHealth(player.Health);
        }

        private PlayerController CreatePlayer()
        {
            var root = new GameObject("Player");
            root.layer = LayerMask.NameToLayer("Ignore Raycast");
            var controller = root.AddComponent<CharacterController>();
            controller.height = 1.85f;
            controller.radius = 0.34f;
            controller.center = Vector3.up * 0.93f;
            root.AddComponent<Health>();
            var visual = Instantiate(Resources.Load<GameObject>("Characters/Survivor"), root.transform);
            visual.name = "Visual";
            foreach (var child in visual.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = root.layer;
            var playerController = root.AddComponent<PlayerController>();
            root.AddComponent<PlayerVisual>();
            return playerController;
        }

        private void BeginEnemySpawn()
        {
            for (var attempt = 0; attempt < 12; attempt++)
            {
                var cell = new Vector2Int(random.Next(level.width), random.Next(level.height));
                var position = world.CellToWorld(cell);
                if (cell == world.Layout.Start || cell == world.Layout.Goal || Vector3.Distance(position, player.transform.position) < 12f)
                    continue;
                var elite = random.NextDouble() < level.eliteChance;
                pendingSpawns++;
                var generation = runGeneration;
                SpawnTelegraph.Create(position, elite, () =>
                {
                    if (generation != runGeneration) return;
                    pendingSpawns = Mathf.Max(0, pendingSpawns - 1);
                    if (Phase == GamePhase.Playing)
                    {
                        pool.Spawn(player, world, position, elite);
                    }
                });
                return;
            }
        }

        private void Lose()
        {
            if (Phase is GamePhase.Lost or GamePhase.Won) return;
            Phase = GamePhase.Lost;
            player.Weapon.ExitAim();
            SetAimSlowMotion(false);
            Hud.Refresh(this, player);
            Hud.ShowResult(false, currentLevel, elapsed, kills);
            SetCursorLocked(false);
        }

        private static void SetCursorLocked(bool locked)
        {
            var desktopLock = locked && !Application.isMobilePlatform;
            Cursor.lockState = desktopLock ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !desktopLock;
        }
    }
}
