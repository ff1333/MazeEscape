using NUnit.Framework;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FogboundMaze.Tests
{
    public sealed class MazeCoreTests
    {
        [Test]
        public void Campaign_HasFrequentBranchesInsteadOfLongSingleCorridors()
        {
            foreach (var level in LevelCatalog.CreateDefault())
            {
                var maze = MazeGenerator.GenerateForLevel(level);
                var path = MazePathfinder.FindPath(maze, maze.Start, maze.Goal);
                var junctions = MazeComplexity.Measure(maze, level).Junctions;
                var firstChoice = -1;
                var routeChoices = 0;
                var gap = 0;
                var longestGap = 0;
                for (var i = 0; i < path.Count; i++)
                {
                    var exits = 0;
                    foreach (var neighbor in maze.GetOpenNeighbors(path[i])) exits++;
                    var choice = exits >= (i == 0 ? 2 : 3);
                    if (choice)
                    {
                        if (firstChoice < 0) firstChoice = i;
                        routeChoices++;
                        gap = 0;
                    }
                    else longestGap = Mathf.Max(longestGap, ++gap);
                }
                TestContext.WriteLine($"Level {level.levelNumber}: route={path.Count} junctions={junctions} routeChoices={routeChoices} firstChoice={firstChoice} longestGap={longestGap}");
                Assert.That(junctions, Is.GreaterThanOrEqualTo(Mathf.CeilToInt(maze.Width * maze.Height * 0.18f)));
                Assert.That(firstChoice, Is.InRange(0, 3));
                Assert.That(routeChoices, Is.GreaterThanOrEqualTo(Mathf.CeilToInt(path.Count / 6f)));
                Assert.That(longestGap, Is.LessThanOrEqualTo(8));
            }
        }

        [TestCase(7, 7, 1101)]
        [TestCase(10, 10, 4409)]
        [TestCase(15, 14, 10151)]
        public void GeneratedMaze_HasPathFromStartToGoal(int width, int height, int seed)
        {
            var maze = MazeGenerator.Generate(width, height, seed);
            var path = MazePathfinder.FindPath(maze, maze.Start, maze.Goal);

            Assert.That(path, Is.Not.Empty);
            Assert.That(path[0], Is.EqualTo(Vector2Int.zero));
            Assert.That(path[^1], Is.EqualTo(new Vector2Int(width - 1, height - 1)));
            var reached = new HashSet<Vector2Int> { maze.Start };
            var pending = new Queue<Vector2Int>();
            pending.Enqueue(maze.Start);
            while (pending.Count > 0)
            {
                var cell = pending.Dequeue();
                foreach (var direction in MazeDirections.All)
                {
                    if (!maze[cell].IsOpen(direction)) continue;
                    var next = cell + MazeDirections.ToOffset(direction);
                    Assert.That(maze.Contains(next), Is.True, "Passage must not escape the grid");
                    Assert.That(maze[next].IsOpen(MazeDirections.Opposite(direction)), Is.True);
                    if (reached.Add(next)) pending.Enqueue(next);
                }
            }
            Assert.That(reached.Count, Is.EqualTo(width * height), "All branches must be reachable");
        }

        [Test]
        public void GeneratedMaze_IsDeterministicForSameSeed()
        {
            var first = MazeGenerator.Generate(9, 9, 3307);
            var second = MazeGenerator.Generate(9, 9, 3307);

            for (var x = 0; x < first.Width; x++)
            {
                for (var y = 0; y < first.Height; y++)
                {
                    Assert.That(first[x, y].Passages, Is.EqualTo(second[x, y].Passages));
                }
            }
        }

        [Test]
        public void Exploration_RevealsOnlyVisitedCells()
        {
            var maze = MazeGenerator.Generate(7, 7, 1101);
            var exploration = new MazeExploration(maze);
            Assert.That(exploration.VisitedCount, Is.EqualTo(1));
            Assert.That(exploration.IsVisited(maze.Start), Is.True);
            Assert.That(exploration.IsGoalDiscovered, Is.False);
            Assert.That(exploration.Reveal(new Vector2Int(-1, 0)), Is.False);
            foreach (var next in maze.GetOpenNeighbors(maze.Start))
            {
                Assert.That(exploration.Reveal(next), Is.True);
                Assert.That(exploration.Reveal(next), Is.False);
                Assert.That(exploration.VisitedCount, Is.EqualTo(2));
                return;
            }
            Assert.Fail("Generated maze start has no open neighbor.");
        }

        [Test]
        public void DefaultCampaign_ContainsValidatedProgression()
        {
            var levels = LevelCatalog.CreateDefault();
            Assert.That(levels.Count, Is.EqualTo(10));

            for (var i = 0; i < levels.Count; i++)
            {
                levels[i].Validate();
                Assert.That(levels[i].levelNumber, Is.EqualTo(i + 1));
                if (i > 0)
                {
                    Assert.That(levels[i].maxEnemies, Is.GreaterThanOrEqualTo(levels[i - 1].maxEnemies));
                    Assert.That(levels[i].spawnInterval, Is.LessThan(levels[i - 1].spawnInterval));
                }
            }

            Assert.That(levels[3].localFogZones, Is.GreaterThan(0));
            Assert.That(levels[4].eliteChance, Is.GreaterThan(0f));
            Assert.That(levels[5].environment, Is.EqualTo(DungeonCondition.Blackout));
            Assert.That(levels[6].miasma, Is.True);
        }

        [Test]
        public void DefaultCampaign_UsesTenDifferentSolvableMazes()
        {
            var fingerprints = new HashSet<string>();
            foreach (var level in LevelCatalog.CreateDefault())
            {
                var maze = MazeGenerator.GenerateForLevel(level);
                var path = MazePathfinder.FindPath(maze, maze.Start, maze.Goal);
                Assert.That(path, Is.Not.Empty, $"Level {level.levelNumber} must remain solvable.");
                Assert.That(fingerprints.Add(Fingerprint(maze)), Is.True, $"Level {level.levelNumber} repeated a layout.");
            }
        }

        [Test]
        public void LaterCampaignMazes_AreLargerAndUseMoreCandidateSelection()
        {
            var levels = LevelCatalog.CreateDefault();
            for (var i = 1; i < levels.Count; i++)
            {
                Assert.That(levels[i].width * levels[i].height,
                    Is.GreaterThan(levels[i - 1].width * levels[i - 1].height));
                Assert.That(levels[i].mazeCandidateCount, Is.GreaterThan(levels[i - 1].mazeCandidateCount));
                Assert.That(levels[i].targetRouteLength, Is.GreaterThan(levels[i - 1].targetRouteLength));
            }

            var first = MazeGenerator.GenerateForLevel(levels[0]);
            var last = MazeGenerator.GenerateForLevel(levels[^1]);
            var firstMetrics = MazeComplexity.Measure(first, levels[0]);
            var lastMetrics = MazeComplexity.Measure(last, levels[^1]);
            Assert.That(lastMetrics.SolutionLength, Is.GreaterThan(firstMetrics.SolutionLength));
            Assert.That(lastMetrics.Turns, Is.GreaterThan(firstMetrics.Turns));
            Assert.That(lastMetrics.DeadEnds, Is.GreaterThan(firstMetrics.DeadEnds));
        }

        [Test]
        public void Campaign_MainRouteLengthIncreasesEveryLevel()
        {
            var previousLength = 0;
            foreach (var level in LevelCatalog.CreateDefault())
            {
                var maze = MazeGenerator.GenerateForLevel(level);
                var length = MazePathfinder.FindPath(maze, maze.Start, maze.Goal).Count;
                Assert.That(length, Is.GreaterThan(previousLength),
                    $"Level {level.levelNumber} route must be longer than the previous level.");
                previousLength = length;
            }
        }

        private static string Fingerprint(MazeLayout maze)
        {
            var value = new StringBuilder(maze.Width * maze.Height * 2);
            value.Append(maze.Width).Append('x').Append(maze.Height).Append(':');
            for (var y = 0; y < maze.Height; y++)
            {
                for (var x = 0; x < maze.Width; x++)
                {
                    value.Append((int)maze[x, y].Passages).Append(',');
                }
            }

            return value.ToString();
        }
    }
}
