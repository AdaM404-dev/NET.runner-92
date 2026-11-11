using System;
using NUnit.Framework;

namespace NetRunner.Core.Tests
{
    /// <summary>
    /// EditMode tests of the patrol walk. The numbers come from
    /// docs/systems/enemy-k7.md: turning at 115°/s, walking once within 25°
    /// of the next point, waiting 1.15 s at either end. Headings: 0 is +z
    /// (north), 90 is +x (east).
    /// </summary>
    public sealed class PatrolTests
    {
        private const float Frame = 1f / 60f;

        // A half turn at 115°/s comes within 25° of the way back after (180 - 25) / 115 = 1.35 s.
        private const float TurnRoundSeconds = 155f / 115f;

        [Test]
        public void ItStartsOnTheFirstPointFacingTheSecond()
        {
            var patrol = new Patrol(new[] { (2f, 3f), (2f, 10f) }, 1f);

            Assert.That(patrol.Position, Is.EqualTo((2f, 3f)));
            Assert.That(patrol.Heading, Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void ARouteNeedsAtLeastTwoPoints()
        {
            Assert.Throws<ArgumentException>(() => new Patrol(new[] { (0f, 0f) }, 1f));
        }

        [Test]
        public void ItWalksAtItsSpeed()
        {
            var patrol = new Patrol(new[] { (0f, 0f), (10f, 0f) }, 1.5f);

            Run(patrol, 2f);

            Assert.That(patrol.Position.X, Is.EqualTo(3f).Within(1e-3f));
            Assert.That(patrol.Position.Z, Is.EqualTo(0f));
            Assert.That(patrol.DistanceWalked, Is.EqualTo(3f).Within(1e-3f));
            Assert.That(patrol.Heading, Is.EqualTo(90f).Within(1e-3f));
        }

        [Test]
        public void ItTurnsAtOneHundredAndFifteenDegreesASecondWithoutWalking()
        {
            var patrol = new Patrol(new[] { (0f, 0f), (10f, 0f) }, 1f);
            patrol.Place((0f, 0f), 0f);

            Run(patrol, 0.4f);

            Assert.That(patrol.Heading, Is.EqualTo(46f).Within(0.01f));
            Assert.That(patrol.IsWalking, Is.False);
            Assert.That(patrol.Position, Is.EqualTo((0f, 0f)));
        }

        [Test]
        public void ItWalksOnceItFacesTheNextPointToWithinTwentyFiveDegrees()
        {
            // North to east is 90°; within 25° of east after (90 - 25) / 115 = 0.57 s.
            var patrol = new Patrol(new[] { (0f, 0f), (10f, 0f) }, 1f);
            patrol.Place((0f, 0f), 0f);

            Run(patrol, 0.55f);
            Assert.That(patrol.IsWalking, Is.False);

            Run(patrol, 0.05f);
            Assert.That(patrol.IsWalking, Is.True);
        }

        [Test]
        public void AtTheEndItTurnsRoundOnTheSpotThenWalksBack()
        {
            var patrol = new Patrol(new[] { (0f, 0f), (1f, 0f) }, 1f);
            Run(patrol, 1f);
            Assert.That(patrol.Position.X, Is.EqualTo(1f).Within(Patrol.ArrivalDistance));

            Run(patrol, TurnRoundSeconds - 0.05f);
            Assert.That(patrol.IsWalking, Is.False);
            Assert.That(patrol.Position.X, Is.EqualTo(1f).Within(Patrol.ArrivalDistance));

            Run(patrol, 0.1f);
            Assert.That(patrol.IsWalking, Is.True);
            Assert.That(patrol.Position.X, Is.LessThan(1f - Patrol.ArrivalDistance));
        }

        [Test]
        public void AtAnEndItWaitsEvenWhenItAlreadyFacesBack()
        {
            var patrol = new Patrol(new[] { (0f, 0f), (1f, 0f) }, 1f);
            Run(patrol, 1f);
            patrol.Place(patrol.Position, -90f);

            Run(patrol, 1.1f);
            Assert.That(patrol.IsWalking, Is.False);

            Run(patrol, 0.1f);
            Assert.That(patrol.IsWalking, Is.True);
        }

        [Test]
        public void ItGoesStraightOnPastPointsInTheMiddle()
        {
            var patrol = new Patrol(new[] { (0f, 0f), (1f, 0f), (2f, 0f) }, 1f);

            Run(patrol, 1.5f);

            Assert.That(patrol.Position.X, Is.EqualTo(1.5f).Within(0.02f));
        }

        [Test]
        public void ItWalksBackAndForth()
        {
            // 1 m legs at 1 m/s: out in 1 s, turn round, back in 1 s, turn round, out again.
            var patrol = new Patrol(new[] { (0f, 0f), (1f, 0f) }, 1f);

            Run(patrol, 2f + TurnRoundSeconds + 0.05f);
            Assert.That(patrol.Position.X, Is.EqualTo(0f).Within(Patrol.ArrivalDistance));

            Run(patrol, TurnRoundSeconds + 0.5f);
            Assert.That(patrol.Position.X, Is.GreaterThan(0.3f).And.LessThan(0.7f));
            Assert.That(patrol.Heading, Is.EqualTo(90f).Within(0.01f));
        }

        [TestCase(1f, 0f)]
        [TestCase(-1f, 0f)]
        [TestCase(1f, 1f)]
        public void AHalfTurnAtAnEndNeverPassesSouth(float endX, float endZ)
        {
            // The two ways round are equally short; the K-7 has always turned through north on its east-west routes.
            var patrol = new Patrol(new[] { (0f, 0f), (endX, endZ) }, 1f);
            float distance = MathF.Sqrt(endX * endX + endZ * endZ);
            Run(patrol, distance + 0.05f);

            for (int frame = 0; frame < 90; frame++)
            {
                patrol.Step(Frame);
                Assert.That(MathF.Abs(patrol.Heading), Is.LessThan(179f), $"Passed south at frame {frame}");
            }
        }

        [Test]
        public void PlacedElsewhereItWalksOnToTheSamePoint()
        {
            var patrol = new Patrol(new[] { (0f, 0f), (10f, 0f) }, 1f);
            Run(patrol, 0.5f);

            patrol.Place((5f, 0f), 90f);
            patrol.Step(Frame);

            Assert.That(patrol.Position.X, Is.EqualTo(5f + Frame).Within(1e-4f));
        }

        [Test]
        public void WhileAttackingItTurnsToTheTargetAtTwoHundredAndFiveDegreesASecond()
        {
            // Phase 11: facing east along its route, a target due north is 90° away; 0.2 s at 205°/s turns 41°.
            var patrol = new Patrol(new[] { (0f, 0f), (10f, 0f) }, 1f);

            RunAttacking(patrol, 0.2f, (0f, 10f), false);

            Assert.That(patrol.Heading, Is.EqualTo(90f - 41f).Within(0.01f));
        }

        [Test]
        public void WhileAttackingItStandsStillUnlessAllowedToWalk()
        {
            var patrol = new Patrol(new[] { (0f, 0f), (10f, 0f) }, 1f);

            RunAttacking(patrol, 1f, (0f, 10f), false);

            Assert.That(patrol.Position, Is.EqualTo((0f, 0f)));
            Assert.That(patrol.IsWalking, Is.False);
        }

        [Test]
        public void WhileAttackingAndAllowedToWalkItKeepsToItsRouteWhateverItFaces()
        {
            // During a breach trace the K-7 walks its route on while it faces the player,
            // here far to the north of where it ends up, so it faces due north.
            var patrol = new Patrol(new[] { (0f, 0f), (10f, 0f) }, 1f);

            RunAttacking(patrol, 2f, (2f, 1000f), true);

            Assert.That(patrol.Position.X, Is.EqualTo(2f).Within(1e-3f));
            Assert.That(patrol.Position.Z, Is.EqualTo(0f));
            Assert.That(patrol.Heading, Is.EqualTo(0f).Within(1e-3f));
        }

        [Test]
        public void WithoutATargetTheAttackStepIsTheNormalStep()
        {
            var normal = new Patrol(new[] { (0f, 0f), (10f, 0f) }, 1f);
            var attackStep = new Patrol(new[] { (0f, 0f), (10f, 0f) }, 1f);

            Run(normal, 3f);
            RunAttacking(attackStep, 3f, null, true);

            Assert.That(attackStep.Position, Is.EqualTo(normal.Position));
            Assert.That(attackStep.Heading, Is.EqualTo(normal.Heading));
        }

        private static void RunAttacking(Patrol patrol, float seconds, (float X, float Z)? target, bool walk)
        {
            int frames = (int)MathF.Round(seconds / Frame);
            for (int i = 0; i < frames; i++)
            {
                patrol.Step(Frame, target, walk);
            }
        }

        private static void Run(Patrol patrol, float seconds)
        {
            int frames = (int)MathF.Round(seconds / Frame);
            for (int i = 0; i < frames; i++)
            {
                patrol.Step(Frame);
            }
        }
    }
}
