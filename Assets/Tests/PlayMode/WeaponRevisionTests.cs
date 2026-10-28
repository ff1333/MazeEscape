using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace FogboundMaze.Tests
{
    public sealed class WeaponRevisionTests
    {
        private GameDirector game;
        private readonly List<GameObject> created = new();
        private readonly Vector3 arena = new(1000f, 0f, 1000f);

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SceneManager.LoadScene("Main");
            yield return null;
            yield return null;
            game = GameDirector.Instance;
            Assert.That(game.Hud.transform.Find("Title Screen/Version").GetComponent<Text>().text,
                Is.EqualTo("v" + ReleaseVersion.Current));
            game.ShowLevelSelection(); game.SelectLevel(1);
            game.SelectWeapon(WeaponType.SubmachineGun); game.BeginRun();
            game.enabled = false;
            game.Player.enabled = false;
            game.CameraRig.enabled = false;
            game.Player.Teleport(arena);
            game.Player.transform.rotation = Quaternion.identity;
            game.CameraRig.transform.SetPositionAndRotation(arena + new Vector3(0,1.35f,-2), Quaternion.identity);
            Physics.SyncTransforms();
        }

        private EnemyAgent Enemy(Vector3 offset)
        {
            var root = new GameObject("Test Target"); created.Add(root);
            var controller = root.AddComponent<CharacterController>();
            controller.height = 1.9f; controller.center = Vector3.up * .95f; controller.radius = .3f;
            root.AddComponent<Health>();
            var enemy = root.AddComponent<EnemyAgent>();
            enemy.Spawn(game.Player, game.World, arena + offset, true);
            enemy.enabled = false;
            Physics.SyncTransforms();
            return enemy;
        }

        private GameObject Wall(Vector3 offset, Vector3 size)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); created.Add(wall);
            wall.transform.position = arena + offset; wall.transform.localScale = size;
            Physics.SyncTransforms();
            return wall;
        }

        [UnityTest]
        public IEnumerator HeldFireUsesCooldownAndManualReloadTransfersReserveAmmunition()
        {
            var enemy = Enemy(Vector3.forward * 8f);
            var gun = game.Player.Weapon;
            gun.Tick(true, false, false);
            gun.Tick(true, false, false);
            Assert.That(gun.Ammunition, Is.EqualTo(29));
            Assert.That(enemy.GetComponent<Health>().Current, Is.EqualTo(125f - WeaponController.GunDamage));
            yield return new WaitForSeconds(.13f);
            gun.Tick(true, false, false);
            Assert.That(gun.Ammunition, Is.EqualTo(28));
            gun.Tick(false, false, true);
            Assert.That(gun.IsReloading, Is.True);
            gun.Tick(true, false, false);
            Assert.That(gun.Ammunition, Is.EqualTo(28));
            yield return new WaitForSeconds(1.5f);
            Assert.That(gun.IsReloading, Is.False);
            Assert.That(gun.Ammunition, Is.EqualTo(30));
            Assert.That(gun.ReserveAmmunition, Is.EqualTo(88));
            Assert.That(gun.ReloadReady, Is.True);
            yield return null;
            game.Hud.Refresh(game, game.Player);
            Assert.That(game.Hud.transform.Find("Weapon HUD/Ammo").GetComponent<Text>().text, Is.EqualTo("30 / 088"));
            Assert.That(game.Hud.transform.Find("Weapon HUD/State").GetComponent<Text>().text, Is.EqualTo("READY"));
            gun.Tick(true, false, false);
            Assert.That(gun.ReloadReady, Is.False);
            Assert.That(gun.Ammunition, Is.EqualTo(29));
        }

        [UnityTest]
        public IEnumerator ReloadPauseFreezesProgressAndReadyExpires()
        {
            var gun = game.Player.Weapon;
            gun.Tick(true, false, false);
            gun.Tick(false, false, true);
            yield return new WaitForSeconds(.2f);
            game.SetPaused(true);
            yield return null;
            var progress = gun.ReloadProgress;
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.That(gun.ReloadProgress, Is.EqualTo(progress).Within(.001f));
            Assert.That(gun.IsReloading, Is.True);
            Assert.That(gun.Ammunition, Is.EqualTo(29));
            Assert.That(gun.ReloadReady, Is.False);
            game.SetPaused(false);
            yield return new WaitForSeconds(1.3f);
            Assert.That(gun.ReloadReady, Is.True);
            yield return new WaitForSeconds(.9f);
            Assert.That(gun.ReloadReady, Is.False);
        }

        [UnityTest]
        public IEnumerator ReloadDeathDoesNotRefillOrSignalReady()
        {
            var gun = game.Player.Weapon;
            gun.Tick(true, false, false);
            gun.Tick(false, false, true);
            game.Player.Health.Damage(200f);
            yield return new WaitForSeconds(1.5f);
            Assert.That(gun.ReloadReady, Is.False);
            Assert.That(gun.IsReloading, Is.False);
            Assert.That(gun.Ammunition, Is.EqualTo(29));
        }

        [UnityTest]
        public IEnumerator ChangingWeaponCancelsReloadCompletion()
        {
            var gun = game.Player.Weapon;
            gun.Tick(true, false, false);
            gun.Tick(false, false, true);
            gun.Equip(WeaponType.LongBlade);
            yield return new WaitForSeconds(1.5f);
            Assert.That(gun.IsReloading, Is.False);
            Assert.That(gun.ReloadReady, Is.False);
            Assert.That(gun.Ammunition, Is.EqualTo(29));
            Assert.That(gun.ReserveAmmunition, Is.EqualTo(90));
            gun.Equip(WeaponType.SubmachineGun);
            Assert.That(gun.Ammunition, Is.EqualTo(29));
        }

        [UnityTest]
        public IEnumerator EmptyMagazineNeverAutoReloads_AndManualReloadIsRequired()
        {
            var gun = game.Player.Weapon;
            for (var shot = 0; shot < WeaponController.GunMagazineSize; shot++)
            {
                gun.Tick(true,false,false);
                yield return new WaitForSeconds(.11f);
            }
            Assert.That(gun.Ammunition,Is.Zero);
            gun.Tick(true,false,false);
            yield return new WaitForSeconds(.4f);
            Assert.That(gun.IsReloading,Is.False);
            Assert.That(gun.Ammunition,Is.Zero);
            Assert.That(gun.EmptyFeedback,Is.True);
            gun.Tick(false,false,true);
            Assert.That(gun.IsReloading,Is.True);
            yield return new WaitForSeconds(WeaponController.ReloadDuration + .1f);
            Assert.That(gun.Ammunition,Is.EqualTo(WeaponController.GunMagazineSize));
            Assert.That(gun.ReserveAmmunition,Is.EqualTo(60));
        }

        [UnityTest]
        public IEnumerator GunDoesNotShootThroughWallOrBeyondRange()
        {
            var enemy = Enemy(Vector3.forward * 8f);
            var wall = Wall(new Vector3(0,1,2), new Vector3(4,3,.2f));
            game.Player.Weapon.Tick(true, false, false);
            Assert.That(enemy.GetComponent<Health>().Current, Is.EqualTo(125f));
            Object.Destroy(wall);
            enemy.Spawn(game.Player, game.World, arena + Vector3.forward * 36f, true);
            enemy.enabled = false;
            yield return new WaitForSeconds(.13f);
            Physics.SyncTransforms();
            game.Player.Weapon.Tick(true, false, false);
            Assert.That(enemy.GetComponent<Health>().Current, Is.EqualTo(125f));
        }

        [UnityTest]
        public IEnumerator MuzzleBehindCoverCannotExploitClearCameraRay()
        {
            var enemy = Enemy(Vector3.forward * 8f);
            var muzzle = game.Player.GetComponent<PlayerVisual>().GunMuzzle;
            var cover = Vector3.Lerp(arena + Vector3.up * 1.35f, muzzle, .7f);
            Wall(cover - arena, new Vector3(.10f,1f,.15f));
            game.Player.Weapon.Tick(true,false,false);
            Assert.That(enemy.GetComponent<Health>().Current, Is.EqualTo(125f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator BladeReachesThreePointTwoMetersAfterWindupAndHitsOnce()
        {
            var enemy = Enemy(Vector3.forward * 3.2f);
            enemy.gameObject.AddComponent<SphereCollider>().center = Vector3.up;
            game.Player.Weapon.Equip(WeaponType.LongBlade);
            Physics.SyncTransforms();
            game.Player.Weapon.Tick(true,false,false);
            Assert.That(enemy.GetComponent<Health>().Current, Is.EqualTo(125f));
            yield return new WaitForSeconds(.2f);
            Assert.That(enemy.GetComponent<Health>().Current, Is.EqualTo(55f));
            Assert.That(game.Hud.transform.Find("Hit Details").GetComponent<Text>().text, Is.EqualTo("HIT  70"));
        }

        [UnityTest]
        public IEnumerator BladeRejectsBehindOutOfRangeAndOccludedTargets()
        {
            var far = Enemy(Vector3.forward * 3.6f);
            var behind = Enemy(Vector3.back * 2f);
            var blocked = Enemy(new Vector3(1.2f,0,2f));
            Wall(new Vector3(.6f,1f,1f), new Vector3(.6f,3f,.3f));
            game.Player.Weapon.Equip(WeaponType.LongBlade);
            game.Player.Weapon.Tick(true,false,false);
            yield return new WaitForSeconds(.2f);
            foreach(var enemy in new[]{far,behind,blocked})
                Assert.That(enemy.GetComponent<Health>().Current, Is.EqualTo(125f), enemy.transform.position.ToString());
        }

        [UnityTest]
        public IEnumerator DeathDuringWindupCancelsPendingDamage()
        {
            var enemy = Enemy(Vector3.forward * 2f);
            game.Player.Weapon.Equip(WeaponType.LongBlade);
            game.Player.Weapon.Tick(true,false,false);
            game.Player.Health.Damage(200f);
            yield return new WaitForSeconds(.2f);
            Assert.That(enemy.GetComponent<Health>().Current,Is.EqualTo(125f));
            Assert.That(game.Player.Weapon.IsSwinging,Is.False);
        }

        [UnityTest]
        public IEnumerator NearbyZombieCannotDamagePlayerThroughWall()
        {
            var enemy = Enemy(Vector3.forward * 1.1f);
            var wall = Wall(new Vector3(0,1,.55f),new Vector3(3,3,.2f));
            enemy.enabled = true;
            yield return new WaitForSeconds(.2f);
            Assert.That(game.Player.Health.Current, Is.EqualTo(100f));
            Object.Destroy(wall);
            yield return new WaitForSeconds(1f);
            Assert.That(game.Player.Health.Current, Is.LessThan(100f));
        }

        [UnityTest]
        public IEnumerator GraphicsAreRightHandedAndLeavingCancelsWindup()
        {
            yield return null;
            var visual = game.Player.GetComponent<PlayerVisual>();
            Assert.That(visual.HeldWeapon.name, Is.EqualTo("SMG"));
            Assert.That(game.Player.transform.InverseTransformPoint(visual.HeldWeapon.position).x, Is.GreaterThan(0f));
            var enemy = Enemy(Vector3.forward * 2f);
            game.Player.Weapon.Equip(WeaponType.LongBlade);
            game.Player.Weapon.Tick(true,false,false);
            game.ReturnToLoadout();
            yield return new WaitForSeconds(.3f);
            Assert.That(enemy.GetComponent<Health>().Current, Is.EqualTo(125f));
            Assert.That(game.Player.Weapon.IsSwinging, Is.False);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            foreach(var value in created) if(value != null) Object.Destroy(value);
            created.Clear();
            yield return null;
        }
    }
}
