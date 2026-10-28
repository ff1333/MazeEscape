using NUnit.Framework;

namespace FogboundMaze.Tests
{
    public sealed class EnemyDecisionTests
    {
        [TestCase(true, false, true, 1.2f, EnemyState.Attack)]
        [TestCase(true, false, true, 8f, EnemyState.Chase)]
        [TestCase(false, true, true, 8f, EnemyState.Investigate)]
        [TestCase(false, false, true, 8f, EnemyState.Search)]
        [TestCase(false, false, false, 8f, EnemyState.Patrol)]
        public void UtilityDecision_SelectsHighestValidIntent(
            bool visible, bool heard, bool memory, float distance, EnemyState expected)
        {
            var result = EnemyDecisionModel.Evaluate(new EnemyDecisionContext(visible, heard, memory, distance));
            Assert.That(result.State, Is.EqualTo(expected));
        }

        [Test]
        public void HearingNeverScoresAboveDirectAttack()
        {
            var result = EnemyDecisionModel.Evaluate(new EnemyDecisionContext(true, true, true, 1f));
            Assert.That(result.AttackScore, Is.GreaterThan(result.InvestigateScore));
            Assert.That(result.State, Is.EqualTo(EnemyState.Attack));
        }
    }
}
