using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace FogboundMaze.Tests
{
    public sealed class CampaignMenuTests
    {
        private readonly Dictionary<string, int?> saved = new();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            foreach (var key in new[] { CampaignProgress.CompletionKey, "Fogbound.SelectedLevel", "Fogbound.UnlockedLevel" })
            {
                saved[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : null;
                PlayerPrefs.DeleteKey(key);
            }
            SceneManager.LoadScene("Main");
            yield return null;
            yield return null;
        }

        [TearDown]
        public void Restore()
        {
            foreach (var pair in saved)
            {
                if (pair.Value.HasValue) PlayerPrefs.SetInt(pair.Key, pair.Value.Value);
                else PlayerPrefs.DeleteKey(pair.Key);
            }
            PlayerPrefs.Save();
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator BootShowsTitle_ThenLockedLevels_ThenLoadout()
        {
            var game = GameDirector.Instance;
            Assert.That(game.Phase, Is.EqualTo(GamePhase.Title));
            Assert.That(game.Hud.transform.Find("Title Screen").gameObject.activeSelf, Is.True);
            Assert.That(game.Hud.transform.Find("Weapon Selection").gameObject.activeSelf, Is.False);
            var position = game.Player.transform.position;
            var cameraRotation = game.CameraRig.transform.rotation;
            game.Input.SetMobileMove(Vector2.up);
            game.Input.AddMobileLook(Vector2.one * 30f);
            yield return new WaitForSeconds(0.1f);
            Assert.That(game.Player.transform.position, Is.EqualTo(position));
            Assert.That(game.CameraRig.transform.rotation, Is.EqualTo(cameraRotation));
            game.Hud.transform.Find("Title Screen/Start").GetComponent<Button>().onClick.Invoke();
            Assert.That(game.Phase, Is.EqualTo(GamePhase.LevelSelect));
            for (var number = 1; number <= 10; number++)
                Assert.That(game.Hud.transform.Find($"Level Select/Level {number:00}").GetComponent<Button>().interactable,
                    Is.EqualTo(number == 1));
            Assert.That(game.SelectLevel(2), Is.False);
            Assert.That(game.SelectLevel(0), Is.False);
            Assert.That(game.SelectLevel(11), Is.False);
            game.Hud.transform.Find("Level Select/Level 01").GetComponent<Button>().onClick.Invoke();
            Assert.That(game.Phase, Is.EqualTo(GamePhase.Staging));
            Assert.That(game.Hud.transform.Find("Weapon Selection").gameObject.activeSelf, Is.True);
            Assert.That(game.Player.Weapon.Type, Is.EqualTo(WeaponType.SubmachineGun));
            Assert.That(game.LoadoutConfirmed, Is.False);
            Assert.That(game.Hud.transform.Find("Weapon Selection/Deploy"), Is.Not.Null);
            game.NextLevel();
            Assert.That(game.CurrentLevel.levelNumber, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator CompletionPersistsAcrossReload_EnteringAndDyingDoNotUnlock()
        {
            var game = GameDirector.Instance;
            game.ShowLevelSelection();
            game.SelectLevel(1);
            game.SelectWeapon(WeaponType.SubmachineGun);
            game.BeginRun();
            game.Player.Health.Damage(200f);
            Assert.That(game.Progress.IsUnlocked(2), Is.False);
            game.Retry();
            game.SelectWeapon(WeaponType.SubmachineGun);
            game.BeginRun();
            game.CompleteLevel();
            Assert.That(game.Progress.IsCompleted(1), Is.True);
            SceneManager.LoadScene("Main");
            yield return null;
            yield return null;
            game = GameDirector.Instance;
            Assert.That(game.Phase, Is.EqualTo(GamePhase.Title));
            game.ShowLevelSelection();
            Assert.That(game.Progress.IsUnlocked(2), Is.True);
            Assert.That(game.Progress.IsUnlocked(3), Is.False);
            Assert.That(game.SelectLevel(2), Is.True);
            game.SelectWeapon(WeaponType.LongBlade);
            game.SetPaused(true);
            game.Hud.transform.Find("Pause/Levels").GetComponent<Button>().onClick.Invoke();
            Assert.That(game.Phase, Is.EqualTo(GamePhase.LevelSelect));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(game.ActiveEnemyCount, Is.Zero);
            Assert.That(game.Progress.IsUnlocked(3), Is.False);
        }

        [Test]
        public void LegacyCompletionMigrates_ButSelectedLevelAndGappedRecordsDoNotUnlock()
        {
            PlayerPrefs.SetInt("Fogbound.SelectedLevel", 10);
            var progress = new CampaignProgress();
            Assert.That(progress.IsUnlocked(2), Is.False);
            PlayerPrefs.SetInt("Fogbound.UnlockedLevel", 4);
            Assert.That(progress.CompletedCount, Is.EqualTo(3));
            progress.Complete(4);
            Assert.That(new CampaignProgress().IsUnlocked(5), Is.True);
            PlayerPrefs.SetInt(CampaignProgress.CompletionKey, 4);
            Assert.That(progress.IsUnlocked(2), Is.False);
            progress.Complete(10);
            Assert.That(progress.CompletedCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator GuideShowsEveryChapterWithoutUnlocking_AndEffectsStayBounded()
        {
            var game = GameDirector.Instance;
            game.Hud.transform.Find("Title Screen/Guide").GetComponent<Button>().onClick.Invoke();
            Assert.That(game.Phase, Is.EqualTo(GamePhase.Guide));
            var guide = game.Hud.transform.Find("Field Guide");
            for (var i = 1; i <= 10; i++)
            {
                guide.Find("Chapter " + i).GetComponent<Button>().onClick.Invoke();
                Assert.That(guide.Find("Chapter Title").GetComponent<Text>().text, Does.StartWith(i.ToString("00")));
            }
            Assert.That(game.Progress.IsUnlocked(2), Is.False);
            Assert.That(game.SelectLevel(1), Is.False);
            guide.Find("Back").GetComponent<Button>().onClick.Invoke();
            Assert.That(game.Phase, Is.EqualTo(GamePhase.Title));
            var effects = CombatEffects.Ensure();
            for (var i = 0; i < 200; i++) CombatEffects.Tracer(Vector3.zero,Vector3.forward);
            Assert.That(effects.GetComponentsInChildren<LineRenderer>().Length,Is.EqualTo(64));
            yield return new WaitForSeconds(.25f);
            Assert.That(effects.ActiveStrokeCount,Is.Zero);
        }

        [UnityTest]
        public IEnumerator ReloadFeedbackCompletes_AndLeavingRunClearsTransientState()
        {
            var game = GameDirector.Instance;
            game.ShowLevelSelection(); game.SelectLevel(1); game.SelectWeapon(WeaponType.SubmachineGun);
            var weapon = game.Player.Weapon;
            weapon.Tick(false,true,false);
            Assert.That(weapon.Ammunition,Is.EqualTo(29));
            weapon.Tick(false,false,true);
            Assert.That(weapon.IsReloading,Is.True);
            yield return new WaitForSeconds(1.5f);
            Assert.That(weapon.Ammunition,Is.EqualTo(30));
            Assert.That(weapon.IsReloading,Is.False);
            weapon.Tick(false,true,false);
            weapon.Tick(false,false,true);
            game.ShowTitle();
            yield return new WaitForSeconds(1.3f);
            Assert.That(weapon.Type,Is.EqualTo(WeaponType.None));
            Assert.That(weapon.IsReloading,Is.False);
            Assert.That(CombatEffects.Ensure().ActiveStrokeCount,Is.Zero);
        }

        [UnityTest]
        public IEnumerator CharacterAssets_HaveTexturesAndWorkingAnimationClips()
        {
            foreach (var name in new[] { "Survivor", "Zombie", "EliteZombie" })
            {
                var model = Object.Instantiate(Resources.Load<GameObject>("Characters/" + name));
                var animation = model.GetComponentInChildren<Animation>();
                Assert.That(animation, Is.Not.Null);
                foreach (var clip in new[] { "Idle", "Walk", "Run", "Death" }) Assert.That(animation[clip], Is.Not.Null);
                foreach (var clip in new[] { "Idle", "Walk", "Run" })
                {
                    animation.Play(clip);
                    animation[clip].normalizedTime = 0.25f;
                    animation.Sample();
                    yield return null;
                    foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        Assert.That(skin.sharedMaterial.mainTexture, Is.Not.Null);
                        Assert.That(skin.sharedMesh.vertexCount, Is.GreaterThan(100));
                    }
                }
                animation.Play("Walk");
                animation["Walk"].normalizedTime = 0.1f;
                animation.Sample();
                var leg = System.Array.Find(model.GetComponentsInChildren<Transform>(), value => value.name == "UpperLeg.L");
                var firstPose = leg.localRotation;
                animation["Walk"].normalizedTime = 0.6f;
                animation.Sample();
                Assert.That(Quaternion.Angle(firstPose, leg.localRotation), Is.GreaterThan(5f), name + " animated leg");
                Object.Destroy(model);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator PooledZombie_ResetsDeathAndSwitchesToEliteModel()
        {
            var game = GameDirector.Instance;
            game.ShowLevelSelection();
            game.SelectLevel(1);
            game.SelectWeapon(WeaponType.SubmachineGun);
            game.BeginRun();
            game.enabled = false;
            var pool = Object.FindFirstObjectByType<EnemyPool>();
            var enemy = pool.Spawn(game.Player, game.World, game.World.EntryPosition, false);
            enemy.TakeDamage(200f);
            Assert.That(enemy.GetComponent<CharacterController>().enabled, Is.False);
            yield return new WaitForSeconds(1.2f);
            Assert.That(enemy.gameObject.activeSelf, Is.False);
            var reused = pool.Spawn(game.Player, game.World, game.World.EntryPosition, true);
            Assert.That(reused, Is.SameAs(enemy));
            Assert.That(reused.GetComponent<CharacterController>().enabled, Is.True);
            Assert.That(reused.State, Is.EqualTo(EnemyState.Wander));
            Assert.That(reused.transform.Find("Elite Visual").gameObject.activeSelf, Is.True);
            Assert.That(reused.transform.Find("Normal Visual").gameObject.activeSelf, Is.False);
            Assert.That(reused.GetComponent<Health>().Current, Is.EqualTo(125f));
        }
    }
}
