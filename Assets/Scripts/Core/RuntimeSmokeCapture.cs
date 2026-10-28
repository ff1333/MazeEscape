using System.Collections;
using System.IO;
using UnityEngine;

namespace FogboundMaze
{
    public sealed class RuntimeSmokeCapture : MonoBehaviour
    {
        private IEnumerator Start()
        {
            var arguments = System.Environment.GetCommandLineArgs();
            var output = ReadArgument(arguments, "-fogboundScreenshot");
            var gameplay = System.Array.Exists(arguments, value => value == "-fogboundGameplay");
            var firstPerson = System.Array.Exists(arguments, value => value == "-fogboundFirstPerson");
            var machete = System.Array.Exists(arguments, value => value == "-fogboundMachete");
            var pause = System.Array.Exists(arguments, value => value == "-fogboundPause");
            var mapTrail = System.Array.Exists(arguments, value => value == "-fogboundMapTrail");
            var walkRoute = System.Array.Exists(arguments, value => value == "-fogboundWalkRoute");
            var healthCheck = System.Array.Exists(arguments, value => value == "-fogboundHealthCheck");
            var fatalHit = System.Array.Exists(arguments, value => value == "-fogboundFatalHit");
            var levels = System.Array.Exists(arguments, value => value == "-fogboundLevels");
            var loadout = System.Array.Exists(arguments, value => value == "-fogboundLoadout");
            var actors = System.Array.Exists(arguments, value => value == "-fogboundActors");
            var guide = System.Array.Exists(arguments, value => value == "-fogboundGuide");
            var combat = System.Array.Exists(arguments, value => value == "-fogboundCombat");
            var reload = System.Array.Exists(arguments, value => value == "-fogboundReload");
            var scope = System.Array.Exists(arguments, value => value == "-fogboundScope");
            var fogZone = System.Array.Exists(arguments, value => value == "-fogboundFogZone");
            var levelNumber = int.TryParse(ReadArgument(arguments, "-fogboundLevel"), out var requestedLevel)
                ? Mathf.Clamp(requestedLevel, 1, 10) : 1;
            yield return new WaitForSecondsRealtime(1f);
            var previousLanguage=PortfolioSettings.Chinese;
            PortfolioSettings.SetLanguage(!System.Array.Exists(arguments,value=>value=="-english"));
            yield return null;
            if(System.Array.Exists(arguments,value=>value=="-settings")) { PortfolioSettingsMenu.Instance.Open();yield return null; }
            if (Application.version != ReleaseVersion.Current)
            {
                Debug.LogError($"FOGBOUND_VERSION_FAIL expected={ReleaseVersion.Current} actual={Application.version}");
                Application.Quit(2);
                yield break;
            }
            Debug.Log($"FOGBOUND_VERSION_PASS {Application.version}");
            if (System.Array.Exists(arguments, value => value == "-fogboundPortfolioVideo"))
            {
                yield return RunPortfolioShot(ReadArgument(arguments,"-fogboundPortfolioShot"),levelNumber);
                Debug.Log("FOGBOUND_PORTFOLIO_SHOT_PASS " + ReadArgument(arguments,"-fogboundPortfolioShot"));
                PortfolioSettings.SetLanguage(previousLanguage);
                Application.Quit(0);
                yield break;
            }
            if (guide)
            {
                GameDirector.Instance.ShowGuide();
                GameDirector.Instance.Hud.transform.Find("Field Guide/Chapter 7")
                    .GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            }

            if (levels || loadout || pause || gameplay || actors)
            {
                GameDirector.Instance.ShowLevelSelection();
                if (!levels)
                {
                    for (var completed = 1; completed < levelNumber; completed++)
                        GameDirector.Instance.Progress.Complete(completed);
                    GameDirector.Instance.SelectLevel(levelNumber);
                }
                yield return null;
            }

            if (actors)
            {
                var director = GameDirector.Instance;
                director.SelectWeapon(machete ? WeaponType.LongBlade : WeaponType.SniperRifle);
                var pool = FindFirstObjectByType<EnemyPool>();
                foreach (var elite in new[] { false, true })
                {
                    var enemy = pool.Spawn(director.Player, director.World,
                        director.World.EntryPosition + new Vector3(elite ? 1.1f : -1.1f, 0f, elite ? 0.8f : 0f), elite);
                    enemy.transform.LookAt(director.Player.transform.position);
                    enemy.enabled = false;
                }
                yield return new WaitForSecondsRealtime(0.5f);
            }

            if (pause)
            {
                var director = GameDirector.Instance;
                director.SelectWeapon(WeaponType.SniperRifle);
                director.SetPaused(true);
                yield return new WaitForSecondsRealtime(0.5f);
            }
            else if (gameplay)
            {
                var director = GameDirector.Instance;
                director.SelectWeapon(machete ? WeaponType.LongBlade : WeaponType.SniperRifle);
                if (!walkRoute)
                    director.Player.GetComponent<CharacterController>().Move(Vector3.forward * 3.5f);
                yield return null;
                if (firstPerson)
                    director.CameraRig.TickLook(Vector2.zero, true);
                if (scope)
                {
                    director.Player.Weapon.Equip(WeaponType.SniperRifle);
                    director.Input.SetMobileAim(true);
                }
                if (walkRoute)
                {
                    var controller = director.Player.GetComponent<CharacterController>();
                    var path = MazePathfinder.FindPath(director.World.Layout,
                        director.World.Layout.Start, director.World.Layout.Goal);
                    var count = Mathf.Min(6, path.Count);
                    for (var i = 0; i < count; i++)
                    {
                        var destination = director.World.CellToWorld(path[i]);
                        for (var step = 0; step < 120; step++)
                        {
                            var delta = destination - director.Player.transform.position;
                            delta.y = 0f;
                            if (delta.magnitude < 0.1f) break;
                            var yaw = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
                            director.CameraRig.TickLook(new Vector2(Mathf.DeltaAngle(director.CameraRig.Yaw, yaw), 0f), false);
                            controller.Move(Vector3.ClampMagnitude(delta, 0.18f));
                            yield return null;
                        }
                        var remaining = destination - director.Player.transform.position;
                        remaining.y = 0f;
                        if (remaining.magnitude >= 0.1f || director.Player.transform.position.y < -0.2f)
                        {
                            Debug.LogError($"FOGBOUND_WALK_FAIL cell={path[i]} position={director.Player.transform.position}");
                            Application.Quit(2);
                            yield break;
                        }
                    }
                    Debug.Log($"FOGBOUND_WALK_PASS cells={count} position={director.Player.transform.position}");
                }
                if (mapTrail)
                {
                    var path = MazePathfinder.FindPath(director.World.Layout,
                        director.World.Layout.Start, director.World.Layout.Goal);
                    for (var i = 0; i < Mathf.Min(5, path.Count); i++)
                    {
                        director.Player.Teleport(director.World.CellToWorld(path[i]) + Vector3.up * 0.2f);
                        yield return null;
                        yield return null;
                    }
                }
                if (fogZone && director.World.FogZones.Count > 0)
                {
                    director.Player.Teleport(director.World.FogZones[0] + Vector3.up * .2f);
                    yield return null;
                    yield return new WaitForSecondsRealtime(1.5f);
                }
                yield return new WaitForSecondsRealtime(2.5f);
            }

            if (healthCheck || fatalHit)
            {
                var director = GameDirector.Instance;
                director.Player.Health.Damage(fatalHit ? 200f : 65f);
                Canvas.ForceUpdateCanvases();
                var top = director.Hud.transform.Find("Top Bar");
                var text = top.Find("Health").GetComponent<UnityEngine.UI.Text>().text;
                var track = top.Find("Health Track").GetComponent<RectTransform>();
                var fill = track.Find("Health Fill").GetComponent<RectTransform>();
                var ratio = fill.rect.width / track.rect.width;
                var expected = fatalHit ? 0f : 0.35f;
                if (Mathf.Abs(ratio - expected) > 0.005f
                    || text != (fatalHit ? "HP  0 / 100" : "HP  35 / 100")
                    || (fatalHit && director.Phase != GamePhase.Lost))
                {
                    Debug.LogError($"FOGBOUND_HEALTH_FAIL text={text} ratio={ratio} phase={director.Phase}");
                    Application.Quit(2);
                    yield break;
                }
                Debug.Log($"FOGBOUND_HEALTH_PASS text={text} ratio={ratio} phase={director.Phase}");
                yield return null;
            }

            if (!string.IsNullOrWhiteSpace(output))
            {
                if (reload)
                {
                    var weapon = GameDirector.Instance.Player.Weapon;
                    weapon.Equip(WeaponType.SniperRifle);
                    weapon.Tick(false, true, false);
                    yield return new WaitForSeconds(WeaponController.BoltCycleDuration + .05f);
                    weapon.Tick(false, false, true);
                    yield return new WaitForSeconds(WeaponController.ReloadDuration + .05f);
                    if (weapon.IsReloading || !weapon.ReloadReady || weapon.Ammunition != 5
                        || weapon.ReserveAmmunition != 19)
                    {
                        Debug.LogError("FOGBOUND_RELOAD_FAIL");
                        Application.Quit(2);
                        yield break;
                    }
                    Debug.Log("FOGBOUND_RELOAD_PASS ammo=5 reserve=19 ready=true");
                    Time.timeScale = 0f;
                    yield return null;
                }
                if (combat)
                {
                    GameDirector.Instance.Player.Weapon.Tick(true,true,false,false);
                    if (machete) yield return new WaitForSeconds(.15f);
                    Time.timeScale = 0f;
                    yield return null;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                CaptureFrame(output);
                if (!File.Exists(output))
                {
                    Debug.LogError("FOGBOUND_SCREENSHOT_FAIL " + output);
                    Application.Quit(2);
                    yield break;
                }
            }

            Debug.Log($"FOGBOUND_RUNTIME_SMOKE_PASS mode={(pause ? "pause" : gameplay ? "gameplay" : "staging")} screenshot={output}");
            PortfolioSettings.SetLanguage(previousLanguage);
            Application.Quit(0);
        }

        private static string ReadArgument(string[] arguments, string name)
        {
            for (var i = 0; i < arguments.Length - 1; i++)
            {
                if (arguments[i] == name) return arguments[i + 1];
            }
            return string.Empty;
        }

        private IEnumerator RunPortfolioShot(string shot,int levelNumber)
        {
            var director = GameDirector.Instance;
            switch (shot)
            {
                case "settings":
                    PortfolioSettingsMenu.Instance.Open();
                    yield return new WaitForSecondsRealtime(10f);
                    yield break;
                case "guide":
                    director.ShowGuide();
                    yield return new WaitForSecondsRealtime(12f);
                    yield break;
                case "levels":
                    for (var completed = 1; completed < 6; completed++)
                        director.Progress.Complete(completed);
                    director.ShowLevelSelection();
                    yield return new WaitForSecondsRealtime(12f);
                    yield break;
                case "loadout":
                    director.ShowLevelSelection();
                    director.SelectLevel(1);
                    yield return new WaitForSecondsRealtime(12f);
                    yield break;
                case "blade":
                    yield return RunBladeCombat();
                    yield break;
                case "view":
                    yield return PrepareLevel(Mathf.Max(1,levelNumber),WeaponType.SniperRifle);
                    director.CameraRig.TickLook(Vector2.zero,true);
                    yield return WalkMazePath(7,false);
                    yield return new WaitForSecondsRealtime(3f);
                    director.CameraRig.TickLook(Vector2.zero,true);
                    yield return WalkMazePath(4,false);
                    yield break;
                case "explore":
                    yield return PrepareLevel(Mathf.Max(4,levelNumber),WeaponType.SniperRifle);
                    yield return WalkMazePath(12,true);
                    yield return new WaitForSecondsRealtime(4f);
                    yield break;
                case "hazards":
                    yield return PrepareLevel(Mathf.Max(8,levelNumber),WeaponType.SniperRifle);
                    yield return WalkMazePath(14,false);
                    yield return new WaitForSecondsRealtime(4f);
                    yield break;
                case "gun":
                    yield return RunGunCombat();
                    yield break;
                default:
                    yield return new WaitForSecondsRealtime(10f);
                    yield break;
            }
        }

        private static IEnumerator PrepareLevel(int levelNumber,WeaponType weapon)
        {
            var director = GameDirector.Instance;
            director.ShowLevelSelection();
            for (var completed = 1; completed < levelNumber; completed++)
                director.Progress.Complete(completed);
            director.SelectLevel(levelNumber);
            yield return null;
            director.SelectWeapon(weapon);
            yield return null;
        }

        private static IEnumerator WalkMazePath(int cells,bool spawnEnemy)
        {
            var director = GameDirector.Instance;
            var path = MazePathfinder.FindPath(director.World.Layout,
                director.World.Layout.Start,director.World.Layout.Goal);
            var controller = director.Player.GetComponent<CharacterController>();
            yield return MoveTo(controller,director.World.EntryPosition,4.5f);
            var count = Mathf.Min(cells,path.Count);
            EnemyAgent actor = null;
            for (var i = 0; i < count; i++)
            {
                if (spawnEnemy && i == 4 && actor == null)
                {
                    var spawnCell = path[Mathf.Min(i + 3,path.Count - 1)];
                    actor = FindFirstObjectByType<EnemyPool>().Spawn(
                        director.Player,director.World,director.World.CellToWorld(spawnCell),false);
                }
                yield return MoveTo(controller,director.World.CellToWorld(path[i]),4.2f);
                if (actor != null && actor.IsActive)
                {
                    AimAt(actor.transform.position);
                    director.Player.Weapon.Tick(true,true,false);
                }
            }
        }

        private static IEnumerator RunGunCombat()
        {
            yield return PrepareLevel(1,WeaponType.SniperRifle);
            var director = GameDirector.Instance;
            var path = MazePathfinder.FindPath(director.World.Layout,
                director.World.Layout.Start,director.World.Layout.Goal);
            var controller = director.Player.GetComponent<CharacterController>();
            yield return MoveTo(controller,director.World.EntryPosition,4.5f);
            yield return MoveTo(controller,director.World.CellToWorld(path[Mathf.Min(2,path.Count - 1)]),4.2f);
            director.CameraRig.TickLook(Vector2.zero,true);
            yield return new WaitForSecondsRealtime(.5f);

            var pool = FindFirstObjectByType<EnemyPool>();
            EnemyAgent enemy = null;
            var elapsed = 0f;
            var nextSpawn = 0f;
            var spawned = 0;
            var initialKills = director.Kills;
            var sawReload = false;
            while (elapsed < 25f && !director.Player.Health.IsDead)
            {
                elapsed += Time.deltaTime;
                if (enemy != null && !enemy.IsActive)
                {
                    enemy = null;
                    nextSpawn = elapsed + 2.2f;
                }
                if (enemy == null && spawned < 8 && elapsed >= nextSpawn)
                {
                    var forward = Quaternion.Euler(0f,director.CameraRig.Yaw,0f) * Vector3.forward;
                    var right = Quaternion.Euler(0f,director.CameraRig.Yaw,0f) * Vector3.right;
                    var offset = (spawned % 3 - 1) * 1.1f;
                    enemy = pool.Spawn(director.Player,director.World,
                        director.Player.transform.position + forward * 7f + right * offset,
                        spawned % 3 == 2);
                    spawned++;
                }

                if (enemy != null && enemy.IsActive)
                {
                    AimAt(enemy.transform.position + Vector3.up);
                    var weapon = director.Player.Weapon;
                    if (weapon.Ammunition <= 3 && !weapon.IsReloading)
                        weapon.Tick(false,false,true);
                    else
                        weapon.Tick(true,true,false);
                }

                sawReload |= director.Player.Weapon.IsReloading;
                var strafe = director.Player.transform.right * Mathf.Sin(elapsed * 2.1f) * 2.8f;
                var retreat = enemy != null && enemy.IsActive
                    && Vector3.Distance(enemy.transform.position,director.Player.transform.position) < 3.8f
                        ? -director.Player.transform.forward * 2.2f : Vector3.zero;
                controller.Move((strafe + retreat) * Time.deltaTime);
                yield return null;
            }
            var defeated = director.Kills - initialKills;
            if (defeated < 4 || !sawReload || director.Player.Health.IsDead)
            {
                Debug.LogError($"FOGBOUND_PORTFOLIO_GUN_FAIL kills={defeated} reload={sawReload} hp={director.Player.Health.Current}");
                Application.Quit(2);
                yield return new WaitForSecondsRealtime(999f);
                yield break;
            }
            Debug.Log($"FOGBOUND_PORTFOLIO_GUN_PASS kills={defeated} reload={sawReload} hp={director.Player.Health.Current}");
            yield return new WaitForSecondsRealtime(2.5f);
        }

        private static IEnumerator RunBladeCombat()
        {
            yield return PrepareLevel(1,WeaponType.LongBlade);
            var director = GameDirector.Instance;
            var path = MazePathfinder.FindPath(director.World.Layout,
                director.World.Layout.Start,director.World.Layout.Goal);
            var controller = director.Player.GetComponent<CharacterController>();
            yield return MoveTo(controller,director.World.EntryPosition,4.5f);
            yield return MoveTo(controller,director.World.CellToWorld(path[Mathf.Min(1,path.Count - 1)]),4.2f);
            var pool = FindFirstObjectByType<EnemyPool>();
            var forward = Quaternion.Euler(0f,director.CameraRig.Yaw,0f) * Vector3.forward;
            var enemy = pool.Spawn(director.Player,director.World,
                director.Player.transform.position + forward * 2.35f,true);
            var elapsed = 0f;
            while (elapsed < 7f)
            {
                elapsed += Time.deltaTime;
                if (enemy == null || !enemy.IsActive)
                {
                    forward = Quaternion.Euler(0f,director.CameraRig.Yaw,0f) * Vector3.forward;
                    enemy = pool.Spawn(director.Player,director.World,
                        director.Player.transform.position + forward * 2.35f,elapsed > 3f);
                }
                AimAt(enemy.transform.position + Vector3.up);
                director.Player.Weapon.Tick(false,true,false);
                var separation = enemy.transform.position - director.Player.transform.position;
                separation.y = 0f;
                if (separation.magnitude < 1.9f)
                    controller.Move(-separation.normalized * (2.8f * Time.deltaTime));
                else if (separation.magnitude > 2.8f)
                    controller.Move(separation.normalized * (1.6f * Time.deltaTime));
                yield return null;
            }
            yield return new WaitForSecondsRealtime(2f);
        }

        private static IEnumerator MoveTo(CharacterController controller,Vector3 destination,float speed)
        {
            var director = GameDirector.Instance;
            var timeout = 0f;
            while (timeout < 5f)
            {
                timeout += Time.deltaTime;
                var delta = destination - director.Player.transform.position;
                delta.y = 0f;
                if (delta.magnitude < .12f) yield break;
                var yaw = Mathf.Atan2(delta.x,delta.z) * Mathf.Rad2Deg;
                director.CameraRig.TickLook(
                    new Vector2(Mathf.DeltaAngle(director.CameraRig.Yaw,yaw),0f),false);
                controller.Move(Vector3.ClampMagnitude(delta,speed * Time.deltaTime));
                yield return null;
            }
        }

        private static void AimAt(Vector3 position)
        {
            var director = GameDirector.Instance;
            var delta = position - director.Player.transform.position;
            var yaw = Mathf.Atan2(delta.x,delta.z) * Mathf.Rad2Deg;
            director.CameraRig.TickLook(
                new Vector2(Mathf.DeltaAngle(director.CameraRig.Yaw,yaw),0f),false);
        }

        private static void CaptureFrame(string output)
        {
            // Render explicitly so a hidden/background smoke-test window still produces evidence.
            var camera = GameDirector.Instance.CameraRig.Camera;
            foreach(var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = .31f;
            }
            Canvas.ForceUpdateCanvases();
            foreach(var label in FindObjectsByType<LocalizedLabel>(FindObjectsSortMode.None)) label.Refresh();
            Canvas.ForceUpdateCanvases();
            var target = new RenderTexture(Screen.width, Screen.height, 24);
            var previous = RenderTexture.active;
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var texture = new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            texture.ReadPixels(new Rect(0,0,target.width,target.height),0,0);
            texture.Apply();
            File.WriteAllBytes(output, texture.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = previous;
            Destroy(target);
            Destroy(texture);
        }
    }
}
