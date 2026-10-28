using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FogboundMaze.Tests
{
    public sealed class EnemyPerceptionTests
    {
        private GameDirector game;
        private readonly List<GameObject> created = new();
        private Vector3 origin;
        private Vector3 forward;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SceneManager.LoadScene("Main");
            yield return null;
            yield return null;
            game = GameDirector.Instance;
            game.ShowLevelSelection();
            game.SelectLevel(1);
            game.ConfirmLoadout();
            game.BeginRun();
            game.enabled = false;
            game.Player.enabled = false;
            var start = game.World.Layout.Start;
            var next = game.World.Layout.GetOpenNeighbors(start).First();
            origin = game.World.CellToWorld(start);
            forward = game.World.CellToWorld(next) - origin;
            forward.y = 0f;
            game.Player.Teleport(origin + forward.normalized * 4f + Vector3.up * .1f);
            Physics.SyncTransforms();
        }

        private EnemyAgent CreateEnemy(Vector3 position, Vector3 facing)
        {
            var root = new GameObject("Perception Test Enemy");
            created.Add(root);
            var controller = root.AddComponent<CharacterController>();
            controller.height = 1.9f;
            controller.center = Vector3.up * .95f;
            root.AddComponent<Health>();
            var enemy = root.AddComponent<EnemyAgent>();
            enemy.Spawn(game.Player, game.World, position, false);
            enemy.transform.rotation = Quaternion.LookRotation(facing.normalized);
            return enemy;
        }

        [UnityTest]
        public IEnumerator VisionUsesFieldOfViewAndWallOcclusion()
        {
            var enemy = CreateEnemy(origin, forward);
            yield return null;
            Assert.That(enemy.CanSeePlayer, Is.True);
            Assert.That(enemy.State, Is.EqualTo(EnemyState.Chase));

            enemy.transform.rotation = Quaternion.LookRotation(-forward.normalized);
            yield return null;
            Assert.That(enemy.CanSeePlayer, Is.False);
            Assert.That(enemy.State, Is.EqualTo(EnemyState.Search));

            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            created.Add(wall);
            wall.transform.position = Vector3.Lerp(enemy.transform.position, game.Player.transform.position, .5f) + Vector3.up;
            wall.transform.localScale = new Vector3(3f, 3f, .35f);
            enemy.transform.rotation = Quaternion.LookRotation(forward.normalized);
            Physics.SyncTransforms();
            yield return null;
            Assert.That(enemy.CanSeePlayer, Is.False);
        }

        [UnityTest]
        public IEnumerator SoundCreatesInvestigationWithoutExactContinuousTracking()
        {
            var enemy = CreateEnemy(origin, -forward);
            var sound = origin + Vector3.right * 2f;
            NoiseSystem.Emit(sound, 8f, NoiseKind.Blade, game.Player);
            yield return null;
            Assert.That(enemy.HeardRecently, Is.True);
            Assert.That(enemy.CanSeePlayer, Is.False);
            Assert.That(enemy.State, Is.EqualTo(EnemyState.Investigate));
            Assert.That(enemy.LastKnownPosition, Is.EqualTo(sound));
        }

        [UnityTest]
        public IEnumerator GroupAlertHasBoundedRadius_AndPoolSpawnClearsMemory()
        {
            var near = CreateEnemy(origin, -forward);
            var far = CreateEnemy(origin + Vector3.right * 20f, -forward);
            NoiseSystem.Emit(origin + Vector3.right, 10f, NoiseKind.EnemyAlert, near);
            yield return null;
            Assert.That(near.HeardRecently, Is.False, "The sender must not hear its own alert.");
            Assert.That(far.HeardRecently, Is.False, "Alert must not cross its configured radius.");

            NoiseSystem.Emit(origin + Vector3.right, 10f, NoiseKind.Gunshot, game.Player);
            yield return null;
            Assert.That(near.HasTargetMemory, Is.True);
            near.gameObject.SetActive(false);
            near.Spawn(game.Player, game.World, origin, false);
            Assert.That(near.HasTargetMemory, Is.False);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            foreach (var value in created) if (value != null) Object.Destroy(value);
            created.Clear();
            yield return null;
        }
    }
}
